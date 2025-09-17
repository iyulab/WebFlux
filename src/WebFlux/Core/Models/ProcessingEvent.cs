namespace WebFlux.Core.Models;

/// <summary>
/// 처리 이벤트의 기본 클래스
/// </summary>
public abstract class ProcessingEvent
{
    /// <summary>
    /// 이벤트 고유 식별자
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 이벤트 타입
    /// </summary>
    public abstract string EventType { get; }

    /// <summary>
    /// 이벤트 발생 시간
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 작업 ID (관련 작업이 있는 경우)
    /// </summary>
    public string? JobId { get; init; }

    /// <summary>
    /// 이벤트 심각도
    /// </summary>
    public EventSeverity Severity { get; init; } = EventSeverity.Info;

    /// <summary>
    /// 이벤트 메시지
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 추가 이벤트 데이터
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 이벤트 소스 (컴포넌트명, 서비스명 등)
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// 관련 URL 또는 리소스
    /// </summary>
    public string? RelatedResource { get; init; }

    /// <summary>
    /// 사용자 ID (해당하는 경우)
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// 상관관계 ID (분산 추적용)
    /// </summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// 크롤링 시작 이벤트
/// </summary>
public class CrawlingStartedEvent : ProcessingEvent
{
    public override string EventType => "CrawlingStarted";

    /// <summary>시작 URL</summary>
#if NET8_0_OR_GREATER
    public required string StartUrl { get; init; }
#else
    public string StartUrl { get; init; } = string.Empty;
#endif

    /// <summary>크롤링 옵션</summary>
#if NET8_0_OR_GREATER
    public required object CrawlOptions { get; init; }
#else
    public object CrawlOptions { get; init; } = new object();
#endif

    /// <summary>예상 페이지 수</summary>
    public int? EstimatedPageCount { get; init; }
}

/// <summary>
/// 크롤링 완료 이벤트
/// </summary>
public class CrawlingCompletedEvent : ProcessingEvent
{
    public override string EventType => "CrawlingCompleted";

    /// <summary>처리된 페이지 수</summary>
    public int ProcessedPages { get; init; }

    /// <summary>성공한 페이지 수</summary>
    public int SuccessfulPages { get; init; }

    /// <summary>실패한 페이지 수</summary>
    public int FailedPages { get; init; }

    /// <summary>총 처리 시간 (밀리초)</summary>
    public long TotalProcessingTimeMs { get; init; }

    /// <summary>평균 처리 시간 (밀리초)</summary>
    public double AverageProcessingTimeMs { get; init; }
}

/// <summary>
/// 페이지 크롤링 이벤트
/// </summary>
public class PageCrawledEvent : ProcessingEvent
{
    public override string EventType => "PageCrawled";

    /// <summary>크롤링된 URL</summary>
#if NET8_0_OR_GREATER
    public required string Url { get; init; }
#else
    public string Url { get; init; } = string.Empty;
#endif

    /// <summary>HTTP 상태 코드</summary>
    public int StatusCode { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>콘텐츠 크기 (바이트)</summary>
    public long? ContentSize { get; init; }

    /// <summary>발견된 링크 수</summary>
    public int DiscoveredLinks { get; init; }

    /// <summary>크롤링 깊이</summary>
    public int Depth { get; init; }
}

/// <summary>
/// 청킹 시작 이벤트
/// </summary>
public class ChunkingStartedEvent : ProcessingEvent
{
    public override string EventType => "ChunkingStarted";

    /// <summary>소스 URL</summary>
#if NET8_0_OR_GREATER
    public required string SourceUrl { get; init; }
#else
    public string SourceUrl { get; init; } = string.Empty;
#endif

    /// <summary>청킹 전략</summary>
#if NET8_0_OR_GREATER
    public required string Strategy { get; init; }
#else
    public string Strategy { get; init; } = string.Empty;
#endif

    /// <summary>콘텐츠 크기 (문자 수)</summary>
    public int ContentLength { get; init; }

    /// <summary>청킹 옵션</summary>
#if NET8_0_OR_GREATER
    public required object ChunkingOptions { get; init; }
#else
    public object ChunkingOptions { get; init; } = new object();
#endif
}

/// <summary>
/// 청킹 완료 이벤트
/// </summary>
public class ChunkingCompletedEvent : ProcessingEvent
{
    public override string EventType => "ChunkingCompleted";

