namespace WebFlux.Core.Models;

/// <summary>
/// 콘텐츠 관계 분석 결과
/// </summary>
public class ContentRelationshipAnalysisResult
{
    /// <summary>
    /// 분석된 페이지들
    /// </summary>
    public List<PageRelationshipInfo> Pages { get; set; } = new();

    /// <summary>
    /// 페이지 간 링크 관계
    /// </summary>
    public List<PageLinkRelationship> LinkRelationships { get; set; } = new();

    /// <summary>
    /// 네비게이션 구조
    /// </summary>
    public NavigationStructureResult? NavigationStructure { get; set; }

    /// <summary>
    /// 콘텐츠 계층 구조
    /// </summary>
    public ContentHierarchyResult? ContentHierarchy { get; set; }

    /// <summary>
    /// 콘텐츠 클러스터
    /// </summary>
    public ContentClusterResult? ContentClusters { get; set; }

    /// <summary>
    /// 사이트 토폴로지 메트릭
    /// </summary>
    public SiteTopologyMetrics? TopologyMetrics { get; set; }

    /// <summary>
    /// 분석 품질 점수 (0.0-1.0)
    /// </summary>
    public double QualityScore { get; set; }

    /// <summary>
    /// 분석 수행 시간
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 페이지 관계 정보
/// </summary>
public class PageRelationshipInfo
{
    /// <summary>
    /// 페이지 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 페이지 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 메타 설명
    /// </summary>
    public string MetaDescription { get; set; } = string.Empty;

    /// <summary>
    /// 페이지 타입
    /// </summary>
    public PageType PageType { get; set; }

    /// <summary>
    /// 콘텐츠 카테고리
    /// </summary>
    public string ContentCategory { get; set; } = string.Empty;

    /// <summary>
    /// 발신 링크 (이 페이지에서 다른 페이지로)
    /// </summary>
    public List<OutboundLink> OutboundLinks { get; set; } = new();

    /// <summary>
    /// 수신 링크 (다른 페이지에서 이 페이지로)
    /// </summary>
    public List<InboundLink> InboundLinks { get; set; } = new();

    /// <summary>
    /// 페이지 깊이 (루트에서의 거리)
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// 페이지 랭크 점수
    /// </summary>
    public double PageRankScore { get; set; }

    /// <summary>
    /// 콘텐츠 유사도 해시
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// 구조적 중요도
    /// </summary>
    public double StructuralImportance { get; set; }

    /// <summary>
    /// 주요 키워드
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// 브레드크럼 경로
    /// </summary>
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = new();

    /// <summary>
    /// 언어 코드
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 마지막 수정 날짜
    /// </summary>
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// 발신 링크 정보
/// </summary>
public class OutboundLink
{
    /// <summary>
    /// 대상 URL
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 링크 텍스트
    /// </summary>
    public string AnchorText { get; set; } = string.Empty;

    /// <summary>
    /// 링크 타입
    /// </summary>
    public LinkType LinkType { get; set; }

    /// <summary>
    /// 링크 위치
    /// </summary>
    public LinkPosition Position { get; set; }

    /// <summary>
    /// 링크 가중치
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// 컨텍스트 정보
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// rel 속성
    /// </summary>
    public string RelAttribute { get; set; } = string.Empty;
}

/// <summary>
/// 수신 링크 정보
/// </summary>
public class InboundLink
{
    /// <summary>
    /// 소스 URL
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// 링크 텍스트
    /// </summary>
    public string AnchorText { get; set; } = string.Empty;

    /// <summary>
    /// 링크 타입
    /// </summary>
    public LinkType LinkType { get; set; }

    /// <summary>
    /// 링크 위치
    /// </summary>
    public LinkPosition Position { get; set; }

    /// <summary>
    /// 링크 권한 (PageRank 전달)
    /// </summary>
    public double Authority { get; set; }

    /// <summary>
    /// 컨텍스트 정보
    /// </summary>
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// 페이지 간 링크 관계
/// </summary>
public class PageLinkRelationship
{
    /// <summary>
    /// 소스 페이지 URL
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// 대상 페이지 URL
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// 관계 유형
    /// </summary>
    public RelationshipType RelationshipType { get; set; }

