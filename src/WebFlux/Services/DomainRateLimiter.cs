using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;

namespace WebFlux.Services;

/// <summary>
/// 도메인별 Rate Limiter 구현
/// ConcurrentDictionary 기반 도메인별 SemaphoreSlim 관리
/// </summary>
public partial class DomainRateLimiter : IDomainRateLimiter, IDisposable
{
    private readonly ILogger<DomainRateLimiter> _logger;
    private readonly ConcurrentDictionary<string, DomainState> _domainStates = new();
    private readonly TimeSpan _defaultMinInterval;
    private long _totalRequests;
    private long _totalWaitTimeMs;
    private bool _disposed;

    /// <summary>
    /// 기본 최소 간격 (1초)
    /// </summary>
    public static readonly TimeSpan DefaultMinInterval = TimeSpan.FromSeconds(1);

    public DomainRateLimiter(ILogger<DomainRateLimiter> logger)
        : this(logger, DefaultMinInterval)
    {
    }

    public DomainRateLimiter(ILogger<DomainRateLimiter> logger, TimeSpan defaultMinInterval)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultMinInterval = defaultMinInterval;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        string domain,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var normalizedDomain = NormalizeDomain(domain);
        var state = GetOrCreateState(normalizedDomain);

        await state.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var waitTime = await WaitIfNeededAsync(state, cancellationToken).ConfigureAwait(false);

            LogExecutingOperation(_logger, normalizedDomain, waitTime.TotalMilliseconds);

            var result = await operation().ConfigureAwait(false);

