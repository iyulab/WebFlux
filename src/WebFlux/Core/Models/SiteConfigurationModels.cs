using YamlDotNet.Serialization;

namespace WebFlux.Core.Models;

/// <summary>
/// 웹사이트 설정 정보
/// Jekyll, Hugo, Next.js 등의 정적 사이트 생성기 설정 분석
/// </summary>
public class SiteConfiguration
{
    /// <summary>원본 URL</summary>
    public string SourceUrl { get; init; } = string.Empty;

    /// <summary>설정 파일 타입</summary>
    public SiteConfigurationType ConfigType { get; init; }

    /// <summary>사이트 기본 정보</summary>
    public SiteConfigurationInfo SiteInfo { get; init; } = new();

    /// <summary>빌드 설정</summary>
    public BuildConfiguration BuildConfig { get; init; } = new();

    /// <summary>콘텐츠 설정</summary>
    public ContentConfiguration ContentConfig { get; init; } = new();

    /// <summary>플러그인 설정</summary>
    public PluginConfiguration PluginConfig { get; init; } = new();

    /// <summary>배포 설정</summary>
    public DeploymentConfiguration DeploymentConfig { get; init; } = new();

    /// <summary>SEO 설정</summary>
    public SeoConfiguration SeoConfig { get; init; } = new();

    /// <summary>성능 설정</summary>
    public SitePerformanceConfiguration PerformanceConfig { get; init; } = new();

    /// <summary>원본 YAML 내용</summary>
    public string RawYaml { get; init; } = string.Empty;

    /// <summary>파싱된 YAML 객체</summary>
    public Dictionary<string, object> ParsedYaml { get; init; } = new();

    /// <summary>설정 품질 점수 (0.0 - 1.0)</summary>
    public double QualityScore { get; init; }

    /// <summary>발견된 문제점들</summary>
    public IReadOnlyList<ConfigurationIssue> Issues { get; init; } = Array.Empty<ConfigurationIssue>();
}

/// <summary>
/// 사이트 설정 타입
/// </summary>
public enum SiteConfigurationType
{
    /// <summary>알 수 없음</summary>
    Unknown,
    /// <summary>Jekyll (_config.yml)</summary>
    Jekyll,
    /// <summary>Hugo (config.yaml/hugo.yaml)</summary>
    Hugo,
    /// <summary>Next.js (next.config.js의 YAML 섹션)</summary>
    NextJs,
    /// <summary>Gatsby (gatsby-config.js의 YAML 섹션)</summary>
    Gatsby,
    /// <summary>VuePress (config.js의 YAML 섹션)</summary>
    VuePress,
    /// <summary>Docusaurus (docusaurus.config.js의 YAML 섹션)</summary>
    Docusaurus,
    /// <summary>GitBook (.gitbook.yaml)</summary>
    GitBook,
    /// <summary>일반 YAML 설정</summary>
    Generic
}

/// <summary>
/// 사이트 기본 정보
/// </summary>
public class SiteConfigurationInfo
{
    /// <summary>사이트 제목</summary>
    [YamlMember(Alias = "title")]
    public string? Title { get; init; }

    /// <summary>사이트 설명</summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; init; }

    /// <summary>사이트 URL</summary>
    [YamlMember(Alias = "url")]
    public string? Url { get; init; }

    /// <summary>베이스 URL</summary>
    [YamlMember(Alias = "baseurl")]
    public string? BaseUrl { get; init; }

    /// <summary>작성자 정보</summary>
    [YamlMember(Alias = "author")]
    public AuthorInfo? Author { get; init; }

    /// <summary>언어 설정</summary>
    [YamlMember(Alias = "lang")]
    public string? Language { get; init; }

    /// <summary>타임존</summary>
    [YamlMember(Alias = "timezone")]
    public string? Timezone { get; init; }

    /// <summary>인코딩</summary>
    [YamlMember(Alias = "encoding")]
    public string? Encoding { get; init; }
}

