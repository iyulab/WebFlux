using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using System.Collections.Concurrent;

namespace WebFlux.Services;

/// <summary>
/// 웹 콘텐츠 처리 파이프라인 메인 클래스
/// 크롤링 → 추출 → 청킹 전체 프로세스 오케스트레이션
/// </summary>
public class WebContentProcessor : IWebContentProcessor
{
    private readonly IServiceFactory _serviceFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WebContentProcessor> _logger;
    private readonly SemaphoreSlim _processingSlot;

    public WebContentProcessor(
        IServiceFactory serviceFactory,
        IEventPublisher eventPublisher,
        ILogger<WebContentProcessor> logger)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processingSlot = new SemaphoreSlim(Environment.ProcessorCount);
    }

    /// <summary>
    /// 웹 콘텐츠 처리 실행
    /// </summary>
    /// <param name="configuration">처리 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 청크 스트림</returns>
    public async IAsyncEnumerable<WebContentChunk> ProcessAsync(
        WebFluxConfiguration configuration,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var processedCount = 0;

        _logger.LogInformation("Starting web content processing with {UrlCount} URLs",
            configuration.Crawling.StartUrls.Count);

        await _eventPublisher.PublishAsync(new ProcessingStartedEvent
        {
            Message = $"웹 콘텐츠 처리 시작 - {configuration.Crawling.StartUrls?.Count ?? 0}개 URL",
            Configuration = configuration,
            StartUrls = configuration.Crawling.StartUrls ?? new List<string>(),
            Timestamp = startTime
        }, cancellationToken);

        // 1단계: 크롤링 파이프라인
        var crawlingResults = CrawlWebContent(configuration, cancellationToken);

        // 2단계: 콘텐츠 추출 파이프라인
        var extractionResults = ExtractContent(crawlingResults, configuration, cancellationToken);

        // 3단계: 청킹 파이프라인
        await foreach (var chunk in ChunkContent(extractionResults, configuration, cancellationToken))
        {
            processedCount++;

            // 진행률 리포팅
            if (processedCount % 10 == 0)
            {
                try
                {
                    await _eventPublisher.PublishAsync(new ProcessingProgressEvent
                    {
                        Message = $"처리 진행중 - {processedCount}개 청크 완료",
                        ProcessedCount = processedCount,
                        ElapsedTime = DateTimeOffset.UtcNow - startTime,
                        EstimatedRemaining = TimeSpan.Zero, // 추정 로직 필요
                        CurrentStage = "Chunking",
                        Timestamp = DateTimeOffset.UtcNow
                    }, cancellationToken);
                }
                catch
                {
                    // 이벤트 발행 실패는 무시
                }
            }

            yield return chunk;
        }

        try
        {
            await _eventPublisher.PublishAsync(new ProcessingCompletedEvent
            {
                Message = $"웹 콘텐츠 처리 완료 - {processedCount}개 청크 생성",
                ProcessedChunkCount = processedCount,
                TotalProcessingTime = DateTimeOffset.UtcNow - startTime,
                AverageProcessingRate = processedCount / Math.Max(1, (DateTimeOffset.UtcNow - startTime).TotalMinutes),
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Web content processing completed. Processed {ChunkCount} chunks in {Duration}",
                processedCount, DateTimeOffset.UtcNow - startTime);
        }
        catch
        {
            // 완료 이벤트 발행 실패는 무시
        }
    }

    /// <summary>
    /// 웹 콘텐츠 크롤링 파이프라인
    /// </summary>
    /// <param name="configuration">구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 웹 콘텐츠 스트림</returns>
    private async IAsyncEnumerable<WebContent> CrawlWebContent(
        WebFluxConfiguration configuration,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 문자열을 CrawlStrategy enum으로 변환
        var crawlStrategy = Enum.TryParse<WebFlux.Core.Options.CrawlStrategy>(configuration.Crawling.Strategy, true, out var strategy)
            ? strategy
            : WebFlux.Core.Options.CrawlStrategy.BreadthFirst;

        var crawler = _serviceFactory.CreateCrawler(crawlStrategy);

        // CrawlingConfiguration을 CrawlOptions로 변환
        var crawlOptions = new CrawlOptions
        {
            MaxDepth = 3, // 기본값
            MaxPages = 100, // 기본값
            DelayMs = configuration.Crawling.DefaultDelayMs
        };

        // 각 시작 URL에 대해 크롤링 수행
        var startUrls = configuration.Crawling.StartUrls ?? new List<string>();
        foreach (var url in startUrls)
        {
            // 웹사이트 크롤링 사용 (여러 페이지)
            await foreach (var crawlResult in crawler.CrawlWebsiteAsync(url, crawlOptions, cancellationToken))
            {
                // CrawlResult를 WebContent로 변환
                var webContent = new WebContent
                {
                    Url = crawlResult.Url,
                    Content = crawlResult.Content ?? string.Empty,
                    ContentType = crawlResult.ContentType ?? "text/html",
                    StatusCode = crawlResult.StatusCode,
                    Metadata = new WebContentMetadata
                    {
                        Title = ExtractTitle(crawlResult.Content),
                        ContentType = crawlResult.ContentType ?? "text/html",
                        ContentLength = crawlResult.Content?.Length ?? 0,
                        Language = "ko"
                    }
                };

                yield return webContent;
            }
        }
    }

    /// <summary>
    /// 콘텐츠 추출 파이프라인
    /// </summary>
    /// <param name="webContents">웹 콘텐츠 스트림</param>
    /// <param name="configuration">구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠 스트림</returns>
    private async IAsyncEnumerable<ExtractedContent> ExtractContent(
        IAsyncEnumerable<WebContent> webContents,
        WebFluxConfiguration configuration,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 병렬 처리를 위한 채널 설정
        var channel = Channel.CreateUnbounded<ExtractedContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 백그라운드에서 추출 작업 실행
        var extractionTask = Task.Run(async () =>
        {
            try
            {
                var tasks = new List<Task>();
                var semaphore = new SemaphoreSlim(configuration.Performance.MaxDegreeOfParallelism);

                await foreach (var webContent in webContents.WithCancellation(cancellationToken))
                {
                    var task = ProcessSingleContent(webContent, configuration, writer, semaphore, cancellationToken);
                    tasks.Add(task);

                    // 완료된 작업 정리
                    tasks.RemoveAll(t => t.IsCompleted);
                }

                // 모든 작업 완료 대기
                await Task.WhenAll(tasks);
                writer.Complete();
            }
            catch (Exception ex)
            {
                writer.Complete(ex);
            }
        }, cancellationToken);

        // 결과를 스트리밍으로 반환
        await foreach (var extracted in reader.ReadAllAsync(cancellationToken))
        {
            yield return extracted;
        }

        await extractionTask;
    }

    /// <summary>
    /// 개별 웹 콘텐츠 처리
    /// </summary>
    /// <param name="webContent">웹 콘텐츠</param>
    /// <param name="configuration">구성</param>
    /// <param name="writer">채널 라이터</param>
    /// <param name="semaphore">동시성 제어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessSingleContent(
        WebContent webContent,
        WebFluxConfiguration configuration,
        ChannelWriter<ExtractedContent> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var extractor = _serviceFactory.CreateContentExtractor(webContent.ContentType);
            var extracted = await extractor.ExtractAutoAsync(
                webContent.Content,
                webContent.Url,
                webContent.ContentType,
                cancellationToken);

            await writer.WriteAsync(extracted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract content from {Url}", webContent.Url);
            // 에러가 발생해도 파이프라인 계속 진행
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// 콘텐츠 청킹 파이프라인
    /// </summary>
    /// <param name="extractedContents">추출된 콘텐츠 스트림</param>
    /// <param name="configuration">구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>청킹된 콘텐츠 스트림</returns>
    private async IAsyncEnumerable<WebContentChunk> ChunkContent(
        IAsyncEnumerable<ExtractedContent> extractedContents,
        WebFluxConfiguration configuration,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var extracted in extractedContents.WithCancellation(cancellationToken))
        {
            var chunkingStrategy = _serviceFactory.CreateChunkingStrategy(configuration.Chunking.DefaultStrategy);

            // ChunkingConfiguration을 ChunkingOptions로 변환
            var chunkingOptions = new ChunkingOptions
            {
                MaxChunkSize = configuration.Chunking.MaxChunkSize,
                ChunkOverlap = 50, // 기본값
                PreserveHeaders = true // 기본값
            };

            var chunks = await chunkingStrategy.ChunkAsync(
                extracted,
                chunkingOptions,
                cancellationToken);

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _processingSlot?.Dispose();
    }

    private string ExtractTitle(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return "Untitled";

        var titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"<title[^>]*>([^<]+)</title>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Untitled";
    }

    // IWebContentProcessor interface implementations (stub implementations for build completion)

    public async Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
        string url,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation
        await Task.CompletedTask;
        return new List<WebContentChunk>().AsReadOnly();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
        IEnumerable<string> urls,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation
        await Task.CompletedTask;
        return new Dictionary<string, IReadOnlyList<WebContentChunk>>().AsReadOnly();
    }

    public async IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stub implementation
        await Task.CompletedTask;
        yield break;
    }

    public async Task<IReadOnlyList<WebContentChunk>> ProcessHtmlAsync(
        string htmlContent,
        string sourceUrl,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation
        await Task.CompletedTask;
        return new List<WebContentChunk>().AsReadOnly();
    }

    public async IAsyncEnumerable<ProcessingProgress> MonitorProgressAsync(string jobId)
    {
        // Stub implementation
        await Task.CompletedTask;
        yield break;
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        // Stub implementation
        await Task.CompletedTask;
        return true;
    }

    public async Task<ProcessingStatistics> GetStatisticsAsync()
    {
        // Stub implementation
        await Task.CompletedTask;
        return new ProcessingStatistics();
    }

    public IReadOnlyList<string> GetAvailableChunkingStrategies()
    {
        // Stub implementation
        return new List<string> { "FixedSize", "Paragraph", "Smart", "Semantic", "Auto", "MemoryOptimized" }.AsReadOnly();
    }
}

/// <summary>
/// 처리 시작 이벤트
/// </summary>
public class ProcessingStartedEvent : ProcessingEvent
{
    public override string EventType => "ProcessingStarted";
    public WebFluxConfiguration Configuration { get; set; } = new();
    public List<string> StartUrls { get; set; } = new();
}

/// <summary>
/// 처리 진행률 이벤트
/// </summary>
public class ProcessingProgressEvent : ProcessingEvent
{
    public override string EventType => "ProcessingProgress";
    public int ProcessedCount { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan EstimatedRemaining { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
}

/// <summary>
/// 처리 완료 이벤트
/// </summary>
public class ProcessingCompletedEvent : ProcessingEvent
{
    public override string EventType => "ProcessingCompleted";
    public int ProcessedChunkCount { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public double AverageProcessingRate { get; set; }
}

/// <summary>
/// 처리 실패 이벤트
/// </summary>
public class ProcessingFailedEvent : ProcessingEvent
{
    public override string EventType => "ProcessingFailed";
    public string Error { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
}