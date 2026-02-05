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
/// DepthFirstCrawler 단위 테스트
/// BaseCrawler를 상속하며 Stack 기반 깊이 우선 탐색을 사용합니다.
/// </summary>
public class DepthFirstCrawlerTests : IDisposable
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly DepthFirstCrawler _crawler;

    public DepthFirstCrawlerTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _crawler = new DepthFirstCrawler(_mockHttpClient.Object, _mockEventPublisher.Object);
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
            new DepthFirstCrawler(null!, _mockEventPublisher.Object));

        ex.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DepthFirstCrawler(_mockHttpClient.Object, null!));

        ex.ParamName.Should().Be("eventPublisher");
    }

    #endregion

    #region CrawlAsync Tests

    [Fact]
    public async Task CrawlAsync_WithValidUrl_ShouldReturnSuccessResult()
    {
        // Arrange
        var url = "https://example.com";
        var htmlContent = "<html><head><title>Test</title></head><body><a href='/page1'>Link</a></body></html>";

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

    #region CrawlWebsiteAsync Tests - DFS 특성 검증

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldUseDepthFirstTraversal()
    {
        // Arrange - 링크 구조 설정
        // Root -> Page1 (첫 번째 링크) -> Page1A (깊이 우선이면 Page1A가 Page2보다 먼저)
        // Root -> Page2

        var rootUrl = "https://example.com";
        var rootHtml = "<html><body><a href='/page1'>Page1</a><a href='/page2'>Page2</a></body></html>";
        var page1Html = "<html><body><a href='/page1a'>Page1A</a></body></html>";
        var page1aHtml = "<html><body>Page1A Content (leaf)</body></html>";
        var page2Html = "<html><body>Page2 Content (leaf)</body></html>";

        SetupMockResponse(rootUrl, rootHtml);
        SetupMockResponse("https://example.com/page1", page1Html);
        SetupMockResponse("https://example.com/page1a", page1aHtml);
        SetupMockResponse("https://example.com/page2", page2Html);

        var options = new CrawlOptions { MaxDepth = 3, MaxPages = 10 };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(rootUrl, options))
        {
            results.Add(result);
        }

        // Assert - DFS는 깊이 우선으로 방문
        // Root -> Page1 -> Page1A -> Page2 순서 (Page1의 자식을 먼저 탐색)
        results.Should().HaveCountGreaterThanOrEqualTo(3);
        results[0].Url.Should().Be(rootUrl);

        // Page1이 먼저 방문되고, Page1A가 Page2보다 먼저 방문되어야 함 (DFS)
        var page1Index = results.FindIndex(r => r.Url.Contains("/page1") && !r.Url.Contains("/page1a"));
        var page1aIndex = results.FindIndex(r => r.Url.Contains("/page1a"));
        var page2Index = results.FindIndex(r => r.Url.Contains("/page2"));

        // Page1 다음에 Page1A가 와야 하고, Page2는 그 이후 (DFS 특성)
        if (page1Index >= 0 && page1aIndex >= 0 && page2Index >= 0)
        {
            page1Index.Should().BeLessThan(page1aIndex, "Page1 should be visited before Page1A");
            page1aIndex.Should().BeLessThan(page2Index, "Page1A should be visited before Page2 (DFS)");
        }
    }

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldRespectMaxDepth()
    {
        // Arrange
        var url = "https://example.com";
        var level0 = "<html><body><a href='/level1'>Level1</a></body></html>";
        var level1 = "<html><body><a href='/level2'>Level2</a></body></html>";
        var level2 = "<html><body><a href='/level3'>Level3</a></body></html>";
        var level3 = "<html><body>Level3 Content</body></html>";

        SetupMockResponse(url, level0);
        SetupMockResponse("https://example.com/level1", level1);
        SetupMockResponse("https://example.com/level2", level2);
        SetupMockResponse("https://example.com/level3", level3);

        var options = new CrawlOptions { MaxDepth = 1, MaxPages = 10 };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, options))
        {
            results.Add(result);
        }

        // Assert - MaxDepth=1이면 depth 0과 1만 방문
        results.Should().HaveCountLessThanOrEqualTo(2);
        results.All(r => r.Depth <= 1).Should().BeTrue();
    }

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldRespectMaxPages()
    {
        // Arrange
        var url = "https://example.com";
        var htmlContent = @"<html><body>
            <a href='/page1'>P1</a>
            <a href='/page2'>P2</a>
            <a href='/page3'>P3</a>
            <a href='/page4'>P4</a>
            <a href='/page5'>P5</a>
        </body></html>";

        SetupMockResponse(url, htmlContent);
        for (int i = 1; i <= 5; i++)
        {
            SetupMockResponse($"https://example.com/page{i}", $"<html><body>Page {i}</body></html>");
        }

        var options = new CrawlOptions { MaxPages = 3, MaxDepth = 2 };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, options))
        {
            results.Add(result);
        }

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(3);
    }

    #endregion

    #region Helper Methods

    private void SetupMockResponse(string url, string content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content),
            RequestMessage = new HttpRequestMessage { RequestUri = new Uri(url) }
        };

        _mockHttpClient.Setup(c => c.GetAsync(url, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    #endregion
}
