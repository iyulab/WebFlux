# WebFlux 처리 파이프라인 설계

> 크롤링부터 청킹까지 완전한 웹 콘텐츠 처리 파이프라인

## 🔄 파이프라인 개요

WebFlux 파이프라인은 **4단계 처리 프로세스**를 통해 웹 콘텐츠를 RAG 최적화 청크로 변환합니다.

```
🕷️ Crawler → 📄 Extractor → 🧠 Parser → 🎯 Chunking → ✨ RAG 청크
```

### 설계 원칙

1. **스트리밍 우선**: 메모리 효율적인 실시간 처리
2. **병렬 처리**: CPU 코어 활용 극대화
3. **백프레셔 제어**: 메모리 압박 시 자동 조절
4. **오류 복구**: 단계별 실패 처리 및 재시도
5. **진행률 추적**: 실시간 처리 상태 제공

## 🏗️ 파이프라인 아키텍처

### 전체 아키텍처 다이어그램

```
┌─────────────────────────────────────────────────────────────────┐
│                    WebFlux Pipeline                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │
│  │   Stage 1   │───▶│   Stage 2   │───▶│   Stage 3   │───┐      │
│  │   Crawler   │    │  Extractor  │    │   Parser    │   │      │
│  └─────────────┘    └─────────────┘    └─────────────┘   │      │
│         │                   │                   │        │      │
│         ▼                   ▼                   ▼        ▼      │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐   │      │
│  │ CrawlQueue  │    │ExtractQueue │    │ ParseQueue  │   │      │
│  │ (Channel)   │    │ (Channel)   │    │ (Channel)   │   │      │
│  └─────────────┘    └─────────────┘    └─────────────┘   │      │
│                                                          │      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Stage 4                              │   │
│  │               Chunking Engine                          │◀──┘
│  │  ┌───────────┐  ┌───────────┐  ┌───────────┐         │
│  │  │Strategy 1 │  │Strategy 2 │  │Strategy N │         │
│  │  └───────────┘  └───────────┘  └───────────┘         │
│  └─────────────────────────────────────────────────────────┘
│                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐
│  │               Output Stream                                 │
│  │    IAsyncEnumerable<ProcessingResult<WebContentChunk>>     │
│  └─────────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────────┘
```

### 핵심 컴포넌트

#### 1. Pipeline Orchestrator
```csharp
public class WebContentProcessingPipeline
{
    private readonly ICrawlerFactory _crawlerFactory;
    private readonly IContentExtractorFactory _extractorFactory;
    private readonly IContentParser _contentParser;
    private readonly IChunkingStrategyFactory _chunkingFactory;
    private readonly IPipelineMonitor _monitor;
    private readonly PipelineConfiguration _config;

    public async IAsyncEnumerable<ProcessingResult<IEnumerable<WebContentChunk>>>
        ProcessAsync(
            string baseUrl,
            CrawlOptions? crawlOptions = null,
            ChunkingOptions? chunkingOptions = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var pipeline = new PipelineContext(_config);

        // 1. 파이프라인 초기화
        await InitializePipelineAsync(pipeline, baseUrl, crawlOptions);

        // 2. 단계별 처리 채널 생성
        var crawlChannel = Channel.CreateBounded<CrawlResult>(_config.CrawlChannelCapacity);
        var extractChannel = Channel.CreateBounded<RawWebContent>(_config.ExtractChannelCapacity);
        var parseChannel = Channel.CreateBounded<ParsedWebContent>(_config.ParseChannelCapacity);

        // 3. 병렬 파이프라인 시작
        var crawlTask = ExecuteCrawlStageAsync(baseUrl, crawlOptions, crawlChannel.Writer, pipeline);
        var extractTask = ExecuteExtractStageAsync(crawlChannel.Reader, extractChannel.Writer, pipeline);
        var parseTask = ExecuteParseStageAsync(extractChannel.Reader, parseChannel.Writer, pipeline);

        // 4. 청킹 결과 스트리밍
        await foreach (var parsedContent in parseChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var chunks = await ExecuteChunkingStageAsync(parsedContent, chunkingOptions, pipeline);

            yield return ProcessingResult<IEnumerable<WebContentChunk>>.Success(
                chunks,
                pipeline.GetCurrentProgress()
            );
        }
    }
}
```

