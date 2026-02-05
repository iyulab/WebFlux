using System.Net;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// SitemapCrawler 단위 테스트
/// BaseCrawler를 상속하며 sitemap.xml 기반 크롤링을 수행합니다.
/// </summary>
public class SitemapCrawlerTests : IDisposable
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly SitemapCrawler _crawler;

    public SitemapCrawlerTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _crawler = new SitemapCrawler(_mockHttpClient.Object, _mockEventPublisher.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SitemapCrawler(null!, _mockEventPublisher.Object));

        ex.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new SitemapCrawler(_mockHttpClient.Object, null!));

        ex.ParamName.Should().Be("eventPublisher");
    }

    #endregion

    #region CrawlAsync Tests

    [Fact]
    public async Task CrawlAsync_WithValidUrl_ShouldReturnSuccessResult()
    {
        // Arrange
        var url = "https://example.com";
        var htmlContent = "<html><head><title>Test</title></head><body>Content</body></html>";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(htmlContent),
            RequestMessage = new HttpRequestMessage { RequestUri = new Uri(url) }
        };

        _mockHttpClient.Setup(c => c.GetAsync(url, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Url.Should().Be(url);
        result.StatusCode.Should().Be(200);
        result.HtmlContent.Should().Be(htmlContent);
    }

    #endregion

    #region CrawlSitemapAsync Tests

    [Fact]
    public async Task CrawlSitemapAsync_WithValidSitemap_ShouldCrawlAllUrls()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var sitemapXml = @"<?xml version='1.0' encoding='UTF-8'?>
            <urlset xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>
                <url><loc>https://example.com/page1</loc></url>
                <url><loc>https://example.com/page2</loc></url>
                <url><loc>https://example.com/page3</loc></url>
            </urlset>";

        SetupMockResponse(sitemapUrl, sitemapXml, "application/xml");
        SetupMockResponse("https://example.com/page1", "<html><body>Page 1</body></html>");
        SetupMockResponse("https://example.com/page2", "<html><body>Page 2</body></html>");
        SetupMockResponse("https://example.com/page3", "<html><body>Page 3</body></html>");

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Select(r => r.Url).Should().Contain("https://example.com/page1");
        results.Select(r => r.Url).Should().Contain("https://example.com/page2");
        results.Select(r => r.Url).Should().Contain("https://example.com/page3");
    }

    [Fact]
    public async Task CrawlSitemapAsync_WithEmptySitemap_ShouldReturnEmpty()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var sitemapXml = @"<?xml version='1.0' encoding='UTF-8'?>
            <urlset xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>
            </urlset>";

        SetupMockResponse(sitemapUrl, sitemapXml, "application/xml");

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task CrawlSitemapAsync_WithSitemapIndex_ShouldProcessNestedSitemaps()
    {
        // Arrange
        var sitemapIndexUrl = "https://example.com/sitemap-index.xml";
        var sitemapIndex = @"<?xml version='1.0' encoding='UTF-8'?>
            <sitemapindex xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>
                <sitemap><loc>https://example.com/sitemap1.xml</loc></sitemap>
            </sitemapindex>";

        var sitemap1 = @"<?xml version='1.0' encoding='UTF-8'?>
            <urlset xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>
                <url><loc>https://example.com/page1</loc></url>
            </urlset>";

        SetupMockResponse(sitemapIndexUrl, sitemapIndex, "application/xml");
        SetupMockResponse("https://example.com/sitemap1.xml", sitemap1, "application/xml");
        SetupMockResponse("https://example.com/page1", "<html><body>Page 1</body></html>");

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapIndexUrl))
        {
            results.Add(result);
        }

        // Assert
        results.Should().NotBeEmpty();
    }

    #endregion

    #region CrawlWebsiteAsync Tests

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldUseBreadthFirstByDefault()
    {
        // SitemapCrawler는 CrawlWebsiteAsync에서 BaseCrawler의 기본 BFS 동작을 사용
        // Arrange
        var url = "https://example.com";
        var htmlContent = "<html><body><a href='/page1'>Page1</a></body></html>";

        SetupMockResponse(url, htmlContent);
        SetupMockResponse("https://example.com/page1", "<html><body>Page 1</body></html>");

        var options = new CrawlOptions { MaxDepth = 1, MaxPages = 10 };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().NotBeEmpty();
        results[0].Url.Should().Be(url);
    }

    #endregion

    #region Helper Methods

    private void SetupMockResponse(string url, string content, string? contentType = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content),
            RequestMessage = new HttpRequestMessage { RequestUri = new Uri(url) }
        };

        if (!string.IsNullOrEmpty(contentType))
        {
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        _mockHttpClient.Setup(c => c.GetAsync(url, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    #endregion
}
