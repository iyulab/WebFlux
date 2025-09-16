# WebFlux SDK 아키텍처 설계

> RAG 최적화 웹 콘텐츠 처리를 위한 Clean Architecture 기반 SDK

## 🏗️ 아키텍처 개요

WebFlux는 **인터페이스 제공자(Interface Provider)** 패턴을 채택하여, 핵심 기능의 인터페이스를 정의하고 소비 애플리케이션이 구현체를 선택할 수 있도록 설계되었습니다.

### 핵심 설계 원칙

1. **의존성 역전 원칙**: 구체적 구현체가 아닌 추상화에 의존
2. **단일 책임 원칙**: 각 컴포넌트는 하나의 명확한 책임
3. **개방-폐쇄 원칙**: 확장에는 열려있고 수정에는 닫혀있음
4. **AI 공급자 중립**: 특정 AI 서비스에 종속되지 않음

## 📦 레이어 구조

```
┌─────────────────────────────────────────┐
│           Consumer Application          │
│     (AI Service Implementations)       │
├─────────────────────────────────────────┤
│              WebFlux SDK                │
│                                         │
│  ┌─────────────────────────────────────┐ │
│  │        Application Layer            │ │
│  │    - IWebContentProcessor          │ │
│  │    - Pipeline Orchestration       │ │
│  └─────────────────────────────────────┘ │
│                                         │
│  ┌─────────────────────────────────────┐ │
│  │         Domain Layer                │ │
│  │    - Business Logic               │ │
│  │    - Domain Models                │ │
│  │    - Service Interfaces           │ │
│  └─────────────────────────────────────┘ │
│                                         │
│  ┌─────────────────────────────────────┐ │
│  │      Infrastructure Layer          │ │
│  │    - Crawlers                     │ │
│  │    - Content Extractors           │ │
│  │    - Chunking Strategies          │ │
│  └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

## 🔌 핵심 인터페이스 정의

### 1. AI 서비스 인터페이스 (Consumer Implementation)

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// LLM 텍스트 완성 서비스 인터페이스
    /// 소비 애플리케이션에서 OpenAI, Anthropic, Azure 등의 구현체 제공
    /// </summary>
    public interface ITextCompletionService
    {
        Task<string> CompleteAsync(
            string prompt,
            TextCompletionOptions? options = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 이미지-텍스트 변환 서비스 인터페이스 (멀티모달 처리용)
    /// GPT-4V, GPT-4o, LLaVA 등의 구현체 지원
    /// </summary>
    public interface IImageToTextService
    {
        Task<ImageToTextResult> ExtractTextFromWebImageAsync(
            string imageUrl,
            ImageToTextOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
```