    /// <summary>소스 URL</summary>
#if NET8_0_OR_GREATER
    public required string SourceUrl { get; init; }
#else
    public string SourceUrl { get; init; } = string.Empty;
#endif

    /// <summary>생성된 청크 수</summary>
    public int GeneratedChunks { get; init; }

    /// <summary>평균 청크 크기</summary>
    public double AverageChunkSize { get; init; }

    /// <summary>평균 품질 점수</summary>
    public double AverageQualityScore { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }
}

/// <summary>
/// 청크 생성 이벤트
/// </summary>
public class ChunkGeneratedEvent : ProcessingEvent
{
    public override string EventType => "ChunkGenerated";

    /// <summary>청크 ID</summary>
#if NET8_0_OR_GREATER
    public required string ChunkId { get; init; }
#else
    public string ChunkId { get; init; } = string.Empty;
#endif

    /// <summary>소스 URL</summary>
#if NET8_0_OR_GREATER
    public required string SourceUrl { get; init; }
#else
    public string SourceUrl { get; init; } = string.Empty;
#endif

    /// <summary>청크 크기 (토큰 수)</summary>
    public int ChunkSize { get; init; }

    /// <summary>품질 점수</summary>
    public double QualityScore { get; init; }

    /// <summary>청크 타입</summary>
    public string ChunkType { get; init; } = "Text";

    /// <summary>시퀀스 번호</summary>
    public int SequenceNumber { get; init; }
}

/// <summary>
/// 이미지 처리 이벤트
/// </summary>
public class ImageProcessedEvent : ProcessingEvent
{
    public override string EventType => "ImageProcessed";

    /// <summary>이미지 URL</summary>
#if NET8_0_OR_GREATER
    public required string ImageUrl { get; init; }
#else
    public string ImageUrl { get; init; } = string.Empty;
#endif

    /// <summary>생성된 설명 길이</summary>
    public int DescriptionLength { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>이미지 크기 (바이트)</summary>
    public long? ImageSize { get; init; }

    /// <summary>이미지 형식</summary>
    public string? ImageFormat { get; init; }
}

/// <summary>
/// 오류 발생 이벤트
/// </summary>
public class ErrorOccurredEvent : ProcessingEvent
{
    public override string EventType => "ErrorOccurred";

    /// <summary>오류 코드</summary>
#if NET8_0_OR_GREATER
    public required string ErrorCode { get; init; }
#else
    public string ErrorCode { get; init; } = string.Empty;
#endif

    /// <summary>오류 카테고리</summary>
    public string ErrorCategory { get; init; } = "General";

    /// <summary>스택 트레이스</summary>
    public string? StackTrace { get; init; }

    /// <summary>재시도 가능 여부</summary>
    public bool IsRetryable { get; init; }

    /// <summary>영향을 받은 리소스</summary>
    public string? AffectedResource { get; init; }

    public ErrorOccurredEvent()
    {
        Severity = EventSeverity.Error;
    }
}

/// <summary>
/// 성능 메트릭 이벤트
/// </summary>
public class PerformanceMetricsEvent : ProcessingEvent
{
    public override string EventType => "PerformanceMetrics";

    /// <summary>메트릭 이름</summary>
#if NET8_0_OR_GREATER
    public required string MetricName { get; init; }
#else
    public string MetricName { get; init; } = string.Empty;
#endif

    /// <summary>메트릭 값</summary>
    public double Value { get; init; }

    /// <summary>메트릭 단위</summary>
    public string? Unit { get; init; }

    /// <summary>측정 기간 (밀리초)</summary>
    public long? MeasurementPeriodMs { get; init; }

    /// <summary>추가 태그</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>
/// 이벤트 심각도 열거형
/// </summary>
public enum EventSeverity
{
    /// <summary>디버그</summary>
    Debug,
    /// <summary>정보</summary>
    Info,
    /// <summary>경고</summary>
    Warning,
    /// <summary>오류</summary>
    Error,
    /// <summary>치명적</summary>
    Critical
}