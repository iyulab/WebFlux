using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using WebFlux.Services.Crawlers;
using WebFlux.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace WebFlux.Tests.Integration;

/// <summary>
/// 실제 웹사이트를 대상으로 한 크롤러 통합 테스트
/// Category=Integration 필터로 실행: dotnet test --filter "Category=Integration"
///
/// 참고: Playwright 브라우저 설치 필요 (동적 크롤링용)
/// pwsh bin/Debug/net10.0/playwright.ps1 install
/// </summary>
[Trait("Category", "Integration")]
public class RealWorldCrawlerTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private ServiceProvider? _serviceProvider;
    private IWebContentProcessor _processor = null!;
    private ICrawler _crawler = null!;

    public RealWorldCrawlerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddWebFlux();
        _serviceProvider = services.BuildServiceProvider();
        _processor = _serviceProvider.GetRequiredService<IWebContentProcessor>();
        _crawler = _serviceProvider.GetRequiredService<ICrawlerFactory>().CreateCrawler(CrawlStrategy.BreadthFirst);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    #region 기본 크롤링 연결성 테스트 (Crawler 직접 사용)

    [Fact]
    public async Task Crawler_ExampleCom_ShouldReturnSuccessResult()
    {
        // Arrange
        var url = TestSites.ExampleCom;

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.HtmlContent.Should().NotBeNullOrEmpty();
        _output.WriteLine($"Crawled {url}: {result.StatusCode}, Content Length: {result.HtmlContent?.Length ?? 0}");
    }

    [Fact]
    public async Task Crawler_HttpBinHtml_ShouldReturnHtmlContent()
    {
        // Arrange
        var url = TestSites.HtmlUrl;

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.HtmlContent.Should().Contain("html");
        _output.WriteLine($"Crawled {url}: Content Length: {result.HtmlContent?.Length ?? 0}");
    }

    [Fact]
    public async Task Crawler_QuotesToScrape_ShouldExtractLinks()
    {
        // Arrange
        var url = TestSites.QuotesToScrape;

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.DiscoveredLinks.Should().NotBeEmpty();
        _output.WriteLine($"Crawled {url}: Found {result.DiscoveredLinks.Count} links");
    }

    #endregion

    #region HTTP 상태 코드 테스트

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    public async Task Crawler_HttpStatusCodes_ShouldHandleCorrectly(int statusCode, bool expectSuccess)
    {
        // Arrange
        var url = TestSites.GetHttpStatusUrl(statusCode);
        var options = new CrawlOptions { MaxRetries = 0 }; // 테스트 속도를 위해 재시도 비활성화

        // Act
        var result = await _crawler.CrawlAsync(url, options);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(statusCode);
        result.IsSuccess.Should().Be(expectSuccess);
        _output.WriteLine($"Status {statusCode}: IsSuccess={result.IsSuccess}");
    }

    #endregion

    #region 재시도 로직 테스트

    [Fact]
    public async Task Crawler_WithRetries_ShouldHandleDelayedResponses()
    {
        // Arrange - httpbin의 지연 응답 사용
        var url = TestSites.GetDelayUrl(1); // 1초 지연
        var options = new CrawlOptions { MaxRetries = 2, TimeoutMs = 10000 };

        // Act
        var result = await _crawler.CrawlAsync(url, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _output.WriteLine($"Delayed response: {result.ResponseTimeMs}ms");
    }

    #endregion

    #region 웹사이트 크롤링 테스트

    [Fact]
    public async Task CrawlWebsiteAsync_QuotesToScrape_ShouldDiscoverMultiplePages()
    {
        // Arrange
        var url = TestSites.QuotesToScrape;
        var crawlOptions = new CrawlOptions
        {
            MaxPages = 5,
            MaxDepth = 1,
            DelayMs = 500 // 예의상 지연
        };

        // Act
        var results = new List<CrawlResult>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, crawlOptions))
        {
            results.Add(result);
            _output.WriteLine($"Crawled: {result.Url} (Depth: {result.Depth}, Links: {result.DiscoveredLinks.Count})");
            if (results.Count >= 5) break; // 테스트용 제한
        }

        // Assert
        results.Should().NotBeEmpty();
        results.Count.Should().BeGreaterThanOrEqualTo(1);
        results.First().IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlWebsiteAsync_ShouldRespectMaxPages()
    {
        // Arrange
        var url = TestSites.BooksToScrape;
        var maxPages = 3;
        var crawlOptions = new CrawlOptions
        {
            MaxPages = maxPages,
            MaxDepth = 1,
            DelayMs = 300
        };

        // Act
        var visitedUrls = new HashSet<string>();
        await foreach (var result in _crawler.CrawlWebsiteAsync(url, crawlOptions))
        {
            visitedUrls.Add(result.Url);
            _output.WriteLine($"Visited: {result.Url}");
            if (visitedUrls.Count > maxPages + 2) break; // 안전 제한
        }

        // Assert
        visitedUrls.Count.Should().BeLessThanOrEqualTo(maxPages);
    }

    #endregion

    #region 성능 및 안정성 테스트

    [Fact]
    public async Task Crawler_LargeResponse_ShouldHandleWithoutMemoryIssues()
    {
        // Arrange - 100KB 랜덤 바이트
        var url = TestSites.GetRandomBytesUrl(102400);

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ContentLength.Should().BeGreaterThan(100000);
        _output.WriteLine($"Large response: {result.ContentLength} bytes");
    }

    [Fact]
    public async Task Crawler_CancellationToken_ShouldRespectCancellation()
    {
        // Arrange - 요청 시작 전에 이미 취소된 토큰 사용
        var url = TestSites.ExampleCom;
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 미리 취소

        // Act & Assert - 취소된 토큰으로 요청 시 예외 발생해야 함
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _crawler.CrawlAsync(url, cancellationToken: cts.Token);
        });
    }

    #endregion

    #region robots.txt 테스트

    [Fact]
    public async Task Crawler_RobotsTxt_ShouldParse()
    {
        // Arrange
        var baseUrl = TestSites.QuotesToScrape;

        // Act
        var robotsInfo = await _crawler.GetRobotsTxtAsync(baseUrl, "*");

        // Assert
        robotsInfo.Should().NotBeNull();
        _output.WriteLine($"robots.txt: {robotsInfo.Content?.Length ?? 0} bytes");
    }

    #endregion

    #region 링크 추출 테스트

    [Fact]
    public async Task Crawler_ExtractLinks_ShouldFindValidLinks()
    {
        // Arrange
        var url = TestSites.QuotesToScrape;

        // Act
        var result = await _crawler.CrawlAsync(url);
        var links = result.DiscoveredLinks;

        // Assert
        links.Should().NotBeEmpty();
        links.Should().AllSatisfy(link =>
        {
            Uri.TryCreate(link, UriKind.Absolute, out _).Should().BeTrue();
        });
        _output.WriteLine($"Found {links.Count} valid links");
    }

    #endregion

    #region 통계 테스트

    [Fact]
    public async Task Crawler_Statistics_ShouldTrackRequests()
    {
        // Arrange
        var urls = new[] { TestSites.ExampleCom, TestSites.HtmlUrl };

        // Act
        foreach (var url in urls)
        {
            await _crawler.CrawlAsync(url);
        }
        var stats = _crawler.GetStatistics();

        // Assert
        stats.TotalRequests.Should().BeGreaterThanOrEqualTo(2);
        stats.SuccessfulRequests.Should().BeGreaterThanOrEqualTo(2);
        _output.WriteLine($"Stats: {stats.TotalRequests} total, {stats.SuccessfulRequests} success");
    }

    #endregion
}
