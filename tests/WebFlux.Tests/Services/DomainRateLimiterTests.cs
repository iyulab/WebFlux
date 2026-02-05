using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// DomainRateLimiter 단위 테스트
/// 도메인별 Rate Limiting 기능 검증
/// </summary>
public class DomainRateLimiterTests : IDisposable
{
    private readonly Mock<ILogger<DomainRateLimiter>> _mockLogger;
    private readonly DomainRateLimiter _rateLimiter;

    public DomainRateLimiterTests()
    {
        _mockLogger = new Mock<ILogger<DomainRateLimiter>>();
        _rateLimiter = new DomainRateLimiter(_mockLogger.Object);
    }

    public void Dispose()
    {
        _rateLimiter.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DomainRateLimiter(null!));
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        // Act & Assert
        using var limiter = new DomainRateLimiter(_mockLogger.Object);
        limiter.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomInterval_ShouldUseInterval()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(2);

        // Act
        using var limiter = new DomainRateLimiter(_mockLogger.Object, interval);

        // Assert
        limiter.GetDomainLimit("example.com").Should().Be(interval);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithNewDomain_ShouldExecuteImmediately()
    {
        // Arrange
        var domain = "example.com";
        var executed = false;

        // Act
        await _rateLimiter.ExecuteAsync(domain, () =>
        {
            executed = true;
            return Task.FromResult(true);
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithSameDomain_ShouldWait()
    {
        // Arrange
        var domain = "example.com";
        _rateLimiter.SetDomainLimit(domain, TimeSpan.FromMilliseconds(100));

        // Act - First request
        await _rateLimiter.ExecuteAsync(domain, () => Task.FromResult(1));

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act - Second request (should wait)
        await _rateLimiter.ExecuteAsync(domain, () => Task.FromResult(2));

        sw.Stop();

        // Assert - Should have waited at least 50ms (allowing for timing variance)
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public async Task ExecuteAsync_WithDifferentDomains_ShouldNotWait()
    {
        // Arrange
        var domain1 = "example1.com";
        var domain2 = "example2.com";

        // Act
        await _rateLimiter.ExecuteAsync(domain1, () => Task.FromResult(1));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _rateLimiter.ExecuteAsync(domain2, () => Task.FromResult(2));
        sw.Stop();

        // Assert - Should execute immediately (no wait needed for different domain)
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        var domain = "example.com";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException is a subtype of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _rateLimiter.ExecuteAsync(domain, () => Task.FromResult(1), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_VoidVersion_ShouldWork()
    {
        // Arrange
        var domain = "example.com";
        var executed = false;

        // Act
        await _rateLimiter.ExecuteAsync(domain, () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithUrl_ShouldExtractDomain()
    {
        // Arrange
        var url = "https://www.example.com/path/to/page";

        // Act
        await _rateLimiter.ExecuteAsync(url, () => Task.FromResult(1));

        // Assert
        var lastRequest = _rateLimiter.GetLastRequestTime("www.example.com");
        lastRequest.Should().NotBeNull();
    }

    #endregion

    #region SetDomainLimit Tests

    [Fact]
    public void SetDomainLimit_ShouldUpdateInterval()
    {
        // Arrange
        var domain = "example.com";
        var interval = TimeSpan.FromSeconds(5);

        // Act
        _rateLimiter.SetDomainLimit(domain, interval);

        // Assert
        _rateLimiter.GetDomainLimit(domain).Should().Be(interval);
    }

    [Fact]
    public void SetDomainLimit_WithEmptyDomain_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _rateLimiter.SetDomainLimit("", TimeSpan.FromSeconds(1)));
    }

    #endregion

    #region GetDomainLimit Tests

    [Fact]
    public void GetDomainLimit_WithUnknownDomain_ShouldReturnDefault()
    {
        // Act
        var limit = _rateLimiter.GetDomainLimit("unknown.com");

        // Assert
        limit.Should().Be(DomainRateLimiter.DefaultMinInterval);
    }

    [Fact]
    public void GetDomainLimit_WithConfiguredDomain_ShouldReturnConfiguredValue()
    {
        // Arrange
        var domain = "example.com";
        var interval = TimeSpan.FromSeconds(3);
        _rateLimiter.SetDomainLimit(domain, interval);

        // Act
        var limit = _rateLimiter.GetDomainLimit(domain);

        // Assert
        limit.Should().Be(interval);
    }

    #endregion

    #region GetLastRequestTime Tests

    [Fact]
    public void GetLastRequestTime_WithNoRequests_ShouldReturnNull()
    {
        // Act
        var lastRequest = _rateLimiter.GetLastRequestTime("example.com");

        // Assert
        lastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetLastRequestTime_AfterRequest_ShouldReturnTime()
    {
        // Arrange
        var domain = "example.com";
        var beforeRequest = DateTimeOffset.UtcNow;

        // Act
        await _rateLimiter.ExecuteAsync(domain, () => Task.FromResult(1));
        var lastRequest = _rateLimiter.GetLastRequestTime(domain);

        // Assert
        lastRequest.Should().NotBeNull();
        lastRequest!.Value.Should().BeOnOrAfter(beforeRequest);
    }

    #endregion

    #region GetWaitTime Tests

    [Fact]
    public void GetWaitTime_WithNoRequests_ShouldReturnZero()
    {
        // Act
        var waitTime = _rateLimiter.GetWaitTime("example.com");

        // Assert
        waitTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetWaitTime_AfterRequest_ShouldReturnRemainingTime()
    {
        // Arrange
        var domain = "example.com";
        _rateLimiter.SetDomainLimit(domain, TimeSpan.FromSeconds(10));

        // Act
        await _rateLimiter.ExecuteAsync(domain, () => Task.FromResult(1));
        var waitTime = _rateLimiter.GetWaitTime(domain);

        // Assert
        waitTime.Should().BeGreaterThan(TimeSpan.Zero);
        waitTime.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_WithNoRequests_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _rateLimiter.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalRequests.Should().Be(0);
        stats.RegisteredDomains.Should().Be(0);
    }

    [Fact]
    public async Task GetStatistics_AfterRequests_ShouldTrackStats()
    {
        // Arrange
        _rateLimiter.SetDomainLimit("example.com", TimeSpan.FromMilliseconds(10));

        // Act
        await _rateLimiter.ExecuteAsync("example.com", () => Task.FromResult(1));
        await _rateLimiter.ExecuteAsync("example.com", () => Task.FromResult(2));
        await _rateLimiter.ExecuteAsync("other.com", () => Task.FromResult(3));

        var stats = _rateLimiter.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(3);
        stats.RegisteredDomains.Should().Be(2);
        stats.RequestsByDomain["example.com"].Should().Be(2);
        stats.RequestsByDomain["other.com"].Should().Be(1);
    }

    #endregion

    #region RemoveDomainLimit Tests

    [Fact]
    public void RemoveDomainLimit_WithExistingDomain_ShouldRemove()
    {
        // Arrange
        var domain = "example.com";
        _rateLimiter.SetDomainLimit(domain, TimeSpan.FromSeconds(5));

        // Act
        _rateLimiter.RemoveDomainLimit(domain);

        // Assert
        _rateLimiter.GetDomainLimit(domain).Should().Be(DomainRateLimiter.DefaultMinInterval);
    }

    [Fact]
    public void RemoveDomainLimit_WithNonExistentDomain_ShouldNotThrow()
    {
        // Act & Assert
        _rateLimiter.Invoking(r => r.RemoveDomainLimit("nonexistent.com")).Should().NotThrow();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_ShouldClearAllState()
    {
        // Arrange
        _rateLimiter.SetDomainLimit("example.com", TimeSpan.FromSeconds(5));
        await _rateLimiter.ExecuteAsync("example.com", () => Task.FromResult(1));

        // Act
        _rateLimiter.Reset();
        var stats = _rateLimiter.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(0);
        stats.RegisteredDomains.Should().Be(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var limiter = new DomainRateLimiter(_mockLogger.Object);

        // Act & Assert
        limiter.Invoking(l => l.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var limiter = new DomainRateLimiter(_mockLogger.Object);

        // Act & Assert
        limiter.Invoking(l =>
        {
            l.Dispose();
            l.Dispose();
            l.Dispose();
        }).Should().NotThrow();
    }

    #endregion
}
