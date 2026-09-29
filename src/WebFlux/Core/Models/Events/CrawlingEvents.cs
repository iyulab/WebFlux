namespace WebFlux.Core.Models.Events;

/// <summary>
/// URL 처리 시작 이벤트
/// </summary>
public class UrlProcessingStartedEvent : ProcessingEvent
{
    public override string EventType => "UrlProcessingStarted";

    /// <summary>처리 대상 URL</summary>
    public required string Url { get; init; }
}

/// <summary>
/// URL 처리 완료 이벤트
/// </summary>
public class UrlProcessedEvent : ProcessingEvent
{
    public override string EventType => "UrlProcessed";

    /// <summary>처리된 URL</summary>
    public required string Url { get; init; }

    /// <summary>콘텐츠 길이</summary>
    public int ContentLength { get; init; }

    /// <summary>콘텐츠 타입</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>발견된 URL 수</summary>
    public int DiscoveredUrlCount { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public int ProcessingTimeMs { get; init; }
}

/// <summary>
/// URL 처리 실패 이벤트
/// </summary>
public class UrlProcessingFailedEvent : ProcessingEvent
{
    public override string EventType => "UrlProcessingFailed";

    /// <summary>실패한 URL</summary>
    public required string Url { get; init; }

    /// <summary>오류 메시지</summary>
    public required string Error { get; init; }
}
