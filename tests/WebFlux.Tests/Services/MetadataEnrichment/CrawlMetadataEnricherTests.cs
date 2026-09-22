using AwesomeAssertions;
using Flux.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using WebFlux.Infrastructure.Html;
using WebFlux.Services.MetadataEnrichment;
using Xunit;

namespace WebFlux.Tests.Services.MetadataEnrichment;

/// <summary>
/// Five <see cref="CrawlOptions"/> members promised a metadata subsystem that was implemented and reachable
/// from nowhere. These facts pin that each option now changes what a crawl result carries — in both
/// directions — and that the default (nothing set) still leaves the extractor's own fields untouched.
/// </summary>
public sealed class CrawlMetadataEnricherTests
{
    private const string Html = """
        <html><head><title>Page Title</title>
        <meta name="description" content="meta description">
        <meta property="og:title" content="OG Title"><meta property="og:description" content="OG Description"><meta property="og:type" content="article">
        </head><body><h1>Heading One</h1><p>Body text here.</p></body></html>
        """;

    private static ExtractedContent Extracted(string? title = "Extractor Title", string? description = "Extractor description") => new()
    {
        Url = "https://example.com/page",
        Title = title ?? string.Empty,
        Text = "Body text here.",
        MainContent = "Body text here.",
        Headings = ["Heading One", "Heading Two"],
        Metadata = new EnrichedMetadata
        {
            Url = "https://example.com/page",
            Domain = "example.com",
            Title = title,
            Description = description,
            Language = "en",
            Keywords = ["body", "text"],
            Source = MetadataSource.Html,
        },
    };

    private static CrawlMetadataEnricher Enricher(IWebMetadataExtractor? ai = null) =>
        new(new HtmlMetadataExtractor(NullLogger<HtmlMetadataExtractor>.Instance), ai, NullLogger<CrawlMetadataEnricher>.Instance);

    [Fact]
    public async Task UseHtmlMetadata_attaches_the_snapshot_and_fills_only_what_was_empty()
    {
        var on = await Enricher().EnrichAsync(Extracted(), Html, "text/html", new CrawlOptions { UseHtmlMetadata = true }, TestContext.Current.CancellationToken);
        var off = await Enricher().EnrichAsync(Extracted(), Html, "text/html", new CrawlOptions { UseHtmlMetadata = false }, TestContext.Current.CancellationToken);

        on.Metadata!.HtmlMetadata.Should().NotBeNull();
        on.Metadata.HtmlMetadata!.OpenGraph!.Title.Should().Be("OG Title");
        on.Metadata.HtmlMetadata.MetaTags.Should().ContainKey("description");
        off.Metadata!.HtmlMetadata.Should().BeNull();

        // What the extractor already had is byte-for-byte what it still has.
        on.Metadata.Title.Should().Be(off.Metadata.Title).And.Be("Extractor Title");
        on.Metadata.Description.Should().Be(off.Metadata.Description).And.Be("Extractor description");
        on.Metadata.Keywords.Should().Equal(off.Metadata.Keywords);
        on.Metadata.Language.Should().Be(off.Metadata.Language);
    }

    [Fact]
    public async Task UseHtmlMetadata_fills_an_empty_title_and_description_from_OpenGraph()
    {
        var result = await Enricher().EnrichAsync(Extracted(title: null, description: null), Html, "text/html", new CrawlOptions(), TestContext.Current.CancellationToken);

        result.Metadata!.Title.Should().Be("OG Title");
        result.Metadata.Description.Should().Be("OG Description");
        result.Metadata.FieldSources["title"].Should().Be(MetadataSource.Html);
    }

    [Fact]
    public async Task A_non_html_page_gets_no_snapshot_even_when_asked()
    {
        var result = await Enricher().EnrichAsync(Extracted(), "# Markdown", "text/markdown", new CrawlOptions { UseHtmlMetadata = true }, TestContext.Current.CancellationToken);

        result.Metadata!.HtmlMetadata.Should().BeNull();
    }

    [Fact]
    public async Task EnableMetadataExtraction_off_never_calls_the_AI_even_when_one_is_available()
    {
        var ai = new RecordingExtractor();

        await Enricher(ai).EnrichAsync(Extracted(), Html, "text/html", new CrawlOptions { EnableMetadataExtraction = false }, TestContext.Current.CancellationToken);

        ai.Calls.Should().Be(0);
    }

    [Fact]
    public async Task EnableMetadataExtraction_on_calls_the_AI_with_the_schema_prompt_and_snapshot_and_merges_the_result()
    {
        var ai = new RecordingExtractor { Result = new EnrichedMetadata { Url = "https://example.com/page", Domain = "example.com", Topics = ["alpha", "beta"], OverallConfidence = 0.9f, Source = MetadataSource.AI } };
        var options = new CrawlOptions { EnableMetadataExtraction = true, MetadataSchema = MetadataSchema.Article, MinConfidence = 0.5f };

        var result = await Enricher(ai).EnrichAsync(Extracted(), Html, "text/html", options, TestContext.Current.CancellationToken);

        ai.Calls.Should().Be(1);
        ai.LastSchema.Should().Be(MetadataSchema.Article);
        ai.LastSnapshot.Should().NotBeNull("UseHtmlMetadata is on by default, so the snapshot is the prompt hint");
        result.Metadata!.Topics.Should().Equal("alpha", "beta");
        result.Metadata.Title.Should().Be("Extractor Title", "what the AI did not answer is kept");
        result.Metadata.Keywords.Should().Equal("body", "text");
        result.Metadata.HtmlMetadata.Should().NotBeNull();
    }

