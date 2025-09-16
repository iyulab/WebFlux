using System.Collections.Concurrent;
using System.Threading.Channels;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 깊이 우선 크롤러
/// 한 경로를 끝까지 탐색한 후 다른 경로로 이동
/// </summary>
public class DepthFirstCrawler : BaseCrawler
{
    private readonly Stack<CrawlItem> _urlStack = new();
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly object _stackLock = new();

    public DepthFirstCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher)
    {
        _concurrencySemaphore = new SemaphoreSlim(3); // 깊이 우선은 낮은 동시성이 적합
    }

    /// <summary>
    /// 내부 크롤링 로직 - 깊이 우선 방식
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 콘텐츠 스트림</returns>
    protected override async IAsyncEnumerable<WebContent> CrawlInternalAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 큐에서 스택으로 시작 URL 이동
        InitializeStack();

        // 동시성 제한 설정
        var maxConcurrency = Math.Min(_configuration.MaxConcurrentRequests, 5);
        _concurrencySemaphore.Release(_concurrencySemaphore.CurrentCount);
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        // 처리 채널 생성
        var channel = Channel.CreateUnbounded<WebContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 백그라운드 처리 태스크 시작
        var processingTask = ProcessStackAsync(writer, semaphore, cancellationToken);

        try
        {
            // 결과를 스트리밍으로 반환
            await foreach (var content in reader.ReadAllAsync(cancellationToken))
            {
                yield return content;

                // 최대 페이지 수 확인
                lock (_lockObject)
                {
                    if (_successCount >= _configuration.MaxPages)
                    {
                        await StopAsync();
                        break;
                    }
                }
            }
        }
        finally
        {
            await writer.TryCompleteAsync();
            await processingTask;
            _isRunning = false;

            await _eventPublisher.PublishAsync(new CrawlCompletedEvent
            {
                TotalProcessed = _processedCount,
                SuccessCount = _successCount,
                ErrorCount = _errorCount,
                Duration = TimeSpan.Zero,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    /// <summary>
    /// 큐에서 스택으로 초기 URL 설정
    /// </summary>
    private void InitializeStack()
    {
        lock (_stackLock)
        {
            _urlStack.Clear();

            // 큐의 URL들을 스택으로 이동 (역순으로 처리되도록)
            var urls = new List<string>();
            while (_urlQueue.TryDequeue(out var url))
            {
                urls.Add(url);
            }

            // 역순으로 스택에 추가 (첫 번째 URL이 먼저 처리되도록)
            for (int i = urls.Count - 1; i >= 0; i--)
            {
                _urlStack.Push(new CrawlItem
                {
                    Url = urls[i],
                    Depth = 0,
                    ParentUrl = null
                });
            }
        }
    }

    /// <summary>
    /// 스택 기반 URL 처리
    /// </summary>
    /// <param name="writer">결과 채널 작성자</param>
    /// <param name="semaphore">동시성 제어 세마포어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessStackAsync(
        ChannelWriter<WebContent> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var activeTasks = new List<Task>();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                CrawlItem? currentItem = null;

                lock (_stackLock)
                {
                    if (_urlStack.Count == 0)
                    {
                        // 스택이 비어있으면 활성 작업 완료 대기
                        if (activeTasks.Count == 0)
                            break;
                    }
                    else
                    {
                        currentItem = _urlStack.Pop();
                    }
                }

                if (currentItem != null)
                {
                    // 새 작업 시작
                    var task = ProcessCrawlItemAsync(currentItem, writer, semaphore, cancellationToken);
                    activeTasks.Add(task);
                }

                // 완료된 작업들 정리
                activeTasks.RemoveAll(t => t.IsCompleted);

                // 동시성 제한 확인
                if (activeTasks.Count >= semaphore.CurrentCount + 1)
                {
                    await Task.WhenAny(activeTasks);
                }

                // CPU 과부하 방지
                if (currentItem == null && activeTasks.Count > 0)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }

            // 모든 활성 작업 완료 대기
            await Task.WhenAll(activeTasks);
        }
        finally
        {
            writer.TryComplete();
        }
    }

    /// <summary>
    /// 개별 크롤 아이템 처리
    /// </summary>
    /// <param name="item">크롤 아이템</param>
    /// <param name="writer">결과 채널 작성자</param>
    /// <param name="semaphore">동시성 제어 세마포어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessCrawlItemAsync(
        CrawlItem item,
        ChannelWriter<WebContent> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        if (!ShouldCrawlUrl(item.Url) || item.Depth > _configuration.MaxDepth)
            return;

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var content = await ProcessUrlAsync(item.Url, cancellationToken);

            if (content != null)
            {
                // 메타데이터에 깊이 정보 추가
                content.Metadata.AdditionalData["Depth"] = item.Depth;
                content.Metadata.AdditionalData["ParentUrl"] = item.ParentUrl ?? string.Empty;

                await writer.WriteAsync(content, cancellationToken);

                // 발견된 URL들을 스택에 추가 (깊이 우선을 위해 역순으로)
                await AddDiscoveredUrlsToStack(content.Content, item.Url, item.Depth);
            }

            // 요청 간 지연
            if (_configuration.DelayBetweenRequests > TimeSpan.Zero)
            {
                await Task.Delay(_configuration.DelayBetweenRequests, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await HandleFailedUrl(item.Url, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// 발견된 URL들을 스택에 추가
    /// </summary>
    /// <param name="content">HTML 콘텐츠</param>
    /// <param name="parentUrl">부모 URL</param>
    /// <param name="parentDepth">부모 깊이</param>
    private async Task AddDiscoveredUrlsToStack(string content, string parentUrl, int parentDepth)
    {
        if (parentDepth >= _configuration.MaxDepth)
            return;

        var discoveredUrls = await DiscoverUrlsAsync(content, parentUrl);
        var childDepth = parentDepth + 1;

        // 우선순위에 따라 정렬 (예: 특정 패턴 우선)
        var prioritizedUrls = PrioritizeUrls(discoveredUrls, parentUrl).ToList();

        lock (_stackLock)
        {
            // 역순으로 스택에 추가하여 우선순위 순서대로 처리되도록 함
            for (int i = prioritizedUrls.Count - 1; i >= 0; i--)
            {
                var url = prioritizedUrls[i];

                if (ShouldCrawlUrl(url))
                {
                    _urlStack.Push(new CrawlItem
                    {
                        Url = url,
                        Depth = childDepth,
                        ParentUrl = parentUrl
                    });
                }
            }
        }
    }

    /// <summary>
    /// URL 우선순위 결정
    /// </summary>
    /// <param name="urls">URL 목록</param>
    /// <param name="parentUrl">부모 URL</param>
    /// <returns>우선순위가 적용된 URL 목록</returns>
    private IEnumerable<string> PrioritizeUrls(IEnumerable<string> urls, string parentUrl)
    {
        var parentUri = new Uri(parentUrl);
        var urlList = urls.ToList();

        // 우선순위 규칙:
        // 1. 같은 도메인의 URL 우선
        // 2. 상위 디렉토리보다 하위 디렉토리 우선
        // 3. 짧은 경로 우선

        return urlList
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .OrderByDescending(url =>
            {
                var uri = new Uri(url);
                var priority = 0;

                // 같은 도메인 우선 (+100점)
                if (uri.Host.Equals(parentUri.Host, StringComparison.OrdinalIgnoreCase))
                    priority += 100;

                // 하위 경로 우선 (+50점)
                if (uri.AbsolutePath.StartsWith(parentUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                    priority += 50;

                // 경로 길이에 따른 점수 (짧을수록 우선)
                priority -= uri.AbsolutePath.Split('/').Length;

                return priority;
            });
    }

    /// <summary>
    /// 크롤링 상태 조회 (스택 정보 포함)
    /// </summary>
    /// <returns>크롤링 상태</returns>
    public override CrawlStatus GetStatus()
    {
        var baseStatus = base.GetStatus();

        lock (_stackLock)
        {
            baseStatus.QueuedCount = _urlStack.Count;
            baseStatus.AdditionalInfo["CrawlerType"] = "DepthFirst";
            baseStatus.AdditionalInfo["StackDepth"] = _urlStack.Count > 0 ? _urlStack.Max(item => item.Depth) : 0;
            baseStatus.AdditionalInfo["CurrentDepthDistribution"] = _urlStack
                .GroupBy(item => item.Depth)
                .ToDictionary(g => $"Depth{g.Key}", g => g.Count());
        }

        return baseStatus;
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public override void Dispose()
    {
        _concurrencySemaphore?.Dispose();
        lock (_stackLock)
        {
            _urlStack.Clear();
        }
        base.Dispose();
    }

    /// <summary>
    /// 크롤 아이템 클래스
    /// </summary>
    private class CrawlItem
    {
        public string Url { get; set; } = string.Empty;
        public int Depth { get; set; }
        public string? ParentUrl { get; set; }
    }
}