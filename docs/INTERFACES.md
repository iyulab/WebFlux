# Interfaces

WebFlux SDK의 핵심 인터페이스와 구현 가이드입니다.

## Design Pattern

WebFlux는 **Interface Provider 패턴**을 사용합니다:
- SDK가 인터페이스를 정의
- 소비자가 AI 서비스 구현체를 제공
- AI 공급자 중립적 설계

## Consumer-Provided Interfaces

소비자가 구현해야 하는 AI 서비스 인터페이스입니다.

### ITextEmbeddingService (필수)

```csharp
public interface ITextEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    int MaxTokens { get; }
    int EmbeddingDimension { get; }
}
```

**구현 예제**:
```csharp
public class OpenAiEmbeddingService : ITextEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // OpenAI API 호출
        var response = await _httpClient.PostAsync(...);
        return ParseEmbedding(response);
    }

    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct = default)
    {
        var tasks = texts.Select(t => GetEmbeddingAsync(t, ct));
        return await Task.WhenAll(tasks);
    }

    public int MaxTokens => 8191;
    public int EmbeddingDimension => 1536;
}
```

### ITextCompletionService (선택)

콘텐츠 재구성 및 AI 기반 메타데이터 추출 기능 사용 시 필요합니다.

```csharp
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
}
```

### IImageToTextService (선택)

멀티모달 처리 사용 시 필요합니다.

```csharp
public interface IImageToTextService
{
    Task<string> ConvertImageToTextAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<string> ConvertImageToTextAsync(
        byte[] imageBytes,
        string mimeType,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ConvertImagesBatchAsync(
        IEnumerable<string> imageUrls,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<string> ExtractTextFromImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetSupportedImageFormats();

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
```

### IWebMetadataExtractor (선택)

AI 기반 웹 메타데이터 추출 기능 사용 시 필요합니다. ITextCompletionService를 사용하여 웹 콘텐츠에서 풍부한 메타데이터를 추출합니다.

```csharp
public interface IWebMetadataExtractor
{
    Task<EnrichedMetadata> ExtractAsync(
        string content,
        string url,
        HtmlMetadataSnapshot? htmlMetadata = null,
        MetadataSchema schema = MetadataSchema.General,
        string? customPrompt = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrichedMetadata>> ExtractBatchAsync(
        IEnumerable<(string content, string url, HtmlMetadataSnapshot? htmlMetadata)> items,
        MetadataSchema schema = MetadataSchema.General,
        string? customPrompt = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<MetadataSchema> GetSupportedSchemas();
    string GetSchemaDescription(MetadataSchema schema);
}

// 메타데이터 스키마 타입
public enum MetadataSchema
{
    General,        // 일반 웹 콘텐츠
    TechnicalDoc,   // 기술 문서 (라이브러리, 프레임워크 정보)
    ProductManual,  // 제품 페이지 (가격, 스펙 정보)
    Article,        // 블로그/뉴스 (작성자, 태그 정보)
    Custom          // 사용자 정의 (customPrompt 필수)
}
```

### IEnrichedChunk (v0.1.7)

FluxIndex와의 연동을 위한 청크 인터페이스입니다.

```csharp
public interface IEnrichedChunk
{
    string Id { get; }
    string Content { get; }
    IReadOnlyDictionary<string, object>? Metadata { get; }
    ISourceMetadata? Source { get; }
}
```

### ISourceMetadata (v0.1.7)

청크의 출처 정보를 제공하는 인터페이스입니다.

```csharp
public interface ISourceMetadata
{
    string? Url { get; }
    string? Title { get; }
    DateTimeOffset? CreatedAt { get; }
    string? Author { get; }
}
```

### IWebDocumentMetadataExtractor (v0.1.7)

웹 문서 메타데이터 추출 인터페이스입니다.

```csharp
public interface IWebDocumentMetadataExtractor
{
    Task<WebDocumentMetadata> ExtractAsync(
        string html,
        string url,
        CancellationToken cancellationToken = default);
}
```

### ICrawlProgressReporter (v0.1.7)

크롤링 진행 상황 리포팅 인터페이스입니다.

```csharp
public interface ICrawlProgressReporter
{
    IAsyncEnumerable<CrawlProgress> ReportProgressAsync(
        CancellationToken cancellationToken = default);
}
```

## SDK-Provided Interfaces

WebFlux가 제공하는 핵심 인터페이스입니다.

### IWebContentProcessor

웹 콘텐츠 처리 파이프라인의 메인 인터페이스입니다. `IContentExtractService`와 `IContentChunkService`를 모두 상속하는 파사드입니다.

