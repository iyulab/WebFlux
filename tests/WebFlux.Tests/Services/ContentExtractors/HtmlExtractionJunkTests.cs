using AwesomeAssertions;
using WebFlux.Services.ContentExtractors;
using Xunit;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// What the extractor must not write into <c>Text</c> / <c>ImageUrls</c>: empty image links, markup fragments,
/// hidden text, form-control options, invisible characters and OS-dependent line endings. Each case is a
/// small hand-written page; the controls at the end keep what is legitimately content.
/// </summary>
public class HtmlExtractionJunkTests
{
    private const string Url = "https://example.com/docs/guide/page.html";
    private const string Intro = "<p>Intro paragraph that is long enough to be kept by any density filter.</p>";

    private static async Task<WebFlux.Core.Models.ExtractedContent> Extract(string html)
    {
        var extractor = new HtmlToMarkdownExtractor(null);
        return await extractor.ExtractFromHtmlAsync(html, Url, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static Task<WebFlux.Core.Models.ExtractedContent> InArticle(string body)
        => Extract("<html><body><article>" + Intro + body + "</article></body></html>");

    #region images

    [Fact]
    public async Task LazyLoadedImage_UsesItsDataSrc()
    {
        var r = await InArticle("<img data-src=\"/lazy1.png\" alt=\"Lazy one\">");

        r.Text.Should().Contain("![Lazy one](https://example.com/lazy1.png)");
        r.ImageUrls.Should().Equal("https://example.com/lazy1.png");
    }

    [Fact]
    public async Task PlaceholderSrc_IsReplacedByTheRealDataSrc()
    {
        var r = await InArticle("<img src=\"data:image/gif;base64,R0lGOD\" data-src=\"/lazy2.png\" alt=\"Lazy two\">");

        r.Text.Should().Contain("![Lazy two](https://example.com/lazy2.png)").And.NotContain("base64");
        r.ImageUrls.Should().Equal("https://example.com/lazy2.png");
    }

    [Fact]
    public async Task ImageWithOnlyAnInlineDataUri_BecomesItsAltText_NotAnEmbeddedBlob()
    {
        var r = await InArticle("<p>Chart: <img src=\"data:image/png;base64,AAAA\" alt=\"Revenue by quarter\"> shows growth.</p>");

        r.Text.Should().Contain("Revenue by quarter").And.NotContain("base64").And.NotContain("](");
        r.ImageUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task ImageWithNoSourceAtAll_NeverProducesAnEmptyLink()
    {
        var r = await InArticle("<p>Look <img alt=\"Decorative swirl\"> here.</p>");

        r.Text.Should().NotContain("]()");
        r.ImageUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task ImageUrls_AreReadPerImage_NotAcrossNeighbours()
    {
        var r = await InArticle("<img data-src=\"/a.png\" alt=\"A\"><img alt=\"no source\"><img src=\"/real.png\" alt=\"Real\">" +
                                "<img src=\"data:image/gif;base64,R0lGOD\" alt=\"Placeholder\">");

        r.ImageUrls.Should().Equal("https://example.com/a.png", "https://example.com/real.png");
        r.ImageUrls.Should().OnlyContain(u => !u.Contains(')') && !u.Contains('!') && !u.Contains('['));
    }

    [Fact]
    public async Task ImageWithATitle_ContributesJustItsUrl()
    {
        var r = await InArticle("<img src=\"/t.png\" alt=\"Titled\" title=\"Hover text\">");

        r.ImageUrls.Should().Equal("https://example.com/t.png");
    }

    #endregion

    #region links

    [Fact]
    public async Task AnchorsWithoutAUsableHref_AreUnwrappedToTheirText()
    {
        var r = await InArticle("<p>See <a>the section</a>, <a name=\"x\">the anchor</a>, <a href=\"\">empty</a>, <a href=\"#\">top</a> " +
                                "and <a href=\"javascript:void(0)\">script link</a> for details in this paragraph of text.</p>");

        r.Text.Should().Contain("See the section, the anchor, empty, top and script link for details");
        r.Text.Should().NotContain("]()").And.NotContain("javascript");
    }

    [Fact]
    public async Task AnchorsWithARealHref_AreStillLinks()
    {
        var r = await InArticle("<p>Read <a href=\"../other.html\">the other page</a> and <a href=\"#part-two\">part two</a> now please.</p>");

        r.Text.Should().Contain("[the other page](https://example.com/docs/other.html)")
            .And.Contain("[part two](https://example.com/docs/guide/page.html#part-two)");
    }

    #endregion

    #region hidden text and form controls

    [Theory]
    [InlineData("<div hidden>Secret hidden text</div>")]
    [InlineData("<div style=\"display:none\">Secret hidden text</div>")]
    [InlineData("<div style=\"color: red; DISPLAY : NONE ;\">Secret hidden text</div>")]
    [InlineData("<p style=\"visibility:hidden\">Secret hidden text</p>")]
    [InlineData("<span hidden>Secret hidden text</span><p>Visible sibling paragraph that stays in the text.</p>")]
    public async Task HiddenElements_AreNotExtracted(string hidden)
    {
        var r = await InArticle(hidden);

        r.Text.Should().Contain("Intro paragraph").And.NotContain("Secret hidden text");
    }

    [Fact]
    public async Task VisibleStyledText_IsKept()
    {
        var r = await InArticle("<p style=\"color:red; font-weight:bold\">Styled but visible paragraph text here.</p>" +
                                "<details><summary>More</summary><p>Folded detail text that is still page content.</p></details>");

        r.Text.Should().Contain("Styled but visible").And.Contain("Folded detail text");
    }

    [Fact]
    public async Task SelectOptions_AreNotGluedIntoTheText()
    {
        var r = await InArticle("<p>Pick a size: <select><option>Small</option><option>Medium</option><option>Large</option></select></p>");

        r.Text.Should().Contain("Pick a size:").And.NotContain("SmallMedium").And.NotContain("Medium");
    }

    #endregion

    #region title, invisible characters, line endings

    [Fact]
    public async Task TitleWhitespace_IsCollapsed()
    {
        var r = await Extract("<html><head><title>\n   Spaced   title\n   here\n</title></head><body><p>Body text that is long enough to be a paragraph.</p></body></html>");

        r.Title.Should().Be("Spaced title here");
    }

    [Fact]
    public async Task HeadingTitle_WhitespaceIsCollapsedToo()
    {
        var r = await Extract("<html><body><h1>  Big\n     heading </h1><p>Body text that is long enough to be a paragraph.</p></body></html>");

        r.Title.Should().Be("Big heading");
    }

    [Fact]
    public async Task ZeroWidthSpaceAndSoftHyphen_AreDropped_ButJoinersAreKept()
    {
        var r = await InArticle("<p>Zero&#8203;width and soft&shy;hyphen and a bom&#65279;mark and word&#8288;joiner, but می&zwnj;خواهم keeps its joiner.</p>");

        r.Text.Should().Contain("Zerowidth and softhyphen and a bommark and wordjoiner");
        r.Text.Should().Contain("می‌خواهم");
    }

    [Fact]
    public async Task Text_UsesLineFeedOnEveryPlatform_AndStillCollapsesBlankRuns()
    {
        var r = await Extract("<html><body><article><h1>Title of the page</h1><p>First paragraph of the page text.</p>" +
                              "<p>Line one<br>Line two</p><div><br><br><br></div><p>Last paragraph of the page text.</p></article></body></html>");

        r.Text.Should().NotContain("\r");
        r.RawMarkdown.Should().NotContain("\r");
        r.Text.Should().NotContain("\n\n\n");
        r.Headings.Should().Equal("# Title of the page");
    }

    #endregion
}
