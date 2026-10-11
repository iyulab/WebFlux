using WebFlux.Core.Models;

namespace WebFlux.Tests.Services;

/// <summary>Policies the resilience tests run with.</summary>
internal static class TestResiliencePolicies
{
    /// <summary>
    /// HTTP 요청용 기본 정책
    /// </summary>
    internal static HttpResiliencePolicy DefaultHttp => new()
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
    /// 데이터베이스 접근용 정책
    /// </summary>
    internal static ResiliencePolicy DefaultDatabase => new()
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
}
