# WebFlux SDK 아키텍처

> RAG 최적화 웹 콘텐츠 처리를 위한 Clean Architecture 기반 SDK (Phase 1 구현)

## 아키텍처 개요

WebFlux는 **인터페이스 제공자(Interface Provider)** 패턴을 채택하여, 핵심 기능의 인터페이스를 정의하고 소비 애플리케이션이 구현체를 선택할 수 있도록 설계되었습니다.

### 핵심 설계 원칙

1. **의존성 역전 원칙**: 구체적 구현체가 아닌 추상화에 의존
2. **단일 책임 원칙**: 각 컴포넌트는 하나의 명확한 책임
3. **개방-폐쇄 원칙**: 확장에는 열려있고 수정에는 닫혀있음
4. **AI 공급자 중립**: 특정 AI 서비스에 종속되지 않음

## 레이어 구조

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

## 핵심 인터페이스

### 1. AI 서비스 인터페이스 (소비자 구현)

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

        IAsyncEnumerable<string> CompleteStreamAsync(
            string prompt,
            TextCompletionOptions? options = null,
            CancellationToken cancellationToken = default);

        HealthInfo GetHealthInfo();
    }
}
```

**구현 예제**:
- `OpenAiTextCompletionService`: SimpleOpenAITest 예제 참조

### 2. WebFlux 핵심 인터페이스 (WebFlux 구현)

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 웹 콘텐츠 처리의 메인 인터페이스
    /// Crawling → Extraction → AI Enhancement → Chunking 파이프라인 오케스트레이션
    /// </summary>
    public interface IWebContentProcessor
    {
        Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
            string url,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
            IEnumerable<string> urls,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
            string startUrl,
            CrawlOptions? crawlOptions = null,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<WebContentChunk>> ProcessHtmlAsync(
            string htmlContent,
            string sourceUrl,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 웹 크롤링 인터페이스
    /// Phase 1: PlaywrightCrawler (동적 렌더링)
    /// </summary>
    public interface ICrawler : IDisposable
    {
        Task<CrawlResult> CrawlAsync(
            string url,
            CrawlOptions? options = null,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
            string baseUrl,
            CrawlOptions? options = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// AI 증강 서비스 인터페이스
    /// Phase 1: BasicAiEnhancementService (요약 + 메타데이터)
    /// </summary>
    public interface IAiEnhancementService
    {
        Task<string> SummarizeAsync(
            string content,
            SummaryOptions? options = null,
            CancellationToken cancellationToken = default);

        Task<AiMetadata> ExtractMetadataAsync(
            string content,
            MetadataExtractionOptions? options = null,
            CancellationToken cancellationToken = default);

        Task<EnhancedContent> EnhanceAsync(
            string content,
            EnhancementOptions? options = null,
            CancellationToken cancellationToken = default);

        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 청킹 전략 인터페이스
    /// Phase 1: Paragraph, FixedSize
    /// </summary>
    public interface IChunkingStrategy
    {
        string Name { get; }
        string Description { get; }

        Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
            ExtractedContent content,
            ChunkingOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 이벤트 발행 시스템
    /// </summary>
    public interface IEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ProcessingEvent;

        void Subscribe<TEvent>(Func<TEvent, Task> handler)
            where TEvent : ProcessingEvent;
    }
}
```

## 도메인 모델

### 핵심 데이터 모델

```csharp
namespace WebFlux.Core.Models
{
    /// <summary>
    /// 웹 콘텐츠 청크 - RAG 시스템에 최적화된 데이터 단위
    /// </summary>
    public class WebContentChunk
    {
        public string ChunkId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; }
        public string SourceUrl { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public Dictionary<string, object> AdditionalMetadata { get; set; }
    }

    /// <summary>
    /// 크롤링 결과
    /// </summary>
    public class CrawlResult
    {
        public string Url { get; set; }
        public string FinalUrl { get; set; }
        public string Content { get; set; }
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public List<string> ImageUrls { get; set; }
        public List<string> DiscoveredLinks { get; set; }
    }

    /// <summary>
    /// 청킹 옵션
    /// </summary>
    public class ChunkingOptions
    {
        public ChunkingStrategyType Strategy { get; set; } = ChunkingStrategyType.Paragraph;
        public int MaxChunkSize { get; set; } = 1000;
        public int MinChunkSize { get; set; } = 100;
        public int ChunkOverlap { get; set; } = 50;
    }

    public enum ChunkingStrategyType
    {
        FixedSize,
        Paragraph,
        Smart,          // Phase 2 계획
        Semantic,       // Phase 2 계획
        Auto
    }
}
```

## 처리 파이프라인 아키텍처

### Phase 1: 4단계 파이프라인

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Crawling   │───▶│ Extraction  │───▶│AI Enhancement│───▶│  Chunking   │
│ (Playwright)│    │   (HTML)    │    │  (Optional) │    │(Paragraph)  │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │                   │
       ▼                   ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ CrawlResult │    │  Extracted  │    │  Enhanced   │    │WebContentChunk│
│             │    │   Content   │    │   Content   │    │   (List)    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

### 스트리밍 처리 (IAsyncEnumerable)

```csharp
// WebContentProcessor 내부 파이프라인 구조
private async IAsyncEnumerable<WebContentChunk> ProcessAsync(
    CrawlOptions crawlOptions,
    ChunkingOptions chunkingOptions,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // 1. Crawling
    var crawlResults = CrawlWebContent(crawlOptions, cancellationToken);

    // 2. Extraction (병렬 처리)
    var extractionResults = ExtractContent(crawlResults, cancellationToken);

    // 3. AI Enhancement (선택적, 병렬 처리)
    var enhancedResults = EnhanceContent(extractionResults, cancellationToken);

    // 4. Chunking (스트리밍 반환)
    await foreach (var chunk in ChunkContent(enhancedResults, chunkingOptions, cancellationToken))
    {
        yield return chunk;
    }
}
```

