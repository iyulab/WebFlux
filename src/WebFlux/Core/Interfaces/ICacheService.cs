using System.Text.Json;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 캐시 서비스 인터페이스
/// 분산 캐시 및 고성능 캐싱 전략 지원
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 캐시에서 값 조회
    /// </summary>
    /// <typeparam name="T">조회할 타입</typeparam>
    /// <param name="key">캐시 키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>캐시된 값, 없으면 null</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 캐시에 값 저장
    /// </summary>
    /// <typeparam name="T">저장할 타입</typeparam>
    /// <param name="key">캐시 키</param>
    /// <param name="value">저장할 값</param>
    /// <param name="expiration">만료 시간</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 캐시에서 값 제거
    /// </summary>
    /// <param name="key">캐시 키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 패턴으로 캐시 키 제거
    /// </summary>
    /// <param name="pattern">키 패턴 (예: "user:*")</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// 캐시 키 존재 여부 확인
    /// </summary>
    /// <param name="key">캐시 키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>존재하면 true</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 캐시 키의 만료 시간 설정
    /// </summary>
    /// <param name="key">캐시 키</param>
    /// <param name="expiration">만료 시간</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 캐시 키의 남은 만료 시간 조회
    /// </summary>
    /// <param name="key">캐시 키</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>남은 만료 시간, 만료되지 않으면 null</returns>
    Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 트랜잭션 지원을 위한 배치 연산
    /// </summary>
    /// <param name="operations">배치 연산 목록</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task ExecuteBatchAsync(IEnumerable<CacheOperation> operations, CancellationToken cancellationToken = default);

    /// <summary>
    /// 캐시 통계 정보 조회
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>캐시 통계</returns>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 캐시 연산 정의
/// </summary>
public class CacheOperation
{
    /// <summary>연산 타입</summary>
    public CacheOperationType Type { get; set; }

    /// <summary>캐시 키</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>값 (Set 연산 시 사용)</summary>
    public object? Value { get; set; }

    /// <summary>만료 시간 (Set 연산 시 사용)</summary>
    public TimeSpan? Expiration { get; set; }
}

/// <summary>
/// 캐시 연산 타입
/// </summary>
public enum CacheOperationType
{
    /// <summary>값 설정</summary>
    Set,
    /// <summary>값 제거</summary>
    Remove,
    /// <summary>만료 시간 설정</summary>
    Expire
}

/// <summary>
/// 캐시 통계 정보
/// </summary>
public class CacheStatistics
{
    /// <summary>총 요청 수</summary>
    public long TotalRequests { get; set; }

    /// <summary>총 히트 수</summary>
    public long TotalHits { get; set; }

    /// <summary>총 미스 수</summary>
    public long TotalMisses { get; set; }

    /// <summary>메모리 히트 수</summary>
    public long MemoryHits { get; set; }

    /// <summary>분산 캐시 히트 수</summary>
    public long DistributedHits { get; set; }

    /// <summary>제거된 항목 수</summary>
    public long Evictions { get; set; }

    /// <summary>현재 엔트리 수</summary>
    public long CurrentEntryCount { get; set; }

    /// <summary>총 키 수</summary>
    public long TotalKeys { get; set; }

    /// <summary>히트 수</summary>
    public long Hits { get; set; }

    /// <summary>미스 수</summary>
    public long Misses { get; set; }

    /// <summary>전체 히트율</summary>
    public double HitRate => TotalRequests > 0 ? (double)TotalHits / TotalRequests : 0;

    /// <summary>메모리 히트율</summary>
    public double MemoryHitRate => TotalRequests > 0 ? (double)MemoryHits / TotalRequests : 0;

    /// <summary>분산 캐시 히트율</summary>
    public double DistributedHitRate => TotalRequests > 0 ? (double)DistributedHits / TotalRequests : 0;

    /// <summary>히트율</summary>
    public double HitRatio => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;

    /// <summary>메모리 사용량 (바이트)</summary>
    public long MemoryUsage { get; set; }

    /// <summary>만료된 키 수</summary>
    public long ExpiredKeys { get; set; }

    /// <summary>제거된 키 수</summary>
    public long EvictedKeys { get; set; }

    /// <summary>키 통계</summary>
    public Dictionary<string, object> KeyStatistics { get; set; } = new();

    /// <summary>마지막 업데이트 시간</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}