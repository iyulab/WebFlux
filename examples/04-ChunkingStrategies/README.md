# 예제 4: 청킹 전략 비교

## 개요
이 예제는 WebFlux SDK의 6가지 청킹 전략을 비교 분석하여 각 전략의 성능, 메모리 효율성, 품질을 측정합니다. 프로젝트에 가장 적합한 전략을 선택하는 데 도움이 됩니다.

## 주요 학습 포인트
1. **성능 벤치마킹**: 처리 시간, 메모리 사용량 측정
2. **품질 평가**: 청크 일관성, 의미적 완결성, 구조 보존
3. **전략 선택**: 사용 시나리오별 최적 전략 추천

## 실행 방법

```bash
cd examples/04-ChunkingStrategies
dotnet build
dotnet run
```

## 6가지 청킹 전략

### 1. FixedSize (고정 크기)
```
✅ 가장 빠른 처리 속도
✅ 예측 가능한 청크 크기
❌ 문장/문단 경계 무시
❌ 의미적 일관성 낮음
```
**사용 시나리오**: 실시간 처리, 성능 최우선

### 2. Paragraph (문단 기반)
```
✅ 자연스러운 텍스트 흐름
✅ 빠른 처리 속도
✅ 읽기 쉬운 청크
❌ 문단 크기 불균일
```
**사용 시나리오**: 뉴스, 블로그, 일반 텍스트

### 3. Smart (구조 인식)
```
✅ HTML/Markdown 헤딩 인식
✅ 코드 블록 보존
✅ 높은 품질
❌ 중간 수준 속도
```
**사용 시나리오**: 기술 문서, API 가이드

### 4. Semantic (의미론적)
```
✅ 최고 의미적 일관성
✅ RAG 품질 향상
❌ 느린 처리 속도
❌ 임베딩 서비스 필요
```
**사용 시나리오**: 학술 논문, 복잡한 텍스트

### 5. MemoryOptimized (메모리 최적화)
```
✅ 84% 메모리 절약
✅ 대용량 문서 처리
✅ 스트리밍 지원
❌ 약간 느린 속도
```
**사용 시나리오**: 대용량 문서(>1MB), 메모리 제약 환경

### 6. Auto (자동 선택)
```
✅ 콘텐츠별 최적 전략 자동 선택
✅ 메타데이터 활용
✅ 균형잡힌 성능/품질
❌ 예측 어려움
```
**사용 시나리오**: 다양한 문서 타입 자동 처리

## 예상 출력

```
=== WebFlux SDK - 청킹 전략 비교 예제 ===

테스트 문서 크기: 1,234 문자

┌────────────────────────────────────────────────────────────────────────────────────────┐
│  전략명           │  청크 수  │  처리 시간  │  메모리 사용  │  품질 점수  │
├────────────────────────────────────────────────────────────────────────────────────────┤
│  FixedSize       │       8  │      12.45ms  │        0.23MB  │       65.20  │
│  Paragraph       │       6  │      15.32ms  │        0.28MB  │       78.50  │
│  Smart           │       9  │      23.67ms  │        0.35MB  │       85.30  │
│  Semantic        │       7  │      45.89ms  │        0.52MB  │       92.10  │
│  MemoryOptimized │       8  │      18.23ms  │        0.15MB  │       72.40  │
│  Auto            │       8  │      19.45ms  │        0.27MB  │       81.60  │
└────────────────────────────────────────────────────────────────────────────────────────┘

📊 상세 분석:

⚡ 가장 빠른 전략: FixedSize (12.45ms)
💾 가장 메모리 효율적: MemoryOptimized (0.15MB)
✨ 가장 높은 품질: Semantic (점수: 92.10)

📋 전략별 특성:

▶ FixedSize
   청크 크기 범위: 485 ~ 512 문자
   평균 청크 크기: 502 문자
   표준 편차: 8 (일관성: 매우 높음)
   권장 사용: 빠른 처리가 필요한 실시간 시스템

▶ Paragraph
   청크 크기 범위: 234 ~ 678 문자
   평균 청크 크기: 456 문자
   표준 편차: 152 (일관성: 낮음)
   권장 사용: 일반 텍스트 문서 (뉴스, 블로그)

▶ Smart
   청크 크기 범위: 312 ~ 589 문자
   평균 청크 크기: 478 문자
   표준 편차: 95 (일관성: 높음)
   권장 사용: 기술 문서, API 가이드 (구조 인식 필요)

▶ Semantic
   청크 크기 범위: 389 ~ 524 문자
   평균 청크 크기: 462 문자
   표준 편차: 62 (일관성: 매우 높음)
   권장 사용: 학술 논문, 복잡한 텍스트 (의미 보존 중요)

▶ MemoryOptimized
   청크 크기 범위: 467 ~ 512 문자
   평균 청크 크기: 495 문자
   표준 편차: 18 (일관성: 매우 높음)
   권장 사용: 대용량 문서, 메모리 제약 환경

▶ Auto
   청크 크기 범위: 298 ~ 534 문자
   평균 청크 크기: 471 문자
   표준 편차: 87 (일관성: 높음)
   권장 사용: 다양한 문서 타입 자동 처리

💡 시나리오별 추천 전략:

📚 일반 텍스트 (뉴스, 블로그):
   1순위: Paragraph - 자연스러운 문단 보존
   2순위: FixedSize - 빠른 처리 속도

📖 기술 문서 (API, 가이드):
   1순위: Smart - 헤딩 구조 인식
   2순위: Auto - 자동 최적 전략 선택

🎓 학술 논문:
   1순위: Semantic - 의미적 일관성
   2순위: Smart - 구조 보존

💾 대용량 문서 (>1MB):
   1순위: MemoryOptimized - 84% 메모리 절약
   2순위: Auto - 자동 메모리 최적화

🚀 성능 우선 (실시간 처리):
   1순위: FixedSize - 최고 속도
   2순위: Paragraph - 빠르고 자연스러움

🎯 품질 우선 (RAG 정확도):
   1순위: Semantic - 최고 의미적 일관성
   2순위: Smart - 구조 보존 + 높은 품질

📈 성능 비교 차트:

처리 시간 (상대적):
  FixedSize        ██████████ 12.45ms
  Paragraph        ████████████ 15.32ms
  MemoryOptimized  ██████████████ 18.23ms
  Auto             ███████████████ 19.45ms
  Smart            ███████████████████ 23.67ms
  Semantic         ████████████████████████████████████ 45.89ms

메모리 사용 (상대적):
  MemoryOptimized  ████████████████ 0.15MB
  FixedSize        ████████████████████████ 0.23MB
  Auto             ████████████████████████████ 0.27MB
  Paragraph        ██████████████████████████████ 0.28MB
  Smart            ████████████████████████████████████ 0.35MB
  Semantic         ██████████████████████████████████████████████████ 0.52MB
```

