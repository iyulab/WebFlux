using AwesomeAssertions;
using FluxCurator.Infrastructure.Chunking;
using WebFlux.Services;
using WebFlux.Services.ChunkingStrategies;
using WebFlux.Services.ContentExtractors;
using Xunit;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// A fenced code block is code: a "# comment" line in it is not a heading and the block is not cut into sections
/// at its short lines. A page with a shell snippet in a &lt;pre&gt; block is the ordinary shape.
/// </summary>
public class CodeBlockFidelityTests
{
    private const string PageWithShellSnippet = """
        <html><head><title>Setup</title></head><body><main>
        <h1>Setup guide</h1>
        <p>Install the tool and run it once to verify the install works as expected on your machine.</p>
        <pre><code># install dependencies
        pip install fluxtool

        # run the tool
        python main.py --verbose</code></pre>
        <p>After the run finishes you should see a summary of the processed files in the output folder.</p>
        </main></body></html>
        """;

    [Fact]
    public async Task HtmlExtraction_ACommentLineInAPreBlock_IsNotAHeading()
    {
        var extractor = new HtmlToMarkdownExtractor(null);

        var result = await extractor.ExtractFromHtmlAsync(PageWithShellSnippet, "https://example.com/setup", cancellationToken: TestContext.Current.CancellationToken);

        result.Headings.Should().Equal("# Setup guide");
        result.Text.Should().Contain("# install dependencies").And.Contain("# run the tool");
    }

    [Fact]
    public async Task HtmlExtraction_APreBlock_KeepsIndentationRepeatedLinesAndBlankLines()
    {
        var html = "<html><head><title>T</title></head><body><main><h1>Code</h1><p>Here is the snippet that matters for the whole page and its readers.</p>"
            + "<pre><code>def f():\n    if x:\n        return 1\n\n\n\n    return 2\n\n}\n}\n}\nline   with   spaces   </code></pre>"
            + "<p>After the snippet comes a closing paragraph with enough words to keep the density filter happy.</p></main></body></html>";
        var extractor = new HtmlToMarkdownExtractor(null);

        var result = await extractor.ExtractFromHtmlAsync(html, "https://example.com/c", cancellationToken: TestContext.Current.CancellationToken);

        result.Text.Should().Contain("def f():\n    if x:\n        return 1\n\n\n\n    return 2\n\n}\n}\n}\nline   with   spaces   \n```");
    }

    [Fact]
    public async Task HtmlExtraction_BlankLineRunsOutsideCode_StillCollapseToOne()
    {
        var html = "<html><head><title>T</title></head><body><main><p>First paragraph.</p><p></p><p></p><br><br><br><p>Second paragraph.</p></main></body></html>";
        var extractor = new HtmlToMarkdownExtractor(null);

        var result = await extractor.ExtractFromHtmlAsync(html, "https://example.com/c", cancellationToken: TestContext.Current.CancellationToken);

        result.Text.Should().NotContain("\n\n\n");
    }

    [Fact]
    public async Task SmartChunking_ACodeBlock_IsNotCutAtItsLines()
    {
        var extractor = new HtmlToMarkdownExtractor(null);
        var content = await extractor.ExtractFromHtmlAsync(PageWithShellSnippet, "https://example.com/setup", cancellationToken: TestContext.Current.CancellationToken);
        var strategy = new SmartChunkingStrategy(new ChunkerFactory());

        var chunks = await strategy.ChunkAsync(content, null, TestContext.Current.CancellationToken);

        chunks.Should().Contain(c =>
            c.Content.Contains("# install dependencies")
            && c.Content.Contains("pip install fluxtool")
            && c.Content.Contains("# run the tool")
            && c.Content.Contains("python main.py --verbose"));
        chunks.Should().NotContain(c => c.Content.Trim() == "```", "the fence line is part of the block, not a section of its own");
    }

    [Fact]
    public async Task MarkdownOutline_AFenceShownInsideALongerFence_HidesItsHashLines()
    {
        // A four-backtick fence holds a three-backtick fence; the inner one does not close the outer.
        var markdown = "# Title\n\n````md\n```\n# not a heading\n```\n# still not a heading\n````\n\n## Real section\n";
        var extractor = new BasicContentExtractor(null);

        var result = await extractor.ExtractFromMarkdownAsync(markdown, "https://example.com/doc", TestContext.Current.CancellationToken);

        result.Headings.Should().Equal("# Title", "## Real section");
    }

    [Fact]
    public void Fences_BacktickAndTildeBlocks_AreFencedWithTheirFenceLines()
    {
        Fenced("a\n```\nb\n```\nc").Should().Equal(false, true, true, true, false);
        Fenced("a\n~~~\nb\n~~~\nc").Should().Equal(false, true, true, true, false);
    }

    [Fact]
    public void Fences_AClosingFenceMustBeAsLongAndHaveNoInfoString()
    {
        // ``` does not close ````; "``` js" does not close ```; "~~~" does not close a backtick fence.
        Fenced("````\nx\n```\ny\n````\nz").Should().Equal(true, true, true, true, true, false);
        Fenced("```\nx\n``` js\ny\n```\nz").Should().Equal(true, true, true, true, true, false);
        Fenced("```\nx\n~~~\ny\n```\nz").Should().Equal(true, true, true, true, true, false);
    }

    [Fact]
    public void Fences_AnUnclosedFence_RunsToTheEnd()
    {
        Fenced("a\n```\nb\nc").Should().Equal(false, true, true, true);
    }

    [Fact]
    public void Fences_NotFences_AreLeftAlone()
    {
        // inline code with three backticks, an indented code line (4 spaces), two backticks
        Fenced("```code``` here\nb").Should().Equal(false, false);
        Fenced("    ```\nb").Should().Equal(false, false);
        Fenced("``\nb").Should().Equal(false, false);
    }

    [Fact]
    public void Fences_AFenceIndentedByUpToThreeSpaces_StillOpens()
    {
        Fenced("   ```\nb\n   ```\nc").Should().Equal(true, true, true, false);
    }

    private static bool[] Fenced(string markdown) => MarkdownCodeFences.FencedLines(markdown.Split('\n'));
}
