namespace WebFlux.Core.Models;

/// <summary>
/// Web App Manifest 파싱 결과
/// </summary>
public class ManifestParseResult
{
    /// <summary>
    /// 파싱 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// manifest.json 파일이 발견되었는지 여부
    /// </summary>
    public bool FileFound { get; set; }

    /// <summary>
    /// manifest.json URL
    /// </summary>
    public string ManifestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 파싱된 메타데이터
    /// </summary>
    public ManifestMetadata? Metadata { get; set; }

    /// <summary>
    /// PWA 호환성 평가 결과
    /// </summary>
    public PwaCompatibilityResult? PwaCompatibility { get; set; }

    /// <summary>
    /// 아이콘 분석 결과
    /// </summary>
    public IconAnalysisResult? IconAnalysis { get; set; }

    /// <summary>
    /// 파싱 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 원본 manifest.json 내용
    /// </summary>
    public string? RawContent { get; set; }

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Web App Manifest 메타데이터
/// </summary>
public class ManifestMetadata
{
    /// <summary>
    /// 웹사이트 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 애플리케이션 이름
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 짧은 이름
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// 애플리케이션 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 시작 URL
    /// </summary>
    public string? StartUrl { get; set; }

    /// <summary>
    /// 범위 (scope)
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// 표시 모드
    /// </summary>
    public DisplayMode? Display { get; set; }

    /// <summary>
    /// 화면 방향
    /// </summary>
    public ScreenOrientation? Orientation { get; set; }

    /// <summary>
    /// 테마 색상
    /// </summary>
    public string? ThemeColor { get; set; }

    /// <summary>
    /// 배경 색상
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 언어
    /// </summary>
    public string? Lang { get; set; }

    /// <summary>
    /// 텍스트 방향
    /// </summary>
    public TextDirection? Dir { get; set; }

    /// <summary>
    /// 아이콘 목록
    /// </summary>
    public List<ManifestIcon> Icons { get; set; } = new();

    /// <summary>
    /// 스크린샷 목록
    /// </summary>
    public List<ManifestScreenshot> Screenshots { get; set; } = new();

    /// <summary>
    /// 카테고리 목록
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// IARC 등급 ID
    /// </summary>
    public string? IarcRatingId { get; set; }

    /// <summary>
    /// 관련 애플리케이션
    /// </summary>
    public List<RelatedApplication> RelatedApplications { get; set; } = new();

    /// <summary>
    /// 관련 애플리케이션 우선 사용 여부
    /// </summary>
    public bool? PreferRelatedApplications { get; set; }

    /// <summary>
    /// 바로가기 목록
    /// </summary>
    public List<ManifestShortcut> Shortcuts { get; set; } = new();

    /// <summary>
    /// 프로토콜 핸들러
    /// </summary>
    public List<ProtocolHandler> ProtocolHandlers { get; set; } = new();

    /// <summary>
    /// 파일 핸들러
    /// </summary>
    public List<FileHandler> FileHandlers { get; set; } = new();

    /// <summary>
    /// 공유 대상
    /// </summary>
    public ShareTarget? ShareTarget { get; set; }

    /// <summary>
    /// 추가 기능들
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();

    /// <summary>
    /// 매니페스트 스펙 버전
    /// </summary>
    public string SpecVersion { get; set; } = "1.0";

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 매니페스트 아이콘
/// </summary>
public class ManifestIcon
{
    /// <summary>
    /// 아이콘 URL
    /// </summary>
    public string Src { get; set; } = string.Empty;

    /// <summary>
    /// 아이콘 크기 목록
    /// </summary>
    public List<string> Sizes { get; set; } = new();

    /// <summary>
    /// 아이콘 타입 (MIME 타입)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 아이콘 목적 (any, maskable, monochrome)
    /// </summary>
    public List<IconPurpose> Purpose { get; set; } = new();

    /// <summary>
    /// 플랫폼별 추가 정보
    /// </summary>
    public Dictionary<string, object> Platform { get; set; } = new();
}

/// <summary>
/// 매니페스트 스크린샷
/// </summary>
public class ManifestScreenshot
{
    /// <summary>
    /// 스크린샷 URL
    /// </summary>
    public string Src { get; set; } = string.Empty;

    /// <summary>
    /// 스크린샷 크기 목록
    /// </summary>
    public List<string> Sizes { get; set; } = new();

    /// <summary>
    /// 스크린샷 타입 (MIME 타입)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 스크린샷 형태 인수
    /// </summary>
    public ScreenshotFormFactor? FormFactor { get; set; }

