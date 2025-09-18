using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 고성능 하이브리드 캐시 서비스 구현
/// 메모리 캐시 + 분산 캐시 조합으로 최적의 성능 제공
/// 지능형 캐시 전략 및 통계 수집 기능 포함
/// </summary>
public class CacheService : ICacheService
{
    private readonly ILogger<CacheService> _logger;
    private readonly WebFluxConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;

    // 캐시 통계 및 메트릭
    private readonly ConcurrentDictionary<string, CacheEntryInfo> _cacheEntries;
    private readonly ConcurrentDictionary<string, CacheKeyStatistics> _keyStatistics;
    private readonly Timer _cleanupTimer;
    private readonly Timer _statisticsTimer;

    // 성능 카운터
    private long _totalRequests;
    private long _memoryHits;
    private long _distributedHits;
    private long _misses;
    private long _evictions;

    private readonly object _statsLock = new();
    private volatile bool _disposed;

    public CacheService(
        ILogger<CacheService> logger,
        IOptions<WebFluxConfiguration> configuration,
        IMemoryCache memoryCache,
        IDistributedCache? distributedCache = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _distributedCache = distributedCache;

        _cacheEntries = new ConcurrentDictionary<string, CacheEntryInfo>();
        _keyStatistics = new ConcurrentDictionary<string, CacheKeyStatistics>();

        // 주기적 정리 (5분 간격)
        _cleanupTimer = new Timer(PerformCleanup, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        // 통계 수집 (1분 간격)
        _statisticsTimer = new Timer(CollectStatistics, null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        _logger.LogInformation("CacheService initialized with memory cache{DistributedCache}",
            _distributedCache != null ? " and distributed cache" : " only");
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_disposed) return null;

        Interlocked.Increment(ref _totalRequests);

        try
        {
            // 1단계: 메모리 캐시에서 확인
            if (_memoryCache.TryGetValue(key, out var memoryCachedValue))
            {
                Interlocked.Increment(ref _memoryHits);
                UpdateKeyStatistics(key, CacheHitType.L1Hit);

                _logger.LogTrace("Cache hit (memory): {Key}", key);
                return (T?)memoryCachedValue;
            }

            // 2단계: 분산 캐시에서 확인 (사용 가능한 경우)
            if (_distributedCache != null)
            {
                var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    Interlocked.Increment(ref _distributedHits);
                    UpdateKeyStatistics(key, CacheHitType.L2Hit);

                    try
                    {
                        var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue);

                        // 메모리 캐시에도 저장 (L1 캐시로 승격)
                        var memoryOptions = CreateMemoryCacheOptions(key);
                        _memoryCache.Set(key, deserializedValue, memoryOptions);

                        _logger.LogTrace("Cache hit (distributed): {Key}", key);
                        return deserializedValue;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cached value for key: {Key}", key);
                        await _distributedCache.RemoveAsync(key, cancellationToken);
                    }
                }
            }

            // 3단계: 캐시 미스
            Interlocked.Increment(ref _misses);
            UpdateKeyStatistics(key, CacheHitType.Miss);

            _logger.LogTrace("Cache miss: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        if (_disposed) return;

        try
        {
            var effectiveExpiration = expiration ?? GetDefaultExpiration(key);

            // 메모리 캐시에 저장
            var memoryOptions = CreateMemoryCacheOptions(key, effectiveExpiration);
            _memoryCache.Set(key, value, memoryOptions);

            // 분산 캐시에 저장 (사용 가능한 경우)
            if (_distributedCache != null)
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = effectiveExpiration
                };

                await _distributedCache.SetStringAsync(key, serializedValue, distributedOptions, cancellationToken);
            }

            // 캐시 엔트리 정보 저장
            UpdateCacheEntryInfo(key, value, effectiveExpiration);

            _logger.LogTrace("Cached value: {Key} (expires in {Expiration})", key, effectiveExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_disposed) return;

        try
        {
            // 메모리 캐시에서 제거
            _memoryCache.Remove(key);

            // 분산 캐시에서 제거
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }

            // 엔트리 정보 제거
            _cacheEntries.TryRemove(key, out _);

            _logger.LogTrace("Removed cached value: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        if (_disposed) return;

        try
        {
            var matchingKeys = _cacheEntries.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            var removeTasks = matchingKeys.Select(key => RemoveAsync(key, cancellationToken));
            await Task.WhenAll(removeTasks);

            _logger.LogDebug("Removed {Count} cached values matching pattern: {Pattern}",
                matchingKeys.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_disposed) return false;

        try
        {
            // 메모리 캐시 확인
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            // 분산 캐시 확인
            if (_distributedCache != null)
            {
                var value = await _distributedCache.GetStringAsync(key, cancellationToken);
                return !string.IsNullOrEmpty(value);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return Task.CompletedTask;

        try
        {
            // 메모리 캐시 클리어 (IMemoryCache는 전체 클리어 메서드가 없음)
            if (_memoryCache is MemoryCache mc)
            {
                mc.Compact(1.0); // 모든 엔트리 제거
            }

            // 분산 캐시는 패턴별 제거 (전체 클리어 메서드가 없는 경우)
            _cacheEntries.Clear();
            _keyStatistics.Clear();

            _logger.LogInformation("Cache cleared");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_statsLock)
            {
                var totalRequests = Interlocked.Read(ref _totalRequests);
                var memoryHits = Interlocked.Read(ref _memoryHits);
                var distributedHits = Interlocked.Read(ref _distributedHits);
                var misses = Interlocked.Read(ref _misses);
                var evictions = Interlocked.Read(ref _evictions);

                var totalHits = memoryHits + distributedHits;

                var statistics = new CacheStatistics
                {
                    TotalRequests = totalRequests,
                    TotalHits = totalHits,
                    TotalMisses = misses,
                    MemoryHits = memoryHits,
                    DistributedHits = distributedHits,
                    Evictions = evictions,
                    HitRate = totalRequests > 0 ? (double)totalHits / totalRequests : 0.0,
                    MemoryHitRate = totalRequests > 0 ? (double)memoryHits / totalRequests : 0.0,
                    DistributedHitRate = totalRequests > 0 ? (double)distributedHits / totalRequests : 0.0,
                    CurrentEntryCount = _cacheEntries.Count,
                    KeyStatistics = new Dictionary<string, CacheKeyStatistics>(_keyStatistics),
                    LastUpdated = DateTime.UtcNow
                };

                return Task.FromResult(statistics);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return Task.FromResult(new CacheStatistics());
        }
    }

    #region Private Methods

    private MemoryCacheEntryOptions CreateMemoryCacheOptions(string key, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        // 캐시 전략 결정
        var strategy = DetermineCacheStrategy(key);
        switch (strategy)
        {
            case CacheStrategy.HighPriority:
                options.Priority = CacheItemPriority.High;
                options.SlidingExpiration = TimeSpan.FromMinutes(30);
                break;

            case CacheStrategy.LowPriority:
                options.Priority = CacheItemPriority.Low;
                options.SlidingExpiration = TimeSpan.FromMinutes(5);
                break;

            default:
                options.Priority = CacheItemPriority.Normal;
                options.SlidingExpiration = TimeSpan.FromMinutes(15);
                break;
        }

        // 제거 콜백 설정
        options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
        {
            EvictionCallback = OnEvicted,
            State = key
        });

        return options;
    }

    private TimeSpan GetDefaultExpiration(string key)
    {
        // 키 패턴에 따른 기본 만료 시간
        if (key.StartsWith("chunk:"))
            return TimeSpan.FromHours(2);

        if (key.StartsWith("metadata:"))
            return TimeSpan.FromMinutes(30);

        if (key.StartsWith("analysis:"))
            return TimeSpan.FromMinutes(15);

        return TimeSpan.FromMinutes(10); // 기본값
    }

    private CacheStrategy DetermineCacheStrategy(string key)
    {
        // 키 패턴 및 통계에 따른 캐시 전략 결정
        if (_keyStatistics.TryGetValue(key, out var stats))
        {
            if (stats.HitCount > 10 && stats.HitRate > 0.7)
                return CacheStrategy.HighPriority;

            if (stats.HitRate < 0.2)
                return CacheStrategy.LowPriority;
        }

        // 키 패턴 기반 전략
        if (key.StartsWith("frequent:") || key.StartsWith("user:"))
            return CacheStrategy.HighPriority;

        if (key.StartsWith("temp:") || key.StartsWith("debug:"))
            return CacheStrategy.LowPriority;

        return CacheStrategy.Normal;
    }

    private void UpdateCacheEntryInfo<T>(string key, T value, TimeSpan expiration) where T : class
    {
        var info = new CacheEntryInfo
        {
            Key = key,
            ValueType = typeof(T).Name,
            SizeBytes = EstimateObjectSize(value),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            AccessCount = 0
        };

        _cacheEntries.AddOrUpdate(key, info, (k, existing) =>
        {
            existing.AccessCount++;
            return existing;
        });
    }

    private void UpdateKeyStatistics(string key, CacheHitType hitType)
    {
        _keyStatistics.AddOrUpdate(key,
            new CacheKeyStatistics
            {
                Key = key,
                HitCount = hitType != CacheHitType.Miss ? 1 : 0,
                MissCount = hitType == CacheHitType.Miss ? 1 : 0,
                LastHitType = hitType,
                LastAccessTime = DateTimeOffset.UtcNow
            },
            (k, existing) => new CacheKeyStatistics
            {
                Key = key,
                HitCount = existing.HitCount + (hitType != CacheHitType.Miss ? 1 : 0),
                MissCount = existing.MissCount + (hitType == CacheHitType.Miss ? 1 : 0),
                LastHitType = hitType,
                LastAccessTime = DateTimeOffset.UtcNow
            });
    }

    private void OnEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        Interlocked.Increment(ref _evictions);

        if (key is string keyStr)
        {
            _cacheEntries.TryRemove(keyStr, out _);
            _logger.LogTrace("Cache entry evicted: {Key} (reason: {Reason})", keyStr, reason);
        }
    }

    private bool IsPatternMatch(string key, string pattern)
    {
        // 간단한 와일드카드 패턴 매칭
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
        }

        return key.Contains(pattern);
    }

    private long EstimateObjectSize(object obj)
    {
        try
        {
            // 간단한 객체 크기 추정
            var serialized = JsonSerializer.Serialize(obj);
            return Encoding.UTF8.GetByteCount(serialized);
        }
        catch
        {
            return 1024; // 기본값 1KB
        }
    }

    private void PerformCleanup(object? state)
    {
        if (_disposed) return;

        try
        {
            // 만료된 엔트리 정리
            var expiredKeys = _cacheEntries
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cacheEntries.TryRemove(key, out _);
            }

            // 오래된 통계 정리 (7일 이상)
            var oldStatKeys = _keyStatistics
                .Where(kvp => kvp.Value.LastAccessed < DateTime.UtcNow.AddDays(-7))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldStatKeys)
            {
                _keyStatistics.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0 || oldStatKeys.Count > 0)
            {
                _logger.LogDebug("Cleanup completed: {ExpiredEntries} expired entries, {OldStats} old statistics removed",
                    expiredKeys.Count, oldStatKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache cleanup");
        }
    }

    private void CollectStatistics(object? state)
    {
        if (_disposed) return;

        try
        {
            // 주기적 통계 수집 및 로깅
            var totalRequests = Interlocked.Read(ref _totalRequests);
            var totalHits = Interlocked.Read(ref _memoryHits) + Interlocked.Read(ref _distributedHits);
            var hitRate = totalRequests > 0 ? (double)totalHits / totalRequests : 0.0;

            _logger.LogDebug("Cache statistics: {TotalRequests} requests, {HitRate:P2} hit rate, {EntryCount} entries",
                totalRequests, hitRate, _cacheEntries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error collecting cache statistics");
        }
    }

    /// <summary>
    /// 캐시 키의 만료 시간 설정
    /// </summary>
    public async Task ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            // 메모리 캐시에서 기존 값 가져오기
            if (_memoryCache.TryGetValue(key, out var value))
            {
                // 새로운 만료 시간으로 다시 설정
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration,
                    Priority = CacheItemPriority.Normal
                };

                _memoryCache.Set(key, value, options);
            }

            // 분산 캐시가 있다면 갱신
            if (_distributedCache != null && value != null)
            {
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };

                var serialized = JsonSerializer.Serialize(value);
                await _distributedCache.SetStringAsync(key, serialized, distributedOptions, cancellationToken);
            }

