# WebFlux 처리 파이프라인 설계

> 추출부터 청킹까지 완전한 웹 콘텐츠 처리 파이프라인

## 🔄 파이프라인 개요

WebFlux 파이프라인은 **4단계 처리 프로세스**를 통해 웹 콘텐츠를 RAG 최적화 청크로 변환합니다.

```
📄 Extract → 🔍 Analyze → ✨ Reconstruct → 🎯 Chunk → RAG Ready
```

### 설계 원칙

1. **스트리밍 우선**: 메모리 효율적인 실시간 처리
2. **병렬 처리**: CPU 코어 활용 극대화
3. **독립 실행 가능**: 각 단계를 독립적으로 실행 가능
4. **유연한 구성**: 필요한 단계만 선택하여 사용
5. **품질 중심**: 각 단계에서 품질 메트릭 추적

## 🏗️ 새로운 4단계 아키텍처

### 전체 아키텍처 다이어그램

```
┌────────────────────────────────────────────────────────────────────┐
│                        WebFlux Pipeline v0.2                       │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐          │
│  │   Stage 1    │──▶│   Stage 2    │──▶│   Stage 3    │──┐       │
│  │   Extract    │   │   Analyze    │   │ Reconstruct  │  │       │
│  │              │   │              │   │              │  │       │
│  │ RawContent   │   │ Analyzed     │   │Reconstructed │  │       │
│  │              │   │ Content      │   │  Content     │  │       │
│  └──────────────┘   └──────────────┘   └──────────────┘  │       │
│         │                   │                   │         │       │
│         ▼                   ▼                   ▼         ▼       │
│  ┌────────────────────────────────────────────────────────────┐   │
│  │                      Stage 4                              │   │
│  │                  Chunking Engine                          │◀──┘
│  │                                                            │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐     │
│  │  │  Fixed  │  │Paragraph│  │ Smart   │  │Semantic │     │
│  │  │  Size   │  │         │  │         │  │         │     │
│  │  └─────────┘  └─────────┘  └─────────┘  └─────────┘     │
│  └────────────────────────────────────────────────────────────┘
│                                                                    │
│  ┌────────────────────────────────────────────────────────────────┐
│  │                    Output Stream                               │
│  │         IAsyncEnumerable<WebContentChunk>                      │
│  └────────────────────────────────────────────────────────────────┘
└────────────────────────────────────────────────────────────────────┘
```

### 단계별 상세 설명

## 📄 Stage 1: Extract (추출)

**목적**: 원본 콘텐츠를 있는 그대로 추출하여 보존

**입력**: URL 또는 HTML 문자열
**출력**: `RawContent`

**핵심 기능**:
- HTML 콘텐츠 다운로드
- 메타데이터 추출 (title, description, keywords)
- 이미지 URL 수집
- 링크 추출
- HTTP 헤더 정보 보존

**설계 철학**:
- 원본 콘텐츠 **완전 보존** (손실 없음)
- 가공 및 정제 **일절 하지 않음**
- 향후 재처리 가능하도록 원본 유지

```csharp
public class RawContent
{
    public string Url { get; set; }
    public string Content { get; set; }              // 원본 HTML
    public string ContentType { get; set; }
    public WebContentMetadata Metadata { get; set; }
    public List<string> ImageUrls { get; set; }
    public List<string> Links { get; set; }
    public DateTime ExtractedAt { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}
```

## 🔍 Stage 2: Analyze (분석)

**목적**: 콘텐츠 구조 분석 및 불필요한 요소 제거, 원본은 유지

**입력**: `RawContent`
**출력**: `AnalyzedContent`

**핵심 기능**:
- **구조 분석**: 섹션, 제목, 목록, 표 인식
- **노이즈 제거**: 광고, 네비게이션, 푸터 제거
- **콘텐츠 정제**: 깨끗한 텍스트 추출
- **품질 평가**: 콘텐츠 품질 점수 계산
- **원본 유지**: RawContent와 CleanedContent 모두 보관

**설계 철학**:
- 원본 유지 + 정제된 버전 제공
- 구조 정보 최대한 보존
- 품질 메트릭으로 후속 처리 가이드

