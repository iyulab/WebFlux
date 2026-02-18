using FluentAssertions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Models;

public class BatchExtractResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new BatchExtractResult();

        result.Succeeded.Should().BeEmpty();
        result.Failed.Should().BeEmpty();
        result.TotalDurationMs.Should().Be(0);
        result.Statistics.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 0, 3)]
    [InlineData(0, 2, 2)]
    [InlineData(5, 3, 8)]
    public void TotalCount_ShouldComputeCorrectly(int succeeded, int failed, int expected)
    {
        var result = new BatchExtractResult
        {
            Succeeded = Enumerable.Range(0, succeeded)
                .Select(i => new ExtractedContent { Url = $"https://example.com/{i}" })
                .ToArray(),
            Failed = Enumerable.Range(0, failed)
                .Select(i => new FailedExtraction { Url = $"https://fail.com/{i}" })
                .ToArray()
        };

        result.TotalCount.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(10, 0, 1.0)]
    [InlineData(0, 10, 0.0)]
    [InlineData(7, 3, 0.7)]
    public void SuccessRate_ShouldComputeCorrectly(int succeeded, int failed, double expected)
    {
        var result = new BatchExtractResult
        {
            Succeeded = Enumerable.Range(0, succeeded)
                .Select(i => new ExtractedContent { Url = $"https://example.com/{i}" })
                .ToArray(),
            Failed = Enumerable.Range(0, failed)
                .Select(i => new FailedExtraction { Url = $"https://fail.com/{i}" })
                .ToArray()
        };

        result.SuccessRate.Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void Empty_ShouldReturnDefaultResult()
    {
        var result = BatchExtractResult.Empty;

        result.Succeeded.Should().BeEmpty();
        result.Failed.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void ToString_ShouldIncludeStats()
    {
        var result = new BatchExtractResult
        {
            Succeeded = [new ExtractedContent { Url = "https://example.com" }],
            Failed = [],
            TotalDurationMs = 500
        };

        result.ToString().Should().Contain("1/1 succeeded");
        result.ToString().Should().Contain("100%");
        result.ToString().Should().Contain("500ms");
    }
}

public class FailedExtractionTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var failure = new FailedExtraction { Url = "https://broken.com" };

        failure.ErrorCode.Should().Be("Unknown");
        failure.ErrorMessage.Should().BeEmpty();
        failure.HttpStatusCode.Should().BeNull();
        failure.RetryCount.Should().Be(0);
        failure.ProcessingTimeMs.Should().Be(0);
        failure.Exception.Should().BeNull();
    }

    [Fact]
    public void ShouldInitialize_WithAllFields()
    {
        var failure = new FailedExtraction
        {
            Url = "https://example.com/broken",
            ErrorCode = "Timeout",
            ErrorMessage = "Request timed out",
            HttpStatusCode = 504,
            RetryCount = 3,
            ProcessingTimeMs = 30000
        };

        failure.ErrorCode.Should().Be("Timeout");
        failure.HttpStatusCode.Should().Be(504);
        failure.RetryCount.Should().Be(3);
    }

    [Fact]
    public void ToString_ShouldIncludeStatusCode()
    {
        var failure = new FailedExtraction
        {
            Url = "https://example.com",
            ErrorCode = "NotFound",
            ErrorMessage = "Page not found",
            HttpStatusCode = 404
        };

        failure.ToString().Should().Contain("404");
        failure.ToString().Should().Contain("NotFound");
    }

    [Fact]
    public void ToString_WithoutStatusCode_ShouldOmitIt()
    {
        var failure = new FailedExtraction
        {
            Url = "https://example.com",
            ErrorCode = "Timeout",
            ErrorMessage = "Timed out"
        };

        failure.ToString().Should().NotContain("HTTP");
    }
}

public class BatchStatisticsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var stats = new BatchStatistics();

        stats.AverageProcessingTimeMs.Should().Be(0);
        stats.TotalCharactersExtracted.Should().Be(0);
        stats.CacheHitRate.Should().Be(0);
        stats.CacheHits.Should().Be(0);
        stats.CacheMisses.Should().Be(0);
        stats.ProcessedByDomain.Should().BeEmpty();
        stats.FailuresByErrorCode.Should().BeEmpty();
        stats.MinProcessingTimeMs.Should().Be(0);
        stats.MaxProcessingTimeMs.Should().Be(0);
        stats.DynamicRenderingCount.Should().Be(0);
        stats.StaticRenderingCount.Should().Be(0);
    }
}

public class ExtractErrorCodesTests
{
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        ExtractErrorCodes.Timeout.Should().Be("Timeout");
        ExtractErrorCodes.NotFound.Should().Be("NotFound");
        ExtractErrorCodes.Blocked.Should().Be("Blocked");
        ExtractErrorCodes.ParseError.Should().Be("ParseError");
        ExtractErrorCodes.NetworkError.Should().Be("NetworkError");
        ExtractErrorCodes.ServerError.Should().Be("ServerError");
        ExtractErrorCodes.EmptyContent.Should().Be("EmptyContent");
        ExtractErrorCodes.InvalidUrl.Should().Be("InvalidUrl");
        ExtractErrorCodes.UnsupportedContentType.Should().Be("UnsupportedContentType");
        ExtractErrorCodes.SslError.Should().Be("SslError");
        ExtractErrorCodes.TooManyRedirects.Should().Be("TooManyRedirects");
        ExtractErrorCodes.Unknown.Should().Be("Unknown");
    }

    [Theory]
    [InlineData(404, "NotFound")]
    [InlineData(403, "Blocked")]
    [InlineData(429, "Blocked")]
    [InlineData(401, "Blocked")]
    [InlineData(500, "ServerError")]
    [InlineData(502, "ServerError")]
    [InlineData(503, "ServerError")]
    [InlineData(400, "Unknown")]
    [InlineData(200, "Unknown")]
    public void FromHttpStatusCode_ShouldMapCorrectly(int statusCode, string expected)
    {
        ExtractErrorCodes.FromHttpStatusCode(statusCode).Should().Be(expected);
    }
}
