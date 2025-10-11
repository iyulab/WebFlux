# WebFlux SDK 인터페이스

> 확장 가능하고 AI 공급자 중립적인 인터페이스 설계 (Phase 1 구현)

## 설계 원칙

WebFlux SDK는 **인터페이스 제공자 패턴**을 따릅니다:

**WebFlux가 정의하는 인터페이스** (소비자가 구현):
- `ITextCompletionService`: LLM 텍스트 완성 서비스

**WebFlux가 제공하는 인터페이스**:
- `IWebContentProcessor`: 메인 처리 파이프라인
- `ICrawler`: 웹 크롤링 전략
- `IAiEnhancementService`: AI 콘텐츠 증강
- `IChunkingStrategy`: 청킹 전략
- `IEventPublisher`: 이벤트 시스템

## AI 서비스 인터페이스 (소비자 구현)

### ITextCompletionService

LLM 텍스트 완성을 위한 인터페이스입니다. 소비자가 OpenAI, Anthropic, Azure, Ollama 등으로 구현합니다.

```csharp
public interface ITextCompletionService
{
    /// <summary>
    /// 텍스트 완성을 수행합니다.
    /// </summary>
    Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스트리밍 텍스트 완성을 수행합니다.
    /// </summary>
    IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 서비스 상태 및 정보를 반환합니다.
    /// </summary>
    HealthInfo GetHealthInfo();
}

public class TextCompletionOptions
{
    public int? MaxTokens { get; set; } = 2000;
    public float? Temperature { get; set; } = 0.3f;
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}
```

**구현 예제** (OpenAI):

```csharp
public class OpenAiTextCompletionService : ITextCompletionService
{
    private readonly string _model;
    private readonly string _apiKey;

    public OpenAiTextCompletionService(string model, string apiKey)
    {
        _model = model;
        _apiKey = apiKey;
    }

    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // OpenAI API 호출 구현
        // ...
    }

    public HealthInfo GetHealthInfo()
    {
        return new HealthInfo
        {
            Status = "Healthy",
            Metadata = new Dictionary<string, object>
            {
                ["Provider"] = "OpenAI",
                ["Model"] = _model
            }
        };
    }
}
```

## WebFlux 핵심 인터페이스

### IWebContentProcessor

웹 콘텐츠 처리 파이프라인의 메인 인터페이스입니다.

```csharp
public interface IWebContentProcessor
{
    /// <summary>
    /// 단일 URL을 처리하여 청크를 생성합니다.
    /// </summary>
    Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
        string url,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 URL을 배치 처리합니다.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
        IEnumerable<string> urls,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 웹사이트 전체를 크롤링하여 처리합니다.
    /// </summary>
    IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HTML 문자열을 직접 처리합니다.
    /// </summary>
    Task<IReadOnlyList<WebContentChunk>> ProcessHtmlAsync(
        string htmlContent,
        string sourceUrl,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);
}
```

**사용 예제**:

```csharp
var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();

var chunkingOptions = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Paragraph,
    MaxChunkSize = 1000,
    MinChunkSize = 100
};

var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);

foreach (var chunk in chunks)
{
    Console.WriteLine($"청크 {chunk.ChunkIndex}: {chunk.Content.Length}자");
}
```

### ICrawler

웹 크롤링 전략 인터페이스입니다.

```csharp
public interface ICrawler : IDisposable
{
    /// <summary>
    /// 단일 URL을 크롤링합니다.
    /// </summary>
    Task<CrawlResult> CrawlAsync(
        string url,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 페이지를 크롤링합니다.
    /// </summary>
    IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
        string baseUrl,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);
}

public class CrawlResult
{
    public string Url { get; set; }
    public string FinalUrl { get; set; }
    public string Content { get; set; }             // 렌더링된 HTML
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public List<string> ImageUrls { get; set; }
    public List<string> DiscoveredLinks { get; set; }
}
```

**구현 크롤러**:
- `PlaywrightCrawler`: Chromium 기반 동적 렌더링 (Phase 1 구현)

### IAiEnhancementService

AI를 활용한 콘텐츠 증강 서비스입니다.

```csharp
public interface IAiEnhancementService
{
    /// <summary>
    /// 콘텐츠를 요약합니다.
    /// </summary>
    Task<string> SummarizeAsync(
        string content,
        SummaryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 메타데이터를 추출합니다.
    /// </summary>
    Task<AiMetadata> ExtractMetadataAsync(
        string content,
        MetadataExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 통합 증강을 수행합니다 (요약 + 메타데이터).
    /// </summary>
    Task<EnhancedContent> EnhanceAsync(
        string content,
        EnhancementOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 서비스 가용성을 확인합니다.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

public class EnhancedContent
{
    public string OriginalContent { get; set; }
    public string? Summary { get; set; }
    public string? RewrittenContent { get; set; }
    public AiMetadata Metadata { get; set; }
    public long ProcessingTimeMs { get; set; }
}

public class AiMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public IReadOnlyList<string> Keywords { get; set; }
    public IReadOnlyList<string> Topics { get; set; }
    public string? MainTopic { get; set; }
    public string? Sentiment { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? Language { get; set; }
}
```

