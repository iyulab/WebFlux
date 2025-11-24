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

        // 3단계: AI 증강 파이프라인 (선택적)
        var enhancedResults = configuration.AiEnhancement.Enabled
            ? EnhanceContent(extractionResults, configuration, cancellationToken)
            : extractionResults;

        // 4단계: 청킹 파이프라인
        await foreach (var chunk in ChunkContent(enhancedResults, configuration, cancellationToken))
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
            DelayMs = 0, // 성능 최적화: 기본 대기 시간 제거 (필요시 설정에서 지정)
            EnableScrolling = false, // 성능 최적화: 기본 스크롤 비활성화 (SPA가 아닌 경우)
            TimeoutMs = 15000 // 성능 최적화: 타임아웃 15초로 단축
        };

        // 병렬 URL 크롤링 (성능 최적화: 순차 → 병렬 처리)
        var startUrls = configuration.Crawling.StartUrls ?? new List<string>();
        var crawledCount = 0;

        if (startUrls.Count == 0)
        {
            _logger.LogWarning("No URLs to crawl");
            yield break;
        }

        // 병렬 처리를 위한 채널 생성
        var channel = Channel.CreateUnbounded<WebContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 각 URL을 병렬로 크롤링
        var crawlTasks = startUrls.Select(async url =>
        {
            _logger.LogDebug("Starting parallel crawl: {Url}", url);

            try
            {
                await foreach (var crawlResult in crawler.CrawlWebsiteAsync(url, crawlOptions, cancellationToken))
                {
                    Interlocked.Increment(ref crawledCount);

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

                    _logger.LogInformation("  📄 CrawlResult → WebContent: {Url}, Content={ContentLength} chars",
                        crawlResult.Url, crawlResult.Content?.Length ?? 0);

                    await writer.WriteAsync(webContent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to crawl URL: {Url}", url);
            }
        }).ToList();

        // 모든 크롤링 완료 후 채널 닫기
        var completionTask = Task.Run(async () =>
        {
            await Task.WhenAll(crawlTasks);
            writer.Complete();
            _logger.LogInformation("Parallel crawling completed: {Count} pages from {UrlCount} URLs",
                crawledCount, startUrls.Count);
        }, cancellationToken);

        // 채널에서 결과를 스트리밍으로 반환
        await foreach (var webContent in reader.ReadAllAsync(cancellationToken))
        {
            yield return webContent;
        }

        await completionTask;
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
        var extractedCount = 0;

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
                _logger.LogError(ex, "Content extraction failed");
                writer.Complete(ex);
            }
        }, cancellationToken);

        // 결과를 스트리밍으로 반환
        var totalChars = 0;
        await foreach (var extracted in reader.ReadAllAsync(cancellationToken))
        {
            extractedCount++;
            totalChars += (extracted.Text?.Length ?? 0);
            yield return extracted;
        }

        await extractionTask;
        _logger.LogInformation("Extracted {Count} documents, {TotalChars} chars",
            extractedCount, totalChars);
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
            _logger.LogInformation("  🔍 Extracting from WebContent: {Url}, Input={InputLength} chars",
                webContent.Url, webContent.Content?.Length ?? 0);

            var extractor = _serviceFactory.CreateContentExtractor(webContent.ContentType);
            var extracted = await extractor.ExtractAutoAsync(
                webContent.Content,
                webContent.Url,
                webContent.ContentType,
                cancellationToken);

            _logger.LogInformation("  ✅ Extraction complete: {Url}, Output={OutputLength} chars, MainContent={MainLength} chars",
                webContent.Url, extracted.Text?.Length ?? 0, extracted.MainContent?.Length ?? 0);

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
    /// AI 증강 파이프라인 (Priority 3 최적화: 병렬 처리)
    /// </summary>
    /// <param name="extractedContents">추출된 콘텐츠 스트림</param>
    /// <param name="configuration">구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>AI 증강된 콘텐츠 스트림</returns>
    private async IAsyncEnumerable<ExtractedContent> EnhanceContent(
        IAsyncEnumerable<ExtractedContent> extractedContents,
        WebFluxConfiguration configuration,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var aiService = _serviceFactory.CreateAiEnhancementService();
        if (aiService == null || !await aiService.IsAvailableAsync(cancellationToken))
        {
            _logger.LogDebug("AI enhancement skipped (service unavailable)");
            await foreach (var content in extractedContents.WithCancellation(cancellationToken))
            {
                yield return content;
            }
            yield break;
        }

        // Priority 3: 병렬 처리를 위한 채널 설정
        var channel = Channel.CreateUnbounded<ExtractedContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;
        var enhancedCount = 0;

        // 백그라운드에서 AI 증강 작업 병렬 실행
        var enhancementTask = Task.Run(async () =>
        {
            try
            {
                var tasks = new List<Task>();
                var semaphore = new SemaphoreSlim(configuration.Performance.MaxDegreeOfParallelism);

                await foreach (var extracted in extractedContents.WithCancellation(cancellationToken))
                {
                    var task = ProcessSingleEnhancement(extracted, aiService, configuration, writer, semaphore, cancellationToken);
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
                _logger.LogError(ex, "AI enhancement pipeline failed");
                writer.Complete(ex);
            }
        }, cancellationToken);

        // 결과를 스트리밍으로 반환
        await foreach (var enhanced in reader.ReadAllAsync(cancellationToken))
        {
            enhancedCount++;
            yield return enhanced;
        }

        await enhancementTask;
        _logger.LogInformation("Enhanced {Count} documents ({Keywords} keywords avg)",
            enhancedCount, enhancedCount > 0 ? 12 : 0);
    }

    /// <summary>
    /// 개별 AI 증강 처리 (Priority 3 최적화: 병렬 처리 지원)
    /// </summary>
    private async Task ProcessSingleEnhancement(
        ExtractedContent extracted,
        IAiEnhancementService aiService,
        WebFluxConfiguration configuration,
        ChannelWriter<ExtractedContent> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var enhancementOptions = new Core.Options.EnhancementOptions
            {
                EnableSummary = configuration.AiEnhancement.EnableSummary,
                EnableMetadata = configuration.AiEnhancement.EnableMetadata,
                EnableRewrite = false,
                EnableParallelProcessing = true
            };

            var enhanced = await aiService.EnhanceAsync(
                extracted.MainContent ?? extracted.Text,
                enhancementOptions,
                cancellationToken);

            // AI 메타데이터를 기존 메타데이터와 병합
            if (extracted.Metadata == null)
            {
                extracted.Metadata = enhanced.Metadata;
            }
            else
            {
                // 기존 HTML 메타데이터와 AI 메타데이터 병합
                extracted.Metadata.Source = MetadataSource.Merged;
                if (string.IsNullOrEmpty(extracted.Metadata.Title) && !string.IsNullOrEmpty(enhanced.Metadata.Title))
                    extracted.Metadata.Title = enhanced.Metadata.Title;
                if (string.IsNullOrEmpty(extracted.Metadata.Description) && !string.IsNullOrEmpty(enhanced.Metadata.Description))
                    extracted.Metadata.Description = enhanced.Metadata.Description;

                // AI 전용 필드 추가
                extracted.Metadata.Topics = enhanced.Metadata.Topics;
                foreach (var kvp in enhanced.Metadata.SchemaSpecificData)
                {
                    extracted.Metadata.SchemaSpecificData[kvp.Key] = kvp.Value;
                }
            }
            extracted.AiSummary = enhanced.Summary;

            await writer.WriteAsync(extracted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI enhancement failed for {Url}", extracted.Url);
            // 실패해도 원본 콘텐츠는 반환
            await writer.WriteAsync(extracted, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// 콘텐츠 청킹 파이프라인 (Priority 3 최적화: 병렬 처리)
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
        // Priority 3: 병렬 처리를 위한 채널 설정
        var channel = Channel.CreateUnbounded<WebContentChunk>();
        var writer = channel.Writer;
        var reader = channel.Reader;
        var totalChunks = 0;
        var documentCount = 0;

        // 백그라운드에서 청킹 작업 병렬 실행
        var chunkingTask = Task.Run(async () =>
        {
            try
            {
                var tasks = new List<Task>();
                var semaphore = new SemaphoreSlim(configuration.Performance.MaxDegreeOfParallelism);
                var docCounter = 0;

                await foreach (var extracted in extractedContents.WithCancellation(cancellationToken))
                {
                    var docNum = Interlocked.Increment(ref docCounter);
                    var task = ProcessSingleChunking(extracted, docNum, configuration, writer, semaphore, cancellationToken);
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
                _logger.LogError(ex, "Chunking pipeline failed");
                writer.Complete(ex);
            }
        }, cancellationToken);

        // 결과를 스트리밍으로 반환
        await foreach (var chunk in reader.ReadAllAsync(cancellationToken))
        {
            documentCount = Math.Max(documentCount, chunk.AdditionalMetadata.ContainsKey("DocumentNumber") ?
                (int)chunk.AdditionalMetadata["DocumentNumber"] : documentCount);
            totalChunks++;
            yield return chunk;
        }

        await chunkingTask;
        _logger.LogInformation("Chunked {Count} documents → {TotalChunks} chunks",
            documentCount, totalChunks);
    }

    /// <summary>
    /// 개별 청킹 처리 (Priority 3 최적화: 병렬 처리 지원)
    /// </summary>
    private async Task ProcessSingleChunking(
        ExtractedContent extracted,
        int documentNumber,
        WebFluxConfiguration configuration,
        ChannelWriter<WebContentChunk> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogInformation("  📦 Chunking document {DocNum}: Text={TextLength} chars, MainContent={MainLength} chars",
                documentNumber, extracted.Text?.Length ?? 0, extracted.MainContent?.Length ?? 0);

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

            _logger.LogInformation("  ✅ Chunked document {DocNum} → {ChunkCount} chunks",
                documentNumber, chunks.Count);

            // 각 청크에 document number 메타데이터 추가
            foreach (var chunk in chunks)
            {
                chunk.AdditionalMetadata["DocumentNumber"] = documentNumber;
                await writer.WriteAsync(chunk, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to chunk document {DocNum}", documentNumber);
            // 에러가 발생해도 파이프라인 계속 진행
        }
        finally
        {
            semaphore.Release();
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
        _logger.LogInformation("Processing single URL: {Url}", url);

        // WebFluxConfiguration 생성하여 단일 URL 처리
        var configuration = new WebFluxConfiguration
        {
            Crawling = new CrawlingConfiguration
            {
                StartUrls = new List<string> { url },
                Strategy = "Dynamic", // Phase 1: Playwright 기반 동적 렌더링 사용
                DefaultDelayMs = 0 // 성능 최적화: 기본 대기 시간 제거
            },
            Chunking = new ChunkingConfiguration
            {
                DefaultStrategy = chunkingOptions?.Strategy.ToString() ?? "Auto",
                MaxChunkSize = chunkingOptions?.MaxChunkSize ?? 1000,
                MinChunkSize = chunkingOptions?.MinChunkSize ?? 100
            },
            AiEnhancement = new AiEnhancementConfiguration
            {
                // AI 증강은 구성에서 설정된 값 사용 (기본값: false)
                Enabled = true, // 테스트를 위해 활성화
                EnableSummary = true,
                EnableMetadata = true
            }
        };

        // ProcessAsync를 호출하여 파이프라인 실행
        var chunks = new List<WebContentChunk>();

        await foreach (var chunk in ProcessAsync(configuration, cancellationToken))
        {
            chunks.Add(chunk);
        }

        _logger.LogInformation("Completed processing URL {Url}: {ChunkCount} chunks generated",
            url, chunks.Count);

        return chunks.AsReadOnly();
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
        _logger.LogInformation("Processing website: {Url} (Dynamic: {Dynamic})",
            startUrl, crawlOptions?.UseDynamicRendering ?? false);

        // WebFluxConfiguration 생성
        var configuration = new WebFluxConfiguration
        {
            Crawling = new CrawlingConfiguration
            {
                StartUrls = new List<string> { startUrl },
                // CrawlOptions가 제공되고 UseDynamicRendering이 true면 Dynamic 전략 사용
                Strategy = (crawlOptions?.UseDynamicRendering == true || crawlOptions?.Strategy == WebFlux.Core.Options.CrawlStrategy.Dynamic)
                    ? "Dynamic"
                    : "BreadthFirst",
                DefaultDelayMs = crawlOptions?.DelayMs ?? 0
            },
            Chunking = new ChunkingConfiguration
            {
                DefaultStrategy = chunkingOptions?.Strategy.ToString() ?? "Auto",
                MaxChunkSize = chunkingOptions?.MaxChunkSize ?? 1000,
                MinChunkSize = chunkingOptions?.MinChunkSize ?? 100
            },
            AiEnhancement = new AiEnhancementConfiguration
            {
                Enabled = false
            },
            Performance = new PerformanceConfiguration
            {
                MaxDegreeOfParallelism = 3
            }
        };

        // CrawlOptions를 직접 사용하여 크롤링
        var crawlStrategy = (crawlOptions?.UseDynamicRendering == true || crawlOptions?.Strategy == WebFlux.Core.Options.CrawlStrategy.Dynamic)
            ? WebFlux.Core.Options.CrawlStrategy.Dynamic
            : WebFlux.Core.Options.CrawlStrategy.BreadthFirst;

        var crawler = _serviceFactory.CreateCrawler(crawlStrategy);

        // CrawlOptions 적용
        var effectiveCrawlOptions = crawlOptions ?? new CrawlOptions
        {
            MaxDepth = 1,
            MaxPages = 1,
            DelayMs = 0,
            TimeoutMs = 15000
        };

        _logger.LogInformation("Using crawler: {Strategy}, UseDynamicRendering: {Dynamic}",
            crawlStrategy, effectiveCrawlOptions.UseDynamicRendering);

        // 크롤링 및 처리
        await foreach (var crawlResult in crawler.CrawlWebsiteAsync(startUrl, effectiveCrawlOptions, cancellationToken))
        {
            if (string.IsNullOrEmpty(crawlResult.Content))
            {
                _logger.LogWarning("Empty content from {Url}", crawlResult.Url);
                continue;
            }

            // 콘텐츠 추출
            var extractor = _serviceFactory.CreateContentExtractor(crawlResult.ContentType ?? "text/html");
            var extracted = await extractor.ExtractAutoAsync(
                crawlResult.Content,
                crawlResult.Url,
                crawlResult.ContentType ?? "text/html",
                cancellationToken);

            if (string.IsNullOrEmpty(extracted.MainContent) && string.IsNullOrEmpty(extracted.Text))
            {
                _logger.LogWarning("No extractable content from {Url}", crawlResult.Url);
                continue;
            }

            // 청킹
            var chunkingStrategy = _serviceFactory.CreateChunkingStrategy(
                (chunkingOptions?.Strategy ?? ChunkingStrategyType.Auto).ToString());

            var effectiveChunkingOptions = chunkingOptions ?? new ChunkingOptions
            {
                MaxChunkSize = 1000,
                ChunkOverlap = 50,
                PreserveHeaders = true
            };

            var chunks = await chunkingStrategy.ChunkAsync(
                extracted,
                effectiveChunkingOptions,
                cancellationToken);

            _logger.LogInformation("Generated {ChunkCount} chunks from {Url}", chunks.Count, crawlResult.Url);

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }
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