```csharp
// IContentChunkService에서 상속
public interface IWebContentProcessor : IContentExtractService, IContentChunkService
{
    // 사용 가능한 청킹 전략 목록
    IReadOnlyList<string> GetAvailableChunkingStrategies();
}

// IContentChunkService — 청킹까지 처리
public interface IContentChunkService
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

// IContentExtractService — 청킹 없는 경량 추출
public interface IContentExtractService
{
    Task<ProcessingResult<ExtractedContent>> ExtractContentAsync(
        string url,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<BatchExtractResult> ExtractBatchAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ProcessingResult<ExtractedContent>> ExtractBatchStreamAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

### ICrawler

웹 크롤링 인터페이스입니다.

```csharp
public interface ICrawler
{
    Task<CrawlResult> CrawlAsync(
        string url,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
        string startUrl,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<CrawlResult> CrawlSitemapAsync(
        string sitemapUrl,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<RobotsTxtInfo> GetRobotsTxtAsync(
        string baseUrl,
        string userAgent,
        CancellationToken cancellationToken = default);

    Task<bool> IsUrlAllowedAsync(string url, string userAgent);

    IReadOnlyList<string> ExtractLinks(string htmlContent, string baseUrl);

    CrawlStatistics GetStatistics();
}
```

### IChunkingStrategy

청킹 전략 인터페이스입니다.

```csharp
public interface IChunkingStrategy
{
    string Name { get; }
    string Description { get; }

    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions options,
        CancellationToken cancellationToken = default);
}
```

### IEventPublisher (v0.5.0)

파이프라인 이벤트 구독 인터페이스입니다. `AddWebFlux()` 호출 시 Singleton으로 자동 등록됩니다.

```csharp
public interface IEventPublisher
{
    // 이벤트 발행 (비동기)
    Task PublishAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default);

    // 이벤트 발행 (동기)
    void Publish(ProcessingEvent processingEvent);

    // 특정 이벤트 타입 구독 (비동기 핸들러)
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : ProcessingEvent;

    // 특정 이벤트 타입 구독 (동기 핸들러)
    IDisposable Subscribe<T>(Action<T> handler) where T : ProcessingEvent;

    // 모든 이벤트 구독
    IDisposable SubscribeAll(Func<ProcessingEvent, Task> handler);

    // 발행 통계 조회
    EventPublishingStatistics GetStatistics();
}
```

이벤트 타입은 `WebFlux.Core.Models.Events` 네임스페이스에 위치합니다.

| 카테고리 | 이벤트 |
|---------|--------|
| Pipeline | `ProcessingStartedEvent`, `ProcessingProgressEvent`, `ProcessingCompletedEvent`, `ProcessingFailedEvent` |
| Crawling | `CrawlingStartedEvent`, `CrawlingCompletedEvent`, `PageCrawledEvent`, `UrlProcessingStartedEvent`, `UrlProcessedEvent`, `UrlProcessingFailedEvent` |
| Extraction | `ContentExtractionStartedEvent`, `ContentExtractionCompletedEvent`, `ContentExtractionFailedEvent`, `ImageProcessedEvent` |
| Chunking | `ChunkingStartedEvent`, `ChunkingCompletedEvent`, `ChunkGeneratedEvent` |
| Monitoring | `ErrorOccurredEvent`, `PerformanceMetricsEvent` |

모든 이벤트는 `ProcessingEvent` (기본 클래스, `EventId`, `EventType`, `Timestamp`, `Severity`, `CorrelationId` 포함)를 상속합니다.

**사용 예시**:

```csharp
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models.Events;

var publisher = provider.GetRequiredService<IEventPublisher>();

// 특정 이벤트 구독
using var s1 = publisher.Subscribe<PageCrawledEvent>(async e =>
{
    Console.WriteLine($"Crawled {e.Url} [{e.StatusCode}] in {e.ProcessingTimeMs}ms");
});

using var s2 = publisher.Subscribe<ChunkGeneratedEvent>(e =>
{
    Console.WriteLine($"Chunk #{e.SequenceNumber} ({e.ChunkSize} tokens) from {e.SourceUrl}");
});

// 모든 이벤트 구독
using var sAll = publisher.SubscribeAll(async e =>
{
    await logger.LogEventAsync(e.EventType, e);
});
```

> **v0.5.0 Breaking Change**: 이전 버전의 `*EventV2` 클래스와 `Services` 계층 내부 이벤트 정의가 제거되었습니다.
> 모든 이벤트 클래스는 `WebFlux.Core.Models.Events` 네임스페이스로 일원화되었습니다.
> 소비자는 `using WebFlux.Core.Models.Events;`를 추가해야 합니다.

---

## Data Models

### WebContentChunk

RAG 시스템에서 사용하는 청크 데이터입니다.

```csharp
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
```

### EnrichedMetadata

HTML 메타데이터와 AI 추출 메타데이터를 통합한 풍부한 메타데이터 모델입니다.

```csharp
public class EnrichedMetadata
{
    // 기본 메타데이터 (HTML 우선, AI로 보완)
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset? PublishedDate { get; set; }
    public string? Language { get; set; }

