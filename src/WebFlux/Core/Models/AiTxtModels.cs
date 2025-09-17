namespace WebFlux.Core.Models;

/// <summary>
/// ai.txt 파싱 결과
/// </summary>
public class AiTxtParseResult
{
    /// <summary>
    /// 파싱 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// ai.txt 파일이 발견되었는지 여부
    /// </summary>
    public bool FileFound { get; set; }

    /// <summary>
    /// ai.txt URL
    /// </summary>
    public string AiTxtUrl { get; set; } = string.Empty;

    /// <summary>
    /// 파싱된 메타데이터
    /// </summary>
    public AiTxtMetadata? Metadata { get; set; }

    /// <summary>
    /// 파싱 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 원본 ai.txt 내용
    /// </summary>
    public string? RawContent { get; set; }

    /// <summary>
    /// 파싱 시간
    /// </summary>
    public DateTimeOffset ParsedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// ai.txt 메타데이터
/// </summary>
public class AiTxtMetadata
{
    /// <summary>
    /// 웹사이트 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// ai.txt 버전
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 사이트 소유자/관리자 정보
    /// </summary>
    public SiteOwnerInfo? Owner { get; set; }

    /// <summary>
    /// AI 에이전트별 권한 설정
    /// </summary>
    public Dictionary<string, AiAgentPermissions> AgentPermissions { get; set; } = new();

    /// <summary>
    /// 기본 AI 권한 (명시되지 않은 에이전트용)
    /// </summary>
    public AiAgentPermissions? DefaultPermissions { get; set; }

    /// <summary>
    /// 콘텐츠 라이센스 정보
    /// </summary>
    public List<ContentLicense> ContentLicenses { get; set; } = new();

    /// <summary>
    /// 데이터 사용 정책
    /// </summary>
    public List<DataUsagePolicy> DataUsagePolicies { get; set; } = new();

    /// <summary>
    /// 연락처 정보
    /// </summary>
    public ContactInfo? Contact { get; set; }

    /// <summary>
    /// 지원되는 AI 모델/서비스
    /// </summary>
    public List<SupportedAiModel> SupportedModels { get; set; } = new();

    /// <summary>
    /// API 엔드포인트 정보
    /// </summary>
    public List<ApiEndpoint> ApiEndpoints { get; set; } = new();

    /// <summary>
    /// 콘텐츠 분류 체계
    /// </summary>
    public List<ContentCategory> ContentCategories { get; set; } = new();

    /// <summary>
    /// 크롤링 가이드라인
    /// </summary>
    public AiCrawlingGuidelines? CrawlingGuidelines { get; set; }

    /// <summary>
    /// 윤리 가이드라인
    /// </summary>
    public EthicsGuidelines? EthicsGuidelines { get; set; }

    /// <summary>
    /// 개인정보보호 정책
    /// </summary>
    public PrivacyPolicy? PrivacyPolicy { get; set; }

    /// <summary>
    /// 보안 요구사항
    /// </summary>
    public SecurityRequirements? SecurityRequirements { get; set; }

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }

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
/// 사이트 소유자 정보
/// </summary>
public class SiteOwnerInfo
{
    /// <summary>
    /// 소유자 이름 또는 조직명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 이메일 주소
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 웹사이트 URL
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// 조직 유형
    /// </summary>
    public OrganizationType? OrganizationType { get; set; }

    /// <summary>
    /// 국가/지역
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 법적 주체
    /// </summary>
    public string? LegalEntity { get; set; }

    /// <summary>
    /// 소셜 미디어 링크
    /// </summary>
    public Dictionary<string, string> SocialMedia { get; set; } = new();
}

/// <summary>
/// AI 에이전트 권한
/// </summary>
public class AiAgentPermissions
{
    /// <summary>
    /// 에이전트 이름 또는 패턴
    /// </summary>
    public string AgentPattern { get; set; } = "*";

    /// <summary>
    /// 허용된 작업 목록
    /// </summary>
    public List<AiAction> AllowedActions { get; set; } = new();

