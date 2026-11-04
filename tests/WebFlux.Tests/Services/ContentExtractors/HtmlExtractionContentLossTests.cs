using AwesomeAssertions;
using WebFlux.Services.ContentExtractors;
using Xunit;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// A page's own text must reach <c>Text</c>. Each case is a small hand-written page of the kind real sites
/// serve; the first group lost content (selector heuristics removed the content's own container or part of the
/// article), the second group is the control: boilerplate the cleaner is for is still removed.
/// </summary>
public class HtmlExtractionContentLossTests
{
    private const string Url = "https://example.com/docs/guide/page.html";
    private const string Intro = "<p>Intro paragraph that is long enough to be kept by any density filter.</p>";

    private static async Task<string> TextOf(string html)
    {
        var extractor = new HtmlToMarkdownExtractor(null);
        var result = await extractor.ExtractFromHtmlAsync(html, Url, cancellationToken: TestContext.Current.CancellationToken);
        return result.Text;
    }

    #region content that used to be removed

    [Fact]
    public async Task HeaderInsideArticle_KeepsTheArticleTitleAndByline()
    {
        var text = await TextOf("<html><body><article><header><h1>Guide title inside header</h1><p>By Jane, 2026</p></header>" +
                                "<p>Body paragraph that is long enough to be kept by the density filter.</p></article></body></html>");

        text.Should().Contain("Guide title inside header").And.Contain("By Jane, 2026");
    }

    [Fact]
    public async Task HeaderInsideMain_KeepsThePageTitleBlock()
    {
        var text = await TextOf("<html><body><main><header><h1>Reference page title</h1></header>" +
                                "<p>Body paragraph that is long enough to be kept by the density filter.</p></main></body></html>");

        text.Should().Contain("Reference page title");
    }

    [Fact]
    public async Task FooterInsideArticle_KeepsItsText()
    {
        var text = await TextOf("<html><body><article>" + Intro +
                                "<footer><p>Last reviewed by the documentation team in March.</p></footer></article></body></html>");

        text.Should().Contain("Last reviewed by the documentation team");
    }

    [Fact]
    public async Task AsideInsideArticle_KeepsTheCallout()
    {
        var text = await TextOf("<html><body><article>" + Intro +
                                "<aside class=\"note\"><p>Warning: back up your data before running the migration.</p></aside>" +
                                "<p>Next paragraph that is long enough to be kept by the density filter.</p></article></body></html>");

        text.Should().Contain("Warning: back up your data before running the migration.");
    }

    [Fact]
    public async Task MainWhoseIdMentionsCookie_IsNotRemoved()
    {
        var text = await TextOf("<html><body><main id=\"cookie-faq\"><h1>Cookie FAQ</h1>" +
                                "<p>What is a cookie? A small text file a site stores in your browser to remember you.</p></main></body></html>");

        text.Should().Contain("What is a cookie?");
    }

    [Theory]
    [InlineData("<body class=\"modal-open\">")]
    [InlineData("<body id=\"cookie-policy\">")]
    [InlineData("<body class=\"popup-active ads\">")]
    public async Task BodyWhoseClassOrIdLooksLikeBoilerplate_KeepsThePage(string bodyOpen)
    {
        var text = await TextOf("<html>" + bodyOpen + "<article>" + Intro + "</article></body></html>");

        text.Should().Contain("Intro paragraph");
    }

    [Fact]
    public async Task WrapperContainingTheArticle_IsNotRemoved()
    {
        var text = await TextOf("<html><body><div class=\"page modal-layout\"><article>" + Intro + "</article></div></body></html>");

        text.Should().Contain("Intro paragraph");
    }

    [Fact]
    public async Task WrapperHoldingMostOfThePageText_IsNotRemovedEvenWithoutAnArticle()
    {
        var text = await TextOf("<html><body><div class=\"popup-layout\"><h2>Install</h2>" +
                                "<p>Download the archive, unpack it and run the setup program from the folder you chose. " +
                                "The installer asks for a target directory and offers to create a desktop shortcut; accept the defaults " +
                                "unless your administrator has told you otherwise, then restart the machine when it finishes.</p></div></body></html>");

        text.Should().Contain("Download the archive");
    }

    [Fact]
    public async Task SectionIdAdHoc_InsideAnArticle_IsKept()
    {
        var text = await TextOf("<html><body><article>" + Intro + "<section id=\"ad-hoc-queries\"><h2>Ad hoc queries</h2>" +
                                "<p>You can run ad hoc queries against the live index without a saved view.</p></section></article></body></html>");

        text.Should().Contain("Ad hoc queries").And.Contain("without a saved view");
    }

    [Fact]
    public async Task SectionIdAdHoc_WithoutAnArticle_IsKept()
    {
        var text = await TextOf("<html><body><p>Short lead.</p><section id=\"ad-hoc-queries\"><h2>Ad hoc queries</h2>" +
                                "<p>You can run ad hoc queries against the live index without a saved view.</p></section>" +
                                "<p>Another paragraph of the page so the section is not most of it at all, really.</p></body></html>");

        text.Should().Contain("without a saved view");
    }

