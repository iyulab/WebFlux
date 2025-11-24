namespace WebFlux.Core.Models;

/// <summary>
/// 웹 문서의 구조화된 메타데이터
/// SEO, Open Graph, Schema.org 등 웹 표준 메타데이터를 포함
/// LLM 호출 없이도 고품질 메타데이터를 제공
/// </summary>
public class WebDocumentMetadata
{
    // ===================================================================
    // 기본 정보
    // ===================================================================

    /// <summary>
    /// 웹 페이지 URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// 페이지 제목 (&lt;title&gt; 태그)
    /// </summary>
    public required string Title { get; init; }

    // ===================================================================
    // SEO 메타데이터
    // ===================================================================

    /// <summary>
    /// 페이지 설명 (&lt;meta name="description"&gt;)
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 키워드 목록 (&lt;meta name="keywords"&gt;)
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 작성자 (&lt;meta name="author"&gt;)
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// 로봇 지시어 (&lt;meta name="robots"&gt;)
    /// 예: "index, follow", "noindex, nofollow"
    /// </summary>
    public string? Robots { get; init; }

    /// <summary>
    /// 정식 URL (&lt;link rel="canonical"&gt;)
    /// </summary>
    public string? CanonicalUrl { get; init; }

    // ===================================================================
    // Open Graph 메타데이터
    // ===================================================================

    /// <summary>
    /// OG 제목 (og:title)
    /// </summary>
    public string? OgTitle { get; init; }

    /// <summary>
    /// OG 설명 (og:description)
    /// </summary>
    public string? OgDescription { get; init; }

    /// <summary>
    /// OG 이미지 URL (og:image)
    /// </summary>
    public string? OgImage { get; init; }

    /// <summary>
    /// OG 타입 (og:type)
    /// 예: "article", "website", "product", "video.movie"
    /// </summary>
    public string? OgType { get; init; }

    /// <summary>
    /// OG 사이트 이름 (og:site_name)
    /// </summary>
    public string? OgSiteName { get; init; }

    /// <summary>
    /// OG 로케일 (og:locale)
    /// 예: "ko_KR", "en_US"
    /// </summary>
    public string? OgLocale { get; init; }

    // ===================================================================
    // 시간 정보
    // ===================================================================

    /// <summary>
    /// 발행일 (article:published_time)
    /// </summary>
    public DateTime? PublishedAt { get; init; }

    /// <summary>
    /// 수정일 (article:modified_time)
    /// </summary>
    public DateTime? ModifiedAt { get; init; }

    // ===================================================================
    // Schema.org 구조화 데이터
    // ===================================================================

    /// <summary>
    /// Schema.org @type
    /// 예: "Article", "Product", "FAQPage", "HowTo", "Recipe"
    /// </summary>
    public string? SchemaOrgType { get; init; }

    /// <summary>
    /// Schema.org 구조화 데이터 (JSON-LD에서 추출)
    /// 스키마 타입별 주요 속성들
    /// </summary>
    public IReadOnlyDictionary<string, string> StructuredData { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// JSON-LD 원본 데이터 (전체 구조 보존)
    /// </summary>
    public IReadOnlyList<object> JsonLdData { get; init; } = Array.Empty<object>();

    // ===================================================================
    // 언어 정보
    // ===================================================================

    /// <summary>
    /// 언어 코드 (ISO 639-1)
    /// 예: "ko", "en", "ja"
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// 언어 감지 방법
    /// </summary>
    public LanguageDetectionMethod LanguageDetectionMethod { get; init; }

    // ===================================================================
    // 사이트 컨텍스트
    // ===================================================================

    /// <summary>
    /// 사이트 내 위치 및 네비게이션 정보
    /// </summary>
    public SiteContext? SiteContext { get; init; }

    // ===================================================================
    // 추가 정보
    // ===================================================================

    /// <summary>
    /// Twitter Card 데이터
    /// </summary>
    public TwitterCardData? TwitterCard { get; init; }

    /// <summary>
    /// RSS/Atom 피드 URL
    /// </summary>
    public string? FeedUrl { get; init; }

    /// <summary>
    /// 도메인
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// 메타데이터 추출 시간
    /// </summary>
    public DateTime ExtractedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 추가 메타데이터 (확장용)
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalData { get; init; } =
        new Dictionary<string, object>();

    // ===================================================================
    // 유틸리티 메서드
    // ===================================================================

    /// <summary>
    /// 유효 제목 반환 (OG 제목 우선, 없으면 기본 제목)
    /// </summary>
    public string GetEffectiveTitle() => OgTitle ?? Title;

    /// <summary>
    /// 유효 설명 반환 (OG 설명 우선, 없으면 SEO 설명)
    /// </summary>
    public string? GetEffectiveDescription() => OgDescription ?? Description;

    /// <summary>
    /// 카테고리 자동 분류 (OgType 기반)
    /// </summary>
    public string GetCategory() => OgType switch
    {
        "article" => "Article",
        "product" => "Product",
        "video.movie" or "video.episode" => "Video",
        "music.song" or "music.album" => "Music",
        "book" => "Book",
        "profile" => "Profile",
        "website" => "Website",
        _ => "General"
    };
}

/// <summary>
/// 언어 감지 방법 열거형
/// </summary>
public enum LanguageDetectionMethod
{
    /// <summary>
    /// HTML lang 속성에서 감지
    /// </summary>
    HtmlLangAttribute,

    /// <summary>
    /// Content-Language HTTP 헤더에서 감지
    /// </summary>
    HttpHeader,

    /// <summary>
    /// 콘텐츠 분석을 통한 감지
    /// </summary>
    ContentAnalysis,

    /// <summary>
    /// 감지 실패 (기본값 사용)
    /// </summary>
    Unknown
}

/// <summary>
/// 사이트 컨텍스트 정보
/// Breadcrumbs, 관련 페이지 등 사이트 내 위치 정보
/// </summary>
public class SiteContext
{
    /// <summary>
    /// Breadcrumb 계층 구조
    /// 예: ["Documentation", "API Reference", "Search API"]
    /// </summary>
    public IReadOnlyList<string> Breadcrumbs { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Breadcrumb 항목 (URL 포함)
    /// 기존 BreadcrumbItem 모델 재사용 (ContentRelationshipModels.cs)
    /// </summary>
    public IReadOnlyList<BreadcrumbItem> BreadcrumbItems { get; init; } = Array.Empty<BreadcrumbItem>();

    /// <summary>
    /// 관련 페이지 URL (내부 링크 분석)
    /// </summary>
    public IReadOnlyList<string> RelatedPages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 이전 페이지 URL
    /// </summary>
    public string? PreviousPage { get; init; }

    /// <summary>
    /// 다음 페이지 URL
    /// </summary>
    public string? NextPage { get; init; }

    /// <summary>
    /// 사이트맵 우선순위 (0.0 - 1.0)
    /// </summary>
    public double? SitemapPriority { get; init; }

    /// <summary>
    /// 사이트맵 변경 빈도
    /// </summary>
    public string? ChangeFrequency { get; init; }
}

// BreadcrumbItem은 ContentRelationshipModels.cs에 정의되어 있습니다
