using FluentAssertions;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// URL 패턴 필터링 테스트용 구체 크롤러
/// BaseCrawler의 protected 메서드를 테스트하기 위한 서브클래스
/// </summary>
public class TestCrawlerForFiltering : BaseCrawler
{
    public TestCrawlerForFiltering(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher) { }

    /// <summary>
    /// protected ShouldCrawlUrl 메서드를 테스트에 노출
    /// </summary>
    public bool TestShouldCrawlUrl(string url, string? baseUrl = null, CrawlOptions? options = null)
    {
        return ShouldCrawlUrl(url, baseUrl, options);
    }
}

/// <summary>
/// URL 필터링 단위 테스트
/// ShouldCrawlUrl의 패턴 매칭, 확장자 제외, 도메인 필터링 검증
/// </summary>
public class UrlFilteringTests
{
    private readonly TestCrawlerForFiltering _crawler;

    public UrlFilteringTests()
    {
        var mockHttp = Substitute.For<IHttpClientService>();
        var mockPublisher = Substitute.For<IEventPublisher>();
        _crawler = new TestCrawlerForFiltering(mockHttp, mockPublisher);
    }

    [Fact]
    public void ShouldCrawlUrl_WithExcludePattern_ShouldRejectMatchingUrl()
    {
        var options = new CrawlOptions
        {
            ExcludeUrlPatterns = new List<string> { @"/admin/.*", @"/login" }
        };
        _crawler.TestShouldCrawlUrl("https://example.com/admin/settings", "https://example.com", options).Should().BeFalse();
        _crawler.TestShouldCrawlUrl("https://example.com/page", "https://example.com", options).Should().BeTrue();
    }

    [Fact]
    public void ShouldCrawlUrl_WithIncludePattern_ShouldOnlyAllowMatchingUrl()
    {
        var options = new CrawlOptions
        {
            IncludeUrlPatterns = new List<string> { @"/docs/.*", @"/api/.*" }
        };
        _crawler.TestShouldCrawlUrl("https://example.com/docs/getting-started", "https://example.com", options).Should().BeTrue();
        _crawler.TestShouldCrawlUrl("https://example.com/blog/post", "https://example.com", options).Should().BeFalse();
    }

    [Fact]
    public void ShouldCrawlUrl_WithExcludedExtensions_ShouldRejectFileUrls()
    {
        var options = new CrawlOptions(); // Has default excluded extensions
        _crawler.TestShouldCrawlUrl("https://example.com/image.jpg", "https://example.com", options).Should().BeFalse();
        _crawler.TestShouldCrawlUrl("https://example.com/page", "https://example.com", options).Should().BeTrue();
    }

    [Fact]
    public void ShouldCrawlUrl_DifferentDomain_ShouldReject()
    {
        _crawler.TestShouldCrawlUrl("https://other.com/page", "https://example.com").Should().BeFalse();
    }
}