/// <summary>
/// 작성자 정보
/// </summary>
public class AuthorInfo
{
    /// <summary>이름</summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; init; }

    /// <summary>이메일</summary>
    [YamlMember(Alias = "email")]
    public string? Email { get; init; }

    /// <summary>웹사이트</summary>
    [YamlMember(Alias = "url")]
    public string? Url { get; init; }

    /// <summary>소셜 미디어</summary>
    [YamlMember(Alias = "social")]
    public Dictionary<string, string> Social { get; init; } = new();
}

/// <summary>
/// 빌드 설정
/// </summary>
public class BuildConfiguration
{
    /// <summary>소스 디렉토리</summary>
    [YamlMember(Alias = "source")]
    public string? SourceDirectory { get; init; }

    /// <summary>출력 디렉토리</summary>
    [YamlMember(Alias = "destination")]
    public string? OutputDirectory { get; init; }

    /// <summary>제외할 파일/폴더</summary>
    [YamlMember(Alias = "exclude")]
    public IReadOnlyList<string> ExcludePatterns { get; init; } = Array.Empty<string>();

    /// <summary>포함할 파일/폴더</summary>
    [YamlMember(Alias = "include")]
    public IReadOnlyList<string> IncludePatterns { get; init; } = Array.Empty<string>();

    /// <summary>빌드 환경</summary>
    [YamlMember(Alias = "environment")]
    public string? Environment { get; init; }

    /// <summary>증분 빌드 사용</summary>
    [YamlMember(Alias = "incremental")]
    public bool IncrementalBuild { get; init; }

    /// <summary>드래프트 포함</summary>
    [YamlMember(Alias = "show_drafts")]
    public bool ShowDrafts { get; init; }

    /// <summary>미래 날짜 포함</summary>
    [YamlMember(Alias = "future")]
    public bool ShowFuture { get; init; }
}

/// <summary>
/// 콘텐츠 설정
/// </summary>
public class ContentConfiguration
{
    /// <summary>마크다운 엔진</summary>
    [YamlMember(Alias = "markdown")]
    public string? MarkdownEngine { get; init; }

    /// <summary>하이라이터</summary>
    [YamlMember(Alias = "highlighter")]
    public string? Highlighter { get; init; }

    /// <summary>퍼머링크 형식</summary>
    [YamlMember(Alias = "permalink")]
    public string? PermalinkFormat { get; init; }

    /// <summary>페이지네이션 설정</summary>
    [YamlMember(Alias = "paginate")]
    public int? PaginateCount { get; init; }

    /// <summary>페이지네이션 경로</summary>
    [YamlMember(Alias = "paginate_path")]
    public string? PaginatePath { get; init; }

    /// <summary>발췌 구분자</summary>
    [YamlMember(Alias = "excerpt_separator")]
    public string? ExcerptSeparator { get; init; }

    /// <summary>컬렉션 설정</summary>
    [YamlMember(Alias = "collections")]
    public Dictionary<string, CollectionConfig> Collections { get; init; } = new();

    /// <summary>기본값 설정</summary>
    [YamlMember(Alias = "defaults")]
    public IReadOnlyList<DefaultConfig> Defaults { get; init; } = Array.Empty<DefaultConfig>();
}

/// <summary>
/// 컬렉션 설정
/// </summary>
public class CollectionConfig
{
    /// <summary>출력 여부</summary>
    [YamlMember(Alias = "output")]
    public bool Output { get; init; }

    /// <summary>퍼머링크</summary>
    [YamlMember(Alias = "permalink")]
    public string? Permalink { get; init; }
}

/// <summary>
/// 기본값 설정
/// </summary>
public class DefaultConfig
{
    /// <summary>스코프</summary>
    [YamlMember(Alias = "scope")]
    public DefaultScope? Scope { get; init; }

    /// <summary>값</summary>
    [YamlMember(Alias = "values")]
    public Dictionary<string, object> Values { get; init; } = new();
}

/// <summary>
/// 기본값 스코프
/// </summary>
public class DefaultScope
{
    /// <summary>경로</summary>
    [YamlMember(Alias = "path")]
    public string? Path { get; init; }

