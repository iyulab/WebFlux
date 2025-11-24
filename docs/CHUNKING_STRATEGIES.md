# Chunking Strategies

WebFlux가 제공하는 청킹 전략과 선택 가이드입니다.

## Overview

청킹 전략은 콘텐츠를 RAG 시스템에 최적화된 작은 단위로 분할하는 알고리즘입니다. 콘텐츠 유형, 크기, 용도에 따라 적절한 전략을 선택할 수 있습니다.

## Available Strategies

| Strategy | Use Case | AI Required |
|----------|----------|-------------|
| **Auto** | 자동 선택 (권장) | No |
| **Smart** | 구조화된 HTML 문서 | No |
| **Semantic** | 일반 웹페이지, 기사 | Yes (Embedding) |
| **Intelligent** | 블로그, 지식베이스 | Yes (LLM) |
| **MemoryOptimized** | 대용량 문서 (메모리 제약) | No |
| **Paragraph** | Markdown, 자연스러운 경계 | No |
| **FixedSize** | 균일한 크기, 테스트용 | No |
| **DomStructure** | HTML DOM 시맨틱 경계 기반 청킹 | No |

## Strategy Details

### Auto Strategy

콘텐츠 분석을 통해 최적의 전략을 자동 선택합니다.

**선택 알고리즘**:
```
if (hasImages && imageRatio > 0.3) → Smart
if (pageCount > 50 || contentLength > 100KB) → MemoryOptimized
if (hasTechnicalContent) → Semantic
if (isMarkdown || hasNaturalParagraphs) → Paragraph
else → Intelligent
```

**사용**:
```csharp
var options = new ChunkingOptions
{
    Strategy = "Auto",
    MaxChunkSize = 512,
    OverlapSize = 64
};
```

### Smart Strategy

HTML 구조를 분석하여 의미 있는 경계에서 분할합니다.

**특징**:
- HTML 태그 구조 인식 (h1-h6, section, article)
- 코드 블록 보존
- 테이블, 리스트 완전성 유지
- 이미지 캡션 연결

**적합한 콘텐츠**:
- API 문서
- 기술 문서
- 구조화된 가이드

### Semantic Strategy

임베딩 기반으로 의미적으로 유사한 문장을 그룹화합니다.

**요구사항**:
- `ITextEmbeddingService` 구현 필수

**알고리즘**:
1. 문장 단위 분리
2. 각 문장 임베딩 생성
3. 코사인 유사도 계산
4. 유사도 임계값으로 경계 결정

**적합한 콘텐츠**:
- 일반 웹페이지
- 뉴스 기사
- 블로그 포스트

### Intelligent Strategy

LLM을 사용하여 내용을 분석하고 최적의 분할 지점을 결정합니다.

**요구사항**:
- `ITextCompletionService` 구현 필수

**기능**:
- 주제 전환 감지
- 문맥 완전성 보장
- 의미적 경계 인식

**적합한 콘텐츠**:
- 복잡한 설명문
- 학술 자료
- 긴 형식의 콘텐츠

### MemoryOptimized Strategy

메모리 사용을 최소화하면서 대용량 문서를 처리합니다.

**특징**:
- 스트리밍 처리
- 버퍼 크기 제한
- 백프레셔 제어
- 84% 메모리 사용량 감소

**적합한 콘텐츠**:
- 50페이지 이상 문서
- 메모리 제약 환경
- 배치 처리

### Paragraph Strategy

자연스러운 단락 경계에서 분할합니다.

**경계 인식**:
- 빈 줄 (double newline)
- Markdown 헤딩
- HTML 단락 태그

**적합한 콘텐츠**:
- Markdown 문서
- 소설, 에세이
- 자연스러운 단락 구조

### FixedSize Strategy

고정된 문자 수로 균일하게 분할합니다.

**특징**:
- 예측 가능한 청크 크기
- 간단한 구현
- 빠른 처리 속도

**적합한 콘텐츠**:
- 테스트 및 벤치마크
- 단순한 텍스트
- 균일한 크기 요구사항

### DomStructure Strategy

HTML DOM 구조를 분석하여 시맨틱 경계에서 분할합니다.

**특징**:
- HTML 시맨틱 태그 인식 (section, article, aside, nav)
- 헤딩 계층 구조 보존 (h1-h6 경로 추적)
- 코드 블록, 테이블, 리스트 완전성 유지
- 작은 청크 자동 병합

**적합한 콘텐츠**:
- 구조화된 HTML 문서
- 기술 문서 사이트
- 블로그 포스트
- API 문서

## Selection Guide

### By Content Type

```
Technical Documentation → Smart or DomStructure
General Web Pages → Semantic
Blog Posts → Intelligent or DomStructure
Markdown Files → Paragraph
Large Documents → MemoryOptimized
Structured HTML → DomStructure
Testing → FixedSize
Unknown/Mixed → Auto
```

