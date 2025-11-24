using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 웹 크롤링 인터페이스
/// 다양한 크롤링 전략 구현을 위한 계약 정의
/// </summary>
public interface ICrawler
{
    /// <summary>
    /// 단일 URL을 크롤링합니다.
    /// </summary>
    /// <param name="url">크롤링할 URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 웹 페이지 정보</returns>
    Task<CrawlResult> CrawlAsync(
        string url,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 웹사이트를 전체적으로 크롤링합니다.
    /// </summary>
    /// <param name="startUrl">시작 URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>발견된 URL과 크롤링 결과 스트림</returns>
    IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
        string startUrl,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sitemap을 기반으로 크롤링합니다.
    /// </summary>
    /// <param name="sitemapUrl">sitemap.xml URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링 결과 스트림</returns>
    IAsyncEnumerable<CrawlResult> CrawlSitemapAsync(
        string sitemapUrl,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// robots.txt를 확인합니다.
    /// </summary>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="userAgent">User-Agent</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>robots.txt 정보</returns>
    Task<RobotsTxtInfo> GetRobotsTxtAsync(
        string baseUrl,
        string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// URL이 크롤링 가능한지 확인합니다.
    /// </summary>
    /// <param name="url">확인할 URL</param>
    /// <param name="userAgent">User-Agent</param>
    /// <returns>크롤링 가능 여부</returns>
    Task<bool> IsUrlAllowedAsync(string url, string userAgent);

    /// <summary>
    /// 링크를 추출합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="baseUrl">기본 URL</param>
    /// <returns>추출된 링크 목록</returns>
    IReadOnlyList<string> ExtractLinks(string htmlContent, string baseUrl);

    /// <summary>
    /// 크롤링 통계를 반환합니다.
    /// </summary>
    /// <returns>크롤링 통계 정보</returns>
    CrawlStatistics GetStatistics();
}

/// <summary>
/// 크롤링 결과
/// </summary>
public class CrawlResult
{
    /// <summary>요청 URL</summary>
#if NET8_0_OR_GREATER
    public required string Url { get; init; }
#else
    public string Url { get; init; } = string.Empty;
#endif

    /// <summary>실제 응답 URL (리디렉션 포함)</summary>
    public string FinalUrl { get; init; } = string.Empty;

    /// <summary>HTTP 상태 코드</summary>
    public int StatusCode { get; init; }

    /// <summary>성공 여부</summary>
    public bool IsSuccess { get; init; }

    /// <summary>성공 여부 (IsSuccess의 별칭)</summary>
    public bool IsSuccessful
    {
        get => IsSuccess;
        init => IsSuccess = value;
    }

    /// <summary>콘텐츠 내용 (HtmlContent의 별칭)</summary>
    public string? Content
    {
        get => HtmlContent;
        init => HtmlContent = value;
    }

    /// <summary>HTML 콘텐츠</summary>
    public string? HtmlContent { get; init; }

    /// <summary>응답 헤더</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>콘텐츠 타입</summary>
    public string? ContentType { get; init; }

    /// <summary>문자 인코딩</summary>
    public string? Encoding { get; init; }

    /// <summary>콘텐츠 길이</summary>
    public long? ContentLength { get; init; }

    /// <summary>응답 시간 (밀리초)</summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>크롤링 시간</summary>
    public DateTimeOffset CrawledAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>크롤링 깊이</summary>
    public int Depth { get; init; }

    /// <summary>부모 URL</summary>
    public string? ParentUrl { get; init; }

    /// <summary>발견된 링크 목록</summary>
    public IReadOnlyList<string> DiscoveredLinks { get; init; } = Array.Empty<string>();

    /// <summary>오류 메시지 (실패한 경우)</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>예외 정보 (실패한 경우)</summary>
    public Exception? Exception { get; init; }

    /// <summary>이미지 URL 목록</summary>
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();

    /// <summary>추가 메타데이터</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>웹 콘텐츠 메타데이터</summary>
    public WebContentMetadata? WebMetadata { get; init; }
}

/// <summary>
/// robots.txt 정보
/// </summary>
public class RobotsTxtInfo
{
    /// <summary>robots.txt 내용</summary>
    public string? Content { get; init; }

    /// <summary>User-Agent별 규칙</summary>
    public IReadOnlyDictionary<string, RobotRules> Rules { get; init; } =
        new Dictionary<string, RobotRules>();

    /// <summary>Sitemap URL 목록</summary>
    public IReadOnlyList<string> Sitemaps { get; init; } = Array.Empty<string>();

    /// <summary>크롤 지연 시간 (초)</summary>
    public int? CrawlDelay { get; init; }

    /// <summary>마지막 수정 시간</summary>
    public DateTimeOffset? LastModified { get; init; }
}

/// <summary>
/// Robot 규칙
/// </summary>
public class RobotRules
{
    /// <summary>허용된 경로 패턴</summary>
    public IReadOnlyList<string> AllowedPaths { get; init; } = Array.Empty<string>();

    /// <summary>금지된 경로 패턴</summary>
    public IReadOnlyList<string> DisallowedPaths { get; init; } = Array.Empty<string>();

    /// <summary>크롤 지연 시간 (초)</summary>
    public int? CrawlDelay { get; init; }
}

/// <summary>
/// 크롤링 통계
/// </summary>
public class CrawlStatistics
{
    /// <summary>총 요청 수</summary>
    public long TotalRequests { get; init; }

    /// <summary>성공한 요청 수</summary>
    public long SuccessfulRequests { get; init; }

    /// <summary>실패한 요청 수</summary>
    public long FailedRequests { get; init; }

    /// <summary>평균 응답 시간 (밀리초)</summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>처리된 바이트 수</summary>
    public long TotalBytesProcessed { get; init; }

    /// <summary>초당 요청 수</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>도메인별 요청 수</summary>
    public IReadOnlyDictionary<string, long> RequestsByDomain { get; init; } =
        new Dictionary<string, long>();

    /// <summary>상태 코드별 분포</summary>
    public IReadOnlyDictionary<int, long> StatusCodeDistribution { get; init; } =
        new Dictionary<int, long>();

    /// <summary>크롤링 시작 시간</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}