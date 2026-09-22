using AwesomeAssertions;
using Markdig;
using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// <see cref="MarkdownConversionOptions"/> was eleven declared flags over a pipeline fixed in the constructor. Each fact
/// runs a flag both ways against a document that exercises it; the first pins that the defaults still produce the HTML
/// the fixed pipeline produced.
/// </summary>
public sealed class MarkdownConversionOptionsWiringTests
{
    private const string Doc = @"# Title

Bare link https://example.com/x and an emoji :smile: plus math $x^2$ and a footnote[^1].

- [ ] task
- [x] done

| A | B |
|---|---|
| 1 | 2 |

![alt](https://example.com/i.png)

[good](https://example.com/ok) [bad](<ht tp://not a url>)

[^1]: note
";

    private static readonly string[] AllMarkers = ["<table", "<input", "<a href=\"https://example.com/x\"", "class=\"footnote", "class=\"math\"", "id=\"title\""];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly MarkdownStructureAnalyzer _analyzer = new();

    [Fact]
    public async Task Defaults_reproduce_the_fixed_pipeline_byte_for_byte()
    {
        var fixedPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        var result = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, cancellationToken: Ct);

        result.Html.Should().Be(Markdown.ToHtml(Doc, fixedPipeline));
        result.Html.Should().Contain("<table").And.Contain("<input").And.Contain("<a href=\"https://example.com/x\"")
            .And.Contain("id=\"title\"").And.Contain("footnote").And.Contain("class=\"math\"");
        result.Html.Should().Contain(":smile:", "emoji conversion is off by default, as it always was");
    }

    [Theory]
    [InlineData("EnableTables", "<table")]
    [InlineData("EnableTaskLists", "<input")]
    [InlineData("EnableAutoLinks", "<a href=\"https://example.com/x\"")]
    [InlineData("EnableFootnotes", "class=\"footnote")]
    [InlineData("EnableMath", "class=\"math\"")]
    [InlineData("GenerateAnchorIds", "id=\"title\"")]
    public async Task Turning_a_flag_off_removes_that_extension_and_only_that_one(string flag, string marker)
    {
        var options = flag switch
        {
            "EnableTables" => new MarkdownConversionOptions { EnableTables = false },
            "EnableTaskLists" => new MarkdownConversionOptions { EnableTaskLists = false },
            "EnableAutoLinks" => new MarkdownConversionOptions { EnableAutoLinks = false },
            "EnableFootnotes" => new MarkdownConversionOptions { EnableFootnotes = false },
            "EnableMath" => new MarkdownConversionOptions { EnableMath = false },
            _ => new MarkdownConversionOptions { GenerateAnchorIds = false },
        };

        var off = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, options, Ct);
        var on = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, cancellationToken: Ct);

        on.Html.Should().Contain(marker);
        off.Html.Should().NotContain(marker, $"{flag} = false removes exactly that extension");
        // every other marker survives
        foreach (var other in AllMarkers.Where(m => m != marker))
            off.Html.Should().Contain(other, "the other extensions are untouched");
    }

    [Fact]
    public async Task EnableEmojis_adds_the_emoji_extension()
    {
        var on = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, new MarkdownConversionOptions { EnableEmojis = true }, Ct);

        on.Html.Should().NotContain(":smile:").And.Contain("😄");
    }

    [Fact]
    public async Task EnableExtensions_false_is_plain_CommonMark_whatever_the_other_flags_say()
    {
        var plain = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, new MarkdownConversionOptions { EnableExtensions = false, EnableTables = true, EnableEmojis = true }, Ct);

        plain.Html.Should().NotContain("<table").And.NotContain("<input").And.NotContain("id=\"title\"").And.Contain(":smile:");
    }

    [Fact]
    public async Task TableOfContents_and_image_info_are_only_built_when_asked_for()
    {
        var on = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, cancellationToken: Ct);
        var off = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, new MarkdownConversionOptions { GenerateTableOfContents = false, ExtractImageInfo = false }, Ct);

        on.StructureInfo.TableOfContents.Items.Should().NotBeEmpty();
        on.StructureInfo.Images.Should().ContainSingle();
        off.StructureInfo.TableOfContents.Items.Should().BeEmpty();
        off.StructureInfo.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateLinks_fills_a_syntactic_result_per_link_and_nothing_otherwise()
    {
        var off = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, cancellationToken: Ct);
        var on = await _analyzer.ConvertToHtmlWithStructureAsync(Doc, new MarkdownConversionOptions { ValidateLinks = true }, Ct);

        off.StructureInfo.Links.Should().NotBeEmpty().And.OnlyContain(l => l.ValidationResult == null);
        on.StructureInfo.Links.Should().OnlyContain(l => l.ValidationResult != null && l.ValidationResult.StatusCode == null, "syntactic only, no request");
        on.StructureInfo.Links.Single(l => l.Url == "https://example.com/ok").ValidationResult!.IsValid.Should().BeTrue();
        on.StructureInfo.Links.Single(l => l.Url.StartsWith("ht tp://", StringComparison.Ordinal)).ValidationResult!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeStructureAsync_carries_lists_footnotes_and_math_it_extracted()
    {
        var info = await _analyzer.AnalyzeStructureAsync(Doc, "test", Ct);

        info.Lists.Should().NotBeEmpty("the public result dropped Lists/Quotes/MathExpressions/Footnotes/Embeds on reassembly until 0.14.0");
        info.Footnotes.Should().NotBeEmpty();
        info.MathExpressions.Should().NotBeEmpty();
    }
}
