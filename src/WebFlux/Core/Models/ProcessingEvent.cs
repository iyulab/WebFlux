namespace WebFlux.Core.Models;

/// <summary>
/// 처리 이벤트의 기본 클래스
/// 구체적인 이벤트 타입은 <see cref="WebFlux.Core.Models.Events"/> 네임스페이스 참고
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
