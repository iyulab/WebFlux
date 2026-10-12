using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using WebFlux.Services.Crawlers;
using WebFlux.Tests.TestSupport;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// The crawler as a consumer gets it (<c>AddWebFlux()</c>) against real HTTP on a local listener: status codes, a slow
/// answer, a large body, robots.txt rules. These facts used to call a public HTTP test service and failed whenever that
/// service was down; served locally they can only fail on WebFlux, so they run with the unit suite.
/// </summary>
public sealed class CrawlerHttpBehaviourTests : IAsyncDisposable
{
    private const string Html = "<html><head><title>Moby Dick</title></head><body><h1>Herman Melville</h1>" +
        "<p>Availing himself of the mild, summer-cool weather that now reigned in these latitudes.</p></body></html>";

    private readonly LocalHttpServer _server = new();
    private readonly ServiceProvider _provider;
    private readonly ICrawler _crawler;
    private readonly IWebContentProcessor _processor;

    public CrawlerHttpBehaviourTests()
    {
        var services = new ServiceCollection();
        services.AddWebFlux();
        _provider = services.BuildServiceProvider();
        _crawler = _provider.GetRequiredService<ICrawlerFactory>().CreateCrawler(CrawlStrategy.BreadthFirst);
        _processor = _provider.GetRequiredService<IWebContentProcessor>();
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        _server.Dispose();
    }

    private static CrawlOptions SinglePage => new() { MaxDepth = 0, MaxPages = 1 };

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    public async Task A_status_code_is_recorded_and_decides_success(int statusCode, bool expectSuccess)
    {
        _server.Status($"/status/{statusCode}", statusCode);

        var result = await _crawler.CrawlAsync(
            _server.Url($"/status/{statusCode}"), new CrawlOptions { MaxRetries = 0 }, TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(statusCode);
        result.IsSuccess.Should().Be(expectSuccess);
    }

    [Fact]
    public async Task A_slow_answer_inside_the_timeout_succeeds()
    {
        _server.Delay("/delay/1", TimeSpan.FromSeconds(1));

        var result = await _crawler.CrawlAsync(
            _server.Url("/delay/1"), new CrawlOptions { MaxRetries = 2, TimeoutMs = 10_000 }, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.ResponseTimeMs.Should().BeGreaterThanOrEqualTo(900);
    }

    [Fact]
    public async Task A_large_binary_body_is_read_whole()
    {
        _server.Serve("/bytes/102400", new string('x', 102_400), "application/octet-stream");

        var result = await _crawler.CrawlAsync(_server.Url("/bytes/102400"), cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.ContentLength.Should().BeGreaterThan(100_000);
    }

    [Fact]
    public async Task An_html_page_comes_back_as_html_and_is_counted()
    {
        _server.Serve("/html", Html);
        _server.Serve("/other", Html);

        var result = await _crawler.CrawlAsync(_server.Url("/html"), cancellationToken: TestContext.Current.CancellationToken);
        await _crawler.CrawlAsync(_server.Url("/other"), cancellationToken: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.HtmlContent.Should().Contain("Herman Melville");
        var stats = _crawler.GetStatistics();
        stats.TotalRequests.Should().BeGreaterThanOrEqualTo(2);
        stats.SuccessfulRequests.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task A_path_robots_txt_disallows_is_refused_by_the_crawler()
    {
        ServeRobotsDenying("/deny");

        var result = await _crawler.CrawlAsync(_server.Url("/deny"), SinglePage, TestContext.Current.CancellationToken);

        result.DisallowedByRobotsTxt.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        _server.Hits("/deny").Should().Be(0, "a refused path is never requested");
    }

    [Fact]
    public async Task A_refusal_arrives_as_its_own_error_code_on_the_extract_path()
    {
        ServeRobotsDenying("/deny");

        var ex = await Assert.ThrowsAsync<WebExtractionException>(() => _processor.ExtractContentAsync(
            _server.Url("/deny"), new ExtractOptions { EvaluateQuality = false, UseCache = false }, TestContext.Current.CancellationToken));

        ex.ErrorCode.Should().Be(ExtractErrorCodes.DisallowedByRobotsTxt);
    }

    [Fact]
    public async Task The_robots_opt_out_reaches_the_extract_path()
    {
        ServeRobotsDenying("/deny");

        var failure = await Record.ExceptionAsync(() => _processor.ExtractContentAsync(
            _server.Url("/deny"),
            new ExtractOptions { RespectRobotsTxt = false, EvaluateQuality = false, UseCache = false },
            TestContext.Current.CancellationToken));

        ((failure as WebExtractionException)?.ErrorCode).Should().NotBe(ExtractErrorCodes.DisallowedByRobotsTxt);
        _server.Hits("/deny").Should().Be(1, "with the opt-out the page itself is requested");
    }

    // The public test service's file, as measured 2026-09-20: "User-agent: *" + "Disallow: /deny".
    private void ServeRobotsDenying(string path)
    {
        _server.Serve("/robots.txt", $"User-agent: *\nDisallow: {path}\n", "text/plain");
        _server.Serve(path, Html);
    }
}