## 🕷️ Stage 1: Web Crawling

### 설계 목표
- robots.txt 준수하는 예의 있는 크롤링
- 중복 URL 자동 필터링
- 동적 확장 가능한 병렬 처리

### 구현 아키텍처

```csharp
public class CrawlStageProcessor
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly HashSet<string> _visitedUrls;
    private readonly IRobotsTxtParser _robotsParser;
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task ExecuteAsync(
        string baseUrl,
        CrawlOptions options,
        ChannelWriter<CrawlResult> outputChannel,
        PipelineContext context)
    {
        var crawler = _crawlerFactory.CreateOptimalCrawler(baseUrl, options);
        var urlQueue = new PriorityQueue<CrawlTask, int>();

        // 초기 URL 추가
        urlQueue.Enqueue(new CrawlTask(baseUrl, 0), 0);

        var activeTasks = new List<Task>();
        var processedCount = 0;

        while (urlQueue.Count > 0 || activeTasks.Any())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // 동시 처리 한도 관리
            while (activeTasks.Count < options.MaxConcurrentRequests && urlQueue.TryDequeue(out var task, out var priority))
            {
                var crawlTask = ProcessSingleUrlAsync(task, options, outputChannel, context);
                activeTasks.Add(crawlTask);
            }

            // 완료된 태스크 정리
            var completedTask = await Task.WhenAny(activeTasks);
            activeTasks.Remove(completedTask);

            var result = await completedTask;
            if (result != null)
            {
                processedCount++;
                context.UpdateProgress(processedCount, "Crawling");

                // 새로운 URL 추가
                await AddDiscoveredUrls(result, urlQueue, options, context);
            }
        }

        outputChannel.Complete();
    }

    private async Task<CrawlResult?> ProcessSingleUrlAsync(
        CrawlTask task,
        CrawlOptions options,
        ChannelWriter<CrawlResult> outputChannel,
        PipelineContext context)
    {
        await _concurrencySemaphore.WaitAsync(context.CancellationToken);

        try
        {
            // 중복 체크
            lock (_visitedUrls)
            {
                if (!_visitedUrls.Add(task.Url))
                    return null; // 이미 처리됨
            }

            // robots.txt 확인
            if (options.RespectRobotsTxt && !await _robotsParser.IsAllowedAsync(task.Url, options.UserAgent))
            {
                return new CrawlResult
                {
                    Url = task.Url,
                    Error = new CrawlError { ErrorType = "RobotsDisallowed", Message = "Blocked by robots.txt" }
                };
            }

            // HTTP 요청 실행
            using var httpClient = _httpClientFactory.CreateClient("WebFlux");
            var response = await httpClient.GetAsync(task.Url, context.CancellationToken);

            var result = new CrawlResult
            {
                Url = task.Url,
                Depth = task.Depth,
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "unknown",
                ContentLength = response.Content.Headers.ContentLength ?? 0,
                ResponseTime = context.Stopwatch.Elapsed
            };

            // 출력 채널에 전송
            await outputChannel.WriteAsync(result, context.CancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new CrawlResult
            {
                Url = task.Url,
                Error = new CrawlError { ErrorType = ex.GetType().Name, Message = ex.Message }
            };

            await outputChannel.WriteAsync(errorResult, context.CancellationToken);
            return errorResult;
        }
        finally
        {
            _concurrencySemaphore.Release();

            // 요청 간 지연
            if (options.DelayBetweenRequests > TimeSpan.Zero)
                await Task.Delay(options.DelayBetweenRequests, context.CancellationToken);
        }
    }
}

public class CrawlTask
{
    public string Url { get; set; }
    public int Depth { get; set; }
    public string? ParentUrl { get; set; }
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;

    public CrawlTask(string url, int depth, string? parentUrl = null)
    {
        Url = url;
        Depth = depth;
        ParentUrl = parentUrl;
    }
}
```

