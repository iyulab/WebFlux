namespace WebFlux.Core.Models;

/// <summary>
/// 패키지 생태계 분석 결과
/// </summary>
public class PackageEcosystemAnalysisResult
{
    /// <summary>
    /// 발견된 패키지 파일들
    /// </summary>
    public List<PackageFileInfo> DiscoveredPackageFiles { get; set; } = new();

    /// <summary>
    /// 주요 기술 스택
    /// </summary>
    public TechStackAnalysisResult? PrimaryTechStack { get; set; }

    /// <summary>
    /// 프로젝트 복잡도 평가
    /// </summary>
    public ProjectComplexityResult? ComplexityAnalysis { get; set; }

    /// <summary>
    /// 보안 분석 결과
    /// </summary>
    public SecurityAnalysisResult? SecurityAnalysis { get; set; }

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
/// 패키지 파일 정보
/// </summary>
public class PackageFileInfo
{
    /// <summary>
    /// 파일 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 파일명
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 패키지 생태계 타입
    /// </summary>
    public PackageEcosystemType EcosystemType { get; set; }

    /// <summary>
    /// 파일 크기 (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 발견 방법
    /// </summary>
    public string DiscoveryMethod { get; set; } = string.Empty;
}

/// <summary>
/// 패키지 메타데이터
/// </summary>
public class PackageMetadata
{
    /// <summary>
    /// 프로젝트명
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// 프로젝트 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 프로젝트 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 패키지 생태계 타입
    /// </summary>
    public PackageEcosystemType EcosystemType { get; set; }

    /// <summary>
    /// 의존성 목록
    /// </summary>
    public List<DependencyInfo> Dependencies { get; set; } = new();

    /// <summary>
    /// 개발 의존성 목록
    /// </summary>
    public List<DependencyInfo> DevDependencies { get; set; } = new();

    /// <summary>
    /// 스크립트 정보
    /// </summary>
    public Dictionary<string, string> Scripts { get; set; } = new();

    /// <summary>
    /// 라이선스 정보
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// 저장소 URL
    /// </summary>
    public string RepositoryUrl { get; set; } = string.Empty;

    /// <summary>
    /// 키워드
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// 원본 내용
    /// </summary>
    public string RawContent { get; set; } = string.Empty;
}

/// <summary>
/// 의존성 정보
/// </summary>
public class DependencyInfo
{
    /// <summary>
    /// 패키지명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 버전 제약
    /// </summary>
    public string VersionConstraint { get; set; } = string.Empty;

    /// <summary>
    /// 의존성 타입
    /// </summary>
    public DependencyType Type { get; set; }

    /// <summary>
    /// 보안 위험도 (0.0-1.0)
    /// </summary>
    public double SecurityRisk { get; set; }

    /// <summary>
    /// 인기도 점수 (0.0-1.0)
    /// </summary>
    public double PopularityScore { get; set; }

    /// <summary>
    /// 카테고리
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// 기술 스택 분석 결과
/// </summary>
public class TechStackAnalysisResult
{
    /// <summary>
    /// 주요 프로그래밍 언어
    /// </summary>
    public string PrimaryLanguage { get; set; } = string.Empty;

    /// <summary>
    /// 프레임워크 정보
    /// </summary>
    public List<FrameworkInfo> Frameworks { get; set; } = new();

    /// <summary>
    /// 데이터베이스 기술
    /// </summary>
    public List<string> Databases { get; set; } = new();

    /// <summary>
    /// 빌드 도구
    /// </summary>
    public List<string> BuildTools { get; set; } = new();

    /// <summary>
    /// 테스트 프레임워크
    /// </summary>
    public List<string> TestingFrameworks { get; set; } = new();

    /// <summary>
    /// 프로젝트 타입 (웹앱, 라이브러리, CLI 등)
    /// </summary>
    public ProjectType ProjectType { get; set; }

    /// <summary>
    /// 아키텍처 패턴
    /// </summary>
    public List<string> ArchitecturalPatterns { get; set; } = new();

    /// <summary>
    /// 기술 성숙도 점수 (0.0-1.0)
    /// </summary>
    public double TechMaturityScore { get; set; }
}

/// <summary>
/// 프레임워크 정보
/// </summary>
public class FrameworkInfo
{
    /// <summary>
    /// 프레임워크명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 카테고리 (웹, 모바일, 데스크톱 등)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 인기도 점수 (0.0-1.0)
    /// </summary>
    public double PopularityScore { get; set; }
}

/// <summary>
/// 프로젝트 복잡도 분석 결과
/// </summary>
public class ProjectComplexityResult
{
    /// <summary>
    /// 복잡도 점수 (0.0-1.0)
    /// </summary>
    public double ComplexityScore { get; set; }

    /// <summary>
    /// 의존성 수
    /// </summary>
    public int DependencyCount { get; set; }

    /// <summary>
    /// 프로젝트 규모
    /// </summary>
    public ProjectScale Scale { get; set; }

    /// <summary>
    /// 성숙도 레벨
    /// </summary>
    public ProjectMaturityLevel MaturityLevel { get; set; }

    /// <summary>
    /// 유지보수성 점수 (0.0-1.0)
    /// </summary>
    public double MaintainabilityScore { get; set; }

    /// <summary>
    /// 복잡도 요인들
    /// </summary>
    public List<ComplexityFactor> ComplexityFactors { get; set; } = new();
}

/// <summary>
/// 복잡도 요인
/// </summary>
public class ComplexityFactor
{
    /// <summary>
    /// 요인명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 점수 (0.0-1.0)
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 보안 분석 결과
/// </summary>
public class SecurityAnalysisResult
{
    /// <summary>
    /// 전체 보안 점수 (0.0-1.0, 높을수록 안전)
    /// </summary>
    public double SecurityScore { get; set; }

