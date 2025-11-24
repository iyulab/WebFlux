using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// SitemapCrawler 단위 테스트
/// 기본 구현 검증 (Interface Provider 패턴)
/// </summary>
public class SitemapCrawlerTests
{
    private readonly SitemapCrawler _crawler;

    public SitemapCrawlerTests()
    {
        _crawler = new SitemapCrawler();
    }

    #region CrawlAsync Tests

    [Fact]
    public async Task CrawlAsync_WithValidUrl_ShouldReturnBasicResult()
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
        result.Content.Should().Contain("Sitemap");
        result.Content.Should().Contain(url);
    }

    [Fact]
    public async Task CrawlAsync_WithOptions_ShouldIgnoreOptions()
    {
        // Arrange
        var url = "https://example.com";
        var options = new CrawlOptions
        {
            MaxPages = 10,
            MaxDepth = 5
        };

        // Act
        var result = await _crawler.CrawlAsync(url, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var url = "https://example.com";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _crawler.CrawlAsync(url, cancellationToken: cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
    }

    #endregion

    #region CrawlWebsiteAsync Tests

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldReturnSingleResult()
    {
        // Arrange
        var startUrl = "https://example.com";

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(startUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Url.Should().Be(startUrl);
    }

    [Fact]
    public async Task CrawlWebsiteAsync_WithOptions_ShouldIgnoreOptions()
    {
        // Arrange
        var startUrl = "https://example.com";
        var options = new CrawlOptions
        {
            MaxPages = 100,
            MaxDepth = 10
        };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(startUrl, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1); // Basic implementation returns only start URL
    }

    #endregion

    #region CrawlSitemapAsync Tests

    [Fact]
    public async Task CrawlSitemapAsync_ShouldReturnSingleResult()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Url.Should().Be(sitemapUrl);
    }

    [Fact]
    public async Task CrawlSitemapAsync_WithMultipleUrls_ShouldReturnSingleResult()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap-index.xml";

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1); // Basic implementation returns only sitemap URL
    }

    #endregion

    #region GetRobotsTxtAsync Tests

    [Fact]
    public async Task GetRobotsTxtAsync_ShouldReturnBasicRobotsTxtInfo()
    {
        // Arrange
        var baseUrl = "https://example.com";
        var userAgent = "*";

        // Act
        var result = await _crawler.GetRobotsTxtAsync(baseUrl, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Contain("robots.txt");
    }

    #endregion

    #region IsUrlAllowedAsync Tests

    [Fact]
    public async Task IsUrlAllowedAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var url = "https://example.com/any/path";
        var userAgent = "*";

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, userAgent);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/admin")]
    [InlineData("https://example.com/private/secret")]
    public async Task IsUrlAllowedAsync_WithVariousUrls_ShouldAlwaysReturnTrue(string url)
    {
        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, "*");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ExtractLinks Tests

    [Fact]
    public void ExtractLinks_ShouldReturnEmptyList()
    {
        // Arrange
        var htmlContent = "<html><body><a href='/page1'>Link</a></body></html>";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().BeEmpty(); // Basic implementation returns no links
    }

    [Fact]
    public void ExtractLinks_WithComplexHtml_ShouldReturnEmptyList()
    {
        // Arrange
        var htmlContent = @"
            <html>
                <body>
                    <a href='/page1'>Page 1</a>
                    <a href='/page2'>Page 2</a>
                    <a href='https://external.com'>External</a>
                </body>
            </html>";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().BeEmpty();
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_ShouldReturnEmptyStatistics()
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
    public async Task GetStatistics_AfterCrawling_ShouldStillReturnZero()
    {
        // Arrange
        await _crawler.CrawlAsync("https://example.com");

        // Act
        var stats = _crawler.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(0); // Basic implementation doesn't track statistics
    }

    #endregion
}
