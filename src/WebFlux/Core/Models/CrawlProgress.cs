namespace WebFlux.Core.Models;

/// <summary>
/// 배치 크롤링 진행률 정보
/// 대량 URL 크롤링 시 상세 진행률 및 에러 리포팅
/// </summary>
public class CrawlProgress
{
    /// <summary>
    /// 총 URL 수
    /// </summary>
    public int TotalUrls { get; set; }

    /// <summary>
    /// 처리된 URL 수
    /// </summary>
    public int ProcessedUrls { get; set; }

    /// <summary>
    /// 성공한 URL 수
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 실패한 URL 수
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 생성된 총 청크 수
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// 현재 처리 중인 URL
    /// </summary>
    public string CurrentUrl { get; set; } = string.Empty;

    /// <summary>
    /// 경과 시간
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// 예상 남은 시간
    /// </summary>
    public TimeSpan EstimatedRemaining { get; set; }

    /// <summary>
    /// 에러 목록
    /// </summary>
    public List<CrawlError> Errors { get; set; } = new();

    /// <summary>
    /// 진행률 (0.0 - 1.0)
    /// </summary>
    public double ProgressPercentage =>
        TotalUrls > 0 ? (double)ProcessedUrls / TotalUrls : 0.0;

    /// <summary>
    /// 성공률 (0.0 - 1.0)
    /// </summary>
    public double SuccessRate =>
        ProcessedUrls > 0 ? (double)SuccessCount / ProcessedUrls : 0.0;

    /// <summary>
    /// 초당 처리 URL 수
    /// </summary>
    public double UrlsPerSecond =>
        ElapsedTime.TotalSeconds > 0 ? ProcessedUrls / ElapsedTime.TotalSeconds : 0.0;

    /// <summary>
    /// 처리된 URL당 평균 청크 수
    /// </summary>
    public double AverageChunksPerUrl =>
        SuccessCount > 0 ? (double)TotalChunks / SuccessCount : 0.0;

    /// <summary>
    /// 크롤링 시작 시간
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 추가 통계 정보
    /// </summary>
    public CrawlStatisticsDetails Statistics { get; set; } = new();
}

/// <summary>
/// 크롤링 에러 정보
/// </summary>
public class CrawlError
{
    /// <summary>
    /// 에러가 발생한 URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// 에러 유형
    /// </summary>
    public required string ErrorType { get; init; }

    /// <summary>
    /// 에러 메시지
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 에러 발생 시간
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// HTTP 상태 코드 (해당하는 경우)
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// 스택 트레이스 (디버깅용)
    /// </summary>
    public string? StackTrace { get; init; }
}

/// <summary>
/// 상세 통계 정보
/// </summary>
public class CrawlStatisticsDetails
{
    /// <summary>
    /// 총 다운로드 바이트
    /// </summary>
    public long TotalBytesDownloaded { get; set; }

    /// <summary>
    /// 평균 응답 시간 (밀리초)
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// 최소 응답 시간 (밀리초)
    /// </summary>
    public double MinResponseTimeMs { get; set; } = double.MaxValue;

    /// <summary>
    /// 최대 응답 시간 (밀리초)
    /// </summary>
    public double MaxResponseTimeMs { get; set; }

    /// <summary>
    /// 도메인별 URL 수
    /// </summary>
    public Dictionary<string, int> UrlsByDomain { get; set; } = new();

    /// <summary>
    /// 에러 유형별 카운트
    /// </summary>
    public Dictionary<string, int> ErrorsByType { get; set; } = new();

    /// <summary>
    /// HTTP 상태 코드별 카운트
    /// </summary>
    public Dictionary<int, int> StatusCodeCounts { get; set; } = new();

    /// <summary>
    /// 콘텐츠 타입별 카운트
    /// </summary>
    public Dictionary<string, int> ContentTypeCounts { get; set; } = new();
}

/// <summary>
/// 일반적인 크롤링 에러 유형
/// </summary>
public static class CrawlErrorTypes
{
    /// <summary>요청 타임아웃</summary>
    public const string Timeout = "Timeout";

    /// <summary>연결 실패</summary>
    public const string ConnectionFailed = "ConnectionFailed";

    /// <summary>DNS 확인 실패</summary>
    public const string DnsFailure = "DnsFailure";

    /// <summary>SSL/TLS 오류</summary>
    public const string SslError = "SslError";

    /// <summary>HTTP 404 Not Found</summary>
    public const string NotFound = "NotFound";

    /// <summary>HTTP 403 Forbidden</summary>
    public const string Forbidden = "Forbidden";

    /// <summary>HTTP 429 Too Many Requests</summary>
    public const string RateLimited = "RateLimited";

    /// <summary>HTTP 5xx Server Error</summary>
    public const string ServerError = "ServerError";

    /// <summary>robots.txt에 의해 차단됨</summary>
    public const string RobotsBlocked = "RobotsBlocked";

    /// <summary>콘텐츠 파싱 실패</summary>
    public const string ParseError = "ParseError";

    /// <summary>콘텐츠 타입 미지원</summary>
    public const string UnsupportedContentType = "UnsupportedContentType";

    /// <summary>콘텐츠 크기 초과</summary>
    public const string ContentTooLarge = "ContentTooLarge";

    /// <summary>알 수 없는 오류</summary>
    public const string Unknown = "Unknown";
}