### 크롤링 전략 구현

#### BreadthFirstCrawler
```csharp
public class BreadthFirstCrawler : ICrawler
{
    public string StrategyName => "BreadthFirst";

    public async Task<IEnumerable<CrawlResult>> CrawlAsync(
        string baseUrl,
        CrawlOptions options,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CrawlResult>();
        var queue = new Queue<(string Url, int Depth)>();
        var visited = new HashSet<string>();

        queue.Enqueue((baseUrl, 0));

        while (queue.Count > 0 && results.Count < options.MaxPages)
        {
            var (currentUrl, depth) = queue.Dequeue();

            if (depth > options.MaxDepth || visited.Contains(currentUrl))
                continue;

            visited.Add(currentUrl);

            try
            {
                var result = await CrawlSinglePageAsync(currentUrl, depth);
                results.Add(result);

                // 링크 추출 및 큐에 추가
                if (depth < options.MaxDepth)
                {
                    var links = ExtractLinks(result.Content, currentUrl);
                    foreach (var link in links.Take(10)) // 페이지당 최대 10개 링크
                    {
                        if (!visited.Contains(link))
                            queue.Enqueue((link, depth + 1));
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new CrawlResult
                {
                    Url = currentUrl,
                    Error = new CrawlError { Message = ex.Message }
                });
            }
        }

        return results;
    }
}
```

## 📄 Stage 2: Content Extraction

### 설계 목표
- 다양한 콘텐츠 형식 지원 (HTML, Markdown, JSON, XML)
- 노이즈 제거 및 구조 보존
- 메타데이터 정확한 추출

### 구현 아키텍처

```csharp
public class ExtractStageProcessor
{
    private readonly IContentExtractorFactory _extractorFactory;
    private readonly ILogger<ExtractStageProcessor> _logger;

    public async Task ExecuteAsync(
        ChannelReader<CrawlResult> inputChannel,
        ChannelWriter<RawWebContent> outputChannel,
        PipelineContext context)
    {
        var semaphore = new SemaphoreSlim(context.Config.ExtractionConcurrency);
        var tasks = new List<Task>();

        await foreach (var crawlResult in inputChannel.ReadAllAsync(context.CancellationToken))
        {
            var task = ProcessSingleContentAsync(crawlResult, outputChannel, semaphore, context);
            tasks.Add(task);

            // 태스크 정리 (메모리 관리)
            if (tasks.Count >= context.Config.MaxConcurrentExtractions)
            {
                var completed = await Task.WhenAny(tasks);
                tasks.Remove(completed);
                await completed; // 예외 전파
            }
        }

        // 남은 태스크 완료 대기
        await Task.WhenAll(tasks);
        outputChannel.Complete();
    }

    private async Task ProcessSingleContentAsync(
        CrawlResult crawlResult,
        ChannelWriter<RawWebContent> outputChannel,
        SemaphoreSlim semaphore,
        PipelineContext context)
    {
        await semaphore.WaitAsync(context.CancellationToken);

        try
        {
            // 추출기 선택
            var extractor = _extractorFactory.GetExtractor(crawlResult.ContentType, crawlResult.Url);

            if (extractor == null)
            {
                _logger.LogWarning("No suitable extractor found for {ContentType} at {Url}",
                    crawlResult.ContentType, crawlResult.Url);
                return;
            }

            // HTTP 재요청 (콘텐츠 추출용)
            using var httpClient = context.HttpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(crawlResult.Url, context.CancellationToken);

            // 콘텐츠 추출
            var extractedContent = await extractor.ExtractAsync(crawlResult.Url, response, context.CancellationToken);

            // 품질 검증
            var quality = extractor.EvaluateQuality(extractedContent);
            if (quality < context.Config.MinContentQuality)
            {
                _logger.LogWarning("Content quality too low ({Quality}) for {Url}", quality, crawlResult.Url);
                return;
            }

            await outputChannel.WriteAsync(extractedContent, context.CancellationToken);
            context.UpdateProgress($"Extracted: {crawlResult.Url}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract content from {Url}", crawlResult.Url);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
```

