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
    Task<double[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
```

**구현 예제**:
```csharp
public class OpenAiEmbeddingService : ITextEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public async Task<double[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // OpenAI API 호출
        var response = await _httpClient.PostAsync(...);
        return ParseEmbedding(response);
    }
}
```

### ITextCompletionService (선택)

콘텐츠 재구성 기능 사용 시 필요합니다.

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
    Task<string> ConvertAsync(
        byte[] imageData,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

## SDK-Provided Interfaces

WebFlux가 제공하는 핵심 인터페이스입니다.

### IWebContentProcessor

웹 콘텐츠 처리 파이프라인의 메인 인터페이스입니다.

```csharp
public interface IWebContentProcessor
{
    // 단일 URL 처리
    Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
        string url,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    // 웹사이트 크롤링 및 처리 (스트리밍)
    IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    // 진행 상황 추적
    IAsyncEnumerable<ProcessingResult<IReadOnlyList<WebContentChunk>>> ProcessWithProgressAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);
}
```

### ICrawler

웹 크롤링 인터페이스입니다.

```csharp
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

    Task<RobotsTxtInfo> GetRobotsTxtAsync(
        string baseUrl,
        string userAgent,
        CancellationToken cancellationToken = default);

    Task<bool> IsUrlAllowedAsync(
        string url,
        string userAgent,
        CancellationToken cancellationToken = default);
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