## 의존성 주입 구성

### ServiceCollection 확장 (Phase 1)

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebFlux(
        this IServiceCollection services,
        Action<WebFluxConfiguration>? configureOptions = null)
    {
        // 설정
        var config = new WebFluxConfiguration();
        configureOptions?.Invoke(config);
        services.AddSingleton(config);

        // 핵심 서비스
        services.AddScoped<IWebContentProcessor, WebContentProcessor>();
        services.AddScoped<IEventPublisher, BasicEventPublisher>();

        // 크롤러 (Phase 1: Playwright만 구현)
        services.AddScoped<ICrawler, PlaywrightCrawler>();

        // 추출기
        services.AddScoped<IContentExtractor, HtmlExtractor>();

        // 청킹 전략 (Phase 1: Paragraph, FixedSize)
        services.AddScoped<IChunkingStrategy, ParagraphChunkingStrategy>();
        services.AddScoped<IChunkingStrategy, FixedSizeChunkingStrategy>();
        services.AddScoped<IChunkingStrategyFactory, ChunkingStrategyFactory>();

        return services;
    }

    public static IServiceCollection AddWebFluxAIEnhancement(this IServiceCollection services)
    {
        // AI 증강 서비스 (ITextCompletionService 필요)
        services.AddScoped<IAiEnhancementService, BasicAiEnhancementService>();
        return services;
    }
}
```

## 사용 예제

### 기본 설정 (SimpleOpenAITest 패턴)

```csharp
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Extensions;

var services = new ServiceCollection();

// 로깅
services.AddLogging(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.IncludeScopes = false;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

// WebFlux SDK 등록
services.AddWebFlux(config =>
{
    config.Crawling.Strategy = "Dynamic";
    config.Crawling.DefaultTimeoutSeconds = 30;

    config.AiEnhancement.Enabled = true;
    config.AiEnhancement.EnableSummary = true;
    config.AiEnhancement.EnableMetadata = true;

    config.Chunking.DefaultStrategy = "Paragraph";
    config.Chunking.MaxChunkSize = 1000;
});

// AI 서비스 구현체 등록 (소비자가 제공)
services.AddSingleton<ITextCompletionService>(sp =>
    new OpenAiTextCompletionService(model, apiKey));

// AI 증강 서비스 등록
services.AddWebFluxAIEnhancement();

var serviceProvider = services.BuildServiceProvider();
```

### URL 처리

```csharp
var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();
var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();

// 이벤트 구독
eventPublisher.Subscribe<ProcessingEvent>(evt =>
{
    if (evt.EventType == "ProcessingProgress")
    {
        var progressEvt = evt as ProcessingProgressEvent;
        Console.WriteLine($"  → {progressEvt.CurrentStage}: {progressEvt.ProcessedCount} processed");
    }
    return Task.CompletedTask;
});

// 청킹 옵션
var chunkingOptions = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Paragraph,
    MaxChunkSize = 1000,
    MinChunkSize = 100,
    ChunkOverlap = 50
};

// URL 처리
var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);

Console.WriteLine($"✓ Completed: {chunks.Count} chunks generated");

// 메타데이터 확인
if (chunks.Count > 0)
{
    var aiSummary = chunks[0].AdditionalMetadata.ContainsKey("ai_summary")
        ? chunks[0].AdditionalMetadata["ai_summary"]?.ToString()
        : null;

    if (!string.IsNullOrEmpty(aiSummary))
    {
        Console.WriteLine($"→ Summary: {aiSummary}");
    }
}
```

## 병렬 처리 최적화

### Channels 기반 파이프라인

```csharp
// ExtractContent 내부 구현 (병렬 처리)
private async IAsyncEnumerable<ExtractedContent> ExtractContent(
    IAsyncEnumerable<CrawlResult> crawlResults,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var semaphore = new SemaphoreSlim(_config.Performance.MaxDegreeOfParallelism);
    var channel = Channel.CreateUnbounded<ExtractedContent>();

    var processingTask = Task.Run(async () =>
    {
        await foreach (var crawlResult in crawlResults.WithCancellation(cancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    var extracted = await _extractor.ExtractAutoAsync(crawlResult.Content);
                    await channel.Writer.WriteAsync(extracted, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
        }
    }, cancellationToken);

    await foreach (var extracted in channel.Reader.ReadAllAsync(cancellationToken))
    {
        yield return extracted;
    }
}
```

## 향후 계획

### Phase 2: Advanced Chunking
- Smart 전략: 의미 경계 인식
- Semantic 전략: 임베딩 기반 청킹

### Phase 3: Multimodal
- IImageToTextService 인터페이스
- 이미지-텍스트 변환 통합

### Phase 4: Performance
- 병렬 처리 엔진 (CPU 코어별 동적 스케일링)
- 지능형 캐싱 시스템
- 메모리 백프레셔 제어
- 적응형 성능 튜닝

## 참고 문서

- [INTERFACES.md](./INTERFACES.md) - 인터페이스 상세 설명
- [PIPELINE_DESIGN.md](./PIPELINE_DESIGN.md) - 파이프라인 설계
- [CHUNKING_STRATEGIES.md](./CHUNKING_STRATEGIES.md) - 청킹 전략

