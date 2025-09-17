namespace WebFlux.Core.Models;

/// <summary>
/// 메타데이터 발견 결과
/// </summary>
public class MetadataDiscoveryResult
{
    /// <summary>
    /// 웹사이트 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 전체 발견 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 발견된 메타데이터 개수
    /// </summary>
    public int DiscoveredCount { get; set; }

    /// <summary>
    /// 발견 시작 시간
    /// </summary>
    public DateTimeOffset DiscoveredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 발견에 소요된 총 시간 (밀리초)
    /// </summary>
    public long TotalDiscoveryTimeMs { get; set; }

    /// <summary>
    /// robots.txt 메타데이터
    /// </summary>
    public RobotsMetadata? RobotsMetadata { get; set; }

    /// <summary>
    /// sitemap.xml 메타데이터
    /// </summary>
    public SitemapMetadata? SitemapMetadata { get; set; }

    /// <summary>
    /// llms.txt 메타데이터
    /// </summary>
    public LlmsMetadata? LlmsMetadata { get; set; }

    /// <summary>
    /// ai.txt 메타데이터
    /// </summary>
    public AiTxtMetadata? AiTxtMetadata { get; set; }

    /// <summary>
    /// manifest.json 메타데이터
    /// </summary>
    public ManifestMetadata? ManifestMetadata { get; set; }

    /// <summary>
    /// README.md 메타데이터
    /// </summary>
    public ReadmeMetadata? ReadmeMetadata { get; set; }

    /// <summary>
    /// _config.yml 메타데이터
    /// </summary>
    public ConfigMetadata? ConfigMetadata { get; set; }

    /// <summary>
    /// package.json 메타데이터
    /// </summary>
    public PackageMetadata? PackageMetadata { get; set; }

    /// <summary>
    /// humans.txt 메타데이터
    /// </summary>
    public HumansMetadata? HumansMetadata { get; set; }

    /// <summary>
    /// security.txt 메타데이터
    /// </summary>
    public SecurityMetadata? SecurityMetadata { get; set; }

    /// <summary>
    /// ads.txt 메타데이터
    /// </summary>
    public AdsMetadata? AdsMetadata { get; set; }

    /// <summary>
    /// BingSiteAuth.xml 메타데이터
    /// </summary>
    public BingSiteAuthMetadata? BingSiteAuthMetadata { get; set; }

    /// <summary>
    /// OpenAPI 스펙 메타데이터
    /// </summary>
    public OpenApiMetadata? OpenApiMetadata { get; set; }

    /// <summary>
    /// Schema.org 메타데이터
    /// </summary>
    public SchemaMetadata? SchemaMetadata { get; set; }

    /// <summary>
    /// .well-known 메타데이터
    /// </summary>
    public WellKnownMetadata? WellKnownMetadata { get; set; }

    /// <summary>
    /// 발견 오류 목록
    /// </summary>
    public List<MetadataDiscoveryError> Errors { get; set; } = new();

    /// <summary>
    /// 메타데이터 타입별 발견 상태
    /// </summary>
    public Dictionary<MetadataType, DiscoveryStatus> DiscoveryStatus { get; set; } = new();

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new();

    /// <summary>
    /// 발견된 메타데이터의 신뢰성 점수 (0-100)
    /// </summary>
    public double ReliabilityScore { get; set; }

    /// <summary>
    /// 웹사이트 지능 레벨 (0-10)
    /// </summary>
    public int IntelligenceLevel { get; set; }
}

/// <summary>
/// 메타데이터 발견 옵션
/// </summary>
public class MetadataDiscoveryOptions
{
    /// <summary>
    /// 발견할 메타데이터 타입 목록 (null이면 모든 타입)
    /// </summary>
    public List<MetadataType>? TargetTypes { get; set; }

    /// <summary>
    /// 최대 발견 시간 제한 (초)
    /// </summary>
    public int MaxDiscoveryTimeSeconds { get; set; } = 30;

