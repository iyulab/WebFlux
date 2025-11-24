using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 사이트 설정 분석 인터페이스
/// Jekyll, Hugo, Next.js 등의 정적 사이트 생성기 설정 분석
/// 90% 설정 품질 목표 달성을 위한 분석 기능 제공
/// </summary>
public interface ISiteConfigurationAnalyzer
{
    /// <summary>
    /// YAML 설정 파일을 분석합니다
    /// </summary>
    /// <param name="yamlContent">YAML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사이트 설정 정보</returns>
    Task<SiteConfiguration> AnalyzeConfigurationAsync(
        string yamlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 설정 파일 타입을 감지합니다
    /// </summary>
    /// <param name="yamlContent">YAML 콘텐츠</param>
    /// <param name="fileName">파일명 (선택)</param>
    /// <returns>설정 타입</returns>
    SiteConfigurationType DetectConfigurationType(string yamlContent, string? fileName = null);

    /// <summary>
    /// 설정 품질을 평가합니다
    /// </summary>
    /// <param name="configuration">사이트 설정</param>
    /// <returns>품질 평가 결과</returns>
    ConfigurationQualityAssessment AssessQuality(SiteConfiguration configuration);

    /// <summary>
    /// 설정을 검증하고 문제점을 찾습니다
    /// </summary>
    /// <param name="configuration">사이트 설정</param>
    /// <returns>발견된 문제점들</returns>
    IReadOnlyList<ConfigurationIssue> ValidateConfiguration(SiteConfiguration configuration);

    /// <summary>
    /// 설정 최적화 권장사항을 제공합니다
    /// </summary>
    /// <param name="configuration">사이트 설정</param>
    /// <returns>최적화 권장사항</returns>
    IReadOnlyList<ConfigurationRecommendation> GetOptimizationRecommendations(SiteConfiguration configuration);

    /// <summary>
    /// YAML을 특정 사이트 생성기 형식으로 변환합니다
    /// </summary>
    /// <param name="configuration">사이트 설정</param>
    /// <param name="targetType">대상 타입</param>
    /// <returns>변환된 YAML</returns>
    string ConvertToFormat(SiteConfiguration configuration, SiteConfigurationType targetType);

    /// <summary>
    /// 설정 마이그레이션 가이드를 생성합니다
    /// </summary>
    /// <param name="sourceConfig">원본 설정</param>
    /// <param name="targetType">대상 타입</param>
    /// <returns>마이그레이션 가이드</returns>
    ConfigurationMigrationGuide GenerateMigrationGuide(
        SiteConfiguration sourceConfig,
        SiteConfigurationType targetType);
}

/// <summary>
/// 설정 최적화 권장사항
/// </summary>
public class ConfigurationRecommendation
{
    /// <summary>권장사항 타입</summary>
    public ConfigurationRecommendationType Type { get; init; }

    /// <summary>우선순위</summary>
    public RecommendationPriority Priority { get; init; }

    /// <summary>제목</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>설명</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>현재 값</summary>
    public string? CurrentValue { get; init; }

    /// <summary>권장 값</summary>
    public string? RecommendedValue { get; init; }

    /// <summary>적용 방법</summary>
    public string? Implementation { get; init; }

    /// <summary>예상 효과</summary>
    public string? ExpectedBenefit { get; init; }
}

/// <summary>
/// 권장사항 타입
/// </summary>
public enum ConfigurationRecommendationType
{
    /// <summary>성능 개선</summary>
    Performance,
    /// <summary>보안 강화</summary>
    Security,
    /// <summary>SEO 최적화</summary>
    Seo,
    /// <summary>접근성 개선</summary>
    Accessibility,
    /// <summary>모범 사례</summary>
    BestPractice,
    /// <summary>호환성</summary>
    Compatibility
}

/// <summary>
/// 권장사항 우선순위
/// </summary>
public enum RecommendationPriority
{
    /// <summary>낮음</summary>
    Low,
    /// <summary>보통</summary>
    Medium,
    /// <summary>높음</summary>
    High,
    /// <summary>필수</summary>
    Critical
}

/// <summary>
/// 설정 마이그레이션 가이드
/// </summary>
public class ConfigurationMigrationGuide
{
    /// <summary>원본 타입</summary>
    public SiteConfigurationType SourceType { get; init; }

    /// <summary>대상 타입</summary>
    public SiteConfigurationType TargetType { get; init; }

    /// <summary>호환성 점수 (0.0 - 1.0)</summary>
    public double CompatibilityScore { get; init; }

    /// <summary>마이그레이션 단계</summary>
    public IReadOnlyList<MigrationStep> Steps { get; init; } = Array.Empty<MigrationStep>();

    /// <summary>변환된 설정</summary>
    public string ConvertedConfiguration { get; init; } = string.Empty;

    /// <summary>호환되지 않는 설정</summary>
    public IReadOnlyList<IncompatibleSetting> IncompatibleSettings { get; init; } = Array.Empty<IncompatibleSetting>();

    /// <summary>추가 작업</summary>
    public IReadOnlyList<string> AdditionalTasks { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 마이그레이션 단계
/// </summary>
public class MigrationStep
{
    /// <summary>단계 번호</summary>
    public int StepNumber { get; init; }

    /// <summary>제목</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>설명</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>명령어</summary>
    public string? Command { get; init; }

    /// <summary>필수 여부</summary>
    public bool IsRequired { get; init; }

    /// <summary>예상 소요 시간 (분)</summary>
    public int EstimatedMinutes { get; init; }
}

/// <summary>
/// 호환되지 않는 설정
/// </summary>
public class IncompatibleSetting
{
    /// <summary>설정 키</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>현재 값</summary>
    public string CurrentValue { get; init; } = string.Empty;

    /// <summary>호환되지 않는 이유</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>대안</summary>
    public string? Alternative { get; init; }

    /// <summary>수동 작업 필요 여부</summary>
    public bool RequiresManualWork { get; init; }
}