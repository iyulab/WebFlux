using WebFlux.Core.Options;

namespace WebFlux.Core.Models;

/// <summary>
/// WebFlux SDK 전체 구성 클래스
/// </summary>
public class WebFluxConfiguration
{
    /// <summary>
    /// 크롤링 기본 설정
    /// </summary>
    public CrawlingConfiguration Crawling { get; set; } = new();

    /// <summary>
    /// 청킹 기본 설정
    /// </summary>
    public ChunkingConfiguration Chunking { get; set; } = new();

    /// <summary>
    /// 콘텐츠 추출 설정
    /// </summary>
    public ExtractionConfiguration Extraction { get; set; } = new();

    /// <summary>
    /// 성능 및 리소스 설정
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// 로깅 설정
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// 캐싱 설정
    /// </summary>
    public CachingConfiguration Caching { get; set; } = new();

    /// <summary>
    /// 보안 설정
    /// </summary>
    public SecurityConfiguration Security { get; set; } = new();

    /// <summary>
    /// 이벤트 설정
    /// </summary>
    public EventConfiguration Events { get; set; } = new();

    /// <summary>
    /// 처리 최적화 설정 (Phase 5C.2)
    /// </summary>
    public ProcessingOptimizationConfiguration ProcessingOptimization { get; set; } = new();

    /// <summary>
    /// 토큰 카운팅 설정 (Phase 5C.2)
    /// </summary>
    public TokenCountingConfiguration TokenCounting { get; set; } = new();

    /// <summary>
    /// 기본 토크나이저 모델 (Phase 5C.2)
    /// </summary>
    public TokenizerModel? DefaultTokenizerModel { get; set; }

    /// <summary>
    /// AI 증강 설정 (Phase 1)
    /// </summary>
    public AiEnhancementConfiguration AiEnhancement { get; set; } = new();

