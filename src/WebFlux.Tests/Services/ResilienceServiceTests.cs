using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;
using FluentAssertions;
using Polly.CircuitBreaker;
using Polly.Bulkhead;
using Polly.Timeout;

namespace WebFlux.Tests.Services;

/// <summary>
/// ResilienceService 단위 테스트
/// Polly 기반 회복탄력성 패턴 검증
/// 재시도, 회로차단기, 시간초과, 벌크헤드 테스트
/// </summary>
public class ResilienceServiceTests
{
    private readonly IResilienceService _resilienceService;
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceServiceTests()
    {
        _logger = new LoggerFactory().CreateLogger<ResilienceService>();
        _resilienceService = new ResilienceService(_logger);
    }

    #region 재시도 정책 테스트

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryStrategy.Fixed
        };

        var expectedResult = "success";
        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult(expectedResult);

        // Act
        var result = await _resilienceService.ExecuteWithRetryAsync(operation, retryPolicy);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FailingThenSuccessfulOperation_ShouldRetryAndSucceed()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryStrategy.Fixed
        };

        var attemptCount = 0;
        var expectedResult = "success after retry";

        Func<CancellationToken, Task<string>> operation = _ =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await _resilienceService.ExecuteWithRetryAsync(operation, retryPolicy);

        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ExponentialBackoff_ShouldIncreaseDelays()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxRetryAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryStrategy.ExponentialBackoff,
            UseJitter = false
        };

        var attemptTimes = new List<DateTime>();
        Func<CancellationToken, Task<string>> operation = _ =>
        {
            attemptTimes.Add(DateTime.UtcNow);
            throw new InvalidOperationException("Always fails");
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _resilienceService.ExecuteWithRetryAsync(operation, retryPolicy));

        // Verify exponential backoff pattern
        attemptTimes.Should().HaveCount(4); // 초기 시도 + 3번 재시도
    }

    [Theory]
    [InlineData(RetryStrategy.Fixed)]
    [InlineData(RetryStrategy.Linear)]
    [InlineData(RetryStrategy.ExponentialBackoff)]
    public async Task ExecuteWithRetryAsync_DifferentStrategies_ShouldWork(RetryStrategy strategy)
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = strategy
        };

        var attemptCount = 0;
        Func<CancellationToken, Task<string>> operation = _ =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new InvalidOperationException("Temporary failure");
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _resilienceService.ExecuteWithRetryAsync(operation, retryPolicy);

        // Assert
        result.Should().Be("success");
        attemptCount.Should().Be(2);
    }

    #endregion

    #region 회로차단기 테스트

    [Fact]
    public async Task ExecuteWithCircuitBreakerAsync_SuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var circuitBreakerPolicy = new WebFlux.Core.Models.CircuitBreakerPolicy
        {
            Name = "test-circuit-breaker",
            FailureThreshold = 3,
            DurationOfBreak = TimeSpan.FromMilliseconds(100),
            SamplingDuration = TimeSpan.FromSeconds(1),
            MinimumThroughput = 2,
            FailureRatio = 0.5
        };

        var expectedResult = "success";
        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult(expectedResult);

        // Act
        var result = await _resilienceService.ExecuteWithCircuitBreakerAsync(operation, circuitBreakerPolicy);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void GetCircuitBreakerState_InitialState_ShouldBeClosed()
    {
        // Arrange
        var circuitBreakerName = "test-circuit-breaker";

        // Act
        var state = _resilienceService.GetCircuitBreakerState(circuitBreakerName);

        // Assert
        state.Should().Be(CircuitBreakerState.Closed);
    }

    #endregion

    #region 시간초과 테스트

    [Fact]
    public async Task ExecuteWithTimeoutAsync_FastOperation_ShouldSucceed()
    {
        // Arrange
        var timeoutPolicy = new WebFlux.Core.Models.TimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(1),
            Strategy = WebFlux.Core.Models.TimeoutStrategy.Cooperative
        };

        var expectedResult = "fast result";
        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult(expectedResult);

        // Act
        var result = await _resilienceService.ExecuteWithTimeoutAsync(operation, timeoutPolicy);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_SlowOperation_ShouldTimeout()
    {
        // Arrange
        var timeoutPolicy = new WebFlux.Core.Models.TimeoutPolicy
        {
            Timeout = TimeSpan.FromMilliseconds(100),
            Strategy = WebFlux.Core.Models.TimeoutStrategy.Cooperative
        };

        Func<CancellationToken, Task<string>> operation = async (ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "slow result";
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            () => _resilienceService.ExecuteWithTimeoutAsync(operation, timeoutPolicy));
    }

    [Theory]
    [InlineData(WebFlux.Core.Models.TimeoutStrategy.Cooperative)]
    [InlineData(WebFlux.Core.Models.TimeoutStrategy.Pessimistic)]
    public async Task ExecuteWithTimeoutAsync_DifferentStrategies_ShouldWork(WebFlux.Core.Models.TimeoutStrategy strategy)
    {
        // Arrange
        var timeoutPolicy = new WebFlux.Core.Models.TimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(1),
            Strategy = strategy
        };

        var expectedResult = "result";
        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult(expectedResult);

        // Act
        var result = await _resilienceService.ExecuteWithTimeoutAsync(operation, timeoutPolicy);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region 벌크헤드 테스트

    [Fact]
    public async Task ExecuteWithBulkheadAsync_WithinLimits_ShouldSucceed()
    {
        // Arrange
        var bulkheadPolicy = new WebFlux.Core.Models.BulkheadPolicy
        {
            Name = "test-bulkhead",
            MaxParallelization = 2,
            MaxQueuingActions = 1
        };

        var expectedResult = "bulkhead result";
        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult(expectedResult);

        // Act
        var result = await _resilienceService.ExecuteWithBulkheadAsync(operation, bulkheadPolicy);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void GetBulkheadUtilization_UnknownBulkhead_ShouldReturnZero()
    {
        // Arrange
        var bulkheadName = "unknown-bulkhead";

        // Act
        var utilization = _resilienceService.GetBulkheadUtilization(bulkheadName);

        // Assert
        utilization.Should().Be(0.0);
    }

    #endregion

    #region 복합 정책 테스트

    [Fact]
    public async Task ExecuteWithResilienceAsync_ComplexPolicy_ShouldApplyAllPolicies()
    {
        // Arrange
        var resiliencePolicy = new ResiliencePolicy
        {
            Name = "complex-policy",
            Retry = new RetryPolicy
            {
                MaxRetryAttempts = 2,
                BaseDelay = TimeSpan.FromMilliseconds(10),
                Strategy = RetryStrategy.Fixed
            },
            Timeout = new WebFlux.Core.Models.TimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(1),
                Strategy = WebFlux.Core.Models.TimeoutStrategy.Cooperative
            },
            ExecutionOrder = new[] { PolicyType.Retry, PolicyType.Timeout }
        };

        var attemptCount = 0;
        var expectedResult = "complex result";

        Func<CancellationToken, Task<string>> operation = _ =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new InvalidOperationException("Temporary failure");
            }
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await _resilienceService.ExecuteWithResilienceAsync(operation, resiliencePolicy);

        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteHttpWithResilienceAsync_HttpPolicy_ShouldWork()
    {
        // Arrange
        var httpPolicy = PredefinedResiliencePolicies.DefaultHttp;
        var expectedResult = "http result";

        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult(expectedResult);

        // Act
        var result = await _resilienceService.ExecuteHttpWithResilienceAsync(operation, httpPolicy);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region 통계 테스트

    [Fact]
    public async Task GetStatistics_AfterOperations_ShouldReturnCorrectStats()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxRetryAttempts = 1,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryStrategy.Fixed
        };

        // Act - 성공적인 작업 실행
        await _resilienceService.ExecuteWithRetryAsync(
            _ => Task.FromResult("success"), retryPolicy);

        // Act - 통계 가져오기
        var stats = _resilienceService.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalExecutions.Should().BeGreaterThan(0);
        stats.SuccessfulExecutions.Should().BeGreaterThan(0);
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    #endregion

    #region 사전 정의된 정책 테스트

    [Fact]
    public void PredefinedResiliencePolicies_DefaultHttp_ShouldHaveCorrectConfiguration()
    {
        // Act
        var policy = PredefinedResiliencePolicies.DefaultHttp;

        // Assert
        policy.Should().NotBeNull();
        policy.Name.Should().Be("DefaultHttp");
        policy.Retry.Should().NotBeNull();
        policy.CircuitBreaker.Should().NotBeNull();
        policy.Timeout.Should().NotBeNull();
        policy.HttpTimeout.Should().Be(TimeSpan.FromSeconds(30));
        policy.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void PredefinedResiliencePolicies_DefaultFileIO_ShouldHaveCorrectConfiguration()
    {
        // Act
        var policy = PredefinedResiliencePolicies.DefaultFileIO;

        // Assert
        policy.Should().NotBeNull();
        policy.Name.Should().Be("DefaultFileIO");
        policy.Retry.Should().NotBeNull();
        policy.Timeout.Should().NotBeNull();
        policy.CircuitBreaker.Should().BeNull(); // 파일 I/O에는 회로차단기 불필요
        policy.ExecutionOrder.Should().Contain(PolicyType.Retry);
        policy.ExecutionOrder.Should().Contain(PolicyType.Timeout);
    }

    [Fact]
    public void PredefinedResiliencePolicies_DefaultDatabase_ShouldHaveCorrectConfiguration()
    {
        // Act
        var policy = PredefinedResiliencePolicies.DefaultDatabase;

        // Assert
        policy.Should().NotBeNull();
        policy.Name.Should().Be("DefaultDatabase");
        policy.Retry.Should().NotBeNull();
        policy.CircuitBreaker.Should().NotBeNull();
        policy.Timeout.Should().NotBeNull();
        policy.Bulkhead.Should().NotBeNull();
        policy.Bulkhead.MaxParallelization.Should().Be(20);
        policy.Bulkhead.MaxQueuingActions.Should().Be(50);
    }

    [Fact]
    public void PredefinedResiliencePolicies_DefaultExternalApi_ShouldHaveCorrectConfiguration()
    {
        // Act
        var policy = PredefinedResiliencePolicies.DefaultExternalApi;

        // Assert
        policy.Should().NotBeNull();
        policy.Name.Should().Be("DefaultExternalApi");
        policy.Retry.Should().NotBeNull();
        policy.CircuitBreaker.Should().NotBeNull();
        policy.Timeout.Should().NotBeNull();
        policy.Bulkhead.Should().NotBeNull();
        policy.Retry.MaxRetryAttempts.Should().Be(5);
        policy.CircuitBreaker.FailureThreshold.Should().Be(3);
        policy.Timeout.Timeout.Should().Be(TimeSpan.FromMinutes(2));
    }

    #endregion

    #region 에러 처리 테스트

    [Fact]
    public async Task ExecuteWithRetryAsync_NullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var retryPolicy = new RetryPolicy();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _resilienceService.ExecuteWithRetryAsync<string>(null!, retryPolicy));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NullPolicy_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<CancellationToken, Task<string>> operation = _ => Task.FromResult("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _resilienceService.ExecuteWithRetryAsync(operation, null!));
    }

    [Fact]
    public void GetCircuitBreakerState_NullOrEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resilienceService.GetCircuitBreakerState(""));
        Assert.Throws<ArgumentException>(() => _resilienceService.GetCircuitBreakerState(null!));
    }

    [Fact]
    public void GetBulkheadUtilization_NullOrEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _resilienceService.GetBulkheadUtilization(""));
        Assert.Throws<ArgumentException>(() => _resilienceService.GetBulkheadUtilization(null!));
    }

    #endregion

    #region 통합 시나리오 테스트

    [Fact]
    public async Task IntegrationScenario_HttpRequestWithResilience_ShouldHandleFailuresGracefully()
    {
        // Arrange
        var httpPolicy = new HttpResiliencePolicy
        {
            Name = "integration-test",
            Retry = new RetryPolicy
            {
                MaxRetryAttempts = 3,
                BaseDelay = TimeSpan.FromMilliseconds(10),
                Strategy = RetryStrategy.ExponentialBackoff
            },
            CircuitBreaker = new WebFlux.Core.Models.CircuitBreakerPolicy
            {
                Name = "integration-circuit-breaker",
                FailureThreshold = 2,
                DurationOfBreak = TimeSpan.FromMilliseconds(100),
                SamplingDuration = TimeSpan.FromSeconds(1),
                MinimumThroughput = 2,
                FailureRatio = 0.5
            },
            Timeout = new WebFlux.Core.Models.TimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(1),
                Strategy = WebFlux.Core.Models.TimeoutStrategy.Cooperative
            }
        };

        var attemptCount = 0;
        var expectedResult = "integration success";

        Func<CancellationToken, Task<string>> httpOperation = async (ct) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                // 네트워크 오류 시뮬레이션
                throw new HttpRequestException($"HTTP request failed on attempt {attemptCount}");
            }

            await Task.Delay(10, ct); // 약간의 지연 시뮬레이션
            return expectedResult;
        };

        // Act
        var result = await _resilienceService.ExecuteHttpWithResilienceAsync(httpOperation, httpPolicy);

        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(3);

        // 통계 확인
        var stats = _resilienceService.GetStatistics();
        stats.TotalExecutions.Should().BeGreaterThan(0);
        stats.SuccessfulExecutions.Should().BeGreaterThan(0);
    }

    #endregion
}