```csharp
public class AnalyzedContent
{
    public string Url { get; set; }
    public string RawContent { get; set; }           // 원본 유지
    public string CleanedContent { get; set; }       // 정제된 버전
    public string Title { get; set; }

    // 구조 정보
    public List<ContentSection> Sections { get; set; }
    public List<TableData> Tables { get; set; }
    public List<ImageData> Images { get; set; }
    public StructureInfo Structure { get; set; }

    // 메타데이터 및 품질
    public WebContentMetadata Metadata { get; set; }
    public AnalysisMetrics Metrics { get; set; }     // 품질 점수

    public DateTime AnalyzedAt { get; set; }
}
```

**분석 메트릭**:
```csharp
public class AnalysisMetrics
{
    public double ContentQuality { get; set; }       // 0-1
    public int WordCount { get; set; }
    public int SectionCount { get; set; }
    public double ReadabilityScore { get; set; }
    public bool HasCodeBlocks { get; set; }
    public bool HasTables { get; set; }
}
```

## ✨ Stage 3: Reconstruct (재구성)

**목적**: LLM을 활용한 콘텐츠 재구성 및 품질 향상 (Optional)

**입력**: `AnalyzedContent`
**출력**: `ReconstructedContent`

**핵심 기능**:
- **전략 기반 재구성**: 5가지 전략 제공
- **LLM 증강**: 선택적 AI 품질 향상
- **원본 보존**: 항상 원본 콘텐츠 유지
- **메트릭 추적**: 재구성 품질 및 비용 추적

### 재구성 전략

#### 1. None (기본값)
```csharp
// LLM 불필요, 항상 사용 가능
// 원본 콘텐츠를 그대로 유지
// 품질: 원본 품질 유지, 비용: 0
```

#### 2. Summarize (요약)
```csharp
// LLM 필수: ITextCompletionService
// 긴 콘텐츠를 핵심만 추출하여 요약
// 품질: High, 비용: Medium
// 사용 사례: 토큰 절감, 빠른 검색
```

#### 3. Expand (확장)
```csharp
// LLM 필수: ITextCompletionService
// 간략한 콘텐츠에 상세 설명 추가
// 품질: Very High, 비용: High
// 사용 사례: 학습 자료, 상세 정보 필요
```

#### 4. Rewrite (재작성)
```csharp
// LLM 필수: ITextCompletionService
// 명확성과 일관성 향상을 위한 재작성
// 품질: Very High, 비용: High
// 사용 사례: RAG 검색 최적화, 스타일 통일
```

#### 5. Enrich (보강)
```csharp
// LLM 필수: ITextCompletionService
// 추가 컨텍스트, 정의, 예시로 보강
// 품질: Very High, 비용: Very High
// 사용 사례: 검색 정확도 향상, 배경지식 추가
```

### Optional 서비스 처리

**서비스 가용성 경고 시스템**:

```csharp
// Factory 생성 시 자동 체크
var factory = new ReconstructStrategyFactory(
    llmService,  // null이면 INFO 로그
    logger
);
// LOG: "ITextCompletionService not registered.
//       LLM-based strategies will not be available."

// 전략 생성 시 WARNING
var strategy = factory.CreateStrategy("Summarize");
// LOG WARNING: "Strategy 'Summarize' requires ITextCompletionService,
//               but it is not registered. Consider using 'None' strategy."

// Auto 선택 시 자동 Fallback
var optimal = factory.CreateOptimalStrategy(content, options);
// LOG INFO: "ITextCompletionService not available.
//            Using 'None' strategy."
```

**예외 메시지**:
```csharp
// 서비스 없이 LLM 전략 실행 시
throw new InvalidOperationException(
    "ITextCompletionService is required for Summarize strategy. " +
    "Please register ITextCompletionService in your DI container, " +
    "or use ReconstructOptions.Strategy = \"None\".");
```

### 재구성 결과

