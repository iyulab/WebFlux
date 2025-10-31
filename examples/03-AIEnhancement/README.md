# 예제 3: AI Enhancement (OpenAI 통합)

## 개요
이 예제는 OpenAI API를 사용하여 크롤링된 콘텐츠를 요약하고, 키워드를 추출하며, 관련 질문을 생성하는 방법을 보여줍니다. RAG 시스템의 품질을 크게 향상시킬 수 있습니다.

## 주요 학습 포인트
1. **OpenAI 통합**: GPT 모델을 사용한 콘텐츠 향상
2. **다국어 처리**: 영문 콘텐츠를 한국어로 요약
3. **메타데이터 추출**: 키워드 및 관련 질문 자동 생성
4. **비용 최적화**: 토큰 사용량 추적 및 비용 분석

## 실행 방법

### 필수 조건
- .NET 8.0 이상
- WebFlux NuGet 패키지
- OpenAI API 키 (https://platform.openai.com/api-keys)

### 환경 변수 설정
```bash
# Windows
setx OPENAI_API_KEY "sk-your-api-key-here"

# Linux/Mac
export OPENAI_API_KEY="sk-your-api-key-here"
```

### 빌드 및 실행
```bash
cd examples/03-AIEnhancement
dotnet build
dotnet run
```

## AI Enhancement 기능

### 1. 자동 요약
```
원본: C# 12 introduces several new features that improve developer productivity...
     (1500+ characters)

요약: C# 12는 개발자 생산성을 향상시키는 여러 새로운 기능을 도입했습니다.
     주요 기능으로는 Primary Constructors, Collection Expressions,
     그리고 개선된 Lambda 표현식이 있습니다.
```

### 2. 키워드 추출
```
키워드: C# 12, Primary Constructors, Collection Expressions,
       Lambda, Record Types, Pattern Matching
```

### 3. 관련 질문 생성
```
- C# 12의 Primary Constructors는 어떻게 작동하나요?
- Collection Expressions를 언제 사용해야 하나요?
- C# 11과 비교했을 때 가장 큰 변화는 무엇인가요?
```

## 코드 설명

### 1. OpenAI 서비스 등록
```csharp
services.AddSingleton<ITextCompletionService>(sp =>
    new OpenAITextCompletionService(apiKey, "gpt-4o-mini"));

services.AddSingleton<IAiEnhancementService, BasicAiEnhancementService>();
```

**모델 선택 가이드**:
- `gpt-4o-mini`: 비용 효율적, 일반 요약/번역
- `gpt-4o`: 고품질, 복잡한 분석
- `gpt-3.5-turbo`: 저비용, 간단한 작업

### 2. AI 향상 옵션
```csharp
var enhancementOptions = new AiEnhancementOptions
{
    GenerateSummary = true,
    ExtractKeywords = true,
    GenerateQuestions = true,
    TranslateToLanguage = "ko",  // 한국어로 번역
    MaxSummaryLength = 200
};
```

### 3. 콘텐츠 향상 실행
```csharp
var enhanced = await aiEnhancement.EnhanceContentAsync(
    chunk.Content,
    enhancementOptions
);

Console.WriteLine(enhanced.Summary);
Console.WriteLine(string.Join(", ", enhanced.Keywords));
```

## 예상 출력

```
=== WebFlux SDK - AI Enhancement 예제 ===

✅ OpenAI API 키 확인 완료

AI 향상 크롤링 시작: 1개 페이지

📡 웹 페이지 크롤링 중...

📄 URL: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12
   제목: What's new in C# 12
   청크 수: 45

🤖 AI 콘텐츠 향상 중...

청크 1/3:
원본 (영문, 1234자):
C# 12 introduces several new features that improve developer productivity
and code quality. The primary constructors feature allows you to declare
constructor parameters directly in the class or struct declaration...

✨ AI 향상 결과:
📝 요약 (한국어):
   C# 12는 개발자 생산성과 코드 품질을 향상시키는 여러 새로운 기능을 도입합니다.
   Primary Constructors를 통해 클래스 선언에서 직접 생성자 매개변수를 선언할 수 있으며,
   Collection Expressions를 사용하여 더 간결한 컬렉션 초기화가 가능합니다.

🔑 키워드:
   C# 12, Primary Constructors, Collection Expressions, Developer Productivity,
   Code Quality, Lambda Expressions

❓ 관련 질문:
   - Primary Constructors는 기존 생성자와 어떻게 다른가요?
   - Collection Expressions를 사용하면 어떤 이점이 있나요?
   - C# 12의 새로운 기능을 기존 프로젝트에 적용하려면 어떻게 해야 하나요?

처리 시간: 2.34초
토큰 사용: 1,456 토큰

--------------------------------------------------------------------------------

청크 2/3:
...

📊 전체 문서 요약 생성 중...

📄 전체 문서 요약 (한국어):
C# 12는 .NET 8과 함께 출시되어 개발자 생산성을 크게 향상시키는 혁신적인 기능들을
도입했습니다. Primary Constructors는 클래스와 구조체에서 생성자 매개변수를 간결하게
선언할 수 있게 하고, Collection Expressions는 컬렉션 초기화를 더 직관적으로 만듭니다.
또한, 개선된 Lambda 표현식, Record Types 향상, 그리고 Pattern Matching 기능 확대로
더 표현력 있고 안전한 코드를 작성할 수 있습니다. 이러한 기능들은 특히 최신 웹
애플리케이션과 클라우드 네이티브 개발에 최적화되어 있습니다.

💰 AI 처리 비용 분석:
   총 토큰 사용: 4,523 토큰
   예상 비용 (gpt-4o-mini): $0.0007
   청크당 평균: 1,508 토큰

✅ AI 향상 완료!
```

## 비용 최적화 전략

### 1. 모델 선택
```csharp
// 저비용 (권장)
new OpenAITextCompletionService(apiKey, "gpt-4o-mini");  // $0.15/1M tokens

// 고품질
new OpenAITextCompletionService(apiKey, "gpt-4o");  // $5.00/1M tokens
```

### 2. 청크 크기 최적화
```csharp
var chunkingOptions = new ChunkingOptions
{
    MaxChunkSize = 1024,  // 너무 크면 비용 증가
    MinChunkSize = 200,   // 너무 작으면 맥락 손실
};
```

**권장 사항**:
- 요약: 1024-2048자 청크
- 키워드 추출: 512-1024자 청크
- 번역: 2048-4096자 청크

### 3. 선택적 처리
```csharp
// 모든 청크가 아닌 중요한 청크만 AI 처리
var chunksToEnhance = result.Chunks
    .Where(c => c.Metadata.ContainsKey("IsImportant"))
    .Take(10)  // 상위 10개만
    .ToList();
```

### 4. 배치 처리
```csharp
// 여러 청크를 하나의 요청으로 처리
var combinedContent = string.Join("\n\n", chunks.Select(c => c.Content));
var enhanced = await aiEnhancement.EnhanceContentAsync(combinedContent, options);
```

## 고급 사용 사례

### 다국어 문서 처리
```csharp
var enhancementOptions = new AiEnhancementOptions
{
    GenerateSummary = true,
    TranslateToLanguage = "ko",  // 또는 "ja", "zh", "es", "fr" 등
    PreserveCodeBlocks = true,   // 코드 블록은 번역하지 않음
};
```

### 기술 문서 분석
```csharp
var enhancementOptions = new AiEnhancementOptions
{
    ExtractKeywords = true,
    GenerateQuestions = true,
    IdentifyCodeExamples = true,  // 코드 예제 식별
    GenerateTechStack = true,      // 기술 스택 추출
};
```

### RAG 품질 향상
```csharp
// 1. 요약으로 검색 정확도 향상
chunk.Metadata["Summary"] = enhanced.Summary;

// 2. 키워드로 검색 범위 확대
chunk.Metadata["Keywords"] = enhanced.Keywords;

// 3. 질문으로 사용자 경험 개선
chunk.Metadata["SuggestedQuestions"] = enhanced.SuggestedQuestions;
```

## 문제 해결

### Q: "Incorrect API key provided" 오류
A: OpenAI API 키를 확인하세요:
1. https://platform.openai.com/api-keys 에서 새 키 생성
2. 환경 변수 재설정
3. IDE 재시작 (환경 변수 반영 위해)

### Q: "Rate limit exceeded" 오류
A: API 속도 제한에 도달했습니다:
```csharp
// 청크 간 대기 시간 추가
await Task.Delay(2000);  // 2초 대기
```

### Q: 비용이 너무 높습니다
A: 다음을 시도하세요:
1. `gpt-4o-mini` 모델 사용
2. 청크 크기 줄이기
3. 중요한 청크만 AI 처리
4. 배치 처리로 요청 수 감소

### Q: 요약 품질이 낮습니다
A: 다음을 시도하세요:
1. `gpt-4o` 모델로 업그레이드
2. 청크 크기 늘리기 (더 많은 맥락)
3. `MaxSummaryLength` 늘리기
4. 프롬프트 엔지니어링 개선

## 다음 단계
- [예제 4: 청킹 전략 비교](../04-ChunkingStrategies) - 다양한 전략 성능 비교
- [예제 5: 커스텀 서비스](../05-CustomServices) - 자체 AI 서비스 구현

## 참고 자료
- [OpenAI API 문서](https://platform.openai.com/docs)
- [WebFlux AI 통합 가이드](../../docs/MULTIMODAL_DESIGN.md)
- [토큰 최적화 가이드](../../docs/PERFORMANCE_DESIGN.md#token-optimization)