    /// <summary>
    /// 스크린샷 라벨
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// 플랫폼별 추가 정보
    /// </summary>
    public Dictionary<string, object> Platform { get; set; } = new();
}

/// <summary>
/// 관련 애플리케이션
/// </summary>
public class RelatedApplication
{
    /// <summary>
    /// 플랫폼 (play, itunes, windows, etc.)
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 애플리케이션 URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 애플리케이션 ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 최소 버전
    /// </summary>
    public string? MinVersion { get; set; }

    /// <summary>
    /// 핑거프린트
    /// </summary>
    public List<AppFingerprint> Fingerprints { get; set; } = new();
}

/// <summary>
/// 애플리케이션 핑거프린트
/// </summary>
public class AppFingerprint
{
    /// <summary>
    /// 핑거프린트 타입
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 핑거프린트 값
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// 매니페스트 바로가기
/// </summary>
public class ManifestShortcut
{
    /// <summary>
    /// 바로가기 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 짧은 이름
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 아이콘 목록
    /// </summary>
    public List<ManifestIcon> Icons { get; set; } = new();
}

/// <summary>
/// 프로토콜 핸들러
/// </summary>
public class ProtocolHandler
{
    /// <summary>
    /// 프로토콜 스킴
    /// </summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// 핸들러 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// 파일 핸들러
/// </summary>
public class FileHandler
{
    /// <summary>
    /// 액션 URL
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 수락하는 파일 타입
    /// </summary>
    public Dictionary<string, List<string>> Accept { get; set; } = new();

    /// <summary>
    /// 아이콘 목록
    /// </summary>
    public List<ManifestIcon> Icons { get; set; } = new();

    /// <summary>
    /// 런치 타입
    /// </summary>
    public string? LaunchType { get; set; }
}

/// <summary>
/// 공유 대상
/// </summary>
public class ShareTarget
{
    /// <summary>
    /// 액션 URL
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 메서드
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// 인코딩 타입
    /// </summary>
    public string Enctype { get; set; } = "application/x-www-form-urlencoded";

    /// <summary>
    /// 매개변수
    /// </summary>
    public ShareTargetParams? Params { get; set; }
}

/// <summary>
/// 공유 대상 매개변수
/// </summary>
public class ShareTargetParams
{
    /// <summary>
    /// 제목 매개변수명
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 텍스트 매개변수명
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// URL 매개변수명
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 파일 매개변수명 및 타입
    /// </summary>
    public List<ShareTargetFile> Files { get; set; } = new();
}

/// <summary>
/// 공유 대상 파일
/// </summary>
public class ShareTargetFile
{
    /// <summary>
    /// 매개변수명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 수락하는 파일 타입
    /// </summary>
    public List<string> Accept { get; set; } = new();
}

/// <summary>
/// PWA 호환성 평가 결과
/// </summary>
public class PwaCompatibilityResult
{
    /// <summary>
    /// 전체 PWA 점수 (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// PWA 필수 요구사항 충족 여부
    /// </summary>
    public bool MeetsMinimumRequirements { get; set; }

    /// <summary>
    /// 설치 가능 여부
    /// </summary>
    public bool IsInstallable { get; set; }

    /// <summary>
    /// 오프라인 지원 여부 (추정)
    /// </summary>
    public bool SupportsOffline { get; set; }

    /// <summary>
    /// 평가 세부 항목
    /// </summary>
    public List<PwaRequirement> Requirements { get; set; } = new();

    /// <summary>
    /// 개선 권장사항
    /// </summary>
    public List<PwaImprovement> ImprovementSuggestions { get; set; } = new();

    /// <summary>
    /// PWA 성숙도 레벨
    /// </summary>
    public PwaMaturityLevel MaturityLevel { get; set; }

    /// <summary>
    /// 플랫폼별 지원 현황
    /// </summary>
    public Dictionary<string, bool> PlatformSupport { get; set; } = new();

    /// <summary>
    /// 평가 시간
    /// </summary>
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// PWA 요구사항
/// </summary>
public class PwaRequirement
{
    /// <summary>
    /// 요구사항 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 충족 여부
    /// </summary>
    public bool IsMet { get; set; }

    /// <summary>
    /// 점수 (0-100)
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 중요도
    /// </summary>
    public RequirementImportance Importance { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 실패 이유
    /// </summary>
    public string? FailureReason { get; set; }
}

/// <summary>
/// PWA 개선사항
/// </summary>
public class PwaImprovement
{
    /// <summary>
    /// 개선사항 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 상세 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 우선순위 (1-10)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 예상 점수 향상
    /// </summary>
    public double ExpectedScoreIncrease { get; set; }

    /// <summary>
    /// 구현 난이도 (1-10)
    /// </summary>
    public int ImplementationDifficulty { get; set; }

