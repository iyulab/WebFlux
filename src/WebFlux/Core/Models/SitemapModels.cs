namespace WebFlux.Core.Models;

/// <summary>
/// 사이트맵 분석 결과
/// </summary>
public class SitemapAnalysisResult
{
    /// <summary>
    /// 분석 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 사이트맵이 발견되었는지 여부
    /// </summary>
    public bool SitemapFound { get; set; }

    /// <summary>
    /// 발견된 사이트맵 URL 목록
    /// </summary>
    public List<string> DiscoveredSitemaps { get; set; } = new();

    /// <summary>
    /// 통합 사이트맵 메타데이터
    /// </summary>
    public SitemapMetadata? Metadata { get; set; }

    /// <summary>
    /// 분석 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 분석 시간
    /// </summary>
    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 분석에 소요된 시간 (밀리초)
    /// </summary>
    public long AnalysisTimeMs { get; set; }

    /// <summary>
    /// URL 패턴 분석 결과
    /// </summary>
    public UrlPatternAnalysis? UrlPatterns { get; set; }
}

/// <summary>
/// 사이트맵 메타데이터
/// </summary>
public class SitemapMetadata
{
    /// <summary>
    /// 웹사이트 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 사이트맵 URL 목록
    /// </summary>
    public List<string> SitemapUrls { get; set; } = new();

    /// <summary>
    /// 사이트맵 형식
    /// </summary>
    public SitemapFormat Format { get; set; }

    /// <summary>
    /// 총 URL 개수
    /// </summary>
    public int TotalUrls { get; set; }

    /// <summary>
    /// URL 엔트리 목록
    /// </summary>
    public List<SitemapUrlEntry> UrlEntries { get; set; } = new();

    /// <summary>
    /// 이미지 URL 목록
    /// </summary>
    public List<SitemapImageEntry> ImageEntries { get; set; } = new();

    /// <summary>
    /// 비디오 URL 목록
    /// </summary>
    public List<SitemapVideoEntry> VideoEntries { get; set; } = new();

    /// <summary>
    /// 뉴스 URL 목록
    /// </summary>
    public List<SitemapNewsEntry> NewsEntries { get; set; } = new();

    /// <summary>
    /// 네임스페이스 정보
    /// </summary>
    public Dictionary<string, string> Namespaces { get; set; } = new();

    /// <summary>
    /// 마지막 수정 시간
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 사이트맵 URL 엔트리
/// </summary>
public class SitemapUrlEntry
{
    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 마지막 수정 시간
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// 변경 빈도
    /// </summary>
    public ChangeFrequency? ChangeFrequency { get; set; }

    /// <summary>
    /// 우선순위 (0.0 - 1.0)
    /// </summary>
    public double? Priority { get; set; }

    /// <summary>
    /// 이미지 목록
    /// </summary>
    public List<SitemapImageEntry> Images { get; set; } = new();

    /// <summary>
    /// 비디오 목록
    /// </summary>
    public List<SitemapVideoEntry> Videos { get; set; } = new();

    /// <summary>
    /// 뉴스 정보
    /// </summary>
    public SitemapNewsEntry? News { get; set; }

    /// <summary>
    /// 추가 속성
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}

/// <summary>
/// 사이트맵 이미지 엔트리
/// </summary>
public class SitemapImageEntry
{
    /// <summary>
    /// 이미지 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 이미지 제목
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 이미지 캡션
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// 지리적 위치
    /// </summary>
    public string? GeoLocation { get; set; }

    /// <summary>
    /// 라이센스 URL
    /// </summary>
    public string? LicenseUrl { get; set; }
}

/// <summary>
/// 사이트맵 비디오 엔트리
/// </summary>
public class SitemapVideoEntry
{
    /// <summary>
    /// 썸네일 URL
    /// </summary>
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 콘텐츠 URL
    /// </summary>
    public string? ContentUrl { get; set; }

    /// <summary>
    /// 플레이어 URL
    /// </summary>
    public string? PlayerUrl { get; set; }

    /// <summary>
    /// 재생 시간 (초)
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 만료 날짜
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// 평점
    /// </summary>
    public double? Rating { get; set; }

    /// <summary>
    /// 조회수
    /// </summary>
    public int? ViewCount { get; set; }

    /// <summary>
    /// 게시 날짜
    /// </summary>
    public DateTimeOffset? PublicationDate { get; set; }

    /// <summary>
    /// 가족 친화적 여부
    /// </summary>
    public bool? FamilyFriendly { get; set; }

    /// <summary>
    /// 제한된 국가 목록
    /// </summary>
    public List<string> Restrictions { get; set; } = new();

    /// <summary>
    /// 갤러리 링크
    /// </summary>
    public string? GalleryUrl { get; set; }

    /// <summary>
    /// 태그 목록
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 카테고리
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// 사이트맵 뉴스 엔트리
/// </summary>
public class SitemapNewsEntry
{
    /// <summary>
    /// 게시자명
    /// </summary>
    public string Publication { get; set; } = string.Empty;

    /// <summary>
    /// 언어 코드
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 게시 날짜
    /// </summary>
    public DateTimeOffset PublicationDate { get; set; }

