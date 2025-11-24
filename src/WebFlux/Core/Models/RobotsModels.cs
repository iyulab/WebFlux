namespace WebFlux.Core.Models;

/// <summary>
/// robots.txt 파싱 결과
/// </summary>
public class RobotsTxtParseResult
{
    /// <summary>
    /// 파싱 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// robots.txt 파일이 발견되었는지 여부
    /// </summary>
    public bool FileFound { get; set; }

    /// <summary>
    /// robots.txt URL
    /// </summary>
    public string RobotsUrl { get; set; } = string.Empty;

    /// <summary>
    /// 파싱된 메타데이터
    /// </summary>
    public RobotsMetadata? Metadata { get; set; }

    /// <summary>
    /// 파싱 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 원본 robots.txt 내용
    /// </summary>
    public string? RawContent { get; set; }

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// robots.txt 메타데이터
/// </summary>
public class RobotsMetadata
{
    /// <summary>
    /// 웹사이트 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent별 크롤링 규칙
    /// </summary>
    public Dictionary<string, List<RobotsRule>> Rules { get; set; } = new();

    /// <summary>
    /// 크롤링 지연 시간
    /// </summary>
    public TimeSpan? CrawlDelay { get; set; }

    /// <summary>
    /// 사이트맵 URL 목록
    /// </summary>
    public List<string> Sitemaps { get; set; } = new();

    /// <summary>
    /// 요청 속도 제한
    /// </summary>
    public RequestRateLimit? RequestRate { get; set; }

    /// <summary>
    /// 방문 시간 제한
    /// </summary>
    public VisitTimeWindow? VisitTimeWindow { get; set; }

    /// <summary>
    /// 선호 호스트
    /// </summary>
    public string? PreferredHost { get; set; }

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();
}

/// <summary>
/// robots.txt 크롤링 규칙
/// </summary>
public class RobotsRule
{
    /// <summary>
    /// 규칙 유형
    /// </summary>
    public RobotsRuleType Type { get; set; }

    /// <summary>
    /// 적용 패턴
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent
    /// </summary>
    public string UserAgent { get; set; } = "*";

    /// <summary>
    /// 우선순위 (낮을수록 높은 우선순위)
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// robots.txt 규칙 유형
/// </summary>
public enum RobotsRuleType
{
    /// <summary>
    /// 크롤링 허용
    /// </summary>
    Allow,

    /// <summary>
    /// 크롤링 금지
    /// </summary>
    Disallow
}

/// <summary>
/// 요청 속도 제한
/// </summary>
public class RequestRateLimit
{
    /// <summary>
    /// 시간 창 내 허용 요청 수
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// 시간 창
    /// </summary>
    public TimeSpan TimeWindow { get; set; }
}

/// <summary>
/// 방문 시간 제한
/// </summary>
public class VisitTimeWindow
{
    /// <summary>
    /// 크롤링 시작 시간
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 크롤링 종료 시간
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// 현재 시간이 허용 시간 내인지 확인
    /// </summary>
    public bool IsCurrentTimeAllowed(TimeZoneInfo? timeZone = null)
    {
        var currentTime = timeZone != null
            ? TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).TimeOfDay
            : DateTimeOffset.Now.TimeOfDay;

        return currentTime >= StartTime && currentTime <= EndTime;
    }
}