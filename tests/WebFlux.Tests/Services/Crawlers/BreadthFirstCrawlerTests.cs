using System.Net;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// BreadthFirstCrawler 단위 테스트
/// BaseCrawler를 상속하며 Queue 기반 너비 우선 탐색을 사용합니다.
/// </summary>
public class BreadthFirstCrawlerTests : IDisposable
{
    private readonly IHttpClientService _mockHttpClient;
    private readonly IEventPublisher _mockEventPublisher;
    private readonly BreadthFirstCrawler _crawler;

    public BreadthFirstCrawlerTests()
    {
        _mockHttpClient = Substitute.For<IHttpClientService>();
        _mockEventPublisher = Substitute.For<IEventPublisher>();
        _crawler = new BreadthFirstCrawler(_mockHttpClient, _mockEventPublisher);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BreadthFirstCrawler(null!, _mockEventPublisher));

        ex.ParamName.Should().Be("httpClient");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BreadthFirstCrawler(_mockHttpClient, null!));

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

        _mockHttpClient.GetAsync(url, null, Arg.Any<CancellationToken>())
            .Returns(response);

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

    #region CrawlWebsiteAsync Tests - BFS 특성 검증

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldUseBreadthFirstTraversal()
    {
        // Arrange - 3단계 깊이의 링크 구조 설정
        // Root -> Page1, Page2
        // Page1 -> Page1A
        // Page2 -> Page2A

        var rootUrl = "https://example.com";
        var rootHtml = "<html><body><a href='/page1'>Page1</a><a href='/page2'>Page2</a></body></html>";
        var page1Html = "<html><body><a href='/page1a'>Page1A</a></body></html>";
        var page2Html = "<html><body><a href='/page2a'>Page2A</a></body></html>";
        var page1aHtml = "<html><body>Page1A Content</body></html>";
        var page2aHtml = "<html><body>Page2A Content</body></html>";

        SetupMockResponse(rootUrl, rootHtml);
        SetupMockResponse("https://example.com/page1", page1Html);
        SetupMockResponse("https://example.com/page2", page2Html);
        SetupMockResponse("https://example.com/page1a", page1aHtml);
        SetupMockResponse("https://example.com/page2a", page2aHtml);

        var options = new CrawlOptions { MaxDepth = 2, MaxPages = 10 };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(rootUrl, options))
        {
            results.Add(result);
        }

        // Assert - BFS는 레벨 순서로 방문
        // Root(depth 0) -> Page1, Page2(depth 1) -> Page1A, Page2A(depth 2)
        results.Should().NotBeEmpty();
        results[0].Url.Should().Be(rootUrl);
        results[0].Depth.Should().Be(0);

        // Page1과 Page2는 depth 1에서 방문
        var depth1Results = results.Where(r => r.Depth == 1).ToList();
        depth1Results.Should().HaveCountGreaterThanOrEqualTo(1);
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

        _mockHttpClient.GetAsync(url, null, Arg.Any<CancellationToken>())
            .Returns(response);
    }

    #endregion
}
