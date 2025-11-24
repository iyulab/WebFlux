# Architecture

WebFlux SDK의 시스템 아키텍처와 설계 원칙을 설명합니다.

## Design Principles

1. **Interface Provider Pattern**: SDK는 인터페이스를 정의하고, 소비자가 구현체를 제공
2. **Dependency Inversion**: 추상화에 의존하여 AI 공급자 중립성 유지
3. **Clean Architecture**: 레이어 분리로 유지보수성과 테스트 용이성 확보
4. **Streaming First**: AsyncEnumerable 기반 스트리밍 처리

## Layer Structure

```
┌─────────────────────────────────────────┐
│       Consumer Application              │
│   (AI Service Implementations)          │
├─────────────────────────────────────────┤
│            WebFlux SDK                  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │    Application Layer              │  │
│  │  - IWebContentProcessor           │  │
│  │  - Pipeline Orchestration         │  │
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │    Domain Layer                   │  │
│  │  - Business Logic                 │  │
│  │  - Domain Models                  │  │
│  └───────────────────────────────────┘  │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │    Infrastructure Layer           │  │
│  │  - Crawlers                       │  │
│  │  - Extractors                     │  │
│  │  - Chunking Strategies            │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Processing Pipeline

4단계 파이프라인으로 웹 콘텐츠를 처리합니다:

```
┌──────────┐   ┌───────────┐   ┌──────────┐   ┌──────────┐
│ Crawl    │──▶│ Extract   │──▶│ Analyze  │──▶│ Chunk    │
└──────────┘   └───────────┘   └──────────┘   └──────────┘
     │              │               │              │
     ▼              ▼               ▼              ▼
CrawlResult   ExtractedContent  Metadata    WebContentChunk
```

### 1. Crawl
- HTML 콘텐츠 수집
- robots.txt, sitemap.xml 준수
- 동적 콘텐츠 렌더링 (Playwright)

### 2. Extract
- HTML, Markdown, JSON, XML 파싱
- 메인 콘텐츠 추출
- 메타데이터 수집

### 3. Analyze
- 문서 구조 분석
- 품질 메트릭 계산
- 웹 표준 메타데이터 파싱 (ai.txt, llms.txt 등)

### 4. Chunk
- 청킹 전략에 따라 콘텐츠 분할
- 의미 경계 인식
- 오버랩 처리

## Core Interfaces

### Consumer-Provided (AI Services)

```csharp
// 필수: 임베딩 생성
public interface ITextEmbeddingService
{
    Task<double[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}

// 선택: LLM 텍스트 생성 (콘텐츠 재구성용)
public interface ITextCompletionService
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
}

// 선택: 이미지-텍스트 변환 (멀티모달 처리용)
public interface IImageToTextService
{
    Task<string> ConvertAsync(byte[] imageData, CancellationToken ct = default);
}
```

### SDK-Provided (WebFlux Core)

```csharp
// 메인 처리 인터페이스
public interface IWebContentProcessor
{
    Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(string url);
    IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(string startUrl);
}

// 크롤러
public interface ICrawler
{
    Task<CrawlResult> CrawlAsync(string url);
    IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(string baseUrl);
}

// 청킹 전략
public interface IChunkingStrategy
{
    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(ExtractedContent content);
}
```

## Streaming Architecture

대규모 웹사이트 처리를 위한 스트리밍 파이프라인:

```csharp
await foreach (var chunk in processor.ProcessWebsiteAsync(url))
{
    // 청크 생성 즉시 처리 (메모리 효율적)
    await StoreChunkAsync(chunk);
}
```

**장점**:
- 메모리 효율성: 전체 사이트를 메모리에 로드하지 않음
- 즉시 처리: 청크 생성 즉시 벡터 DB 저장 가능
- 취소 가능: CancellationToken으로 언제든 중단

## Parallel Processing

Channels 기반 병렬 처리:

```csharp
var channel = Channel.CreateBounded<T>(capacity);
var semaphore = new SemaphoreSlim(maxParallelism);

// Producer
_ = Task.Run(async () => {
    await foreach (var item in source)
    {
        await semaphore.WaitAsync();
        _ = ProcessAsync(item, channel.Writer, semaphore);
    }
    channel.Writer.Complete();
});

// Consumer
await foreach (var result in channel.Reader.ReadAllAsync())
{
    yield return result;
}
```

## Configuration

```csharp
services.AddWebFlux(config =>
{
    // Crawling
    config.MaxDepth = 3;
    config.MaxPages = 100;
    config.RespectRobotsTxt = true;

    // Chunking
    config.DefaultStrategy = ChunkingStrategy.Auto;
    config.MaxChunkSize = 512;
    config.OverlapSize = 64;

    // Performance
    config.MaxDegreeOfParallelism = 4;
    config.EnableCaching = true;
});
```

## Web Standards Support

WebFlux가 분석하는 웹 표준:

| 표준 | 용도 |
|------|------|
| robots.txt | 크롤링 규칙 |
| sitemap.xml | URL 디스커버리 |
| ai.txt | AI 사용 정책 |
| llms.txt | 사이트 구조 정보 |
| manifest.json | PWA 메타데이터 |
| security.txt | 보안 정책 |
| .well-known/* | 표준 메타데이터 |

## Performance Optimization

### 1. Caching
- HTTP 응답 캐싱
- 메타데이터 캐싱
- 임베딩 캐싱 (동일 콘텐츠)

### 2. Memory Management
- 스트리밍 처리로 메모리 사용 최소화
- 백프레셔 제어 (Channel bounded capacity)
- 청크 단위 즉시 처리

### 3. Parallel Processing
- 크롤링 병렬화
- 추출 병렬화
- 청킹 병렬화
- CPU 코어 수에 따른 동적 조절

## Error Handling

```csharp
await foreach (var result in processor.ProcessWithProgressAsync(url))
{
    if (!result.IsSuccess)
    {
        // 개별 페이지 실패는 전체 처리를 중단하지 않음
        _logger.LogWarning("Failed: {Url}, Error: {Error}",
            result.Url, result.Error);
        continue;
    }

    // 성공한 청크만 처리
    await StoreChunksAsync(result.Result);
}
```

## Testing

### Unit Tests
- 각 레이어별 독립적 테스트
- Mock 인터페이스 활용
- 95%+ 코드 커버리지 목표

### Integration Tests
- 실제 웹사이트 크롤링 테스트
- E2E 파이프라인 검증
- 성능 벤치마크

## References

- [INTERFACES.md](./INTERFACES.md) - API 상세 문서
- [CHUNKING_STRATEGIES.md](./CHUNKING_STRATEGIES.md) - 청킹 전략 가이드