    /// <summary>
    /// 사용자 정의 설정
    /// </summary>
    public IDictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 환경별 설정 오버라이드
    /// </summary>
    public IDictionary<string, object> EnvironmentOverrides { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// 크롤링 구성
/// </summary>
public class CrawlingConfiguration
{
    /// <summary>
    /// 시작 URL 목록
    /// </summary>
    public List<string> StartUrls { get; set; } = new();

    /// <summary>
    /// 크롤링 전략 (BreadthFirst, DepthFirst, Sitemap)
    /// </summary>
    public string Strategy { get; set; } = "BreadthFirst";

    /// <summary>
    /// 기본 User-Agent
    /// </summary>
    public string DefaultUserAgent { get; set; } = "WebFlux/1.0 (+https://github.com/webflux/webflux)";

    /// <summary>
    /// 기본 요청 타임아웃 (초)
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 기본 요청 간 지연 (밀리초)
    /// </summary>
    public int DefaultDelayMs { get; set; } = 1000;

    /// <summary>
    /// 최대 동시 요청 수
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// 기본 재시도 횟수
    /// </summary>
    public int DefaultRetryCount { get; set; } = 3;

    /// <summary>
    /// robots.txt 준수 여부
    /// </summary>
    public bool RespectRobotsTxt { get; set; } = true;

    /// <summary>
    /// 기본 허용 콘텐츠 타입
    /// </summary>
    public ISet<string> DefaultAllowedContentTypes { get; set; } = new HashSet<string>
    {
        "text/html", "application/xhtml+xml", "text/plain", "application/json"
    };

    /// <summary>
    /// 기본 제외 확장자
    /// </summary>
    public ISet<string> DefaultExcludedExtensions { get; set; } = new HashSet<string>
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".zip"
    };

    /// <summary>
    /// 사용자 정의 헤더
    /// </summary>
    public IDictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// 청킹 구성
/// </summary>
public class ChunkingConfiguration
{
    /// <summary>
    /// 기본 청킹 전략
    /// </summary>
    public string DefaultStrategy { get; set; } = "Auto";

    /// <summary>
    /// 기본 최대 청크 크기 (토큰)
    /// </summary>
    public int DefaultMaxChunkSize { get; set; } = 512;

    /// <summary>
    /// 기본 청크 겹침 크기 (토큰)
    /// </summary>
    public int DefaultChunkOverlap { get; set; } = 50;

    /// <summary>
    /// 기본 최소 청크 크기 (토큰)
    /// </summary>
    public int DefaultMinChunkSize { get; set; } = 50;

    /// <summary>
    /// 기본 품질 임계값
    /// </summary>
    public double DefaultQualityThreshold { get; set; } = 0.6;

    /// <summary>
    /// 기본 의미론적 임계값
    /// </summary>
    public double DefaultSemanticThreshold { get; set; } = 0.7;

    /// <summary>
    /// 전략별 기본 설정
    /// </summary>
    public IDictionary<string, object> StrategyDefaults { get; set; } = new Dictionary<string, object>();

    // BaseChunkingStrategy에서 사용되는 속성들 추가
    /// <summary>
    /// 최소 청크 크기
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// 최대 청크 크기
    /// </summary>
    public int MaxChunkSize { get; set; } = 2000;

    /// <summary>
    /// 겹침 크기
    /// </summary>
    public int OverlapSize { get; set; } = 200;

    /// <summary>
    /// 공백 정규화 여부
    /// </summary>
    public bool NormalizeWhitespace { get; set; } = true;

    /// <summary>
    /// 멀티모달 처리 활성화 여부 (Phase 5A.3)
    /// </summary>
    public bool EnableMultimodalProcessing { get; set; } = false;

    /// <summary>
    /// 멀티모달 처리 옵션
    /// </summary>
    public MultimodalProcessingOptions? MultimodalOptions { get; set; }

    /// <summary>
    /// 언어별 설정
    /// </summary>
    public IDictionary<string, LanguageChunkingSettings> LanguageSettings { get; set; } =
        new Dictionary<string, LanguageChunkingSettings>();
}

/// <summary>
/// 언어별 청킹 설정
/// </summary>
public class LanguageChunkingSettings
{
    /// <summary>
    /// 권장 청킹 전략
    /// </summary>
    public string? PreferredStrategy { get; set; }

    /// <summary>
    /// 언어별 구분자
    /// </summary>
    public IList<string> CustomSeparators { get; set; } = new List<string>();

    /// <summary>
    /// 토큰화 설정
    /// </summary>
    public IDictionary<string, object> TokenizationSettings { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// 성능 구성
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// 최대 병렬 처리 수
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 메모리 사용 한계 (바이트)
    /// </summary>
    public long? MaxMemoryUsageBytes { get; set; }

    /// <summary>
    /// 메모리 최적화 모드 활성화 임계값 (0.0-1.0)
    /// </summary>
    public double MemoryOptimizationThreshold { get; set; } = 0.8;

    /// <summary>
    /// 배치 처리 크기
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// 큐 크기 제한
    /// </summary>
    public int? QueueSizeLimit { get; set; } = 1000;

    /// <summary>
    /// 백프레셔 임계값
    /// </summary>
    public double BackpressureThreshold { get; set; } = 0.9;

    /// <summary>
    /// 성능 모니터링 간격 (밀리초)
    /// </summary>
    public int PerformanceMonitoringIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 자동 스케일링 활성화
    /// </summary>
    public bool EnableAutoScaling { get; set; } = true;
}

/// <summary>
/// 로깅 구성
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// 최소 로그 레벨
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// 로그 이벤트 활성화 여부
    /// </summary>
    public bool EnableEvents { get; set; } = true;

    /// <summary>
    /// 성능 로깅 활성화
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    /// 상세 오류 로깅 활성화
    /// </summary>
    public bool EnableDetailedErrorLogging { get; set; } = true;

    /// <summary>
    /// 구조화된 로깅 활성화
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// 카테고리별 로그 레벨
    /// </summary>
    public IDictionary<string, string> CategoryLevels { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// 로그 필터
    /// </summary>
    public IList<string> LogFilters { get; set; } = new List<string>();
}

/// <summary>
/// 캐싱 구성
/// </summary>
public class CachingConfiguration
{
    /// <summary>
    /// 캐싱 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 기본 캐시 만료 시간 (분)
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 최대 캐시 크기 (엔트리 수)
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// 캐시 압축 활성화
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// 캐시 지표 수집 활성화
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// 캐시 유형별 설정
    /// </summary>
    public IDictionary<string, CacheTypeSettings> TypeSettings { get; set; } =
        new Dictionary<string, CacheTypeSettings>();
}

/// <summary>
/// 캐시 유형별 설정
/// </summary>
public class CacheTypeSettings
{
    /// <summary>
    /// 만료 시간 (분)
    /// </summary>
    public int ExpirationMinutes { get; set; }

    /// <summary>
    /// 최대 크기 (엔트리 수)
    /// </summary>
    public int MaxSize { get; set; }

    /// <summary>
    /// 압축 활성화
    /// </summary>
    public bool EnableCompression { get; set; }
}

/// <summary>
/// 보안 구성
/// </summary>
public class SecurityConfiguration
{
    /// <summary>
    /// API 키 암호화 활성화
    /// </summary>
    public bool EncryptApiKeys { get; set; } = true;

    /// <summary>
    /// SSL 인증서 검증 활성화
    /// </summary>
    public bool ValidateSslCertificates { get; set; } = true;

    /// <summary>
    /// 허용된 도메인 목록
    /// </summary>
    public ISet<string> AllowedDomains { get; set; } = new HashSet<string>();

    /// <summary>
    /// 차단된 도메인 목록
    /// </summary>
    public ISet<string> BlockedDomains { get; set; } = new HashSet<string>();

    /// <summary>
    /// 요청 속도 제한 (요청/분)
    /// </summary>
    public int? RateLimitPerMinute { get; set; }

    /// <summary>
    /// 사용자 에이전트 검증
    /// </summary>
    public bool ValidateUserAgent { get; set; } = false;

    /// <summary>
    /// 콘텐츠 스캔 활성화
    /// </summary>
    public bool EnableContentScanning { get; set; } = false;
}

/// <summary>
/// 이벤트 구성
/// </summary>
public class EventConfiguration
{
    /// <summary>
    /// 이벤트 발행 활성화
    /// </summary>
    public bool EnableEventPublishing { get; set; } = true;

    /// <summary>
    /// 이벤트 버퍼 크기
    /// </summary>
    public int EventBufferSize { get; set; } = 1000;

    /// <summary>
    /// 이벤트 배치 크기
    /// </summary>
    public int EventBatchSize { get; set; } = 10;

    /// <summary>
    /// 이벤트 플러시 간격 (밀리초)
    /// </summary>
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 이벤트 유형별 활성화 설정
    /// </summary>
    public IDictionary<string, bool> EventTypeEnabled { get; set; } = new Dictionary<string, bool>
    {
        ["CrawlingStarted"] = true,
        ["CrawlingCompleted"] = true,
        ["PageCrawled"] = false,  // 너무 빈번할 수 있음
        ["ChunkingStarted"] = true,
        ["ChunkingCompleted"] = true,
        ["ChunkGenerated"] = false,  // 너무 빈번할 수 있음
        ["ErrorOccurred"] = true,
        ["PerformanceMetrics"] = true
    };

    /// <summary>
    /// 이벤트 필터링 규칙
    /// </summary>
    public IList<EventFilter> EventFilters { get; set; } = new List<EventFilter>();
}

/// <summary>
/// 이벤트 필터
/// </summary>
public class EventFilter
{
    /// <summary>
    /// 필터 이름
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 이벤트 타입 패턴
    /// </summary>
    public string? EventTypePattern { get; set; }

    /// <summary>
    /// 최소 심각도
    /// </summary>
    public EventSeverity? MinSeverity { get; set; }

    /// <summary>
    /// 속성 조건
    /// </summary>
    public IDictionary<string, object> PropertyConditions { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// 포함 여부 (기본: true = 포함, false = 제외)
    /// </summary>
    public bool Include { get; set; } = true;
}

/// <summary>
/// 처리 최적화 구성 (Phase 5C.2)
/// </summary>
public class ProcessingOptimizationConfiguration
{
    /// <summary>
    /// 최적화 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 자동 전략 선택 활성화
    /// </summary>
    public bool EnableAutoStrategySelection { get; set; } = true;

    /// <summary>
    /// 성능 모니터링 간격 (초)
    /// </summary>
    public int PerformanceMonitoringInterval { get; set; } = 60;

    /// <summary>
    /// 리소스 사용량 임계값
    /// </summary>
    public ResourceThresholds ResourceThresholds { get; set; } = new();

    /// <summary>
    /// 캐시 최적화 설정
    /// </summary>
    public CacheOptimizationSettings CacheOptimization { get; set; } = new();

    /// <summary>
    /// 병목 감지 활성화
    /// </summary>
    public bool EnableBottleneckDetection { get; set; } = true;

    /// <summary>
    /// 토큰 최적화 활성화
    /// </summary>
    public bool EnableTokenOptimization { get; set; } = true;

    /// <summary>
    /// 최적화 통계 수집 활성화
    /// </summary>
    public bool EnableStatisticsCollection { get; set; } = true;
}

/// <summary>
/// 리소스 임계값 설정
/// </summary>
public class ResourceThresholds
{
    /// <summary>
    /// CPU 사용률 경고 임계값 (0.0-1.0)
    /// </summary>
    public double CpuWarningThreshold { get; set; } = 0.7;

    /// <summary>
    /// CPU 사용률 위험 임계값 (0.0-1.0)
    /// </summary>
    public double CpuCriticalThreshold { get; set; } = 0.9;

    /// <summary>
    /// 메모리 사용률 경고 임계값 (0.0-1.0)
    /// </summary>
    public double MemoryWarningThreshold { get; set; } = 0.75;

    /// <summary>
    /// 메모리 사용률 위험 임계값 (0.0-1.0)
    /// </summary>
    public double MemoryCriticalThreshold { get; set; } = 0.9;

    /// <summary>
    /// 응답 시간 경고 임계값 (밀리초)
    /// </summary>
    public int ResponseTimeWarningMs { get; set; } = 5000;

    /// <summary>
    /// 응답 시간 위험 임계값 (밀리초)
    /// </summary>
    public int ResponseTimeCriticalMs { get; set; } = 10000;
}

/// <summary>
/// 캐시 최적화 설정
/// </summary>
public class CacheOptimizationSettings
{
    /// <summary>
    /// 지능형 TTL 계산 활성화
    /// </summary>
    public bool EnableIntelligentTTL { get; set; } = true;

    /// <summary>
    /// 캐시 키 최적화 활성화
    /// </summary>
    public bool EnableKeyOptimization { get; set; } = true;

    /// <summary>
    /// 압축 활성화 임계값 (바이트)
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;

    /// <summary>
    /// 캐시 예열 활성화
    /// </summary>
    public bool EnableWarmup { get; set; } = false;

    /// <summary>
    /// 최대 캐시 엔트리 수
    /// </summary>
    public int MaxCacheEntries { get; set; } = 10000;
}

/// <summary>
/// 토큰 카운팅 구성 (Phase 5C.2)
/// </summary>
public class TokenCountingConfiguration
{
    /// <summary>
    /// 토큰 카운팅 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 캐시 활성화 여부
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// 캐시 최대 크기
    /// </summary>
    public int CacheMaxSize { get; set; } = 10000;

    /// <summary>
    /// 지원되는 토크나이저 모델
    /// </summary>
    public ISet<string> SupportedModels { get; set; } = new HashSet<string>
    {
        "GPT3", "GPT4", "GPT4Turbo", "Claude3", "Claude3Opus", "Llama2", "Llama3", "Generic"
    };

    /// <summary>
    /// 모델별 비용 정보 (토큰당 USD)
    /// </summary>
    public IDictionary<string, double> ModelCosts { get; set; } = new Dictionary<string, double>
    {
        ["GPT4"] = 0.00003,
        ["GPT4Turbo"] = 0.00001,
        ["GPT3"] = 0.000002,
        ["Claude3Opus"] = 0.000015,
        ["Claude3"] = 0.000003,
        ["Llama3"] = 0.000001,
        ["Llama2"] = 0.0000005,
        ["Generic"] = 0.000001
    };

    /// <summary>
    /// 토큰 분석 활성화
    /// </summary>
    public bool EnableTokenAnalysis { get; set; } = true;

    /// <summary>
    /// 토큰 통계 수집 활성화
    /// </summary>
    public bool EnableStatistics { get; set; } = true;
}

/// <summary>
/// 토크나이저 모델 열거형 (Phase 5C.2에서 이동)
/// </summary>
public enum TokenizerModel
{
    /// <summary>GPT-3.5</summary>
    GPT3,
    /// <summary>GPT-4</summary>
    GPT4,
    /// <summary>GPT-4 Turbo</summary>
    GPT4Turbo,
    /// <summary>Claude 3 Haiku</summary>
    Claude3,
    /// <summary>Claude 3 Opus</summary>
    Claude3Opus,
    /// <summary>Llama 2</summary>
    Llama2,
    /// <summary>Llama 3</summary>
    Llama3,
    /// <summary>범용 토크나이저</summary>
    Generic
}

/// <summary>
/// AI 증강 구성 (Phase 1)
/// </summary>
public class AiEnhancementConfiguration
{
    /// <summary>
    /// AI 증강 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 요약 생성 활성화
    /// </summary>
    public bool EnableSummary { get; set; } = true;

    /// <summary>
    /// 메타데이터 추출 활성화
    /// </summary>
    public bool EnableMetadata { get; set; } = true;

    /// <summary>
    /// 재작성 활성화 (기본값: false)
    /// </summary>
    public bool EnableRewrite { get; set; } = false;

    /// <summary>
    /// 병렬 처리 활성화
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// AI 처리 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMs { get; set; } = 60000;

    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}