# Chunking Strategies

WebFlux가 제공하는 청킹 전략과 선택 가이드입니다. 이 문서의 모든 전략·옵션은 코드에 실재하며, 등록되지 않은 전략은 싣지 않습니다.

## Overview

청킹 전략은 추출된 웹 콘텐츠를 RAG 시스템에 맞는 작은 단위로 나눕니다. 전략은 `ChunkingOptions.Strategy`(열거형 `ChunkingStrategyType`)로 고르거나, `IChunkingStrategyFactory.CreateStrategyAsync(name)`에 이름으로 요청합니다. **등록되지 않은 이름은 예외를 던집니다**(0.14.0 — 이전에는 조용히 Paragraph 로 대체했다). 사용 가능한 이름은 `GetAvailableStrategies()`가 돌려줍니다.

## Available Strategies

| Strategy | 동작 | 추가 요구사항 |
|----------|------|---------------|
| **Auto** | 콘텐츠를 분석해 아래 다섯 전략 중 하나를 고른다 (권장) | 없음 |
| **Smart** | HTML/Markdown 헤더 구조를 따라 분할 (WebFlux 자체 구현) | 없음 |
| **Semantic** | 임베딩 유사도로 경계를 정한다 (FluxCurator 위임) | FluxCurator 에 임베더 등록 필수 |
| **Paragraph** | 문단 경계에서 분할 (FluxCurator 위임) | 없음 |
| **FixedSize** | 토큰 기준 일정 크기로 분할 (FluxCurator 위임) | 없음 |
| **MemoryOptimized** | FixedSize 와 같은 토큰 기준 분할 — 대용량 문서용 이름으로 유지 | 없음 |

## Strategy Details

### Auto

`FixedSize`·`Paragraph`·`Smart`·`Semantic`·`MemoryOptimized` 각각에 점수를 매겨 최고점 전략을 실행합니다. 점수 요소는 문서 길이, 구조적 복잡도, 콘텐츠 유형, 이미지 유무·밀도, 기술 콘텐츠 여부, 해당 전략의 과거 성능 이력입니다. 선택된 점수는 로그(`Debug`)에 남습니다.

### Smart

HTML/Markdown 헤더를 경계로 삼아 섹션 맥락을 보존합니다. 헤더가 없으면 문단 분할로 동작합니다. 헤더를 청크 앞에 붙일지는 `ChunkingOptions.PreserveHeaders`가 정합니다.

### Semantic

FluxCurator 의 Semantic 청커에 위임합니다. **임베더가 등록되지 않은 구성에서는 `InvalidOperationException`을 던집니다** — 문단 분할로 조용히 대체하지 않습니다. 경계 민감도는 `ChunkingOptions.SemanticThreshold`로 조정합니다.

### Paragraph

빈 줄과 Markdown 헤딩 같은 자연스러운 문단 경계에서 분할합니다. 뉴스·블로그·에세이 같은 서술형 콘텐츠의 안정적인 기본값입니다.

### FixedSize / MemoryOptimized

둘 다 FluxCurator 의 토큰 기준 청커로 일정한 크기의 청크를 만듭니다. `MemoryOptimized`는 이름을 선택하던 소비자를 위해 유지되는 이름이며, 별도의 스트리밍·버퍼 동작은 없습니다.

## Selection Guide

```
Technical documentation, guides → Smart
General web pages, articles     → Paragraph, or Semantic (embedder required)
Markdown files                  → Paragraph or Smart
Uniform chunk size needed       → FixedSize
Unknown / mixed                 → Auto
```

## Configuration

`ChunkingOptions`의 실제 멤버:

| 멤버 | 기본값 | 의미 |
|---|---|---|
| `Strategy` | `ChunkingStrategyType.Auto` | 전략 선택 |
| `MaxChunkSize` | 512 | 최대 청크 크기 |
| `MinChunkSize` | 50 | 최소 청크 크기 |
| `ChunkOverlap` | 50 | 인접 청크 겹침 |
| `SemanticThreshold` | 0.7 | Semantic 경계 민감도 |
| `QualityThreshold` | 0.6 | 품질 임계값 |
| `PreserveHeaders` | `true` | 섹션 헤더를 청크 앞에 붙일지 |
| `MaxParallelism` | `Environment.ProcessorCount` | 병렬도 |
| `Language` | `"ko"` | 콘텐츠 언어 |
| `MinimizeMemoryUsage` | `false` | 메모리 최소화 전략 선택 힌트 |

멀티모달 관련 멤버(`EnableMultimodalProcessing`·`MultimodalOptions`·`IncludeImageDescriptions`)는 [README](../README.md) 의 멀티모달 절을 본다.

```csharp
var options = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Auto,
    MaxChunkSize = 512,
    ChunkOverlap = 64
};

await foreach (var chunk in processor.ProcessWebsiteAsync(url, new CrawlOptions { MaxPages = 100 }, options))
{
    await StoreChunkAsync(chunk);
}
```

## Troubleshooting

- **청크가 너무 작음** — `MinChunkSize`를 올리거나 Paragraph → Smart 로 바꾼다.
- **청크가 너무 큼** — `MaxChunkSize`를 줄이고 임베딩 모델의 입력 한도를 확인한다.
- **의미가 끊김** — `ChunkOverlap`을 늘리거나, 임베더가 있으면 Semantic 을 쓴다.
- **`Unknown chunking strategy` 예외** — 이름 오타이거나 0.14.0 에서 제거된 전략(`Intelligent`, `DomStructure`)이다. 메시지가 사용 가능한 이름을 나열한다.
- **Semantic 에서 `InvalidOperationException`** — FluxCurator 에 임베더가 등록되지 않았다.

## References

- [ARCHITECTURE.md](./ARCHITECTURE.md) - 시스템 설계
- [INTERFACES.md](./INTERFACES.md) - API 문서
