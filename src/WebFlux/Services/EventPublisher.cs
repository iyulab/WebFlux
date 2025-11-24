using System.Collections.Concurrent;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 이벤트 발행자 구현체
/// 시스템 내에서 발생하는 이벤트를 발행하고 구독자에게 전달
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ConcurrentDictionary<Type, List<Func<ProcessingEvent, Task>>> _asyncHandlers = new();
    private readonly ConcurrentDictionary<Type, List<Action<ProcessingEvent>>> _syncHandlers = new();
    private readonly object _lock = new();
    private long _totalEventsPublished = 0;
    private long _publishErrors = 0;
    private readonly ConcurrentDictionary<string, long> _eventsByType = new();

    /// <summary>
    /// 이벤트를 비동기적으로 발행합니다.
    /// </summary>
    /// <param name="processingEvent">발행할 이벤트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public async Task PublishAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default)
    {
        if (processingEvent == null) throw new ArgumentNullException(nameof(processingEvent));

        Interlocked.Increment(ref _totalEventsPublished);
        var eventTypeName = processingEvent.GetType().Name;
        _eventsByType.AddOrUpdate(eventTypeName, 1, (key, count) => count + 1);

        try
        {
            var eventType = processingEvent.GetType();

            // 비동기 핸들러 실행
            if (_asyncHandlers.TryGetValue(eventType, out var asyncHandlers))
            {
                var tasks = asyncHandlers.Select(handler => handler(processingEvent));
                await Task.WhenAll(tasks);
            }

            // 동기 핸들러 실행 (백그라운드에서)
            if (_syncHandlers.TryGetValue(eventType, out var syncHandlers))
            {
                _ = Task.Run(() =>
                {
                    foreach (var handler in syncHandlers)
                    {
                        try
                        {
                            handler(processingEvent);
                        }
                        catch
                        {
                            // 개별 핸들러 오류는 무시 (로깅은 향후 추가)
                            Interlocked.Increment(ref _publishErrors);
                        }
                    }
                }, cancellationToken);
            }
        }
        catch
        {
            Interlocked.Increment(ref _publishErrors);
            throw;
        }
    }

    /// <summary>
    /// 이벤트를 동기적으로 발행합니다.
    /// </summary>
    /// <param name="processingEvent">발행할 이벤트</param>
    public void Publish(ProcessingEvent processingEvent)
    {
        _ = Task.Run(async () => await PublishAsync(processingEvent));
    }

    /// <summary>
    /// 이벤트 구독을 등록합니다.
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제용 IDisposable</returns>
    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : ProcessingEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(T);
        var wrappedHandler = new Func<ProcessingEvent, Task>(evt => handler((T)evt));

        lock (_lock)
        {
            if (!_asyncHandlers.ContainsKey(eventType))
            {
                _asyncHandlers[eventType] = new List<Func<ProcessingEvent, Task>>();
            }
            _asyncHandlers[eventType].Add(wrappedHandler);
        }

        return new EventSubscription(() =>
        {
            lock (_lock)
            {
                if (_asyncHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(wrappedHandler);
                    if (handlers.Count == 0)
                    {
                        _asyncHandlers.TryRemove(eventType, out _);
                    }
                }
            }
        });
    }

    /// <summary>
    /// 이벤트 구독을 등록합니다. (동기)
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제용 IDisposable</returns>
    public IDisposable Subscribe<T>(Action<T> handler) where T : ProcessingEvent
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(T);
        var wrappedHandler = new Action<ProcessingEvent>(evt => handler((T)evt));

        lock (_lock)
        {
            if (!_syncHandlers.ContainsKey(eventType))
            {
                _syncHandlers[eventType] = new List<Action<ProcessingEvent>>();
            }
            _syncHandlers[eventType].Add(wrappedHandler);
        }

        return new EventSubscription(() =>
        {
            lock (_lock)
            {
                if (_syncHandlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(wrappedHandler);
                    if (handlers.Count == 0)
                    {
                        _syncHandlers.TryRemove(eventType, out _);
                    }
                }
            }
        });
    }

    /// <summary>
    /// 모든 이벤트를 구독합니다.
    /// </summary>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제용 IDisposable</returns>
    public IDisposable SubscribeAll(Func<ProcessingEvent, Task> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var subscriptions = new List<IDisposable>();

        // Interface Provider 패턴: 기본 이벤트 타입만 등록
        // 소비자가 필요한 추가 이벤트 타입은 구독 시 자동 등록됨
        var knownEventTypes = new[]
        {
            typeof(ProcessingEvent)
        };

        foreach (var eventType in knownEventTypes)
        {
            lock (_lock)
            {
                if (!_asyncHandlers.ContainsKey(eventType))
                {
                    _asyncHandlers[eventType] = new List<Func<ProcessingEvent, Task>>();
                }
                _asyncHandlers[eventType].Add(handler);
            }

            subscriptions.Add(new EventSubscription(() =>
            {
                lock (_lock)
                {
                    if (_asyncHandlers.TryGetValue(eventType, out var handlers))
                    {
                        handlers.Remove(handler);
                    }
                }
            }));
        }

        return new CompositeEventSubscription(subscriptions);
    }

    /// <summary>
    /// 이벤트 발행 통계를 반환합니다.
    /// </summary>
    /// <returns>발행 통계</returns>
    public EventPublishingStatistics GetStatistics()
    {
        return new EventPublishingStatistics
        {
            TotalEventsPublished = _totalEventsPublished,
            EventsByType = new Dictionary<string, long>(_eventsByType),
            SubscriberCount = GetTotalSubscriberCount(),
            AveragePublishTimeMs = 0, // 실제 구현에서는 측정 필요
            PublishErrors = _publishErrors,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 총 구독자 수 계산
    /// </summary>
    private int GetTotalSubscriberCount()
    {
        lock (_lock)
        {
            var asyncCount = _asyncHandlers.Values.Sum(handlers => handlers.Count);
            var syncCount = _syncHandlers.Values.Sum(handlers => handlers.Count);
            return asyncCount + syncCount;
        }
    }
}

/// <summary>
/// 이벤트 구독 관리 클래스
/// </summary>
public class EventSubscription : IDisposable
{
    private readonly Action _unsubscribe;
    private bool _disposed = false;

    public EventSubscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _unsubscribe();
            _disposed = true;
        }
    }
}

