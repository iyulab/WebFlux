namespace WebFlux.Core.Models;

/// <summary>
/// 웹 메타데이터 모델
/// 웹 표준 메타데이터를 지원합니다
/// </summary>
public class WebMetadata
{
    /// <summary>원본 URL</summary>
#if NET8_0_OR_GREATER
    public required string SourceUrl { get; init; }
#else
    public string SourceUrl { get; init; } = string.Empty;
#endif

    /// <summary>추출 시간</summary>
    public DateTimeOffset ExtractedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>기본 HTML 메타데이터</summary>
    public BasicHtmlMetadata Basic { get; init; } = new();

    /// <summary>Open Graph 메타데이터</summary>
    public OpenGraphMetadata OpenGraph { get; init; } = new();

    /// <summary>Twitter Cards 메타데이터</summary>
    public TwitterCardsMetadata TwitterCards { get; init; } = new();

    /// <summary>Schema.org 구조화 데이터</summary>
    public SchemaOrgMetadata SchemaOrg { get; init; } = new();

    /// <summary>Dublin Core 메타데이터</summary>
    public DublinCoreMetadata DublinCore { get; init; } = new();

    /// <summary>문서 구조 정보</summary>
    public DocumentStructure Structure { get; init; } = new();

    /// <summary>사이트 네비게이션 정보</summary>
    public SiteNavigation Navigation { get; init; } = new();

    /// <summary>기술적 메타데이터</summary>
    public TechnicalMetadata Technical { get; init; } = new();

    /// <summary>콘텐츠 분류 정보</summary>
    public ContentClassification Classification { get; init; } = new();

    /// <summary>접근성 정보</summary>
    public AccessibilityMetadata Accessibility { get; init; } = new();

    /// <summary>메타데이터 품질 점수 (0.0 - 1.0)</summary>
    public double QualityScore { get; init; }

