using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// robots.txt 파일 파싱 및 규칙 검증 서비스 인터페이스
/// RFC 9309 표준을 따르는 robots.txt 처리
/// </summary>
public interface IRobotsTxtParser
{
    /// <summary>
    /// 웹사이트에서 robots.txt 파일을 감지하고 파싱합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>robots.txt 파싱 결과</returns>
    Task<RobotsTxtParseResult> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// robots.txt 파일 내용을 직접 파싱합니다.
    /// </summary>
    /// <param name="content">robots.txt 파일 내용</param>
    /// <param name="baseUrl">기본 URL (상대 경로 해석용)</param>
    /// <returns>파싱된 robots.txt 메타데이터</returns>
    Task<RobotsMetadata> ParseContentAsync(string content, string baseUrl);

    /// <summary>
    /// 특정 URL이 크롤링 허용되는지 확인합니다.
    /// </summary>
    /// <param name="metadata">robots.txt 메타데이터</param>
    /// <param name="url">확인할 URL</param>
    /// <param name="userAgent">User-Agent 문자열</param>
    /// <returns>크롤링 허용 여부</returns>
    bool IsUrlAllowed(RobotsMetadata metadata, string url, string userAgent = "*");

    /// <summary>
    /// 크롤링 지연 시간을 가져옵니다.
    /// </summary>
    /// <param name="metadata">robots.txt 메타데이터</param>
    /// <param name="userAgent">User-Agent 문자열</param>
    /// <returns>크롤링 지연 시간</returns>
    TimeSpan? GetCrawlDelay(RobotsMetadata metadata, string userAgent = "*");

    /// <summary>
    /// robots.txt에 명시된 사이트맵 URL 목록을 가져옵니다.
    /// </summary>
    /// <param name="metadata">robots.txt 메타데이터</param>
    /// <returns>사이트맵 URL 목록</returns>
    IReadOnlyList<string> GetSitemaps(RobotsMetadata metadata);
}