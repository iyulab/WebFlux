namespace WebFlux.Core.Models;

/// <summary>
/// llms.txt 파싱 결과
/// </summary>
public class LlmsParseResult
{
    /// <summary>
    /// 파싱 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// llms.txt 파일이 발견되었는지 여부
    /// </summary>
    public bool FileFound { get; set; }

    /// <summary>
    /// llms.txt URL
    /// </summary>
    public string LlmsUrl { get; set; } = string.Empty;

    /// <summary>
    /// 파싱된 메타데이터
    /// </summary>
    public LlmsMetadata? Metadata { get; set; }

    /// <summary>
    /// 파싱 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 원본 llms.txt 내용
    /// </summary>
    public string? RawContent { get; set; }

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// llms.txt 메타데이터
/// </summary>
public class LlmsMetadata
{
    /// <summary>
    /// 웹사이트 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 사이트 제목 또는 이름
    /// </summary>
    public string SiteName { get; set; } = string.Empty;

    /// <summary>
    /// 사이트 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// llms.txt 버전
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 주요 섹션들
    /// </summary>
    public List<LlmsSection> Sections { get; set; } = new();

    /// <summary>
    /// 중요한 페이지들
    /// </summary>
    public List<LlmsPage> ImportantPages { get; set; } = new();

    /// <summary>
    /// 연락처 정보
    /// </summary>
    public LlmsContact? Contact { get; set; }

    /// <summary>
    /// 크롤링 가이드라인
    /// </summary>
    public LlmsCrawlingGuidelines? CrawlingGuidelines { get; set; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();

    /// <summary>
    /// 콘텐츠 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 언어 정보
    /// </summary>
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }
}

/// <summary>
/// llms.txt 섹션 정보
/// </summary>
public class LlmsSection
{
    /// <summary>
    /// 섹션 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 섹션 경로 (상대 URL)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 섹션 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 중요도 (1-10, 높을수록 중요)
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 예상 페이지 수
    /// </summary>
    public int? EstimatedPageCount { get; set; }

    /// <summary>
    /// 콘텐츠 타입
    /// </summary>
    public string ContentType { get; set; } = "general";

    /// <summary>
    /// 하위 섹션들
    /// </summary>
    public List<LlmsSection> SubSections { get; set; } = new();

    /// <summary>
    /// 섹션 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// llms.txt 중요 페이지 정보
/// </summary>
public class LlmsPage
{
    /// <summary>
    /// 페이지 경로 (상대 URL)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 페이지 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 페이지 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 중요도 (1-10, 높을수록 중요)
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 페이지 타입 (예: tutorial, reference, guide)
    /// </summary>
    public string PageType { get; set; } = "content";

    /// <summary>
    /// 관련 페이지들
    /// </summary>
    public List<string> RelatedPages { get; set; } = new();

    /// <summary>
    /// 페이지 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 예상 콘텐츠 길이
    /// </summary>
    public int? EstimatedLength { get; set; }
}

/// <summary>
/// llms.txt 연락처 정보
/// </summary>
public class LlmsContact
{
    /// <summary>
    /// 이메일 주소
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 웹사이트 URL
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// 소셜 미디어 링크
    /// </summary>
    public Dictionary<string, string> SocialMedia { get; set; } = new();

    /// <summary>
    /// 기타 연락 방법
    /// </summary>
    public Dictionary<string, string> OtherContacts { get; set; } = new();
}

/// <summary>
/// llms.txt 크롤링 가이드라인
/// </summary>
public class LlmsCrawlingGuidelines
{
    /// <summary>
    /// 권장 크롤링 속도 (요청/초)
    /// </summary>
    public double? RecommendedRateLimit { get; set; }

    /// <summary>
    /// 최대 동시 연결 수
    /// </summary>
    public int? MaxConcurrentConnections { get; set; }

    /// <summary>
    /// 권장 User-Agent
    /// </summary>
    public string? PreferredUserAgent { get; set; }

    /// <summary>
    /// 제외할 패턴들
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// 포함할 패턴들
    /// </summary>
    public List<string> IncludePatterns { get; set; } = new();

    /// <summary>
    /// 권장 크롤링 시간대
    /// </summary>
    public List<string> PreferredCrawlingHours { get; set; } = new();

    /// <summary>
    /// 추가 제한사항
    /// </summary>
    public Dictionary<string, object> AdditionalRestrictions { get; set; } = new();
}

/// <summary>
/// llms.txt 파싱 통계
/// </summary>
public class LlmsParsingStatistics
{
    /// <summary>
    /// 총 파싱 시도 횟수
    /// </summary>
    public int TotalParseAttempts { get; set; }

    /// <summary>
    /// 성공한 파싱 횟수
    /// </summary>
    public int SuccessfulParses { get; set; }

    /// <summary>
    /// llms.txt 파일이 발견된 사이트 수
    /// </summary>
    public int SitesWithLlmsFile { get; set; }

    /// <summary>
    /// 평균 파싱 시간 (밀리초)
    /// </summary>
    public double AverageParseTime { get; set; }

    /// <summary>
    /// 가장 일반적인 오류들
    /// </summary>
    public Dictionary<string, int> CommonErrors { get; set; } = new();

    /// <summary>
    /// 지원되는 버전별 통계
    /// </summary>
    public Dictionary<string, int> VersionStatistics { get; set; } = new();

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

/* EnhancedCrawlResult class removed to avoid namespace conflicts
   Use CrawlResult from WebFlux.Core.Interfaces and add llms.txt metadata to its Metadata dictionary
*/