    /// <summary>
    /// 금지된 작업 목록
    /// </summary>
    public List<AiAction> DisallowedActions { get; set; } = new();

    /// <summary>
    /// 허용된 경로 패턴
    /// </summary>
    public List<string> AllowedPaths { get; set; } = new();

    /// <summary>
    /// 금지된 경로 패턴
    /// </summary>
    public List<string> DisallowedPaths { get; set; } = new();

    /// <summary>
    /// 사용량 제한
    /// </summary>
    public AiUsageLimits? UsageLimits { get; set; }

    /// <summary>
    /// 시간 제한 (특정 시간대에만 허용)
    /// </summary>
    public List<TimeWindow> TimeWindows { get; set; } = new();

    /// <summary>
    /// 추가 조건
    /// </summary>
    public Dictionary<string, object> AdditionalConditions { get; set; } = new();
}

/// <summary>
/// AI 사용량 제한
/// </summary>
public class AiUsageLimits
{
    /// <summary>
    /// 시간당 최대 요청 수
    /// </summary>
    public int? MaxRequestsPerHour { get; set; }

    /// <summary>
    /// 일일 최대 요청 수
    /// </summary>
    public int? MaxRequestsPerDay { get; set; }

    /// <summary>
    /// 최대 동시 연결 수
    /// </summary>
    public int? MaxConcurrentConnections { get; set; }

    /// <summary>
    /// 최대 데이터 전송량 (바이트)
    /// </summary>
    public long? MaxDataTransferPerDay { get; set; }

    /// <summary>
    /// 요청 간 최소 지연 시간 (밀리초)
    /// </summary>
    public int? MinDelayBetweenRequests { get; set; }

    /// <summary>
    /// 최대 세션 지속 시간 (분)
    /// </summary>
    public int? MaxSessionDurationMinutes { get; set; }

    /// <summary>
    /// 쿼터 리셋 시간대
    /// </summary>
    public string? QuotaResetTimezone { get; set; }
}

/// <summary>
/// 시간 창
/// </summary>
public class TimeWindow
{
    /// <summary>
    /// 시작 시간 (HH:MM)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// 종료 시간 (HH:MM)
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// 해당하는 요일 (null이면 모든 요일)
    /// </summary>
    public List<DayOfWeek>? DaysOfWeek { get; set; }

    /// <summary>
    /// 시간대
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// 현재 시간이 허용 시간 내인지 확인
    /// </summary>
    public bool IsCurrentTimeAllowed(DateTimeOffset? currentTime = null, TimeZoneInfo? timeZone = null)
    {
        var now = currentTime ?? DateTimeOffset.UtcNow;

        if (timeZone != null)
        {
            now = TimeZoneInfo.ConvertTime(now, timeZone);
        }

        // 요일 확인
        if (DaysOfWeek != null && !DaysOfWeek.Contains(now.DayOfWeek))
        {
            return false;
        }

        var currentTimeOfDay = now.TimeOfDay;

        // 시간 범위가 자정을 넘나드는 경우 처리
        if (StartTime <= EndTime)
        {
            return currentTimeOfDay >= StartTime && currentTimeOfDay <= EndTime;
        }
        else
        {
            return currentTimeOfDay >= StartTime || currentTimeOfDay <= EndTime;
        }
    }
}

/// <summary>
/// 콘텐츠 라이센스
/// </summary>
public class ContentLicense
{
    /// <summary>
    /// 라이센스 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 라이센스 URL
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 적용되는 콘텐츠 패턴
    /// </summary>
    public List<string> ContentPatterns { get; set; } = new();

    /// <summary>
    /// 라이센스 유형
    /// </summary>
    public LicenseType Type { get; set; }

    /// <summary>
    /// 상업적 사용 허용 여부
    /// </summary>
    public bool AllowCommercialUse { get; set; }

    /// <summary>
    /// 수정 허용 여부
    /// </summary>
    public bool AllowModification { get; set; }

    /// <summary>
    /// 재배포 허용 여부
    /// </summary>
    public bool AllowRedistribution { get; set; }

