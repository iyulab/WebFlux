using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
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

        try
        {
            _logger.LogInformation("Starting web content processing with {UrlCount} URLs",
                configuration.Crawling.StartUrls.Count);

            await _eventPublisher.PublishAsync(new ProcessingStartedEvent
            {
                Configuration = configuration,
                StartUrls = configuration.Crawling.StartUrls,
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
                    await _eventPublisher.PublishAsync(new ProcessingProgressEvent
                    {
                        ProcessedCount = processedCount,
                        ElapsedTime = DateTimeOffset.UtcNow - startTime,
                        EstimatedRemaining = TimeSpan.Zero, // 추정 로직 필요
                        CurrentStage = "Chunking",
                        Timestamp = DateTimeOffset.UtcNow
                    }, cancellationToken);
                }

                yield return chunk;
            }

            await _eventPublisher.PublishAsync(new ProcessingCompletedEvent
            {
                ProcessedChunkCount = processedCount,
                TotalProcessingTime = DateTimeOffset.UtcNow - startTime,
                AverageProcessingRate = processedCount / Math.Max(1, (DateTimeOffset.UtcNow - startTime).TotalMinutes),
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Web content processing completed. Processed {ChunkCount} chunks in {Duration}",
                processedCount, DateTimeOffset.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during web content processing");

            await _eventPublisher.PublishAsync(new ProcessingFailedEvent
            {
                Error = ex.Message,
                ProcessedCount = processedCount,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            throw;
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
        var crawler = _serviceFactory.CreateCrawler(configuration.Crawling.Strategy);
        var crawlResults = await crawler.CrawlAsync(
            configuration.Crawling.StartUrls,
            configuration.Crawling,
            cancellationToken);

        await foreach (var content in crawlResults.WithCancellation(cancellationToken))
        {
            yield return content;
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
            var extracted = await extractor.ExtractAsync(
                webContent,
                configuration.Extraction,
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
            var chunks = await chunkingStrategy.ChunkAsync(
                extracted,
                configuration.Chunking,
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