using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// IntelligentCrawler 단위 테스트
/// Interface Provider 패턴 기본 구현 검증
/// </summary>
public class IntelligentCrawlerTests
{
    private readonly IntelligentCrawler _crawler;

    public IntelligentCrawlerTests()
    {
        _crawler = new IntelligentCrawler();
    }

    #region CrawlAsync Tests

    [Fact]
    public async Task CrawlAsync_WithValidUrl_ShouldReturnSuccessResult()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Be(url);
        result.IsSuccessful.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.ContentType.Should().Be("text/html");
    }

    [Fact]
    public async Task CrawlAsync_ShouldReturnBasicContentWithUrl()
    {
        // Arrange
        var url = "https://test.com/page";

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Content.Should().Contain(url);
        result.Content.Should().Contain("Basic Intelligent crawl result");
    }

    [Fact]
    public async Task CrawlAsync_WithOptions_ShouldAcceptOptions()
    {
        // Arrange
        var url = "https://example.com";
        var options = new CrawlOptions
        {
            MaxDepth = 5,
            MaxPages = 100
        };

        // Act
        var result = await _crawler.CrawlAsync(url, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlAsync_WithCancellationToken_ShouldNotThrow()
    {
        // Arrange
        var url = "https://example.com";
        var cts = new CancellationTokenSource();

        // Act
        var result = await _crawler.CrawlAsync(url, cancellationToken: cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlAsync_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result1 = await _crawler.CrawlAsync(url);
        var result2 = await _crawler.CrawlAsync(url);

        // Assert
        result1.Url.Should().Be(result2.Url);
        result1.IsSuccessful.Should().Be(result2.IsSuccessful);
        result1.StatusCode.Should().Be(result2.StatusCode);
    }

    #endregion

    #region CrawlWebsiteAsync Tests

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldYieldSingleResult()
    {
        // Arrange
        var url = "https://example.com";
        var results = new List<CrawlResult>();

        // Act
        await foreach (var result in _crawler.CrawlWebsiteAsync(url))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Url.Should().Be(url);
        results[0].IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlWebsiteAsync_WithOptions_ShouldAcceptOptions()
    {
        // Arrange
        var url = "https://example.com";
        var options = new CrawlOptions { MaxDepth = 3 };
        var results = new List<CrawlResult>();

        // Act
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CrawlWebsiteAsync_WithCancellation_ShouldBeInterruptible()
    {
        // Arrange
        var url = "https://example.com";
        var cts = new CancellationTokenSource();
        var results = new List<CrawlResult>();

        // Act & Assert (should complete without throwing)
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, cancellationToken: cts.Token))
        {
            results.Add(result);
        }

        results.Should().NotBeEmpty();
    }

    #endregion

    #region CrawlSitemapAsync Tests

    [Fact]
    public async Task CrawlSitemapAsync_ShouldYieldSingleResult()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var results = new List<CrawlResult>();

        // Act
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Url.Should().Be(sitemapUrl);
        results[0].IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlSitemapAsync_WithOptions_ShouldAcceptOptions()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var options = new CrawlOptions();
        var results = new List<CrawlResult>();

        // Act
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CrawlSitemapAsync_WithCancellation_ShouldBeInterruptible()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var cts = new CancellationTokenSource();
        var results = new List<CrawlResult>();

        // Act
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl, cancellationToken: cts.Token))
        {
            results.Add(result);
        }

        // Assert
        results.Should().NotBeEmpty();
    }

    #endregion

    #region GetRobotsTxtAsync Tests

    [Fact]
    public async Task GetRobotsTxtAsync_ShouldReturnBasicRobotsInfo()
    {
        // Arrange
        var baseUrl = "https://example.com";
        var userAgent = "TestBot";

        // Act
        var result = await _crawler.GetRobotsTxtAsync(baseUrl, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeNullOrEmpty();
        result.Content.Should().Contain("robots.txt");
    }

    [Fact]
    public async Task GetRobotsTxtAsync_WithDifferentUrls_ShouldReturnConsistentResults()
    {
        // Arrange
        var baseUrl1 = "https://example.com";
        var baseUrl2 = "https://test.com";
        var userAgent = "TestBot";

        // Act
        var result1 = await _crawler.GetRobotsTxtAsync(baseUrl1, userAgent);
        var result2 = await _crawler.GetRobotsTxtAsync(baseUrl2, userAgent);

        // Assert
        result1.Content.Should().Be(result2.Content);
    }

    [Fact]
    public async Task GetRobotsTxtAsync_WithCancellation_ShouldNotThrow()
    {
        // Arrange
        var baseUrl = "https://example.com";
        var userAgent = "TestBot";
        var cts = new CancellationTokenSource();

        // Act
        var result = await _crawler.GetRobotsTxtAsync(baseUrl, userAgent, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region IsUrlAllowedAsync Tests

    [Fact]
    public async Task IsUrlAllowedAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var url = "https://example.com/page";
        var userAgent = "TestBot";

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, userAgent);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://test.com/path")]
    [InlineData("https://subdomain.example.com/deep/path")]
    public async Task IsUrlAllowedAsync_WithVariousUrls_ShouldAlwaysReturnTrue(string url)
    {
        // Arrange
        var userAgent = "TestBot";

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, userAgent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUrlAllowedAsync_WithEmptyUrl_ShouldStillReturnTrue()
    {
        // Arrange
        var url = "";
        var userAgent = "TestBot";

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, userAgent);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ExtractLinks Tests

    [Fact]
    public void ExtractLinks_ShouldReturnEmptyList()
    {
        // Arrange
        var htmlContent = "<html><body><a href='https://example.com'>Link</a></body></html>";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().NotBeNull();
        links.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithNullContent_ShouldReturnEmptyList()
    {
        // Arrange
        string? htmlContent = null;
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent!, baseUrl);

        // Assert
        links.Should().NotBeNull();
        links.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithEmptyContent_ShouldReturnEmptyList()
    {
        // Arrange
        var htmlContent = "";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().NotBeNull();
        links.Should().BeEmpty();
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_ShouldReturnZeroStatistics()
    {
        // Act
        var stats = _crawler.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalRequests.Should().Be(0);
        stats.SuccessfulRequests.Should().Be(0);
        stats.FailedRequests.Should().Be(0);
    }

    [Fact]
    public void GetStatistics_AfterMultipleCrawls_ShouldStillReturnZero()
    {
        // Arrange - crawl multiple times
        _ = _crawler.CrawlAsync("https://example.com").Result;
        _ = _crawler.CrawlAsync("https://test.com").Result;

        // Act
        var stats = _crawler.GetStatistics();

        // Assert - statistics are not tracked in basic implementation
        stats.TotalRequests.Should().Be(0);
        stats.SuccessfulRequests.Should().Be(0);
        stats.FailedRequests.Should().Be(0);
    }

    #endregion

    #region Interface Provider Pattern Tests

    [Fact]
    public void IntelligentCrawler_ShouldBeAssignableToCrawlerInterface()
    {
        // Assert
        _crawler.Should().BeAssignableTo<ICrawler>();
    }

    [Fact]
    public async Task IntelligentCrawler_ShouldProvideBasicStubImplementation()
    {
        // Act
        var crawlResult = await _crawler.CrawlAsync("https://example.com");
        var robotsInfo = await _crawler.GetRobotsTxtAsync("https://example.com", "Bot");
        var isAllowed = await _crawler.IsUrlAllowedAsync("https://example.com", "Bot");
        var links = _crawler.ExtractLinks("<html></html>", "https://example.com");
        var stats = _crawler.GetStatistics();

        // Assert - all methods should return valid stub data
        crawlResult.Should().NotBeNull();
        robotsInfo.Should().NotBeNull();
        isAllowed.Should().BeTrue();
        links.Should().NotBeNull();
        stats.Should().NotBeNull();
    }

    #endregion
}