            state.LastRequestTime = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref state.RequestCount);
            Interlocked.Increment(ref _totalRequests);

            return result;
        }
        finally
        {
            state.Semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(
        string domain,
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(domain, async () =>
        {
            await operation().ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void SetDomainLimit(string domain, TimeSpan minimumInterval)
    {
        var normalizedDomain = NormalizeDomain(domain);
        var state = GetOrCreateState(normalizedDomain);
        state.MinInterval = minimumInterval;

        LogDomainLimitSet(_logger, normalizedDomain, minimumInterval.TotalMilliseconds);
    }

    /// <inheritdoc />
    public async Task ConfigureFromRobotsTxtAsync(string domain, CancellationToken cancellationToken = default)
    {
        var normalizedDomain = NormalizeDomain(domain);

        try
        {
            // robots.txt에서 crawl-delay 읽기
            // 이 구현은 ICrawler가 제공하는 GetRobotsTxtAsync를 사용할 수 있으나,
            // 의존성 순환을 피하기 위해 간단한 HTTP 요청으로 구현

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var robotsUrl = $"https://{normalizedDomain}/robots.txt";

            var response = await httpClient.GetAsync(robotsUrl, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogNoRobotsTxt(_logger, normalizedDomain);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var crawlDelay = ParseCrawlDelay(content);

            if (crawlDelay.HasValue)
            {
                SetDomainLimit(normalizedDomain, TimeSpan.FromSeconds(crawlDelay.Value));
                LogCrawlDelayApplied(_logger, normalizedDomain, crawlDelay.Value);
            }
        }
        catch (Exception ex)
        {
            LogRobotsTxtFailed(_logger, ex, normalizedDomain);
        }
    }

    /// <inheritdoc />
    public TimeSpan GetDomainLimit(string domain)
    {
        var normalizedDomain = NormalizeDomain(domain);

        if (_domainStates.TryGetValue(normalizedDomain, out var state))
        {
            return state.MinInterval;
        }

        return _defaultMinInterval;
    }

    /// <inheritdoc />
    public DateTimeOffset? GetLastRequestTime(string domain)
    {
        var normalizedDomain = NormalizeDomain(domain);

        if (_domainStates.TryGetValue(normalizedDomain, out var state))
        {
            return state.LastRequestTime;
        }

        return null;
    }

    /// <inheritdoc />
    public TimeSpan GetWaitTime(string domain)
    {
        var normalizedDomain = NormalizeDomain(domain);

        if (!_domainStates.TryGetValue(normalizedDomain, out var state))
        {
            return TimeSpan.Zero;
        }

        if (state.LastRequestTime == null)
        {
            return TimeSpan.Zero;
        }

        var elapsed = DateTimeOffset.UtcNow - state.LastRequestTime.Value;
        var remaining = state.MinInterval - elapsed;

        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <inheritdoc />
    public DomainRateLimiterStatistics GetStatistics()
    {
        var requestsByDomain = new Dictionary<string, long>();
        var waitTimeByDomain = new Dictionary<string, long>();

        foreach (var kvp in _domainStates)
        {
            requestsByDomain[kvp.Key] = kvp.Value.RequestCount;
            waitTimeByDomain[kvp.Key] = kvp.Value.TotalWaitTimeMs;
        }

        return new DomainRateLimiterStatistics
        {
            RegisteredDomains = _domainStates.Count,
            TotalRequests = Interlocked.Read(ref _totalRequests),
            TotalWaitTimeMs = Interlocked.Read(ref _totalWaitTimeMs),
            RequestsByDomain = requestsByDomain,
            WaitTimeByDomain = waitTimeByDomain,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public void RemoveDomainLimit(string domain)
    {
        var normalizedDomain = NormalizeDomain(domain);

        if (_domainStates.TryRemove(normalizedDomain, out var state))
        {
            state.Semaphore.Dispose();
            LogDomainLimitRemoved(_logger, normalizedDomain);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        foreach (var state in _domainStates.Values)
        {
            state.Semaphore.Dispose();
        }

        _domainStates.Clear();
        _totalRequests = 0;
        _totalWaitTimeMs = 0;

        LogRateLimiterReset(_logger);
    }

    private DomainState GetOrCreateState(string domain)
    {
        return _domainStates.GetOrAdd(domain, _ => new DomainState
        {
            MinInterval = _defaultMinInterval,
            Semaphore = new SemaphoreSlim(1, 1)
        });
    }

    private async Task<TimeSpan> WaitIfNeededAsync(DomainState state, CancellationToken cancellationToken)
    {
        if (state.LastRequestTime == null)
        {
            return TimeSpan.Zero;
        }

        var elapsed = DateTimeOffset.UtcNow - state.LastRequestTime.Value;
        var waitTime = state.MinInterval - elapsed;

        if (waitTime > TimeSpan.Zero)
        {
            LogWaitingBeforeRequest(_logger, waitTime.TotalMilliseconds);

            await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);

            var waitMs = (long)waitTime.TotalMilliseconds;
            Interlocked.Add(ref state.TotalWaitTimeMs, waitMs);
            Interlocked.Add(ref _totalWaitTimeMs, waitMs);

            return waitTime;
        }

        return TimeSpan.Zero;
    }

    private static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("Domain cannot be empty", nameof(domain));
        }

        // URL에서 도메인 추출
        if (Uri.TryCreate(domain, UriKind.Absolute, out var uri))
        {
            return uri.Host.ToLowerInvariant();
        }

        // 이미 도메인인 경우
        return domain.ToLowerInvariant().Trim();
    }

    private static int? ParseCrawlDelay(string robotsTxtContent)
    {
        if (string.IsNullOrWhiteSpace(robotsTxtContent))
        {
            return null;
        }

        // 간단한 파싱: Crawl-delay: 숫자 형태 찾기
        var lines = robotsTxtContent.Split('\n');
        var inUserAgentAll = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim().ToLowerInvariant();

            if (trimmedLine.StartsWith("user-agent:", StringComparison.Ordinal))
            {
                var agent = trimmedLine.Substring("user-agent:".Length).Trim();
                inUserAgentAll = agent == "*" || agent.Contains("webflux", StringComparison.Ordinal);
            }
            else if (inUserAgentAll && trimmedLine.StartsWith("crawl-delay:", StringComparison.Ordinal))
            {
                var delayStr = trimmedLine.Substring("crawl-delay:".Length).Trim();

                if (int.TryParse(delayStr, out var delay) && delay > 0 && delay <= 60)
                {
                    return delay;
                }
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var state in _domainStates.Values)
        {
            state.Semaphore.Dispose();
        }

        _domainStates.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 도메인별 상태 정보
    /// </summary>
    private sealed class DomainState
    {
        public TimeSpan MinInterval { get; set; }
        public DateTimeOffset? LastRequestTime { get; set; }
        public SemaphoreSlim Semaphore { get; set; } = null!;
        public long RequestCount;
        public long TotalWaitTimeMs;
    }

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Debug, Message = "Executing operation for domain {Domain} after {WaitMs}ms wait")]
    private static partial void LogExecutingOperation(ILogger logger, string Domain, double WaitMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Set rate limit for domain {Domain}: {IntervalMs}ms")]
    private static partial void LogDomainLimitSet(ILogger logger, string Domain, double IntervalMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No robots.txt found for domain {Domain}")]
    private static partial void LogNoRobotsTxt(ILogger logger, string Domain);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied crawl-delay from robots.txt for domain {Domain}: {Delay}s")]
    private static partial void LogCrawlDelayApplied(ILogger logger, string Domain, int Delay);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to read robots.txt for domain {Domain}")]
    private static partial void LogRobotsTxtFailed(ILogger logger, Exception ex, string Domain);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removed rate limit for domain {Domain}")]
    private static partial void LogDomainLimitRemoved(ILogger logger, string Domain);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rate limiter reset")]
    private static partial void LogRateLimiterReset(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Waiting {WaitMs}ms before next request")]
    private static partial void LogWaitingBeforeRequest(ILogger logger, double WaitMs);
}
