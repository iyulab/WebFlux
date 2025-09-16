# WebFlux SDK 인터페이스 설계

> 확장 가능하고 AI 공급자 중립적인 인터페이스 계약 정의

## 🎯 인터페이스 설계 원칙

### 1. 인터페이스 제공자 패턴
WebFlux SDK는 핵심 기능의 **인터페이스만 정의**하고, 구체적인 구현체는 소비 애플리케이션이 제공하는 패턴을 채택합니다.

#### ✅ WebFlux가 제공하는 인터페이스
- `ITextCompletionService` - LLM 텍스트 완성
- `IImageToTextService` - 이미지-텍스트 변환
- `IEmbeddingService` - 텍스트 임베딩 생성 (선택적)

#### ✅ WebFlux가 구현하는 인터페이스
- `IWebContentProcessor` - 메인 처리 파이프라인
- `ICrawler` - 웹 크롤링 전략
- `IContentExtractor` - 콘텐츠 추출
- `IChunkingStrategy` - 청킹 전략

### 2. 설계 원칙
- **단일 책임**: 각 인터페이스는 하나의 명확한 역할
- **확장성**: 새로운 구현체 추가 용이
- **테스트 용이성**: Mock 구현체 제공
- **비동기 최우선**: 모든 I/O 작업은 비동기

## 🤖 AI 서비스 인터페이스 (Consumer Implementation)

