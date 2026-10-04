# 예제 5: 커스텀 서비스 구현

## 개요
이 예제는 WebFlux SDK의 인터페이스를 구현하여 자체 청킹 전략과 AI 서비스를 만드는 방법을 보여줍니다. 프로젝트의 특수한 요구사항에 맞춘 커스터마이징이 가능합니다.

## 주요 학습 포인트
1. **IChunkingStrategy 구현**: 문장 기반 커스텀 청킹 전략
2. **ITextCompletionService 구현**: 규칙 기반 텍스트 완성 서비스
3. **의존성 주입**: 커스텀 서비스를 DI 컨테이너에 등록
4. **도메인 특화**: 업계/프로젝트 특성에 맞춘 커스터마이징

## 실행 방법

```bash
cd examples/05-CustomServices
dotnet build
dotnet run
```

## 구현된 커스텀 서비스

### 1. SentenceBasedChunkingStrategy
문장 경계를 엄격히 준수하는 청킹 전략:
- 문장을 절대 분할하지 않음
- MaxChunkSize 내에서 최대한 많은 문장 포함
- 문장 수를 메타데이터에 저장

### 2. SimpleTextCompletionService
규칙 기반 텍스트 요약 서비스:
- 첫 번째 문장 추출
- 키워드 자동 추출 (빈도 기반)
- 외부 API 의존성 없음

## 예상 출력

```
=== WebFlux SDK - 커스텀 서비스 구현 예제 ===

📄 테스트 콘텐츠:

WebFlux is a powerful SDK for web content processing. It provides multiple chunking strategies.
The SDK supports RAG preprocessing. You can implement custom services easily.
Integration with AI providers is straightforward. The architecture is clean and extensible.

🔧 커스텀 청킹 전략 (문장 기반):

생성된 청크 수: 3

청크 1:
  내용: WebFlux is a powerful SDK for web content processing. It provides multiple chunking strategies.
  크기: 98 문자
  문장 수: 2

청크 2:
  내용: The SDK supports RAG preprocessing. You can implement custom services easily.
  크기: 86 문자
  문장 수: 2

청크 3:
  내용: Integration with AI providers is straightforward. The architecture is clean and extensible.
  크기: 97 문자
  문장 수: 2

🤖 커스텀 AI 서비스 (간단한 요약):

원본: WebFlux is a powerful SDK for web content processing. It provides multiple chunking strategies.
요약: WebFlux is a powerful SDK for web content processing. 주요 키워드: webflux, processing, powerful.

원본: The SDK supports RAG preprocessing. You can implement custom services easily.
요약: The SDK supports RAG preprocessing. 주요 키워드: supports, preprocessing, implement.

💡 커스텀 서비스 활용 사례:

✅ 문장 기반 청킹: 문장 경계를 엄격히 준수
✅ 간단한 AI 서비스: Mock 대신 실제 로직 구현
✅ 도메인 특화: 업계/프로젝트 특성에 맞춤
✅ 확장성: WebFlux 인터페이스 기반 자유로운 확장
```

## 코드 설명

### 1. IChunkingStrategy 구현
```csharp
public class SentenceBasedChunkingStrategy : IChunkingStrategy
{
    public Task<List<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions options)
    {
        var sentences = SplitIntoSentences(content.Text);
        var chunks = new List<WebContentChunk>();

        // 문장을 MaxChunkSize 내에서 그룹화
        var currentChunk = new List<string>();
        int currentSize = 0;

        foreach (var sentence in sentences)
        {
            if (currentSize + sentence.Length <= options.MaxChunkSize)
            {
                currentChunk.Add(sentence);
                currentSize += sentence.Length;
            }
            else
            {
                // 현재 청크 완성, 새 청크 시작
                chunks.Add(CreateChunk(currentChunk, content));
                currentChunk = new List<string> { sentence };
                currentSize = sentence.Length;
            }
        }

        return Task.FromResult(chunks);
    }
}
```

### 2. ITextCompletionService 구현
```csharp
public class SimpleTextCompletionService : ITextCompletionService
{
    public Task<string> CompleteAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // 규칙 기반 요약
        var text = ExtractTextFromPrompt(prompt);
        var firstSentence = text.Split('.')[0];
        var keywords = ExtractKeywords(text);

        var summary = $"{firstSentence}. 주요 키워드: {string.Join(", ", keywords)}";
        return Task.FromResult(summary);
    }
}
```

### 3. 서비스 등록
```csharp
services.AddSingleton<IChunkingStrategy, SentenceBasedChunkingStrategy>();
services.AddSingleton<ITextCompletionService, SimpleTextCompletionService>();
```

## 커스텀 서비스 구현 가이드

### 청킹 전략 구현 단계

#### 1. IChunkingStrategy 인터페이스 구현
```csharp
public class MyCustomChunkingStrategy : IChunkingStrategy
{
    public Task<List<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions options)
    {
        // 커스텀 청킹 로직
        var chunks = new List<WebContentChunk>();

        // TODO: 텍스트를 청크로 분할

        return Task.FromResult(chunks);
    }
}
```

