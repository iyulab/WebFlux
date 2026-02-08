namespace WebFlux.Core.Models.Events;

/// <summary>
/// 크롤링 관련 이벤트 통합
/// </summary>

/// <summary>
/// 크롤링 시작 이벤트 (통합)
/// </summary>
public class CrawlingStartedEventV2 : ProcessingEvent
{
    public override string EventType => "CrawlingStarted";

    /// <summary>시작 URL</summary>
    public string StartUrl { get; init; } = string.Empty;

    /// <summary>크롤링 옵션</summary>
    public object CrawlOptions { get; init; } = new object();

    /// <summary>예상 페이지 수</summary>
    public int? EstimatedPageCount { get; init; }
}

/// <summary>
/// 크롤링 완료 이벤트 (통합)
/// </summary>
public class CrawlingCompletedEventV2 : ProcessingEvent
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
/// 페이지 크롤링 완료 이벤트 (통합)
/// </summary>
public class PageCrawledEventV2 : ProcessingEvent
{
    public override string EventType => "PageCrawled";

    /// <summary>크롤링된 URL</summary>
    public string Url { get; init; } = string.Empty;

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
/// URL 처리 시작 이벤트 (통합)
/// </summary>
public class UrlProcessingStartedEventV2 : ProcessingEvent
{
    public override string EventType => "UrlProcessingStarted";

    /// <summary>처리 대상 URL</summary>
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// URL 처리 완료 이벤트 (통합)
/// </summary>
public class UrlProcessedEventV2 : ProcessingEvent
{
    public override string EventType => "UrlProcessed";

    /// <summary>처리된 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>콘텐츠 길이</summary>
    public int ContentLength { get; set; }

    /// <summary>콘텐츠 타입</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>발견된 URL 수</summary>
    public int DiscoveredUrlCount { get; set; }

    /// <summary>처리 시간 (밀리초)</summary>
    public int ProcessingTimeMs { get; set; }
}

/// <summary>
/// URL 처리 실패 이벤트 (통합)
/// </summary>
public class UrlProcessingFailedEventV2 : ProcessingEvent
{
    public override string EventType => "UrlProcessingFailed";

    /// <summary>실패한 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>오류 메시지</summary>
    public string Error { get; set; } = string.Empty;
}
