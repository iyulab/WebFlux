using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.Crawlers;
using WebFlux.Tests.TestSupport;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// 재시도는 <b>한 층</b>이 소유하고, «다시 하면 달라질 수 있는 것» 만 다시 한다.
/// </summary>
/// <remarks>
/// 서버가 받은 요청 수로 단언한다. 추출 경로가 크롤러의 재시도 루프를 자기 루프로 감싸면 횟수가
/// 곱해지고(목으로는 보이지 않는다 — 목 크롤러는 한 번 불릴 뿐이다), 404 는 몇 번을 물어도 404 다.
/// </remarks>
public sealed class RetryOwnershipTests : IDisposable
{
    private readonly LocalHttpServer _server = new();
    private readonly HttpClient _httpClient = new();
    private readonly BreadthFirstCrawler _crawler;
    private readonly WebContentProcessor _processor;

    public RetryOwnershipTests()
    {
        _crawler = new BreadthFirstCrawler(new HttpClientService(_httpClient), Substitute.For<IEventPublisher>());
        var factory = Substitute.For<IServiceFactory>();
        factory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(_crawler);
        factory.TryCreateCacheService().Returns((ICacheService?)null);
        factory.TryCreateDomainRateLimiter().Returns((IDomainRateLimiter?)null);
        factory.TryCreateContentQualityEvaluator().Returns((IContentQualityEvaluator?)null);
        _processor = new WebContentProcessor(
            factory, Substitute.For<IEventPublisher>(), Substitute.For<ILogger<WebContentProcessor>>());
    }

    private static ExtractOptions Extract(int maxRetries) =>
        new() { MaxRetries = maxRetries, RespectRobotsTxt = false, EvaluateQuality = false };

    [Fact]
    public async Task Extract_ANotFound_IsAskedOnce()
    {
        _server.Status("/missing", 404);

        var result = await _processor.ExtractContentAsync(
            _server.Url("/missing"), Extract(maxRetries: 2), TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be(ExtractErrorCodes.NotFound);
        _server.Hits("/missing").Should().Be(1);
    }

    [Fact]
    public async Task Extract_AServerError_IsRetried_MaxRetriesTimes()
    {
        // The positive control: the rule is "do not retry what cannot change", not "never retry".
        _server.Status("/flaky", 503);

        await _processor.ExtractContentAsync(
            _server.Url("/flaky"), Extract(maxRetries: 1), TestContext.Current.CancellationToken);

        _server.Hits("/flaky").Should().Be(2);
    }

    [Fact]
    public async Task Extract_ATransportError_IsRetried_MaxRetriesTimes_NotTheProductOfTwoLayers()
    {
        _server.Abort("/reset");

        await _processor.ExtractContentAsync(
            _server.Url("/reset"), Extract(maxRetries: 1), TestContext.Current.CancellationToken);

        // Two layers each retrying would make this (1 + 1) x (1 + 3) = 8.
        _server.Hits("/reset").Should().Be(2);
    }

    [Fact]
    public async Task Crawl_ANotFound_IsAskedOnce_AndAServerErrorIsRetried()
    {
        _server.Status("/missing", 404);
        _server.Status("/flaky", 503);
        var options = new CrawlOptions { MaxRetries = 1, RespectRobotsTxt = false };

        await _crawler.CrawlAsync(_server.Url("/missing"), options, TestContext.Current.CancellationToken);
        await _crawler.CrawlAsync(_server.Url("/flaky"), options, TestContext.Current.CancellationToken);

        _server.Hits("/missing").Should().Be(1);
        _server.Hits("/flaky").Should().Be(2);
    }

    public void Dispose()
    {
        _processor.Dispose();
        _server.Dispose();
        _httpClient.Dispose();
    }
}