#### 2. WebContentChunk 생성
```csharp
private WebContentChunk CreateChunk(string content, ExtractedContent source, int index)
{
    return new WebContentChunk
    {
        Content = content,
        ChunkIndex = index,
        SourceUrl = source.Url,
        Metadata = new Dictionary<string, object>
        {
            ["CustomField"] = "value",
            ["Strategy"] = "MyCustom"
        }
    };
}
```

#### 3. 서비스 등록
```csharp
services.AddSingleton<IChunkingStrategy, MyCustomChunkingStrategy>();
```

### AI 서비스 구현 단계

#### 1. ITextCompletionService 인터페이스 구현
```csharp
public class MyAIService : ITextCompletionService
{
    private readonly HttpClient _httpClient;

    public MyAIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> CompleteAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // 외부 AI API 호출 또는 로컬 모델 실행
        var response = await _httpClient.PostAsync(
            "https://your-ai-api.com/complete",
            new StringContent(prompt),
            cancellationToken);

        return await response.Content.ReadAsStringAsync();
    }
}
```

#### 2. 서비스 등록
```csharp
services.AddHttpClient<ITextCompletionService, MyAIService>();
```

## 실전 사용 사례

### 사례 1: 법률 문서 청킹
```csharp
public class LegalDocumentChunkingStrategy : IChunkingStrategy
{
    // 법률 조항 단위로 청킹
    public Task<List<WebContentChunk>> ChunkAsync(...)
    {
        // "제1조", "제2조" 패턴 인식
        // 조항 경계에서만 분할
        // 조항 번호를 메타데이터에 저장
    }
}
```

### 사례 2: 코드 문서 청킹
```csharp
public class CodeDocumentationChunkingStrategy : IChunkingStrategy
{
    // 함수/클래스 단위로 청킹
    public Task<List<WebContentChunk>> ChunkAsync(...)
    {
        // 코드 블록 경계 인식
        // 함수 시그니처 보존
        // 주석과 코드 함께 그룹화
    }
}
```

### 사례 3: 다국어 AI 서비스
```csharp
public class MultilingualAIService : ITextCompletionService
{
    public async Task<string> CompleteAsync(string prompt, ...)
    {
        // 언어 감지
        var language = DetectLanguage(prompt);

        // 언어별 모델 선택
        var model = SelectModelForLanguage(language);

        // 번역 및 처리
        return await ProcessWithModel(prompt, model);
    }
}
```

## 고급 패턴

### 전략 패턴과 팩토리
```csharp
public interface IChunkingStrategyFactory
{
    IChunkingStrategy CreateStrategy(string strategyType);
}

public class CustomStrategyFactory : IChunkingStrategyFactory
{
    public IChunkingStrategy CreateStrategy(string strategyType)
    {
        return strategyType switch
        {
            "legal" => new LegalDocumentChunkingStrategy(),
            "code" => new CodeDocumentationChunkingStrategy(),
            "sentence" => new SentenceBasedChunkingStrategy(),
            _ => throw new ArgumentException("Unknown strategy")
        };
    }
}
```

### 데코레이터 패턴
```csharp
public class CachingChunkingStrategy : IChunkingStrategy
{
    private readonly IChunkingStrategy _innerStrategy;
    private readonly ICacheService _cache;

    public async Task<List<WebContentChunk>> ChunkAsync(...)
    {
        var cacheKey = GenerateCacheKey(content);

        if (_cache.TryGet(cacheKey, out var cachedChunks))
            return cachedChunks;

        var chunks = await _innerStrategy.ChunkAsync(content, options);
        _cache.Set(cacheKey, chunks);

        return chunks;
    }
}
```

## 테스트 전략

### 단위 테스트
```csharp
[Fact]
public async Task SentenceBasedStrategy_ShouldNotSplitSentences()
{
    // Arrange
    var strategy = new SentenceBasedChunkingStrategy();
    var content = new ExtractedContent
    {
        Text = "First sentence. Second sentence. Third sentence."
    };

    // Act
    var chunks = await strategy.ChunkAsync(content, new ChunkingOptions
    {
        MaxChunkSize = 50
    });

    // Assert
    foreach (var chunk in chunks)
    {
        var sentences = chunk.Content.Split('.');
        Assert.All(sentences, s => Assert.True(s.Length < 50));
    }
}
```

## 다음 단계
- [WebFlux README](../../README.md)
- [인터페이스 가이드](../../docs/INTERFACES.md)
- [아키텍처 설계](../../docs/ARCHITECTURE.md)

## 참고 자료
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [의존성 역전 원칙](https://en.wikipedia.org/wiki/Dependency_inversion_principle)
- [전략 패턴](https://refactoring.guru/design-patterns/strategy)
