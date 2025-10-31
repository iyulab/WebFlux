# WebFlux 튜토리얼

실전 예제로 배우는 WebFlux SDK 완벽 가이드

## 목차

1. [설치](#설치)
2. [첫 번째 프로젝트](#첫-번째-프로젝트)
3. [기본 사용법](#기본-사용법)
4. [핵심 인터페이스](#핵심-인터페이스)
   - [ITextEmbeddingService](#itextembeddingservice-필수)
   - [ITextCompletionService](#itextcompletionservice-선택적)
   - [IImageToTextService](#iimagetotextservice-선택적)
   - [IWebContentProcessor](#iwebcontentprocessor)
   - [IChunkingStrategy](#ichunkingstrategy)
   - [IProgressReporter](#iprogressreporter)
   - [IEventPublisher](#ieventpublisher)
5. [고급 사용법](#고급-사용법)
6. [실전 시나리오](#실전-시나리오)
7. [문제 해결](#문제-해결)

---

## 설치

### 요구사항

- .NET 8 이상 또는 .NET 9
- AI 서비스 (OpenAI, Azure OpenAI, Anthropic 등)

### NuGet 패키지 설치

```bash
dotnet add package WebFlux
```

### AI 서비스 준비

WebFlux는 임베딩 생성을 위한 AI 서비스가 필요합니다. 지원하는 서비스:

- OpenAI (GPT-4, GPT-3.5, text-embedding-3-small/large)
- Azure OpenAI
- Anthropic Claude
- Local models (Ollama, LM Studio 등)
- Custom implementations

---

## 첫 번째 프로젝트

### 1단계: 프로젝트 생성

```bash
mkdir MyWebFluxApp
cd MyWebFluxApp
dotnet new console
dotnet add package WebFlux
dotnet add package OpenAI  # 또는 선호하는 AI SDK
```

### 2단계: AI 서비스 구현

OpenAI를 사용하는 경우:

```csharp
using OpenAI;
using OpenAI.Embeddings;
using WebFlux.Core.Interfaces;

public class OpenAIEmbeddingService : ITextEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public async Task<double[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken);
        return response.Value.ToFloats().Select(f => (double)f).ToArray();
    }
}
```

### 3단계: WebFlux 설정 및 실행

```csharp
using Microsoft.Extensions.DependencyInjection;
using WebFlux;

var services = new ServiceCollection();

// AI 서비스 등록
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
services.AddScoped<ITextEmbeddingService>(
    sp => new OpenAIEmbeddingService(apiKey));

// WebFlux 등록
services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// URL 처리
var chunks = await processor.ProcessUrlAsync("https://example.com");

foreach (var chunk in chunks)
{
    Console.WriteLine($"청크 {chunk.ChunkIndex}:");
    Console.WriteLine($"  내용: {chunk.Content.Substring(0, Math.Min(100, chunk.Content.Length))}...");
    Console.WriteLine($"  길이: {chunk.Content.Length} 문자");
    Console.WriteLine();
}
```

---

## 기본 사용법

### 단일 URL 처리

```csharp
var processor = provider.GetRequiredService<IWebContentProcessor>();

// 기본 옵션으로 처리
var chunks = await processor.ProcessUrlAsync("https://docs.microsoft.com");

Console.WriteLine($"생성된 청크 수: {chunks.Count}");
```

### 청킹 전략 선택

```csharp
// Auto 전략 (권장 - 자동 선택)
var autoChunks = await processor.ProcessUrlAsync(
    url,
    new ChunkingOptions { Strategy = "Auto" }
);

// Smart 전략 (HTML 구조 기반)
var smartChunks = await processor.ProcessUrlAsync(
    url,
    new ChunkingOptions { Strategy = "Smart" }
);

// Semantic 전략 (의미 기반 - 임베딩 사용)
var semanticChunks = await processor.ProcessUrlAsync(
    url,
    new ChunkingOptions { Strategy = "Semantic" }
);
```

### 청크 크기 조정

```csharp
var options = new ChunkingOptions
{
    Strategy = "Auto",
    MaxChunkSize = 1000,    // 최대 1000 토큰
    MinChunkSize = 200,     // 최소 200 토큰
    OverlapSize = 100       // 100 토큰 오버랩
};

var chunks = await processor.ProcessUrlAsync(url, options);
```

### 크롤링 옵션 설정

```csharp
var crawlOptions = new CrawlOptions
{
    MaxDepth = 2,                    // 최대 2단계 깊이
    MaxPages = 50,                   // 최대 50페이지
    RespectRobotsTxt = true,        // robots.txt 준수
    UserAgent = "MyApp/1.0",        // User-Agent 설정
    DelayMs = 1000,                 // 요청 간 1초 대기
    TimeoutSeconds = 30             // 30초 타임아웃
};
```

---

## 핵심 인터페이스

WebFlux는 **Interface Provider** 패턴을 사용합니다. 라이브러리는 인터페이스를 정의하고, 소비 애플리케이션이 구현체를 제공합니다.

### 필수 AI 서비스 인터페이스

#### ITextEmbeddingService (필수)

텍스트를 벡터 임베딩으로 변환하는 서비스입니다. Semantic 청킹 전략에 필수입니다.

**인터페이스 정의:**
```csharp
public interface ITextEmbeddingService
{
    Task<double[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<double[]>> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
    int MaxTokens { get; }
    int EmbeddingDimension { get; }
}
```

**OpenAI 구현 예제:**
```csharp
using OpenAI;
using OpenAI.Embeddings;

public class OpenAIEmbeddingService : ITextEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public async Task<double[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken);
        return response.Value.ToFloats().Select(f => (double)f).ToArray();
    }

    public async Task<IReadOnlyList<double[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var tasks = texts.Select(t => GetEmbeddingAsync(t, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    public int MaxTokens => 8191;
    public int EmbeddingDimension => 1536;
}
```

**서비스 등록:**
```csharp
services.AddScoped<ITextEmbeddingService>(sp =>
    new OpenAIEmbeddingService(Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
```

---

#### ITextCompletionService (선택적)

LLM 텍스트 완성 서비스입니다. 멀티모달 처리 및 콘텐츠 재구성에 사용됩니다.

**인터페이스 정의:**
```csharp
public interface ITextCompletionService
{
    Task<string> CompleteAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> CompleteStreamAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> CompleteBatchAsync(IEnumerable<string> prompts, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    ServiceHealthInfo GetHealthInfo();
}
```

**OpenAI GPT-4 구현 예제:**
```csharp
using OpenAI.Chat;

public class OpenAICompletionService : ITextCompletionService
{
    private readonly ChatClient _client;

    public OpenAICompletionService(string apiKey, string model = "gpt-4")
    {
        _client = new ChatClient(model, apiKey);
    }

    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new[] { new ChatMessage(ChatRole.User, prompt) };
        var response = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new[] { new ChatMessage(ChatRole.User, prompt) };
        await foreach (var update in _client.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            foreach (var content in update.ContentUpdate)
            {
                yield return content.Text;
            }
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await CompleteAsync("test", cancellationToken: cancellationToken);
            return true;
        }
        catch { return false; }
    }

    public ServiceHealthInfo GetHealthInfo()
    {
        return new ServiceHealthInfo { IsHealthy = true, ServiceName = "OpenAI GPT-4" };
    }
}
```

**서비스 등록:**
```csharp
services.AddScoped<ITextCompletionService>(sp =>
    new OpenAICompletionService(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), "gpt-4"));
```

---

#### IImageToTextService (선택적)

이미지를 텍스트 설명으로 변환하는 서비스입니다. 멀티모달 콘텐츠 처리에 필요합니다.

**인터페이스 정의:**
```csharp
public interface IImageToTextService
{
    Task<string> ConvertImageToTextAsync(string imageUrl, ImageToTextOptions? options = null, CancellationToken cancellationToken = default);
    Task<string> ConvertImageToTextAsync(byte[] imageBytes, string mimeType, ImageToTextOptions? options = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ConvertImagesBatchAsync(IEnumerable<string> imageUrls, ImageToTextOptions? options = null, CancellationToken cancellationToken = default);
    Task<string> ExtractTextFromImageAsync(string imageUrl, CancellationToken cancellationToken = default);
    IReadOnlyList<string> GetSupportedImageFormats();
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
```

**OpenAI GPT-4V 구현 예제:**
```csharp
using OpenAI.Chat;

public class OpenAIVisionService : IImageToTextService
{
    private readonly ChatClient _client;

    public OpenAIVisionService(string apiKey)
    {
        _client = new ChatClient("gpt-4-vision-preview", apiKey);
    }

    public async Task<string> ConvertImageToTextAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = options?.Prompt ?? "Describe this image in detail.";

        var messages = new[]
        {
            new ChatMessage(ChatRole.User, new[]
            {
                ChatMessageContentPart.CreateTextPart(prompt),
                ChatMessageContentPart.CreateImagePart(new Uri(imageUrl))
            })
        };

        var response = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }

    public async Task<string> ExtractTextFromImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        return await ConvertImageToTextAsync(
            imageUrl,
            new ImageToTextOptions { Prompt = "Extract all text from this image (OCR)." },
            cancellationToken);
    }

    public IReadOnlyList<string> GetSupportedImageFormats()
    {
        return new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check
            return true;
        }
        catch { return false; }
    }
}
```

**서비스 등록:**
```csharp
services.AddScoped<IImageToTextService>(sp =>
    new OpenAIVisionService(Environment.GetEnvironmentVariable("OPENAI_API_KEY")));
```

---

#### IWebMetadataExtractor (선택적)

웹 콘텐츠에서 AI 기반으로 메타데이터를 추출하는 서비스입니다. ITextCompletionService를 사용하여 콘텐츠를 분석하고 구조화된 메타데이터를 생성합니다.

**인터페이스 정의:**
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
}
```

**사용 예제:**
```csharp
var metadataExtractor = serviceProvider.GetRequiredService<IWebMetadataExtractor>();

// 기술 문서 메타데이터 추출
var technicalMetadata = await metadataExtractor.ExtractAsync(
    content: documentText,
    url: "https://react.dev/reference/react/useState",
    schema: MetadataSchema.TechnicalDoc
);

Console.WriteLine($"주제: {string.Join(", ", technicalMetadata.Topics)}");
Console.WriteLine($"라이브러리: {technicalMetadata.SchemaSpecificData["libraries"]}");

// 블로그 기사 메타데이터 추출
var articleMetadata = await metadataExtractor.ExtractAsync(
    content: blogPost,
    url: "https://blog.example.com/post",
    schema: MetadataSchema.Article
);

Console.WriteLine($"작성자: {articleMetadata.Author}");
Console.WriteLine($"작성일: {articleMetadata.PublishedDate}");
Console.WriteLine($"태그: {string.Join(", ", articleMetadata.SchemaSpecificData["tags"])}");
```

---

### 메인 프로세서

#### IWebContentProcessor

웹 콘텐츠 처리의 메인 진입점입니다. 크롤링부터 청킹까지 전체 파이프라인을 관리합니다.

**주요 메서드:**
```csharp
public interface IWebContentProcessor
{
    // 단일 URL 처리
    Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
        string url,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    // 여러 URL 배치 처리
    Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
        IEnumerable<string> urls,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    // 웹사이트 전체 크롤링 (스트리밍)
    IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    // HTML 직접 처리
    Task<IReadOnlyList<WebContentChunk>> ProcessHtmlAsync(
        string htmlContent,
        string sourceUrl,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    // 진행률 모니터링
    IAsyncEnumerable<ProcessingProgress> MonitorProgressAsync(string jobId);

    // 작업 취소
    Task<bool> CancelJobAsync(string jobId);

    // 처리 통계
    Task<ProcessingStatistics> GetStatisticsAsync();

    // 사용 가능한 청킹 전략 목록
    IReadOnlyList<string> GetAvailableChunkingStrategies();
}
```

**사용 예제:**
```csharp
var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();

// 1. 단일 URL 처리
var chunks = await processor.ProcessUrlAsync("https://example.com");

// 2. 여러 URL 배치 처리
var urls = new[] { "https://example.com/page1", "https://example.com/page2" };
var batchResults = await processor.ProcessUrlsBatchAsync(urls);

// 3. 웹사이트 전체 크롤링 (스트리밍)
await foreach (var chunk in processor.ProcessWebsiteAsync(
    "https://docs.example.com",
    new CrawlOptions { MaxDepth = 2, MaxPages = 100 },
    new ChunkingOptions { Strategy = "Auto" }))
{
    Console.WriteLine($"청크 생성: {chunk.ChunkId}");
}

// 4. HTML 직접 처리
string html = await File.ReadAllTextAsync("page.html");
var htmlChunks = await processor.ProcessHtmlAsync(html, "https://example.com");

// 5. 처리 통계 확인
var stats = await processor.GetStatisticsAsync();
Console.WriteLine($"처리된 페이지: {stats.TotalPagesProcessed}");
Console.WriteLine($"생성된 청크: {stats.TotalChunksGenerated}");
```

---

### 청킹 시스템

#### IChunkingStrategy

청킹 전략 인터페이스입니다. 커스텀 청킹 로직을 구현할 수 있습니다.

**인터페이스 정의:**
```csharp
public interface IChunkingStrategy
{
    string Name { get; }
    string Description { get; }

    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

**커스텀 전략 구현 예제:**
```csharp
public class SentenceBasedChunkingStrategy : IChunkingStrategy
{
    public string Name => "SentenceBased";
    public string Description => "문장 경계 기반 청킹 전략";

    public async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<WebContentChunk>();
        var maxSize = options?.MaxChunkSize ?? 512;
        var overlapSize = options?.OverlapSize ?? 64;

        // 문장 분리 (간단한 예제)
        var sentences = content.Text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > maxSize && currentChunk.Length > 0)
            {
                // 청크 생성
                chunks.Add(new WebContentChunk
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    ChunkIndex = chunkIndex++,
                    Content = currentChunk.ToString().Trim(),
                    SourceUrl = content.SourceUrl,
                    ContentType = content.ContentType,
                    AdditionalMetadata = content.Metadata
                });

                // 오버랩 처리
                var overlapText = GetLastNCharacters(currentChunk.ToString(), overlapSize);
                currentChunk = new StringBuilder(overlapText);
            }

            currentChunk.Append(sentence).Append(". ");
        }

        // 마지막 청크
        if (currentChunk.Length > 0)
        {
            chunks.Add(new WebContentChunk
            {
                ChunkId = Guid.NewGuid().ToString(),
                ChunkIndex = chunkIndex,
                Content = currentChunk.ToString().Trim(),
                SourceUrl = content.SourceUrl,
                ContentType = content.ContentType,
                AdditionalMetadata = content.Metadata
            });
        }

        return chunks;
    }

    private string GetLastNCharacters(string text, int n)
    {
        return text.Length > n ? text.Substring(text.Length - n) : text;
    }
}
```

**서비스 등록:**
```csharp
services.AddScoped<IChunkingStrategy, SentenceBasedChunkingStrategy>();
```

---

### 진행률 모니터링

#### IProgressReporter

처리 진행률을 실시간으로 추적하고 보고합니다.

**인터페이스 정의:**
```csharp
public interface IProgressReporter
{
    Task<IProgressTracker> StartJobAsync(string jobId, string description, int totalSteps);
    Task ReportProgressAsync(string jobId, ProgressInfo progress);
    Task CompleteJobAsync(string jobId, object? result = null);
    Task FailJobAsync(string jobId, Exception error);
    IAsyncEnumerable<ProgressInfo> MonitorProgressAsync(string jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobProgress>> GetAllJobsAsync();
    Task<JobProgress?> GetJobProgressAsync(string jobId);
}
```

**사용 예제:**
```csharp
var progressReporter = serviceProvider.GetRequiredService<IProgressReporter>();

// 작업 시작
var jobId = Guid.NewGuid().ToString();
var tracker = await progressReporter.StartJobAsync(jobId, "웹사이트 크롤링", totalSteps: 3);

try
{
    // 단계 1: 크롤링
    await tracker.UpdateStepAsync("크롤링", 0, "페이지 수집 중...");
    await CrawlWebsiteAsync();

    // 단계 2: 추출
    await tracker.UpdateStepAsync("콘텐츠 추출", 1, "HTML 파싱 중...");
    await ExtractContentAsync();

    // 단계 3: 청킹
    await tracker.UpdateStepAsync("청킹", 2, "청크 생성 중...");
    await ChunkContentAsync();

    // 완료
    await tracker.CompleteAsync(new { TotalChunks = 150 });
}
catch (Exception ex)
{
    await tracker.FailAsync(ex);
}

// 진행률 모니터링 (별도 작업)
await foreach (var progress in progressReporter.MonitorProgressAsync(jobId))
{
    Console.WriteLine($"[{progress.StepName}] {progress.Progress:P0} - {progress.Details}");
}
```

---

#### IEventPublisher

시스템 이벤트를 발행하고 구독합니다.

**인터페이스 정의:**
```csharp
public interface IEventPublisher
{
    Task PublishAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default);
    void Publish(ProcessingEvent processingEvent);
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : ProcessingEvent;
    IDisposable Subscribe<T>(Action<T> handler) where T : ProcessingEvent;
    IDisposable SubscribeAll(Func<ProcessingEvent, Task> handler);
    EventPublishingStatistics GetStatistics();
}
```

**사용 예제:**
```csharp
var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();

// 이벤트 구독
var subscription = eventPublisher.Subscribe<PageProcessedEvent>(async evt =>
{
    Console.WriteLine($"페이지 처리 완료: {evt.Url}");
    await LogToDatabase(evt);
});

// 이벤트 발행
await eventPublisher.PublishAsync(new PageProcessedEvent
{
    Url = "https://example.com",
    ChunkCount = 10,
    ProcessingTimeMs = 250,
    Timestamp = DateTimeOffset.UtcNow
});

// 모든 이벤트 구독
var allEventsSubscription = eventPublisher.SubscribeAll(async evt =>
{
    Console.WriteLine($"[{evt.GetType().Name}] {evt.Timestamp}");
});

// 구독 해제
subscription.Dispose();
allEventsSubscription.Dispose();

// 통계 확인
var stats = eventPublisher.GetStatistics();
Console.WriteLine($"총 발행 이벤트: {stats.TotalEventsPublished}");
Console.WriteLine($"구독자 수: {stats.SubscriberCount}");
```

---

## 고급 사용법

### 웹사이트 전체 크롤링 (스트리밍)

대규모 웹사이트를 처리할 때 메모리 효율적인 스트리밍 방식:

```csharp
var crawlOptions = new CrawlOptions { MaxPages = 100 };
var chunkOptions = new ChunkingOptions { Strategy = "Auto" };

await foreach (var chunk in processor.ProcessWebsiteAsync(
    "https://docs.example.com",
    crawlOptions,
    chunkOptions))
{
    // 청크 생성 즉시 벡터 DB에 저장
    await vectorDb.InsertAsync(new VectorEntry
    {
        Id = chunk.ChunkId,
        Content = chunk.Content,
        Embedding = await embeddingService.GetEmbeddingAsync(chunk.Content),
        Metadata = chunk.AdditionalMetadata
    });

    Console.WriteLine($"처리됨: {chunk.SourceUrl}");
}
```

### 진행 상황 추적

```csharp
await foreach (var result in processor.ProcessWithProgressAsync(
    "https://docs.example.com",
    crawlOptions,
    chunkOptions))
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"✓ 성공: {result.Url} ({result.Result.Count} 청크)");
        await SaveChunksAsync(result.Result);
    }
    else
    {
        Console.WriteLine($"✗ 실패: {result.Url} - {result.Error}");
    }
}
```

### 병렬 처리 설정

```csharp
services.AddWebFlux(config =>
{
    config.MaxDegreeOfParallelism = 4;  // 동시 4개 페이지 처리
    config.EnableCaching = true;         // 캐싱 활성화
});
```

### 커스텀 청킹 전략

```csharp
public class CustomChunkingStrategy : IChunkingStrategy
{
    public string Name => "Custom";
    public string Description => "커스텀 청킹 로직";

    public async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions options,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<WebContentChunk>();

        // 커스텀 로직 구현
        var sentences = content.Text.Split(". ");
        var currentChunk = "";
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > options.MaxChunkSize)
            {
                chunks.Add(new WebContentChunk
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    ChunkIndex = chunkIndex++,
                    Content = currentChunk,
                    SourceUrl = content.SourceUrl
                });
                currentChunk = "";
            }
            currentChunk += sentence + ". ";
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(new WebContentChunk
            {
                ChunkId = Guid.NewGuid().ToString(),
                ChunkIndex = chunkIndex,
                Content = currentChunk,
                SourceUrl = content.SourceUrl
            });
        }

        return chunks;
    }
}

// 등록
services.AddScoped<IChunkingStrategy, CustomChunkingStrategy>();
```

---

## 실전 시나리오

### 시나리오 1: 기술 문서 RAG 시스템

```csharp
public class TechnicalDocumentationRAG
{
    private readonly IWebContentProcessor _processor;
    private readonly IVectorDatabase _vectorDb;
    private readonly ITextEmbeddingService _embedding;

    public async Task IndexDocumentationAsync(string docsUrl)
    {
        var crawlOptions = new CrawlOptions
        {
            MaxDepth = 3,
            MaxPages = 500,
            RespectRobotsTxt = true
        };

        var chunkOptions = new ChunkingOptions
        {
            Strategy = "Smart",      // HTML 구조 인식
            MaxChunkSize = 512,
            OverlapSize = 64
        };

        await foreach (var chunk in _processor.ProcessWebsiteAsync(
            docsUrl, crawlOptions, chunkOptions))
        {
            var embedding = await _embedding.GetEmbeddingAsync(chunk.Content);

            await _vectorDb.UpsertAsync(new DocumentChunk
            {
                Id = chunk.ChunkId,
                Content = chunk.Content,
                Embedding = embedding,
                Source = chunk.SourceUrl,
                Metadata = chunk.AdditionalMetadata
            });
        }
    }

    public async Task<string> QueryAsync(string question)
    {
        var questionEmbedding = await _embedding.GetEmbeddingAsync(question);
        var relevantChunks = await _vectorDb.SearchAsync(questionEmbedding, topK: 5);

        var context = string.Join("\n\n", relevantChunks.Select(c => c.Content));
        return await GenerateAnswerAsync(question, context);
    }
}
```

### 시나리오 2: 블로그 콘텐츠 수집

```csharp
public class BlogContentCollector
{
    public async Task CollectBlogPostsAsync(string blogUrl)
    {
        var processor = GetProcessor();

        var options = new ChunkingOptions
        {
            Strategy = "Intelligent",  // LLM 기반 분할
            MaxChunkSize = 1000,
            MinChunkSize = 300
        };

        var posts = await processor.ProcessUrlAsync(blogUrl, options);

        foreach (var post in posts)
        {
            await SaveToDatabase(new BlogPost
            {
                Title = ExtractTitle(post.AdditionalMetadata),
                Content = post.Content,
                Summary = await GenerateSummaryAsync(post.Content),
                PublishedDate = ExtractDate(post.AdditionalMetadata),
                Author = ExtractAuthor(post.AdditionalMetadata)
            });
        }
    }
}
```

### 시나리오 3: 대용량 문서 처리

```csharp
public class LargeDocumentProcessor
{
    public async Task ProcessLargeWebsiteAsync(string url)
    {
        var options = new ChunkingOptions
        {
            Strategy = "MemoryOptimized",  // 메모리 효율적 처리
            MaxChunkSize = 512,
            BufferSizeBytes = 1024 * 1024  // 1MB 버퍼
        };

        int totalChunks = 0;
        var stopwatch = Stopwatch.StartNew();

        await foreach (var chunk in _processor.ProcessWebsiteAsync(
            url,
            new CrawlOptions { MaxPages = 1000 },
            options))
        {
            await ProcessChunkAsync(chunk);
            totalChunks++;

            if (totalChunks % 100 == 0)
            {
                Console.WriteLine($"처리된 청크: {totalChunks} " +
                    $"(경과 시간: {stopwatch.Elapsed.TotalMinutes:F1}분)");
            }
        }

        Console.WriteLine($"완료: {totalChunks} 청크, " +
            $"{stopwatch.Elapsed.TotalMinutes:F1}분 소요");
    }
}
```

### 시나리오 4: 다국어 콘텐츠 처리

```csharp
public class MultilingualContentProcessor
{
    public async Task ProcessMultilingualSiteAsync(string baseUrl)
    {
        var languages = new[] { "en", "ko", "ja", "zh" };

        foreach (var lang in languages)
        {
            var url = $"{baseUrl}/{lang}";

            var chunks = await _processor.ProcessUrlAsync(
                url,
                new ChunkingOptions
                {
                    Strategy = "Semantic",
                    MaxChunkSize = 512
                });

            foreach (var chunk in chunks)
            {
                // 언어별로 별도 인덱스에 저장
                await _vectorDb.UpsertAsync(
                    index: $"docs_{lang}",
                    document: new
                    {
                        Id = chunk.ChunkId,
                        Content = chunk.Content,
                        Language = lang,
                        Embedding = await GetEmbeddingAsync(chunk.Content, lang)
                    });
            }
        }
    }
}
```

---

## 문제 해결

### 메모리 부족

**증상**: OutOfMemoryException 발생

**해결책**:
```csharp
// MemoryOptimized 전략 사용
var options = new ChunkingOptions
{
    Strategy = "MemoryOptimized",
    BufferSizeBytes = 512 * 1024  // 512KB로 제한
};

// 스트리밍 방식으로 처리
await foreach (var chunk in processor.ProcessWebsiteAsync(url, options))
{
    await ProcessChunkImmediately(chunk);
}
```

### 처리 속도 느림

**증상**: 대규모 사이트 처리가 너무 느림

**해결책**:
```csharp
services.AddWebFlux(config =>
{
    // 병렬 처리 증가
    config.MaxDegreeOfParallelism = 8;

    // 캐싱 활성화
    config.EnableCaching = true;

    // 빠른 전략 사용
    config.Chunking.DefaultStrategy = "FixedSize";
});
```

### 청크 품질 낮음

**증상**: 의미가 잘리거나 문맥이 유실됨

**해결책**:
```csharp
// 고품질 전략 사용
var options = new ChunkingOptions
{
    Strategy = "Intelligent",  // 또는 "Semantic"
    MaxChunkSize = 1000,       // 크기 증가
    OverlapSize = 200          // 오버랩 증가
};
```

### robots.txt 차단

**증상**: 크롤링이 차단됨

**해결책**:
```csharp
var crawlOptions = new CrawlOptions
{
    RespectRobotsTxt = false,  // 주의: 웹사이트 정책 확인 필요
    UserAgent = "MyBot/1.0 (+https://mysite.com/bot)",
    DelayMs = 2000  // 서버 부하 고려
};
```

### AI 서비스 오류

**증상**: 임베딩 또는 LLM 서비스 실패

**해결책**:
```csharp
public class ResilientEmbeddingService : ITextEmbeddingService
{
    public async Task<double[]> GetEmbeddingAsync(string text, CancellationToken ct)
    {
        int retries = 3;
        while (retries > 0)
        {
            try
            {
                return await _innerService.GetEmbeddingAsync(text, ct);
            }
            catch (Exception ex)
            {
                retries--;
                if (retries == 0) throw;
                await Task.Delay(1000 * (4 - retries));  // 백오프
            }
        }
        throw new Exception("Max retries exceeded");
    }
}
```

---

## 다음 단계

- [청킹 전략 상세 가이드](CHUNKING_STRATEGIES.md)
- [API 참조](INTERFACES.md)
- [아키텍처 이해하기](ARCHITECTURE.md)
- [GitHub 샘플 코드](../samples/)

## 지원

- 이슈: [GitHub Issues](https://github.com/iyulab/WebFlux/issues)
- 패키지: [NuGet](https://www.nuget.org/packages/WebFlux/)
