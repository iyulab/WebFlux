namespace WebFlux.Core.Models.Events;

/// <summary>
/// 오류 발생 이벤트
/// </summary>
public class ErrorOccurredEvent : ProcessingEvent
{
    public override string EventType => "ErrorOccurred";

    /// <summary>오류 코드</summary>
    public required string ErrorCode { get; init; }

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
    public required string MetricName { get; init; }

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