    /// <summary>
    /// 관계 강도 (0.0-1.0)
    /// </summary>
    public double Strength { get; set; }

    /// <summary>
    /// 양방향 관계 여부
    /// </summary>
    public bool IsBidirectional { get; set; }

    /// <summary>
    /// 관계 메타데이터
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 네비게이션 구조 분석 결과
/// </summary>
public class NavigationStructureResult
{
    /// <summary>
    /// 주 네비게이션 메뉴
    /// </summary>
    public NavigationMenu? PrimaryNavigation { get; set; }

    /// <summary>
    /// 보조 네비게이션 메뉴들
    /// </summary>
    public List<NavigationMenu> SecondaryNavigations { get; set; } = new();

    /// <summary>
    /// 푸터 네비게이션
    /// </summary>
    public NavigationMenu? FooterNavigation { get; set; }

    /// <summary>
    /// 브레드크럼 패턴
    /// </summary>
    public List<BreadcrumbPattern> BreadcrumbPatterns { get; set; } = new();

    /// <summary>
    /// 네비게이션 일관성 점수 (0.0-1.0)
    /// </summary>
    public double ConsistencyScore { get; set; }

    /// <summary>
    /// 네비게이션 효율성 점수 (0.0-1.0)
    /// </summary>
    public double EfficiencyScore { get; set; }

    /// <summary>
    /// 접근성 점수 (0.0-1.0)
    /// </summary>
    public double AccessibilityScore { get; set; }
}

/// <summary>
/// 네비게이션 메뉴
/// </summary>
public class NavigationMenu
{
    /// <summary>
    /// 메뉴 타입
    /// </summary>
    public NavigationType Type { get; set; }

    /// <summary>
    /// 메뉴 항목들
    /// </summary>
    public List<NavigationItem> Items { get; set; } = new();

    /// <summary>
    /// 메뉴 위치
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// 메뉴 구조 (flat, hierarchical)
    /// </summary>
    public string Structure { get; set; } = string.Empty;

    /// <summary>
    /// 반응형 여부
    /// </summary>
    public bool IsResponsive { get; set; }
}

/// <summary>
/// 네비게이션 항목
/// </summary>
public class NavigationItem
{
    /// <summary>
    /// 항목 텍스트
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 링크 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 하위 항목들
    /// </summary>
    public List<NavigationItem> Children { get; set; } = new();

    /// <summary>
    /// 항목 레벨 (0이 최상위)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 활성 상태 감지 가능 여부
    /// </summary>
    public bool HasActiveState { get; set; }

    /// <summary>
    /// 항목 중요도
    /// </summary>
    public double Importance { get; set; }
}

/// <summary>
/// 브레드크럼 패턴
/// </summary>
public class BreadcrumbPattern
{
    /// <summary>
    /// 패턴 이름
    /// </summary>
    public string PatternName { get; set; } = string.Empty;

    /// <summary>
    /// 패턴 예시들
    /// </summary>
    public List<BreadcrumbExample> Examples { get; set; } = new();

    /// <summary>
    /// 패턴 일관성 점수
    /// </summary>
    public double ConsistencyScore { get; set; }

    /// <summary>
    /// 패턴이 적용된 페이지 수
    /// </summary>
    public int PageCount { get; set; }
}

/// <summary>
/// 브레드크럼 예시
/// </summary>
public class BreadcrumbExample
{
    /// <summary>
    /// 페이지 URL
    /// </summary>
    public string PageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 브레드크럼 항목들
    /// </summary>
    public List<BreadcrumbItem> Items { get; set; } = new();
}

/// <summary>
/// 브레드크럼 항목
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// 항목 텍스트
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 링크 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 항목 순서
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 현재 페이지 여부
    /// </summary>
    public bool IsCurrentPage { get; set; }
}

/// <summary>
/// 콘텐츠 계층 구조 결과
/// </summary>
public class ContentHierarchyResult
{
    /// <summary>
    /// 루트 노드들
    /// </summary>
    public List<ContentNode> RootNodes { get; set; } = new();

