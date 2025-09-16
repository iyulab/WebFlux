using System.Collections.Concurrent;
using System.Threading.Channels;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 너비 우선 크롤러
/// 같은 깊이의 페이지를 먼저 모두 처리한 후 다음 깊이로 진행
/// </summary>
public class BreadthFirstCrawler : BaseCrawler
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly Dictionary<string, int> _urlDepths = new();

    public BreadthFirstCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher)
    {
        _concurrencySemaphore = new SemaphoreSlim(5); // 기본 동시 요청 수
    }

    /// <summary>
    /// 내부 크롤링 로직 - 너비 우선 방식
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 콘텐츠 스트림</returns>
    protected override async IAsyncEnumerable<WebContent> CrawlInternalAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 시작 URL들의 깊이를 0으로 설정
        var startUrls = new List<string>();
        while (_urlQueue.TryDequeue(out var startUrl))
        {
            startUrls.Add(startUrl);
            _urlDepths[startUrl] = 0;
        }

        // 시작 URL들을 다시 큐에 추가
        foreach (var url in startUrls)
        {
            _urlQueue.Enqueue(url);
        }

        // 동시성 제한 설정
        _concurrencySemaphore.Release(_concurrencySemaphore.CurrentCount);
        var maxConcurrency = Math.Min(_configuration.MaxConcurrentRequests, 10);
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency);

        // 처리 채널 생성
        var channel = Channel.CreateUnbounded<WebContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 백그라운드 처리 태스크 시작
        var processingTask = ProcessUrlsConcurrentlyAsync(writer, cancellationToken);

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
                Duration = TimeSpan.Zero, // 실제 구현에서는 시작 시간 기록 필요
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    /// <summary>
    /// URL들을 동시에 처리
    /// </summary>
    /// <param name="writer">결과 채널 작성자</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessUrlsConcurrentlyAsync(
        ChannelWriter<WebContent> writer,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var currentDepth = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 현재 깊이의 모든 URL들 수집
                var currentDepthUrls = GetUrlsAtDepth(currentDepth);

                if (!currentDepthUrls.Any())
                {
                    // 더 이상 처리할 URL이 없으면 완료
                    if (_urlQueue.IsEmpty)
                        break;

                    // 다음 깊이로 이동
                    currentDepth++;
                    if (currentDepth > _configuration.MaxDepth)
                        break;

                    continue;
                }

                // 현재 깊이의 URL들을 동시 처리
                foreach (var url in currentDepthUrls)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var task = ProcessSingleUrlAsync(url, writer, cancellationToken);
                    tasks.Add(task);
                }

                // 현재 깊이의 모든 작업 완료 대기
                await Task.WhenAll(tasks);
                tasks.Clear();

                // 다음 깊이로 이동
                currentDepth++;
                if (currentDepth > _configuration.MaxDepth)
                    break;

                // 잠시 대기 (과부하 방지)
                await Task.Delay(_configuration.DelayBetweenRequests, cancellationToken);
            }
        }
        finally
        {
            writer.TryComplete();
            _concurrencySemaphore.Dispose();
        }
    }

    /// <summary>
    /// 특정 깊이의 URL들 반환
    /// </summary>
    /// <param name="depth">깊이</param>
    /// <returns>해당 깊이의 URL 목록</returns>
    private List<string> GetUrlsAtDepth(int depth)
    {
        var urls = new List<string>();
        var tempQueue = new Queue<string>();

        // 큐에서 현재 깊이에 해당하는 URL들만 추출
        while (_urlQueue.TryDequeue(out var url))
        {
            if (_urlDepths.TryGetValue(url, out var urlDepth) && urlDepth == depth)
            {
                if (ShouldCrawlUrl(url))
                {
                    urls.Add(url);
                }
            }
            else
            {
                tempQueue.Enqueue(url); // 다른 깊이는 다시 큐에 추가
            }
        }

        // 다른 깊이의 URL들을 큐에 다시 추가
        while (tempQueue.Count > 0)
        {
            _urlQueue.Enqueue(tempQueue.Dequeue());
        }

        return urls;
    }

    /// <summary>
    /// 단일 URL 처리 (동시성 제어 포함)
    /// </summary>
    /// <param name="url">처리할 URL</param>
    /// <param name="writer">결과 채널 작성자</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessSingleUrlAsync(
        string url,
        ChannelWriter<WebContent> writer,
        CancellationToken cancellationToken)
    {
        await _concurrencySemaphore.WaitAsync(cancellationToken);

        try
        {
            var content = await ProcessUrlAsync(url, cancellationToken);

            if (content != null)
            {
                await writer.WriteAsync(content, cancellationToken);

                // 발견된 URL들의 깊이 설정
                await SetDiscoveredUrlDepths(content.Content, url);
            }
        }
        catch (Exception ex)
        {
            await HandleFailedUrl(url, ex.Message);
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }

    /// <summary>
    /// 발견된 URL들의 깊이 설정
    /// </summary>
    /// <param name="content">HTML 콘텐츠</param>
    /// <param name="parentUrl">부모 URL</param>
    private async Task SetDiscoveredUrlDepths(string content, string parentUrl)
    {
        var parentDepth = _urlDepths.TryGetValue(parentUrl, out var depth) ? depth : 0;
        var childDepth = parentDepth + 1;

        if (childDepth > _configuration.MaxDepth)
            return;

        var discoveredUrls = await DiscoverUrlsAsync(content, parentUrl);

        foreach (var discoveredUrl in discoveredUrls)
        {
            lock (_lockObject)
            {
                if (!_urlDepths.ContainsKey(discoveredUrl))
                {
                    _urlDepths[discoveredUrl] = childDepth;
                }
            }
        }
    }

    /// <summary>
    /// 크롤링 구성에 따른 동시성 제한 조정
    /// </summary>
    /// <param name="startUrls">시작 URL 목록</param>
    /// <param name="configuration">크롤링 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 웹 콘텐츠</returns>
    public override async Task<IAsyncEnumerable<WebContent>> CrawlAsync(
        IEnumerable<string> startUrls,
        CrawlConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // 동시성 제한 재설정
        _concurrencySemaphore.Dispose();
        var maxConcurrency = Math.Min(configuration.MaxConcurrentRequests, 10);

        return await base.CrawlAsync(startUrls, configuration, cancellationToken);
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public override void Dispose()
    {
        _concurrencySemaphore?.Dispose();
        base.Dispose();
    }
}