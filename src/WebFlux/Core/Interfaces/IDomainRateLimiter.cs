namespace WebFlux.Core.Interfaces;

/// <summary>
/// 도메인별 Rate Limiting 인터페이스
/// 웹사이트별 요청 속도 제한으로 차단 방지 및 예절 있는 크롤링 지원
/// </summary>
public interface IDomainRateLimiter
{
    /// <summary>
    /// Rate Limiting을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="domain">대상 도메인</param>
    /// <param name="operation">실행할 작업</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteAsync<T>(
        string domain,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rate Limiting을 적용하여 작업을 실행합니다 (반환값 없음)
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    /// <param name="operation">실행할 작업</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task ExecuteAsync(
        string domain,
        Func<Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rate Limiting을 적용하여 작업을 실행하되, 이 호출이 요구하는 최소 간격을 쓴다.
    /// </summary>
    /// <remarks>
    /// <paramref name="minimumInterval"/> 은 limiter 의 기본 간격을 대신한다. 다만 <see cref="SetDomainLimit"/> 나 robots.txt
    /// <c>Crawl-delay</c> 로 그 도메인에 명시된 한도보다 짧아지지는 않는다 — 그것은 기본값이 아니라 사이트의 규칙이다.
    /// </remarks>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="domain">대상 도메인</param>
    /// <param name="minimumInterval">같은 도메인 요청 간 최소 간격(0 이상)</param>
    /// <param name="operation">실행할 작업</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteAsync<T>(
        string domain,
        TimeSpan minimumInterval,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 도메인별 요청 간격을 설정합니다
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    /// <param name="minimumInterval">최소 요청 간격</param>
    void SetDomainLimit(string domain, TimeSpan minimumInterval);

    /// <summary>
    /// robots.txt에서 crawl-delay를 읽어 자동 설정합니다
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task ConfigureFromRobotsTxtAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// 도메인의 현재 Rate Limit 설정을 가져옵니다
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    /// <returns>현재 설정된 최소 간격</returns>
    TimeSpan GetDomainLimit(string domain);

    /// <summary>
    /// 도메인의 마지막 요청 시간을 가져옵니다
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    /// <returns>마지막 요청 시간, 없으면 null</returns>
    DateTimeOffset? GetLastRequestTime(string domain);

    /// <summary>
    /// 다음 요청까지 대기해야 하는 시간을 계산합니다
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    /// <returns>대기 시간, 즉시 요청 가능하면 TimeSpan.Zero</returns>
    TimeSpan GetWaitTime(string domain);

    /// <summary>
    /// Rate Limiter 통계를 가져옵니다
    /// </summary>
    /// <returns>통계 정보</returns>
    DomainRateLimiterStatistics GetStatistics();

    /// <summary>
    /// 특정 도메인의 Rate Limit 설정을 제거합니다
    /// </summary>
    /// <param name="domain">대상 도메인</param>
    void RemoveDomainLimit(string domain);

    /// <summary>
    /// 모든 Rate Limit 설정을 초기화합니다
    /// </summary>
    void Reset();
}

/// <summary>
/// Rate Limiter 통계
/// </summary>
public class DomainRateLimiterStatistics
{
    /// <summary>
    /// 등록된 도메인 수
    /// </summary>
    public int RegisteredDomains { get; init; }

    /// <summary>
    /// 총 요청 수
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// 총 대기 시간 (밀리초)
    /// </summary>
    public long TotalWaitTimeMs { get; init; }

    /// <summary>
    /// 평균 대기 시간 (밀리초)
    /// </summary>
    public double AverageWaitTimeMs => TotalRequests > 0 ? (double)TotalWaitTimeMs / TotalRequests : 0;

    /// <summary>
    /// 도메인별 요청 수
    /// </summary>
    public IReadOnlyDictionary<string, long> RequestsByDomain { get; init; } =
        new Dictionary<string, long>();

    /// <summary>
    /// 도메인별 대기 시간 (밀리초)
    /// </summary>
    public IReadOnlyDictionary<string, long> WaitTimeByDomain { get; init; } =
        new Dictionary<string, long>();

    /// <summary>
    /// 마지막 업데이트 시간
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
