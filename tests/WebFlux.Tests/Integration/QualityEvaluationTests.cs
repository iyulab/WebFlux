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
/// 크롤링된 콘텐츠 품질 평가 통합 테스트
/// 실제 웹사이트에서 크롤링한 HTML 콘텐츠의 품질을 평가합니다.
/// Category=Integration 필터로 실행: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class QualityEvaluationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private ServiceProvider? _serviceProvider;
    private ICrawler _crawler = null!;
    private IContentQualityEvaluator? _qualityEvaluator;

    public QualityEvaluationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddWebFlux();
        _serviceProvider = services.BuildServiceProvider();
        _crawler = _serviceProvider.GetRequiredService<ICrawlerFactory>().CreateCrawler(CrawlStrategy.BreadthFirst);
        _qualityEvaluator = _serviceProvider.GetService<IContentQualityEvaluator>();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    #region HTML 콘텐츠 품질 테스트

    [Theory]
    [InlineData("https://quotes.toscrape.com")]
    [InlineData("https://example.com")]
    public async Task CrawlContent_RealWebsite_ShouldHaveValidHtml(string url)
    {
        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.HtmlContent.Should().NotBeNullOrEmpty();
        result.HtmlContent.Should().Contain("<html", "HTML 문서여야 합니다");

        _output.WriteLine($"URL: {url}");
        _output.WriteLine($"Content Length: {result.HtmlContent?.Length ?? 0}");
        _output.WriteLine($"Response Time: {result.ResponseTimeMs}ms");
    }

    [Theory]
    [InlineData("https://quotes.toscrape.com", 0.3)]
    [InlineData("https://example.com", 0.3)]
    public async Task EvaluateHtmlQuality_ShouldMeetThreshold(string url, double minScore)
    {
        // Arrange
        var result = await _crawler.CrawlAsync(url);

        // Skip if no quality evaluator available
        if (_qualityEvaluator == null)
        {
            _output.WriteLine("Quality evaluator not available, skipping quality check");
            return;
        }

        // Act
        var quality = await _qualityEvaluator.EvaluateHtmlAsync(result.HtmlContent!, url);

        // Assert
        quality.Should().NotBeNull();
        quality.OverallScore.Should().BeGreaterThanOrEqualTo(minScore);

        _output.WriteLine($"URL: {url}");
        _output.WriteLine($"Overall Score: {quality.OverallScore:F2}");
        _output.WriteLine($"Content Ratio: {quality.ContentRatio:F2}");
    }

    #endregion

    #region 링크 추출 품질 테스트

    [Fact]
    public async Task ExtractLinks_QuotesToScrape_ShouldFindMeaningfulLinks()
    {
        // Arrange
        var url = TestSites.QuotesToScrape;

        // Act
        var result = await _crawler.CrawlAsync(url);
        var links = result.DiscoveredLinks;

        // Assert
        links.Should().NotBeEmpty();

        // 유효한 URL이어야 함
        links.Should().AllSatisfy(link =>
        {
            Uri.TryCreate(link, UriKind.Absolute, out var uri).Should().BeTrue();
            (uri!.Scheme == "http" || uri.Scheme == "https").Should().BeTrue();
        });

        // 같은 도메인 링크가 존재해야 함
        var sameDomainLinks = links.Where(l => l.Contains("toscrape.com")).ToList();
        sameDomainLinks.Should().NotBeEmpty("내부 링크가 있어야 합니다");

        _output.WriteLine($"Total links: {links.Count}");
        _output.WriteLine($"Same domain links: {sameDomainLinks.Count}");
    }

    [Fact]
    public async Task ExtractLinks_ShouldNotContainInvalidProtocols()
    {
        // Arrange
        var url = TestSites.QuotesToScrape;

        // Act
        var result = await _crawler.CrawlAsync(url);
        var links = result.DiscoveredLinks;

        // Assert
        links.Should().NotContain(l => l.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase));
        links.Should().NotContain(l => l.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase));
        links.Should().NotContain(l => l.StartsWith("tel:", StringComparison.OrdinalIgnoreCase));
        links.Should().NotContain(l => l.StartsWith("#"));
    }

    #endregion

    #region 메타데이터 품질 테스트

    [Theory]
    [InlineData("https://quotes.toscrape.com")]
    [InlineData("https://books.toscrape.com")]
    public async Task CrawlMetadata_ShouldHaveBasicInfo(string url)
    {
        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Url.Should().Be(url);
        result.FinalUrl.Should().NotBeNullOrEmpty();
        result.CrawledAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        result.ResponseTimeMs.Should().BeGreaterThan(0);
        result.ContentType.Should().Contain("html");

        _output.WriteLine($"URL: {result.Url}");
        _output.WriteLine($"Final URL: {result.FinalUrl}");
        _output.WriteLine($"Content-Type: {result.ContentType}");
        _output.WriteLine($"Crawled At: {result.CrawledAt}");
    }

    #endregion

    #region 콘텐츠 구조 품질 테스트

    [Theory]
    [InlineData("https://quotes.toscrape.com")]
    [InlineData("https://example.com")]
    public async Task HtmlContent_ShouldHaveProperStructure(string url)
    {
        // Act
        var result = await _crawler.CrawlAsync(url);
        var html = result.HtmlContent!;

        // Assert
        html.Should().Contain("<head", "head 태그가 있어야 합니다");
        html.Should().Contain("<body", "body 태그가 있어야 합니다");
        html.Should().Contain("</html>", "닫는 html 태그가 있어야 합니다");

        _output.WriteLine($"URL: {url}");
        _output.WriteLine($"Has <title>: {html.Contains("<title")}");
        _output.WriteLine($"Has <meta>: {html.Contains("<meta")}");
    }

    [Fact]
    public async Task HtmlContent_QuotesToScrape_ShouldContainExpectedElements()
    {
        // Arrange
        var url = TestSites.QuotesToScrape;

        // Act
        var result = await _crawler.CrawlAsync(url);
        var html = result.HtmlContent!.ToLower();

        // Assert - quotes.toscrape.com 특성
        html.Should().Contain("quote", "인용문 관련 콘텐츠가 있어야 합니다");

        _output.WriteLine($"Contains 'quote': {html.Contains("quote")}");
        _output.WriteLine($"Contains 'author': {html.Contains("author")}");
    }

    #endregion

    #region 에러 처리 품질 테스트

    [Fact]
    public async Task Crawl_NotFoundPage_ShouldHandleGracefully()
    {
        // Arrange
        var url = $"{TestSites.QuotesToScrape}/nonexistent-page-12345";

        // Act
        var result = await _crawler.CrawlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(404);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();

        _output.WriteLine($"Status: {result.StatusCode}");
        _output.WriteLine($"Error: {result.ErrorMessage}");
    }

    [Fact]
    public async Task Crawl_InvalidDomain_ShouldReturnError()
    {
        // Arrange
        var url = "https://this-domain-definitely-does-not-exist-12345.com";
        var options = new CrawlOptions { MaxRetries = 0 };

        // Act
        var result = await _crawler.CrawlAsync(url, options);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().NotBeNull();

        _output.WriteLine($"Error: {result.ErrorMessage}");
    }

    #endregion
}
