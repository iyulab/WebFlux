# WebFlux 처리 파이프라인

> RAG 전처리를 위한 웹 콘텐츠 처리 파이프라인 (Phase 1 구현)

## 개요

WebFlux는 웹 콘텐츠를 RAG에 최적화된 청크로 변환하는 **4단계 파이프라인**을 제공합니다.

```
URL → Crawling → Extraction → AI Enhancement → Chunking → RAG Ready Chunks
```

## 파이프라인 아키텍처

```
┌──────────────────────────────────────────────────────────────┐
│                  WebFlux Processing Pipeline                 │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐      │
│  │  Stage 1    │──▶│  Stage 2    │──▶│  Stage 3    │──┐   │
│  │  Crawling   │   │ Extraction  │   │  AI Enhance │  │   │
│  │ (Playwright)│   │(HTML→Text)  │   │  (Optional) │  │   │
│  └─────────────┘   └─────────────┘   └─────────────┘  │   │
│                                                         │   │
│  ┌─────────────────────────────────────────────────────┘   │
│  │                                                          │
│  ▼                                                          │
│  ┌─────────────┐                                           │
│  │  Stage 4    │                                           │
│  │  Chunking   │                                           │
│  │             │                                           │
│  │  Paragraph  │ FixedSize │ Auto │ ...                    │
│  └─────────────┘                                           │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │     Output: IReadOnlyList<WebContentChunk>         │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

## Stage 1: Crawling (크롤링)

**목적**: 웹 페이지 다운로드 및 동적 콘텐츠 렌더링

**구현 상태**: ✅ 완료 (Phase 1)

**주요 기능**:
- Playwright 기반 동적 렌더링 (JavaScript SPA 지원)
- 자동 스크롤링 (Lazy Loading 콘텐츠)
- 네트워크 대기 (NetworkIdle)
- 브라우저 재사용 최적화

**사용 크롤러**:
- `PlaywrightCrawler`: Chromium 기반 headless 브라우저

**출력**:
```csharp
public class CrawlResult
{
    public string Url { get; set; }
    public string Content { get; set; }         // 완전 렌더링된 HTML
    public int StatusCode { get; set; }
    public string ContentType { get; set; }
    public long ResponseTimeMs { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## Stage 2: Extraction (추출)

**목적**: HTML에서 텍스트 추출 및 정제

**구현 상태**: ✅ 완료 (Phase 1)

**주요 기능**:
- HTML → 텍스트 변환
- 노이즈 제거 (광고, 네비게이션)
- 메타데이터 추출 (제목, 설명)
- 병렬 처리 (Channels 기반)

**사용 추출기**:
- `HtmlExtractor`: HTML 콘텐츠 추출

**출력**:
```csharp
public class ExtractedContent
{
    public string Url { get; set; }
    public string Text { get; set; }            // 추출된 텍스트
    public string MainContent { get; set; }     // 주요 콘텐츠
    public WebContentMetadata Metadata { get; set; }
}
```

## Stage 3: AI Enhancement (AI 증강)

**목적**: LLM을 활용한 콘텐츠 품질 향상 (선택적)

**구현 상태**: ✅ 완료 (Phase 1)

**주요 기능**:
- 콘텐츠 요약 (Summarize)
- 메타데이터 추출 (키워드, 토픽, 난이도 등)
- 병렬 처리 (요약 + 메타데이터 동시 실행)
- 서비스 가용성 자동 체크

**필요 서비스**:
- `ITextCompletionService`: 소비자가 구현 (OpenAI, Anthropic, Azure 등)

**활성화 방법**:
```csharp
config.AiEnhancement.Enabled = true;
config.AiEnhancement.EnableSummary = true;
config.AiEnhancement.EnableMetadata = true;
```

**출력**:
```csharp
public class EnhancedContent
{
    public string OriginalContent { get; set; }
    public string? Summary { get; set; }        // AI 생성 요약
    public AiMetadata? Metadata { get; set; }   // AI 추출 메타데이터
}

public class AiMetadata
{
    public string Title { get; set; }
    public List<string> Keywords { get; set; }
    public List<string> Topics { get; set; }
    public string Sentiment { get; set; }
    public string DifficultyLevel { get; set; }
}
```

## Stage 4: Chunking (청킹)

**목적**: 콘텐츠를 RAG 최적화 청크로 분할

**구현 상태**: ✅ 부분 완료 (Phase 1)

**구현된 전략**:
1. **Paragraph**: 단락 경계 기준 분할 (기본값)
2. **FixedSize**: 고정 크기 분할

**계획된 전략** (Phase 2+):
- Smart: 의미 경계 인식
- Semantic: 임베딩 기반 (IEmbeddingService 필요)
- MemoryOptimized: 메모리 절감 최적화

**청킹 옵션**:
```csharp
public class ChunkingOptions
{
    public ChunkingStrategyType Strategy { get; set; } = ChunkingStrategyType.Paragraph;
    public int MaxChunkSize { get; set; } = 1000;
    public int MinChunkSize { get; set; } = 100;
    public int ChunkOverlap { get; set; } = 50;
}
```

**출력**:
```csharp
public class WebContentChunk
{
    public string ChunkId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; }
    public string SourceUrl { get; set; }
    public Dictionary<string, object> AdditionalMetadata { get; set; }
}
```

## 사용 예제

### 기본 사용 (SimpleOpenAITest 패턴)

```csharp
// DI 설정
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddSimpleConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// WebFlux SDK 등록
services.AddWebFlux(config =>
{
    config.Crawling.Strategy = "Dynamic";              // Playwright 사용
    config.AiEnhancement.Enabled = true;               // AI 증강 활성화
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
var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();

// 단일 URL 처리
var chunkingOptions = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Paragraph,
    MaxChunkSize = 1000,
    MinChunkSize = 100,
    ChunkOverlap = 50
};

var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);

// 결과 사용
foreach (var chunk in chunks)
{
    Console.WriteLine($"청크 {chunk.ChunkIndex}: {chunk.Content.Length}자");

    // RAG 저장소에 저장
    await vectorDb.AddAsync(chunk);
}
```

### AI 증강 없이 사용

```csharp
// AI 서비스 등록 생략
services.AddWebFlux(config =>
{
    config.Crawling.Strategy = "Dynamic";
    config.AiEnhancement.Enabled = false;  // AI 증강 비활성화
    config.Chunking.DefaultStrategy = "Paragraph";
});

// ITextCompletionService 등록 안함

// 파이프라인 실행: Crawling → Extraction → Chunking
var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);
```

## 성능 최적화

### 스트리밍 처리

파이프라인은 `IAsyncEnumerable`을 사용하여 메모리 효율적으로 처리합니다.

```csharp
// ProcessAsync 내부 구조
var crawlingResults = CrawlWebContent(config, cancellationToken);
var extractionResults = ExtractContent(crawlingResults, config, cancellationToken);
var enhancedResults = EnhanceContent(extractionResults, config, cancellationToken);

await foreach (var chunk in ChunkContent(enhancedResults, config, cancellationToken))
{
    yield return chunk;
}
```

### 병렬 처리

추출 단계에서 병렬 처리를 통해 성능을 최적화합니다.

```csharp
var semaphore = new SemaphoreSlim(config.Performance.MaxDegreeOfParallelism);
var channel = Channel.CreateUnbounded<ExtractedContent>();

// 병렬로 여러 문서 처리
await foreach (var webContent in webContents)
{
    await ProcessSingleContent(webContent, writer, semaphore);
}
```

### 브라우저 재사용

PlaywrightCrawler는 브라우저 인스턴스를 재사용하여 초기화 오버헤드를 제거합니다.

## 이벤트 시스템

파이프라인은 진행 상황을 실시간으로 리포팅합니다.

```csharp
// 이벤트 구독
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

**이벤트 종류**:
- `ProcessingStartedEvent`: 처리 시작
- `ProcessingProgressEvent`: 진행률 업데이트 (10개 청크마다)
- `ProcessingCompletedEvent`: 처리 완료
- `ProcessingFailedEvent`: 처리 실패

## 오류 처리

각 단계에서 발생한 오류는 로깅되며, 파이프라인은 계속 진행됩니다.

```csharp
try
{
    var extracted = await extractor.ExtractAutoAsync(content);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to extract content from {Url}", url);
    // 에러 발생해도 파이프라인 계속 진행
}
```

## 구성 옵션

### CrawlingConfiguration
```csharp
config.Crawling.Strategy = "Dynamic";           // "Dynamic" (Playwright) or "Static"
config.Crawling.DefaultTimeoutSeconds = 30;
config.Crawling.DefaultDelayMs = 500;
```

### AiEnhancementConfiguration
```csharp
config.AiEnhancement.Enabled = true;
config.AiEnhancement.EnableSummary = true;
config.AiEnhancement.EnableMetadata = true;
config.AiEnhancement.EnableRewrite = false;     // Phase 2 계획
config.AiEnhancement.EnableParallelProcessing = true;
```

### ChunkingConfiguration
```csharp
config.Chunking.DefaultStrategy = "Paragraph";  // "Paragraph", "FixedSize", "Auto"
config.Chunking.MaxChunkSize = 1000;
config.Chunking.MinChunkSize = 100;
```

## 향후 계획

### Phase 2: Advanced Chunking
- Smart 전략: 의미 경계 인식
- Semantic 전략: 임베딩 기반 청킹
- MemoryOptimized 전략: 메모리 사용 84% 절감

### Phase 3: Multimodal
- 이미지-텍스트 변환
- IImageToTextService 인터페이스
- 이미지 설명 자동 생성

### Phase 4: Performance
- 캐싱 시스템
- 적응형 성능 최적화
- 품질 모니터링

## 참고 문서

- [INTERFACES.md](./INTERFACES.md) - 인터페이스 상세 설명
- [CHUNKING_STRATEGIES.md](./CHUNKING_STRATEGIES.md) - 청킹 전략 가이드
- [ARCHITECTURE.md](./ARCHITECTURE.md) - 시스템 아키텍처
- [TUTORIAL.md](./TUTORIAL.md) - 단계별 튜토리얼