```csharp
public class ReconstructedContent
{
    public string Url { get; set; }
    public string OriginalContent { get; set; }      // 원본 유지
    public string ReconstructedText { get; set; }    // 재구성된 텍스트

    // 재구성 정보
    public string StrategyUsed { get; set; }         // "None", "Summarize", etc.
    public List<ContentEnhancement> Enhancements { get; set; }

    // LLM 사용 정보
    public bool UsedLLM { get; set; }
    public string? LLMModel { get; set; }

    // 메타데이터 및 메트릭
    public WebContentMetadata Metadata { get; set; }
    public ReconstructMetrics Metrics { get; set; }

    public DateTime ReconstructedAt { get; set; }
}
```

**재구성 메트릭**:
```csharp
public class ReconstructMetrics
{
    public double Quality { get; set; }              // 재구성 품질
    public double CompressionRatio { get; set; }     // 압축/확장 비율
    public int EnhancementBytes { get; set; }        // 추가된 바이트
    public long ProcessingTimeMs { get; set; }
    public int LLMCallCount { get; set; }            // LLM 호출 횟수
    public int? TokensUsed { get; set; }             // 토큰 사용량
    public Dictionary<string, object> AdditionalMetrics { get; set; }
}
```

## 🎯 Stage 4: Chunk (청킹)

**목적**: 재구성된 콘텐츠를 RAG 최적화 청크로 분할

**입력**: `ReconstructedContent`
**출력**: `IEnumerable<WebContentChunk>`

**청킹 전략**:
1. **FixedSize**: 고정 크기 분할
2. **Paragraph**: 단락 경계 기준
3. **Smart**: 의미 경계 인식
4. **Semantic**: 임베딩 기반 (IEmbeddingService 필요)
5. **Intelligent**: ML 기반 최적화
6. **MemoryOptimized**: 84% 메모리 절감

## 🔧 파이프라인 사용 패턴

### 패턴 1: 전체 파이프라인 실행

```csharp
// 모든 단계 실행: Extract → Analyze → Reconstruct → Chunk
var pipeline = new WebContentPipeline(
    extractorFactory,
    analyzer,
    reconstructor,
    chunker
);

await foreach (var chunk in pipeline.ProcessAsync(url, options))
{
    // RAG에 바로 사용 가능한 청크
    await vectorDb.AddAsync(chunk);
}
```

### 패턴 2: 단계별 독립 실행

```csharp
// Stage 1: Extract만 실행
var extractor = extractorFactory.CreateExtractor(url);
var rawContent = await extractor.ExtractAsync(url);

// Stage 2: Analyze만 실행 (나중에)
var analyzer = new ContentAnalyzer();
var analyzedContent = await analyzer.AnalyzeAsync(rawContent, options);

// Stage 3: Reconstruct (LLM 서비스 있을 때)
var factory = new ReconstructStrategyFactory(llmService, logger);
var strategy = factory.CreateOptimalStrategy(analyzedContent, options);
var reconstructed = await strategy.ApplyAsync(analyzedContent, options);

// Stage 4: Chunk
var chunker = chunkingFactory.CreateStrategy("Smart");
var chunks = await chunker.ChunkAsync(reconstructed, chunkOptions);
```

### 패턴 3: 선택적 단계 실행

```csharp
// PipelineStageFlags로 단계 선택
var options = new PipelineOptions
{
    EnabledStages = PipelineStageFlags.Extract |
                   PipelineStageFlags.Analyze |
                   PipelineStageFlags.Chunk,  // Reconstruct 생략

    ReconstructOptions = new ReconstructOptions
    {
        Strategy = "None"  // 재구성 없음
    }
};

// Extract → Analyze → Chunk (Reconstruct 생략)
await foreach (var chunk in pipeline.ProcessAsync(url, options))
{
    // 빠른 처리, LLM 비용 0
}
```

### 패턴 4: LLM 없이 실행

```csharp
// ITextCompletionService 등록하지 않음
var services = new ServiceCollection();
services.AddWebFlux(options =>
{
    // LLM 서비스 등록 안함 (optional)
    // options.AddTextCompletionService<MyLLMService>();
});

// None 전략 자동 선택
var options = new ReconstructOptions { Strategy = "Auto" };
// LOG INFO: "ITextCompletionService not available. Using 'None' strategy."

var reconstructor = factory.CreateOptimalStrategy(content, options);
// None 전략 사용, 원본 품질 유지
```

