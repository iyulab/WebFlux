using AwesomeAssertions;
using Flux.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// A WebFlux chunk is an <see cref="IEnrichedChunk"/>, and consumers of that contract read <see cref="IEnrichedChunk.Source"/>
/// (a contextual header's document title, a classifier's source type). No chunking path ever set it, so the property threw
/// <see cref="InvalidOperationException"/> on every chunk WebFlux produced.
/// </summary>
public class ChunkSourceTests
{
    private const string Html = """
        <html lang="en"><head><title>Release notes</title></head>
        <body><main>
        <h1>Release notes</h1>
        <p>The scheduler now retries failed jobs with exponential backoff and records each attempt in the job history.</p>
        <p>Workers report their heartbeat every ten seconds, and a worker that misses three heartbeats is marked offline.</p>
        <p>The dashboard shows queue depth per tenant and lets an operator pause a queue without stopping its workers.</p>
        </main></body></html>
        """;

    [Fact]
    public async Task ProcessHtml_EveryChunk_CarriesTheSourcePage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebFlux();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IWebContentProcessor>();

        var chunks = await processor.ProcessHtmlAsync(
            Html,
            "https://example.test/notes",
            new ChunkingOptions { Strategy = ChunkingStrategyType.Paragraph, MaxChunkSize = 120, MinChunkSize = 10, ChunkOverlap = 0 },
            TestContext.Current.CancellationToken);

        chunks.Should().NotBeEmpty();
        foreach (IEnrichedChunk chunk in chunks)
        {
            var source = chunk.Source;
            source.Url.Should().Be("https://example.test/notes");
            source.SourceId.Should().Be("https://example.test/notes");
            source.SourceType.Should().Be("url");
            source.Title.Should().Be("Release notes");
            source.ChunkCount.Should().Be(chunks.Count);
        }
    }

    [Fact]
    public void ChunkBuiltWithoutASource_ReadsItsOwnPage_InsteadOfThrowing()
    {
        // A caller that runs a chunking strategy itself gets chunks no processor touched.
        IEnrichedChunk chunk = new WebContentChunk
        {
            Id = "c1",
            Content = "text",
            SourceUrl = "https://example.test/a",
            Title = "Page A",
            StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Paragraph" },
        };

        var source = chunk.Source;

        source.Url.Should().Be("https://example.test/a");
        source.SourceId.Should().Be("https://example.test/a");
        source.Title.Should().Be("Page A");
        source.SourceType.Should().Be("url");
    }
}
