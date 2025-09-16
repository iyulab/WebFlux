namespace WebFlux.Core.Interfaces;

/// <summary>
/// HTTP 클라이언트 서비스 인터페이스
/// WebFlux에 최적화된 HTTP 요청 처리
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// GET 요청을 수행합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> GetAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET 요청을 수행하고 문자열로 반환합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 내용</returns>
    Task<string> GetStringAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET 요청을 수행하고 바이트 배열로 반환합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 바이트</returns>
    Task<byte[]> GetBytesAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HEAD 요청을 수행합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> HeadAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 사용자 에이전트를 설정합니다.
    /// </summary>
    /// <param name="userAgent">사용자 에이전트 문자열</param>
    void SetUserAgent(string userAgent);

    /// <summary>
    /// 타임아웃을 설정합니다.
    /// </summary>
    /// <param name="timeout">타임아웃 시간</param>
    void SetTimeout(TimeSpan timeout);

    /// <summary>
    /// 기본 헤더를 설정합니다.
    /// </summary>
    /// <param name="headers">설정할 헤더들</param>
    void SetDefaultHeaders(IDictionary<string, string> headers);
}

/// <summary>
/// 캐시 서비스 인터페이스
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 캐시에서 값을 가져옵니다.
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="key">캐시 키</param>
    /// <returns>캐시된 값</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// 캐시에 값을 설정합니다.
    /// </summary>
    /// <typeparam name="T">값 타입</typeparam>
    /// <param name="key">캐시 키</param>
    /// <param name="value">캐시할 값</param>
    /// <param name="expiration">만료 시간</param>
    /// <returns>설정 작업</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// 캐시에서 값을 제거합니다.
    /// </summary>
    /// <param name="key">캐시 키</param>
    /// <returns>제거 작업</returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// 캐시 키 존재 여부를 확인합니다.
    /// </summary>
    /// <param name="key">캐시 키</param>
    /// <returns>존재 여부</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 캐시를 초기화합니다.
    /// </summary>
    /// <returns>초기화 작업</returns>
    Task ClearAsync();
}

/// <summary>
/// 성능 모니터 인터페이스
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// 성능 측정을 시작합니다.
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <returns>성능 측정 컨텍스트</returns>
    IDisposable StartMeasurement(string operationName);

    /// <summary>
    /// 메트릭을 기록합니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름</param>
    /// <param name="value">메트릭 값</param>
    /// <param name="tags">추가 태그</param>
    void RecordMetric(string metricName, double value, IDictionary<string, string>? tags = null);

    /// <summary>
    /// 카운터를 증가시킵니다.
    /// </summary>
    /// <param name="counterName">카운터 이름</param>
    /// <param name="increment">증가값</param>
    /// <param name="tags">추가 태그</param>
    void IncrementCounter(string counterName, long increment = 1, IDictionary<string, string>? tags = null);

    /// <summary>
    /// 성능 통계를 반환합니다.
    /// </summary>
    /// <returns>성능 통계</returns>
    PerformanceStatistics GetStatistics();
}

/// <summary>
/// 성능 통계
/// </summary>
public class PerformanceStatistics
{
    /// <summary>총 측정된 작업 수</summary>
    public long TotalOperations { get; init; }

    /// <summary>작업별 평균 실행 시간</summary>
    public IReadOnlyDictionary<string, double> AverageExecutionTimes { get; init; } =
        new Dictionary<string, double>();

    /// <summary>메트릭 통계</summary>
    public IReadOnlyDictionary<string, MetricStatistics> Metrics { get; init; } =
        new Dictionary<string, MetricStatistics>();

    /// <summary>카운터 값</summary>
    public IReadOnlyDictionary<string, long> Counters { get; init; } =
        new Dictionary<string, long>();

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 메트릭 통계
/// </summary>
public class MetricStatistics
{
    /// <summary>총 샘플 수</summary>
    public long Count { get; init; }

    /// <summary>평균값</summary>
    public double Average { get; init; }

    /// <summary>최솟값</summary>
    public double Min { get; init; }

    /// <summary>최댓값</summary>
    public double Max { get; init; }

    /// <summary>표준편차</summary>
    public double StandardDeviation { get; init; }
}

/// <summary>
/// 메트릭 수집기 인터페이스
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// 메트릭을 수집합니다.
    /// </summary>
    /// <param name="context">수집 컨텍스트</param>
    /// <returns>수집 작업</returns>
    Task CollectAsync(string context);

    /// <summary>
    /// 수집된 메트릭을 반환합니다.
    /// </summary>
    /// <returns>메트릭 데이터</returns>
    Task<IReadOnlyDictionary<string, object>> GetMetricsAsync();
}