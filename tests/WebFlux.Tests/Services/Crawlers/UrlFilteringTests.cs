using AwesomeAssertions;
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

    // The host scope was hardcoded to "same host as the start URL" and the two options that say
    // otherwise were read by nothing: FollowExternalLinks = true was silently ignored, and
    // AllowedDomains documented the opposite of what happened ("empty allows every domain").
    // Each option gets both directions, and the default is asserted from a fresh CrawlOptions so
    // that moving a declared default turns these red rather than shipping quietly.

    [Fact]
    public void ShouldCrawlUrl_DefaultOptions_KeepTheSameHostRule()
    {
        var options = new CrawlOptions();

        options.FollowExternalLinks.Should().BeFalse("widening the crawl must stay opt-in");
        options.AllowedDomains.Should().BeEmpty("an empty list must not widen anything");

        _crawler.TestShouldCrawlUrl("https://other.com/page", "https://example.com", options).Should().BeFalse();
        _crawler.TestShouldCrawlUrl("https://example.com/page", "https://example.com", options).Should().BeTrue();
    }

    [Fact]
    public void ShouldCrawlUrl_FollowExternalLinks_AllowsAnotherHost()
    {
        var options = new CrawlOptions { FollowExternalLinks = true };

        _crawler.TestShouldCrawlUrl("https://other.com/page", "https://example.com", options).Should().BeTrue();
    }

    [Fact]
    public void ShouldCrawlUrl_AllowedDomains_AllowsListedHostsAndTheirSubdomains()
    {
        var options = new CrawlOptions { AllowedDomains = new HashSet<string> { "partner.com" } };

        _crawler.TestShouldCrawlUrl("https://partner.com/page", "https://example.com", options).Should().BeTrue();
        _crawler.TestShouldCrawlUrl("https://docs.partner.com/page", "https://example.com", options).Should().BeTrue();

        // Still a list, not a switch: a host nobody named stays out.
        _crawler.TestShouldCrawlUrl("https://other.com/page", "https://example.com", options).Should().BeFalse();

        // And a suffix that merely ends the same way is not a subdomain.
        _crawler.TestShouldCrawlUrl("https://notpartner.com/page", "https://example.com", options).Should().BeFalse();
    }

    [Fact]
    public void ShouldCrawlUrl_WideningHostScope_DoesNotBypassTheOtherFilters()
    {
        var options = new CrawlOptions
        {
            FollowExternalLinks = true,
            ExcludeUrlPatterns = new List<string> { @"/private/.*" }
        };

        _crawler.TestShouldCrawlUrl("https://other.com/private/page", "https://example.com", options).Should().BeFalse();
        _crawler.TestShouldCrawlUrl("https://other.com/public/page", "https://example.com", options).Should().BeTrue();
    }
}