    // AI 추출 메타데이터
    public IReadOnlyList<string> Topics { get; set; }
    public IReadOnlyList<string> Keywords { get; set; }
    public string? ContentType { get; set; }        // "article", "documentation", etc.
    public string? SiteStructure { get; set; }      // "blog", "documentation", etc.

    // 웹 소스 정보
    public string Url { get; set; }
    public string Domain { get; set; }

    // 스키마별 확장 데이터
    public Dictionary<string, object> SchemaSpecificData { get; set; }

    // 메타데이터 출처 추적
    public MetadataSource Source { get; set; }                      // Html, AI, Merged, User
    public Dictionary<string, MetadataSource> FieldSources { get; set; }

    // 신뢰도 및 품질
    public float OverallConfidence { get; set; }
    public Dictionary<string, float> FieldConfidence { get; set; }

    // HTML 메타데이터 원본
    public HtmlMetadataSnapshot? HtmlMetadata { get; set; }

    public DateTimeOffset ExtractedAt { get; set; }
}
```

### CrawlOptions

크롤링 옵션입니다.

```csharp
public class CrawlOptions
{
    public int MaxDepth { get; set; } = 3;
    public int MaxPages { get; set; } = 100;
    public bool RespectRobotsTxt { get; set; } = true;
    public string UserAgent { get; set; } = "WebFlux/1.0";
    public int DelayMs { get; set; } = 500;
    public int TimeoutSeconds { get; set; } = 30;
}
```

### ChunkingOptions

청킹 옵션입니다.

```csharp
public class ChunkingOptions
{
    public string Strategy { get; set; } = "Auto";
    public int MaxChunkSize { get; set; } = 512;
    public int MinChunkSize { get; set; } = 100;
    public int OverlapSize { get; set; } = 64;
}
```

## Setup Example

### 기본 설정

```csharp
using Microsoft.Extensions.DependencyInjection;
using WebFlux;

var services = new ServiceCollection();

// AI 서비스 구현 등록
services.AddScoped<ITextEmbeddingService, YourEmbeddingService>();
services.AddScoped<ITextCompletionService, YourLLMService>(); // Optional

// WebFlux 등록
services.AddWebFlux();

var provider = services.BuildServiceProvider();
```

### URL 처리

```csharp
var processor = provider.GetRequiredService<IWebContentProcessor>();

var chunks = await processor.ProcessUrlAsync("https://example.com");

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content}");
}
```

### 웹사이트 크롤링 (스트리밍)

```csharp
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 100,
    RespectRobotsTxt = true
};

await foreach (var chunk in processor.ProcessWebsiteAsync(url, options))
{
    // 청크 생성 즉시 벡터 DB 저장
    await StoreChunkAsync(chunk);
}
```

### 진행 상황 추적

```csharp
await foreach (var result in processor.ProcessWithProgressAsync(url))
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"✓ Processed: {result.Url}");
        await StoreChunksAsync(result.Result);
    }
    else
    {
        Console.WriteLine($"✗ Failed: {result.Url} - {result.Error}");
    }
}
```

## Custom Implementation

### 커스텀 청킹 전략

```csharp
public class CustomChunkingStrategy : IChunkingStrategy
{
    public string Name => "Custom";
    public string Description => "Custom chunking logic";

    public async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions options,
        CancellationToken ct = default)
    {
        // 커스텀 청킹 로직 구현
        var chunks = new List<WebContentChunk>();
        // ...
        return chunks;
    }
}

// 등록
services.AddScoped<IChunkingStrategy, CustomChunkingStrategy>();
```

## Error Handling

```csharp
try
{
    var chunks = await processor.ProcessUrlAsync(url);
}
catch (CrawlException ex)
{
    // 크롤링 실패
    Console.WriteLine($"Crawl failed: {ex.Message}");
}
catch (ExtractionException ex)
{
    // 추출 실패
    Console.WriteLine($"Extraction failed: {ex.Message}");
}
catch (ChunkingException ex)
{
    // 청킹 실패
    Console.WriteLine($"Chunking failed: {ex.Message}");
}
```

## References

- [ARCHITECTURE.md](./ARCHITECTURE.md) - 시스템 설계
- [CHUNKING_STRATEGIES.md](./CHUNKING_STRATEGIES.md) - 청킹 전략 상세