### 2. 웹플럭스 핵심 인터페이스 (WebFlux Implementation)

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 웹 콘텐츠 처리의 메인 인터페이스
    /// 크롤링 → 추출 → 파싱 → 청킹의 전체 파이프라인 오케스트레이션
    /// </summary>
    public interface IWebContentProcessor
    {
        // 스트리밍 처리 (권장 - 메모리 효율적)
        IAsyncEnumerable<ProcessingResult<IEnumerable<WebContentChunk>>> ProcessWithProgressAsync(
            string baseUrl,
            CrawlOptions? crawlOptions = null,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);

        // 단계별 처리
        Task<IEnumerable<CrawlResult>> CrawlAsync(string baseUrl, CrawlOptions? options = null);
        Task<RawWebContent> ExtractAsync(string url);
        Task<ParsedWebContent> ParseAsync(RawWebContent rawContent);
        Task<IEnumerable<WebContentChunk>> ChunkAsync(ParsedWebContent parsedContent, ChunkingOptions? options = null);
    }

    /// <summary>
    /// 웹 크롤링 인터페이스
    /// 다양한 크롤링 전략 지원 (BreadthFirst, DepthFirst, Sitemap, Intelligent)
    /// </summary>
    public interface ICrawler
    {
        string StrategyName { get; }
        Task<IEnumerable<CrawlResult>> CrawlAsync(
            string baseUrl,
            CrawlOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 콘텐츠 추출 인터페이스
    /// HTML, Markdown, JSON, XML 등 다양한 형식 지원
    /// </summary>
    public interface IContentExtractor
    {
        string ExtractorType { get; }
        IEnumerable<string> SupportedContentTypes { get; }
        bool CanExtract(string contentType, string url);
        Task<RawWebContent> ExtractAsync(
            string url,
            HttpResponseMessage response,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 청킹 전략 인터페이스
    /// 7가지 전략: Auto, Smart, Intelligent, MemoryOptimized, Semantic, Paragraph, FixedSize
    /// </summary>
    public interface IChunkingStrategy
    {
        string StrategyName { get; }
        Task<IEnumerable<WebContentChunk>> ChunkAsync(
            ParsedWebContent content,
            ChunkingOptions options,
            CancellationToken cancellationToken = default);
    }
}
```

## 🎯 도메인 모델

### 핵심 데이터 모델

```csharp
namespace WebFlux.Core.Models
{
    /// <summary>
    /// 웹 콘텐츠 청크 - RAG 시스템에 최적화된 데이터 단위
    /// </summary>
    public class WebContentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public WebContentMetadata Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 청킹 관련 정보
        public string ChunkingStrategy { get; set; } = string.Empty;
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public double ConfidenceScore { get; set; } = 1.0;
    }

    /// <summary>
    /// 웹 콘텐츠 메타데이터
    /// </summary>
    public class WebContentMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public DateTime? LastModified { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public int ContentLength { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 크롤링 옵션
    /// </summary>
    public class CrawlOptions
    {
        public int MaxDepth { get; set; } = 3;
        public int MaxPages { get; set; } = 100;
        public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromMilliseconds(500);
        public bool RespectRobotsTxt { get; set; } = true;
        public string UserAgent { get; set; } = "WebFlux/1.0";
        public List<string> AllowedDomains { get; set; } = new();
        public List<string> ExcludePatterns { get; set; } = new();
        public List<string> IncludePatterns { get; set; } = new();
        public int MaxConcurrentRequests { get; set; } = 5;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int RetryCount { get; set; } = 3;
        public string Strategy { get; set; } = "BreadthFirst";
    }

    /// <summary>
    /// 청킹 옵션
    /// </summary>
    public class ChunkingOptions
    {
        public string Strategy { get; set; } = "Auto";
        public int MaxChunkSize { get; set; } = 512;
        public int OverlapSize { get; set; } = 64;
        public bool PreserveStructure { get; set; } = true;
        public double SemanticThreshold { get; set; } = 0.8;
        public Dictionary<string, object> StrategyParameters { get; set; } = new();
    }
}
```

## 🔄 파이프라인 아키텍처

### 처리 파이프라인 플로우

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Crawler   │───▶│  Extractor  │───▶│   Parser    │───▶│  Chunking   │
│             │    │             │    │             │    │  Strategy   │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       ▼                   ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ CrawlResult │    │RawWebContent│    │ParsedContent│    │WebContentChunk│
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

### 스트리밍 처리 아키텍처

```csharp
public async IAsyncEnumerable<ProcessingResult<IEnumerable<WebContentChunk>>>
    ProcessWithProgressAsync(
        string baseUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // Threading.Channels 기반 파이프라인
    var crawlChannel = Channel.CreateUnbounded<CrawlResult>();
    var extractChannel = Channel.CreateUnbounded<RawWebContent>();
    var parseChannel = Channel.CreateUnbounded<ParsedWebContent>();

    // 병렬 파이프라인 실행
    var crawlTask = CrawlAsync(baseUrl, crawlOptions, crawlChannel.Writer);
    var extractTask = ExtractAsync(crawlChannel.Reader, extractChannel.Writer);
    var parseTask = ParseAsync(extractChannel.Reader, parseChannel.Reader);

    // 스트리밍 청킹 결과 반환
    await foreach (var parsedContent in parseChannel.Reader.ReadAllAsync(cancellationToken))
    {
        var chunks = await ChunkAsync(parsedContent, chunkingOptions);
        yield return ProcessingResult<IEnumerable<WebContentChunk>>.Success(chunks);
    }
}
```

## 🚀 성능 최적화 아키텍처

### 1. 병렬 처리 엔진

```csharp
public class ParallelProcessingEngine
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly Channel<WorkItem> _workQueue;
    private readonly int _maxConcurrency;

    public ParallelProcessingEngine(int maxConcurrency = Environment.ProcessorCount)
    {
        _maxConcurrency = maxConcurrency;
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency);
        _workQueue = Channel.CreateUnbounded<WorkItem>();

        // 워커 태스크 시작
        for (int i = 0; i < maxConcurrency; i++)
        {
            _ = Task.Run(WorkerLoop);
        }
    }

    private async Task WorkerLoop()
    {
        await foreach (var workItem in _workQueue.Reader.ReadAllAsync())
        {
            await _concurrencySemaphore.WaitAsync();
            try
            {
                await ProcessWorkItem(workItem);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }
    }
}
```

### 2. 메모리 백프레셔 제어

```csharp
public class MemoryPressureController
{
    private readonly MemoryPressureOptions _options;
    private long _currentMemoryUsage;

    public async Task<bool> ShouldThrottleAsync()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        var memoryPressure = (double)memoryUsage / _options.MaxMemoryThreshold;

        if (memoryPressure > 0.8)
        {
            // 메모리 압박 시 지연
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            GC.Collect(0, GCCollectionMode.Optimized);
            return true;
        }

        return false;
    }
}
```

### 3. 지능형 캐시 시스템

```csharp
public class LRUWebContentCache
{
    private readonly Dictionary<string, CacheItem> _cache;
    private readonly LinkedList<string> _accessOrder;
    private readonly int _maxItems;

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var item))
        {
            // LRU 순서 업데이트
            _accessOrder.Remove(item.Node);
            _accessOrder.AddFirst(item.Node);
            return (T)item.Value;
        }
        return default(T);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        // 캐시 크기 관리
        while (_cache.Count >= _maxItems)
        {
            var lastKey = _accessOrder.Last!.Value;
            _cache.Remove(lastKey);
            _accessOrder.RemoveLast();
        }

        var node = _accessOrder.AddFirst(key);
        _cache[key] = new CacheItem(value, node, expiry);
    }
}
```

## 🧪 테스트 아키텍처

### 1. Mock 서비스 구현

```csharp
public class MockTextCompletionService : ITextCompletionService
{
    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // 실제 LLM 호출 대신 규칙 기반 응답
        if (prompt.Contains("summarize"))
            return "This is a test summary of the content.";