    /// <summary>
    /// 병렬 발견 수행 여부
    /// </summary>
    public bool EnableParallelDiscovery { get; set; } = true;

    /// <summary>
    /// 최대 병렬 연결 수
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// 캐시 사용 여부
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// 캐시 만료 시간
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// 심층 분석 수행 여부
    /// </summary>
    public bool EnableDeepAnalysis { get; set; } = true;

    /// <summary>
    /// 예측 분석 수행 여부
    /// </summary>
    public bool EnablePredictiveAnalysis { get; set; } = true;

    /// <summary>
    /// 오류 시 재시도 횟수
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>
    /// 재시도 간격 (밀리초)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// 타임아웃 설정 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// 사용자 정의 헤더
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// 품질 임계값 (0-100)
    /// </summary>
    public double QualityThreshold { get; set; } = 70.0;

    /// <summary>
    /// 발견 우선순위 목록
    /// </summary>
    public List<MetadataType> DiscoveryPriorities { get; set; } = new()
    {
        MetadataType.Robots,
        MetadataType.Sitemap,
        MetadataType.Llms,
        MetadataType.AiTxt,
        MetadataType.Manifest
    };
}

/// <summary>
/// 구조적 지능 분석 결과
/// </summary>
public class StructuralIntelligenceResult
{
    /// <summary>
    /// 전체 구조적 지능 점수 (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// AI 친화적 지능 점수 (0-100)
    /// </summary>
    public double AiFriendlinessScore { get; set; }

    /// <summary>
    /// 크롤링 최적화 점수 (0-100)
    /// </summary>
    public double CrawlOptimizationScore { get; set; }

    /// <summary>
    /// 구조적 일관성 점수 (0-100)
    /// </summary>
    public double StructuralConsistencyScore { get; set; }

    /// <summary>
    /// 메타데이터 완전성 점수 (0-100)
    /// </summary>
    public double MetadataCompletenessScore { get; set; }

    /// <summary>
    /// 발견된 인텔리전스 패턴
    /// </summary>
    public List<IntelligencePattern> DetectedPatterns { get; set; } = new();

    /// <summary>
    /// 개선 권장사항
    /// </summary>
    public List<ImprovementRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// 웹사이트 성숙도 레벨
    /// </summary>
    public WebsiteMaturityLevel MaturityLevel { get; set; }

    /// <summary>
    /// 예상 크롤링 효율성 개선 정도 (%)
    /// </summary>
    public double ExpectedCrawlEfficiencyImprovement { get; set; }

    /// <summary>
    /// 분석 시간
    /// </summary>
    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 메타데이터 품질 점수
/// </summary>
public class MetadataQualityScore
{
    /// <summary>
    /// 전체 품질 점수 (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// 타입별 품질 점수
    /// </summary>
    public Dictionary<MetadataType, double> TypeScores { get; set; } = new();

    /// <summary>
    /// 품질 지표별 점수
    /// </summary>
    public QualityMetrics Metrics { get; set; } = new();

    /// <summary>
    /// 품질 문제 목록
    /// </summary>
    public List<QualityIssue> Issues { get; set; } = new();

    /// <summary>
    /// 품질 개선 제안
    /// </summary>
    public List<QualityImprovement> ImprovementSuggestions { get; set; } = new();

    /// <summary>
    /// 평가 시간
    /// </summary>
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 메타데이터 발견 오류
/// </summary>
public class MetadataDiscoveryError
{
    /// <summary>
    /// 메타데이터 타입
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 예외 타입
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// 시도한 URL
    /// </summary>
    public string? AttemptedUrl { get; set; }

    /// <summary>
    /// 오류 발생 시간
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// 심각도 레벨
    /// </summary>
    public ErrorSeverity Severity { get; set; }
}

/// <summary>
/// 인텔리전스 패턴
/// </summary>
public class IntelligencePattern
{
    /// <summary>
    /// 패턴 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 패턴 타입
    /// </summary>
    public PatternType Type { get; set; }

