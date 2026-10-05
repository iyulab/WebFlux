namespace WebFlux.Core.Models;

/// <summary>
/// 브레드크럼 항목
/// </summary>
/// <summary>
/// 브레드크럼 예시
/// </summary>
/// <summary>
/// 브레드크럼 패턴
/// </summary>
/// <summary>
/// 네비게이션 항목
/// </summary>
/// <summary>
/// 네비게이션 메뉴
/// </summary>
/// <summary>
/// 네비게이션 구조 분석 결과
/// </summary>
/// <summary>
/// 페이지 간 링크 관계
/// </summary>
/// <summary>
/// 수신 링크 정보
/// </summary>
/// <summary>
/// 발신 링크 정보
/// </summary>
/// <summary>
/// 페이지 관계 정보
/// </summary>
/// <summary>
/// 콘텐츠 관계 분석 결과
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
/// 링크 타입
/// </summary>
/// <summary>
/// 페이지 타입
/// </summary>
/// <summary>
/// 사이트 토폴로지 메트릭
/// </summary>
/// <summary>
/// 콘텐츠 클러스터
/// </summary>
/// <summary>
/// 콘텐츠 클러스터 결과
/// </summary>
/// <summary>
/// 관련성 메트릭
/// </summary>
/// <summary>
/// 관련성 이유
/// </summary>
/// <summary>
/// 관련 페이지
/// </summary>
/// <summary>
/// 관련 콘텐츠 결과
/// </summary>
/// <summary>
/// 콘텐츠 노드
/// </summary>
/// <summary>
/// 콘텐츠 계층 구조 결과
/// </summary>
/// <summary>
/// 브레드크럼 항목
/// </summary>
/// <summary>
/// 브레드크럼 예시
/// </summary>
/// <summary>
/// 브레드크럼 패턴
/// </summary>
/// <summary>
/// 네비게이션 항목
/// </summary>
/// <summary>
/// 네비게이션 메뉴
/// </summary>
/// <summary>
/// 네비게이션 구조 분석 결과
/// </summary>
/// <summary>
/// 페이지 간 링크 관계
/// </summary>
/// <summary>
/// 수신 링크 정보
/// </summary>
/// <summary>
/// 발신 링크 정보
/// </summary>
/// <summary>
/// 페이지 관계 정보
/// </summary>
/// <summary>
/// 콘텐츠 관계 분석 결과
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
/// 관계 유형
/// </summary>
/// <summary>
/// 링크 위치
/// </summary>
/// <summary>
/// 링크 타입
/// </summary>
/// <summary>
/// 페이지 타입
/// </summary>
/// <summary>
/// 사이트 토폴로지 메트릭
/// </summary>
/// <summary>
/// 콘텐츠 클러스터
/// </summary>
/// <summary>
/// 콘텐츠 클러스터 결과
/// </summary>
/// <summary>
/// 관련성 메트릭
/// </summary>
/// <summary>
/// 관련성 이유
/// </summary>
/// <summary>
/// 관련 페이지
/// </summary>
/// <summary>
/// 관련 콘텐츠 결과
/// </summary>
/// <summary>
/// 콘텐츠 노드
/// </summary>
/// <summary>
/// 콘텐츠 계층 구조 결과
/// </summary>
/// <summary>
/// 브레드크럼 항목
/// </summary>
/// <summary>
/// 브레드크럼 예시
/// </summary>
/// <summary>
/// 브레드크럼 패턴
/// </summary>
/// <summary>
/// 네비게이션 항목
/// </summary>
/// <summary>
/// 네비게이션 메뉴
/// </summary>
/// <summary>
/// 네비게이션 구조 분석 결과
/// </summary>
/// <summary>
/// 페이지 간 링크 관계
/// </summary>
/// <summary>
/// 수신 링크 정보
/// </summary>
/// <summary>
/// 발신 링크 정보
/// </summary>
/// <summary>
/// 페이지 관계 정보
/// </summary>
/// <summary>
/// 콘텐츠 관계 분석 결과
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