            // 엔트리 정보 갱신
            if (_cacheEntries.TryGetValue(key, out var entryInfo))
            {
                entryInfo.ExpiresAt = DateTime.UtcNow.Add(expiration);
            }

            _logger.LogTrace("Cache expiration updated: {Key} -> {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cache expiration for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// 캐시 키의 남은 만료 시간 조회
    /// </summary>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            if (_cacheEntries.TryGetValue(key, out var entryInfo))
            {
                var remaining = entryInfo.ExpiresAt - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : null;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get TTL for cache key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 트랜잭션 지원을 위한 배치 연산
    /// </summary>
    public async Task ExecuteBatchAsync(IEnumerable<CacheOperation> operations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operations);

        var operationList = operations.ToList();
        if (!operationList.Any()) return;

        try
        {
            var tasks = new List<Task>();

            foreach (var operation in operationList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var task = operation.Type switch
                {
                    CacheOperationType.Set => SetAsync(operation.Key, operation.Value!, operation.Expiration, cancellationToken),
                    CacheOperationType.Remove => RemoveAsync(operation.Key, cancellationToken),
                    CacheOperationType.Expire => ExpireAsync(operation.Key, operation.Expiration ?? TimeSpan.FromHours(1), cancellationToken),
                    _ => throw new ArgumentOutOfRangeException(nameof(operation.Type))
                };

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            _logger.LogTrace("Executed batch cache operations: {Count} operations", operationList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute batch cache operations");
            throw;
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _cleanupTimer?.Dispose();
            _statisticsTimer?.Dispose();

            _logger.LogInformation("CacheService disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during CacheService disposal");
        }
    }
}

#region Internal Models

/// <summary>
/// 캐시 전략
/// </summary>
internal enum CacheStrategy
{
    LowPriority,
    Normal,
    HighPriority
}

/// <summary>
/// 캐시 히트 타입
/// </summary>
internal enum CacheHitType
{
    Miss,
    L1Hit,
    L2Hit
}

/// <summary>
/// 캐시 엔트리 정보
/// </summary>
internal class CacheEntryInfo
{
    public string Key { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public long AccessCount { get; set; }
}


#endregion