    /// <summary>타입</summary>
    [YamlMember(Alias = "type")]
    public string? Type { get; init; }
}

/// <summary>
/// 플러그인 설정
/// </summary>
public class PluginConfiguration
{
    /// <summary>플러그인 목록</summary>
    [YamlMember(Alias = "plugins")]
    public IReadOnlyList<string> Plugins { get; init; } = Array.Empty<string>();

    /// <summary>gem 설정</summary>
    [YamlMember(Alias = "gems")]
    public IReadOnlyList<string> Gems { get; init; } = Array.Empty<string>();

    /// <summary>플러그인 설정</summary>
    public Dictionary<string, object> PluginSettings { get; init; } = new();
}

/// <summary>
/// 배포 설정
/// </summary>
public class DeploymentConfiguration
{
    /// <summary>배포 대상</summary>
    public string? Target { get; init; }

    /// <summary>GitHub Pages 설정</summary>
    public GitHubPagesConfig? GitHubPages { get; init; }

    /// <summary>Netlify 설정</summary>
    public NetlifyConfig? Netlify { get; init; }

    /// <summary>Vercel 설정</summary>
    public VercelConfig? Vercel { get; init; }
}

/// <summary>
/// GitHub Pages 설정
/// </summary>
public class GitHubPagesConfig
{
    /// <summary>브랜치</summary>
    public string? Branch { get; init; }

    /// <summary>폴더</summary>
    public string? Folder { get; init; }

    /// <summary>커스텀 도메인</summary>
    public string? CustomDomain { get; init; }
}

/// <summary>
/// Netlify 설정
/// </summary>
public class NetlifyConfig
{
    /// <summary>빌드 명령어</summary>
    public string? BuildCommand { get; init; }

    /// <summary>발행 디렉토리</summary>
    public string? PublishDirectory { get; init; }

    /// <summary>환경 변수</summary>
    public Dictionary<string, string> Environment { get; init; } = new();
}

/// <summary>
/// Vercel 설정
/// </summary>
public class VercelConfig
{
    /// <summary>빌드 명령어</summary>
    public string? BuildCommand { get; init; }

    /// <summary>출력 디렉토리</summary>
    public string? OutputDirectory { get; init; }

    /// <summary>함수</summary>
    public Dictionary<string, object> Functions { get; init; } = new();
}

/// <summary>
/// SEO 설정
/// </summary>
public class SeoConfiguration
{
    /// <summary>Google Analytics</summary>
    [YamlMember(Alias = "google_analytics")]
    public string? GoogleAnalytics { get; init; }

    /// <summary>Google Tag Manager</summary>
    [YamlMember(Alias = "google_tag_manager")]
    public string? GoogleTagManager { get; init; }

    /// <summary>사이트맵 설정</summary>
    [YamlMember(Alias = "sitemap")]
    public bool GenerateSitemap { get; init; }

    /// <summary>robots.txt</summary>
    [YamlMember(Alias = "robots")]
    public string? RobotsConfig { get; init; }

    /// <summary>소셜 미디어 설정</summary>
    [YamlMember(Alias = "social")]
    public SocialMediaConfig? SocialMedia { get; init; }
}

/// <summary>
/// 소셜 미디어 설정
/// </summary>
public class SocialMediaConfig
{
    /// <summary>Twitter</summary>
    [YamlMember(Alias = "twitter")]
    public string? Twitter { get; init; }

    /// <summary>Facebook</summary>
    [YamlMember(Alias = "facebook")]
    public string? Facebook { get; init; }

    /// <summary>LinkedIn</summary>
    [YamlMember(Alias = "linkedin")]
    public string? LinkedIn { get; init; }

    /// <summary>GitHub</summary>
    [YamlMember(Alias = "github")]
    public string? GitHub { get; init; }
}

/// <summary>
/// 성능 설정
/// </summary>
public class SitePerformanceConfiguration
{
    /// <summary>압축 사용</summary>
    [YamlMember(Alias = "compress_html")]
    public bool CompressHtml { get; init; }

