namespace WebFlux.Core.Models;

/// <summary>
/// 재시도 정책 설정
/// </summary>
public class RetryPolicy
{
    /// <summary>최대 재시도 횟수</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>기본 지연 시간 (밀리초)</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>재시도 전략</summary>
    public RetryStrategy Strategy { get; init; } = RetryStrategy.ExponentialBackoff;

    /// <summary>최대 지연 시간 (밀리초)</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>지터 적용 여부 (랜덤 지연 추가)</summary>
    public bool UseJitter { get; init; } = true;

    /// <summary>재시도 조건</summary>
    public Func<Exception, bool>? ShouldRetry { get; init; }

    /// <summary>재시도 시 콜백</summary>
    public Action<Exception, int, TimeSpan>? OnRetry { get; init; }
}

/// <summary>
/// 재시도 전략
/// </summary>
public enum RetryStrategy
{
    /// <summary>고정 간격</summary>
    Fixed,
    /// <summary>선형 증가</summary>
    Linear,
    /// <summary>지수적 증가</summary>
    ExponentialBackoff
}

/// <summary>
/// 회로차단기 정책 설정
/// </summary>
public class CircuitBreakerPolicy
{
    /// <summary>회로차단기 이름</summary>
    public string Name { get; init; } = "default";

    /// <summary>실패 임계값 (연속 실패 횟수)</summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>성공 임계값 (복구를 위한 성공 횟수)</summary>
    public int SuccessThreshold { get; init; } = 3;

    /// <summary>샘플링 기간 (통계 수집 기간)</summary>
    public TimeSpan SamplingDuration { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>최소 처리량 (샘플링 기간 내 최소 요청 수)</summary>
    public int MinimumThroughput { get; init; } = 10;

    /// <summary>실패율 임계값 (0.0 - 1.0)</summary>
    public double FailureRatio { get; init; } = 0.5;

    /// <summary>열림 지속 시간</summary>
    public TimeSpan DurationOfBreak { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>회로차단기 열림 시 콜백</summary>
    public Action<Exception, CircuitBreakerState, TimeSpan>? OnBreak { get; init; }

    /// <summary>회로차단기 복구 시 콜백</summary>
    public Action? OnReset { get; init; }

    /// <summary>하프오픈 상태 시 콜백</summary>
    public Action? OnHalfOpen { get; init; }
}

/// <summary>
/// 회로차단기 상태
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>닫힘 (정상)</summary>
    Closed,
    /// <summary>열림 (차단)</summary>
    Open,
    /// <summary>하프오픈 (테스트)</summary>
    HalfOpen
}

/// <summary>
/// 시간초과 정책 설정
/// </summary>
public class TimeoutPolicy
{
    /// <summary>시간초과 기간</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>시간초과 전략</summary>
    public TimeoutStrategy Strategy { get; init; } = TimeoutStrategy.Cooperative;

    /// <summary>시간초과 시 콜백</summary>
    public Action<TimeSpan>? OnTimeout { get; init; }
}

/// <summary>
/// 시간초과 전략
/// </summary>
public enum TimeoutStrategy
{
    /// <summary>협력적 (CancellationToken 기반)</summary>
    Cooperative,
    /// <summary>비관적 (강제 종료)</summary>
    Pessimistic
}

/// <summary>
/// 벌크헤드 정책 설정
/// </summary>
public class BulkheadPolicy
{
    /// <summary>벌크헤드 이름</summary>
    public string Name { get; init; } = "default";

    /// <summary>최대 병렬 실행 수</summary>
    public int MaxParallelization { get; init; } = 10;

    /// <summary>최대 대기열 크기</summary>
    public int MaxQueuingActions { get; init; } = 20;

    /// <summary>벌크헤드 거부 시 콜백</summary>
    public Action? OnBulkheadRejected { get; init; }
}

/// <summary>
/// 복합 회복탄력성 정책
/// </summary>
public class ResiliencePolicy
{
    /// <summary>정책 이름</summary>
    public string Name { get; init; } = "default";

    /// <summary>재시도 정책</summary>
    public RetryPolicy? Retry { get; init; }

    /// <summary>회로차단기 정책</summary>
    public CircuitBreakerPolicy? CircuitBreaker { get; init; }

    /// <summary>시간초과 정책</summary>
    public TimeoutPolicy? Timeout { get; init; }

    /// <summary>벌크헤드 정책</summary>
    public BulkheadPolicy? Bulkhead { get; init; }