    /// <summary>
    /// 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 키워드
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// 스톡 티커 목록
    /// </summary>
    public List<string> StockTickers { get; set; } = new();
}

/// <summary>
/// URL 패턴 분석 결과
/// </summary>
public class UrlPatternAnalysis
{
    /// <summary>
    /// 감지된 URL 패턴 목록
    /// </summary>
    public List<UrlPattern> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// 카테고리별 URL 분포
    /// </summary>
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();

    /// <summary>
    /// 깊이별 URL 분포
    /// </summary>
    public Dictionary<int, int> DepthDistribution { get; set; } = new();

    /// <summary>
    /// 콘텐츠 타입별 분포
    /// </summary>
    public Dictionary<string, int> ContentTypeDistribution { get; set; } = new();

    /// <summary>
    /// 언어별 분포
    /// </summary>
    public Dictionary<string, int> LanguageDistribution { get; set; } = new();

    /// <summary>
    /// 가장 일반적인 URL 구조
    /// </summary>
    public List<string> CommonStructures { get; set; } = new();

    /// <summary>
    /// 추정되는 사이트 아키텍처 타입
    /// </summary>
    public SiteArchitectureType ArchitectureType { get; set; }
}

/// <summary>
/// URL 패턴
/// </summary>
public class UrlPattern
{
    /// <summary>
    /// 패턴 문자열
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// 매칭되는 URL 개수
    /// </summary>
    public int MatchCount { get; set; }

    /// <summary>
    /// 패턴 타입
    /// </summary>
    public PatternType Type { get; set; }

    /// <summary>
    /// 예시 URL 목록
    /// </summary>
    public List<string> ExampleUrls { get; set; } = new();

    /// <summary>
    /// 패턴 중요도 (1-10)
    /// </summary>
    public int Importance { get; set; } = 5;
}

/// <summary>
/// 사이트맵 형식
/// </summary>
public enum SitemapFormat
{
    /// <summary>
    /// XML 형식
    /// </summary>
    Xml,

    /// <summary>
    /// 텍스트 형식
    /// </summary>
    Text,

    /// <summary>
    /// 사이트맵 인덱스
    /// </summary>
    SitemapIndex,

    /// <summary>
    /// RSS 형식
    /// </summary>
    Rss,

    /// <summary>
    /// Atom 형식
    /// </summary>
    Atom
}

/// <summary>
/// 변경 빈도
/// </summary>
public enum ChangeFrequency
{
    /// <summary>
    /// 항상
    /// </summary>
    Always,

    /// <summary>
    /// 시간마다
    /// </summary>
    Hourly,

    /// <summary>
    /// 일일
    /// </summary>
    Daily,

    /// <summary>
    /// 주간
    /// </summary>
    Weekly,

    /// <summary>
    /// 월간
    /// </summary>
    Monthly,

    /// <summary>
    /// 연간
    /// </summary>
    Yearly,

    /// <summary>
    /// 절대 변경되지 않음
    /// </summary>
    Never
}

/// <summary>
/// 패턴 타입
/// </summary>
public enum PatternType
{
    /// <summary>
    /// 카테고리 기반 패턴
    /// </summary>
    Category,

    /// <summary>
    /// 날짜 기반 패턴
    /// </summary>
    Date,

    /// <summary>
    /// ID 기반 패턴
    /// </summary>
    Id,

    /// <summary>
    /// 슬러그 기반 패턴
    /// </summary>
    Slug,

    /// <summary>
    /// 언어 기반 패턴
    /// </summary>
    Language,

    /// <summary>
    /// 기타 패턴
    /// </summary>
    Other
}

/// <summary>
/// 사이트 아키텍처 타입
/// </summary>
public enum SiteArchitectureType
{
    /// <summary>
    /// 플랫 구조
    /// </summary>
    Flat,

    /// <summary>
    /// 계층적 구조
    /// </summary>
    Hierarchical,

    /// <summary>
    /// 카테고리 기반 구조
    /// </summary>
    CategoryBased,

    /// <summary>
    /// 태그 기반 구조
    /// </summary>
    TagBased,

    /// <summary>
    /// 날짜 기반 구조
    /// </summary>
    DateBased,

    /// <summary>
    /// 혼합 구조
    /// </summary>
    Hybrid,

    /// <summary>
    /// 알 수 없는 구조
    /// </summary>
    Unknown
}

/// <summary>
/// 사이트맵 분석 통계
/// </summary>
public class SitemapAnalysisStatistics
{
    /// <summary>
    /// 총 분석 시도 횟수
    /// </summary>
    public int TotalAnalysisAttempts { get; set; }

    /// <summary>
    /// 성공한 분석 횟수
    /// </summary>
    public int SuccessfulAnalyses { get; set; }

    /// <summary>
    /// 사이트맵이 발견된 사이트 수
    /// </summary>
    public int SitesWithSitemap { get; set; }

    /// <summary>
    /// 평균 분석 시간 (밀리초)
    /// </summary>
    public double AverageAnalysisTime { get; set; }

    /// <summary>
    /// 형식별 사이트맵 통계
    /// </summary>
    public Dictionary<SitemapFormat, int> FormatStatistics { get; set; } = new();

    /// <summary>
    /// 평균 사이트맵당 URL 개수
    /// </summary>
    public double AverageUrlsPerSitemap { get; set; }

    /// <summary>
    /// 가장 일반적인 오류들
    /// </summary>
    public Dictionary<string, int> CommonErrors { get; set; } = new();

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}