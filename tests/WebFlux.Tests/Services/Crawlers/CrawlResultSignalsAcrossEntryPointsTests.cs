using AwesomeAssertions;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.Crawlers;
using WebFlux.Tests.TestSupport;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// <see cref="CrawlResult"/> 의 신호(<c>TimedOut</c> · <c>DisallowedByRobotsTxt</c>)가 결과를 돌려주는
/// <b>모든 진입점</b>에서 살아남는가, 그리고 잘못된 옵션이 모든 진입점에서 같은 방식으로 거절되는가.
/// </summary>
/// <remarks>
/// 신호는 <c>CrawlAsync</c> 가 만든다. 사이트 크롤 경로는 그 결과를 깊이만 바꿔 다시 내보내는데, 그 자리가
/// 필드를 하나씩 옮겨 적는 형태이면 새 필드는 조용히 떨어진다 — <c>CrawlAsync</c> 만 재는 테스트는 그것을 못 본다.
/// 그래서 단위는 «메서드» 가 아니라 «결과를 돌려주는 경로» 다.
/// </remarks>
public sealed class CrawlResultSignalsAcrossEntryPointsTests : IDisposable
{
    private static readonly TimeSpan SlowPage = TimeSpan.FromSeconds(4);

    private readonly LocalHttpServer _server = new();
    private readonly HttpClient _httpClient = new();
    private readonly HttpClientService _http;

    public CrawlResultSignalsAcrossEntryPointsTests()
    {
        _http = new HttpClientService(_httpClient);
    }

    public enum EntryPoint
    {
        CrawlAsync,
        BreadthFirstWebsite,
        DepthFirstWebsite,
        ParallelWebsite,
        Sitemap
    }

    public static TheoryData<EntryPoint> EntryPoints => new(Enum.GetValues<EntryPoint>());