    /// <summary>
    /// 계층 깊이
    /// </summary>
    public int MaxDepth { get; set; }

    /// <summary>
    /// 총 노드 수
    /// </summary>
    public int TotalNodes { get; set; }

    /// <summary>
    /// 고아 페이지 (계층에 속하지 않는 페이지)
    /// </summary>
    public List<string> OrphanPages { get; set; } = new();

    /// <summary>
    /// 계층 구조 품질 점수
    /// </summary>
    public double StructureQuality { get; set; }

    /// <summary>
    /// 균형도 점수 (트리 균형)
    /// </summary>
    public double BalanceScore { get; set; }
}

/// <summary>
/// 콘텐츠 노드
/// </summary>
public class ContentNode
{
    /// <summary>
    /// 페이지 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 페이지 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 노드 타입
    /// </summary>
    public NodeType NodeType { get; set; }

    /// <summary>
    /// 하위 노드들
    /// </summary>
    public List<ContentNode> Children { get; set; } = new();

    /// <summary>
    /// 부모 노드 URL
    /// </summary>
    public string? ParentUrl { get; set; }

    /// <summary>
    /// 노드 깊이
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// 노드 가중치
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// 하위 트리 크기
    /// </summary>
    public int SubtreeSize { get; set; }
}

/// <summary>
/// 관련 콘텐츠 결과
/// </summary>
public class RelatedContentResult
{
    /// <summary>
    /// 기준 페이지 URL
    /// </summary>
    public string BasePageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 관련 페이지들
    /// </summary>
    public List<RelatedPage> RelatedPages { get; set; } = new();

    /// <summary>
    /// 관련성 기준
    /// </summary>
    public List<RelatednessMetric> RelatednessMetrics { get; set; } = new();

    /// <summary>
    /// 추천 신뢰도 점수
    /// </summary>
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// 관련 페이지
/// </summary>
public class RelatedPage
{
    /// <summary>
    /// 페이지 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 페이지 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 관련성 점수 (0.0-1.0)
    /// </summary>
    public double RelatednessScore { get; set; }

    /// <summary>
    /// 관련성 이유
    /// </summary>
    public List<RelatednessReason> Reasons { get; set; } = new();

    /// <summary>
    /// 관련성 타입
    /// </summary>
    public RelatednessType Type { get; set; }
}

/// <summary>
/// 관련성 이유
/// </summary>
public class RelatednessReason
{
    /// <summary>
    /// 이유 타입
    /// </summary>
    public RelatednessReasonType Type { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 가중치
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// 증거
    /// </summary>
    public string Evidence { get; set; } = string.Empty;
}

/// <summary>
/// 관련성 메트릭
/// </summary>
public class RelatednessMetric
{
    /// <summary>
    /// 메트릭 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 메트릭 값
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 콘텐츠 클러스터 결과
/// </summary>
public class ContentClusterResult
{
    /// <summary>
    /// 클러스터들
    /// </summary>
    public List<ContentCluster> Clusters { get; set; } = new();

    /// <summary>
    /// 클러스터링 방법
    /// </summary>
    public string ClusteringMethod { get; set; } = string.Empty;

    /// <summary>
    /// 클러스터링 품질 점수
    /// </summary>
    public double QualityScore { get; set; }

    /// <summary>
    /// 실루엣 점수
    /// </summary>
    public double SilhouetteScore { get; set; }

    /// <summary>
    /// 클러스터 내 응집도
    /// </summary>
    public double IntraClusterCohesion { get; set; }

    /// <summary>
    /// 클러스터 간 분리도
    /// </summary>
    public double InterClusterSeparation { get; set; }
}

/// <summary>
/// 콘텐츠 클러스터
/// </summary>
public class ContentCluster
{
    /// <summary>
    /// 클러스터 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 클러스터 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 클러스터 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 클러스터에 속한 페이지들
    /// </summary>
    public List<string> PageUrls { get; set; } = new();

