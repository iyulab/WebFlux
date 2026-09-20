using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Services.Crawlers;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using Xunit;

namespace WebFlux.Tests.Integration;

/// <summary>
/// 실제 사이트의 robots.txt 를 상대로 한 구동. <c>Category=Integration</c> 이라 CI 에서는 돌지 않는다
/// — 네트워크에 의존하므로 게이트가 아니라 <b>릴리스 전 실측</b> 용이다.
/// </summary>
/// <remarks>
/// <para>
/// 단위 스위트(<c>RobotsTxtTests</c>)는 파일 내용을 직접 준다. 여기서는 그 파일을 <b>실제로 받아서</b>
/// 전체 경로(fetch → parse → match → 결과 모양)를 구동한다. 0.8.0 에서 소비자가 본 것과 같은 방식이다.
/// </para>
/// <para>
/// 대상 사이트의 robots.txt 는 바뀔 수 있다. 각 fact 에 2026-09-20 실측 내용을 적어 두어, 나중에
/// 실패하면 «회귀» 인지 «사이트가 파일을 바꿨다» 인지 가릴 수 있게 한다.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public class RealWorldRobotsTxtTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private IWebContentProcessor _processor = null!;
    private ICrawler _crawler = null!;

    public ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddWebFlux();
        _serviceProvider = services.BuildServiceProvider();
        _processor = _serviceProvider.GetRequiredService<IWebContentProcessor>();
        _crawler = _serviceProvider.GetRequiredService<ICrawlerFactory>().CreateCrawler(CrawlStrategy.BreadthFirst);
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider != null) await _serviceProvider.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private static CrawlOptions SinglePage => new() { MaxDepth = 0, MaxPages = 1 };

    [Fact]
    public async Task AnAllowAllSiteIsFetched()
    {
        // https://www.iana.org/robots.txt is exactly "User-agent: *" + an empty "Disallow:"
        // (2026-09-20). That is the canonical "everything is allowed" file, and it is the one
        // 0.8.0 read as "block the whole site" - this fact is the defect a consumer reported.
        var result = await _crawler.CrawlAsync(
            "https://www.iana.org/help/example-domains", SinglePage, TestContext.Current.CancellationToken);

        result.DisallowedByRobotsTxt.Should().BeFalse("an empty Disallow allows everything");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AnAllowAllSiteIsFetchedThroughTheExtractApiToo()
    {
        var result = await _processor.ExtractContentAsync(
            "https://www.iana.org/help/example-domains",
            new ExtractOptions { EvaluateQuality = false, UseCache = false },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.MainContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ADisallowedPathIsRefused()
    {
        // https://httpbin.org/robots.txt is "User-agent: *" + "Disallow: /deny" (2026-09-20).
        var result = await _crawler.CrawlAsync(
            "https://httpbin.org/deny", SinglePage, TestContext.Current.CancellationToken);

        result.DisallowedByRobotsTxt.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ARefusalArrivesAsItsOwnErrorCodeOnTheExtractPath()
    {
        var result = await _processor.ExtractContentAsync(
            "https://httpbin.org/deny",
            new ExtractOptions { EvaluateQuality = false, UseCache = false },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ExtractErrorCodes.DisallowedByRobotsTxt);
    }

    [Fact]
    public async Task TheOptOutReachesTheExtractApi()
    {
        var result = await _processor.ExtractContentAsync(
            "https://httpbin.org/deny",
            new ExtractOptions { RespectRobotsTxt = false, EvaluateQuality = false, UseCache = false },
            TestContext.Current.CancellationToken);

        // Whatever the page answers, the refusal is no longer the reason we stopped.
        (result.Error?.Code).Should().NotBe(ExtractErrorCodes.DisallowedByRobotsTxt);
    }

    [Fact]
    public async Task GroupedUserAgentLinesAndLongestMatch_OnARealFile()
    {
        // https://www.google.com/robots.txt opens with two consecutive User-agent lines
        // ("*" then "Yandex") followed by "Disallow: /search" and "Allow: /search/about"
        // (2026-09-20). Under 0.8.0 the "*" group ended up with no rules, so /search was fetched.
        var blocked = await _crawler.CrawlAsync(
            "https://www.google.com/search?q=x", SinglePage, TestContext.Current.CancellationToken);
        blocked.DisallowedByRobotsTxt.Should().BeTrue("the star group owns the rules below the second User-agent line");

        var uncovered = await _crawler.CrawlAsync(
            "https://www.google.com/search/about", SinglePage, TestContext.Current.CancellationToken);
        uncovered.DisallowedByRobotsTxt.Should().BeFalse("the longer Allow wins over the shorter Disallow");
    }
}