    /// <summary>
    /// 출처 표시 필요 여부
    /// </summary>
    public bool RequireAttribution { get; set; }

    /// <summary>
    /// 라이센스 조건
    /// </summary>
    public List<string> Conditions { get; set; } = new();

    /// <summary>
    /// 라이센스 제한사항
    /// </summary>
    public List<string> Limitations { get; set; } = new();
}

/// <summary>
/// 데이터 사용 정책
/// </summary>
public class DataUsagePolicy
{
    /// <summary>
    /// 정책 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 사용 유형
    /// </summary>
    public DataUsageType UsageType { get; set; }

    /// <summary>
    /// 허용 여부
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// 적용되는 콘텐츠 패턴
    /// </summary>
    public List<string> ContentPatterns { get; set; } = new();

    /// <summary>
    /// 조건 및 제약사항
    /// </summary>
    public List<string> Conditions { get; set; } = new();

    /// <summary>
    /// 데이터 보관 기간
    /// </summary>
    public TimeSpan? DataRetentionPeriod { get; set; }

    /// <summary>
    /// 익명화 필요 여부
    /// </summary>
    public bool RequireAnonymization { get; set; }

    /// <summary>
    /// 동의 필요 여부
    /// </summary>
    public bool RequireConsent { get; set; }

    /// <summary>
    /// 정책 설명
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 연락처 정보
/// </summary>
public class ContactInfo
{
    /// <summary>
    /// 이메일 주소
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 전화번호
    /// </summary>
    public string? Phone { get; set; }

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

    /// <summary>
    /// 선호하는 연락 방법
    /// </summary>
    public string? PreferredContactMethod { get; set; }

    /// <summary>
    /// 응답 시간 (시간)
    /// </summary>
    public int? ResponseTimeHours { get; set; }

    /// <summary>
    /// 언어
    /// </summary>
    public List<string> Languages { get; set; } = new();
}

/// <summary>
/// 지원되는 AI 모델
/// </summary>
public class SupportedAiModel
{
    /// <summary>
    /// 모델 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 모델 버전
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 모델 제공업체
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 지원되는 작업 유형
    /// </summary>
    public List<string> SupportedTasks { get; set; } = new();

    /// <summary>
    /// 모델 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// API 엔드포인트
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// 인증 방식
    /// </summary>
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// 사용 제한
    /// </summary>
    public AiUsageLimits? UsageLimits { get; set; }
}

/// <summary>
/// API 엔드포인트
/// </summary>
public class ApiEndpoint
{
    /// <summary>
    /// 엔드포인트 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL 경로
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 메서드
    /// </summary>
    public List<string> Methods { get; set; } = new();

    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 인증 필요 여부
    /// </summary>
    public bool RequiresAuthentication { get; set; }

    /// <summary>
    /// 사용량 제한
    /// </summary>
    public AiUsageLimits? UsageLimits { get; set; }

    /// <summary>
    /// 지원되는 콘텐츠 타입
    /// </summary>
    public List<string> SupportedContentTypes { get; set; } = new();

    /// <summary>
    /// 예시 요청
    /// </summary>
    public string? ExampleRequest { get; set; }

    /// <summary>
    /// 예시 응답
    /// </summary>
    public string? ExampleResponse { get; set; }
}

/// <summary>
/// 콘텐츠 카테고리
/// </summary>
public class ContentCategory
{
    /// <summary>
    /// 카테고리 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 경로 패턴
    /// </summary>
    public List<string> PathPatterns { get; set; } = new();

    /// <summary>
    /// 설명
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 콘텐츠 타입
    /// </summary>
    public List<string> ContentTypes { get; set; } = new();

    /// <summary>
    /// 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 우선순위
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 접근 제한
    /// </summary>
    public AccessRestriction? AccessRestriction { get; set; }
}

/// <summary>
/// AI 크롤링 가이드라인
/// </summary>
public class AiCrawlingGuidelines
{
    /// <summary>
    /// 권장 크롤링 속도 (요청/초)
    /// </summary>
    public double? RecommendedRate { get; set; }

