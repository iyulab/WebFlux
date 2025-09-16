namespace WebFlux.Core.Options;

/// <summary>
/// 웹 크롤링 옵션을 정의하는 클래스
/// </summary>
public class CrawlOptions
{
    /// <summary>
    /// 최대 크롤링 페이지 수 (기본값: 100)
    /// </summary>
    public int MaxPages { get; set; } = 100;

    /// <summary>
    /// 최대 크롤링 깊이 (기본값: 5)
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// 요청 간 지연 시간 (밀리초, 기본값: 1000)
    /// </summary>
    public int DelayBetweenRequestsMs { get; set; } = 1000;

    /// <summary>
    /// 요청 간 지연 시간 (TimeSpan, llms.txt 최적화용)
    /// </summary>
    public TimeSpan? DelayBetweenRequests { get; set; }

    /// <summary>
    /// 동시 요청 수 (기본값: 3)
    /// </summary>
    public int ConcurrentRequests { get; set; } = 3;

    /// <summary>
    /// 요청 타임아웃 (초, 기본값: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// robots.txt 준수 여부 (기본값: true)
    /// </summary>
    public bool RespectRobotsTxt { get; set; } = true;

    /// <summary>
    /// User-Agent 문자열
    /// </summary>
    public string UserAgent { get; set; } = "WebFlux/1.0 (+https://github.com/webflux/webflux)";

    /// <summary>
    /// 허용할 콘텐츠 타입 목록
    /// </summary>
    public ISet<string> AllowedContentTypes { get; set; } = new HashSet<string>
    {
        "text/html",
        "application/xhtml+xml",
        "text/plain",
        "application/json",
        "application/xml",
        "text/xml"
    };

    /// <summary>
    /// 제외할 파일 확장자 목록
    /// </summary>
    public ISet<string> ExcludedExtensions { get; set; } = new HashSet<string>
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".zip", ".rar", ".tar", ".gz", ".7z",
        ".mp3", ".mp4", ".avi", ".mov", ".wmv", ".flv",
        ".css", ".js", ".woff", ".woff2", ".ttf", ".eot"
    };

    /// <summary>
    /// 포함할 URL 패턴 (정규식)
    /// </summary>
    public IList<string> IncludeUrlPatterns { get; set; } = new List<string>();

    /// <summary>
    /// 제외할 URL 패턴 (정규식)
    /// </summary>
    public IList<string> ExcludeUrlPatterns { get; set; } = new List<string>();

    /// <summary>
    /// 시작 URL 목록
    /// </summary>
    public IList<string> StartUrls { get; set; } = new List<string>();

    /// <summary>
    /// 허용할 도메인 목록 (비어있으면 모든 도메인 허용)
    /// </summary>
    public ISet<string> AllowedDomains { get; set; } = new HashSet<string>();

    /// <summary>
    /// 크롤링 전략
    /// </summary>
    public CrawlStrategy Strategy { get; set; } = CrawlStrategy.BreadthFirst;

    /// <summary>
    /// 외부 링크 따라가기 여부 (기본값: false)
    /// </summary>
    public bool FollowExternalLinks { get; set; } = false;

    /// <summary>
    /// 요청 타임아웃 (TimeSpan)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 추가 헤더
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// 제외 패턴 (정규식)
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// 포함 패턴 (정규식)
    /// </summary>
    public List<string> IncludePatterns { get; set; } = new();

    /// <summary>
    /// 최대 동시 연결 수 (llms.txt 최적화용)
    /// </summary>
    public int MaxConcurrency { get; set; } = 3;

    /// <summary>
    /// 우선순위 URL 목록
    /// </summary>
    public List<string> PriorityUrls { get; set; } = new();

    /// <summary>
    /// 이미지 다운로드 여부 (기본값: false)
    /// </summary>
    public bool DownloadImages { get; set; } = false;

    /// <summary>
    /// 최대 이미지 크기 (바이트, 기본값: 5MB)
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// 캐시 사용 여부 (기본값: true)
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// 캐시 만료 시간 (분, 기본값: 60)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 재시도 횟수 (기본값: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 커스텀 헤더
    /// </summary>
    public IDictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// 크롤링 전략 열거형
/// </summary>
public enum CrawlStrategy
{
    /// <summary>너비 우선 탐색</summary>
    BreadthFirst,
    /// <summary>깊이 우선 탐색</summary>
    DepthFirst,
    /// <summary>Sitemap 기반</summary>
    Sitemap,
    /// <summary>우선순위 기반</summary>
    Priority,
    /// <summary>llms.txt 메타데이터 기반 지능형 크롤링</summary>
    Intelligent
}