    [Fact]
    public async Task ClassModalityTable_IsNotAModal()
    {
        var text = await TextOf("<html><body><article>" + Intro + "<div class=\"modality-table\">" +
                                "<p>Modality notes: images, audio and text inputs are all accepted by this endpoint.</p></div></article></body></html>");

        text.Should().Contain("Modality notes");
    }

    [Fact]
    public async Task ClassMenu_InsideAnArticle_IsContent()
    {
        var text = await TextOf("<html><body><article>" + Intro + "<div class=\"menu\"><h2>Menu of the day</h2>" +
                                "<p>Soup of the day is tomato and basil, served with fresh bread and butter.</p></div></article></body></html>");

        text.Should().Contain("Menu of the day").And.Contain("Soup of the day");
    }

    [Fact]
    public async Task LinkListInsideAnArticle_IsKept()
    {
        var text = await TextOf("<html><body><article><h1>API index</h1><p>All endpoints are listed below by area.</p>" +
                                "<ul><li><a href=\"/a\">Create user</a></li><li><a href=\"/b\">Delete user</a></li>" +
                                "<li><a href=\"/c\">List users</a></li></ul></article></body></html>");

        text.Should().Contain("Create user").And.Contain("Delete user").And.Contain("List users");
    }

    [Fact]
    public async Task LinkListInsideMain_IsKept_WhateverElseIsOnThePage()
    {
        var withParagraph = await TextOf("<html><body><main><h1>Index</h1><p>Guides by topic, newest first, are listed below.</p>" +
                                         "<ul><li><a href=\"/a\">Alpha guide</a></li><li><a href=\"/b\">Beta guide</a></li></ul></main></body></html>");
        var alone = await TextOf("<html><body><main><h1>Index</h1>" +
                                 "<ul><li><a href=\"/a\">Alpha guide</a></li><li><a href=\"/b\">Beta guide</a></li></ul></main></body></html>");

        withParagraph.Should().Contain("Alpha guide").And.Contain("Beta guide");
        alone.Should().Contain("Alpha guide").And.Contain("Beta guide");
    }

    #endregion

    #region controls: boilerplate that is still removed

    [Fact]
    public async Task PageLevelHeaderAndFooter_AreStillRemoved()
    {
        var text = await TextOf("<html><body><header><h1>Site Name</h1><p>Tagline of the whole site</p></header>" +
                                "<article>" + Intro + "</article><footer><p>Copyright 2026 Example Corp</p></footer></body></html>");

        text.Should().Contain("Intro paragraph");
        text.Should().NotContain("Site Name").And.NotContain("Tagline of the whole site").And.NotContain("Copyright 2026");
    }

    [Fact]
    public async Task AsideBesideTheArticle_IsStillRemoved()
    {
        var text = await TextOf("<html><body><main><article>" + Intro + "</article>" +
                                "<aside><h3>Popular posts</h3><p>Ten things you must know about databases.</p></aside></main></body></html>");

        text.Should().Contain("Intro paragraph");
        text.Should().NotContain("Popular posts");
    }

    [Fact]
    public async Task NavInsideAnArticle_IsStillRemoved()
    {
        var text = await TextOf("<html><body><article>" + Intro +
                                "<nav><a href=\"#a\">Jump to part one</a><a href=\"#b\">Jump to part two</a></nav></article></body></html>");

        text.Should().Contain("Intro paragraph");
        text.Should().NotContain("Jump to part one");
    }

    [Theory]
    [InlineData("<div id=\"cookie-banner\">We use cookies to improve your experience on this site.</div>")]
    [InlineData("<div class=\"cookie-consent\">We use cookies to improve your experience on this site.</div>")]
    [InlineData("<div class=\"modal fade\"><p>Sign up for the newsletter today and get a discount.</p></div>")]
    [InlineData("<div class=\"popup-overlay\"><p>Sign up for the newsletter today and get a discount.</p></div>")]
    [InlineData("<div id=\"ad-slot-1\">Buy our product now and save a lot of money today.</div>")]
    [InlineData("<div id=\"ad-12345\">Buy our product now and save a lot of money today.</div>")]
    [InlineData("<div class=\"menu\"><a href=\"/\">Home</a><a href=\"/about\">About us</a><a href=\"/contact\">Contact page</a></div>")]
    [InlineData("<div class=\"widget\"><p>Follow us on every social network you know of today.</p></div>")]
    public async Task BoilerplateOutsideTheArticle_IsStillRemoved(string boilerplate)
    {
        var text = await TextOf("<html><body>" + boilerplate + "<article>" + Intro + "</article></body></html>");

        text.Should().Contain("Intro paragraph");
        text.Should().NotContain("cookies").And.NotContain("newsletter").And.NotContain("Buy our product")
            .And.NotContain("About us").And.NotContain("Follow us");
    }

    [Fact]
    public async Task LinkOnlyListOutsideTheArticle_IsStillRemoved()
    {
        var text = await TextOf("<html><body><div><ul><li><a href=\"/x\">Pricing</a></li><li><a href=\"/y\">Careers</a></li>" +
                                "<li><a href=\"/z\">Press kit</a></li></ul></div><article>" + Intro + "</article></body></html>");

        text.Should().Contain("Intro paragraph");
        text.Should().NotContain("Careers");
    }

    #endregion
}