### 콘텐츠 추출기 구현 예시

#### HtmlContentExtractor
```csharp
public class HtmlContentExtractor : IContentExtractor
{
    private readonly HtmlToMarkdownConverter _markdownConverter;
    private readonly IImageProcessor _imageProcessor;

    public string ExtractorType => "Html";
    public IEnumerable<string> SupportedContentTypes => new[] { "text/html", "application/xhtml+xml" };

    public async Task<RawWebContent> ExtractAsync(
        string url,
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = new HtmlDocument();
        document.LoadHtml(htmlContent);

        // 1. 메타데이터 추출
        var metadata = ExtractMetadata(document, response);

        // 2. 메인 콘텐츠 추출
        var mainContent = ExtractMainContent(document);

        // 3. 마크다운 변환
        var markdownContent = await _markdownConverter.ConvertAsync(mainContent);

        // 4. 이미지 URL 수집
        var imageUrls = ExtractImageUrls(document, url);

        // 5. 링크 수집
        var links = ExtractLinks(document, url);

        return new RawWebContent
        {
            Url = url,
            Content = markdownContent,
            ContentType = "text/markdown", // 변환 후 타입
            Metadata = metadata,
            ExtractorType = ExtractorType,
            ImageUrls = imageUrls,
            Links = links,
            Properties = new Dictionary<string, object>
            {
                ["OriginalContentType"] = response.Content.Headers.ContentType?.ToString() ?? "unknown",
                ["ProcessingTime"] = DateTime.UtcNow
            }
        };
    }

    private WebContentMetadata ExtractMetadata(HtmlDocument document, HttpResponseMessage response)
    {
        var metadata = new WebContentMetadata();

        // 제목 추출
        var titleNode = document.DocumentNode.SelectSingleNode("//title");
        metadata.Title = titleNode?.InnerText?.Trim() ?? "";

        // 설명 추출
        var descriptionNode = document.DocumentNode
            .SelectSingleNode("//meta[@name='description']/@content") ??
            document.DocumentNode.SelectSingleNode("//meta[@property='og:description']/@content");
        metadata.Description = descriptionNode?.Value ?? "";

        // 키워드 추출
        var keywordsNode = document.DocumentNode.SelectSingleNode("//meta[@name='keywords']/@content");
        if (keywordsNode?.Value != null)
        {
            metadata.Keywords = keywordsNode.Value.Split(',').Select(k => k.Trim()).ToList();
        }

        // 작성자
        var authorNode = document.DocumentNode.SelectSingleNode("//meta[@name='author']/@content");
        metadata.Author = authorNode?.Value ?? "";

        // 언어
        var langNode = document.DocumentNode.SelectSingleNode("//html/@lang") ??
                      document.DocumentNode.SelectSingleNode("//meta[@http-equiv='Content-Language']/@content");
        metadata.Language = langNode?.Value ?? "en";

        // Last-Modified
        metadata.LastModified = response.Content.Headers.LastModified?.DateTime;

        return metadata;
    }
}
```

## 🧠 Stage 3: Content Parsing

### 설계 목표
- 구조화된 콘텐츠 분석 및 파싱
- 섹션, 표, 이미지 정보 구조화
- LLM 기반 콘텐츠 이해 (선택적)

### 구현 아키텍처

