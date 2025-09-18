using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 회복탄력성 서비스 인터페이스
/// Polly 기반 재시도, 회로차단기, 시간초과, 벌크헤드 패턴 제공
/// 네트워크 요청, 파일 I/O, 외부 서비스 호출의 안정성 보장
/// </summary>
public interface IResilienceService
{
    /// <summary>
    /// 재시도 정책을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="operation">실행할 작업</param>
    /// <param name="retryPolicy">재시도 정책</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryPolicy retryPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 회로차단기 정책을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="operation">실행할 작업</param>
    /// <param name="circuitBreakerPolicy">회로차단기 정책</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteWithCircuitBreakerAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CircuitBreakerPolicy circuitBreakerPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 시간초과 정책을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="operation">실행할 작업</param>
    /// <param name="timeoutPolicy">시간초과 정책</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeoutPolicy timeoutPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 벌크헤드 정책을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="operation">실행할 작업</param>
    /// <param name="bulkheadPolicy">벌크헤드 정책</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteWithBulkheadAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        BulkheadPolicy bulkheadPolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 복합 회복탄력성 정책을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="operation">실행할 작업</param>
    /// <param name="resiliencePolicy">복합 회복탄력성 정책</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ResiliencePolicy resiliencePolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HTTP 요청에 최적화된 회복탄력성 정책을 적용하여 작업을 실행합니다
    /// </summary>
    /// <typeparam name="T">반환 타입</typeparam>
    /// <param name="httpOperation">HTTP 작업</param>
    /// <param name="httpResiliencePolicy">HTTP 회복탄력성 정책</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>작업 결과</returns>
    Task<T> ExecuteHttpWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> httpOperation,
        HttpResiliencePolicy httpResiliencePolicy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 현재 회복탄력성 통계를 가져옵니다
    /// </summary>
    /// <returns>회복탄력성 통계</returns>
    ResilienceStatistics GetStatistics();

    /// <summary>
    /// 회로차단기 상태를 가져옵니다
    /// </summary>
    /// <param name="circuitBreakerName">회로차단기 이름</param>
    /// <returns>회로차단기 상태</returns>
    CircuitBreakerState GetCircuitBreakerState(string circuitBreakerName);

    /// <summary>
    /// 회로차단기를 수동으로 열기/닫기 합니다
    /// </summary>
    /// <param name="circuitBreakerName">회로차단기 이름</param>
    /// <param name="open">열기 여부</param>
    Task SetCircuitBreakerStateAsync(string circuitBreakerName, bool open);

    /// <summary>
    /// 벌크헤드 사용률을 가져옵니다
    /// </summary>
    /// <param name="bulkheadName">벌크헤드 이름</param>
    /// <returns>사용률 (0.0 - 1.0)</returns>
    double GetBulkheadUtilization(string bulkheadName);
}