        if (prompt.Contains("chunk boundary"))
            return "Split at paragraph breaks.";

        return "Mock LLM response for testing purposes.";
    }
}

public class MockImageToTextService : IImageToTextService
{
    public async Task<ImageToTextResult> ExtractTextFromWebImageAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return new ImageToTextResult
        {
            ExtractedText = $"Mock description for image: {imageUrl}",
            Confidence = 0.95,
            IsSuccess = true,
            SourceUrl = imageUrl
        };
    }
}
```

### 2. 테스트 전략

```csharp
[TestClass]
public class WebContentProcessorTests
{
    [TestMethod]
    public async Task ProcessWithProgressAsync_Should_ReturnChunks()
    {
        // Arrange
        var mockLLM = new MockTextCompletionService();
        var processor = new WebContentProcessor(mockLLM, ...);

        // Act
        var results = new List<WebContentChunk>();
        await foreach (var result in processor.ProcessWithProgressAsync("https://example.com"))
        {
            if (result.IsSuccess)
                results.AddRange(result.Result!);
        }

        // Assert
        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(c => !string.IsNullOrEmpty(c.Content)));
    }
}
```

## 🔧 의존성 주입 구성

### ServiceCollection 확장

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebFlux(this IServiceCollection services)
    {
        // 핵심 서비스 등록
        services.AddScoped<IWebContentProcessor, WebContentProcessor>();

        // 크롤러 등록
        services.AddTransient<ICrawler, BreadthFirstCrawler>();
        services.AddTransient<ICrawler, DepthFirstCrawler>();
        services.AddTransient<ICrawler, SitemapCrawler>();
        services.AddTransient<ICrawler, IntelligentCrawler>();

        // 콘텐츠 추출기 등록
        services.AddTransient<IContentExtractor, HtmlContentExtractor>();
        services.AddTransient<IContentExtractor, MarkdownContentExtractor>();
        services.AddTransient<IContentExtractor, JsonContentExtractor>();

        // 청킹 전략 등록
        services.AddTransient<IChunkingStrategy, FixedSizeChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, ParagraphChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, SmartChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, SemanticChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, IntelligentChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, MemoryOptimizedChunkingStrategy>();
        services.AddTransient<IChunkingStrategy, AutoChunkingStrategy>();

        // 팩토리 서비스
        services.AddScoped<ICrawlerFactory, CrawlerFactory>();
        services.AddScoped<IChunkingStrategyFactory, ChunkingStrategyFactory>();

        // 유틸리티 서비스
        services.AddScoped<IParallelProcessingEngine, ParallelProcessingEngine>();
        services.AddSingleton<ILRUWebContentCache, LRUWebContentCache>();

        return services;
    }
}
```

## 📊 모니터링 및 메트릭

### 성능 메트릭 수집

```csharp
public class WebFluxMetrics
{
    private static readonly Counter CrawledPagesCount =
        Metrics.CreateCounter("webflux_crawled_pages_total", "Total crawled pages");

    private static readonly Histogram ChunkingDuration =
        Metrics.CreateHistogram("webflux_chunking_duration_seconds", "Chunking duration");

    private static readonly Gauge MemoryUsage =
        Metrics.CreateGauge("webflux_memory_usage_bytes", "Current memory usage");

    public void RecordCrawledPage(string strategy)
    {
        CrawledPagesCount.WithLabels(strategy).Inc();
    }

    public void RecordChunkingDuration(string strategy, double duration)
    {
        ChunkingDuration.WithLabels(strategy).Observe(duration);
    }
}
```

## 🔐 보안 고려사항

### 1. 안전한 웹 크롤링

```csharp
public class SecurityConfig
{
    public List<string> AllowedSchemes { get; set; } = new() { "https", "http" };
    public List<string> BlockedDomains { get; set; } = new();
    public int MaxRedirects { get; set; } = 5;
    public TimeSpan MaxRequestDuration { get; set; } = TimeSpan.FromMinutes(2);
    public long MaxContentLength { get; set; } = 50 * 1024 * 1024; // 50MB
}
```

### 2. 입력 검증

```csharp
public class UrlValidator
{
    public static bool IsValidUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            return false;

        // IP 주소 차단 (내부 네트워크 보호)
        if (IPAddress.TryParse(uri.Host, out var ip) && IsPrivateIP(ip))
            return false;

        return true;
    }
}
```

---

이 아키텍처 설계는 연구 문서의 인사이트를 바탕으로 실제 구현 가능한 Clean Architecture 기반의 확장 가능한 설계를 제공합니다. 다음 단계에서는 각 컴포넌트의 상세 설계 문서를 작성하겠습니다.