    /// <summary>
    /// 최대 동시 연결 수
    /// </summary>
    public int? MaxConcurrentConnections { get; set; }

    /// <summary>
    /// 크롤링 허용 시간대
    /// </summary>
    public List<TimeWindow> AllowedTimeWindows { get; set; } = new();

    /// <summary>
    /// 사용자 에이전트 문자열 요구사항
    /// </summary>
    public List<string> UserAgentRequirements { get; set; } = new();

    /// <summary>
    /// 크롤링 우선순위
    /// </summary>
    public List<CrawlPriority> CrawlPriorities { get; set; } = new();

    /// <summary>
    /// 제외할 경로
    /// </summary>
    public List<string> ExcludePaths { get; set; } = new();

    /// <summary>
    /// 포함할 경로
    /// </summary>
    public List<string> IncludePaths { get; set; } = new();

    /// <summary>
    /// 로그 요구사항
    /// </summary>
    public LoggingRequirements? LoggingRequirements { get; set; }
}

/// <summary>
/// 윤리 가이드라인
/// </summary>
public class EthicsGuidelines
{
    /// <summary>
    /// 가이드라인 버전
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 윤리 원칙
    /// </summary>
    public List<EthicalPrinciple> Principles { get; set; } = new();

    /// <summary>
    /// 금지된 사용 사례
    /// </summary>
    public List<string> ProhibitedUseCases { get; set; } = new();

    /// <summary>
    /// 필수 고려사항
    /// </summary>
    public List<string> RequiredConsiderations { get; set; } = new();

    /// <summary>
    /// 편향성 완화 요구사항
    /// </summary>
    public List<string> BiasMitigationRequirements { get; set; } = new();

    /// <summary>
    /// 투명성 요구사항
    /// </summary>
    public List<string> TransparencyRequirements { get; set; } = new();
}

/// <summary>
/// 개인정보보호 정책
/// </summary>
public class PrivacyPolicy
{
    /// <summary>
    /// 정책 버전
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 수집되는 데이터 유형
    /// </summary>
    public List<DataType> CollectedDataTypes { get; set; } = new();

    /// <summary>
    /// 데이터 처리 목적
    /// </summary>
    public List<string> ProcessingPurposes { get; set; } = new();

    /// <summary>
    /// 데이터 보관 기간
    /// </summary>
    public Dictionary<string, TimeSpan> RetentionPeriods { get; set; } = new();

    /// <summary>
    /// 제3자 공유 정책
    /// </summary>
    public List<ThirdPartySharing> ThirdPartySharing { get; set; } = new();

    /// <summary>
    /// 사용자 권리
    /// </summary>
    public List<string> UserRights { get; set; } = new();

    /// <summary>
    /// 연락처
    /// </summary>
    public ContactInfo? PrivacyContact { get; set; }
}

/// <summary>
/// 보안 요구사항
/// </summary>
public class SecurityRequirements
{
    /// <summary>
    /// 필수 암호화 방식
    /// </summary>
    public List<string> RequiredEncryption { get; set; } = new();

    /// <summary>
    /// 인증 요구사항
    /// </summary>
    public List<string> AuthenticationRequirements { get; set; } = new();

    /// <summary>
    /// 접근 제어 요구사항
    /// </summary>
    public List<string> AccessControlRequirements { get; set; } = new();

    /// <summary>
    /// 감사 로그 요구사항
    /// </summary>
    public AuditLogRequirements? AuditLogRequirements { get; set; }

    /// <summary>
    /// 보안 연락처
    /// </summary>
    public ContactInfo? SecurityContact { get; set; }
}

/// <summary>
/// ai.txt 파싱 통계
/// </summary>
public class AiTxtStatistics
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
    /// ai.txt 파일이 발견된 사이트 수
    /// </summary>
    public int SitesWithAiTxt { get; set; }

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

// 열거형 정의

/// <summary>
/// AI 작업 유형
/// </summary>
public enum AiAction
{
    /// <summary>
    /// 읽기/크롤링
    /// </summary>
    Read,

