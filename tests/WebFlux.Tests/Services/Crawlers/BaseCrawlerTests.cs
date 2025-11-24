using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using WebFlux.Services;
using Xunit;
using FluentAssertions;
using System.Net;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// BaseCrawler 단위 테스트
/// 크롤러 핵심 기능 검증 (HTTP 크롤링, 링크 추출, robots.txt, 통계)
/// </summary>
public class BaseCrawlerTests : IDisposable
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly TestCrawler _crawler;

    public BaseCrawlerTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _crawler = new TestCrawler(_mockHttpClient.Object, _mockEventPublisher.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TestCrawler(null!, _mockEventPublisher.Object));

        ex.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TestCrawler(_mockHttpClient.Object, null!));

        ex.ParamName.Should().Be("eventPublisher");
    }

    #endregion

    #region CrawlAsync Tests

    [Fact]
    public async Task CrawlAsync_WithValidUrl_ShouldReturnSuccessResult()
    {
        // Arrange
        var url = "https://example.com";
        var htmlContent = "<html><head><title>Test Page</title></head><body><a href='/page1'>Link 1</a></body></html>";

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
        result.DiscoveredLinks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CrawlAsync_WithNullOrEmptyUrl_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _crawler.CrawlAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _crawler.CrawlAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _crawler.CrawlAsync("   "));
    }

    [Fact]
    public async Task CrawlAsync_WithHttpError_ShouldReturnErrorResult()
    {
        // Arrange
        var url = "https://example.com/notfound";

        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(""),
            RequestMessage = new HttpRequestMessage { RequestUri = new Uri(url) },
            ReasonPhrase = "Not Found"
        };

        _mockHttpClient.Setup(c => c.GetAsync(url, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.ErrorMessage.Should().Be("Not Found");
    }

    [Fact]
    public async Task CrawlAsync_WithException_ShouldReturnErrorResult()
    {
        // Arrange
        var url = "https://example.com";
        var exceptionMessage = "Network error";

        _mockHttpClient.Setup(c => c.GetAsync(url, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException(exceptionMessage));

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(0);
        result.ErrorMessage.Should().Be(exceptionMessage);
        result.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task CrawlAsync_ShouldPublishStartEvent()
    {
        // Arrange
        var url = "https://example.com";
        SetupSuccessfulHttpResponse(url, "<html></html>");

        // Act
        await _crawler.CrawlAsync(url);

        // Assert
        _mockEventPublisher.Verify(
            e => e.PublishAsync(
                It.Is<UrlProcessingStartedEvent>(evt => evt.Url == url),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CrawlAsync_ShouldUpdateStatistics()
    {
        // Arrange
        var url = "https://example.com";
        SetupSuccessfulHttpResponse(url, "<html></html>");

        // Act
        await _crawler.CrawlAsync(url);
        var stats = _crawler.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(1);
        stats.SuccessfulRequests.Should().Be(1);
        stats.FailedRequests.Should().Be(0);
    }

    #endregion

    #region CrawlWebsiteAsync Tests

    [Fact]
    public async Task CrawlWebsiteAsync_WithValidStartUrl_ShouldReturnResults()
    {
        // Arrange
        var startUrl = "https://example.com";
        var htmlContent = @"
            <html>
                <body>
                    <a href='https://example.com/page1'>Page 1</a>
                    <a href='https://example.com/page2'>Page 2</a>
                </body>
            </html>";

        SetupSuccessfulHttpResponse(startUrl, htmlContent);
        SetupSuccessfulHttpResponse("https://example.com/page1", "<html><body>Page 1</body></html>");
        SetupSuccessfulHttpResponse("https://example.com/page2", "<html><body>Page 2</body></html>");

        var options = new CrawlOptions
        {
            MaxPages = 3,
            MaxDepth = 1,
            DelayMs = 0
        };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(startUrl, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCountGreaterThan(0);
        results.Should().HaveCountLessThanOrEqualTo(3);
        results[0].Url.Should().Be(startUrl);
        results[0].Depth.Should().Be(0);
    }

    [Fact]
    public async Task CrawlWebsiteAsync_WithNullOrEmptyUrl_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in _crawler.CrawlWebsiteAsync(null!))
            {
            }
        });

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in _crawler.CrawlWebsiteAsync(""))
            {
            }
        });
    }

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldRespectMaxDepth()
    {
        // Arrange
        var startUrl = "https://example.com";
        SetupSuccessfulHttpResponse(startUrl, @"<html><body><a href='https://example.com/level1'>Level 1</a></body></html>");
        SetupSuccessfulHttpResponse("https://example.com/level1", @"<html><body><a href='https://example.com/level2'>Level 2</a></body></html>");
        SetupSuccessfulHttpResponse("https://example.com/level2", "<html><body>Deep page</body></html>");

        var options = new CrawlOptions
        {
            MaxDepth = 1,
            MaxPages = 100,
            DelayMs = 0
        };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(startUrl, options))
        {
            results.Add(result);
        }

        // Assert
        results.All(r => r.Depth <= 1).Should().BeTrue();
    }

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldRespectMaxPages()
    {
        // Arrange
        var startUrl = "https://example.com";
        var htmlWithLinks = @"<html><body>" +
            string.Join("", Enumerable.Range(1, 20).Select(i => $"<a href='https://example.com/page{i}'>Page {i}</a>")) +
            "</body></html>";

        SetupSuccessfulHttpResponse(startUrl, htmlWithLinks);
        for (int i = 1; i <= 20; i++)
        {
            SetupSuccessfulHttpResponse($"https://example.com/page{i}", $"<html><body>Page {i}</body></html>");
        }

        var options = new CrawlOptions
        {
            MaxPages = 5,
            MaxDepth = 2,
            DelayMs = 0
        };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(startUrl, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldNotVisitSameUrlTwice()
    {
        // Arrange
        var startUrl = "https://example.com";
        var htmlContent = @"
            <html><body>
                <a href='https://example.com/page1'>Page 1</a>
                <a href='https://example.com/page1'>Page 1 Again</a>
            </body></html>";

        SetupSuccessfulHttpResponse(startUrl, htmlContent);
        SetupSuccessfulHttpResponse("https://example.com/page1", htmlContent);

        var options = new CrawlOptions
        {
            MaxPages = 10,
            MaxDepth = 2,
            DelayMs = 0
        };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(startUrl, options))
        {
            results.Add(result);
        }

        // Assert
        var urls = results.Select(r => r.Url).ToList();
        urls.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CrawlWebsiteAsync_WithDelayOption_ShouldDelayBetweenRequests()
    {
        // Arrange
        var startUrl = "https://example.com";
        SetupSuccessfulHttpResponse(startUrl, @"<html><body><a href='https://example.com/page1'>Page 1</a></body></html>");
        SetupSuccessfulHttpResponse("https://example.com/page1", "<html><body>Page 1</body></html>");

        var options = new CrawlOptions
        {
            MaxPages = 2,
            MaxDepth = 1,
            DelayMs = 100
        };

        // Act
        var startTime = DateTime.UtcNow;
        await foreach (var _ in _crawler.CrawlWebsiteAsync(startUrl, options))
        {
        }
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assert
        elapsed.Should().BeGreaterThan(50); // At least some delay occurred
    }

    #endregion

    #region CrawlSitemapAsync Tests

    [Fact]
    public async Task CrawlSitemapAsync_WithValidSitemap_ShouldReturnResults()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var sitemapContent = @"<?xml version='1.0' encoding='UTF-8'?>
            <urlset xmlns='http://www.sitemaps.org/schemas/sitemap/0.9'>
                <url><loc>https://example.com/page1</loc></url>
                <url><loc>https://example.com/page2</loc></url>
            </urlset>";

        SetupSuccessfulHttpResponse(sitemapUrl, sitemapContent);
        SetupSuccessfulHttpResponse("https://example.com/page1", "<html><body>Page 1</body></html>");
        SetupSuccessfulHttpResponse("https://example.com/page2", "<html><body>Page 2</body></html>");

        var options = new CrawlOptions { DelayMs = 0 };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlSitemapAsync(sitemapUrl, options))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.Url == "https://example.com/page1");
        results.Should().Contain(r => r.Url == "https://example.com/page2");
    }

    [Fact]
    public async Task CrawlSitemapAsync_WithNullOrEmptyUrl_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in _crawler.CrawlSitemapAsync(null!))
            {
            }
        });
    }

    [Fact]
    public async Task CrawlSitemapAsync_ShouldRespectDelay()
    {
        // Arrange
        var sitemapUrl = "https://example.com/sitemap.xml";
        var sitemapContent = @"<?xml version='1.0' encoding='UTF-8'?>
            <urlset><url><loc>https://example.com/page1</loc></url></urlset>";

        SetupSuccessfulHttpResponse(sitemapUrl, sitemapContent);
        SetupSuccessfulHttpResponse("https://example.com/page1", "<html></html>");

        var options = new CrawlOptions { DelayMs = 50 };

        // Act
        var startTime = DateTime.UtcNow;
        await foreach (var _ in _crawler.CrawlSitemapAsync(sitemapUrl, options))
        {
        }
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assert
        elapsed.Should().BeGreaterThan(25);
    }

    #endregion

    #region GetRobotsTxtAsync Tests

    [Fact]
    public async Task GetRobotsTxtAsync_WithValidRobotsTxt_ShouldParseCorrectly()
    {
        // Arrange
        var baseUrl = "https://example.com";
        var robotsTxtContent = @"
User-agent: *
Disallow: /admin/
Allow: /public/
Crawl-delay: 10
Sitemap: https://example.com/sitemap.xml
";

        SetupSuccessfulHttpResponse("https://example.com/robots.txt", robotsTxtContent);

        // Act
        var result = await _crawler.GetRobotsTxtAsync(baseUrl, "*");

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be(robotsTxtContent);
        result.Rules.Should().ContainKey("*");
        result.Rules["*"].DisallowedPaths.Should().Contain("/admin/");
        result.Rules["*"].AllowedPaths.Should().Contain("/public/");
        result.Rules["*"].CrawlDelay.Should().Be(10);
        result.Sitemaps.Should().Contain("https://example.com/sitemap.xml");
    }

    [Fact]
    public async Task GetRobotsTxtAsync_WhenNotFound_ShouldReturnEmptyRobotsTxtInfo()
    {
        // Arrange
        var baseUrl = "https://example.com";

        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _mockHttpClient.Setup(c => c.GetAsync(
            It.Is<string>(url => url.Contains("robots.txt")),
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _crawler.GetRobotsTxtAsync(baseUrl, "*");

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeNull();
        result.Rules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRobotsTxtAsync_WithMultipleUserAgents_ShouldParseAllRules()
    {
        // Arrange
        var baseUrl = "https://example.com";
        var robotsTxtContent = @"
User-agent: Googlebot
Disallow: /private/
Crawl-delay: 5

User-agent: *
Disallow: /admin/
Crawl-delay: 10
";

        SetupSuccessfulHttpResponse("https://example.com/robots.txt", robotsTxtContent);

        // Act
        var result = await _crawler.GetRobotsTxtAsync(baseUrl, "*");

        // Assert
        result.Rules.Should().HaveCount(2);
        result.Rules.Should().ContainKey("Googlebot");
        result.Rules.Should().ContainKey("*");
        result.Rules["Googlebot"].DisallowedPaths.Should().Contain("/private/");
        result.Rules["*"].DisallowedPaths.Should().Contain("/admin/");
    }

    #endregion

    #region IsUrlAllowedAsync Tests

    [Fact]
    public async Task IsUrlAllowedAsync_WithAllowedPath_ShouldReturnTrue()
    {
        // Arrange
        var baseUrl = "https://example.com";
        var url = "https://example.com/public/page";
        var robotsTxtContent = @"
User-agent: *
Allow: /public/
Disallow: /
";

        SetupSuccessfulHttpResponse("https://example.com/robots.txt", robotsTxtContent);

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, "*");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUrlAllowedAsync_WithDisallowedPath_ShouldReturnFalse()
    {
        // Arrange
        var url = "https://example.com/admin/secret";
        var robotsTxtContent = @"
User-agent: *
Disallow: /admin/
";

        SetupSuccessfulHttpResponse("https://example.com/robots.txt", robotsTxtContent);

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, "*");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUrlAllowedAsync_WhenRobotsTxtNotFound_ShouldReturnTrue()
    {
        // Arrange
        var url = "https://example.com/any/path";

        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        _mockHttpClient.Setup(c => c.GetAsync(
            It.Is<string>(u => u.Contains("robots.txt")),
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _crawler.IsUrlAllowedAsync(url, "*");

        // Assert
        result.Should().BeTrue(); // Default to allow when robots.txt not found
    }

    #endregion

    #region ExtractLinks Tests

    [Fact]
    public void ExtractLinks_WithValidHtml_ShouldExtractAllLinks()
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
        links.Should().NotBeEmpty();
        links.Should().Contain("https://example.com/page1");
        links.Should().Contain("https://example.com/page2");
        links.Should().Contain("https://external.com/");
    }

    [Fact]
    public void ExtractLinks_WithRelativeUrls_ShouldConvertToAbsolute()
    {
        // Arrange
        var htmlContent = @"<html><body><a href='/relative/path'>Link</a></body></html>";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().Contain("https://example.com/relative/path");
    }

    [Fact]
    public void ExtractLinks_WithEmptyHtml_ShouldReturnEmptyList()
    {
        // Arrange
        var htmlContent = "";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithNoLinks_ShouldReturnEmptyList()
    {
        // Arrange
        var htmlContent = "<html><body><p>No links here</p></body></html>";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().BeEmpty();
    }

    [Fact]
    public void ExtractLinks_WithInvalidUrls_ShouldIgnoreThem()
    {
        // Arrange
        var htmlContent = @"
            <html><body>
                <a href='https://valid.com'>Valid</a>
                <a href='javascript:void(0)'>Invalid</a>
                <a href='#anchor'>Anchor</a>
            </body></html>";
        var baseUrl = "https://example.com";

        // Act
        var links = _crawler.ExtractLinks(htmlContent, baseUrl);

        // Assert
        links.Should().Contain("https://valid.com/");
        links.Should().NotContain(l => l.StartsWith("javascript:"));
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_InitialState_ShouldReturnEmptyStatistics()
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
    public async Task GetStatistics_AfterSuccessfulCrawl_ShouldUpdateCorrectly()
    {
        // Arrange
        var url = "https://example.com";
        SetupSuccessfulHttpResponse(url, "<html></html>");

        // Act
        await _crawler.CrawlAsync(url);
        var stats = _crawler.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(1);
        stats.SuccessfulRequests.Should().Be(1);
        stats.FailedRequests.Should().Be(0);
        stats.RequestsByDomain.Should().ContainKey("example.com");
        stats.StatusCodeDistribution.Should().ContainKey(200);
    }

    [Fact]
    public async Task GetStatistics_AfterFailedCrawl_ShouldUpdateCorrectly()
    {
        // Arrange
        var url = "https://example.com";
        _mockHttpClient.Setup(c => c.GetAsync(url, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        await _crawler.CrawlAsync(url);
        var stats = _crawler.GetStatistics();

        // Assert
        stats.TotalRequests.Should().Be(1);
        stats.SuccessfulRequests.Should().Be(0);
        stats.FailedRequests.Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_ShouldTrackRequestsByDomain()
    {
        // Arrange
        SetupSuccessfulHttpResponse("https://example1.com", "<html></html>");
        SetupSuccessfulHttpResponse("https://example2.com", "<html></html>");
        SetupSuccessfulHttpResponse("https://example1.com/page", "<html></html>");

        // Act
        await _crawler.CrawlAsync("https://example1.com");
        await _crawler.CrawlAsync("https://example2.com");
        await _crawler.CrawlAsync("https://example1.com/page");
        var stats = _crawler.GetStatistics();

        // Assert
        stats.RequestsByDomain["example1.com"].Should().Be(2);
        stats.RequestsByDomain["example2.com"].Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulHttpResponse(string url, string content)
    {
        // HttpResponseMessage는 mock 불가능한 프로퍼티들이 있으므로 실제 객체 생성
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content),
            RequestMessage = new HttpRequestMessage { RequestUri = new Uri(url) }
        };

        _mockHttpClient.Setup(c => c.GetAsync(
            It.Is<string>(u => u == url),
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    public void Dispose()
    {
        _crawler?.Dispose();
    }

    #endregion

    #region Test Crawler Implementation

    /// <summary>
    /// BaseCrawler를 테스트하기 위한 구체 클래스
    /// </summary>
    private class TestCrawler : BaseCrawler
    {
        public TestCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
            : base(httpClient, eventPublisher)
        {
        }
    }

    #endregion
}