    /// <summary>
    /// 카테고리
    /// </summary>
    public ImprovementCategory Category { get; set; }
}

/// <summary>
/// 아이콘 분석 결과
/// </summary>
public class IconAnalysisResult
{
    /// <summary>
    /// 총 아이콘 개수
    /// </summary>
    public int TotalIcons { get; set; }

    /// <summary>
    /// 사용 가능한 크기 목록
    /// </summary>
    public List<string> AvailableSizes { get; set; } = new();

    /// <summary>
    /// 지원하는 형식
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new();

    /// <summary>
    /// Maskable 아이콘 지원 여부
    /// </summary>
    public bool SupportsMaskable { get; set; }

    /// <summary>
    /// Monochrome 아이콘 지원 여부
    /// </summary>
    public bool SupportsMonochrome { get; set; }

    /// <summary>
    /// 플랫폼별 권장 크기 충족 현황
    /// </summary>
    public Dictionary<string, IconCoverage> PlatformCoverage { get; set; } = new();

    /// <summary>
    /// 아이콘 품질 점수 (0-100)
    /// </summary>
    public double QualityScore { get; set; }

    /// <summary>
    /// 누락된 권장 크기
    /// </summary>
    public List<string> MissingRecommendedSizes { get; set; } = new();

    /// <summary>
    /// 아이콘 최적화 권장사항
    /// </summary>
    public List<string> OptimizationSuggestions { get; set; } = new();
}

/// <summary>
/// 아이콘 커버리지
/// </summary>
public class IconCoverage
{
    /// <summary>
    /// 플랫폼명
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 커버리지 점수 (0-100)
    /// </summary>
    public double CoverageScore { get; set; }

    /// <summary>
    /// 필요한 크기들
    /// </summary>
    public List<string> RequiredSizes { get; set; } = new();

    /// <summary>
    /// 사용 가능한 크기들
    /// </summary>
    public List<string> AvailableSizes { get; set; } = new();

    /// <summary>
    /// 누락된 크기들
    /// </summary>
    public List<string> MissingSizes { get; set; } = new();
}

/// <summary>
/// 앱 카테고리 예측 결과
/// </summary>
public class AppCategoryPrediction
{
    /// <summary>
    /// 예측된 주 카테고리
    /// </summary>
    public string PrimaryCategory { get; set; } = string.Empty;

    /// <summary>
    /// 예측 신뢰도 (0-100)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 가능한 카테고리들과 점수
    /// </summary>
    public Dictionary<string, double> CategoryScores { get; set; } = new();

    /// <summary>
    /// 예측 근거
    /// </summary>
    public List<string> PredictionReasons { get; set; } = new();

    /// <summary>
    /// W3C 표준 카테고리 매핑
    /// </summary>
    public List<string> StandardCategories { get; set; } = new();
}

/// <summary>
/// 매니페스트 유효성 검사 결과
/// </summary>
public class ManifestValidationResult
{
    /// <summary>
    /// 전체 유효성
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 검증 점수 (0-100)
    /// </summary>
    public double ValidationScore { get; set; }

    /// <summary>
    /// 검증 규칙 결과
    /// </summary>
    public List<ValidationRule> RuleResults { get; set; } = new();

    /// <summary>
    /// 경고 메시지
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 표준 준수 레벨
    /// </summary>
    public StandardComplianceLevel ComplianceLevel { get; set; }
}

/// <summary>
/// 검증 규칙
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// 규칙 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 통과 여부
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 심각도
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// 메시지
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 권장 수정사항
    /// </summary>
    public string? Recommendation { get; set; }
}

/// <summary>
/// 매니페스트 파싱 통계
/// </summary>
public class ManifestStatistics
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
    /// manifest.json 파일이 발견된 사이트 수
    /// </summary>
    public int SitesWithManifest { get; set; }

    /// <summary>
    /// PWA 호환 사이트 수
    /// </summary>
    public int PwaCompatibleSites { get; set; }

    /// <summary>
    /// 평균 파싱 시간 (밀리초)
    /// </summary>
    public double AverageParseTime { get; set; }

    /// <summary>
    /// 평균 PWA 점수
    /// </summary>
    public double AveragePwaScore { get; set; }

    /// <summary>
    /// 가장 일반적인 표시 모드
    /// </summary>
    public Dictionary<DisplayMode, int> DisplayModeDistribution { get; set; } = new();

    /// <summary>
    /// 가장 일반적인 카테고리
    /// </summary>
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();

    /// <summary>
    /// 가장 일반적인 오류들
    /// </summary>
    public Dictionary<string, int> CommonErrors { get; set; } = new();

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

// 열거형 정의

/// <summary>
/// 표시 모드
/// </summary>
public enum DisplayMode
{
    /// <summary>
    /// 풀스크린
    /// </summary>
    Fullscreen,

