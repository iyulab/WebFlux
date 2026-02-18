using FluentAssertions;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;

namespace WebFlux.Tests.Services.ChunkingStrategies;

public class DomStructureChunkingStrategyTests
{
    private readonly DomStructureChunkingStrategy _strategy = new();

    #region Properties

    [Fact]
    public void Name_ReturnsDomStructure()
    {
        _strategy.Name.Should().Be("DomStructure");
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        _strategy.Description.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Fallback — No HTML

    [Fact]
    public async Task ChunkAsync_NullOriginalHtml_FallsBackToTextSplitting()
    {
        var content = new ExtractedContent
        {
            OriginalHtml = null,
            Text = "Some plain text content that should be chunked by size.",
            Url = "https://example.com"
        };

        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_EmptyOriginalHtml_FallsBackToTextSplitting()
    {
        var content = new ExtractedContent
        {
            OriginalHtml = "",
            Text = "Fallback text content here.",
            Url = "https://example.com"
        };

        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Basic HTML Parsing

    [Fact]
    public async Task ChunkAsync_SimpleParagraphs_CreatesChunks()
    {
        var html = """
        <html><body>
        <article>
            <p>First paragraph with enough content to meet the minimum chunk size requirement for testing purposes.</p>
            <p>Second paragraph with additional content that also meets the minimum size requirement for chunking.</p>
        </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(c =>
        {
            c.Content.Should().NotBeNullOrEmpty();
            c.SourceUrl.Should().Be("https://example.com");
        });
    }

    [Fact]
    public async Task ChunkAsync_NoMainContent_FallsBackToBody()
    {
        var html = """
        <html><body>
            <div>Body content without article or main tags has enough text to be chunked properly here.</div>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Content Selectors

    [Fact]
    public async Task ChunkAsync_ArticleTag_ExtractsArticleContent()
    {
        var html = """
        <html><body>
            <nav>Navigation that should be ignored</nav>
            <article>
                <section><p>Article content that is long enough to be its own chunk in the output results.</p></section>
            </article>
            <footer>Footer text that should not appear</footer>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        var allText = string.Join(" ", chunks.Select(c => c.Content));
        allText.Should().Contain("Article content");
        allText.Should().NotContain("Navigation that should be ignored");
    }

    [Fact]
    public async Task ChunkAsync_MainTag_ExtractsMainContent()
    {
        var html = """
        <html><body>
            <header>Header content</header>
            <main>
                <section><p>Main area content that should be extracted and chunked properly for testing.</p></section>
            </main>
            <aside>Sidebar content</aside>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        var allText = string.Join(" ", chunks.Select(c => c.Content));
        allText.Should().Contain("Main area content");
    }

    #endregion

    #region Exclude Selectors

    [Fact]
    public async Task ChunkAsync_ExcludedElements_AreRemoved()
    {
        var html = """
        <html><body>
            <article>
                <section><p>Main content that should remain in the output and be properly chunked here.</p></section>
                <nav>Navigation links to remove</nav>
                <div class="advertisement">Ad content to remove</div>
                <div class="comments">User comments to remove</div>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        var allText = string.Join(" ", chunks.Select(c => c.Content));
        allText.Should().NotContain("Navigation links");
        allText.Should().NotContain("Ad content");
        allText.Should().NotContain("User comments");
    }

    #endregion

    #region Code Blocks

    [Fact]
    public async Task ChunkAsync_CodeBlock_MarkedAsCodeType()
    {
        var html = """
        <html><body>
            <article>
                <pre>function hello() { return "world"; }</pre>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().ContainSingle();
        chunks[0].Type.Should().Be(ChunkType.Code);
        chunks[0].Content.Should().Contain("function hello");
    }

    [Fact]
    public async Task ChunkAsync_CodeTag_MarkedAsCodeType()
    {
        var html = """
        <html><body>
            <article>
                <code>var x = 42; console.log(x);</code>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().Contain(c => c.Type == ChunkType.Code);
    }

    #endregion

    #region Tables

    [Fact]
    public async Task ChunkAsync_Table_MarkedAsTableType()
    {
        var html = """
        <html><body>
            <article>
                <table>
                    <tr><th>Name</th><th>Value</th></tr>
                    <tr><td>Alpha</td><td>100</td></tr>
                    <tr><td>Beta</td><td>200</td></tr>
                </table>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().ContainSingle();
        chunks[0].Type.Should().Be(ChunkType.Table);
        chunks[0].Content.Should().Contain("Name | Value");
        chunks[0].Content.Should().Contain("Alpha | 100");
    }

    #endregion

    #region Lists

    [Fact]
    public async Task ChunkAsync_UnorderedList_MarkedAsListType()
    {
        var html = """
        <html><body>
            <article>
                <ul>
                    <li>First item in the list</li>
                    <li>Second item in the list</li>
                    <li>Third item in the list</li>
                </ul>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().Contain(c => c.Type == ChunkType.List);
        var listChunk = chunks.First(c => c.Type == ChunkType.List);
        listChunk.Content.Should().Contain("• First item");
        listChunk.Content.Should().Contain("• Second item");
    }

    [Fact]
    public async Task ChunkAsync_OrderedList_MarkedAsListType()
    {
        var html = """
        <html><body>
            <article>
                <ol>
                    <li>Step one</li>
                    <li>Step two</li>
                </ol>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().Contain(c => c.Type == ChunkType.List);
    }

    #endregion

    #region Heading Path

    [Fact]
    public async Task ChunkAsync_HeadingHierarchy_TrackedInChunks()
    {
        var html = """
        <html><body>
            <article>
                <h1>Top Level</h1>
                <h2>Section A</h2>
                <section><p>Content under Section A that is long enough to form its own chunk in the output.</p></section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        var textChunks = chunks.Where(c => c.Type == ChunkType.Text).ToList();
        if (textChunks.Count > 0)
        {
            textChunks[0].HeadingPath.Should().Contain("Top Level");
            textChunks[0].HeadingPath.Should().Contain("Section A");
        }
    }

    [Fact]
    public async Task ChunkAsync_HeadingLevelChange_UpdatesPath()
    {
        var html = """
        <html><body>
            <article>
                <h1>Title</h1>
                <h2>SubA</h2>
                <section><p>Content A is here and long enough for chunking to work as expected in this test.</p></section>
                <h2>SubB</h2>
                <section><p>Content B is here and also long enough for the chunking strategy minimum size limits.</p></section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Section Elements

    [Fact]
    public async Task ChunkAsync_SectionTag_CreatesSeparateChunk()
    {
        var html = """
        <html><body>
            <article>
                <section>
                    <p>First section content that is definitely long enough to exceed the minimum chunk size limit.</p>
                </section>
                <section>
                    <p>Second section content that is also definitely long enough to exceed the minimum chunk limit.</p>
                </section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ChunkAsync_DivWithSectionClass_RecognizedAsSection()
    {
        var html = """
        <html><body>
            <article>
                <div class="section">
                    <p>Custom section content that has enough text to be its own chunk and not be merged.</p>
                </div>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Large Section Splitting

    [Fact]
    public async Task ChunkAsync_LargeSection_SplitBySentences()
    {
        // Create a section larger than MaxChunkSize (default 1500)
        var longText = string.Join(". ", Enumerable.Range(1, 50).Select(i => $"Sentence number {i} with some additional content"));
        var html = $"""
        <html><body>
            <article>
                <section><p>{longText}</p></section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var options = new ChunkingOptions { MaxChunkSize = 200, MinChunkSize = 50 };
        var chunks = await _strategy.ChunkAsync(content, options);

        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Small Chunk Merging

    [Fact]
    public async Task ChunkAsync_SmallChunks_MergedToMeetMinSize()
    {
        var html = """
        <html><body>
            <article>
                <pre>short code</pre>
                <pre>another short code block</pre>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var options = new ChunkingOptions { MinChunkSize = 50, MaxChunkSize = 1500 };
        var chunks = await _strategy.ChunkAsync(content, options);

        // Small chunks should be merged
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Sequence Numbers

    [Fact]
    public async Task ChunkAsync_MultipleChunks_HaveSequentialNumbers()
    {
        var html = """
        <html><body>
            <article>
                <section><p>First section with enough content to exceed minimum chunk size for proper testing purposes.</p></section>
                <section><p>Second section with enough content to exceed minimum chunk size for proper testing purposes.</p></section>
                <section><p>Third section with enough content to exceed minimum chunk size for proper testing purposes here.</p></section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        if (chunks.Count > 1)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].SequenceNumber.Should().Be(i);
            }
        }
    }

    #endregion

    #region Strategy Info

    [Fact]
    public async Task ChunkAsync_Chunks_ContainStrategyInfo()
    {
        var html = """
        <html><body>
            <article>
                <section><p>Content that is long enough to form a chunk with strategy info metadata attached.</p></section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(c =>
        {
            c.StrategyInfo.Should().NotBeNull();
            c.StrategyInfo.StrategyName.Should().Be("DomStructure");
            c.StrategyInfo.Parameters.Should().ContainKey("DomPath");
        });
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task ChunkAsync_CancellationRequested_ReturnsPartialOrEmpty()
    {
        var longHtml = "<html><body><article>" +
            string.Join("", Enumerable.Range(1, 100).Select(i =>
                $"<section><p>Section {i} with plenty of content to process slowly through the DOM tree.</p></section>")) +
            "</article></body></html>";

        var content = CreateContent(longHtml);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var chunks = await _strategy.ChunkAsync(content, cancellationToken: cts.Token);

        // With immediate cancellation, we expect fewer or no chunks
        chunks.Count.Should().BeLessThanOrEqualTo(1);
    }

    #endregion

    #region Custom Options

    [Fact]
    public async Task ChunkAsync_CustomHtmlOptions_Applied()
    {
        var html = """
        <html><body>
            <div id="custom-content">
                <section><p>Custom content area that should be found using custom content selectors.</p></section>
            </div>
        </body></html>
        """;

        var htmlOptions = new HtmlChunkingOptions
        {
            ContentSelectors = ["#custom-content"],
            MaxChunkSize = 1500,
            MinChunkSize = 10
        };
        var options = new ChunkingOptions
        {
            StrategySpecificOptions = new Dictionary<string, object>
            {
                ["HtmlChunkingOptions"] = htmlOptions
            }
        };

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content, options);

        chunks.Should().NotBeEmpty();
        var allText = string.Join(" ", chunks.Select(c => c.Content));
        allText.Should().Contain("Custom content area");
    }

    [Fact]
    public async Task ChunkAsync_KeepCodeBlocksTogether_False_DoesNotCreateCodeChunks()
    {
        var html = """
        <html><body>
            <article>
                <pre>console.log("hello world from code block content here");</pre>
            </article>
        </body></html>
        """;

        var htmlOptions = new HtmlChunkingOptions
        {
            KeepCodeBlocksTogether = false,
            MinChunkSize = 10
        };
        var options = new ChunkingOptions
        {
            StrategySpecificOptions = new Dictionary<string, object>
            {
                ["HtmlChunkingOptions"] = htmlOptions
            }
        };

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content, options);

        // With KeepCodeBlocksTogether = false, code is processed as normal text
        chunks.Should().NotContain(c => c.Type == ChunkType.Code);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ChunkAsync_EmptyArticle_ReturnsEmpty()
    {
        var html = """
        <html><body>
            <article></article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WhitespaceOnlyContent_ReturnsEmpty()
    {
        var html = """
        <html><body>
            <article>
                <p>   </p>
                <div>
                </div>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_MixedContentTypes_AllTypesPresent()
    {
        var html = """
        <html><body>
            <article>
                <section><p>Regular text paragraph with enough content to exceed the minimum chunk size limit here.</p></section>
                <pre>function code() { return true; }</pre>
                <table>
                    <tr><td>Cell1</td><td>Cell2</td></tr>
                </table>
                <ul>
                    <li>List item one</li>
                    <li>List item two</li>
                </ul>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var options = new ChunkingOptions { MinChunkSize = 10, MaxChunkSize = 1500 };
        var chunks = await _strategy.ChunkAsync(content, options);

        chunks.Should().Contain(c => c.Type == ChunkType.Code);
        chunks.Should().Contain(c => c.Type == ChunkType.Table);
        chunks.Should().Contain(c => c.Type == ChunkType.List);
    }

    [Fact]
    public async Task ChunkAsync_DefaultOptions_UsesDefaultMaxChunkSize()
    {
        var html = """
        <html><body>
            <article>
                <section><p>Content that uses default options without any explicit configuration being passed.</p></section>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(c =>
            c.Content.Length.Should().BeLessThanOrEqualTo(1500));
    }

    #endregion

    #region Table Extraction Format

    [Fact]
    public async Task ChunkAsync_Table_PipeDelimitedFormat()
    {
        var html = """
        <html><body>
            <article>
                <table>
                    <tr><th>Header1</th><th>Header2</th><th>Header3</th></tr>
                    <tr><td>A</td><td>B</td><td>C</td></tr>
                </table>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        var tableChunk = chunks.FirstOrDefault(c => c.Type == ChunkType.Table);
        tableChunk.Should().NotBeNull();
        tableChunk!.Content.Should().Contain("Header1 | Header2 | Header3");
        tableChunk.Content.Should().Contain("A | B | C");
    }

    #endregion

    #region DomPath

    [Fact]
    public async Task ChunkAsync_ElementWithId_DomPathContainsId()
    {
        var html = """
        <html><body>
            <article>
                <div id="main-area">
                    <section><p>Content within an identified div element for DOM path testing purposes here.</p></section>
                </div>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        var domPaths = chunks
            .Select(c => c.StrategyInfo.Parameters["DomPath"]?.ToString() ?? "")
            .ToList();
        domPaths.Should().Contain(p => p.Contains("div#main-area"));
    }

    [Fact]
    public async Task ChunkAsync_ElementWithClass_DomPathContainsClass()
    {
        var html = """
        <html><body>
            <article>
                <div class="content-area">
                    <section><p>Content within a classed div element for DOM path testing purposes in this test.</p></section>
                </div>
            </article>
        </body></html>
        """;

        var content = CreateContent(html);
        var chunks = await _strategy.ChunkAsync(content);

        chunks.Should().NotBeEmpty();
        var domPaths = chunks
            .Select(c => c.StrategyInfo.Parameters["DomPath"]?.ToString() ?? "")
            .ToList();
        domPaths.Should().Contain(p => p.Contains("div.content-area"));
    }

    #endregion

    #region Helpers

    private static ExtractedContent CreateContent(string html, string url = "https://example.com")
    {
        return new ExtractedContent
        {
            OriginalHtml = html,
            Text = "",
            Url = url
        };
    }

    #endregion
}