    /// <summary>
    /// 인덱싱
    /// </summary>
    Index,

    /// <summary>
    /// 학습 데이터로 사용
    /// </summary>
    Training,

    /// <summary>
    /// 미세조정
    /// </summary>
    FineTuning,

    /// <summary>
    /// 추론/생성
    /// </summary>
    Inference,

    /// <summary>
    /// 분석
    /// </summary>
    Analysis,

    /// <summary>
    /// 요약
    /// </summary>
    Summarization,

    /// <summary>
    /// 번역
    /// </summary>
    Translation,

    /// <summary>
    /// 검색
    /// </summary>
    Search,

    /// <summary>
    /// 캐싱
    /// </summary>
    Caching
}

/// <summary>
/// 조직 유형
/// </summary>
public enum OrganizationType
{
    /// <summary>
    /// 개인
    /// </summary>
    Individual,

    /// <summary>
    /// 영리 기업
    /// </summary>
    Corporation,

    /// <summary>
    /// 비영리 단체
    /// </summary>
    NonProfit,

    /// <summary>
    /// 정부 기관
    /// </summary>
    Government,

    /// <summary>
    /// 교육 기관
    /// </summary>
    Educational,

    /// <summary>
    /// 연구 기관
    /// </summary>
    Research,

    /// <summary>
    /// 기타
    /// </summary>
    Other
}

/// <summary>
/// 라이센스 유형
/// </summary>
public enum LicenseType
{
    /// <summary>
    /// 독점적 저작권
    /// </summary>
    Proprietary,

    /// <summary>
    /// 크리에이티브 커먼즈
    /// </summary>
    CreativeCommons,

    /// <summary>
    /// MIT 라이센스
    /// </summary>
    MIT,

    /// <summary>
    /// GPL
    /// </summary>
    GPL,

    /// <summary>
    /// 아파치 라이센스
    /// </summary>
    Apache,

    /// <summary>
    /// BSD 라이센스
    /// </summary>
    BSD,

    /// <summary>
    /// 퍼블릭 도메인
    /// </summary>
    PublicDomain,

    /// <summary>
    /// 사용자 정의
    /// </summary>
    Custom
}

/// <summary>
/// 데이터 사용 유형
/// </summary>
public enum DataUsageType
{
    /// <summary>
    /// AI 모델 학습
    /// </summary>
    Training,

    /// <summary>
    /// 미세조정
    /// </summary>
    FineTuning,

    /// <summary>
    /// 추론/생성
    /// </summary>
    Inference,

    /// <summary>
    /// 연구 목적
    /// </summary>
    Research,

    /// <summary>
    /// 상업적 사용
    /// </summary>
    Commercial,

    /// <summary>
    /// 개인적 사용
    /// </summary>
    Personal,

    /// <summary>
    /// 분석
    /// </summary>
    Analytics,

    /// <summary>
    /// 캐싱
    /// </summary>
    Caching
}

// 추가 지원 클래스들 (간략화된 정의)

public class AccessRestriction
{
    public bool RequireAuthentication { get; set; }
    public List<string> AllowedRoles { get; set; } = new();
    public List<string> RestrictedCountries { get; set; } = new();
}

public class CrawlPriority
{
    public List<string> PathPatterns { get; set; } = new();
    public int Priority { get; set; } = 5;
    public string? Reason { get; set; }
}

public class LoggingRequirements
{
    public bool RequireAccessLogs { get; set; }
    public bool RequireUsageLogs { get; set; }
    public TimeSpan LogRetentionPeriod { get; set; }
}

public class EthicalPrinciple
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
}

public class DataType
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPersonal { get; set; }
    public bool IsSensitive { get; set; }
}

public class ThirdPartySharing
{
    public string PartyName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public List<string> DataTypes { get; set; } = new();
    public bool RequireConsent { get; set; }
}

public class AuditLogRequirements
{
    public bool RequireAccessLogs { get; set; }
    public bool RequireModificationLogs { get; set; }
    public bool RequirePermissionLogs { get; set; }
    public TimeSpan LogRetentionPeriod { get; set; }
}