    /// <summary>
    /// 패턴 신뢰도 (0-100)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 패턴 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 관련 메타데이터 타입
    /// </summary>
    public List<MetadataType> RelatedMetadataTypes { get; set; } = new();

    /// <summary>
    /// 패턴 증거
    /// </summary>
    public List<string> Evidence { get; set; } = new();

    /// <summary>
    /// 예상 영향도
    /// </summary>
    public double ExpectedImpact { get; set; }
}

/// <summary>
/// 개선 권장사항
/// </summary>
public class ImprovementRecommendation
{
    /// <summary>
    /// 권장사항 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 상세 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 우선순위 (1-10, 높을수록 중요)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 예상 개선 효과 (%)
    /// </summary>
    public double ExpectedImprovement { get; set; }

    /// <summary>
    /// 구현 난이도 (1-10, 높을수록 어려움)
    /// </summary>
    public int ImplementationDifficulty { get; set; }

    /// <summary>
    /// 관련 메타데이터 타입
    /// </summary>
    public List<MetadataType> RelatedMetadataTypes { get; set; } = new();

    /// <summary>
    /// 추천 이유
    /// </summary>
    public List<string> Rationale { get; set; } = new();
}

/// <summary>
/// 품질 지표
/// </summary>
public class QualityMetrics
{
    /// <summary>
    /// 완전성 점수 (0-100)
    /// </summary>
    public double Completeness { get; set; }

    /// <summary>
    /// 정확성 점수 (0-100)
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// 일관성 점수 (0-100)
    /// </summary>
    public double Consistency { get; set; }

    /// <summary>
    /// 최신성 점수 (0-100)
    /// </summary>
    public double Freshness { get; set; }

    /// <summary>
    /// 유용성 점수 (0-100)
    /// </summary>
    public double Usefulness { get; set; }

    /// <summary>
    /// 표준 준수 점수 (0-100)
    /// </summary>
    public double StandardCompliance { get; set; }
}

/// <summary>
/// 품질 문제
/// </summary>
public class QualityIssue
{
    /// <summary>
    /// 문제 유형
    /// </summary>
    public QualityIssueType Type { get; set; }

    /// <summary>
    /// 문제 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 심각도
    /// </summary>
    public QualityIssueSeverity Severity { get; set; }

    /// <summary>
    /// 관련 메타데이터 타입
    /// </summary>
    public MetadataType? RelatedMetadataType { get; set; }

    /// <summary>
    /// 영향 받는 필드
    /// </summary>
    public List<string> AffectedFields { get; set; } = new();
}

/// <summary>
/// 품질 개선 제안
/// </summary>
public class QualityImprovement
{
    /// <summary>
    /// 개선 제안 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 개선 방법
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 예상 점수 향상
    /// </summary>
    public double ExpectedScoreIncrease { get; set; }

    /// <summary>
    /// 구현 우선순위
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 관련 메타데이터 타입
    /// </summary>
    public List<MetadataType> RelatedTypes { get; set; } = new();
}

/// <summary>
/// 메타데이터 발견 통계
/// </summary>
public class MetadataDiscoveryStatistics
{
    /// <summary>
    /// 총 발견 시도 횟수
    /// </summary>
    public int TotalDiscoveryAttempts { get; set; }

    /// <summary>
    /// 성공한 발견 횟수
    /// </summary>
    public int SuccessfulDiscoveries { get; set; }

    /// <summary>
    /// 평균 발견 시간 (밀리초)
    /// </summary>
    public double AverageDiscoveryTime { get; set; }

    /// <summary>
    /// 타입별 발견 성공률
    /// </summary>
    public Dictionary<MetadataType, double> TypeSuccessRates { get; set; } = new();

    /// <summary>
    /// 타입별 평균 품질 점수
    /// </summary>
    public Dictionary<MetadataType, double> AverageQualityScores { get; set; } = new();

    /// <summary>
    /// 가장 일반적인 오류들
    /// </summary>
    public Dictionary<string, int> CommonErrors { get; set; } = new();

