namespace WebFlux.Core.Models;

/// <summary>
/// HTML 메타데이터와 AI 메타데이터를 통합한 풍부한 메타데이터 모델
/// HTML 메타 태그 + AI 추출 + 사용자 검증을 모두 포함
/// </summary>
public class EnrichedMetadata
{
    // ===================================================================
    // 기본 메타데이터 (HTML 우선, AI로 보완)
    // ===================================================================

    /// <summary>페이지 제목 (HTML meta 우선, AI 생성으로 보완)</summary>
    public string? Title { get; set; }

    /// <summary>페이지 설명 (HTML meta 우선, AI 생성으로 보완)</summary>
    public string? Description { get; set; }

    /// <summary>작성자 (HTML meta 우선, AI 추출로 보완)</summary>
    public string? Author { get; set; }

    /// <summary>게시 날짜 (HTML meta 또는 AI 추출)</summary>
    public DateTimeOffset? PublishedDate { get; set; }

    /// <summary>수정 날짜 (HTML meta 또는 AI 추출)</summary>
    public DateTimeOffset? ModifiedDate { get; set; }

    /// <summary>언어 코드 (HTML lang 속성 우선, AI 감지로 보완, 예: "ko", "en")</summary>
    public string? Language { get; set; }

    // ===================================================================
    // AI 추출 메타데이터 (검색 및 RAG 최적화)
    // ===================================================================

    /// <summary>주제 목록 (AI 추출, 3-5개 권장, 예: ["React Hooks", "useState", "useEffect"])</summary>
    public IReadOnlyList<string> Topics { get; set; } = Array.Empty<string>();

    /// <summary>키워드 목록 (HTML meta + AI 추출 병합, 5-10개 권장)</summary>
    public IReadOnlyList<string> Keywords { get; set; } = Array.Empty<string>();

    /// <summary>콘텐츠 타입 (AI 분류, 예: "article", "documentation", "product", "tutorial")</summary>
    public string? ContentType { get; set; }

    /// <summary>사이트 구조 (AI 분석, 예: "blog", "documentation", "product", "news", "forum")</summary>
    public string? SiteStructure { get; set; }

    // ===================================================================
    // 웹 소스 메타데이터
    // ===================================================================

    /// <summary>소스 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>도메인 (예: "react.dev", "github.com")</summary>
    public string Domain { get; set; } = string.Empty;

    // ===================================================================
    // 스키마별 메타데이터 (유연한 확장성)
    // ===================================================================

    /// <summary>
    /// 스키마별 전용 메타데이터 (Dictionary로 유연성 확보)
    /// 예시:
    /// - TechnicalDoc: ["libraries": ["react@18.2.0"], "frameworks": ["React"], "technologies": ["JavaScript"]]
    /// - ProductManual: ["productName": "iPhone 15 Pro", "company": "Apple", "price": 999.00]
    /// - Article: ["readingTimeMinutes": 8, "tags": ["javascript", "patterns"]]
    /// </summary>
    public Dictionary<string, object> SchemaSpecificData { get; set; } = new();

    // ===================================================================
    // 메타데이터 소스 추적 (투명성)
    // ===================================================================

    /// <summary>메타데이터 전체 소스 (Html, AI, Merged, User)</summary>
    public MetadataSource Source { get; set; } = MetadataSource.Merged;

    /// <summary>
    /// 필드별 소스 추적
    /// 예: {"title": MetadataSource.Html, "topics": MetadataSource.AI, "keywords": MetadataSource.Merged}
    /// </summary>
    public Dictionary<string, MetadataSource> FieldSources { get; set; } = new();

    // ===================================================================
    // 신뢰도 및 품질 메트릭
    // ===================================================================

    /// <summary>전체 신뢰도 점수 (0.0 - 1.0, AI 추출의 신뢰도)</summary>
    public float OverallConfidence { get; set; }

    /// <summary>
    /// 필드별 신뢰도 점수
    /// 예: {"topics": 0.96, "libraries": 0.95, "contentType": 0.92}
    /// </summary>
    public Dictionary<string, float> FieldConfidence { get; set; } = new();

    // ===================================================================
    // 사용자 검증 및 수정
    // ===================================================================

    /// <summary>사용자가 메타데이터를 검증했는지 여부</summary>
    public bool UserVerified { get; set; } = false;

    /// <summary>
    /// 사용자 수정 사항
    /// 예: {"topics": ["React", "Hooks"], "contentType": "reference"}
    /// </summary>
    public Dictionary<string, object> UserCorrections { get; set; } = new();

    // ===================================================================
    // HTML 메타데이터 원본 보관 (참조용)
    // ===================================================================

    /// <summary>HTML 메타데이터 원본 스냅샷 (디버깅 및 추적용)</summary>
    public HtmlMetadataSnapshot? HtmlMetadata { get; set; }

    // ===================================================================
    // 타임스탬프
    // ===================================================================

    /// <summary>메타데이터 추출 시간</summary>
    public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 메타데이터 소스 열거형
/// </summary>
public enum MetadataSource
{
    /// <summary>HTML 메타 태그에서 추출 (meta, og:*, twitter:*)</summary>
    Html,

    /// <summary>AI로 추출 (LLM 텍스트 분석)</summary>
    AI,

    /// <summary>HTML과 AI 메타데이터 병합</summary>
    Merged,

    /// <summary>사용자가 검증하거나 수정한 데이터</summary>
    User
}

/// <summary>
/// HTML 메타데이터 스냅샷 (원본 보관용)
/// </summary>
public class HtmlMetadataSnapshot
{
    /// <summary>표준 HTML 메타 태그 (name, content 쌍)</summary>
    public Dictionary<string, string> MetaTags { get; set; } = new();

    /// <summary>OpenGraph 메타데이터 (og:* 태그)</summary>
    public OpenGraphData? OpenGraph { get; set; }

    /// <summary>Twitter Card 메타데이터 (twitter:* 태그)</summary>
    public TwitterCardData? TwitterCard { get; set; }

    /// <summary>구조화된 데이터 (JSON-LD, Microdata)</summary>
    public Dictionary<string, object> StructuredData { get; set; } = new();

    /// <summary>HTML 추출 시간</summary>
    public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;
}