/// <summary>
/// 복합 이벤트 구독 관리 클래스
/// </summary>
public class CompositeEventSubscription : IDisposable
{
    private readonly List<IDisposable> _subscriptions;
    private bool _disposed = false;

    public CompositeEventSubscription(List<IDisposable> subscriptions)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _disposed = true;
        }
    }
}

// 이벤트 클래스들 (기존 파일에서 이미 정의되어 있지 않다면)
public class CrawlStartedEvent : ProcessingEvent
{
    public override string EventType => "CrawlStarted";
    public List<string> StartUrls { get; set; } = new();
    public CrawlConfiguration Configuration { get; set; } = new();
}

public class UrlProcessingStartedEvent : ProcessingEvent
{
    public override string EventType => "UrlProcessingStarted";
    public string Url { get; set; } = string.Empty;
}

public class UrlProcessedEvent : ProcessingEvent
{
    public override string EventType => "UrlProcessed";
    public string Url { get; set; } = string.Empty;
    public int ContentLength { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int DiscoveredUrlCount { get; set; }
    public int ProcessingTimeMs { get; set; }
}

public class UrlProcessingFailedEvent : ProcessingEvent
{
    public override string EventType => "UrlProcessingFailed";
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class CrawlCompletedEvent : ProcessingEvent
{
    public override string EventType => "CrawlCompleted";
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan Duration { get; set; }
}

public class CrawlErrorEvent : ProcessingEvent
{
    public override string EventType => "CrawlError";
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class CrawlWarningEvent : ProcessingEvent
{
    public override string EventType => "CrawlWarning";
}