    /// <summary>정책 적용 순서</summary>
    public IReadOnlyList<PolicyType> ExecutionOrder { get; init; } = new[]
    {
        PolicyType.Bulkhead,
        PolicyType.CircuitBreaker,
        PolicyType.Retry,
        PolicyType.Timeout
    };
}

/// <summary>
/// HTTP 전용 회복탄력성 정책
/// </summary>
public class HttpResiliencePolicy : ResiliencePolicy
{
    /// <summary>HTTP 상태 코드별 재시도 여부</summary>
    public IReadOnlyDictionary<int, bool> RetryOnHttpStatusCodes { get; init; } =
        new Dictionary<int, bool>
        {
            { 408, true }, // Request Timeout
            { 429, true }, // Too Many Requests
            { 502, true }, // Bad Gateway
            { 503, true }, // Service Unavailable
            { 504, true }  // Gateway Timeout
        };

    /// <summary>네트워크 오류 시 재시도 여부</summary>
    public bool RetryOnNetworkFailure { get; init; } = true;

    /// <summary>HTTP 요청 시간초과</summary>
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>연결 시간초과</summary>
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// 정책 타입
/// </summary>
public enum PolicyType
{
    /// <summary>재시도</summary>
    Retry,
    /// <summary>회로차단기</summary>
    CircuitBreaker,
    /// <summary>시간초과</summary>
    Timeout,
    /// <summary>벌크헤드</summary>
    Bulkhead
}

/// <summary>
/// 회복탄력성 통계
/// </summary>
public class ResilienceStatistics
{
    /// <summary>총 실행 횟수</summary>
    public long TotalExecutions { get; init; }

    /// <summary>성공 횟수</summary>
    public long SuccessfulExecutions { get; init; }

    /// <summary>실패 횟수</summary>
    public long FailedExecutions { get; init; }

    /// <summary>재시도 횟수</summary>
    public long RetryAttempts { get; init; }

    /// <summary>회로차단기 열림 횟수</summary>
    public long CircuitBreakerOpenings { get; init; }

    /// <summary>시간초과 횟수</summary>
    public long TimeoutOccurrences { get; init; }

    /// <summary>벌크헤드 거부 횟수</summary>
    public long BulkheadRejections { get; init; }

    /// <summary>평균 실행 시간</summary>
    public TimeSpan AverageExecutionTime { get; init; }

    /// <summary>성공률 (0.0 - 1.0)</summary>
    public double SuccessRate => TotalExecutions > 0
        ? (double)SuccessfulExecutions / TotalExecutions
        : 0.0;

    /// <summary>회로차단기별 상태</summary>
    public IReadOnlyDictionary<string, CircuitBreakerInfo> CircuitBreakers { get; init; } =
        new Dictionary<string, CircuitBreakerInfo>();

    /// <summary>벌크헤드별 정보</summary>
    public IReadOnlyDictionary<string, BulkheadInfo> Bulkheads { get; init; } =
        new Dictionary<string, BulkheadInfo>();

    /// <summary>마지막 업데이트 시간</summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 회로차단기 정보
/// </summary>
public class CircuitBreakerInfo
{
    /// <summary>현재 상태</summary>
    public CircuitBreakerState State { get; init; }

    /// <summary>마지막 상태 변경 시간</summary>
    public DateTime LastStateChange { get; init; }

    /// <summary>연속 실패 횟수</summary>
    public int ConsecutiveFailures { get; init; }

    /// <summary>마지막 실패 시간</summary>
    public DateTime? LastFailureTime { get; init; }

    /// <summary>다음 시도 허용 시간</summary>
    public DateTime? NextAttemptAt { get; init; }
}

/// <summary>
/// 벌크헤드 정보
/// </summary>
public class BulkheadInfo
{
    /// <summary>현재 사용 중인 슬롯 수</summary>
    public int CurrentUsage { get; init; }

    /// <summary>최대 슬롯 수</summary>
    public int MaxSlots { get; init; }

    /// <summary>대기열 크기</summary>
    public int QueuedActions { get; init; }

    /// <summary>최대 대기열 크기</summary>
    public int MaxQueueSize { get; init; }

    /// <summary>사용률 (0.0 - 1.0)</summary>
    public double Utilization => MaxSlots > 0
        ? (double)CurrentUsage / MaxSlots
        : 0.0;

    /// <summary>대기열 사용률 (0.0 - 1.0)</summary>
    public double QueueUtilization => MaxQueueSize > 0
        ? (double)QueuedActions / MaxQueueSize
        : 0.0;
}

/// <summary>
/// 회복탄력성 이벤트
/// </summary>
public class ResilienceEvent
{
    /// <summary>이벤트 타입</summary>
    public ResilienceEventType EventType { get; init; }

    /// <summary>정책 이름</summary>
    public string PolicyName { get; init; } = string.Empty;

    /// <summary>이벤트 발생 시간</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>실행 시간</summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>예외 정보</summary>
    public Exception? Exception { get; init; }