## 📊 품질 및 성능 추적

### 단계별 메트릭

```csharp
public class PipelineMetrics
{
    // Extract 메트릭
    public int ExtractedBytes { get; set; }
    public int ImageCount { get; set; }
    public int LinkCount { get; set; }

    // Analyze 메트릭
    public double ContentQuality { get; set; }
    public int SectionCount { get; set; }
    public int NoiseRemovedBytes { get; set; }

    // Reconstruct 메트릭
    public string StrategyUsed { get; set; }
    public bool UsedLLM { get; set; }
    public int LLMCallCount { get; set; }
    public int TokensUsed { get; set; }
    public double CompressionRatio { get; set; }

    // Chunk 메트릭
    public int ChunkCount { get; set; }
    public int AvgChunkSize { get; set; }
    public double SemanticCoherence { get; set; }

    // 전체 파이프라인
    public long TotalProcessingTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
}
```

## 🎛️ 구성 옵션

### PipelineOptions

```csharp
public class PipelineOptions
{
    // 단계 제어
    public PipelineStageFlags EnabledStages { get; set; } = PipelineStageFlags.All;

    // 단계별 옵션
    public ExtractionOptions? ExtractionOptions { get; set; }
    public AnalysisOptions? AnalysisOptions { get; set; }
    public ReconstructOptions? ReconstructOptions { get; set; }
    public ChunkingOptions? ChunkingOptions { get; set; }

    // 성능 제어
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;
    public int BufferSize { get; set; } = 100;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

[Flags]
public enum PipelineStageFlags
{
    None = 0,
    Extract = 1,
    Analyze = 2,
    Reconstruct = 4,
    Chunk = 8,
    All = Extract | Analyze | Reconstruct | Chunk
}
```

## 🔐 오류 처리 및 복구

### 오류 레벨

**EXCEPTION (즉시 실패)**:
- 필수 파라미터 null/invalid
- 서비스 없이 LLM 전략 호출
- 복구 불가능한 시스템 오류

**WARNING (로그 + 계속)**:
- Optional 서비스 누락 → Fallback 전략
- 품질 임계값 미달 → 처리 완료하지만 경고
- 개별 이미지 처리 실패 → 이미지 스킵

**INFO (정상 동작)**:
- 전략 선택 및 생성
- 서비스 가용성 확인
- 처리 완료

### 복구 전략

```csharp
// 단계 실패 시 Fallback
try
{
    var reconstructed = await reconstructor.ApplyAsync(content, options);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("ITextCompletionService"))
{
    logger.LogWarning("LLM service not available, using None strategy");
    var none = new NoneReconstructStrategy();
    var reconstructed = await none.ApplyAsync(content, options);
}
```

## 🚀 성능 최적화

### 메모리 효율

- **스트리밍 처리**: `IAsyncEnumerable` 사용
- **청크 단위 처리**: 전체 로드 방지
- **백프레셔**: 메모리 압박 시 자동 조절

### 병렬 처리

- **단계별 파이프라인**: 각 단계 동시 실행
- **청크 병렬 생성**: 멀티코어 활용
- **비동기 I/O**: 네트워크 대기 최소화

### 캐싱 전략

- **원본 캐싱**: RawContent 재사용
- **분석 결과 캐싱**: AnalyzedContent 보존
- **LLM 결과 캐싱**: 토큰 비용 절감

## 📚 참고 문서

- [INTERFACES.md](./INTERFACES.md) - 인터페이스 상세 설명
- [CHUNKING_STRATEGIES.md](./CHUNKING_STRATEGIES.md) - 청킹 전략 가이드
- [MULTIMODAL_DESIGN.md](./MULTIMODAL_DESIGN.md) - 이미지 처리 설계
- [PERFORMANCE_DESIGN.md](./PERFORMANCE_DESIGN.md) - 성능 최적화 가이드
