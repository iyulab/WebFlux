using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

using WebFlux.Core.Utilities;

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
    /// 동시 요청 수 (기본값: 3)
    /// </summary>
    public int ConcurrentRequests { get; set; } = 3;

    /// <summary>
    /// robots.txt 준수 여부 (기본값: true)
    /// </summary>
    public bool RespectRobotsTxt { get; set; } = true;

    /// <summary>
    /// 이 크롤이 보내는 모든 요청의 <c>User-Agent</c> (기본값: <see cref="WebFluxUserAgent.Default"/>).
    /// </summary>
    /// <remarks>
    /// 같은 값이 두 곳에 쓰인다 — 전송되는 헤더, 그리고 robots.txt 그룹 선택. 그룹은 RFC 9309 2.2.1 대로
    /// <b>제품 토큰</b>으로 고른다: <c>"MyBot/1.0 (+https://…)"</c> 는 <c>User-agent: MyBot</c> 그룹을 따른다.
    /// 비우면 기본값을 쓴다.
    /// </remarks>
    public string UserAgent { get; set; } = WebFluxUserAgent.Default;

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
    /// 시작 URL 의 호스트에 더해 크롤을 허용할 호스트. 항목은 그 호스트 자체와 하위 도메인을
    /// 덮는다(<c>example.com</c> 이 <c>docs.example.com</c> 을 덮는다).
    /// </summary>
    /// <remarks>
    /// 비어 있으면(기본값) 아무것도 넓히지 않는다 - 시작 URL 과 같은 호스트만 크롤한다.
    /// 「비어 있으면 모든 도메인 허용」이라고 적혀 있던 종전 문구는 구현과 반대였다.
    /// 모든 호스트를 원하면 <see cref="FollowExternalLinks"/> 를 쓴다.
    /// </remarks>
    public ISet<string> AllowedDomains { get; set; } = new HashSet<string>();

    /// <summary>
    /// 크롤링 전략
    /// </summary>
    public CrawlStrategy Strategy { get; set; } = CrawlStrategy.BreadthFirst;

    /// <summary>
    /// 시작 URL 과 다른 호스트의 링크도 따라간다 (기본값: <c>false</c>).
    /// </summary>
    /// <remarks>
    /// <c>true</c> 면 호스트 제한이 사라진다 - <see cref="ExcludeUrlPatterns"/> 나
    /// <see cref="MaxPages"/> 같은 다른 제한이 없으면 크롤이 넓게 퍼질 수 있다.
    /// 특정 호스트만 더하려면 <see cref="AllowedDomains"/> 쪽이 맞다.
    /// </remarks>
    public bool FollowExternalLinks { get; set; }

    /// <summary>
    /// 재시도 횟수 (기본값: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 요청에 실을 커스텀 헤더.
    /// </summary>
    /// <remarks>
    /// 요청마다 실린다(공유 <c>HttpClient</c> 의 기본 헤더를 바꾸지 않으므로 동시에 도는 다른 크롤로
    /// 새지 않는다) — 페이지, robots.txt, sitemap 요청 모두, 정적 크롤러와 Playwright 크롤러 모두.
    /// <c>User-Agent</c> 는 여기가 아니라 <see cref="UserAgent"/> 로 준다.
    /// </remarks>
    public IDictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 동적 렌더링 사용 여부 (기본값: false)
    /// JavaScript로 렌더링되는 SPA (React, Vue, Angular) 처리
    /// </summary>
    /// <remarks>
    /// <c>true</c>로 두면 <see cref="CrawlStrategy.Dynamic"/>으로 라우팅되므로
    /// <b>WebFlux.Playwright 패키지가 필요하다.</b> 없으면 어느 패키지가 빠졌는지 알려주는
    /// <see cref="InvalidOperationException"/>으로 실패한다.
    /// </remarks>
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
    /// 요청 하나의 타임아웃 (밀리초, 기본값: 30000). 응답 본문 수신까지 포함한다.
    /// </summary>
    /// <remarks>
    /// 이 크롤이 보내는 <b>모든</b> 요청에 적용된다 — 페이지, 그 페이지의 robots.txt, sitemap.
    /// 정적(HTTP) 크롤러와 Playwright 크롤러가 같은 값을 읽는다.
    /// <para>
    /// 타임아웃은 <b>재시도하지 않는다</b>(<see cref="MaxRetries"/> 와 무관): 2초를 요청한 호출자는
    /// 약 2초 뒤에 답을 받아야 하고, 한 번 느린 서버는 대개 다시 느리다. 더 기다릴 수 있으면 이 값을
    /// 키운다. 결과는 <c>CrawlResult.TimedOut = true</c> 로 도착한다.
    /// </para>
    /// </remarks>
    public int TimeoutMs { get; set; } = 30000;

    // ===================================================================
    // AI 메타데이터 추출 옵션
    // ===================================================================

    /// <summary>
    /// AI 메타데이터 추출 활성화 (기본값: false).
    /// true 면 크롤 경로(<c>ProcessWebsiteAsync</c>)에서 컨테이너의 <c>IWebMetadataExtractor</c> — 없으면
    /// 등록된 <c>ITextCompletionService</c> 로 만든 기본 추출기 — 가 <see cref="MetadataSchema"/> 에 따라
    /// 메타데이터를 추출해 결과의 <c>Metadata</c> 에 병합합니다. 둘 다 없으면 경고 한 번을 찍고 HTML
    /// 메타데이터만 실립니다(무음 no-op 아님). <see cref="MinConfidence"/> 미만의 AI 결과는 병합하지 않습니다.
    /// </summary>
    public bool EnableMetadataExtraction { get; set; }

    /// <summary>
    /// 메타데이터 스키마 (기본값: General)
    /// 웹 콘텐츠 타입에 따라 최적화된 추출 전략을 선택합니다
    /// General: 일반 웹 콘텐츠, TechnicalDoc: 기술 문서, ProductManual: 제품 페이지, Article: 블로그/뉴스
    /// </summary>
    public MetadataSchema MetadataSchema { get; set; } = MetadataSchema.General;

    /// <summary>
    /// 커스텀 추출 프롬프트 (<c>MetadataSchema.Custom</c> 사용 시 필수 — 없으면
    /// <see cref="Validate"/> 가 거부합니다). 특정 도메인에 맞는 메타데이터 추출을 위한 사용자 정의 프롬프트.
    /// </summary>
    public string? CustomMetadataPrompt { get; set; }

    /// <summary>
    /// HTML 메타데이터 사용 여부 (기본값: true).
    /// true 면 HTML 페이지의 meta 태그 · OpenGraph · Twitter Card · JSON-LD 를 읽어 결과 <c>Metadata.HtmlMetadata</c>
    /// 에 싣고(AI 불필요), 추출기가 비워 둔 제목·설명만 OpenGraph 로 채웁니다 — 이미 있는 값은 덮지 않습니다.
    /// AI 추출이 켜져 있으면 같은 스냅숏이 프롬프트 힌트로도 쓰입니다.
    /// </summary>
    public bool UseHtmlMetadata { get; set; } = true;

    /// <summary>
    /// 최소 신뢰도 임계값 (기본값: 0.6, 범위: 0.0 - 1.0)
    /// 이 값보다 낮은 신뢰도의 메타데이터는 포함되지 않습니다
    /// </summary>
    public float MinConfidence { get; set; } = 0.6f;

    /// <summary>
    /// AI 메타데이터 추출에 보내는 콘텐츠의 최대 문자 수 (기본값: 8000; 0 이하 = 자르지 않음).
    /// 긴 문서는 제목 + 헤딩 + 본문 첫 N자로 샘플링돼 그 길이 안에서만 프롬프트에 실립니다
    /// (<c>MetadataContentSampler</c>). 0.13.0 이전에는 이 값을 읽는 코드가 없어 본문 전체가 갔습니다.
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

        if (TimeoutMs <= 0)
            errors.Add("TimeoutMs must be greater than 0");

        if (MinConfidence < 0 || MinConfidence > 1)
            errors.Add("MinConfidence must be between 0 and 1");

        if (MaxRetries < 0)
            errors.Add("MaxRetries must be greater than or equal to 0");

        if (DelayBetweenRequestsMs < 0)
            errors.Add("DelayBetweenRequestsMs must be greater than or equal to 0");

        if (MetadataSchema == MetadataSchema.Custom && string.IsNullOrWhiteSpace(CustomMetadataPrompt))
            errors.Add("CustomMetadataPrompt is required when MetadataSchema is Custom");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}