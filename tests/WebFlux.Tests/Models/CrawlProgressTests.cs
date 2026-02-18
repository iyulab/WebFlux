using FluentAssertions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Models;

public class CrawlProgressTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var progress = new CrawlProgress();

        progress.TotalUrls.Should().Be(0);
        progress.ProcessedUrls.Should().Be(0);
        progress.SuccessCount.Should().Be(0);
        progress.FailureCount.Should().Be(0);
        progress.TotalChunks.Should().Be(0);
        progress.CurrentUrl.Should().BeEmpty();
        progress.ElapsedTime.Should().Be(TimeSpan.Zero);
        progress.EstimatedRemaining.Should().Be(TimeSpan.Zero);
        progress.Errors.Should().BeEmpty();
        progress.Statistics.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(10, 0, 0.0)]
    [InlineData(10, 5, 0.5)]
    [InlineData(10, 10, 1.0)]
    [InlineData(100, 33, 0.33)]
    public void ProgressPercentage_ShouldComputeCorrectly(int total, int processed, double expected)
    {
        var progress = new CrawlProgress { TotalUrls = total, ProcessedUrls = processed };
        progress.ProgressPercentage.Should().BeApproximately(expected, 0.001);
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(10, 0, 0.0)]
    [InlineData(10, 8, 0.8)]
    [InlineData(10, 10, 1.0)]
    public void SuccessRate_ShouldComputeCorrectly(int processed, int success, double expected)
    {
        var progress = new CrawlProgress { ProcessedUrls = processed, SuccessCount = success };
        progress.SuccessRate.Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void UrlsPerSecond_ShouldComputeCorrectly()
    {
        var progress = new CrawlProgress
        {
            ProcessedUrls = 100,
            ElapsedTime = TimeSpan.FromSeconds(10)
        };
        progress.UrlsPerSecond.Should().BeApproximately(10.0, 0.001);
    }

    [Fact]
    public void UrlsPerSecond_WithZeroElapsed_ShouldReturnZero()
    {
        var progress = new CrawlProgress { ProcessedUrls = 50 };
        progress.UrlsPerSecond.Should().Be(0.0);
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(5, 20, 4.0)]
    [InlineData(10, 30, 3.0)]
    public void AverageChunksPerUrl_ShouldComputeCorrectly(int success, int chunks, double expected)
    {
        var progress = new CrawlProgress { SuccessCount = success, TotalChunks = chunks };
        progress.AverageChunksPerUrl.Should().BeApproximately(expected, 0.001);
    }
}

public class CrawlErrorTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var error = new CrawlError
        {
            Url = "https://example.com",
            ErrorType = "Timeout",
            Message = "Request timed out"
        };

        error.StatusCode.Should().BeNull();
        error.RetryCount.Should().Be(0);
        error.StackTrace.Should().BeNull();
    }

    [Fact]
    public void ShouldInitialize_WithAllFields()
    {
        var error = new CrawlError
        {
            Url = "https://example.com/page",
            ErrorType = "ServerError",
            Message = "Internal Server Error",
            StatusCode = 500,
            RetryCount = 3,
            StackTrace = "at WebFlux..."
        };

        error.Url.Should().Be("https://example.com/page");
        error.ErrorType.Should().Be("ServerError");
        error.StatusCode.Should().Be(500);
        error.RetryCount.Should().Be(3);
    }
}

public class CrawlStatisticsDetailsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var stats = new CrawlStatisticsDetails();

        stats.TotalBytesDownloaded.Should().Be(0);
        stats.AverageResponseTimeMs.Should().Be(0);
        stats.MinResponseTimeMs.Should().Be(double.MaxValue);
        stats.MaxResponseTimeMs.Should().Be(0);
        stats.UrlsByDomain.Should().BeEmpty();
        stats.ErrorsByType.Should().BeEmpty();
        stats.StatusCodeCounts.Should().BeEmpty();
        stats.ContentTypeCounts.Should().BeEmpty();
    }
}

public class CrawlErrorTypesTests
{
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        CrawlErrorTypes.Timeout.Should().Be("Timeout");
        CrawlErrorTypes.ConnectionFailed.Should().Be("ConnectionFailed");
        CrawlErrorTypes.DnsFailure.Should().Be("DnsFailure");
        CrawlErrorTypes.SslError.Should().Be("SslError");
        CrawlErrorTypes.NotFound.Should().Be("NotFound");
        CrawlErrorTypes.Forbidden.Should().Be("Forbidden");
        CrawlErrorTypes.RateLimited.Should().Be("RateLimited");
        CrawlErrorTypes.ServerError.Should().Be("ServerError");
        CrawlErrorTypes.RobotsBlocked.Should().Be("RobotsBlocked");
        CrawlErrorTypes.ParseError.Should().Be("ParseError");
        CrawlErrorTypes.UnsupportedContentType.Should().Be("UnsupportedContentType");
        CrawlErrorTypes.ContentTooLarge.Should().Be("ContentTooLarge");
        CrawlErrorTypes.Unknown.Should().Be("Unknown");
    }
}