    /// <summary>추가 메타데이터</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// 회복탄력성 이벤트 타입
/// </summary>
public enum ResilienceEventType
{
    /// <summary>성공</summary>
    Success,
    /// <summary>실패</summary>
    Failure,
    /// <summary>재시도</summary>
    Retry,
    /// <summary>회로차단기 열림</summary>
    CircuitBreakerOpened,
    /// <summary>회로차단기 닫힘</summary>
    CircuitBreakerClosed,
    /// <summary>회로차단기 하프오픈</summary>
    CircuitBreakerHalfOpened,
    /// <summary>시간초과</summary>
    Timeout,
    /// <summary>벌크헤드 거부</summary>
    BulkheadRejected
}

/// <summary>
/// 사전 정의된 회복탄력성 정책들
/// </summary>
public static class PredefinedResiliencePolicies
{
    /// <summary>
    /// HTTP 요청용 기본 정책
    /// </summary>
    public static HttpResiliencePolicy DefaultHttp => new()
    {
        Name = "DefaultHttp",
        Retry = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(500),
            Strategy = RetryStrategy.ExponentialBackoff,
            MaxDelay = TimeSpan.FromSeconds(10),
            UseJitter = true
        },
        CircuitBreaker = new CircuitBreakerPolicy
        {
            Name = "HttpCircuitBreaker",
            FailureThreshold = 5,
            SuccessThreshold = 3,
            SamplingDuration = TimeSpan.FromMinutes(1),
            MinimumThroughput = 10,
            FailureRatio = 0.5,
            DurationOfBreak = TimeSpan.FromSeconds(30)
        },
        Timeout = new TimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30),
            Strategy = TimeoutStrategy.Cooperative
        },
        HttpTimeout = TimeSpan.FromSeconds(30),
        ConnectionTimeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// 파일 I/O용 기본 정책
    /// </summary>
    public static ResiliencePolicy DefaultFileIO => new()
    {
        Name = "DefaultFileIO",
        Retry = new RetryPolicy
        {
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryStrategy.Linear,
            MaxDelay = TimeSpan.FromSeconds(5),
            UseJitter = false
        },
        Timeout = new TimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(60),
            Strategy = TimeoutStrategy.Cooperative
        },
        ExecutionOrder = new[] { PolicyType.Retry, PolicyType.Timeout }
    };

    /// <summary>
    /// 데이터베이스 접근용 정책
    /// </summary>
    public static ResiliencePolicy DefaultDatabase => new()
    {
        Name = "DefaultDatabase",
        Retry = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(200),
            Strategy = RetryStrategy.ExponentialBackoff,
            MaxDelay = TimeSpan.FromSeconds(5),
            UseJitter = true
        },
        CircuitBreaker = new CircuitBreakerPolicy
        {
            Name = "DatabaseCircuitBreaker",
            FailureThreshold = 10,
            SuccessThreshold = 5,
            SamplingDuration = TimeSpan.FromMinutes(2),
            MinimumThroughput = 20,
            FailureRatio = 0.6,
            DurationOfBreak = TimeSpan.FromMinutes(1)
        },
        Timeout = new TimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30),
            Strategy = TimeoutStrategy.Cooperative
        },
        Bulkhead = new BulkheadPolicy
        {
            Name = "DatabaseBulkhead",
            MaxParallelization = 20,
            MaxQueuingActions = 50
        }
    };

    /// <summary>
    /// 외부 API 호출용 정책
    /// </summary>
    public static HttpResiliencePolicy DefaultExternalApi => new()
    {
        Name = "DefaultExternalApi",
        Retry = new RetryPolicy
        {
            MaxRetryAttempts = 5,
            BaseDelay = TimeSpan.FromSeconds(1),
            Strategy = RetryStrategy.ExponentialBackoff,
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true
        },
        CircuitBreaker = new CircuitBreakerPolicy
        {
            Name = "ExternalApiCircuitBreaker",
            FailureThreshold = 3,
            SuccessThreshold = 2,
            SamplingDuration = TimeSpan.FromMinutes(1),
            MinimumThroughput = 5,
            FailureRatio = 0.4,
            DurationOfBreak = TimeSpan.FromMinutes(2)
        },
        Timeout = new TimeoutPolicy
        {
            Timeout = TimeSpan.FromMinutes(2),
            Strategy = TimeoutStrategy.Cooperative
        },
        Bulkhead = new BulkheadPolicy
        {
            Name = "ExternalApiBulkhead",
            MaxParallelization = 5,
            MaxQueuingActions = 10
        },
        HttpTimeout = TimeSpan.FromMinutes(2),
        ConnectionTimeout = TimeSpan.FromSeconds(30)
    };
}