namespace WebFlux.Tests.Fixtures;

/// <summary>
/// 통합 테스트에 사용할 수 있는 허용된 테스트 사이트 목록
/// 모든 사이트는 robots.txt에서 크롤링을 허용하거나 테스트 목적으로 제공됩니다.
/// </summary>
public static class TestSites
{
    /// <summary>
    /// 스크래핑 교육용 사이트 - 인용문 페이지
    /// robots.txt: 허용
    /// </summary>
    public const string QuotesToScrape = "https://quotes.toscrape.com";

    /// <summary>
    /// HTTP 테스트용 사이트 - 다양한 HTTP 기능 테스트
    /// robots.txt: 허용
    /// </summary>
    public const string HttpBin = "https://httpbin.org";

    /// <summary>
    /// IANA 예시 도메인 - 단순 HTML 페이지
    /// robots.txt: 허용
    /// </summary>
    public const string ExampleCom = "https://example.com";

    /// <summary>
    /// HTTP 상태 코드 테스트용 사이트
    /// robots.txt: 허용
    /// </summary>
    public const string HttpStat = "https://httpbin.org/status";

    /// <summary>
    /// JSON 플레이스홀더 API - 테스트용 REST API
    /// robots.txt: 허용
    /// </summary>
    public const string JsonPlaceholder = "https://jsonplaceholder.typicode.com";

    /// <summary>
    /// Books To Scrape - 스크래핑 교육용 전자상거래 사이트
    /// robots.txt: 허용
    /// </summary>
    public const string BooksToScrape = "https://books.toscrape.com";

    /// <summary>
    /// 다양한 HTTP 상태 코드를 테스트할 수 있는 URL 생성
    /// httpbin.org/status 엔드포인트 사용 (더 안정적)
    /// </summary>
    /// <param name="statusCode">반환받을 HTTP 상태 코드</param>
    /// <returns>해당 상태 코드를 반환하는 URL</returns>
    public static string GetHttpStatusUrl(int statusCode) => $"{HttpStat}/{statusCode}";

    /// <summary>
    /// 장시간 응답을 지속하는 URL 생성 (취소 테스트용)
    /// drip 엔드포인트: 지정된 시간 동안 데이터를 천천히 반환
    /// </summary>
    /// <param name="durationSeconds">응답 지속 시간 (초)</param>
    /// <returns>느린 응답을 반환하는 URL</returns>
    public static string GetDripUrl(int durationSeconds) =>
        $"{HttpBin}/drip?duration={durationSeconds}&numbytes=10&code=200&delay=0";

    /// <summary>
    /// 지정된 지연 후 응답을 반환하는 URL 생성
    /// </summary>
    /// <param name="delaySeconds">지연 시간 (초)</param>
    /// <returns>지연된 응답을 반환하는 URL</returns>
    public static string GetDelayUrl(int delaySeconds) => $"{HttpBin}/delay/{delaySeconds}";

    /// <summary>
    /// 지정된 크기의 랜덤 바이트를 반환하는 URL 생성
    /// </summary>
    /// <param name="bytes">바이트 수</param>
    /// <returns>랜덤 바이트를 반환하는 URL</returns>
    public static string GetRandomBytesUrl(int bytes) => $"{HttpBin}/bytes/{bytes}";

    /// <summary>
    /// HTML 응답을 반환하는 URL
    /// </summary>
    public static string HtmlUrl => $"{HttpBin}/html";

    /// <summary>
    /// JSON 응답을 반환하는 URL
    /// </summary>
    public static string JsonUrl => $"{HttpBin}/json";

    /// <summary>
    /// 요청 헤더를 에코하는 URL
    /// </summary>
    public static string HeadersUrl => $"{HttpBin}/headers";

    /// <summary>
    /// User-Agent를 반환하는 URL
    /// </summary>
    public static string UserAgentUrl => $"{HttpBin}/user-agent";

    /// <summary>
    /// IP 주소를 반환하는 URL
    /// </summary>
    public static string IpUrl => $"{HttpBin}/ip";
}
