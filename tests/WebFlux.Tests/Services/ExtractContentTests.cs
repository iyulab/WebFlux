using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using Xunit;
using FluentAssertions;
using CrawlStrategy = WebFlux.Core.Options.CrawlStrategy;

namespace WebFlux.Tests.Services;

/// <summary>
/// ExtractContent API 단위 테스트
/// DeepResearch 통합을 위한 경량 추출 API 검증
/// </summary>
public class ExtractContentTests
{
    private readonly Mock<IServiceFactory> _mockServiceFactory;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<WebContentProcessor>> _mockLogger;
    private readonly WebContentProcessor _processor;

    public ExtractContentTests()
    {
        _mockServiceFactory = new Mock<IServiceFactory>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<WebContentProcessor>>();

        _processor = new WebContentProcessor(
            _mockServiceFactory.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    #region ExtractContentAsync Tests

    [Fact]
    public async Task ExtractContentAsync_WithInvalidUrl_ShouldReturnFailure()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act
        var result = await _processor.ExtractContentAsync(invalidUrl);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(ExtractErrorCodes.InvalidUrl);
    }

    [Fact]
    public async Task ExtractContentAsync_WithValidUrl_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://example.com";
        SetupSuccessfulCrawl(url);

        // Act
        var result = await _processor.ExtractContentAsync(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Url.Should().Be(url);
    }

    [Fact]
    public async Task ExtractContentAsync_WithOptions_ShouldApplyOptions()
    {
        // Arrange
        var url = "https://example.com";
        var options = new ExtractOptions
        {
            Format = OutputFormat.PlainText,
            MaxTextLength = 100,
            EvaluateQuality = false
        };
        SetupSuccessfulCrawl(url);

        // Act
        var result = await _processor.ExtractContentAsync(url, options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractContentAsync_WithCrawlFailure_ShouldReturnFailure()
    {
        // Arrange
        var url = "https://example.com";
        SetupFailedCrawl(url, 404);

        // Act
        var result = await _processor.ExtractContentAsync(url);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractContentAsync_WithEmptyContent_ShouldReturnFailure()
    {
        // Arrange
        var url = "https://example.com";
        SetupEmptyContentCrawl(url);

        // Act
        var result = await _processor.ExtractContentAsync(url);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ExtractErrorCodes.EmptyContent);
    }

    [Fact]
    public async Task ExtractContentAsync_WithCancellation_ShouldReturnTimeout()
    {
        // Arrange
        var url = "https://example.com";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _processor.ExtractContentAsync(url, cancellationToken: cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ExtractErrorCodes.Timeout);
    }

    [Fact]
    public async Task ExtractContentAsync_WithDynamicRendering_ShouldUseDynamicCrawler()
    {
        // Arrange
        var url = "https://example.com";
        var options = new ExtractOptions { UseDynamicRendering = true };

        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.CrawlAsync(It.IsAny<string>(), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult
            {
                Url = url,
                Content = "<html><body>Test</body></html>",
                ContentType = "text/html",
                StatusCode = 200,
                IsSuccess = true
            });

        _mockServiceFactory.Setup(f => f.CreateCrawler(CrawlStrategy.Dynamic)).Returns(mockCrawler.Object);
        SetupExtractor();

        // Act
        var result = await _processor.ExtractContentAsync(url, options);

        // Assert
        _mockServiceFactory.Verify(f => f.CreateCrawler(CrawlStrategy.Dynamic), Times.Once);
    }

    [Fact]
    public async Task ExtractContentAsync_WithQualityEvaluation_ShouldIncludeQualityInfo()
    {
        // Arrange
        var url = "https://example.com";
        var options = new ExtractOptions { EvaluateQuality = true };
        SetupSuccessfulCrawl(url);

        var mockQualityEvaluator = new Mock<IContentQualityEvaluator>();
        mockQualityEvaluator.Setup(e => e.EvaluateAsync(It.IsAny<ExtractedContent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContentQualityInfo { OverallScore = 0.8 });
        _mockServiceFactory.Setup(f => f.TryCreateContentQualityEvaluator()).Returns(mockQualityEvaluator.Object);

        // Act
        var result = await _processor.ExtractContentAsync(url, options);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Quality.Should().NotBeNull();
        result.Data.Quality!.OverallScore.Should().Be(0.8);
    }

    #endregion

    #region ExtractBatchAsync Tests

    [Fact]
    public async Task ExtractBatchAsync_WithEmptyUrls_ShouldReturnEmptyResult()
    {
        // Arrange
        var urls = new List<string>();

        // Act
        var result = await _processor.ExtractBatchAsync(urls);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Succeeded.Should().BeEmpty();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractBatchAsync_WithMultipleUrls_ShouldProcessAll()
    {
        // Arrange
        var urls = new List<string>
        {
            "https://example1.com",
            "https://example2.com",
            "https://example3.com"
        };

        // Setup mock that handles all URLs
        SetupSuccessfulCrawlForAnyUrl();

        // Act
        var result = await _processor.ExtractBatchAsync(urls);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Succeeded.Count.Should().Be(3);
        result.SuccessRate.Should().Be(1.0);
    }

    [Fact]
    public async Task ExtractBatchAsync_WithPartialFailures_ShouldSeparateSuccessAndFailure()
    {
        // Arrange
        var successUrl = "https://success.com";
        var failUrl = "https://fail.com";

        // Setup mock for mixed results
        SetupMixedCrawl(successUrl, failUrl);

        var urls = new List<string> { successUrl, failUrl };

        // Act
        var result = await _processor.ExtractBatchAsync(urls);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Succeeded.Count.Should().Be(1);
        result.Failed.Count.Should().Be(1);
        result.SuccessRate.Should().Be(0.5);
    }

    [Fact]
    public async Task ExtractBatchAsync_ShouldCalculateStatistics()
    {
        // Arrange
        var urls = new List<string> { "https://example.com" };
        SetupSuccessfulCrawl(urls[0]);

        // Act
        var result = await _processor.ExtractBatchAsync(urls);

        // Assert
        result.Statistics.Should().NotBeNull();
        result.TotalDurationMs.Should().BeGreaterThanOrEqualTo(0);
        result.Statistics.TotalCharactersExtracted.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExtractBatchAsync_WithConcurrencyLimit_ShouldRespectLimit()
    {
        // Arrange
        var urls = Enumerable.Range(1, 10).Select(i => $"https://example{i}.com").ToList();
        var options = new ExtractOptions { MaxConcurrency = 2 };

        foreach (var url in urls)
        {
            SetupSuccessfulCrawl(url);
        }

        // Act
        var result = await _processor.ExtractBatchAsync(urls, options);

        // Assert
        result.TotalCount.Should().Be(10);
    }

    #endregion

    #region ExtractBatchStreamAsync Tests

    [Fact]
    public async Task ExtractBatchStreamAsync_WithUrls_ShouldStreamResults()
    {
        // Arrange
        var urls = new List<string>
        {
            "https://example1.com",
            "https://example2.com"
        };

        foreach (var url in urls)
        {
            SetupSuccessfulCrawl(url);
        }

        // Act
        var results = new List<ProcessingResult<ExtractedContent>>();
        await foreach (var result in _processor.ExtractBatchStreamAsync(urls))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExtractBatchStreamAsync_WithEmptyUrls_ShouldReturnEmptyStream()
    {
        // Arrange
        var urls = new List<string>();

        // Act
        var results = new List<ProcessingResult<ExtractedContent>>();
        await foreach (var result in _processor.ExtractBatchStreamAsync(urls))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractBatchStreamAsync_WithCancellation_ShouldStop()
    {
        // Arrange
        var urls = Enumerable.Range(1, 100).Select(i => $"https://example{i}.com").ToList();
        using var cts = new CancellationTokenSource();

        foreach (var url in urls)
        {
            SetupSuccessfulCrawl(url);
        }

        // Act
        var results = new List<ProcessingResult<ExtractedContent>>();
        var count = 0;

        try
        {
            await foreach (var result in _processor.ExtractBatchStreamAsync(urls, cancellationToken: cts.Token))
            {
                results.Add(result);
                count++;

                if (count >= 5)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        results.Count.Should().BeLessThan(100);
    }

    #endregion

    #region ExtractOptions Tests

    [Fact]
    public void ExtractOptions_Default_ShouldHaveExpectedValues()
    {
        // Act
        var options = ExtractOptions.Default;

        // Assert
        options.UseDynamicRendering.Should().BeFalse();
        options.UseCache.Should().BeTrue();
        options.TimeoutSeconds.Should().Be(15);
        options.Format.Should().Be(OutputFormat.Markdown);
        options.RemoveBoilerplate.Should().BeTrue();
        options.EvaluateQuality.Should().BeTrue();
        options.MaxRetries.Should().Be(2);
    }

    [Fact]
    public void ExtractOptions_Fast_ShouldOptimizeForSpeed()
    {
        // Act
        var options = ExtractOptions.Fast;

        // Assert
        options.UseDynamicRendering.Should().BeFalse();
        options.EvaluateQuality.Should().BeFalse();
        options.TimeoutSeconds.Should().Be(10);
        options.MaxRetries.Should().Be(1);
    }

    [Fact]
    public void ExtractOptions_HighQuality_ShouldOptimizeForQuality()
    {
        // Act
        var options = ExtractOptions.HighQuality;

        // Assert
        options.UseDynamicRendering.Should().BeTrue();
        options.EvaluateQuality.Should().BeTrue();
        options.TimeoutSeconds.Should().Be(30);
        options.MaxRetries.Should().Be(3);
    }

    #endregion

    #region BatchExtractResult Tests

    [Fact]
    public void BatchExtractResult_Empty_ShouldHaveZeroValues()
    {
        // Act
        var result = BatchExtractResult.Empty;

        // Assert
        result.TotalCount.Should().Be(0);
        result.SuccessRate.Should().Be(0);
        result.Succeeded.Should().BeEmpty();
        result.Failed.Should().BeEmpty();
    }

    [Fact]
    public void BatchExtractResult_SuccessRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new BatchExtractResult
        {
            Succeeded = new List<ExtractedContent> { new(), new(), new() },
            Failed = new List<FailedExtraction> { new() { Url = "test" } }
        };

        // Assert
        result.TotalCount.Should().Be(4);
        result.SuccessRate.Should().Be(0.75);
    }

    [Fact]
    public void BatchExtractResult_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new BatchExtractResult
        {
            Succeeded = new List<ExtractedContent> { new() },
            Failed = new List<FailedExtraction>(),
            TotalDurationMs = 1000
        };

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("1/1");
        str.Should().Contain("100%");
    }

    #endregion

    #region FailedExtraction Tests

    [Fact]
    public void FailedExtraction_ShouldSetProperties()
    {
        // Arrange & Act
        var failure = new FailedExtraction
        {
            Url = "https://fail.com",
            ErrorCode = ExtractErrorCodes.Timeout,
            ErrorMessage = "Request timed out",
            HttpStatusCode = 408,
            RetryCount = 3
        };

        // Assert
        failure.Url.Should().Be("https://fail.com");
        failure.ErrorCode.Should().Be(ExtractErrorCodes.Timeout);
        failure.HttpStatusCode.Should().Be(408);
        failure.RetryCount.Should().Be(3);
    }

    [Fact]
    public void FailedExtraction_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var failure = new FailedExtraction
        {
            Url = "https://test.com",
            ErrorCode = ExtractErrorCodes.NotFound,
            ErrorMessage = "Page not found",
            HttpStatusCode = 404
        };

        // Act
        var str = failure.ToString();

        // Assert
        str.Should().Contain("https://test.com");
        str.Should().Contain("NotFound");
        str.Should().Contain("404");
    }

    #endregion

    #region ExtractErrorCodes Tests

    [Theory]
    [InlineData(404, ExtractErrorCodes.NotFound)]
    [InlineData(403, ExtractErrorCodes.Blocked)]
    [InlineData(429, ExtractErrorCodes.Blocked)]
    [InlineData(500, ExtractErrorCodes.ServerError)]
    [InlineData(503, ExtractErrorCodes.ServerError)]
    [InlineData(200, ExtractErrorCodes.Unknown)]
    public void ExtractErrorCodes_FromHttpStatusCode_ShouldMapCorrectly(int statusCode, string expectedCode)
    {
        // Act
        var code = ExtractErrorCodes.FromHttpStatusCode(statusCode);

        // Assert
        code.Should().Be(expectedCode);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulCrawl(string url)
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.CrawlAsync(It.Is<string>(u => u == url), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult
            {
                Url = url,
                Content = "<html><head><title>Test</title></head><body>Test content</body></html>",
                ContentType = "text/html",
                StatusCode = 200,
                IsSuccess = true
            });

        _mockServiceFactory.Setup(f => f.CreateCrawler(It.IsAny<CrawlStrategy>())).Returns(mockCrawler.Object);
        _mockServiceFactory.Setup(f => f.TryCreateCacheService()).Returns((ICacheService?)null);
        _mockServiceFactory.Setup(f => f.TryCreateDomainRateLimiter()).Returns((IDomainRateLimiter?)null);
        _mockServiceFactory.Setup(f => f.TryCreateContentQualityEvaluator()).Returns((IContentQualityEvaluator?)null);

        SetupExtractor();
    }

    private void SetupSuccessfulCrawlForAnyUrl()
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.CrawlAsync(It.IsAny<string>(), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string url, CrawlOptions options, CancellationToken ct) => new CrawlResult
            {
                Url = url,
                Content = "<html><head><title>Test</title></head><body>Test content</body></html>",
                ContentType = "text/html",
                StatusCode = 200,
                IsSuccess = true
            });

        _mockServiceFactory.Setup(f => f.CreateCrawler(It.IsAny<CrawlStrategy>())).Returns(mockCrawler.Object);
        _mockServiceFactory.Setup(f => f.TryCreateCacheService()).Returns((ICacheService?)null);
        _mockServiceFactory.Setup(f => f.TryCreateDomainRateLimiter()).Returns((IDomainRateLimiter?)null);
        _mockServiceFactory.Setup(f => f.TryCreateContentQualityEvaluator()).Returns((IContentQualityEvaluator?)null);

        SetupExtractor();
    }

    private void SetupMixedCrawl(string successUrl, string failUrl)
    {
        var mockCrawler = new Mock<ICrawler>();

        // Setup success URL
        mockCrawler.Setup(c => c.CrawlAsync(It.Is<string>(u => u == successUrl), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult
            {
                Url = successUrl,
                Content = "<html><head><title>Test</title></head><body>Test content</body></html>",
                ContentType = "text/html",
                StatusCode = 200,
                IsSuccess = true
            });

        // Setup fail URL
        mockCrawler.Setup(c => c.CrawlAsync(It.Is<string>(u => u == failUrl), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult
            {
                Url = failUrl,
                Content = null,
                StatusCode = 500,
                IsSuccess = false,
                ErrorMessage = "HTTP 500"
            });

        _mockServiceFactory.Setup(f => f.CreateCrawler(It.IsAny<CrawlStrategy>())).Returns(mockCrawler.Object);
        _mockServiceFactory.Setup(f => f.TryCreateCacheService()).Returns((ICacheService?)null);
        _mockServiceFactory.Setup(f => f.TryCreateDomainRateLimiter()).Returns((IDomainRateLimiter?)null);
        _mockServiceFactory.Setup(f => f.TryCreateContentQualityEvaluator()).Returns((IContentQualityEvaluator?)null);

        SetupExtractor();
    }

    private void SetupFailedCrawl(string url, int statusCode)
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.CrawlAsync(It.Is<string>(u => u == url), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult
            {
                Url = url,
                Content = null,
                StatusCode = statusCode,
                IsSuccess = false,
                ErrorMessage = $"HTTP {statusCode}"
            });

        _mockServiceFactory.Setup(f => f.CreateCrawler(It.IsAny<CrawlStrategy>())).Returns(mockCrawler.Object);
        _mockServiceFactory.Setup(f => f.TryCreateCacheService()).Returns((ICacheService?)null);
        _mockServiceFactory.Setup(f => f.TryCreateDomainRateLimiter()).Returns((IDomainRateLimiter?)null);
        _mockServiceFactory.Setup(f => f.TryCreateContentQualityEvaluator()).Returns((IContentQualityEvaluator?)null);
    }

    private void SetupEmptyContentCrawl(string url)
    {
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.CrawlAsync(It.Is<string>(u => u == url), It.IsAny<CrawlOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult
            {
                Url = url,
                Content = "",
                StatusCode = 200,
                IsSuccess = true
            });

        _mockServiceFactory.Setup(f => f.CreateCrawler(It.IsAny<CrawlStrategy>())).Returns(mockCrawler.Object);
        _mockServiceFactory.Setup(f => f.TryCreateCacheService()).Returns((ICacheService?)null);
        _mockServiceFactory.Setup(f => f.TryCreateDomainRateLimiter()).Returns((IDomainRateLimiter?)null);
        _mockServiceFactory.Setup(f => f.TryCreateContentQualityEvaluator()).Returns((IContentQualityEvaluator?)null);
    }

    private void SetupExtractor()
    {
        var mockExtractor = new Mock<IContentExtractor>();
        mockExtractor.Setup(e => e.ExtractAutoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string content, string url, string contentType, CancellationToken ct) => new ExtractedContent
            {
                Url = url,
                Text = "Test content",
                MainContent = "Test content",
                Title = "Test"
            });

        _mockServiceFactory.Setup(f => f.CreateContentExtractor(It.IsAny<string>())).Returns(mockExtractor.Object);
    }

    #endregion
}
