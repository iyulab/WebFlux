using AwesomeAssertions;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Services.Crawlers;
using Xunit;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// A page the crawler never discovers is a page nobody indexes, silently. Each case is a small hand-written
/// page of the kind real sites serve (minified markup, escaped query strings, a base element, code samples
/// that mention a link); the controls keep what was already right.
/// </summary>
public class LinkDiscoveryTests
{
    private const string Page = "https://example.com/blog/post.html";

    private sealed class Crawler : BaseCrawler
    {
        public Crawler() : base(Substitute.For<IHttpClientService>(), Substitute.For<IEventPublisher>())
        {
        }
    }

    private static IReadOnlyList<string> LinksOf(string html, string baseUrl = Page) => new Crawler().ExtractLinks(html, baseUrl);

    #region links that were missed or wrong

    [Fact]
    public void UnquotedHref_IsFound()
    {
        var links = LinksOf("<p><a href=/about>About</a><a class=x href=page2.html id=y>Two</a><a href=\"/quoted\">Q</a></p>");

        links.Should().Contain("https://example.com/about")
            .And.Contain("https://example.com/blog/page2.html")
            .And.Contain("https://example.com/quoted");
    }

    [Fact]
    public void EscapedAmpersandInTheQuery_IsDecoded()
    {
        var links = LinksOf("<a href=\"/search?a=1&amp;b=2&amp;c=3\">Search</a>");

        links.Should().Equal("https://example.com/search?a=1&b=2&c=3");
    }

    [Fact]
    public void BaseElement_IsHonoured()
    {
        var links = LinksOf("<html><head><base href=\"https://example.com/docs/v2/\"></head><body>" +
                            "<a href=\"intro.html\">Intro</a><a href=\"/root\">Root</a><a href=\"../v1/old.html\">Old</a></body></html>");

        links.Should().Equal("https://example.com/docs/v2/intro.html", "https://example.com/root", "https://example.com/docs/v1/old.html");
    }

    [Fact]
    public void RelativeBaseElement_IsResolvedAgainstThePageUrl()
    {
        var links = LinksOf("<html><head><base href=\"/docs/\"></head><body><a href=\"intro.html\">Intro</a></body></html>");

        links.Should().Equal("https://example.com/docs/intro.html");
    }

    [Fact]
    public void LinksInCommentsAndScripts_AreNotLinks()
    {
        var links = LinksOf("<body><!-- <a href=\"/old\">old</a> --><script>var s = '<a href=\"/in-script\">x</a>';</script>" +
                            "<p>Write <code>&lt;a href=\"/in-code\"&gt;</code> to link.</p><a href=\"/real\">Real</a></body>");

        links.Should().Equal("https://example.com/real");
    }

    [Fact]
    public void AttributesThatMerelyEndInHref_AreNotLinks()
    {
        var links = LinksOf("<div data-href=\"/not-a-link\" data-target=\"x\"></div><svg><use xlink:href=\"#icon\"></use></svg><a href=\"/real\">Real</a>");

        links.Should().Equal("https://example.com/real");
    }

    [Fact]
    public void AreaElements_AreLinks()
    {
        var links = LinksOf("<map name=m><area shape=rect coords=\"0,0,9,9\" href=\"/from-map\"></map>");

        links.Should().Equal("https://example.com/from-map");
    }

    [Fact]
    public void PaginationLinkElements_AreLinks_ButStylesheetsAndIconsAreNot()
    {
        var links = LinksOf("<head><link rel=\"stylesheet\" href=\"/s.css\"><link rel=\"icon\" href=\"/favicon.ico\">" +
                            "<link rel=\"manifest\" href=\"/m.json\"><link rel=\"alternate\" type=\"application/rss+xml\" href=\"/feed\">" +
                            "<link rel=\"next\" href=\"/blog/page/3\"><link rel=\"prev\" href=\"/blog/page/1\"></head>");

        links.Should().Equal("https://example.com/blog/page/3", "https://example.com/blog/page/1");
    }

    [Fact]
    public void WhitespaceAroundTheValue_IsIgnored()
    {
        var links = LinksOf("<a href=\"  /spaced  \">S</a><a href=\"\n/newline\n\">N</a>");

        links.Should().Equal("https://example.com/spaced", "https://example.com/newline");
    }

    #endregion

    #region controls

    [Theory]
    [InlineData("<a href=\"javascript:void(0)\">x</a>")]
    [InlineData("<a href=\"mailto:a@example.com\">x</a>")]
    [InlineData("<a href=\"tel:+8212345\">x</a>")]
    [InlineData("<a href=\"#top\">x</a>")]
    [InlineData("<a href=\"\">x</a>")]
    [InlineData("<a>no href</a>")]
    [InlineData("<a href=\"data:text/html,hi\">x</a>")]
    [InlineData("<a href=\"ftp://example.com/file\">x</a>")]
    public void NonNavigableTargets_AreSkipped(string html)
    {
        LinksOf(html).Should().BeEmpty();
    }

    [Fact]
    public void UppercaseAttributesAndElements_AreFound()
    {
        LinksOf("<A HREF=\"/Upper\">U</A>").Should().Equal("https://example.com/Upper");
    }

    [Fact]
    public void SingleQuotedAndAbsoluteLinks_StillWork()
    {
        LinksOf("<a href='/single'>S</a><a href=\"https://other.org/x\">O</a>")
            .Should().Equal("https://example.com/single", "https://other.org/x");
    }

    [Fact]
    public void DuplicateLinks_AreReportedOncePerOccurrence_NotInvented()
    {
        LinksOf("<a href=\"/a\">1</a><a href=\"/a\">2</a>").Should().Equal("https://example.com/a", "https://example.com/a");
    }

    #endregion
}