    /// <summary>
    /// 캐시 히트율
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 메타데이터 타입
/// </summary>
public enum MetadataType
{
    /// <summary>
    /// robots.txt
    /// </summary>
    Robots,

    /// <summary>
    /// sitemap.xml
    /// </summary>
    Sitemap,

    /// <summary>
    /// llms.txt
    /// </summary>
    Llms,

    /// <summary>
    /// ai.txt
    /// </summary>
    AiTxt,

    /// <summary>
    /// manifest.json
    /// </summary>
    Manifest,

    /// <summary>
    /// README.md
    /// </summary>
    Readme,

    /// <summary>
    /// _config.yml
    /// </summary>
    Config,

    /// <summary>
    /// package.json
    /// </summary>
    Package,

    /// <summary>
    /// humans.txt
    /// </summary>
    Humans,

    /// <summary>
    /// security.txt
    /// </summary>
    Security,

    /// <summary>
    /// ads.txt
    /// </summary>
    Ads,

    /// <summary>
    /// BingSiteAuth.xml
    /// </summary>
    BingSiteAuth,

    /// <summary>
    /// OpenAPI 스펙
    /// </summary>
    OpenApi,

    /// <summary>
    /// Schema.org
    /// </summary>
    Schema,

    /// <summary>
    /// .well-known
    /// </summary>
    WellKnown
}

/// <summary>
/// 발견 상태
/// </summary>
public enum DiscoveryStatus
{
    /// <summary>
    /// 발견 안 함
    /// </summary>
    NotFound,

    /// <summary>
    /// 발견 중
    /// </summary>
    Discovering,

    /// <summary>
    /// 발견 성공
    /// </summary>
    Found,

    /// <summary>
    /// 발견 실패
    /// </summary>
    Failed,

    /// <summary>
    /// 건너뜀
    /// </summary>
    Skipped,

    /// <summary>
    /// 캐시에서 로드됨
    /// </summary>
    FromCache
}

/// <summary>
/// 오류 심각도
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// 낮음
    /// </summary>
    Low,

    /// <summary>
    /// 보통
    /// </summary>
    Medium,

    /// <summary>
    /// 높음
    /// </summary>
    High,

    /// <summary>
    /// 오류
    /// </summary>
    Error,

    /// <summary>
    /// 심각
    /// </summary>
    Critical
}

/// <summary>
/// 웹사이트 성숙도 레벨
/// </summary>
public enum WebsiteMaturityLevel
{
    /// <summary>
    /// 기본 (메타데이터 없음)
    /// </summary>
    Basic,

    /// <summary>
    /// 초보 (기본 메타데이터만)
    /// </summary>
    Beginner,

    /// <summary>
    /// 중급 (일부 고급 메타데이터)
    /// </summary>
    Intermediate,

    /// <summary>
    /// 고급 (대부분의 메타데이터)
    /// </summary>
    Advanced,

    /// <summary>
    /// 전문가 (모든 메타데이터 + AI 최적화)
    /// </summary>
    Expert
}

/// <summary>
/// 품질 문제 유형
/// </summary>
public enum QualityIssueType
{
    /// <summary>
    /// 누락된 필수 필드
    /// </summary>
    MissingRequiredField,

    /// <summary>
    /// 잘못된 형식
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// 오래된 데이터
    /// </summary>
    OutdatedData,

    /// <summary>
    /// 일관성 부족
    /// </summary>
    InconsistentData,

    /// <summary>
    /// 표준 위반
    /// </summary>
    StandardViolation,

    /// <summary>
    /// 불완전한 정보
    /// </summary>
    IncompleteInformation
}

/// <summary>
/// 품질 문제 심각도
/// </summary>
public enum QualityIssueSeverity
{
    /// <summary>
    /// 정보성
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

// 추가로 필요한 메타데이터 모델들은 별도 파일로 구현 예정
// (AiTxtMetadata, ManifestMetadata, ReadmeMetadata, 등)
// 현재는 기본 구조만 정의