    /// <summary>
    /// 독립형 (권장)
    /// </summary>
    Standalone,

    /// <summary>
    /// 최소 UI
    /// </summary>
    MinimalUi,

    /// <summary>
    /// 브라우저
    /// </summary>
    Browser
}

/// <summary>
/// 화면 방향
/// </summary>
public enum ScreenOrientation
{
    /// <summary>
    /// 모든 방향
    /// </summary>
    Any,

    /// <summary>
    /// 자연 방향
    /// </summary>
    Natural,

    /// <summary>
    /// 가로
    /// </summary>
    Landscape,

    /// <summary>
    /// 가로 (primary)
    /// </summary>
    LandscapePrimary,

    /// <summary>
    /// 가로 (secondary)
    /// </summary>
    LandscapeSecondary,

    /// <summary>
    /// 세로
    /// </summary>
    Portrait,

    /// <summary>
    /// 세로 (primary)
    /// </summary>
    PortraitPrimary,

    /// <summary>
    /// 세로 (secondary)
    /// </summary>
    PortraitSecondary
}

/// <summary>
/// 텍스트 방향
/// </summary>
public enum TextDirection
{
    /// <summary>
    /// 왼쪽에서 오른쪽
    /// </summary>
    Ltr,

    /// <summary>
    /// 오른쪽에서 왼쪽
    /// </summary>
    Rtl,

    /// <summary>
    /// 자동
    /// </summary>
    Auto
}

/// <summary>
/// 아이콘 목적
/// </summary>
public enum IconPurpose
{
    /// <summary>
    /// 모든 용도
    /// </summary>
    Any,

    /// <summary>
    /// 마스크 가능한 아이콘
    /// </summary>
    Maskable,

    /// <summary>
    /// 단색 아이콘
    /// </summary>
    Monochrome
}

/// <summary>
/// 스크린샷 형태 인수
/// </summary>
public enum ScreenshotFormFactor
{
    /// <summary>
    /// 좁은 화면 (모바일)
    /// </summary>
    Narrow,

    /// <summary>
    /// 넓은 화면 (데스크톱)
    /// </summary>
    Wide
}

/// <summary>
/// PWA 성숙도 레벨
/// </summary>
public enum PwaMaturityLevel
{
    /// <summary>
    /// 기본 웹사이트
    /// </summary>
    Basic,

    /// <summary>
    /// PWA 시작 단계
    /// </summary>
    Beginner,

    /// <summary>
    /// PWA 중급
    /// </summary>
    Intermediate,

    /// <summary>
    /// PWA 고급
    /// </summary>
    Advanced,

    /// <summary>
    /// PWA 전문가
    /// </summary>
    Expert
}

/// <summary>
/// 요구사항 중요도
/// </summary>
public enum RequirementImportance
{
    /// <summary>
    /// 필수
    /// </summary>
    Critical,

    /// <summary>
    /// 중요
    /// </summary>
    High,

    /// <summary>
    /// 보통
    /// </summary>
    Medium,

    /// <summary>
    /// 낮음
    /// </summary>
    Low
}

/// <summary>
/// 개선사항 카테고리
/// </summary>
public enum ImprovementCategory
{
    /// <summary>
    /// 매니페스트 기본사항
    /// </summary>
    ManifestBasics,

    /// <summary>
    /// 아이콘 및 이미지
    /// </summary>
    IconsAndImages,

    /// <summary>
    /// 사용자 경험
    /// </summary>
    UserExperience,

    /// <summary>
    /// 성능
    /// </summary>
    Performance,

    /// <summary>
    /// 접근성
    /// </summary>
    Accessibility,

    /// <summary>
    /// 고급 기능
    /// </summary>
    AdvancedFeatures
}

/// <summary>
/// 표준 준수 레벨
/// </summary>
public enum StandardComplianceLevel
{
    /// <summary>
    /// 완전 준수
    /// </summary>
    FullCompliance,

    /// <summary>
    /// 높은 준수
    /// </summary>
    HighCompliance,

    /// <summary>
    /// 보통 준수
    /// </summary>
    MediumCompliance,

    /// <summary>
    /// 낮은 준수
    /// </summary>
    LowCompliance,

    /// <summary>
    /// 미준수
    /// </summary>
    NonCompliant
}

/// <summary>
/// 검증 심각도
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// 정보
    /// </summary>
    Info,

    /// <summary>
    /// 경고
    /// </summary>
    Warning,

    /// <summary>
    /// 오류
    /// </summary>
    Error,

    /// <summary>
    /// 심각
    /// </summary>
    Critical
}