```csharp
public class ParseStageProcessor
{
    private readonly IContentParser _contentParser;
    private readonly ITextCompletionService? _llmService; // 선택적
    private readonly ILogger<ParseStageProcessor> _logger;

    public async Task ExecuteAsync(
        ChannelReader<RawWebContent> inputChannel,
        ChannelWriter<ParsedWebContent> outputChannel,
        PipelineContext context)
    {
        await foreach (var rawContent in inputChannel.ReadAllAsync(context.CancellationToken))
        {
            try
            {
                var parsedContent = await _contentParser.ParseAsync(rawContent, context.CancellationToken);

                // LLM 기반 보강 (선택적)
                if (_llmService != null && context.Config.EnableLLMEnhancement)
                {
                    parsedContent = await EnhanceWithLLM(parsedContent, context.CancellationToken);
                }

                await outputChannel.WriteAsync(parsedContent, context.CancellationToken);
                context.UpdateProgress($"Parsed: {rawContent.Url}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse content from {Url}", rawContent.Url);
            }
        }

        outputChannel.Complete();
    }
}

public class ContentParser : IContentParser
{
    public async Task<ParsedWebContent> ParseAsync(
        RawWebContent rawContent,
        CancellationToken cancellationToken = default)
    {
        var parsedContent = new ParsedWebContent
        {
            Url = rawContent.Url,
            Metadata = rawContent.Metadata
        };

        // 1. 제목 추출
        parsedContent.Title = ExtractTitle(rawContent);

        // 2. 섹션 구조 분석
        parsedContent.Sections = ExtractSections(rawContent.Content);

        // 3. 메인 콘텐츠 추출
        parsedContent.MainContent = ExtractMainContent(parsedContent.Sections);

        // 4. 표 데이터 추출
        parsedContent.Tables = ExtractTables(rawContent.Content);

        // 5. 이미지 데이터 구조화
        parsedContent.Images = await ProcessImages(rawContent.ImageUrls);

        // 6. 구조 정보 생성
        parsedContent.Structure = AnalyzeStructure(parsedContent);

        return parsedContent;
    }

    private List<ContentSection> ExtractSections(string markdownContent)
    {
        var sections = new List<ContentSection>();
        var lines = markdownContent.Split('\n');
        var currentSection = new ContentSection();
        var position = 0;

        foreach (var line in lines)
        {
            if (IsHeaderLine(line))
            {
                // 이전 섹션 완료
                if (!string.IsNullOrEmpty(currentSection.Content))
                {
                    currentSection.EndPosition = position;
                    sections.Add(currentSection);
                }

                // 새 섹션 시작
                currentSection = new ContentSection
                {
                    Heading = ExtractHeadingText(line),
                    Level = GetHeaderLevel(line),
                    StartPosition = position
                };
            }
            else
            {
                currentSection.Content += line + "\n";
            }

            position += line.Length + 1;
        }

        // 마지막 섹션 추가
        if (!string.IsNullOrEmpty(currentSection.Content))
        {
            currentSection.EndPosition = position;
            sections.Add(currentSection);
        }

        return BuildHierarchy(sections);
    }
}
```

## 🎯 Stage 4: Chunking

### 설계 목표
- 다중 청킹 전략 지원
- 자동 최적 전략 선택
- 품질 기반 동적 조정

### 구현 아키텍처

