# WebFlux 튜토리얼

실전 예제로 배우는 WebFlux SDK 완벽 가이드

## 목차

1. [설치](#설치)
2. [첫 번째 프로젝트](#첫-번째-프로젝트)
3. [기본 사용법](#기본-사용법)
4. [고급 사용법](#고급-사용법)
5. [실전 시나리오](#실전-시나리오)
6. [문제 해결](#문제-해결)

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