    [Theory]
    [MemberData(nameof(EntryPoints))]
    public async Task ATimeout_IsFlagged_OnEveryEntryPoint(EntryPoint entryPoint)
    {
        _server.Delay("/page", SlowPage);
        ServeSitemapFor("/page");

        var results = await RunAsync(entryPoint, "/page", Options(timeoutMs: 300));

        results.Should().ContainSingle();
        results[0].IsSuccess.Should().BeFalse();
        results[0].TimedOut.Should().BeTrue("the request was cut off by TimeoutMs, whichever method returned the result");
        _server.Hits("/page").Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(EntryPoints))]
    public async Task TheSamePage_WithARoomyTimeout_IsNotFlagged(EntryPoint entryPoint)
    {
        _server.Delay("/page", TimeSpan.FromMilliseconds(200));
        ServeSitemapFor("/page");

        var results = await RunAsync(entryPoint, "/page", Options(timeoutMs: 10_000));

        results.Should().ContainSingle();
        results[0].IsSuccess.Should().BeTrue();
        results[0].TimedOut.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(EntryPoints))]
    public async Task ARobotsRefusal_IsNeverReportedAsAPlainFailure(EntryPoint entryPoint)
    {
        // The site-crawl paths skip a disallowed URL; the single-URL and sitemap paths report it.
        // What no path may do is hand back a failed result that has lost the reason.
        _server.Serve("/robots.txt", "User-agent: *\nDisallow: /private/\n", "text/plain");
        _server.Delay("/private/page", TimeSpan.Zero);
        ServeSitemapFor("/private/page");

        var results = await RunAsync(entryPoint, "/private/page", Options(timeoutMs: 10_000, respectRobots: true));

        _server.Hits("/private/page").Should().Be(0, "a disallowed URL is not fetched");
        results.Should().OnlyContain(r => r.DisallowedByRobotsTxt, "a refusal that is reported carries its reason");
    }

    [Theory]
    [MemberData(nameof(EntryPoints))]
    public async Task TheSameUrl_WithRobotsOff_IsFetched(EntryPoint entryPoint)
    {
        _server.Serve("/robots.txt", "User-agent: *\nDisallow: /private/\n", "text/plain");
        _server.Delay("/private/page", TimeSpan.Zero);
        ServeSitemapFor("/private/page");

        var results = await RunAsync(entryPoint, "/private/page", Options(timeoutMs: 10_000, respectRobots: false));

        results.Should().ContainSingle();
        results[0].IsSuccess.Should().BeTrue();
        results[0].DisallowedByRobotsTxt.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(EntryPoints))]
    public async Task TheDepthOfTheResult_IsTheOnlyThingTheSiteCrawlChanges(EntryPoint entryPoint)
    {
        _server.Serve("/page", "<html><body><a href=\"/child\">c</a><img src=\"/i.png\"></body></html>");
        _server.Delay("/child", TimeSpan.Zero);
        ServeSitemapFor("/page");

        var results = await RunAsync(entryPoint, "/page", Options(timeoutMs: 10_000, maxPages: 2, maxDepth: 1));
        var direct = await NewCrawler(entryPoint).CrawlAsync(
            _server.Url("/page"), Options(timeoutMs: 10_000), TestContext.Current.CancellationToken);

        var page = results.Single(r => r.Url == _server.Url("/page"));
        page.Should().BeEquivalentTo(direct, o => o
            .Excluding(r => r.Depth)
            .Excluding(r => r.ResponseTimeMs)
            .Excluding(r => r.CrawledAt)
            .Excluding(r => r.Headers));
    }

    [Theory]
    [MemberData(nameof(EntryPoints))]
    public async Task ANonPositiveTimeoutMs_IsRejectedOnce_ByName_BeforeAnyRequest(EntryPoint entryPoint)
    {
        _server.Delay("/page", TimeSpan.Zero);
        ServeSitemapFor("/page");

        var act = async () => await RunAsync(entryPoint, "/page", Options(timeoutMs: 0));

        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*TimeoutMs*");
        _server.Hits("/page").Should().Be(0);
        _server.Hits("/sitemap.xml").Should().Be(0);
    }

    private void ServeSitemapFor(string path) =>
        _server.Serve(
            "/sitemap.xml",
            $"<?xml version=\"1.0\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><url><loc>{_server.Url(path)}</loc></url></urlset>",
            "application/xml");

    private static CrawlOptions Options(int timeoutMs, bool respectRobots = false, int maxPages = 1, int maxDepth = 0) => new()
    {
        TimeoutMs = timeoutMs,
        RespectRobotsTxt = respectRobots,
        MaxPages = maxPages,
        MaxDepth = maxDepth,
        DelayMs = 0,
        UserAgent = "*"
    };

    private BaseCrawler NewCrawler(EntryPoint entryPoint)
    {
        var events = Substitute.For<IEventPublisher>();
        return entryPoint switch
        {
            EntryPoint.DepthFirstWebsite => new DepthFirstCrawler(_http, events),
            EntryPoint.Sitemap => new SitemapCrawler(_http, events),
            _ => new BreadthFirstCrawler(_http, events)
        };
    }

    private async Task<List<CrawlResult>> RunAsync(EntryPoint entryPoint, string path, CrawlOptions options)
    {
        var ct = TestContext.Current.CancellationToken;
        var crawler = NewCrawler(entryPoint);
        var results = new List<CrawlResult>();

        switch (entryPoint)
        {
            case EntryPoint.CrawlAsync:
                results.Add(await crawler.CrawlAsync(_server.Url(path), options, ct));
                break;
            case EntryPoint.ParallelWebsite:
                await foreach (var r in crawler.CrawlWebsiteParallelAsync(_server.Url(path), options, ct))
                    results.Add(r);
                break;
            case EntryPoint.Sitemap:
                await foreach (var r in crawler.CrawlSitemapAsync(_server.Url("/sitemap.xml"), options, ct))
                    results.Add(r);
                break;
            default:
                await foreach (var r in crawler.CrawlWebsiteAsync(_server.Url(path), options, ct))
                    results.Add(r);
                break;
        }

        return results;
    }

    public void Dispose()
    {
        _server.Dispose();
        _httpClient.Dispose();
    }
}