    /// <summary>
    /// 알려진 취약성
    /// </summary>
    public List<VulnerabilityInfo> KnownVulnerabilities { get; set; } = new();

    /// <summary>
    /// 위험한 의존성
    /// </summary>
    public List<RiskyDependency> RiskyDependencies { get; set; } = new();

    /// <summary>
    /// 보안 권장사항
    /// </summary>
    public List<SecurityRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// 라이선스 호환성
    /// </summary>
    public LicenseCompatibilityResult? LicenseCompatibility { get; set; }
}

/// <summary>
/// 취약성 정보
/// </summary>
public class VulnerabilityInfo
{
    /// <summary>
    /// CVE ID
    /// </summary>
    public string CveId { get; set; } = string.Empty;

    /// <summary>
    /// 영향받는 패키지
    /// </summary>
    public string AffectedPackage { get; set; } = string.Empty;

    /// <summary>
    /// 심각도
    /// </summary>
    public VulnerabilitySeverity Severity { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 수정 버전
    /// </summary>
    public string? FixedInVersion { get; set; }
}

/// <summary>
/// 위험한 의존성
/// </summary>
public class RiskyDependency
{
    /// <summary>
    /// 패키지명
    /// </summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>
    /// 위험 타입
    /// </summary>
    public RiskType RiskType { get; set; }

    /// <summary>
    /// 위험도 점수 (0.0-1.0)
    /// </summary>
    public double RiskScore { get; set; }

    /// <summary>
    /// 위험 설명
    /// </summary>
    public string RiskDescription { get; set; } = string.Empty;
}

/// <summary>
/// 보안 권장사항
/// </summary>
public class SecurityRecommendation
{
    /// <summary>
    /// 권장사항 타입
    /// </summary>
    public RecommendationType Type { get; set; }

    /// <summary>
    /// 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 우선순위 (0.0-1.0)
    /// </summary>
    public double Priority { get; set; }
}

/// <summary>
/// 라이선스 호환성 결과
/// </summary>
public class LicenseCompatibilityResult
{
    /// <summary>
    /// 호환성 점수 (0.0-1.0)
    /// </summary>
    public double CompatibilityScore { get; set; }

    /// <summary>
    /// 라이선스 충돌
    /// </summary>
    public List<LicenseConflict> Conflicts { get; set; } = new();

    /// <summary>
    /// 상업적 사용 가능 여부
    /// </summary>
    public bool CommercialUseAllowed { get; set; }
}

/// <summary>
/// 라이선스 충돌
/// </summary>
public class LicenseConflict
{
    /// <summary>
    /// 충돌하는 패키지들
    /// </summary>
    public List<string> ConflictingPackages { get; set; } = new();

    /// <summary>
    /// 충돌 설명
    /// </summary>
    public string ConflictDescription { get; set; } = string.Empty;

    /// <summary>
    /// 심각도
    /// </summary>
    public ConflictSeverity Severity { get; set; }
}

/// <summary>
/// 패키지 생태계 타입
/// </summary>
public enum PackageEcosystemType
{
    Unknown,
    NodeJs,           // package.json
    Python,           // requirements.txt, setup.py, pyproject.toml
    CSharp,           // *.csproj, packages.config
    Java,             // pom.xml, build.gradle
    Php,              // composer.json
    Ruby,             // Gemfile
    Go,               // go.mod
    Rust,             // Cargo.toml
    Swift,            // Package.swift
    Dart              // pubspec.yaml
}

/// <summary>
/// 의존성 타입
/// </summary>
public enum DependencyType
{
    Production,
    Development,
    Test,
    Build,
    Peer,
    Optional
}

/// <summary>
/// 프로젝트 타입
/// </summary>
public enum ProjectType
{
    Unknown,
    WebApplication,
    MobileApp,
    DesktopApp,
    Library,
    Framework,
    CLI,
    API,
    Microservice,
    Game,
    Plugin
}

/// <summary>
/// 프로젝트 규모
/// </summary>
public enum ProjectScale
{
    Unknown,
    Small,      // < 10 dependencies
    Medium,     // 10-50 dependencies
    Large,      // 50-200 dependencies
    Enterprise  // > 200 dependencies
}

/// <summary>
/// 프로젝트 성숙도 레벨
/// </summary>
public enum ProjectMaturityLevel
{
    Unknown,
    Experimental,   // 0.x 버전
    Beta,           // 1.0 미만
    Stable,         // 1.0 이상
    Mature,         // 장기간 안정적 업데이트
    Legacy          // 오래된 의존성 사용
}

/// <summary>
/// 취약성 심각도
/// </summary>
public enum VulnerabilitySeverity
{
    Unknown,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// 위험 타입
/// </summary>
public enum RiskType
{
    Unknown,
    SecurityVulnerability,
    LicenseIncompatibility,
    Deprecated,
    Unmaintained,
    MaliciousCode,
    PrivacyRisk
}

/// <summary>
/// 권장사항 타입
/// </summary>
public enum RecommendationType
{
    Unknown,
    UpdateDependency,
    RemoveDependency,
    AddSecurityTool,
    ChangeConfiguration,
    LicenseReview
}

/// <summary>
/// 충돌 심각도
/// </summary>
public enum ConflictSeverity
{
    Unknown,
    Minor,
    Moderate,
    Major,
    Critical
}