```csharp
public class ChunkingStageProcessor
{
    private readonly IChunkingStrategyFactory _strategyFactory;
    private readonly IChunkingQualityEvaluator _qualityEvaluator;
    private readonly ILogger<ChunkingStageProcessor> _logger;

    public async Task<IEnumerable<WebContentChunk>> ExecuteAsync(
        ParsedWebContent parsedContent,
        ChunkingOptions? options,
        PipelineContext context)
    {
        options ??= new ChunkingOptions();

        try
        {
            // 1. 최적 전략 선택 또는 지정된 전략 사용
            var strategy = options.Strategy == "Auto"
                ? _strategyFactory.CreateOptimalStrategy(parsedContent, options)
                : _strategyFactory.CreateStrategy(options.Strategy);

            // 2. 청킹 실행
            var chunks = await strategy.ChunkAsync(parsedContent, options, context.CancellationToken);

            // 3. 품질 평가
            var quality = _qualityEvaluator.EvaluateQuality(chunks, parsedContent);

            // 4. 품질 기준 미달 시 대안 전략 시도
            if (quality.OverallQuality < 0.7 && options.Strategy == "Auto")
            {
                _logger.LogWarning("Chunking quality below threshold ({Quality}) for {Url}, trying alternative strategy",
                    quality.OverallQuality, parsedContent.Url);

                var alternativeStrategy = SelectAlternativeStrategy(strategy.StrategyName, parsedContent);
                chunks = await alternativeStrategy.ChunkAsync(parsedContent, options, context.CancellationToken);
            }

            // 5. 메타데이터 보강
            return EnrichChunkMetadata(chunks, parsedContent, quality);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chunking failed for {Url}, falling back to FixedSize strategy", parsedContent.Url);

            // 실패 시 FixedSize 전략으로 폴백
            var fallbackStrategy = _strategyFactory.CreateStrategy("FixedSize");
            return await fallbackStrategy.ChunkAsync(parsedContent, options, context.CancellationToken);
        }
    }
}
```

## 🔄 병렬 처리 및 성능 최적화

### Threading.Channels 기반 파이프라인

```csharp
public class PipelineConfiguration
{
    // 채널 용량 설정
    public int CrawlChannelCapacity { get; set; } = 100;
    public int ExtractChannelCapacity { get; set; } = 50;
    public int ParseChannelCapacity { get; set; } = 25;

    // 동시성 설정
    public int CrawlingConcurrency { get; set; } = Environment.ProcessorCount;
    public int ExtractionConcurrency { get; set; } = Environment.ProcessorCount * 2;
    public int ParsingConcurrency { get; set; } = Environment.ProcessorCount;

    // 메모리 관리
    public long MaxMemoryUsage { get; set; } = 1024 * 1024 * 1024; // 1GB
    public int MemoryCheckInterval { get; set; } = 10; // 10개 처리마다 체크

    // 품질 관리
    public double MinContentQuality { get; set; } = 0.5;
    public bool EnableLLMEnhancement { get; set; } = false;
}

public class PipelineContext : IDisposable
{
    public CancellationToken CancellationToken { get; }
    public PipelineConfiguration Config { get; }
    public IHttpClientFactory HttpClientFactory { get; }
    public Stopwatch Stopwatch { get; }

    private ProcessingProgress _progress;
    private readonly object _progressLock = new();

    public PipelineContext(PipelineConfiguration config)
    {
        Config = config;
        Stopwatch = Stopwatch.StartNew();
        _progress = new ProcessingProgress();
    }

    public void UpdateProgress(int processed, string phase, string? currentUrl = null)
    {
        lock (_progressLock)
        {
            _progress.PagesProcessed = processed;
            _progress.CurrentPhase = phase;
            _progress.CurrentUrl = currentUrl;
            _progress.ElapsedTime = Stopwatch.Elapsed;
        }
    }

    public ProcessingProgress GetCurrentProgress()
    {
        lock (_progressLock)
        {
            return new ProcessingProgress
            {
                TotalPages = _progress.TotalPages,
                PagesProcessed = _progress.PagesProcessed,
                ChunksGenerated = _progress.ChunksGenerated,
                ElapsedTime = Stopwatch.Elapsed,
                CurrentPhase = _progress.CurrentPhase,
                CurrentUrl = _progress.CurrentUrl
            };
        }
    }

    public void Dispose()
    {
        Stopwatch?.Stop();
        GC.SuppressFinalize(this);
    }
}
```

### 메모리 백프레셔 제어

