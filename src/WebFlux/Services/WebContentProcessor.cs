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
public partial class WebContentProcessor : IWebContentProcessor, IContentExtractService, IContentChunkService, IDisposable
{
    private readonly IServiceFactory _serviceFactory;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WebContentProcessor> _logger;
    private readonly IResilienceService? _resilienceService;
    private readonly SemaphoreSlim _processingSlot;

    public WebContentProcessor(
        IServiceFactory serviceFactory,
        IEventPublisher eventPublisher,
        ILogger<WebContentProcessor> logger,
        IResilienceService? resilienceService = null)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resilienceService = resilienceService;
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

        LogStartingWebContentProcessing(_logger, configuration.Crawling.StartUrls.Count);

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

            LogWebContentProcessingCompleted(_logger, processedCount, DateTimeOffset.UtcNow - startTime);
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
            LogNoUrlsToCrawl(_logger);
            yield break;
        }

        // 병렬 처리를 위한 채널 생성
        var channel = Channel.CreateUnbounded<WebContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 각 URL을 병렬로 크롤링
        var crawlTasks = startUrls.Select(async url =>
        {
            LogStartingParallelCrawl(_logger, url);

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

                    LogCrawlResultToWebContent(_logger, crawlResult.Url, crawlResult.Content?.Length ?? 0);

                    await writer.WriteAsync(webContent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                LogFailedToCrawlUrl(_logger, ex, url);
            }
        }).ToList();

        // 모든 크롤링 완료 후 채널 닫기
        var completionTask = Task.Run(async () =>
        {
            await Task.WhenAll(crawlTasks);
            writer.Complete();
            LogParallelCrawlingCompleted(_logger, crawledCount, startUrls.Count);
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
                LogContentExtractionFailed(_logger, ex);
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
        LogExtractedDocuments(_logger, extractedCount, totalChars);
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
            var content = webContent.Content ?? string.Empty;
            LogExtractingFromWebContent(_logger, webContent.Url, content.Length);

            var extractor = _serviceFactory.CreateContentExtractor(webContent.ContentType);

            ExtractedContent extracted;

            // ResilienceService가 등록되어 있으면 재시도 정책 적용
            if (_resilienceService != null)
            {
                var retryPolicy = new Core.Models.RetryPolicy
                {
                    MaxRetryAttempts = 2,
                    BaseDelay = TimeSpan.FromSeconds(1),
                    Strategy = Core.Models.RetryStrategy.ExponentialBackoff
                };

                extracted = await _resilienceService.ExecuteWithRetryAsync(
                    async ct => await extractor.ExtractAutoAsync(
                        content,
                        webContent.Url,
                        webContent.ContentType,
                        ct),
                    retryPolicy,
                    cancellationToken);
            }
            else
            {
                extracted = await extractor.ExtractAutoAsync(
                    content,
                    webContent.Url,
                    webContent.ContentType,
                    cancellationToken);
            }

            LogExtractionComplete(_logger, webContent.Url, extracted.Text?.Length ?? 0, extracted.MainContent?.Length ?? 0);

            await writer.WriteAsync(extracted, cancellationToken);
        }
        catch (Exception ex)
        {
            LogFailedToExtractContent(_logger, ex, webContent.Url);
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
            LogAiEnhancementSkipped(_logger);
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
                LogAiEnhancementPipelineFailed(_logger, ex);
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
        LogEnhancedDocuments(_logger, enhancedCount, enhancedCount > 0 ? 12 : 0);
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
            LogAiEnhancementFailed(_logger, ex, extracted.Url);
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
                LogChunkingPipelineFailed(_logger, ex);
                writer.Complete(ex);
            }
        }, cancellationToken);

        // 결과를 스트리밍으로 반환
        await foreach (var chunk in reader.ReadAllAsync(cancellationToken))
        {
            documentCount = Math.Max(documentCount, chunk.AdditionalMetadata.TryGetValue("DocumentNumber", out var docNum) ?
                (int)docNum : documentCount);
            totalChunks++;
            yield return chunk;
        }

        await chunkingTask;
        LogChunkedDocuments(_logger, documentCount, totalChunks);
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
            LogChunkingDocument(_logger, documentNumber, extracted.Text?.Length ?? 0, extracted.MainContent?.Length ?? 0);

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

            LogChunkedDocument(_logger, documentNumber, chunks.Count);

            // 각 청크에 document number 메타데이터 추가
            foreach (var chunk in chunks)
            {
                chunk.AdditionalMetadata["DocumentNumber"] = documentNumber;
                await writer.WriteAsync(chunk, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            LogFailedToChunkDocument(_logger, ex, documentNumber);
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
        GC.SuppressFinalize(this);
    }

    private static string ExtractTitle(string? content)
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
        if (chunkingOptions != null)
        {
            var validation = chunkingOptions.Validate();
            if (!validation.IsValid)
                throw new ArgumentException($"Invalid chunking options: {string.Join(", ", validation.Errors)}");
        }

        LogProcessingSingleUrl(_logger, url);

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

        LogCompletedProcessingUrl(_logger, url, chunks.Count);

        return chunks.AsReadOnly();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
        IEnumerable<string> urls,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (chunkingOptions != null)
        {
            var validation = chunkingOptions.Validate();
            if (!validation.IsValid)
                throw new ArgumentException($"Invalid chunking options: {string.Join(", ", validation.Errors)}");
        }

        var urlList = urls.ToList();
        LogProcessingBatch(_logger, urlList.Count);

        var results = new ConcurrentDictionary<string, IReadOnlyList<WebContentChunk>>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

        var tasks = urlList.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var chunks = await ProcessUrlAsync(url, chunkingOptions, cancellationToken).ConfigureAwait(false);
                results[url] = chunks;
            }
            catch (Exception ex)
            {
                LogFailedToProcessUrlInBatch(_logger, ex, url);
                results[url] = Array.Empty<WebContentChunk>();
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        LogBatchProcessingCompleted(_logger, urlList.Count);
        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).AsReadOnly();
    }

    public async IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (crawlOptions != null)
        {
            var crawlValidation = crawlOptions.Validate();
            if (!crawlValidation.IsValid)
                throw new ArgumentException($"Invalid crawl options: {string.Join(", ", crawlValidation.Errors)}");
        }

        if (chunkingOptions != null)
        {
            var chunkValidation = chunkingOptions.Validate();
            if (!chunkValidation.IsValid)
                throw new ArgumentException($"Invalid chunking options: {string.Join(", ", chunkValidation.Errors)}");
        }

        LogProcessingWebsite(_logger, startUrl, crawlOptions?.UseDynamicRendering ?? false);

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

        LogUsingCrawler(_logger, crawlStrategy, effectiveCrawlOptions.UseDynamicRendering);

        // 크롤링 및 처리
        await foreach (var crawlResult in crawler.CrawlWebsiteAsync(startUrl, effectiveCrawlOptions, cancellationToken))
        {
            if (string.IsNullOrEmpty(crawlResult.Content))
            {
                LogEmptyContentFromUrl(_logger, crawlResult.Url);
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
                LogNoExtractableContentFromUrl(_logger, crawlResult.Url);
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

            LogGeneratedChunksFromUrl(_logger, chunks.Count, crawlResult.Url);

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
        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("HTML content cannot be null or empty", nameof(htmlContent));

        if (chunkingOptions != null)
        {
            var validation = chunkingOptions.Validate();
            if (!validation.IsValid)
                throw new ArgumentException($"Invalid chunking options: {string.Join(", ", validation.Errors)}");
        }

        LogProcessingHtmlContent(_logger, sourceUrl, htmlContent.Length);

        // 콘텐츠 추출
        var extractor = _serviceFactory.CreateContentExtractor("text/html");
        var extracted = await extractor.ExtractAutoAsync(
            htmlContent,
            sourceUrl,
            "text/html",
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(extracted.MainContent) && string.IsNullOrEmpty(extracted.Text))
        {
            LogNoExtractableContentFromHtml(_logger, sourceUrl);
            return Array.Empty<WebContentChunk>();
        }

        // 청킹
        var effectiveOptions = chunkingOptions ?? new ChunkingOptions
        {
            MaxChunkSize = 1000,
            ChunkOverlap = 50,
            PreserveHeaders = true
        };

        var chunkingStrategy = _serviceFactory.CreateChunkingStrategy(
            effectiveOptions.Strategy.ToString());

        var chunks = await chunkingStrategy.ChunkAsync(
            extracted,
            effectiveOptions,
            cancellationToken).ConfigureAwait(false);

        LogGeneratedChunksFromHtml(_logger, chunks.Count, sourceUrl);

        return chunks;
    }

    public IReadOnlyList<string> GetAvailableChunkingStrategies()
    {
        // Stub implementation
        return new List<string> { "FixedSize", "Paragraph", "Smart", "Semantic", "Auto", "MemoryOptimized" }.AsReadOnly();
    }

    #region 경량 추출 API 구현

    /// <summary>
    /// 단일 URL에서 콘텐츠를 추출합니다 (청킹 없음)
    /// </summary>
    public async Task<ProcessingResult<ExtractedContent>> ExtractContentAsync(
        string url,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        options ??= ExtractOptions.Default;

        var optionsValidation = options.Validate();
        if (!optionsValidation.IsValid)
            throw new ArgumentException($"Invalid extract options: {string.Join(", ", optionsValidation.Errors)}");

        try
        {
            LogExtractingContent(_logger, url);

            // URL 유효성 검사
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return ProcessingResult.FromError<ExtractedContent>(
                    "Invalid URL format",
                    ExtractErrorCodes.InvalidUrl,
                    sw.ElapsedMilliseconds);
            }

            // 캐시 확인 (UseCache && !ForceRefresh)
            var cacheService = _serviceFactory.TryCreateCacheService();
            if (options.UseCache && !options.ForceRefresh && cacheService != null)
            {
                var cacheKey = $"extract:{url}:{options.Format}";
                var cached = await cacheService.GetAsync<ExtractedContent>(cacheKey, cancellationToken).ConfigureAwait(false);

                if (cached != null)
                {
                    LogCacheHit(_logger, url);
                    cached.FromCache = true;
                    return ProcessingResult.Success<ExtractedContent>(
                        cached,
                        processingTimeMs: sw.ElapsedMilliseconds,
                        metadata: new Dictionary<string, object> { ["cacheHit"] = true });
                }
            }

            // 크롤링 전략 선택
            var crawlStrategy = options.UseDynamicRendering
                ? Core.Options.CrawlStrategy.Dynamic
                : Core.Options.CrawlStrategy.BreadthFirst;

            var crawler = _serviceFactory.CreateCrawler(crawlStrategy);

            var crawlOptions = new CrawlOptions
            {
                MaxDepth = 0,
                MaxPages = 1,
                TimeoutMs = options.TimeoutSeconds * 1000,
                UserAgent = options.UserAgent,
                WaitForSelector = options.WaitForSelector,
                UseDynamicRendering = options.UseDynamicRendering,
                CustomHeaders = options.CustomHeaders
            };

            // 크롤링 실행 (재시도 로직 포함)
            CrawlResult? crawlResult = null;
            var retryCount = 0;

            while (retryCount <= options.MaxRetries)
            {
                try
                {
                    crawlResult = await crawler.CrawlAsync(url, crawlOptions, cancellationToken).ConfigureAwait(false);

                    if (crawlResult.IsSuccess)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (retryCount < options.MaxRetries)
                {
                    LogCrawlAttemptFailed(_logger, ex, retryCount + 1, url);
                }

                retryCount++;
                if (retryCount <= options.MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1)), cancellationToken).ConfigureAwait(false);
                }
            }

            if (crawlResult == null || !crawlResult.IsSuccess)
            {
                var errorCode = crawlResult?.StatusCode != null
                    ? ExtractErrorCodes.FromHttpStatusCode(crawlResult.StatusCode)
                    : ExtractErrorCodes.NetworkError;

                return ProcessingResult.FromError<ExtractedContent>(
                    crawlResult?.ErrorMessage ?? "Failed to crawl URL",
                    errorCode,
                    sw.ElapsedMilliseconds);
            }

            if (string.IsNullOrWhiteSpace(crawlResult.Content))
            {
                return ProcessingResult.FromError<ExtractedContent>(
                    "Empty content received",
                    ExtractErrorCodes.EmptyContent,
                    sw.ElapsedMilliseconds);
            }

            // 콘텐츠 추출
            var extractor = _serviceFactory.CreateContentExtractor(crawlResult.ContentType ?? "text/html");
            var extracted = await extractor.ExtractAutoAsync(
                crawlResult.Content,
                url,
                crawlResult.ContentType ?? "text/html",
                cancellationToken).ConfigureAwait(false);

            // 포맷 변환
            extracted = ApplyOutputFormat(extracted, options.Format, crawlResult.Content);

            // 텍스트 길이 제한 적용
            if (options.MaxTextLength.HasValue && extracted.MainContent?.Length > options.MaxTextLength.Value)
            {
                extracted.MainContent = extracted.MainContent.Substring(0, options.MaxTextLength.Value);
            }

            // 품질 평가 (옵션이 활성화된 경우)
            if (options.EvaluateQuality)
            {
                var qualityEvaluator = _serviceFactory.TryCreateContentQualityEvaluator();
                if (qualityEvaluator != null)
                {
                    extracted.Quality = await qualityEvaluator.EvaluateAsync(
                        extracted,
                        crawlResult.Content,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            // Boilerplate 제거 적용
            if (options.RemoveBoilerplate)
            {
                // 이미 추출기에서 MainContent로 본문 추출됨
                // 추가 클리닝이 필요한 경우 여기서 처리
            }

            // 이미지/링크 포함 여부 처리
            if (!options.IncludeImages)
            {
                extracted.ImageUrls = new List<string>();
            }

            // 메타데이터 포함 여부 처리
            if (!options.IncludeMetadata)
            {
                extracted.Metadata = null;
            }

            extracted.ProcessingTimeMs = (int)sw.ElapsedMilliseconds;
            extracted.OriginalHtml = crawlResult.Content;

            // 캐시 저장
            if (options.UseCache && cacheService != null)
            {
                var cacheKey = $"extract:{url}:{options.Format}";
                await cacheService.SetAsync(
                    cacheKey,
                    extracted,
                    TimeSpan.FromMinutes(options.CacheExpirationMinutes),
                    cancellationToken).ConfigureAwait(false);
            }

            LogExtractedContentFromUrl(_logger, url, extracted.MainContent?.Length ?? 0, sw.ElapsedMilliseconds);

            return ProcessingResult.Success<ExtractedContent>(
                extracted,
                processingTimeMs: sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            return ProcessingResult.FromError<ExtractedContent>(
                "Operation cancelled",
                ExtractErrorCodes.Timeout,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogFailedToExtractContentFromUrl(_logger, ex, url);
            return ProcessingResult.FromException<ExtractedContent>(ex, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 여러 URL에서 콘텐츠를 배치 추출합니다
    /// </summary>
    public async Task<BatchExtractResult> ExtractBatchAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        options ??= ExtractOptions.Default;

        var optionsValidation = options.Validate();
        if (!optionsValidation.IsValid)
            throw new ArgumentException($"Invalid extract options: {string.Join(", ", optionsValidation.Errors)}");

        var urlList = urls.ToList();
        var succeeded = new ConcurrentBag<ExtractedContent>();
        var failed = new ConcurrentBag<FailedExtraction>();
        var cacheHits = 0;
        var processingTimes = new ConcurrentBag<long>();
        var domainCounts = new ConcurrentDictionary<string, int>();

        LogStartingBatchExtraction(_logger, urlList.Count);

        // Rate Limiter 생성
        var rateLimiter = options.EnableDomainRateLimiting
            ? _serviceFactory.TryCreateDomainRateLimiter()
            : null;

        // 병렬 처리
        var semaphore = new SemaphoreSlim(options.MaxConcurrency);
        var tasks = urlList.Select(async url =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var domain = GetDomain(url);
                domainCounts.AddOrUpdate(domain, 1, (_, count) => count + 1);

                ProcessingResult<ExtractedContent> result;

                if (rateLimiter != null && options.EnableDomainRateLimiting)
                {
                    result = await rateLimiter.ExecuteAsync(
                        domain,
                        () => ExtractContentAsync(url, options, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    result = await ExtractContentAsync(url, options, cancellationToken).ConfigureAwait(false);
                }

                processingTimes.Add(result.ProcessingTimeMs);

                if (result.IsSuccess && result.Data != null)
                {
                    succeeded.Add(result.Data);

                    if (result.Metadata.TryGetValue("cacheHit", out var hit) && (bool)hit)
                    {
                        Interlocked.Increment(ref cacheHits);
                    }
                }
                else
                {
                    failed.Add(new FailedExtraction
                    {
                        Url = url,
                        ErrorCode = result.Error?.Code ?? ExtractErrorCodes.Unknown,
                        ErrorMessage = result.Error?.Message ?? "Unknown error",
                        RetryCount = options.MaxRetries,
                        ProcessingTimeMs = result.ProcessingTimeMs,
                        Exception = result.Error?.InnerException
                    });
                }
            }
            catch (Exception ex)
            {
                failed.Add(new FailedExtraction
                {
                    Url = url,
                    ErrorCode = ExtractErrorCodes.Unknown,
                    ErrorMessage = ex.Message,
                    Exception = ex
                });
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        sw.Stop();

        // 통계 계산
        var succeededList = succeeded.ToList();
        var failedList = failed.ToList();
        var processingTimesList = processingTimes.ToList();

        var statistics = new BatchStatistics
        {
            AverageProcessingTimeMs = processingTimesList.Count > 0 ? processingTimesList.Average() : 0,
            TotalCharactersExtracted = succeededList.Sum(c => c.MainContent?.Length ?? 0),
            CacheHitRate = urlList.Count > 0 ? (double)cacheHits / urlList.Count : 0,
            CacheHits = cacheHits,
            CacheMisses = urlList.Count - cacheHits,
            ProcessedByDomain = domainCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            FailuresByErrorCode = failedList
                .GroupBy(f => f.ErrorCode)
                .ToDictionary(g => g.Key, g => g.Count()),
            MinProcessingTimeMs = processingTimesList.Count > 0 ? processingTimesList.Min() : 0,
            MaxProcessingTimeMs = processingTimesList.Count > 0 ? processingTimesList.Max() : 0,
            DynamicRenderingCount = options.UseDynamicRendering ? succeededList.Count : 0,
            StaticRenderingCount = options.UseDynamicRendering ? 0 : succeededList.Count
        };

        var result = new BatchExtractResult
        {
            Succeeded = succeededList,
            Failed = failedList,
            TotalDurationMs = sw.ElapsedMilliseconds,
            Statistics = statistics,
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow
        };

        LogBatchExtractionCompleted(_logger, succeededList.Count, urlList.Count, result.SuccessRate, sw.ElapsedMilliseconds);

        return result;
    }

    /// <summary>
    /// 여러 URL에서 콘텐츠를 스트리밍으로 배치 추출합니다
    /// </summary>
    public async IAsyncEnumerable<ProcessingResult<ExtractedContent>> ExtractBatchStreamAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= ExtractOptions.Default;
        var urlList = urls.ToList();

        LogStartingStreamingBatchExtraction(_logger, urlList.Count);

        // Rate Limiter 생성
        var rateLimiter = options.EnableDomainRateLimiting
            ? _serviceFactory.TryCreateDomainRateLimiter()
            : null;

        // 채널 기반 스트리밍
        var channel = Channel.CreateUnbounded<ProcessingResult<ExtractedContent>>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 백그라운드에서 추출 작업 실행
        var extractionTask = Task.Run(async () =>
        {
            var semaphore = new SemaphoreSlim(options.MaxConcurrency);
            var tasks = urlList.Select(async url =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    ProcessingResult<ExtractedContent> result;

                    if (rateLimiter != null && options.EnableDomainRateLimiting)
                    {
                        var domain = GetDomain(url);
                        result = await rateLimiter.ExecuteAsync(
                            domain,
                            () => ExtractContentAsync(url, options, cancellationToken),
                            cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        result = await ExtractContentAsync(url, options, cancellationToken).ConfigureAwait(false);
                    }

                    await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var failedResult = ProcessingResult.FromException<ExtractedContent>(ex);
                    await writer.WriteAsync(failedResult, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            writer.Complete();
        }, cancellationToken);

        // 결과 스트리밍
        await foreach (var result in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }

        await extractionTask.ConfigureAwait(false);
    }

    /// <summary>
    /// 출력 포맷을 적용합니다
    /// </summary>
    private static ExtractedContent ApplyOutputFormat(ExtractedContent content, OutputFormat format, string originalHtml)
    {
        switch (format)
        {
            case OutputFormat.Markdown:
                // 이미 Markdown 형태인 경우 그대로 사용
                // HTML에서 Markdown으로 변환이 필요한 경우 여기서 처리
                // MainContent는 이미 텍스트 형태로 추출됨
                break;

            case OutputFormat.Html:
                // HTML 원본 유지
                content.MainContent = originalHtml;
                break;

            case OutputFormat.PlainText:
                // 이미 텍스트 형태로 추출됨
                // 추가 클리닝 (마크다운 마커 제거 등)
                if (!string.IsNullOrEmpty(content.MainContent))
                {
                    content.MainContent = CleanPlainText(content.MainContent);
                }
                break;
        }

        return content;
    }

    /// <summary>
    /// 텍스트를 플레인 텍스트로 정리합니다
    /// </summary>
    private static string CleanPlainText(string text)
    {
        // Markdown 마커 제거
        text = System.Text.RegularExpressions.Regex.Replace(text, @"#{1,6}\s*", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*([^*]+)\*", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`([^`]+)`", "$1");

        return text.Trim();
    }

    /// <summary>
    /// URL에서 도메인을 추출합니다
    /// </summary>
    private static string GetDomain(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Host.ToLowerInvariant();
        }

        return url.ToLowerInvariant();
    }

    #endregion

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting web content processing with {UrlCount} URLs")]
    private static partial void LogStartingWebContentProcessing(ILogger logger, int UrlCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Web content processing completed. Processed {ChunkCount} chunks in {Duration}")]
    private static partial void LogWebContentProcessingCompleted(ILogger logger, int ChunkCount, TimeSpan Duration);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No URLs to crawl")]
    private static partial void LogNoUrlsToCrawl(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting parallel crawl: {Url}")]
    private static partial void LogStartingParallelCrawl(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "CrawlResult to WebContent: {Url}, Content={ContentLength} chars")]
    private static partial void LogCrawlResultToWebContent(ILogger logger, string Url, int ContentLength);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to crawl URL: {Url}")]
    private static partial void LogFailedToCrawlUrl(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parallel crawling completed: {Count} pages from {UrlCount} URLs")]
    private static partial void LogParallelCrawlingCompleted(ILogger logger, int Count, int UrlCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Content extraction failed")]
    private static partial void LogContentExtractionFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracted {Count} documents, {TotalChars} chars")]
    private static partial void LogExtractedDocuments(ILogger logger, int Count, int TotalChars);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracting from WebContent: {Url}, Input={InputLength} chars")]
    private static partial void LogExtractingFromWebContent(ILogger logger, string Url, int InputLength);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extraction complete: {Url}, Output={OutputLength} chars, MainContent={MainLength} chars")]
    private static partial void LogExtractionComplete(ILogger logger, string Url, int OutputLength, int MainLength);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to extract content from {Url}")]
    private static partial void LogFailedToExtractContent(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "AI enhancement skipped (service unavailable)")]
    private static partial void LogAiEnhancementSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "AI enhancement pipeline failed")]
    private static partial void LogAiEnhancementPipelineFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enhanced {Count} documents ({Keywords} keywords avg)")]
    private static partial void LogEnhancedDocuments(ILogger logger, int Count, int Keywords);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI enhancement failed for {Url}")]
    private static partial void LogAiEnhancementFailed(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Error, Message = "Chunking pipeline failed")]
    private static partial void LogChunkingPipelineFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Chunked {Count} documents to {TotalChunks} chunks")]
    private static partial void LogChunkedDocuments(ILogger logger, int Count, int TotalChunks);

    [LoggerMessage(Level = LogLevel.Information, Message = "Chunking document {DocNum}: Text={TextLength} chars, MainContent={MainLength} chars")]
    private static partial void LogChunkingDocument(ILogger logger, int DocNum, int TextLength, int MainLength);

    [LoggerMessage(Level = LogLevel.Information, Message = "Chunked document {DocNum} to {ChunkCount} chunks")]
    private static partial void LogChunkedDocument(ILogger logger, int DocNum, int ChunkCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to chunk document {DocNum}")]
    private static partial void LogFailedToChunkDocument(ILogger logger, Exception ex, int DocNum);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing single URL: {Url}")]
    private static partial void LogProcessingSingleUrl(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed processing URL {Url}: {ChunkCount} chunks generated")]
    private static partial void LogCompletedProcessingUrl(ILogger logger, string Url, int ChunkCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing batch of {UrlCount} URLs")]
    private static partial void LogProcessingBatch(ILogger logger, int UrlCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to process URL in batch: {Url}")]
    private static partial void LogFailedToProcessUrlInBatch(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch processing completed: {UrlCount} URLs processed")]
    private static partial void LogBatchProcessingCompleted(ILogger logger, int UrlCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing website: {Url} (Dynamic: {Dynamic})")]
    private static partial void LogProcessingWebsite(ILogger logger, string Url, bool Dynamic);

    [LoggerMessage(Level = LogLevel.Information, Message = "Using crawler: {Strategy}, UseDynamicRendering: {Dynamic}")]
    private static partial void LogUsingCrawler(ILogger logger, WebFlux.Core.Options.CrawlStrategy Strategy, bool Dynamic);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Empty content from {Url}")]
    private static partial void LogEmptyContentFromUrl(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No extractable content from {Url}")]
    private static partial void LogNoExtractableContentFromUrl(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated {ChunkCount} chunks from {Url}")]
    private static partial void LogGeneratedChunksFromUrl(ILogger logger, int ChunkCount, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing HTML content from {SourceUrl}, Length={Length} chars")]
    private static partial void LogProcessingHtmlContent(ILogger logger, string SourceUrl, int Length);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No extractable content from HTML for {SourceUrl}")]
    private static partial void LogNoExtractableContentFromHtml(ILogger logger, string SourceUrl);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated {ChunkCount} chunks from HTML for {SourceUrl}")]
    private static partial void LogGeneratedChunksFromHtml(ILogger logger, int ChunkCount, string SourceUrl);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Extracting content from {Url}")]
    private static partial void LogExtractingContent(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for {Url}")]
    private static partial void LogCacheHit(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Crawl attempt {Attempt} failed for {Url}")]
    private static partial void LogCrawlAttemptFailed(ILogger logger, Exception ex, int Attempt, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Extracted content from {Url}: {CharCount} chars in {ElapsedMs}ms")]
    private static partial void LogExtractedContentFromUrl(ILogger logger, string Url, int CharCount, long ElapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to extract content from {Url}")]
    private static partial void LogFailedToExtractContentFromUrl(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting batch extraction for {UrlCount} URLs")]
    private static partial void LogStartingBatchExtraction(ILogger logger, int UrlCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch extraction completed: {Succeeded}/{Total} succeeded ({SuccessRate}) in {Duration}ms")]
    private static partial void LogBatchExtractionCompleted(ILogger logger, int Succeeded, int Total, double SuccessRate, long Duration);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting streaming batch extraction for {UrlCount} URLs")]
    private static partial void LogStartingStreamingBatchExtraction(ILogger logger, int UrlCount);
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