## 코드 설명

### 1. 전략 목록 정의
```csharp
var strategies = new Dictionary<string, IChunkingStrategy>
{
    ["FixedSize"] = new FixedSizeChunkingStrategy(),
    ["Paragraph"] = new ParagraphChunkingStrategy(),
    ["Smart"] = new SmartChunkingStrategy(),
    ["Semantic"] = new SemanticChunkingStrategy(),
    ["MemoryOptimized"] = new MemoryOptimizedChunkingStrategy(),
    ["Auto"] = new AutoChunkingStrategy()
};
```

### 2. 성능 측정
```csharp
// 메모리 측정
var initialMemory = GC.GetTotalMemory(false);

// 시간 측정
var stopwatch = Stopwatch.StartNew();
var chunks = await strategy.ChunkAsync(content, options);
stopwatch.Stop();

// 메모리 사용량 계산
var memoryUsed = (GC.GetTotalMemory(false) - initialMemory) / (1024.0 * 1024.0);
```

### 3. 품질 점수 계산
```csharp
double qualityScore = 0;

// 1. 크기 일관성 (30%)
score += sizeConsistency * 30;

// 2. 의미적 완결성 (40%) - 문장 종결 확인
score += sentenceCompleteness * 40;

// 3. 구조 보존 (30%) - 헤딩/메타데이터 보존
score += structurePreservation * 30;
```

## 전략 선택 가이드

### 결정 트리
```
문서 크기 > 1MB?
  ├─ Yes → MemoryOptimized 또는 Auto
  └─ No
      ├─ 구조가 중요? (헤딩, 코드 블록)
      │   ├─ Yes → Smart
      │   └─ No
      │       ├─ 의미 보존 중요?
      │       │   ├─ Yes → Semantic
      │       │   └─ No
      │       │       ├─ 속도 우선?
      │       │           ├─ Yes → FixedSize
      │       │           └─ No → Paragraph
      └─ 문서 타입 다양?
          └─ Yes → Auto
```

## 성능 최적화 팁

### 1. 적절한 청크 크기 설정
```csharp
// 짧은 문서 (뉴스)
MaxChunkSize = 256-512

// 중간 문서 (블로그)
MaxChunkSize = 512-1024

// 긴 문서 (기술 가이드)
MaxChunkSize = 1024-2048
```

### 2. 겹침 최적화
```csharp
// 낮은 겹침 (속도 우선)
ChunkOverlap = 32

// 균형
ChunkOverlap = 64

// 높은 겹침 (품질 우선)
ChunkOverlap = 128
```

## 다음 단계
- [예제 5: 커스텀 서비스](../05-CustomServices) - 자체 청킹 전략 구현

## 참고 자료
- [청킹 전략 가이드](../../docs/CHUNKING_STRATEGIES.md)
- [성능 최적화 가이드](../../docs/PERFORMANCE_DESIGN.md)
- [품질 평가 기준](../../docs/CHUNKING_STRATEGIES.md#quality-metrics)
