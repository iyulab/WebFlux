using AwesomeAssertions;
using WebFlux.Services.ContentExtractors;
using Xunit;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// <see cref="BasicContentExtractor"/> is the pipeline's fallback for HTML and the extractor registered for
/// Markdown. It keeps the text, but it must keep the structure a reader sees: separate blocks stay separate,
/// script text is not content, and a Markdown page names itself by its own title.
/// </summary>
public class BasicContentExtractorStructureTests
{
    private const string Url = "https://example.com/docs/page";
    private readonly BasicContentExtractor _extractor = new(null);

    #region HTML fallback keeps block boundaries

    [Fact]
    public async Task Html_ParagraphsLinesListItemsAndCells_AreNotFusedTogether()
    {
        var html = "<html><body><p>Line one<br>Line two</p><p>Next paragraph</p><ul><li>Alpha</li><li>Beta</li></ul>" +
                   "<table><tr><td>key</td><td>value</td></tr></table></body></html>";

        var result = await _extractor.ExtractFromHtmlAsync(html, Url, cancellationToken: TestContext.Current.CancellationToken);

        result.Text.Split('\n').Should().Equal("Line one", "Line two", "Next paragraph", "Alpha", "Beta", "key value");
    }

    [Fact]
    public async Task Html_InlineElements_DoNotSplitWords()
    {
        var html = "<html><body><p>un<b>believ</b>able and <i>italic</i> text</p></body></html>";

        var result = await _extractor.ExtractFromHtmlAsync(html, Url, cancellationToken: TestContext.Current.CancellationToken);

        result.Text.Should().Be("unbelievable and italic text");
    }

    [Fact]
    public async Task Html_ScriptAndStyleText_IsNotContent()
    {
        var html = "<html><head><style>.x{color:red}</style><script>var secret = 1;</script></head>" +
                   "<body><p>Visible text</p><script>alert('inline')</script><noscript>Enable JS</noscript></body></html>";

        var result = await _extractor.ExtractFromHtmlAsync(html, Url, cancellationToken: TestContext.Current.CancellationToken);

        result.Text.Should().Be("Visible text");
    }

    [Fact]
    public async Task Html_TitleWhitespace_IsCollapsed()
    {
        var html = "<html><head><title>\n  Spaced   title\n</title></head><body><p>Body</p></body></html>";

        var result = await _extractor.ExtractFromHtmlAsync(html, Url, cancellationToken: TestContext.Current.CancellationToken);

        result.Title.Should().Be("Spaced title");
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("text/html; charset=utf-8")]
    [InlineData("TEXT/HTML;charset=UTF-8")]
    [InlineData("application/xhtml+xml; charset=utf-8")]
    public async Task Auto_ContentTypeWithParameters_IsRecognised(string contentType)
    {
        var result = await _extractor.ExtractAutoAsync("<html><body><p>One</p><p>Two</p></body></html>", Url, contentType,
            TestContext.Current.CancellationToken);

        result.Text.Should().Be("One\nTwo");
    }

    [Theory]
    [InlineData("text/markdown; charset=utf-8")]
    [InlineData("text/x-markdown")]
    public async Task Auto_MarkdownContentTypeVariants_AreRecognised(string contentType)
    {
        var result = await _extractor.ExtractAutoAsync("# Named page\n\nBody.", Url, contentType, TestContext.Current.CancellationToken);

        result.Title.Should().Be("Named page");
    }

    #endregion

    #region Markdown names itself

    [Fact]
    public async Task Markdown_FirstHeading_IsTheTitle_AndHeadingsAreListed()
    {
        var markdown = "Some intro text.\n\n# Install guide\n\nText.\n\n## Requirements\n\nMore.\n\n### Detail ###\n";

        var result = await _extractor.ExtractFromMarkdownAsync(markdown, Url, TestContext.Current.CancellationToken);

        result.Title.Should().Be("Install guide");
        result.Headings.Should().Equal("# Install guide", "## Requirements", "### Detail");
        result.Text.Should().Be(markdown);
    }

    [Fact]
    public async Task Markdown_FrontMatterTitle_WinsOverTheFirstHeading_AndStaysInTheText()
    {
        var markdown = "---\ntitle: \"Front matter title\"\ntags: [a, b]\n---\n# Heading\n\nBody.\n";

        var result = await _extractor.ExtractFromMarkdownAsync(markdown, Url, TestContext.Current.CancellationToken);

        result.Title.Should().Be("Front matter title");
        result.Headings.Should().Equal("# Heading");
        result.Text.Should().Be(markdown);
    }

    [Fact]
    public async Task Markdown_HashLinesInsideCodeFences_AreNotHeadings()
    {
        var markdown = "# Real\n\n```bash\n# a comment, not a heading\necho hi\n```\n\n~~~\n## also not\n~~~\n\n## After\n";

        var result = await _extractor.ExtractFromMarkdownAsync(markdown, Url, TestContext.Current.CancellationToken);

        result.Headings.Should().Equal("# Real", "## After");
    }

    [Fact]
    public async Task Markdown_WithCrLfLineEndings_IsReadTheSame()
    {
        var markdown = "---\r\ntitle: CRLF page\r\n---\r\n\r\n# Heading\r\n\r\nBody.\r\n";

        var result = await _extractor.ExtractFromMarkdownAsync(markdown, Url, TestContext.Current.CancellationToken);

        result.Title.Should().Be("CRLF page");
        result.Headings.Should().Equal("# Heading");
    }

    [Fact]
    public async Task Markdown_WithoutAnyHeading_KeepsThePlaceholderTitle()
    {
        var result = await _extractor.ExtractFromMarkdownAsync("Just a paragraph.\n", Url, TestContext.Current.CancellationToken);

        result.Title.Should().Be("Markdown Content");
        result.Headings.Should().BeEmpty();
    }

    [Fact]
    public async Task Markdown_SetextAndIndentedLookalikes_AreNotAtxHeadings()
    {
        var markdown = "    # indented code, not a heading\n\n#hashtag is not a heading\n\n# Real heading\n";

        var result = await _extractor.ExtractFromMarkdownAsync(markdown, Url, TestContext.Current.CancellationToken);

        result.Headings.Should().Equal("# Real heading");
    }

    #endregion
}
