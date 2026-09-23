using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using WebFlux.Tests.TestSupport;

namespace WebFlux.Tests.Services;

/// <summary>
/// <see cref="IContentChunkService"/> resolved the way a consumer gets it — <c>AddWebFlux()</c>, no Playwright package —
/// turns a served page into chunks. The processor's other tests hand it a substituted service factory, so the crawler
/// this path actually picks was never exercised.
/// </summary>
public sealed class ChunkServiceThroughRegistrationTests : IDisposable
{
    private const string Page =
        "<html><head><title>Guide</title></head><body><h1>Guide</h1>" +
        "<p>WebFlux turns web pages into chunks for retrieval. This paragraph is long enough to be kept as content " +
        "rather than dropped as boilerplate, and it says something a search could find.</p>" +
        "<p>A second paragraph adds more text so the chunker has something to work with on this page.</p>" +
        "</body></html>";

    private readonly LocalHttpServer _server = new();
    private readonly CapturingLoggerProvider _logs = new();
    private readonly ServiceProvider _provider;

    public ChunkServiceThroughRegistrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(_logs).SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug));
        services.AddWebFlux();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessUrlAsync_WithoutPlaywright_ReturnsChunksOfThePage()
    {
        _server.Serve("/guide", Page);
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContentChunkService>();

        var chunks = await service.ProcessUrlAsync(_server.Url("/guide"), cancellationToken: TestContext.Current.CancellationToken);

        chunks.Should().NotBeEmpty("the page has text. Log:" + Environment.NewLine + string.Join(Environment.NewLine, _logs.Lines));
        string.Concat(chunks.Select(c => c.Content)).Should().Contain("turns web pages into chunks");
    }

    [Fact]
    public async Task ProcessUrlsBatchAsync_WithoutPlaywright_ReturnsChunksForEachUrl()
    {
        _server.Serve("/a", Page);
        _server.Serve("/b", Page);
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContentChunkService>();

        var results = await service.ProcessUrlsBatchAsync(
            [_server.Url("/a"), _server.Url("/b")],
            new ChunkingOptions { MaxChunkSize = 512, ChunkOverlap = 64 },
            TestContext.Current.CancellationToken);

        results.Should().HaveCount(2);
        results.Values.Should().OnlyContain(chunks => chunks.Count > 0, "each page was served and has text");
    }

    [Fact]
    public async Task ProcessUrlAsync_ProcessesThatPage_NotTheSiteItLinksTo()
    {
        _server.Serve("/start", Page.Replace("</body>", "<a href=\"/other\">other</a></body>"));
        _server.Serve("/other", Page);
        using var scope = _provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContentChunkService>();

        var chunks = await service.ProcessUrlAsync(_server.Url("/start"), cancellationToken: TestContext.Current.CancellationToken);

        chunks.Should().NotBeEmpty();
        _server.Hits("/other").Should().Be(0, "a per-URL call processes that page; ProcessWebsiteAsync is the site crawl");
    }

    [Fact]
    public async Task ProcessUrlsBatchAsync_APipelineThatCannotRun_Throws_InsteadOfAnEmptyResultPerUrl()
    {
        // Positive control for the batch's catch: before, a missing package surfaced as {url: []} for every URL.
        var factory = NSubstitute.Substitute.For<IServiceFactory>();
        factory.CreateCrawler(NSubstitute.Arg.Any<WebFlux.Core.Models.CrawlStrategy>())
            .Returns(_ => throw new InvalidOperationException("no crawler registered"));
        var processor = new WebFlux.Services.WebContentProcessor(
            factory,
            NSubstitute.Substitute.For<IEventPublisher>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WebFlux.Services.WebContentProcessor>.Instance);

        var act = () => processor.ProcessUrlsBatchAsync([_server.Url("/a")], cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _server.Dispose();
    }
}