### ITextCompletionService

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// LLM 텍스트 완성 서비스 인터페이스
    /// 소비 애플리케이션에서 OpenAI, Anthropic, Azure, Ollama 등의 구현체 제공
    /// </summary>
    public interface ITextCompletionService
    {
        /// <summary>
        /// 주어진 프롬프트에 대한 텍스트 완성을 수행합니다.
        /// </summary>
        /// <param name="prompt">완성을 요청할 프롬프트</param>
        /// <param name="options">완성 옵션 (토큰 수, 온도 등)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>완성된 텍스트</returns>
        Task<string> CompleteAsync(
            string prompt,
            TextCompletionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 스트리밍 텍스트 완성을 수행합니다.
        /// </summary>
        /// <param name="prompt">완성을 요청할 프롬프트</param>
        /// <param name="options">완성 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>스트리밍 텍스트 청크</returns>
        IAsyncEnumerable<string> CompleteStreamAsync(
            string prompt,
            TextCompletionOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 서비스가 현재 사용 가능한지 확인합니다.
        /// </summary>
        /// <returns>사용 가능 여부</returns>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// 지원하는 모델 목록을 반환합니다.
        /// </summary>
        /// <returns>모델 목록</returns>
        Task<IEnumerable<string>> GetAvailableModelsAsync();
    }

    /// <summary>
    /// 텍스트 완성 옵션
    /// </summary>
    public class TextCompletionOptions
    {
        /// <summary>최대 토큰 수</summary>
        public int? MaxTokens { get; set; } = 2000;

        /// <summary>온도 (창의성 조절)</summary>
        public float? Temperature { get; set; } = 0.3f;

        /// <summary>사용할 모델 이름</summary>
        public string? Model { get; set; }

        /// <summary>시스템 프롬프트</summary>
        public string? SystemPrompt { get; set; }

        /// <summary>응답 형식 (JSON, Markdown 등)</summary>
        public string? ResponseFormat { get; set; }

        /// <summary>추가 매개변수</summary>
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }
}
```

### IImageToTextService

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 이미지-텍스트 변환 서비스 인터페이스
    /// 멀티모달 RAG를 위한 이미지 설명 생성
    /// </summary>
    public interface IImageToTextService
    {
        /// <summary>
        /// 웹 이미지 URL에서 텍스트 설명을 추출합니다.
        /// </summary>
        /// <param name="imageUrl">이미지 URL</param>
        /// <param name="options">추출 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>추출된 텍스트 결과</returns>
        Task<ImageToTextResult> ExtractTextFromWebImageAsync(
            string imageUrl,
            ImageToTextOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 이미지 바이트 데이터에서 텍스트 설명을 추출합니다.
        /// </summary>
        /// <param name="imageData">이미지 바이트 데이터</param>
        /// <param name="contentType">이미지 MIME 타입</param>
        /// <param name="options">추출 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>추출된 텍스트 결과</returns>
        Task<ImageToTextResult> ExtractTextFromImageDataAsync(
            byte[] imageData,
            string contentType,
            ImageToTextOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 지원하는 이미지 형식 목록을 반환합니다.
        /// </summary>
        /// <returns>지원 형식 목록</returns>
        IEnumerable<string> GetSupportedImageFormats();
    }

    /// <summary>
    /// 이미지-텍스트 변환 결과
    /// </summary>
    public class ImageToTextResult
    {
        /// <summary>추출된 텍스트</summary>
        public string ExtractedText { get; set; } = string.Empty;

        /// <summary>신뢰도 점수 (0.0 ~ 1.0)</summary>
        public double Confidence { get; set; }

        /// <summary>성공 여부</summary>
        public bool IsSuccess { get; set; }

        /// <summary>오류 메시지</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>원본 이미지 URL</summary>
        public string? SourceUrl { get; set; }

        /// <summary>이미지 메타데이터</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 이미지-텍스트 변환 옵션
    /// </summary>
    public class ImageToTextOptions
    {
        /// <summary>추출 유형 (OCR, Description, Detailed 등)</summary>
        public string ExtractionType { get; set; } = "Description";

        /// <summary>언어 설정</summary>
        public string Language { get; set; } = "en";

        /// <summary>세부 수준 (Brief, Detailed, Comprehensive)</summary>
        public string DetailLevel { get; set; } = "Detailed";

        /// <summary>컨텍스트 프롬프트 (이미지 설명에 추가할 맥락)</summary>
        public string? ContextPrompt { get; set; }

        /// <summary>최대 텍스트 길이</summary>
        public int MaxTextLength { get; set; } = 1000;
    }
}
```

### IEmbeddingService (선택적)

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 텍스트 임베딩 생성 서비스 인터페이스 (선택적)
    /// 의미론적 청킹에서 사용
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// 텍스트에 대한 임베딩 벡터를 생성합니다.
        /// </summary>
        /// <param name="text">임베딩할 텍스트</param>
        /// <param name="options">임베딩 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>임베딩 벡터</returns>
        Task<float[]> GenerateAsync(
            string text,
            EmbeddingOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 여러 텍스트에 대한 임베딩을 배치 생성합니다.
        /// </summary>
        /// <param name="texts">임베딩할 텍스트 목록</param>
        /// <param name="options">임베딩 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>임베딩 벡터 목록</returns>
        Task<IEnumerable<float[]>> GenerateBatchAsync(
            IEnumerable<string> texts,
            EmbeddingOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 임베딩 벡터의 차원 수를 반환합니다.
        /// </summary>
        /// <returns>벡터 차원</returns>
        int GetDimensions();
    }

    /// <summary>
    /// 임베딩 옵션
    /// </summary>
    public class EmbeddingOptions
    {
        /// <summary>사용할 모델 이름</summary>
        public string? Model { get; set; }

        /// <summary>정규화 여부</summary>
        public bool Normalize { get; set; } = true;

        /// <summary>추가 매개변수</summary>
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }
}
```

## 🚀 WebFlux 핵심 인터페이스 (WebFlux Implementation)

### IWebContentProcessor

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 웹 콘텐츠 처리의 메인 인터페이스
    /// 크롤링 → 추출 → 파싱 → 청킹의 전체 파이프라인 오케스트레이션
    /// </summary>
    public interface IWebContentProcessor
    {
        /// <summary>
        /// 스트리밍 방식으로 웹 콘텐츠를 처리합니다 (권장).
        /// 메모리 효율적이며 실시간 진행률 제공
        /// </summary>
        /// <param name="baseUrl">처리할 기본 URL</param>
        /// <param name="crawlOptions">크롤링 옵션</param>
        /// <param name="chunkingOptions">청킹 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>처리 결과 스트림</returns>
        IAsyncEnumerable<ProcessingResult<IEnumerable<WebContentChunk>>> ProcessWithProgressAsync(
            string baseUrl,
            CrawlOptions? crawlOptions = null,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 전체 프로세스를 한 번에 실행합니다.
        /// 작은 규모의 처리에 적합
        /// </summary>
        /// <param name="baseUrl">처리할 기본 URL</param>
        /// <param name="crawlOptions">크롤링 옵션</param>
        /// <param name="chunkingOptions">청킹 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>모든 청크 목록</returns>
        Task<ProcessingResult<IEnumerable<WebContentChunk>>> ProcessAsync(
            string baseUrl,
            CrawlOptions? crawlOptions = null,
            ChunkingOptions? chunkingOptions = null,
            CancellationToken cancellationToken = default);

        // 단계별 처리 메서드 (고급 사용자용)

        /// <summary>
        /// 1단계: 웹 크롤링을 수행합니다.
        /// </summary>
        Task<IEnumerable<CrawlResult>> CrawlAsync(
            string baseUrl,
            CrawlOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 2단계: URL에서 원시 콘텐츠를 추출합니다.
        /// </summary>
        Task<RawWebContent> ExtractAsync(
            string url,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 3단계: 원시 콘텐츠를 파싱하여 구조화합니다.
        /// </summary>
        Task<ParsedWebContent> ParseAsync(
            RawWebContent rawContent,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 4단계: 파싱된 콘텐츠를 청킹합니다.
        /// </summary>
        Task<IEnumerable<WebContentChunk>> ChunkAsync(
            ParsedWebContent parsedContent,
            ChunkingOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 처리 통계를 반환합니다.
        /// </summary>
        Task<ProcessingStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// 처리 결과 래퍼
    /// </summary>
    public class ProcessingResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public ProcessingProgress? Progress { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public static ProcessingResult<T> Success(T result, ProcessingProgress? progress = null)
        {
            return new ProcessingResult<T>
            {
                IsSuccess = true,
                Result = result,
                Progress = progress
            };
        }

        public static ProcessingResult<T> Failure(string errorMessage)
        {
            return new ProcessingResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// 처리 진행률 정보
    /// </summary>
    public class ProcessingProgress
    {
        public int TotalPages { get; set; }
        public int PagesProcessed { get; set; }
        public int ChunksGenerated { get; set; }
        public double PercentComplete => TotalPages > 0 ? (double)PagesProcessed / TotalPages * 100 : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public string CurrentPhase { get; set; } = string.Empty;
        public string? CurrentUrl { get; set; }
    }
}
```

### ICrawler

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 웹 크롤링 전략 인터페이스
    /// 다양한 크롤링 알고리즘을 지원
    /// </summary>
    public interface ICrawler
    {
        /// <summary>전략 이름</summary>
        string StrategyName { get; }

        /// <summary>전략 설명</summary>
        string Description { get; }

        /// <summary>이 전략이 주어진 URL과 옵션에 적합한지 확인</summary>
        bool IsApplicable(string baseUrl, CrawlOptions options);

        /// <summary>
        /// 웹 크롤링을 수행합니다.
        /// </summary>
        /// <param name="baseUrl">시작 URL</param>
        /// <param name="options">크롤링 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>크롤링 결과 목록</returns>
        Task<IEnumerable<CrawlResult>> CrawlAsync(
            string baseUrl,
            CrawlOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 스트리밍 방식으로 크롤링을 수행합니다.
        /// </summary>
        /// <param name="baseUrl">시작 URL</param>
        /// <param name="options">크롤링 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>크롤링 결과 스트림</returns>
        IAsyncEnumerable<CrawlResult> CrawlStreamAsync(
            string baseUrl,
            CrawlOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 크롤링 전 예상 페이지 수를 추정합니다.
        /// </summary>
        Task<int> EstimatePageCountAsync(string baseUrl, CrawlOptions options);
    }

    /// <summary>
    /// 크롤링 결과
    /// </summary>
    public class CrawlResult
    {
        public string Url { get; set; } = string.Empty;
        public int Depth { get; set; }
        public DateTime CrawledAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long ContentLength { get; set; }
        public string? ParentUrl { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public List<string> ExtractedLinks { get; set; } = new();
        public CrawlError? Error { get; set; }
    }

    /// <summary>
    /// 크롤링 오류 정보
    /// </summary>
    public class CrawlError
    {
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
```

### IContentExtractor

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 콘텐츠 추출 인터페이스
    /// 다양한 웹 콘텐츠 형식에서 구조화된 텍스트 추출
    /// </summary>
    public interface IContentExtractor
    {
        /// <summary>추출기 타입 이름</summary>
        string ExtractorType { get; }

        /// <summary>지원하는 콘텐츠 타입 목록</summary>
        IEnumerable<string> SupportedContentTypes { get; }

        /// <summary>
        /// 주어진 콘텐츠 타입과 URL을 처리할 수 있는지 확인
        /// </summary>
        /// <param name="contentType">MIME 타입</param>
        /// <param name="url">URL</param>
        /// <returns>처리 가능 여부</returns>
        bool CanExtract(string contentType, string url);

        /// <summary>
        /// HTTP 응답에서 콘텐츠를 추출합니다.
        /// </summary>
        /// <param name="url">원본 URL</param>
        /// <param name="response">HTTP 응답</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>추출된 원시 콘텐츠</returns>
        Task<RawWebContent> ExtractAsync(
            string url,
            HttpResponseMessage response,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 추출 품질을 평가합니다.
        /// </summary>
        /// <param name="content">추출된 콘텐츠</param>
        /// <returns>품질 점수 (0.0 ~ 1.0)</returns>
        double EvaluateQuality(RawWebContent content);
    }

    /// <summary>
    /// 원시 웹 콘텐츠
    /// </summary>
    public class RawWebContent
    {
        public string Url { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public WebContentMetadata Metadata { get; set; } = new();
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
        public string ExtractorType { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
        public List<string> Links { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// 파싱된 웹 콘텐츠
    /// </summary>
    public class ParsedWebContent
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string MainContent { get; set; } = string.Empty;
        public List<ContentSection> Sections { get; set; } = new();
        public List<TableData> Tables { get; set; } = new();
        public List<ImageData> Images { get; set; } = new();
        public WebContentMetadata Metadata { get; set; } = new();
        public StructureInfo Structure { get; set; } = new();
        public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 콘텐츠 섹션
    /// </summary>
    public class ContentSection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Heading { get; set; } = string.Empty;
        public int Level { get; set; } // H1=1, H2=2, etc.
        public string Content { get; set; } = string.Empty;
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public List<ContentSection> SubSections { get; set; } = new();
    }
}
```

### IChunkingStrategy

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 청킹 전략 인터페이스
    /// RAG 최적화를 위한 다양한 청킹 알고리즘 지원
    /// </summary>
    public interface IChunkingStrategy
    {
        /// <summary>전략 이름</summary>
        string StrategyName { get; }

        /// <summary>전략 설명</summary>
        string Description { get; }

        /// <summary>권장 사용 사례</summary>
        IEnumerable<string> RecommendedUseCases { get; }

        /// <summary>이 전략이 주어진 콘텐츠에 적합한지 확인</summary>
        bool IsApplicable(ParsedWebContent content, ChunkingOptions options);

        /// <summary>
        /// 콘텐츠를 청킹합니다.
        /// </summary>
        /// <param name="content">파싱된 콘텐츠</param>
        /// <param name="options">청킹 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>청크 목록</returns>
        Task<IEnumerable<WebContentChunk>> ChunkAsync(
            ParsedWebContent content,
            ChunkingOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 청킹 품질을 평가합니다.
        /// </summary>
        /// <param name="chunks">생성된 청크</param>
        /// <param name="originalContent">원본 콘텐츠</param>
        /// <returns>품질 점수 (0.0 ~ 1.0)</returns>
        double EvaluateQuality(IEnumerable<WebContentChunk> chunks, ParsedWebContent originalContent);

        /// <summary>
        /// 예상 청킹 시간을 추정합니다.
        /// </summary>
        TimeSpan EstimateProcessingTime(ParsedWebContent content, ChunkingOptions options);
    }

    /// <summary>
    /// 청킹 품질 메트릭
    /// </summary>
    public class ChunkingQualityMetrics
    {
        public double CompletionScore { get; set; } // 청크 완성도
        public double ContextPreservationScore { get; set; } // 컨텍스트 보존
        public double SemanticConsistencyScore { get; set; } // 의미론적 일관성
        public double OptimalSizeScore { get; set; } // 최적 크기
        public double OverallQuality => (CompletionScore + ContextPreservationScore +
                                        SemanticConsistencyScore + OptimalSizeScore) / 4.0;
    }
}
```

## 🏭 팩토리 인터페이스

### ICrawlerFactory

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 크롤러 팩토리 인터페이스
    /// 크롤링 전략 선택 및 생성
    /// </summary>
    public interface ICrawlerFactory
    {
        /// <summary>
        /// 지원하는 모든 크롤링 전략을 반환합니다.
        /// </summary>
        IEnumerable<string> GetAvailableStrategies();

        /// <summary>
        /// 특정 전략의 크롤러를 생성합니다.
        /// </summary>
        ICrawler CreateCrawler(string strategyName);

        /// <summary>
        /// URL과 옵션에 가장 적합한 크롤러를 자동 선택합니다.
        /// </summary>
        ICrawler CreateOptimalCrawler(string baseUrl, CrawlOptions options);

        /// <summary>
        /// 전략별 추천 사용 사례를 반환합니다.
        /// </summary>
        Dictionary<string, IEnumerable<string>> GetStrategyRecommendations();
    }
}
```

### IChunkingStrategyFactory

```csharp
namespace WebFlux.Core.Interfaces
{
    /// <summary>
    /// 청킹 전략 팩토리 인터페이스
    /// 청킹 전략 선택 및 생성
    /// </summary>
    public interface IChunkingStrategyFactory
    {
        /// <summary>
        /// 지원하는 모든 청킹 전략을 반환합니다.
        /// </summary>
        IEnumerable<string> GetAvailableStrategies();

        /// <summary>
        /// 특정 전략의 청킹기를 생성합니다.
        /// </summary>
        IChunkingStrategy CreateStrategy(string strategyName);

        /// <summary>
        /// 콘텐츠와 옵션에 가장 적합한 전략을 자동 선택합니다 (Auto 전략).
        /// </summary>
        IChunkingStrategy CreateOptimalStrategy(ParsedWebContent content, ChunkingOptions options);

        /// <summary>
        /// 전략별 성능 특성을 반환합니다.
        /// </summary>
        Dictionary<string, StrategyCharacteristics> GetStrategyCharacteristics();
    }

    /// <summary>
    /// 전략 특성 정보
    /// </summary>
    public class StrategyCharacteristics
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QualityLevel QualityLevel { get; set; }
        public MemoryUsage MemoryUsage { get; set; }
        public ComputationCost ComputationCost { get; set; }
        public IEnumerable<string> OptimalContentTypes { get; set; } = new List<string>();
        public IEnumerable<string> RecommendedUseCases { get; set; } = new List<string>();
    }

    public enum QualityLevel { Low, Medium, High, VeryHigh }
    public enum MemoryUsage { Low, Medium, High }
    public enum ComputationCost { Low, Medium, High, VeryHigh }
}
```

## 🧪 Mock 서비스 구현

### MockTextCompletionService

```csharp
namespace WebFlux.Testing.Mocks
{
    /// <summary>
    /// 테스트용 Mock LLM 서비스
    /// 실제 AI 서비스 없이 테스트 가능
    /// </summary>
    public class MockTextCompletionService : ITextCompletionService
    {
        private readonly Dictionary<string, string> _responses;
        private readonly Random _random = new();

        public MockTextCompletionService()
        {
            _responses = new Dictionary<string, string>
            {
                ["summarize"] = "This is a test summary of the web content.",
                ["chunk boundary"] = "Split at paragraph breaks and section headers.",
                ["analyze structure"] = "The content has a hierarchical structure with clear sections.",
                ["extract keywords"] = "web, content, processing, RAG, chunking, extraction"
            };
        }

        public async Task<string> CompleteAsync(
            string prompt,
            TextCompletionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // 간단한 지연 시뮬레이션
            await Task.Delay(_random.Next(100, 500), cancellationToken);

            // 키워드 기반 응답 매칭
            foreach (var kvp in _responses)
            {
                if (prompt.ToLower().Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Mock LLM response for testing purposes.";
        }

        public async IAsyncEnumerable<string> CompleteStreamAsync(
            string prompt,
            TextCompletionOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await CompleteAsync(prompt, options, cancellationToken);
            var words = response.Split(' ');

            foreach (var word in words)
            {
                await Task.Delay(50, cancellationToken);
                yield return word + " ";
            }
        }

        public Task<bool> IsAvailableAsync() => Task.FromResult(true);

        public Task<IEnumerable<string>> GetAvailableModelsAsync() =>
            Task.FromResult<IEnumerable<string>>(new[] { "mock-gpt-4", "mock-claude-3" });
    }
}
```

## 📋 사용 예제

### 기본 사용법

```csharp
// DI 컨테이너 설정
var services = new ServiceCollection();

// 필수 서비스 등록 (소비 애플리케이션에서 구현)
services.AddScoped<ITextCompletionService, OpenAiTextCompletionService>();
services.AddScoped<IImageToTextService, OpenAiImageToTextService>();

// WebFlux 서비스 등록
services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// 스트리밍 처리
await foreach (var result in processor.ProcessWithProgressAsync("https://docs.example.com"))
{
    if (result.IsSuccess && result.Result != null)
    {
        foreach (var chunk in result.Result)
        {
            Console.WriteLine($"청크 {chunk.ChunkIndex}: {chunk.Content.Length}자");
        }
    }
}
```

### 고급 사용법 (단계별 제어)

```csharp
// 1단계: 크롤링
var crawlResults = await processor.CrawlAsync("https://docs.example.com", new CrawlOptions
{
    MaxDepth = 3,
    Strategy = "Intelligent"
});

// 2단계: 콘텐츠 추출
foreach (var crawlResult in crawlResults)
{
    var rawContent = await processor.ExtractAsync(crawlResult.Url);
    var parsedContent = await processor.ParseAsync(rawContent);

    // 3단계: 청킹
    var chunks = await processor.ChunkAsync(parsedContent, new ChunkingOptions
    {
        Strategy = "Auto",
        MaxChunkSize = 512
    });
}
```

---

이 인터페이스 설계는 확장성과 테스트 용이성을 고려하여 작성되었으며, 실제 구현 시 단계적으로 발전시킬 수 있는 구조로 되어 있습니다.