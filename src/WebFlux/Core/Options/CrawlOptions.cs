using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// 웹 크롤링 옵션을 정의하는 클래스
/// </summary>
public class CrawlOptions : IValidatable
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
    /// 요청 간 지연 시간 (밀리초, DelayBetweenRequestsMs의 별칭)
    /// </summary>
    public int DelayMs
    {
        get => DelayBetweenRequestsMs;
        set => DelayBetweenRequestsMs = value;
    }

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
    public bool FollowExternalLinks { get; set; }

    /// <summary>
    /// 요청 타임아웃 (TimeSpan)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 추가 헤더
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

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
    public bool DownloadImages { get; set; }

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

    /// <summary>
    /// 동적 렌더링 사용 여부 (Playwright 사용, 기본값: false)
    /// JavaScript로 렌더링되는 SPA (React, Vue, Angular) 처리
    /// </summary>
    public bool UseDynamicRendering { get; set; }

    /// <summary>
    /// 동적 렌더링 시 대기할 CSS 셀렉터
    /// 특정 요소가 로드될 때까지 대기
    /// </summary>
    public string? WaitForSelector { get; set; }

    /// <summary>
    /// 자동 스크롤 활성화 (Lazy Loading 콘텐츠, 기본값: true)
    /// </summary>
    public bool EnableScrolling { get; set; } = true;

    /// <summary>
    /// 요청 타임아웃 (밀리초, 기본값: 30000)
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    // ===================================================================
    // AI 메타데이터 추출 옵션
    // ===================================================================

    /// <summary>
    /// AI 메타데이터 추출 활성화 (기본값: false)
    /// true일 경우 IWebMetadataExtractor를 사용하여 콘텐츠에서 메타데이터를 추출합니다
    /// </summary>
    public bool EnableMetadataExtraction { get; set; }

    /// <summary>
    /// 메타데이터 스키마 (기본값: General)
    /// 웹 콘텐츠 타입에 따라 최적화된 추출 전략을 선택합니다
    /// General: 일반 웹 콘텐츠, TechnicalDoc: 기술 문서, ProductManual: 제품 페이지, Article: 블로그/뉴스
    /// </summary>
    public MetadataSchema MetadataSchema { get; set; } = MetadataSchema.General;

    /// <summary>
    /// 커스텀 추출 프롬프트 (MetadataSchema.Custom 사용 시 필수)
    /// 특정 도메인에 맞는 메타데이터 추출을 위한 사용자 정의 프롬프트
    /// </summary>
    public string? CustomMetadataPrompt { get; set; }

    /// <summary>
    /// HTML 메타데이터 사용 여부 (기본값: true)
    /// true일 경우 HTML meta 태그, OpenGraph, Twitter Card를 AI 프롬프트 힌트로 사용합니다
    /// </summary>
    public bool UseHtmlMetadata { get; set; } = true;

    /// <summary>
    /// 최소 신뢰도 임계값 (기본값: 0.6, 범위: 0.0 - 1.0)
    /// 이 값보다 낮은 신뢰도의 메타데이터는 포함되지 않습니다
    /// </summary>
    public float MinConfidence { get; set; } = 0.6f;

    /// <summary>
    /// 메타데이터 추출용 최대 문자 수 (기본값: 8000)
    /// 토큰 최적화를 위해 콘텐츠를 샘플링할 최대 문자 수
    /// 긴 문서의 경우 제목 + 헤딩 + 첫 N자를 사용합니다
    /// </summary>
    public int MetadataExtractionMaxChars { get; set; } = 8000;

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxPages <= 0)
            errors.Add("MaxPages must be greater than 0");

        if (MaxDepth < 0)
            errors.Add("MaxDepth must be greater than or equal to 0");

        if (ConcurrentRequests <= 0)
            errors.Add("ConcurrentRequests must be greater than 0");

        if (TimeoutSeconds <= 0)
            errors.Add("TimeoutSeconds must be greater than 0");

        if (MinConfidence < 0 || MinConfidence > 1)
            errors.Add("MinConfidence must be between 0 and 1");

        if (MaxRetries < 0)
            errors.Add("MaxRetries must be greater than or equal to 0");

        if (DelayBetweenRequestsMs < 0)
            errors.Add("DelayBetweenRequestsMs must be greater than or equal to 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}