    /// <summary>
    /// 클러스터 중심점 (대표 페이지)
    /// </summary>
    public string CentroidUrl { get; set; } = string.Empty;

    /// <summary>
    /// 클러스터 크기
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// 클러스터 밀도
    /// </summary>
    public double Density { get; set; }

    /// <summary>
    /// 주요 키워드
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// 클러스터 카테고리
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// 사이트 토폴로지 메트릭
/// </summary>
public class SiteTopologyMetrics
{
    /// <summary>
    /// 총 페이지 수
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 총 링크 수
    /// </summary>
    public int TotalLinks { get; set; }

    /// <summary>
    /// 평균 페이지 깊이
    /// </summary>
    public double AverageDepth { get; set; }

    /// <summary>
    /// 최대 깊이
    /// </summary>
    public int MaxDepth { get; set; }

    /// <summary>
    /// 평균 발신 링크 수
    /// </summary>
    public double AverageOutboundLinks { get; set; }

    /// <summary>
    /// 평균 수신 링크 수
    /// </summary>
    public double AverageInboundLinks { get; set; }

    /// <summary>
    /// 허브 페이지 수 (많은 발신 링크)
    /// </summary>
    public int HubPages { get; set; }

    /// <summary>
    /// 권위 페이지 수 (많은 수신 링크)
    /// </summary>
    public int AuthorityPages { get; set; }

    /// <summary>
    /// 고아 페이지 수
    /// </summary>
    public int OrphanPages { get; set; }

    /// <summary>
    /// 데드 엔드 페이지 수 (발신 링크 없음)
    /// </summary>
    public int DeadEndPages { get; set; }

    /// <summary>
    /// 네트워크 밀도
    /// </summary>
    public double NetworkDensity { get; set; }

    /// <summary>
    /// 클러스터링 계수
    /// </summary>
    public double ClusteringCoefficient { get; set; }

    /// <summary>
    /// 평균 경로 길이
    /// </summary>
    public double AveragePathLength { get; set; }

    /// <summary>
    /// 직경 (최대 경로 길이)
    /// </summary>
    public int Diameter { get; set; }
}

/// <summary>
/// 페이지 타입
/// </summary>
public enum PageType
{
    Unknown,
    Homepage,
    CategoryPage,
    ProductPage,
    ArticlePage,
    AboutPage,
    ContactPage,
    SearchPage,
    ErrorPage,
    LandingPage,
    BlogPost,
    NewsArticle,
    DocumentationPage,
    SitemapPage,
    ArchivePage
}

/// <summary>
/// 링크 타입
/// </summary>
public enum LinkType
{
    Unknown,
    Internal,
    External,
    Anchor,
    Download,
    Email,
    Phone,
    Social
}

/// <summary>
/// 링크 위치
/// </summary>
public enum LinkPosition
{
    Unknown,
    Header,
    Navigation,
    Content,
    Sidebar,
    Footer,
    Breadcrumb,
    Menu
}

/// <summary>
/// 관계 유형
/// </summary>
public enum RelationshipType
{
    Unknown,
    Parent,
    Child,
    Sibling,
    Related,
    Reference,
    Navigation,
    Hierarchical,
    CrossReference,
    Temporal
}

/// <summary>
/// 네비게이션 타입
/// </summary>
public enum NavigationType
{
    Unknown,
    Primary,
    Secondary,
    Footer,
    Breadcrumb,
    Pagination,
    Tags,
    Categories
}

/// <summary>
/// 노드 타입
/// </summary>
public enum NodeType
{
    Unknown,
    Root,
    Branch,
    Leaf,
    Hub,
    Authority
}

/// <summary>
/// 관련성 타입
/// </summary>
public enum RelatednessType
{
    Unknown,
    Semantic,
    Structural,
    Temporal,
    Behavioral,
    Contextual
}

/// <summary>
/// 관련성 이유 타입
/// </summary>
public enum RelatednessReasonType
{
    Unknown,
    SharedKeywords,
    SimilarContent,
    StructuralProximity,
    DirectLink,
    CommonCategory,
    SimilarAudience,
    TemporalRelation,
    AuthorRelation
}