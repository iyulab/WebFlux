namespace WebFlux.Core.Models.Events;

/// <summary>
/// 모니터링/성능 관련 이벤트 통합
/// </summary>

/// <summary>
/// 오류 발생 이벤트 (통합)
/// </summary>
public class ErrorOccurredEventV2 : ProcessingEvent
{
    public override string EventType => "ErrorOccurred";

    /// <summary>오류 코드</summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>오류 카테고리</summary>
    public string ErrorCategory { get; init; } = "General";

    /// <summary>스택 트레이스</summary>
    public string? StackTrace { get; init; }

    /// <summary>재시도 가능 여부</summary>
    public bool IsRetryable { get; init; }

    /// <summary>영향을 받은 리소스</summary>
    public string? AffectedResource { get; init; }

    public ErrorOccurredEventV2()
    {
        Severity = EventSeverity.Error;
    }
}

/// <summary>
/// 성능 메트릭 이벤트 (통합)
/// </summary>
public class PerformanceMetricsEventV2 : ProcessingEvent
{
    public override string EventType => "PerformanceMetrics";

    /// <summary>메트릭 이름</summary>
    public string MetricName { get; init; } = string.Empty;

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
/// 크롤링 오류 이벤트 (통합)
/// </summary>
public class CrawlErrorEventV2 : ProcessingEvent
{
    public override string EventType => "CrawlError";

    /// <summary>오류 발생 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>오류 메시지</summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// 크롤링 경고 이벤트 (통합)
/// </summary>
public class CrawlWarningEventV2 : ProcessingEvent
{
    public override string EventType => "CrawlWarning";

    /// <summary>경고 대상 URL</summary>
    public string? Url { get; set; }

    /// <summary>경고 세부 내용</summary>
    public string? Detail { get; set; }
}