**구현 서비스**:
- `BasicAiEnhancementService`: ITextCompletionService 기반 (Phase 1 구현)

### IChunkingStrategy

청킹 전략 인터페이스입니다.

```csharp
public interface IChunkingStrategy
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// 콘텐츠를 청킹합니다.
    /// </summary>
    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions options,
        CancellationToken cancellationToken = default);
}

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
```

**구현 전략** (Phase 1):
- `ParagraphChunkingStrategy`: 단락 경계 기준
- `FixedSizeChunkingStrategy`: 고정 크기

### IEventPublisher

이벤트 발행 및 구독 시스템입니다.

```csharp
public interface IEventPublisher
{
    /// <summary>
    /// 이벤트를 발행합니다.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : ProcessingEvent;

    /// <summary>
    /// 이벤트를 구독합니다.
    /// </summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : ProcessingEvent;
}

public abstract class ProcessingEvent
{
    public abstract string EventType { get; }
    public string Message { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class ProcessingProgressEvent : ProcessingEvent
{
    public override string EventType => "ProcessingProgress";
    public int ProcessedCount { get; set; }
    public string CurrentStage { get; set; }
}
```

**사용 예제**:

```csharp
eventPublisher.Subscribe<ProcessingEvent>(evt =>
{
    if (evt.EventType == "ProcessingProgress")
    {
        var progressEvt = evt as ProcessingProgressEvent;
        Console.WriteLine($"→ {progressEvt.CurrentStage}: {progressEvt.ProcessedCount} 처리됨");
    }
    return Task.CompletedTask;
});
```

## 통합 사용 예제

### 기본 설정 (SimpleOpenAITest 패턴)

```csharp
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Extensions;

var services = new ServiceCollection();

// 로깅 설정
services.AddLogging(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.IncludeScopes = false;
        options.TimestampFormat = "";
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

// WebFlux SDK 등록
services.AddWebFlux(config =>
{
    config.Crawling.Strategy = "Dynamic";
    config.Crawling.DefaultTimeoutSeconds = 30;
    config.Crawling.DefaultDelayMs = 500;

    config.AiEnhancement.Enabled = true;
    config.AiEnhancement.EnableSummary = true;
    config.AiEnhancement.EnableMetadata = true;
    config.AiEnhancement.EnableParallelProcessing = true;

    config.Chunking.DefaultStrategy = "Paragraph";
    config.Chunking.MaxChunkSize = 1000;
    config.Chunking.MinChunkSize = 100;
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
        Console.WriteLine($"  → {progressEvt.CurrentStage}: {progressEvt.ProcessedCount} 처리됨");
    }
    return Task.CompletedTask;
});

// 청킹 옵션 설정
var chunkingOptions = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Paragraph,
    MaxChunkSize = 1000,
    MinChunkSize = 100,
    ChunkOverlap = 50
};

// URL 처리
var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);

// 결과 출력
Console.WriteLine($"✓ Completed: {chunks.Count} chunks generated");

// 메타데이터 확인
if (chunks.Count > 0)
{
    var firstChunk = chunks[0];

    var aiSummary = firstChunk.AdditionalMetadata.ContainsKey("ai_summary")
        ? firstChunk.AdditionalMetadata["ai_summary"]?.ToString()
        : null;

    if (!string.IsNullOrEmpty(aiSummary))
    {
        Console.WriteLine($"→ Summary: {aiSummary}");
    }
}
```

### AI 증강 없이 사용

```csharp
// ITextCompletionService 등록 생략
services.AddWebFlux(config =>
{
    config.Crawling.Strategy = "Dynamic";
    config.AiEnhancement.Enabled = false;  // AI 증강 비활성화
    config.Chunking.DefaultStrategy = "Paragraph";
});

// AI 증강 서비스 등록 안함
// services.AddWebFluxAIEnhancement(); // 호출하지 않음

var serviceProvider = services.BuildServiceProvider();
var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();

// 파이프라인 실행: Crawling → Extraction → Chunking
var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);
```

## 향후 계획 인터페이스

다음 인터페이스들은 향후 Phase에서 구현 예정입니다:

### Phase 3: Multimodal

**IImageToTextService** - 이미지-텍스트 변환
```csharp
public interface IImageToTextService
{
    Task<ImageToTextResult> ExtractTextFromWebImageAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

### Phase 2+: Advanced Features

**IEmbeddingService** - 텍스트 임베딩 (Semantic 청킹용)
```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateAsync(
        string text,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default);

    int GetDimensions();
}
```

**ICacheManager** - 캐싱 시스템
**IPerformanceOptimizer** - 성능 최적화
**IQualityAnalyzer** - 품질 분석

## 참고 문서

- [PIPELINE_DESIGN.md](./PIPELINE_DESIGN.md) - 파이프라인 설계
- [ARCHITECTURE.md](./ARCHITECTURE.md) - 시스템 아키텍처
- [CHUNKING_STRATEGIES.md](./CHUNKING_STRATEGIES.md) - 청킹 전략
- [TUTORIAL.md](./TUTORIAL.md) - 사용 가이드