### By Requirements

**AI Services Available**:
- Embedding만: Semantic
- LLM만: Intelligent
- 둘 다: Auto, Smart, Semantic, Intelligent 중 선택
- 없음: Auto, Smart, MemoryOptimized, Paragraph, FixedSize

**Memory Constraints**:
- 제약 있음: MemoryOptimized
- 제약 없음: 콘텐츠 유형에 따라 선택

**Processing Speed Priority**:
- 빠른 처리: FixedSize, Paragraph
- 품질 우선: Semantic, Intelligent

## Configuration

### Basic Options

```csharp
var options = new ChunkingOptions
{
    Strategy = "Auto",           // 전략 선택
    MaxChunkSize = 512,          // 최대 청크 크기 (토큰)
    MinChunkSize = 100,          // 최소 청크 크기
    OverlapSize = 64             // 인접 청크 오버랩
};
```

### Advanced Options

```csharp
var options = new ChunkingOptions
{
    Strategy = "Semantic",
    MaxChunkSize = 512,
    OverlapSize = 64,

    // Semantic 전용
    SimilarityThreshold = 0.75,   // 유사도 임계값

    // Intelligent 전용
    ContextWindowSize = 2048,     // LLM 컨텍스트 윈도우

    // MemoryOptimized 전용
    BufferSizeBytes = 1024 * 1024 // 1MB 버퍼
};
```

## Usage Examples

### Auto Strategy (권장)

```csharp
var processor = provider.GetRequiredService<IWebContentProcessor>();

var chunks = await processor.ProcessUrlAsync(
    "https://example.com",
    new ChunkingOptions { Strategy = "Auto" }
);
```

### Semantic Strategy

```csharp
// ITextEmbeddingService 필요
services.AddScoped<ITextEmbeddingService, YourEmbeddingService>();

var chunks = await processor.ProcessUrlAsync(
    url,
    new ChunkingOptions
    {
        Strategy = "Semantic",
        MaxChunkSize = 512,
        SimilarityThreshold = 0.8
    }
);
```

### Memory-Optimized for Large Documents

```csharp
await foreach (var chunk in processor.ProcessWebsiteAsync(
    url,
    new CrawlOptions { MaxPages = 1000 },
    new ChunkingOptions
    {
        Strategy = "MemoryOptimized",
        MaxChunkSize = 512,
        BufferSizeBytes = 512 * 1024 // 512KB 버퍼
    }
))
{
    // 청크 생성 즉시 처리
    await StoreChunkAsync(chunk);
}
```

## Performance Characteristics

| Strategy | Speed | Quality | Memory | AI Required |
|----------|-------|---------|--------|-------------|
| Auto | Medium | High | Medium | Optional |
| Smart | Fast | High | Low | No |
| Semantic | Medium | Very High | Medium | Yes (Embedding) |
| Intelligent | Slow | Very High | Medium | Yes (LLM) |
| MemoryOptimized | Fast | Medium | Very Low | No |
| Paragraph | Fast | Medium | Low | No |
| FixedSize | Very Fast | Low | Very Low | No |
| DomStructure | Fast | High | Low | No |

## Best Practices

### 1. Start with Auto
처음에는 Auto 전략으로 시작하여 결과를 평가한 후, 필요시 특정 전략으로 전환합니다.

### 2. Consider AI Costs
Semantic, Intelligent 전략은 AI 서비스 호출 비용이 발생합니다. 비용과 품질을 고려하여 선택하세요.

### 3. Test with Sample Data
실제 콘텐츠 샘플로 여러 전략을 테스트하여 최적의 전략을 찾으세요.

### 4. Monitor Performance
처리 시간, 메모리 사용량, 청크 품질을 모니터링하여 전략을 조정하세요.

### 5. Optimize Overlap
Overlap 크기는 검색 정확도에 영향을 줍니다:
- 적은 오버랩 (32-64): 빠른 처리, 낮은 중복
- 많은 오버랩 (128-256): 느린 처리, 높은 검색 정확도

## Troubleshooting

### 청크가 너무 작음
- `MinChunkSize` 증가
- 다른 전략 시도 (예: Paragraph → Smart)

### 청크가 너무 큼
- `MaxChunkSize` 감소
- 임베딩 모델 컨텍스트 크기 확인

### 의미가 끊김
- Overlap 크기 증가
- Semantic 또는 Intelligent 전략 사용

### 처리 속도가 느림
- MemoryOptimized 또는 FixedSize 전략 사용
- 병렬 처리 증가
- AI 서비스 없는 전략 사용

### 메모리 부족
- MemoryOptimized 전략 사용
- BufferSize 감소
- 스트리밍 처리 활성화

## References

- [ARCHITECTURE.md](./ARCHITECTURE.md) - 시스템 설계
- [INTERFACES.md](./INTERFACES.md) - API 문서