    /// <summary>추가 사용자 정의 메타데이터</summary>
    public IReadOnlyDictionary<string, object> CustomMetadata { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// 기본 HTML 메타데이터
/// </summary>
public class BasicHtmlMetadata
{
    /// <summary>문서 제목</summary>
    public string? Title { get; init; }

    /// <summary>문서 설명</summary>
    public string? Description { get; init; }

    /// <summary>키워드</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>작성자</summary>
    public string? Author { get; init; }

    /// <summary>로봇 지시사항</summary>
    public string? Robots { get; init; }

    /// <summary>언어 코드</summary>
    public string? Language { get; init; }

    /// <summary>문자 인코딩</summary>
    public string? Charset { get; init; }

    /// <summary>뷰포트 설정</summary>
    public string? Viewport { get; init; }

    /// <summary>테마 컬러</summary>
    public string? ThemeColor { get; init; }

    /// <summary>정규 URL</summary>
    public string? CanonicalUrl { get; init; }

    /// <summary>대체 언어 링크</summary>
    public IReadOnlyList<AlternateLanguage> AlternateLanguages { get; init; } = Array.Empty<AlternateLanguage>();

    /// <summary>마지막 수정 시간</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>발행 시간</summary>
    public DateTimeOffset? Published { get; init; }
}

/// <summary>
/// Open Graph 메타데이터
/// </summary>
public class OpenGraphMetadata
{
    /// <summary>제목</summary>
    public string? Title { get; init; }

    /// <summary>설명</summary>
    public string? Description { get; init; }

    /// <summary>이미지 URL</summary>
    public string? Image { get; init; }

    /// <summary>페이지 URL</summary>
    public string? Url { get; init; }

    /// <summary>콘텐츠 타입</summary>
    public string? Type { get; init; }

    /// <summary>사이트 이름</summary>
    public string? SiteName { get; init; }

    /// <summary>로케일</summary>
    public string? Locale { get; init; }

    /// <summary>이미지 대체 텍스트</summary>
    public string? ImageAlt { get; init; }

    /// <summary>이미지 크기</summary>
    public ImageDimensions? ImageDimensions { get; init; }

    /// <summary>비디오 URL (해당되는 경우)</summary>
    public string? Video { get; init; }

    /// <summary>오디오 URL (해당되는 경우)</summary>
    public string? Audio { get; init; }
}

/// <summary>
/// Twitter Cards 메타데이터
/// </summary>
public class TwitterCardsMetadata
{
    /// <summary>카드 타입</summary>
    public string? Card { get; init; }

    /// <summary>제목</summary>
    public string? Title { get; init; }

    /// <summary>설명</summary>
    public string? Description { get; init; }

    /// <summary>이미지 URL</summary>
    public string? Image { get; init; }

    /// <summary>이미지 대체 텍스트</summary>
    public string? ImageAlt { get; init; }

    /// <summary>사이트 계정</summary>
    public string? Site { get; init; }

    /// <summary>작성자 계정</summary>
    public string? Creator { get; init; }

    /// <summary>플레이어 URL (비디오/오디오 카드)</summary>
    public string? Player { get; init; }

    /// <summary>플레이어 크기</summary>
    public ImageDimensions? PlayerDimensions { get; init; }
}

/// <summary>
/// Schema.org 구조화 데이터
/// </summary>
public class SchemaOrgMetadata
{
    /// <summary>주요 엔티티 타입</summary>
    public string? MainEntityType { get; init; }

    /// <summary>조직 정보</summary>
    public OrganizationInfo? Organization { get; init; }

    /// <summary>개인 정보</summary>
    public PersonInfo? Person { get; init; }

    /// <summary>기사 정보</summary>
    public ArticleInfo? Article { get; init; }

    /// <summary>소프트웨어 정보</summary>
    public SoftwareInfo? Software { get; init; }

    /// <summary>제품 정보</summary>
    public ProductInfo? Product { get; init; }

    /// <summary>웹사이트 정보</summary>
    public WebSiteInfo? WebSite { get; init; }

    /// <summary>빵 부스러기 네비게이션</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; init; } = Array.Empty<BreadcrumbItem>();

    /// <summary>FAQ 항목</summary>
    public IReadOnlyList<FaqItem> FaqItems { get; init; } = Array.Empty<FaqItem>();

    /// <summary>원시 JSON-LD 데이터</summary>
    public IReadOnlyList<string> RawJsonLd { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Dublin Core 메타데이터
/// </summary>
public class DublinCoreMetadata
{
    /// <summary>제목</summary>
    public string? Title { get; init; }

    /// <summary>작성자</summary>
    public string? Creator { get; init; }

    /// <summary>주제</summary>
    public string? Subject { get; init; }

    /// <summary>설명</summary>
    public string? Description { get; init; }

    /// <summary>발행자</summary>
    public string? Publisher { get; init; }

    /// <summary>언어</summary>
    public string? Language { get; init; }

    /// <summary>형식</summary>
    public string? Format { get; init; }

    /// <summary>타입</summary>
    public string? Type { get; init; }

    /// <summary>날짜</summary>
    public string? Date { get; init; }

    /// <summary>커버리지</summary>
    public string? Coverage { get; init; }

    /// <summary>권한</summary>
    public string? Rights { get; init; }
}

/// <summary>
/// 문서 구조 정보
/// </summary>
public class DocumentStructure
{
    /// <summary>헤딩 계층 구조</summary>
    public IReadOnlyList<HeadingInfo> Headings { get; init; } = Array.Empty<HeadingInfo>();

    /// <summary>섹션 수</summary>
    public int SectionCount { get; init; }

    /// <summary>문단 수</summary>
    public int ParagraphCount { get; init; }

    /// <summary>링크 수</summary>
    public int LinkCount { get; init; }

    /// <summary>이미지 수</summary>
    public int ImageCount { get; init; }

    /// <summary>테이블 수</summary>
    public int TableCount { get; init; }

    /// <summary>목록 수</summary>
    public int ListCount { get; init; }

    /// <summary>코드 블록 수</summary>
    public int CodeBlockCount { get; init; }

    /// <summary>추정 읽기 시간 (분)</summary>
    public int EstimatedReadingTimeMinutes { get; init; }

    /// <summary>문서 복잡도 점수 (0.0 - 1.0)</summary>
    public double ComplexityScore { get; init; }
}

/// <summary>
/// 사이트 네비게이션 정보
/// </summary>
public class SiteNavigation
{
    /// <summary>기본 네비게이션 링크</summary>
    public IReadOnlyList<NavigationLink> MainNavigation { get; init; } = Array.Empty<NavigationLink>();

    /// <summary>풋터 링크</summary>
    public IReadOnlyList<NavigationLink> FooterLinks { get; init; } = Array.Empty<NavigationLink>();

    /// <summary>사이드바 링크</summary>
    public IReadOnlyList<NavigationLink> SidebarLinks { get; init; } = Array.Empty<NavigationLink>();

    /// <summary>관련 링크</summary>
    public IReadOnlyList<NavigationLink> RelatedLinks { get; init; } = Array.Empty<NavigationLink>();

    /// <summary>홈페이지 링크</summary>
    public string? HomeUrl { get; init; }

    /// <summary>사이트맵 링크</summary>
    public string? SitemapUrl { get; init; }

    /// <summary>RSS 피드 링크</summary>
    public string? RssFeedUrl { get; init; }
}

/// <summary>
/// 기술적 메타데이터
/// </summary>
public class TechnicalMetadata
{
    /// <summary>콘텐츠 타입</summary>
    public string? ContentType { get; init; }

    /// <summary>HTTP 상태 코드</summary>
    public int? StatusCode { get; init; }

    /// <summary>응답 헤더</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>JavaScript 필요 여부</summary>
    public bool RequiresJavaScript { get; init; }

    /// <summary>모바일 친화적 여부</summary>
    public bool IsMobileFriendly { get; init; }

    /// <summary>PWA 여부</summary>
    public bool IsPwa { get; init; }

    /// <summary>AMP 페이지 여부</summary>
    public bool IsAmpPage { get; init; }

    /// <summary>페이지 로드 성능 점수 (0-100)</summary>
    public int? PerformanceScore { get; init; }

    /// <summary>보안 정보</summary>
    public SecurityInfo Security { get; init; } = new();
}

/// <summary>
/// 콘텐츠 분류 정보
/// </summary>
public class ContentClassification
{
    /// <summary>콘텐츠 카테고리</summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

    /// <summary>태그</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>콘텐츠 타입 (기사, 제품, 문서 등)</summary>
    public string? ContentType { get; init; }

    /// <summary>대상 독자</summary>
    public string? TargetAudience { get; init; }

    /// <summary>콘텐츠 레벨 (초급, 중급, 고급)</summary>
    public string? ContentLevel { get; init; }

    /// <summary>주제 키워드</summary>
    public IReadOnlyList<string> TopicKeywords { get; init; } = Array.Empty<string>();

    /// <summary>감정 분석 결과</summary>
    public string? SentimentAnalysis { get; init; }
}

/// <summary>
/// 접근성 메타데이터
/// </summary>
public class AccessibilityMetadata
{
    /// <summary>alt 텍스트 있는 이미지 비율</summary>
    public double ImageAltTextCoverage { get; init; }

    /// <summary>헤딩 구조 적절성</summary>
    public bool HasProperHeadingStructure { get; init; }

    /// <summary>스킵 네비게이션 링크 존재</summary>
    public bool HasSkipNavigation { get; init; }

    /// <summary>색상 대비 정보</summary>
    public string? ColorContrast { get; init; }

    /// <summary>키보드 네비게이션 지원</summary>
    public bool SupportsKeyboardNavigation { get; init; }

    /// <summary>ARIA 레이블 사용</summary>
    public bool UsesAriaLabels { get; init; }

    /// <summary>접근성 점수 (0-100)</summary>
    public int AccessibilityScore { get; init; }
}

// Supporting classes
public class AlternateLanguage
{
    public string? Language { get; init; }
    public string? Url { get; init; }
}

public class ImageDimensions
{
    public int Width { get; init; }
    public int Height { get; init; }
}

public class OrganizationInfo
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? Logo { get; init; }
    public string? Description { get; init; }
}

public class PersonInfo
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? Image { get; init; }
    public string? JobTitle { get; init; }
}

public class ArticleInfo
{
    public string? Headline { get; init; }
    public DateTimeOffset? DatePublished { get; init; }
    public DateTimeOffset? DateModified { get; init; }
    public string? Author { get; init; }
    public string? Publisher { get; init; }
    public string? Section { get; init; }
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
}

public class SoftwareInfo
{
    public string? Name { get; init; }
    public string? Version { get; init; }
    public string? ProgrammingLanguage { get; init; }
    public string? RuntimePlatform { get; init; }
    public string? License { get; init; }
    public string? CodeRepository { get; init; }
}

public class ProductInfo
{
    public string? Name { get; init; }
    public string? Brand { get; init; }
    public string? Category { get; init; }
    public string? Price { get; init; }
    public string? Currency { get; init; }
    public string? Availability { get; init; }
}

public class WebSiteInfo
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? SearchAction { get; init; }
    public string? PotentialAction { get; init; }
}

// Note: BreadcrumbItem is already defined in ContentRelationshipModels.cs

public class FaqItem
{
    public string? Question { get; init; }
    public string? Answer { get; init; }
}

public class HeadingInfo
{
    public int Level { get; init; }
    public string? Text { get; init; }
    public string? Id { get; init; }
}

public class NavigationLink
{
    public string? Text { get; init; }
    public string? Url { get; init; }
    public string? Title { get; init; }
}

public class SecurityInfo
{
    public bool IsHttps { get; init; }
    public bool HasCsp { get; init; }
    public bool HasHsts { get; init; }
    public IReadOnlyList<string> SecurityHeaders { get; init; } = Array.Empty<string>();
}