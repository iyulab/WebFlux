using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Bulkhead;
using Polly.Timeout;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// Polly 기반 회복탄력성 서비스 구현
/// 재시도, 회로차단기, 시간초과, 벌크헤드 패턴을 통한 안정성 보장
/// HTTP 요청, 파일 I/O, 외부 서비스 호출의 회복탄력성 제공
/// </summary>
public class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerInfo> _circuitBreakerStates;
    private readonly ConcurrentDictionary<string, BulkheadInfo> _bulkheadStates;
    private readonly ConcurrentBag<ResilienceEvent> _events;
    private readonly object _statsLock = new();

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _circuitBreakerStates = new ConcurrentDictionary<string, CircuitBreakerInfo>();
        _bulkheadStates = new ConcurrentDictionary<string, BulkheadInfo>();
        _events = new ConcurrentBag<ResilienceEvent>();

        _logger.LogInformation("ResilienceService initialized");
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryPolicy retryPolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(retryPolicy);

        var policy = CreateRetryPolicy<T>(retryPolicy);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await policy.ExecuteAsync(operation, cancellationToken);
            LogEvent(ResilienceEventType.Success, "Retry", DateTime.UtcNow - startTime);
            return result;
        }
        catch (Exception ex)
        {
            LogEvent(ResilienceEventType.Failure, "Retry", DateTime.UtcNow - startTime, ex);
            throw;
        }
    }

    public async Task<T> ExecuteWithCircuitBreakerAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        WebFlux.Core.Models.CircuitBreakerPolicy circuitBreakerPolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(circuitBreakerPolicy);

        var policy = CreateCircuitBreakerPolicy<T>(circuitBreakerPolicy);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await policy.ExecuteAsync(operation, cancellationToken);
            LogEvent(ResilienceEventType.Success, circuitBreakerPolicy.Name, DateTime.UtcNow - startTime);
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            LogEvent(ResilienceEventType.CircuitBreakerOpened, circuitBreakerPolicy.Name, DateTime.UtcNow - startTime, ex);
            throw;
        }
        catch (Exception ex)
        {
            LogEvent(ResilienceEventType.Failure, circuitBreakerPolicy.Name, DateTime.UtcNow - startTime, ex);
            throw;
        }
    }

    public async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        WebFlux.Core.Models.TimeoutPolicy timeoutPolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(timeoutPolicy);

        var policy = CreateTimeoutPolicy<T>(timeoutPolicy);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await policy.ExecuteAsync(operation, cancellationToken);
            LogEvent(ResilienceEventType.Success, "Timeout", DateTime.UtcNow - startTime);
            return result;
        }
        catch (TimeoutRejectedException ex)
        {
            LogEvent(ResilienceEventType.Timeout, "Timeout", DateTime.UtcNow - startTime, ex);
            throw;
        }
        catch (Exception ex)
        {
            LogEvent(ResilienceEventType.Failure, "Timeout", DateTime.UtcNow - startTime, ex);
            throw;
        }
    }

    public async Task<T> ExecuteWithBulkheadAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        WebFlux.Core.Models.BulkheadPolicy bulkheadPolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(bulkheadPolicy);

        var policy = CreateBulkheadPolicy<T>(bulkheadPolicy);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await policy.ExecuteAsync(operation, cancellationToken);
            LogEvent(ResilienceEventType.Success, bulkheadPolicy.Name, DateTime.UtcNow - startTime);
            return result;
        }
        catch (BulkheadRejectedException ex)
        {
            LogEvent(ResilienceEventType.BulkheadRejected, bulkheadPolicy.Name, DateTime.UtcNow - startTime, ex);
            throw;
        }
        catch (Exception ex)
        {
            LogEvent(ResilienceEventType.Failure, bulkheadPolicy.Name, DateTime.UtcNow - startTime, ex);
            throw;
        }
    }

    public async Task<T> ExecuteWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ResiliencePolicy resiliencePolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(resiliencePolicy);

        var policy = CreateCompositePolicy<T>(resiliencePolicy);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await policy.ExecuteAsync(operation, cancellationToken);
            LogEvent(ResilienceEventType.Success, resiliencePolicy.Name, DateTime.UtcNow - startTime);
            return result;
        }
        catch (Exception ex)
        {
            LogEvent(ResilienceEventType.Failure, resiliencePolicy.Name, DateTime.UtcNow - startTime, ex);
            throw;
        }
    }

    public async Task<T> ExecuteHttpWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> httpOperation,
        HttpResiliencePolicy httpResiliencePolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpOperation);
        ArgumentNullException.ThrowIfNull(httpResiliencePolicy);

        var policy = CreateCompositePolicy<T>(httpResiliencePolicy);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await policy.ExecuteAsync(httpOperation, cancellationToken);
            LogEvent(ResilienceEventType.Success, httpResiliencePolicy.Name, DateTime.UtcNow - startTime);
            return result;
        }
        catch (Exception ex)
        {
            LogEvent(ResilienceEventType.Failure, httpResiliencePolicy.Name, DateTime.UtcNow - startTime, ex);
            throw;
        }
    }

    public ResilienceStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            var events = _events.ToList();
            var totalExecutions = events.Count;
            var successfulExecutions = events.Count(e => e.EventType == ResilienceEventType.Success);
            var failedExecutions = events.Count(e => e.EventType == ResilienceEventType.Failure);
            var retryAttempts = events.Count(e => e.EventType == ResilienceEventType.Retry);
            var circuitBreakerOpenings = events.Count(e => e.EventType == ResilienceEventType.CircuitBreakerOpened);
            var timeoutOccurrences = events.Count(e => e.EventType == ResilienceEventType.Timeout);
            var bulkheadRejections = events.Count(e => e.EventType == ResilienceEventType.BulkheadRejected);

            var averageExecutionTime = events.Any()
                ? TimeSpan.FromTicks((long)events.Average(e => e.ExecutionTime.Ticks))
                : TimeSpan.Zero;

            return new ResilienceStatistics
            {
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions,
                RetryAttempts = retryAttempts,
                CircuitBreakerOpenings = circuitBreakerOpenings,
                TimeoutOccurrences = timeoutOccurrences,
                BulkheadRejections = bulkheadRejections,
                AverageExecutionTime = averageExecutionTime,
                CircuitBreakers = new Dictionary<string, CircuitBreakerInfo>(_circuitBreakerStates),
                Bulkheads = new Dictionary<string, BulkheadInfo>(_bulkheadStates),
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    public CircuitBreakerState GetCircuitBreakerState(string circuitBreakerName)
    {
        if (circuitBreakerName == null)
            throw new ArgumentNullException(nameof(circuitBreakerName));
        if (string.IsNullOrEmpty(circuitBreakerName))
            throw new ArgumentException("Circuit breaker name cannot be empty", nameof(circuitBreakerName));

        return _circuitBreakerStates.TryGetValue(circuitBreakerName, out var info)
            ? info.State
            : CircuitBreakerState.Closed;
    }

    public async Task SetCircuitBreakerStateAsync(string circuitBreakerName, bool open)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitBreakerName);

        _logger.LogInformation("Manual circuit breaker state change requested for {CircuitBreakerName}: {State}",
            circuitBreakerName, open ? "Open" : "Closed");

        await Task.CompletedTask;
    }

    public double GetBulkheadUtilization(string bulkheadName)
    {
        if (bulkheadName == null)
            throw new ArgumentNullException(nameof(bulkheadName));
        if (string.IsNullOrEmpty(bulkheadName))
            throw new ArgumentException("Bulkhead name cannot be empty", nameof(bulkheadName));

        return _bulkheadStates.TryGetValue(bulkheadName, out var info)
            ? info.Utilization
            : 0.0;
    }

    private IAsyncPolicy<T> CreateRetryPolicy<T>(RetryPolicy retryPolicy)
    {
        var nonGenericPolicy = Policy.Handle<Exception>(ex =>
            retryPolicy.ShouldRetry?.Invoke(ex) ?? true);

        var concretePolicy = retryPolicy.Strategy switch
        {
            WebFlux.Core.Models.RetryStrategy.Fixed => nonGenericPolicy.WaitAndRetryAsync(
                retryPolicy.MaxRetryAttempts,
                _ => retryPolicy.BaseDelay),

            WebFlux.Core.Models.RetryStrategy.Linear => nonGenericPolicy.WaitAndRetryAsync(
                retryPolicy.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromTicks(Math.Min(
                    (retryPolicy.BaseDelay.Ticks * retryAttempt) +
                    (retryPolicy.UseJitter ? Random.Shared.Next(0, (int)retryPolicy.BaseDelay.TotalMilliseconds) * TimeSpan.TicksPerMillisecond : 0),
                    retryPolicy.MaxDelay.Ticks))),

            WebFlux.Core.Models.RetryStrategy.ExponentialBackoff => nonGenericPolicy.WaitAndRetryAsync(
                retryPolicy.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromTicks(Math.Min(
                    (long)(retryPolicy.BaseDelay.Ticks * Math.Pow(2, retryAttempt - 1)) +
                    (retryPolicy.UseJitter ? Random.Shared.Next(0, (int)retryPolicy.BaseDelay.TotalMilliseconds) * TimeSpan.TicksPerMillisecond : 0),
                    retryPolicy.MaxDelay.Ticks))),

            _ => throw new ArgumentOutOfRangeException(nameof(retryPolicy.Strategy))
        };

        return concretePolicy.AsAsyncPolicy<T>();
    }

    private IAsyncPolicy<T> CreateCircuitBreakerPolicy<T>(WebFlux.Core.Models.CircuitBreakerPolicy circuitBreakerPolicy)
    {
        var nonGenericPolicy = Policy.Handle<Exception>()
            .CircuitBreakerAsync(
                circuitBreakerPolicy.FailureThreshold,
                circuitBreakerPolicy.DurationOfBreak);

        return nonGenericPolicy.AsAsyncPolicy<T>();
    }

    private IAsyncPolicy<T> CreateTimeoutPolicy<T>(WebFlux.Core.Models.TimeoutPolicy timeoutPolicy)
    {
        return timeoutPolicy.Strategy switch
        {
            WebFlux.Core.Models.TimeoutStrategy.Cooperative => Policy.TimeoutAsync<T>(
                timeoutPolicy.Timeout),

            WebFlux.Core.Models.TimeoutStrategy.Pessimistic => Policy.TimeoutAsync<T>(
                timeoutPolicy.Timeout),

            _ => throw new ArgumentOutOfRangeException(nameof(timeoutPolicy.Strategy))
        };
    }

    private IAsyncPolicy<T> CreateBulkheadPolicy<T>(WebFlux.Core.Models.BulkheadPolicy bulkheadPolicy)
    {
        return Policy.BulkheadAsync<T>(
            bulkheadPolicy.MaxParallelization,
            bulkheadPolicy.MaxQueuingActions);
    }

    private IAsyncPolicy<T> CreateCompositePolicy<T>(ResiliencePolicy resiliencePolicy)
    {
        var policies = new List<IAsyncPolicy<T>>();

        foreach (var policyType in resiliencePolicy.ExecutionOrder)
        {
            switch (policyType)
            {
                case PolicyType.Retry when resiliencePolicy.Retry != null:
                    policies.Add(CreateRetryPolicy<T>(resiliencePolicy.Retry));
                    break;

                case PolicyType.CircuitBreaker when resiliencePolicy.CircuitBreaker != null:
                    policies.Add(CreateCircuitBreakerPolicy<T>(resiliencePolicy.CircuitBreaker));
                    break;

                case PolicyType.Timeout when resiliencePolicy.Timeout != null:
                    policies.Add(CreateTimeoutPolicy<T>(resiliencePolicy.Timeout));
                    break;

                case PolicyType.Bulkhead when resiliencePolicy.Bulkhead != null:
                    policies.Add(CreateBulkheadPolicy<T>(resiliencePolicy.Bulkhead));
                    break;
            }
        }

        return policies.Count switch
        {
            0 => Policy.NoOpAsync<T>(),
            1 => policies[0],
            _ => Policy.WrapAsync(policies.ToArray())
        };
    }

    private void LogEvent(ResilienceEventType eventType, string policyName, TimeSpan executionTime, Exception? exception = null)
    {
        var resilienceEvent = new ResilienceEvent
        {
            EventType = eventType,
            PolicyName = policyName,
            ExecutionTime = executionTime,
            Exception = exception,
            Timestamp = DateTime.UtcNow
        };

        _events.Add(resilienceEvent);

        // 이벤트 수가 너무 많으면 오래된 것들 제거 (메모리 관리)
        if (_events.Count > 10000)
        {
            var eventsToKeep = _events.OrderByDescending(e => e.Timestamp).Take(5000).ToList();
            _events.Clear();
            foreach (var evt in eventsToKeep)
            {
                _events.Add(evt);
            }
        }
    }
}