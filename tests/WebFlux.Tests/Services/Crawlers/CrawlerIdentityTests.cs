using AwesomeAssertions;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.Crawlers;
using WebFlux.Tests.TestSupport;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// 크롤러가 사이트에 <b>자기를 누구라고 말하는가</b> — 그리고 robots.txt 를 같은 이름으로 읽는가.
/// </summary>
/// <remarks>
/// 로컬 listener 가 받은 헤더로 단언한다: 무엇이 전송됐는지는 목(mock)으로는 볼 수 없다.
/// </remarks>
public sealed class CrawlerIdentityTests : IDisposable
{
    private readonly LocalHttpServer _server = new();
    private readonly HttpClient _httpClient = new();
    private readonly BreadthFirstCrawler _crawler;

    public CrawlerIdentityTests()
    {
        _crawler = new BreadthFirstCrawler(new HttpClientService(_httpClient), Substitute.For<IEventPublisher>());
        _server.Delay("/page", TimeSpan.Zero);
    }

    [Fact]
    public async Task TheDefaultUserAgent_IsOneProductToken_PointingAtThisProject()
    {
        await _crawler.CrawlAsync(_server.Url("/page"), new CrawlOptions { RespectRobotsTxt = false },
            TestContext.Current.CancellationToken);

        var sent = _server.LastHeaders("/page")["User-Agent"];
        sent.Should().StartWith("WebFlux/");
        sent.Split("WebFlux/").Length.Should().Be(2, "one product token, not two");
        sent.Should().NotContain("WebFlux-SDK");
        sent.Should().Contain("github.com/iyulab/WebFlux");
    }

    [Fact]
    public async Task CrawlOptionsUserAgent_IsWhatTheSiteReceives_OnThePageAndOnRobotsTxt()
    {
        await _crawler.CrawlAsync(_server.Url("/page"), new CrawlOptions { UserAgent = "MyBot/1.0" },
            TestContext.Current.CancellationToken);

        _server.LastHeaders("/page")["User-Agent"].Should().Be("MyBot/1.0");
        _server.LastHeaders("/robots.txt")["User-Agent"].Should().Be("MyBot/1.0");
    }

    [Fact]
    public async Task CustomHeaders_AreSent()
    {
        var options = new CrawlOptions { RespectRobotsTxt = false };
        options.CustomHeaders["X-Api-Key"] = "k-123";

        await _crawler.CrawlAsync(_server.Url("/page"), options, TestContext.Current.CancellationToken);

        _server.LastHeaders("/page").Should().ContainKey("X-Api-Key").WhoseValue.Should().Be("k-123");
    }

    [Fact]
    public async Task ARobotsGroupNamedForTheBot_AppliesToAUserAgentStringThatCarriesAVersion()
    {
        // RFC 9309 2.2.1: groups are matched on the product token. "MyBot/1.0" is the bot "MyBot".
        _server.Serve("/robots.txt", "User-agent: MyBot\nDisallow: /\n\nUser-agent: *\nDisallow:\n", "text/plain");

        var result = await _crawler.CrawlAsync(_server.Url("/page"), new CrawlOptions { UserAgent = "MyBot/1.0" },
            TestContext.Current.CancellationToken);

        result.DisallowedByRobotsTxt.Should().BeTrue();
        _server.Hits("/page").Should().Be(0);
    }

    [Fact]
    public async Task TheSameRobotsFile_DoesNotStopADifferentBot()
    {
        _server.Serve("/robots.txt", "User-agent: MyBot\nDisallow: /\n\nUser-agent: *\nDisallow:\n", "text/plain");

        var result = await _crawler.CrawlAsync(_server.Url("/page"), new CrawlOptions { UserAgent = "OtherBot/2.0" },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ARobotsGroupNamedWebFlux_AppliesToTheDefaultUserAgent()
    {
        _server.Serve("/robots.txt", "User-agent: webflux\nDisallow: /\n", "text/plain");

        var result = await _crawler.CrawlAsync(_server.Url("/page"), new CrawlOptions(),
            TestContext.Current.CancellationToken);

        result.DisallowedByRobotsTxt.Should().BeTrue();
    }

    public void Dispose()
    {
        _server.Dispose();
        _httpClient.Dispose();
    }
}
