# WebFlux 튜토리얼

실전 예제로 배우는 WebFlux SDK 완벽 가이드

## 목차

1. [설치](#설치)
2. [첫 번째 프로젝트](#첫-번째-프로젝트)
3. [기본 사용법](#기본-사용법)
4. [핵심 인터페이스](#핵심-인터페이스)
   - [ITextEmbeddingService](#itextembeddingservice-선택적)
   - [ITextCompletionService](#itextcompletionservice-선택적)
   - [IWebContentProcessor](#iwebcontentprocessor)
   - [IChunkingStrategy](#ichunkingstrategy)
   - [IEventPublisher](#ieventpublisher)
5. [고급 사용법](#고급-사용법)
6. [실전 시나리오](#실전-시나리오)
7. [문제 해결](#문제-해결)

---

## 설치

### 요구사항

- .NET 10 이상
- (선택) AI 서비스 (OpenAI, Azure OpenAI, Anthropic 등)

### NuGet 패키지 설치

```bash
dotnet add package WebFlux
```

### AI 서비스 준비

크롤링·추출·청킹은 AI 서비스 없이 동작합니다. AI 서비스를 등록하면 메타데이터 추출과 AI 증강
(`ITextCompletionService`), Semantic 청킹(FluxCurator `IEmbedder`)이 켜집니다. 연결할 수 있는 서비스:

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

### 2단계: AI 서비스 구현 (선택)

크롤링·청킹은 AI 서비스 없이 동작하므로 이 단계는 건너뛰어도 됩니다. 청크를 벡터 DB 에 넣으려면 임베딩
서비스가 필요합니다. OpenAI를 사용하는 경우:

```csharp
using OpenAI.Embeddings;
using WebFlux.Core.Interfaces;

public class OpenAIEmbeddingService : ITextEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return response.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
        return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }

    public int MaxTokens => 8191;
    public int EmbeddingDimension => 1536;
}
```

### 3단계: WebFlux 설정 및 실행

```csharp
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Extensions;

var services = new ServiceCollection();

// AI 서비스 없이 동작한다. Semantic 청킹을 쓰려면 FluxCurator IEmbedder 를 먼저 등록한다 (README «Chunking Strategies»).

// WebFlux 등록
services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// URL 처리
var chunks = await processor.ProcessUrlAsync("https://example.com");

foreach (var chunk in chunks)
{
    Console.WriteLine($"청크 {chunk.SequenceNumber}:");
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
    new ChunkingOptions { Strategy = ChunkingStrategyType.Auto }
);

// Smart 전략 (HTML 구조 기반)
var smartChunks = await processor.ProcessUrlAsync(
    url,
    new ChunkingOptions { Strategy = ChunkingStrategyType.Smart }
);

// Semantic 전략 (의미 기반 - 임베딩 사용)
var semanticChunks = await processor.ProcessUrlAsync(
    url,
    new ChunkingOptions { Strategy = ChunkingStrategyType.Semantic }
);
```

### 청크 크기 조정

```csharp
var options = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Auto,
    MaxChunkSize = 1000,    // 최대 1000 토큰
    MinChunkSize = 200,     // 최소 200 토큰
    ChunkOverlap = 100      // 100 토큰 오버랩
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
    TimeoutMs = 30000               // 요청당 30초 타임아웃 (타임아웃은 재시도하지 않는다)
};
```

---

## 핵심 인터페이스

WebFlux는 **Interface Provider** 패턴을 사용합니다. 라이브러리는 인터페이스를 정의하고, 소비 애플리케이션이 구현체를 제공합니다.

### AI 서비스 인터페이스

#### ITextEmbeddingService (선택적)

텍스트를 벡터 임베딩으로 변환하는 서비스 계약입니다. WebFlux 파이프라인은 이 서비스를 호출하지 않습니다 — 청크를
벡터 DB 에 넣을 때 여러분의 코드가 씁니다. Semantic 청킹은 이 인터페이스가 아니라 FluxCurator
`IEmbedder`(`FluxCurator.Core.Core.IEmbedder`)를 `AddWebFlux()` 전에 등록해야 켜집니다.

멤버(`GetEmbeddingAsync`, `GetEmbeddingsAsync`, `MaxTokens`, `EmbeddingDimension`)는
[소스](../src/WebFlux/Core/Interfaces/ITextEmbeddingService.cs)의 XML 문서를 참고하세요.

**OpenAI 구현 예제:**
```csharp
using OpenAI.Embeddings;

public class OpenAIEmbeddingService : ITextEmbeddingService
{
    private readonly EmbeddingClient _client;

    public OpenAIEmbeddingService(string apiKey)
    {
        _client = new EmbeddingClient("text-embedding-3-small", apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return response.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
        return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }

    public int MaxTokens => 8191;
    public int EmbeddingDimension => 1536;
}
```

**서비스 등록:**
```csharp
services.AddScoped<ITextEmbeddingService>(sp =>
    new OpenAIEmbeddingService(Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("OPENAI_API_KEY is not set")));
```

---

#### ITextCompletionService (선택적)

LLM 텍스트 완성 서비스입니다. AI 증강(요약·재작성)과 메타데이터 추출에 사용됩니다.

공유 계약 패키지 [`Flux.Abstractions`](https://www.nuget.org/packages/Flux.Abstractions/)의 인터페이스로,
`CompleteAsync` 만 구현하면 됩니다 — `CompleteJsonAsync`·`CompleteBatchAsync`·`CompleteStreamAsync` 는 기본 구현이
있습니다(스트리밍을 지원하는 SDK 라면 `CompleteStreamAsync` 를 직접 구현하세요). 상태 확인(`IsAvailableAsync`,
`GetHealthInfo`)까지 제공하려면 이를 확장한 `WebFlux.Core.Interfaces.IWebLlmService` 를 구현하고
`AddWebFluxAIServices<TService>()` 로 등록합니다.

**OpenAI GPT-4 구현 예제:**
```csharp
using Flux.Abstractions;
using OpenAI.Chat;
using TextCompletionOptions = Flux.Abstractions.TextCompletionOptions;

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
        ChatMessage[] messages = [new UserChatMessage(prompt)];
        var response = await _client.CompleteChatAsync(messages, ToChatOptions(options), cancellationToken);
        return response.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatMessage[] messages = [new UserChatMessage(prompt)];
        await foreach (var update in _client.CompleteChatStreamingAsync(messages, ToChatOptions(options), cancellationToken))
        {
            foreach (var content in update.ContentUpdate)
            {
                yield return content.Text;
            }
        }
    }

    private static ChatCompletionOptions ToChatOptions(TextCompletionOptions? options) => new()
    {
        Temperature = options?.Temperature,
        MaxOutputTokenCount = options?.MaxTokens,
    };
}
```

**서비스 등록:**
```csharp
using Flux.Abstractions;

services.AddScoped<ITextCompletionService>(sp =>
    new OpenAICompletionService(Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("OPENAI_API_KEY is not set"), "gpt-4"));

// AI 증강(요약·재작성)은 구성에서 켜고 증강 서비스를 등록한다
services.AddWebFlux(config => config.AiEnhancement.Enabled = true);
services.AddWebFluxAIEnhancement();
```

---

#### IWebMetadataExtractor (선택적)

웹 콘텐츠에서 AI 기반으로 메타데이터를 추출하는 서비스입니다. ITextCompletionService를 사용하여 콘텐츠를 분석하고 구조화된 메타데이터를 생성합니다.
멤버(`ExtractAsync`, `ExtractBatchAsync`, `GetSupportedSchemas`, `GetSchemaDescription`)는
[소스](../src/WebFlux/Core/Interfaces/IWebMetadataExtractor.cs)의 XML 문서를 참고하세요.

`AddWebFlux()` 는 이 인터페이스를 컨테이너에 등록하지 않습니다. 크롤 경로(`ProcessWebsiteAsync`)에서 옵션으로 켜면
컨테이너의 `IWebMetadataExtractor` — 없으면 등록된 `ITextCompletionService` 로 만든 기본 추출기 — 가 실행되어
추출 결과의 `Metadata` 에 병합됩니다:

```csharp
var crawlOptions = new CrawlOptions
{
    EnableMetadataExtraction = true,          // ITextCompletionService 등록 필요
    MetadataSchema = MetadataSchema.TechnicalDoc
};
```

**직접 호출 예제** (컨테이너 밖에서는 기본 구현 `AIWebMetadataExtractor` 를 만든다):
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using WebFlux.Infrastructure.AI;

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
var metadataExtractor = new AIWebMetadataExtractor(
    new OpenAICompletionService(apiKey, "gpt-4"),
    NullLogger<AIWebMetadataExtractor>.Instance);

var documentText = await File.ReadAllTextAsync("useState.md");
var blogPost = await File.ReadAllTextAsync("post.md");

// 기술 문서 메타데이터 추출
var technicalMetadata = await metadataExtractor.ExtractAsync(
    content: documentText,
    url: "https://react.dev/reference/react/useState",
    schema: MetadataSchema.TechnicalDoc
);

Console.WriteLine($"주제: {string.Join(", ", technicalMetadata.Topics)}");
Console.WriteLine($"라이브러리: {technicalMetadata.SchemaSpecificData.GetValueOrDefault("libraries")}");

// 블로그 기사 메타데이터 추출
var articleMetadata = await metadataExtractor.ExtractAsync(
    content: blogPost,
    url: "https://blog.example.com/post",
    schema: MetadataSchema.Article
);

Console.WriteLine($"작성자: {articleMetadata.Author}");
Console.WriteLine($"작성일: {articleMetadata.PublishedDate}");
Console.WriteLine($"키워드: {string.Join(", ", articleMetadata.Keywords)}");
```

---

### 메인 프로세서

#### IWebContentProcessor

웹 콘텐츠 처리의 메인 진입점입니다. 크롤링부터 청킹까지 전체 파이프라인을 관리합니다.

`IWebContentProcessor` 는 추출(`IContentExtractService` — `ExtractContentAsync`, `ExtractBatchAsync`,
`ExtractBatchStreamAsync`)과 청킹(`IContentChunkService` — `ProcessUrlAsync`, `ProcessUrlsBatchAsync`,
`ProcessWebsiteAsync`, `ProcessHtmlAsync`)을 묶은 파사드이고, `GetAvailableChunkingStrategies()` 를 더합니다.
시그니처와 옵션 설명은 소스의 XML 문서를 참고하세요:
[IWebContentProcessor](../src/WebFlux/Core/Interfaces/IWebContentProcessor.cs) ·
[IContentExtractService](../src/WebFlux/Core/Interfaces/IContentExtractService.cs) ·
[IContentChunkService](../src/WebFlux/Core/Interfaces/IContentChunkService.cs).

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
    new ChunkingOptions { Strategy = ChunkingStrategyType.Auto }))
{
    Console.WriteLine($"청크 생성: {chunk.Id}");
}

// 4. HTML 직접 처리
string html = await File.ReadAllTextAsync("page.html");
var htmlChunks = await processor.ProcessHtmlAsync(html, "https://example.com");

// 5. 사용 가능한 청킹 전략 목록 확인
var strategies = processor.GetAvailableChunkingStrategies();
Console.WriteLine($"사용 가능한 전략: {string.Join(", ", strategies)}");
```

---

### 청킹 시스템

#### IChunkingStrategy

청킹 전략 인터페이스입니다. 커스텀 청킹 로직을 구현할 수 있습니다. 멤버(`Name`, `Description`, `ChunkAsync`)는
[소스](../src/WebFlux/Core/Interfaces/IChunkingStrategy.cs)의 XML 문서를 참고하세요.

내장 전략은 `ChunkingOptions.Strategy`(enum)로 고르며, 전략 팩토리는 내장 전략만 만듭니다 — 컨테이너에
`IChunkingStrategy` 를 추가 등록해도 `ProcessUrlAsync` 가 그것을 고르지 않습니다. 커스텀 전략은 추출 결과
(`ExtractContentAsync`)에 직접 적용합니다.

**커스텀 전략 구현 및 사용 예제:**
```csharp
// 사용: 추출한 콘텐츠에 커스텀 전략을 직접 적용한다
var content = await processor.ExtractContentAsync(url);   // 실패하면 WebExtractionException
var chunks = await new SentenceBasedChunkingStrategy().ChunkAsync(content, new ChunkingOptions { MaxChunkSize = 512 });
Console.WriteLine($"생성된 청크 수: {chunks.Count}");

public class SentenceBasedChunkingStrategy : IChunkingStrategy
{
    public string Name => "SentenceBased";
    public string Description => "문장 경계 기반 청킹 전략";

    public Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<WebContentChunk>();
        var maxSize = options?.MaxChunkSize ?? 512;
        var overlapSize = options?.ChunkOverlap ?? 64;

        // 문장 분리 (간단한 예제)
        var sentences = content.Text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > maxSize && currentChunk.Length > 0)
            {
                // 청크 생성
                chunks.Add(CreateChunk(content, currentChunk.ToString().Trim(), chunkIndex++));

                // 오버랩 처리
                var overlapText = GetLastNCharacters(currentChunk.ToString(), overlapSize);
                currentChunk = new StringBuilder(overlapText);
            }

            currentChunk.Append(sentence).Append(". ");
        }

        // 마지막 청크
        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateChunk(content, currentChunk.ToString().Trim(), chunkIndex));
        }

        return Task.FromResult<IReadOnlyList<WebContentChunk>>(chunks);
    }

    private WebContentChunk CreateChunk(ExtractedContent content, string text, int sequenceNumber) => new()
    {
        Id = Guid.NewGuid().ToString(),
        SequenceNumber = sequenceNumber,
        Content = text,
        Title = content.Title,
        SourceUrl = content.SourceUrl,
        StrategyInfo = new ChunkingStrategyInfo { StrategyName = Name }
    };

    private static string GetLastNCharacters(string text, int n)
    {
        return text.Length > n ? text.Substring(text.Length - n) : text;
    }
}
```

---

### 진행률 모니터링

#### 진행률은 이벤트로

진행률은 [`IEventPublisher`](#ieventpublisher)로 받습니다. 처리 실행마다 `ProcessingStartedEvent`, 청크마다 `ChunkGeneratedEvent`, 청크 10개마다 `ProcessingProgressEvent`,
끝에 `ProcessingCompletedEvent`(또는 `ProcessingFailedEvent`)가 발행되고, 크롤러가 가져오는 URL마다 `UrlProcessingStartedEvent` → `UrlProcessedEvent`/`UrlProcessingFailedEvent`가 발행됩니다.

#### IEventPublisher

시스템 이벤트를 발행하고 구독합니다. 멤버(`PublishAsync`, `Publish`, `Subscribe<T>`, `SubscribeAll`,
`GetStatistics`)는 [소스](../src/WebFlux/Core/Interfaces/IEventPublisher.cs)의 XML 문서를 참고하세요.

**사용 예제:**
```csharp
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models.Events;

var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();

// 페이지 크롤링 완료 이벤트 구독
using var s1 = eventPublisher.Subscribe<UrlProcessedEvent>(async evt =>
{
    Console.WriteLine($"페이지 크롤링 완료: {evt.Url} ({evt.ContentLength}자) {evt.ProcessingTimeMs}ms");
    await LogToDatabase(evt);
});

// 청크 생성 이벤트 구독
using var s2 = eventPublisher.Subscribe<ChunkGeneratedEvent>(evt =>
{
    Console.WriteLine($"청크 생성: #{evt.SequenceNumber} ({evt.ChunkSize}자) from {evt.SourceUrl}");
});

// 모든 이벤트 구독
using var sAll = eventPublisher.SubscribeAll(evt =>
{
    Console.WriteLine($"[{evt.EventType}] {evt.Timestamp}");
    return Task.CompletedTask;
});

// 통계 확인
var stats = eventPublisher.GetStatistics();
Console.WriteLine($"총 발행 이벤트: {stats.TotalEventsPublished}");
Console.WriteLine($"구독자 수: {stats.SubscriberCount}");

// 여러분의 저장 로직
static Task LogToDatabase(UrlProcessedEvent evt) => Task.CompletedTask;
```

---

## 고급 사용법

### 웹사이트 전체 크롤링 (스트리밍)

대규모 웹사이트를 처리할 때 메모리 효율적인 스트리밍 방식:

```csharp
public class StreamingIndexer(
    IWebContentProcessor processor,
    ITextEmbeddingService embeddingService,
    IVectorDatabase vectorDb)
{
    public async Task IndexAsync(CancellationToken cancellationToken = default)
    {
        var crawlOptions = new CrawlOptions { MaxPages = 100 };
        var chunkOptions = new ChunkingOptions { Strategy = ChunkingStrategyType.Auto };

        await foreach (var chunk in processor.ProcessWebsiteAsync(
            "https://docs.example.com",
            crawlOptions,
            chunkOptions,
            cancellationToken))
        {
            // 청크 생성 즉시 벡터 DB에 저장
            await vectorDb.InsertAsync(new VectorEntry
            {
                Id = chunk.Id,
                Content = chunk.Content,
                Embedding = await embeddingService.GetEmbeddingAsync(chunk.Content, cancellationToken),
                Metadata = chunk.AdditionalMetadata
            }, cancellationToken);

            Console.WriteLine($"처리됨: {chunk.SourceUrl}");
        }
    }
}

// 벡터 DB 는 여러분의 저장소 코드다
public interface IVectorDatabase
{
    Task InsertAsync(VectorEntry entry, CancellationToken cancellationToken = default);
}

public class VectorEntry
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required float[] Embedding { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
```

### 진행 상황 추적

```csharp
// 사이트 크롤은 청크를 도착하는 대로 흘려보낸다 — URL 별로 세면 진행 상황이 된다.
var perPage = new Dictionary<string, int>();
await foreach (var chunk in processor.ProcessWebsiteAsync(
    "https://docs.example.com",
    crawlOptions,
    chunkOptions))
{
    perPage[chunk.SourceUrl] = perPage.GetValueOrDefault(chunk.SourceUrl) + 1;
    Console.WriteLine($"✓ {chunk.SourceUrl} ({perPage[chunk.SourceUrl]} 청크)");
    await SaveChunkAsync(chunk);
}

// 여러분의 저장 로직
static Task SaveChunkAsync(WebContentChunk chunk) => Task.CompletedTask;
```

### 병렬 처리 설정

```csharp
services.AddWebFlux(config =>
{
    config.Performance.MaxDegreeOfParallelism = 4;  // 동시 4개 페이지 처리
});
```

### 커스텀 청킹 전략

전략 팩토리는 내장 전략만 만들므로, 커스텀 전략은 컨테이너에 등록하지 않고 추출 결과에 직접 적용합니다
([IChunkingStrategy](#ichunkingstrategy) 참조).

```csharp
// 사용
var content = await processor.ExtractContentAsync(url);
var chunks = await new CustomChunkingStrategy().ChunkAsync(content, new ChunkingOptions());

public class CustomChunkingStrategy : IChunkingStrategy
{
    public string Name => "Custom";
    public string Description => "커스텀 청킹 로직";

    public Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<WebContentChunk>();
        var maxSize = options?.MaxChunkSize ?? 512;

        // 커스텀 로직 구현
        var sentences = content.Text.Split(". ");
        var currentChunk = "";
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > maxSize)
            {
                chunks.Add(new WebContentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    SequenceNumber = chunkIndex++,
                    Content = currentChunk,
                    SourceUrl = content.SourceUrl,
                    StrategyInfo = new ChunkingStrategyInfo { StrategyName = Name }
                });
                currentChunk = "";
            }
            currentChunk += sentence + ". ";
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(new WebContentChunk
            {
                Id = Guid.NewGuid().ToString(),
                SequenceNumber = chunkIndex,
                Content = currentChunk,
                SourceUrl = content.SourceUrl,
                StrategyInfo = new ChunkingStrategyInfo { StrategyName = Name }
            });
        }

        return Task.FromResult<IReadOnlyList<WebContentChunk>>(chunks);
    }
}
```

---

## 실전 시나리오

### 시나리오 1: 기술 문서 RAG 시스템

```csharp
using Flux.Abstractions;

public class TechnicalDocumentationRAG
{
    private readonly IWebContentProcessor _processor;
    private readonly IVectorDatabase _vectorDb;
    private readonly ITextEmbeddingService _embedding;
    private readonly ITextCompletionService _completion;

    public TechnicalDocumentationRAG(
        IWebContentProcessor processor,
        IVectorDatabase vectorDb,
        ITextEmbeddingService embedding,
        ITextCompletionService completion)
    {
        _processor = processor;
        _vectorDb = vectorDb;
        _embedding = embedding;
        _completion = completion;
    }

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
            Strategy = ChunkingStrategyType.Smart,      // HTML 구조 인식
            MaxChunkSize = 512,
            ChunkOverlap = 64
        };

        await foreach (var chunk in _processor.ProcessWebsiteAsync(
            docsUrl, crawlOptions, chunkOptions))
        {
            var embedding = await _embedding.GetEmbeddingAsync(chunk.Content);

            await _vectorDb.UpsertAsync(new DocumentChunk
            {
                Id = chunk.Id,
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
        return await _completion.CompleteAsync($"다음 문서를 근거로 답하라.\n\n{context}\n\n질문: {question}");
    }
}

// 벡터 DB 는 여러분의 저장소 코드다
public interface IVectorDatabase
{
    Task UpsertAsync(DocumentChunk chunk);
    Task<IReadOnlyList<DocumentChunk>> SearchAsync(float[] embedding, int topK);
}

public class DocumentChunk
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required float[] Embedding { get; init; }
    public required string Source { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
```

### 시나리오 2: 블로그 콘텐츠 수집

```csharp
using Flux.Abstractions;

public class BlogContentCollector(IWebContentProcessor processor, ITextCompletionService completion)
{
    public async Task CollectBlogPostsAsync(string blogUrl)
    {
        var options = new ChunkingOptions
        {
            Strategy = ChunkingStrategyType.Paragraph,  // 서술형 블로그 글: 문단 경계
            MaxChunkSize = 1000,
            MinChunkSize = 300
        };

        var posts = await processor.ProcessUrlAsync(blogUrl, options);

        foreach (var post in posts)
        {
            await SaveToDatabase(new BlogPost
            {
                Title = post.Title ?? post.Metadata.Title,
                Content = post.Content,
                Summary = await completion.CompleteAsync($"다음 글을 세 문장으로 요약하라:\n\n{post.Content}"),
                PublishedDate = post.Metadata.PublishedDate,
                Author = post.Metadata.Author
            });
        }
    }

    // 여러분의 저장 로직
    private static Task SaveToDatabase(BlogPost post) => Task.CompletedTask;
}

public class BlogPost
{
    public string? Title { get; init; }
    public required string Content { get; init; }
    public required string Summary { get; init; }
    public DateTimeOffset? PublishedDate { get; init; }
    public string? Author { get; init; }
}
```

### 시나리오 3: 대용량 문서 처리

```csharp
using System.Diagnostics;

public class LargeDocumentProcessor(IWebContentProcessor processor)
{
    public async Task ProcessLargeWebsiteAsync(string url)
    {
        var options = new ChunkingOptions
        {
            Strategy = ChunkingStrategyType.MemoryOptimized,  // 메모리 효율적 처리
            MaxChunkSize = 512
        };

        int totalChunks = 0;
        var stopwatch = Stopwatch.StartNew();

        await foreach (var chunk in processor.ProcessWebsiteAsync(
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

    // 여러분의 청크 처리 로직
    private static Task ProcessChunkAsync(WebContentChunk chunk) => Task.CompletedTask;
}
```

### 시나리오 4: 다국어 콘텐츠 처리

```csharp
public class MultilingualContentProcessor(
    IWebContentProcessor processor,
    ITextEmbeddingService embedding,
    IVectorDatabase vectorDb)
{
    public async Task ProcessMultilingualSiteAsync(string baseUrl)
    {
        var languages = new[] { "en", "ko", "ja", "zh" };

        foreach (var lang in languages)
        {
            var url = $"{baseUrl}/{lang}";

            var chunks = await processor.ProcessUrlAsync(
                url,
                new ChunkingOptions
                {
                    Strategy = ChunkingStrategyType.Semantic,  // FluxCurator IEmbedder 등록 필요
                    MaxChunkSize = 512
                });

            foreach (var chunk in chunks)
            {
                // 언어별로 별도 인덱스에 저장
                await vectorDb.UpsertAsync(
                    index: $"docs_{lang}",
                    document: new
                    {
                        Id = chunk.Id,
                        Content = chunk.Content,
                        Language = lang,
                        Embedding = await embedding.GetEmbeddingAsync(chunk.Content)
                    });
            }
        }
    }
}

// 벡터 DB 는 여러분의 저장소 코드다
public interface IVectorDatabase
{
    Task UpsertAsync(string index, object document);
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
    Strategy = ChunkingStrategyType.MemoryOptimized
};

// 스트리밍 방식으로 처리
await foreach (var chunk in processor.ProcessWebsiteAsync(url, chunkingOptions: options))
{
    await ProcessChunkImmediately(chunk);
}

// 여러분의 청크 처리 로직
static Task ProcessChunkImmediately(WebContentChunk chunk) => Task.CompletedTask;
```

### 처리 속도 느림

**증상**: 대규모 사이트 처리가 너무 느림

**해결책**:
```csharp
services.AddWebFlux(config =>
{
    // 병렬 처리 증가
    config.Performance.MaxDegreeOfParallelism = 8;

    // 빠른 전략 사용
    config.Chunking.DefaultStrategy = ChunkingStrategyType.FixedSize;
});
```

### 청크 품질 낮음

**증상**: 의미가 잘리거나 문맥이 유실됨

**해결책**:
```csharp
// 고품질 전략 사용
var options = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Semantic,  // 임베더 등록 필요 — 없으면 Smart
    MaxChunkSize = 1000,       // 크기 증가
    ChunkOverlap = 200         // 오버랩 증가
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
// 기존 임베딩 서비스를 감싸 재시도를 더하는 데코레이터
public class ResilientEmbeddingService(ITextEmbeddingService innerService) : ITextEmbeddingService
{
    public Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        RetryAsync(ct => innerService.GetEmbeddingAsync(text, ct), cancellationToken);

    public Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default) =>
        RetryAsync(ct => innerService.GetEmbeddingsAsync(texts, ct), cancellationToken);

    public int MaxTokens => innerService.MaxTokens;
    public int EmbeddingDimension => innerService.EmbeddingDimension;

    private static async Task<T> RetryAsync<T>(Func<CancellationToken, Task<T>> call, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await call(ct);
            }
            catch (Exception) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                await Task.Delay(1000 * attempt, ct);  // 백오프
            }
        }
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