    /// <summary>CSS 최소화</summary>
    [YamlMember(Alias = "sass")]
    public SassConfig? Sass { get; init; }

    /// <summary>캐시 설정</summary>
    public CacheConfig? Cache { get; init; }

    /// <summary>CDN 설정</summary>
    public CdnConfig? Cdn { get; init; }
}

/// <summary>
/// Sass 설정
/// </summary>
public class SassConfig
{
    /// <summary>스타일</summary>
    [YamlMember(Alias = "style")]
    public string? Style { get; init; }

    /// <summary>디렉토리</summary>
    [YamlMember(Alias = "sass_dir")]
    public string? Directory { get; init; }
}

/// <summary>
/// 캐시 설정
/// </summary>
public class CacheConfig
{
    /// <summary>TTL (초)</summary>
    public int TtlSeconds { get; init; }

    /// <summary>캐시 타입</summary>
    public string? Type { get; init; }
}

/// <summary>
/// CDN 설정
/// </summary>
public class CdnConfig
{
    /// <summary>CDN URL</summary>
    public string? Url { get; init; }

    /// <summary>에셋 URL</summary>
    public string? AssetsUrl { get; init; }
}

/// <summary>
/// 설정 문제점
/// </summary>
public class ConfigurationIssue
{
    /// <summary>문제 타입</summary>
    public ConfigurationIssueType Type { get; init; }

    /// <summary>심각도</summary>
    public ConfigurationIssueSeverity Severity { get; init; }

    /// <summary>메시지</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>영향을 받는 키</summary>
    public string? AffectedKey { get; init; }

    /// <summary>권장 해결책</summary>
    public string? Recommendation { get; init; }
}

/// <summary>
/// 설정 문제 타입
/// </summary>
public enum ConfigurationIssueType
{
    /// <summary>필수 설정 누락</summary>
    MissingRequired,
    /// <summary>잘못된 값</summary>
    InvalidValue,
    /// <summary>권장하지 않는 설정</summary>
    Deprecated,
    /// <summary>보안 문제</summary>
    Security,
    /// <summary>성능 문제</summary>
    Performance,
    /// <summary>SEO 문제</summary>
    Seo,
    /// <summary>호환성 문제</summary>
    Compatibility
}

/// <summary>
/// 설정 문제 심각도
/// </summary>
public enum ConfigurationIssueSeverity
{
    /// <summary>정보</summary>
    Info,
    /// <summary>경고</summary>
    Warning,
    /// <summary>오류</summary>
    Error,
    /// <summary>치명적</summary>
    Critical
}

/// <summary>
/// 설정 품질 평가
/// </summary>
public class ConfigurationQualityAssessment
{
    /// <summary>전체 품질 점수 (0.0 - 1.0)</summary>
    public double OverallScore { get; init; }

    /// <summary>완성도 점수</summary>
    public double CompletenessScore { get; init; }

    /// <summary>보안 점수</summary>
    public double SecurityScore { get; init; }

    /// <summary>성능 점수</summary>
    public double PerformanceScore { get; init; }

    /// <summary>SEO 친화성 점수</summary>
    public double SeoScore { get; init; }

    /// <summary>모범 사례 준수 점수</summary>
    public double BestPracticesScore { get; init; }

    /// <summary>발견된 문제점 수</summary>
    public ConfigurationIssueSummary IssueSummary { get; init; } = new();
}

/// <summary>
/// 설정 문제점 요약
/// </summary>
public class ConfigurationIssueSummary
{
    /// <summary>총 문제점 수</summary>
    public int TotalIssues { get; init; }

    /// <summary>치명적 문제 수</summary>
    public int CriticalIssues { get; init; }

    /// <summary>오류 수</summary>
    public int ErrorIssues { get; init; }

    /// <summary>경고 수</summary>
    public int WarningIssues { get; init; }

    /// <summary>정보 수</summary>
    public int InfoIssues { get; init; }
}