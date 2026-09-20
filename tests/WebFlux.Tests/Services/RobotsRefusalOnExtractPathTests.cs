using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Core.Utilities;
using WebFlux.Services;
using AwesomeAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// 단일 URL 추출 경로에서 robots.txt 거절이 소비자에게 <b>어떻게 도착하는가</b>.
/// </summary>
/// <remarks>
/// 소비자 보고: 같은 URL 이 <c>CrawlAsync</c> 로는 29 ms 에 답하는데 <c>ExtractContentAsync</c> 로는
/// 3 021 ms 가 걸렸고(<c>MaxRetries</c> 백오프를 돌았다), 결과는 <c>Error.Code = "Unknown"</c> 이라
/// 정책 거절과 네트워크 실패를 구분할 수 없었다. 그리고 옵션으로 끌 수가 없었다.
/// 경과 시간이 아니라 <b>크롤러 호출 횟수</b>를 단언한다 — 같은 사실을 벽시계 없이 고정한다.
/// </remarks>
public class RobotsRefusalOnExtractPathTests : IDisposable
{
    private readonly IServiceFactory _serviceFactory = Substitute.For<IServiceFactory>();
    private readonly WebContentProcessor _processor;
    private readonly ICrawler _crawler = Substitute.For<ICrawler>();

    public RobotsRefusalOnExtractPathTests()
    {
        _processor = new WebContentProcessor(
            _serviceFactory,
            Substitute.For<IEventPublisher>(),
            Substitute.For<ILogger<WebContentProcessor>>());

        _serviceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(_crawler);
        _serviceFactory.TryCreateCacheService().Returns((ICacheService?)null);
        _serviceFactory.TryCreateDomainRateLimiter().Returns((IDomainRateLimiter?)null);
        _serviceFactory.TryCreateContentQualityEvaluator().Returns((IContentQualityEvaluator?)null);
    }

    private void CrawlerRefusesByRobots(string url) =>
        _crawler.CrawlAsync(Arg.Is<string>(u => u == url), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(new CrawlResult
            {
                Url = url,
                FinalUrl = url,
                StatusCode = 0,
                IsSuccess = false,
                DisallowedByRobotsTxt = true,
                ErrorMessage = "Disallowed by robots.txt"
            });

    [Fact]
    public async Task ARobotsRefusalCarriesItsOwnErrorCode_NotTheCatchAll()
    {
        const string url = "https://example.com/private/x.html";
        CrawlerRefusesByRobots(url);

        var result = await _processor.ExtractContentAsync(
            url, cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ExtractErrorCodes.DisallowedByRobotsTxt);
        result.Error.Code.Should().NotBe(ExtractErrorCodes.Unknown);
    }

    [Fact]
    public async Task ARobotsRefusalIsNotRetried()
    {
        const string url = "https://example.com/private/x.html";
        CrawlerRefusesByRobots(url);

        // MaxRetries = 3 would mean four attempts if the refusal were treated as a failed request.
        await _processor.ExtractContentAsync(
            url,
            new ExtractOptions { MaxRetries = 3, EvaluateQuality = false },
            TestContext.Current.CancellationToken);

        await _crawler.Received(1).CrawlAsync(
            Arg.Is<string>(u => u == url), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AGenuineFailureIsStillRetried()
    {
        // The positive control for the test above: the no-retry rule must be specific to a policy
        // refusal, not a blanket "stop retrying on any unsuccessful result".
        const string url = "https://example.com/flaky";
        _crawler.CrawlAsync(Arg.Is<string>(u => u == url), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(new CrawlResult
            {
                Url = url,
                FinalUrl = url,
                StatusCode = 503,
                IsSuccess = false,
                ErrorMessage = "Service Unavailable"
            });

        await _processor.ExtractContentAsync(
            url,
            new ExtractOptions { MaxRetries = 2, EvaluateQuality = false },
            TestContext.Current.CancellationToken);

        await _crawler.Received(3).CrawlAsync(
            Arg.Is<string>(u => u == url), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RespectRobotsTxt_DefaultsToTrueAndReachesTheCrawler()
    {
        const string url = "https://example.com/page";
        CrawlerRefusesByRobots(url);

        await _processor.ExtractContentAsync(
            url, cancellationToken: TestContext.Current.CancellationToken);

        await _crawler.Received().CrawlAsync(
            Arg.Any<string>(),
            Arg.Is<CrawlOptions>(o => o.RespectRobotsTxt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RespectRobotsTxt_CanBeTurnedOffFromExtractOptions()
    {
        const string url = "https://example.com/page";
        CrawlerRefusesByRobots(url);

        await _processor.ExtractContentAsync(
            url,
            new ExtractOptions { RespectRobotsTxt = false, EvaluateQuality = false },
            TestContext.Current.CancellationToken);

        await _crawler.Received().CrawlAsync(
            Arg.Any<string>(),
            Arg.Is<CrawlOptions>(o => !o.RespectRobotsTxt),
            Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        _processor.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// robots.txt 를 <b>읽지 못한 것</b>과 robots.txt 가 <b>아무 말도 하지 않은 것</b>은 반대의 답을
/// 요구한다 — RFC 9309 section 2.3.1.
/// </summary>
public class RobotsTxtAccessResultTests
{
    [Fact]
    public void ADefaultConstructedInfo_MeansNoRulesApply()
    {
        // The value a 4xx (and a transport failure) produces: nothing is forbidden.
        RobotsTxt.IsAllowed(new RobotsTxtInfo(), "https://example.com/anything", "WebFlux")
            .Should().BeTrue();
        new RobotsTxtInfo().Access.Should().Be(RobotsTxtAccess.Unavailable);
    }

    [Fact]
    public void AnUnreachableFile_ForbidsEverything()
    {
        // Section 2.3.1.4: "the crawler MUST assume complete disallow".
        var info = new RobotsTxtInfo { Access = RobotsTxtAccess.Unreachable };

        RobotsTxt.IsAllowed(info, "https://example.com/", "WebFlux").Should().BeFalse();
        RobotsTxt.IsAllowed(info, "https://example.com/anything", "WebFlux").Should().BeFalse();
    }

    [Fact]
    public void AParsedFile_IsMarkedAsSuch()
    {
        RobotsTxt.Parse("User-agent: *\nDisallow:\n").Access.Should().Be(RobotsTxtAccess.Parsed);
    }

    [Fact]
    public void AParsedAllowAllFile_IsNotConfusedWithAnUnreachableOne()
    {
        // The two states the old model could not tell apart.
        var parsed = RobotsTxt.Parse("User-agent: *\nDisallow:\n");
        var unreachable = new RobotsTxtInfo { Access = RobotsTxtAccess.Unreachable };

        RobotsTxt.IsAllowed(parsed, "https://example.com/x", "WebFlux").Should().BeTrue();
        RobotsTxt.IsAllowed(unreachable, "https://example.com/x", "WebFlux").Should().BeFalse();
    }
}