    [Fact]
    public async Task A_custom_schema_passes_the_custom_prompt_through()
    {
        var ai = new RecordingExtractor();
        var options = new CrawlOptions { EnableMetadataExtraction = true, MetadataSchema = MetadataSchema.Custom, CustomMetadataPrompt = "extract the SKU" };

        await Enricher(ai).EnrichAsync(Extracted(), Html, "text/html", options, TestContext.Current.CancellationToken);

        ai.LastSchema.Should().Be(MetadataSchema.Custom);
        ai.LastCustomPrompt.Should().Be("extract the SKU");
    }

    [Fact]
    public void A_custom_schema_without_a_prompt_is_refused_by_Validate()
    {
        var invalid = new CrawlOptions { MetadataSchema = MetadataSchema.Custom }.Validate();
        var valid = new CrawlOptions { MetadataSchema = MetadataSchema.Custom, CustomMetadataPrompt = "x" }.Validate();

        invalid.IsValid.Should().BeFalse();
        invalid.Errors.Should().Contain(e => e.Contains("CustomMetadataPrompt"));
        valid.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task An_AI_result_below_MinConfidence_is_not_merged()
    {
        var ai = new RecordingExtractor { Result = new EnrichedMetadata { Url = "u", Domain = "d", Topics = ["noise"], OverallConfidence = 0.2f } };
        var options = new CrawlOptions { EnableMetadataExtraction = true, MinConfidence = 0.6f };

        var result = await Enricher(ai).EnrichAsync(Extracted(), Html, "text/html", options, TestContext.Current.CancellationToken);

        ai.Calls.Should().Be(1);
        result.Metadata!.Topics.Should().BeEmpty();
        result.Metadata.Title.Should().Be("Extractor Title");
    }

    [Fact]
    public async Task Enabled_without_any_AI_extractor_keeps_the_HTML_metadata_and_does_not_throw()
    {
        var enricher = Enricher(ai: null);

        var result = await enricher.EnrichAsync(Extracted(), Html, "text/html", new CrawlOptions { EnableMetadataExtraction = true }, TestContext.Current.CancellationToken);

        enricher.HasAiExtractor.Should().BeFalse();
        result.Metadata!.HtmlMetadata.Should().NotBeNull();
        result.Metadata.Source.Should().Be(MetadataSource.Html);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(200)]
    public async Task MetadataExtractionMaxChars_bounds_what_reaches_the_AI(int maxChars)
    {
        var ai = new RecordingExtractor();
        var extracted = Extracted();
        extracted.MainContent = new string('x', 5000);
        var options = new CrawlOptions { EnableMetadataExtraction = true, MetadataExtractionMaxChars = maxChars };

        await Enricher(ai).EnrichAsync(extracted, Html, "text/html", options, TestContext.Current.CancellationToken);

        ai.LastContent!.Length.Should().BeLessThanOrEqualTo(maxChars);
        ai.LastContent.Should().StartWith("Extractor Title\n", "the sample is title + headings + the first N characters");
    }

    [Fact]
    public void The_sampler_puts_title_and_headings_first_and_cuts_the_body()
    {
        var sample = MetadataContentSampler.Sample("T", ["H1", "H2"], new string('b', 100), maxChars: 20);

        sample.Should().Be("T\nH1\nH2\n" + new string('b', 12));
        MetadataContentSampler.Sample("T", ["H1"], "body", maxChars: 0).Should().Be("body", "a non-positive budget means no sampling");
    }

    [Fact]
    public void AddWebFlux_registers_the_enricher_without_AI_and_builds_the_AI_extractor_when_a_completion_service_exists()
    {
        var plain = new ServiceCollection().AddWebFlux().BuildServiceProvider();
        var withAi = new ServiceCollection().AddWebFlux().AddScoped<ITextCompletionService, EchoCompletion>().BuildServiceProvider();

        using var s1 = plain.CreateScope();
        using var s2 = withAi.CreateScope();
        ((CrawlMetadataEnricher)s1.ServiceProvider.GetRequiredService<ICrawlMetadataEnricher>()).HasAiExtractor.Should().BeFalse();
        ((CrawlMetadataEnricher)s2.ServiceProvider.GetRequiredService<ICrawlMetadataEnricher>()).HasAiExtractor.Should().BeTrue();
    }

    private sealed class EchoCompletion : ITextCompletionService
    {
        public Task<string> CompleteAsync(string prompt, Flux.Abstractions.TextCompletionOptions? options = null, CancellationToken cancellationToken = default) => Task.FromResult("{}");
    }

    private sealed class RecordingExtractor : IWebMetadataExtractor
    {
        public int Calls { get; private set; }
        public string? LastContent { get; private set; }
        public HtmlMetadataSnapshot? LastSnapshot { get; private set; }
        public MetadataSchema LastSchema { get; private set; }
        public string? LastCustomPrompt { get; private set; }
        public EnrichedMetadata Result { get; init; } = new() { Url = "u", Domain = "d", OverallConfidence = 1f, Source = MetadataSource.AI };

        public Task<EnrichedMetadata> ExtractAsync(string content, string url, HtmlMetadataSnapshot? htmlMetadata = null, MetadataSchema schema = MetadataSchema.General, string? customPrompt = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastContent = content;
            LastSnapshot = htmlMetadata;
            LastSchema = schema;
            LastCustomPrompt = customPrompt;
            return Task.FromResult(Result);
        }

        public Task<IReadOnlyList<EnrichedMetadata>> ExtractBatchAsync(IEnumerable<(string content, string url, HtmlMetadataSnapshot? htmlMetadata)> items, MetadataSchema schema = MetadataSchema.General, string? customPrompt = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EnrichedMetadata>>([]);

        public IReadOnlyList<MetadataSchema> GetSupportedSchemas() => [];
        public string GetSchemaDescription(MetadataSchema schema) => string.Empty;
    }
}