```csharp
public class MemoryPressureController
{
    private readonly PipelineConfiguration _config;
    private readonly ILogger<MemoryPressureController> _logger;

    public async Task<bool> ShouldThrottleAsync()
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryPressure = (double)currentMemory / _config.MaxMemoryUsage;

        if (memoryPressure > 0.8)
        {
            _logger.LogWarning("High memory pressure detected: {MemoryUsage}MB ({Percentage}%)",
                currentMemory / (1024 * 1024), memoryPressure * 100);

            // 메모리 압박 시 처리 지연
            await Task.Delay(100);

            // 가비지 컬렉션 수행
            GC.Collect(0, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();

            return true;
        }

        return false;
    }

    public void OptimizeMemoryUsage()
    {
        // Gen 0, 1 정리
        GC.Collect(1, GCCollectionMode.Optimized);

        // 대형 객체 힙 정리 (필요시)
        var gen2Collections = GC.CollectionCount(2);
        if (gen2Collections == 0) // Gen2 컬렉션이 없었다면 수행
        {
            GC.Collect(2, GCCollectionMode.Optimized);
        }
    }
}
```

## 📊 모니터링 및 메트릭

### 파이프라인 모니터링

```csharp
public interface IPipelineMonitor
{
    void RecordStageMetrics(string stageName, TimeSpan duration, bool success);
    void RecordThroughputMetrics(string stageName, int itemsProcessed);
    void RecordErrorMetrics(string stageName, string errorType);
    PipelineMetrics GetCurrentMetrics();
}

public class PipelineMetrics
{
    public Dictionary<string, StageMetrics> StageMetrics { get; set; } = new();
    public TimeSpan TotalProcessingTime { get; set; }
    public int TotalItemsProcessed { get; set; }
    public double OverallThroughput { get; set; }
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
}

public class StageMetrics
{
    public string StageName { get; set; } = string.Empty;
    public TimeSpan AverageProcessingTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public int ItemsProcessed { get; set; }
    public int ErrorCount { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double SuccessRate { get; set; }
}
```

### 실시간 진행률 추적

```csharp
public class ProgressTracker
{
    private readonly IProgress<ProcessingProgress>? _progress;
    private ProcessingProgress _currentProgress;

    public ProgressTracker(IProgress<ProcessingProgress>? progress = null)
    {
        _progress = progress;
        _currentProgress = new ProcessingProgress();
    }

    public void UpdateProgress(string phase, int processed, int total = 0, string? currentItem = null)
    {
        _currentProgress.CurrentPhase = phase;
        _currentProgress.PagesProcessed = processed;

        if (total > 0)
            _currentProgress.TotalPages = total;

        _currentProgress.CurrentUrl = currentItem;
        _currentProgress.ElapsedTime = DateTime.UtcNow - _currentProgress.StartTime;

        // 남은 시간 추정
        if (_currentProgress.PagesProcessed > 0 && _currentProgress.TotalPages > 0)
        {
            var avgTimePerPage = _currentProgress.ElapsedTime.TotalSeconds / _currentProgress.PagesProcessed;
            var remainingPages = _currentProgress.TotalPages - _currentProgress.PagesProcessed;
            _currentProgress.EstimatedTimeRemaining = TimeSpan.FromSeconds(avgTimePerPage * remainingPages);
        }

        _progress?.Report(_currentProgress);
    }
}
```

## 🔧 오류 처리 및 복구

### 재시도 메커니즘

```csharp
public class RetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly double _backoffMultiplier;

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Predicate<Exception> shouldRetry,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (shouldRetry(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                var delay = TimeSpan.FromMilliseconds(
                    _baseDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt));

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after retries");
    }
}
```

### Circuit Breaker 패턴

```csharp
public class CircuitBreaker
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _recoveryTimeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _recoveryTimeout)
            {
                _state = CircuitState.HalfOpen;
            }
            else
            {
                throw new CircuitBreakerOpenException();
            }
        }

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;

        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitState.Open;
        }
    }
}

public enum CircuitState { Closed, Open, HalfOpen }
```

---

이 파이프라인 설계는 연구 문서의 성능 요구사항을 만족하면서, 실제 구현 가능한 확장성 있는 아키텍처를 제공합니다. 각 단계는 독립적으로 확장 가능하며, 전체적으로 100페이지/분 목표 성능을 달성할 수 있도록 설계되었습니다.