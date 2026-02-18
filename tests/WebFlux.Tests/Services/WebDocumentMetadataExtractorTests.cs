using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Models;
using WebFlux.Services;

namespace WebFlux.Tests.Services;

public class WebDocumentMetadataExtractorTests
{
    private readonly WebDocumentMetadataExtractor _extractor;

    public WebDocumentMetadataExtractorTests()
    {
        var logger = Substitute.For<ILogger<WebDocumentMetadataExtractor>>();
        _extractor = new WebDocumentMetadataExtractor(logger);
    }

    // --- Title ---

    [Fact]
    public async Task ExtractAsync_Title_ExtractsFromTitleTag()
    {
        var html = "<html><head><title>Test Page</title></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Title.Should().Be("Test Page");
    }

    [Fact]
    public async Task ExtractAsync_NoTitle_ReturnsEmpty()
    {
        var html = "<html><head></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Title.Should().BeEmpty();
    }

    // --- SEO Meta ---

    [Fact]
    public async Task ExtractAsync_Description_ExtractsMetaDescription()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta name="description" content="A great page about testing">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Description.Should().Be("A great page about testing");
    }

    [Fact]
    public async Task ExtractAsync_Keywords_SplitsCommaList()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta name="keywords" content="testing, unit test, c#, dotnet">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Keywords.Should().HaveCount(4);
        result.Keywords.Should().Contain("testing");
        result.Keywords.Should().Contain("c#");
    }

    [Fact]
    public async Task ExtractAsync_NoKeywords_ReturnsEmpty()
    {
        var html = "<html><head><title>T</title></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Keywords.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_Author_ExtractsMetaAuthor()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta name="author" content="John Doe">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Author.Should().Be("John Doe");
    }

    [Fact]
    public async Task ExtractAsync_Robots_ExtractsMetaRobots()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta name="robots" content="noindex, nofollow">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Robots.Should().Be("noindex, nofollow");
    }

    [Fact]
    public async Task ExtractAsync_CanonicalUrl_ExtractsLinkCanonical()
    {
        var html = """
            <html><head>
                <title>T</title>
                <link rel="canonical" href="https://example.com/canonical">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com/page");

        result.CanonicalUrl.Should().Be("https://example.com/canonical");
    }

    // --- Open Graph ---

    [Fact]
    public async Task ExtractAsync_OpenGraph_ExtractsAllOgProperties()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta property="og:title" content="OG Title">
                <meta property="og:description" content="OG Desc">
                <meta property="og:image" content="https://example.com/img.jpg">
                <meta property="og:type" content="article">
                <meta property="og:site_name" content="Example Site">
                <meta property="og:locale" content="ko_KR">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.OgTitle.Should().Be("OG Title");
        result.OgDescription.Should().Be("OG Desc");
        result.OgImage.Should().Be("https://example.com/img.jpg");
        result.OgType.Should().Be("article");
        result.OgSiteName.Should().Be("Example Site");
        result.OgLocale.Should().Be("ko_KR");
    }

    // --- DateTime ---

    [Fact]
    public async Task ExtractAsync_PublishedAt_ParsesDateTime()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta property="article:published_time" content="2024-03-15T10:00:00Z">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.PublishedAt.Should().NotBeNull();
        result.PublishedAt!.Value.Year.Should().Be(2024);
        result.PublishedAt.Value.Month.Should().Be(3);
    }

    [Fact]
    public async Task ExtractAsync_InvalidDateTime_ReturnsNull()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta property="article:published_time" content="not-a-date">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.PublishedAt.Should().BeNull();
    }

    // --- Schema.org ---

    [Fact]
    public async Task ExtractAsync_SchemaOrgType_ExtractsFromJsonLd()
    {
        var html = """
            <html><head>
                <title>T</title>
                <script type="application/ld+json">
                {"@type": "Article", "@context": "https://schema.org", "headline": "Test"}
                </script>
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.SchemaOrgType.Should().Be("Article");
    }

    [Fact]
    public async Task ExtractAsync_SchemaOrgType_FallsBackToMicrodata()
    {
        var html = """
            <html><head><title>T</title></head>
            <body>
                <div itemtype="https://schema.org/Product" itemscope>
                    <span itemprop="name">Product</span>
                </div>
            </body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.SchemaOrgType.Should().Be("Product");
    }

    [Fact]
    public async Task ExtractAsync_StructuredData_ExtractsJsonLdProperties()
    {
        var html = """
            <html><head>
                <title>T</title>
                <script type="application/ld+json">
                {"@type": "Article", "headline": "Test Headline", "wordCount": 500}
                </script>
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.StructuredData.Should().ContainKey("@type");
        result.StructuredData.Should().ContainKey("headline");
        result.StructuredData["headline"].Should().Be("Test Headline");
    }

    [Fact]
    public async Task ExtractAsync_InvalidJsonLd_IgnoresGracefully()
    {
        var html = """
            <html><head>
                <title>T</title>
                <script type="application/ld+json">
                {invalid json!!!}
                </script>
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.SchemaOrgType.Should().BeNull();
        result.StructuredData.Should().BeEmpty();
    }

    // --- Language Detection ---

    [Fact]
    public async Task ExtractAsync_Language_DetectsFromHtmlLangAttribute()
    {
        var html = """<html lang="ko-KR"><head><title>T</title></head><body></body></html>""";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Language.Should().Be("ko");
        result.LanguageDetectionMethod.Should().Be(LanguageDetectionMethod.HtmlLangAttribute);
    }

    [Fact]
    public async Task ExtractAsync_Language_DetectsFromHttpHeader()
    {
        var html = "<html><head><title>T</title></head><body></body></html>";
        var headers = new Dictionary<string, string> { ["Content-Language"] = "ja" };

        var result = await _extractor.ExtractAsync(html, "https://example.com", headers);

        result.Language.Should().Be("ja");
        result.LanguageDetectionMethod.Should().Be(LanguageDetectionMethod.HttpHeader);
    }

    [Fact]
    public async Task ExtractAsync_Language_DetectsKoreanFromContent()
    {
        // Korean text with significant Korean character ratio
        var koreanText = string.Concat(Enumerable.Repeat("한글테스트문장입니다", 20));
        var html = $"<html><head><title>T</title></head><body><p>{koreanText}</p></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Language.Should().Be("ko");
        result.LanguageDetectionMethod.Should().Be(LanguageDetectionMethod.ContentAnalysis);
    }

    [Fact]
    public async Task ExtractAsync_Language_HtmlLangPriorityOverHeader()
    {
        var html = """<html lang="en"><head><title>T</title></head><body></body></html>""";
        var headers = new Dictionary<string, string> { ["Content-Language"] = "ko" };

        var result = await _extractor.ExtractAsync(html, "https://example.com", headers);

        result.Language.Should().Be("en");
        result.LanguageDetectionMethod.Should().Be(LanguageDetectionMethod.HtmlLangAttribute);
    }

    [Fact]
    public async Task ExtractAsync_Language_NormalizesLangCode()
    {
        var html = """<html lang="en-US"><head><title>T</title></head><body></body></html>""";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.Language.Should().Be("en");
    }

    // --- Twitter Card ---

    [Fact]
    public async Task ExtractAsync_TwitterCard_ExtractsAllFields()
    {
        var html = """
            <html><head>
                <title>T</title>
                <meta name="twitter:card" content="summary_large_image">
                <meta name="twitter:site" content="@example">
                <meta name="twitter:creator" content="@author">
                <meta name="twitter:title" content="Twitter Title">
                <meta name="twitter:description" content="Twitter Desc">
                <meta name="twitter:image" content="https://example.com/tw.jpg">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.TwitterCard.Should().NotBeNull();
        result.TwitterCard!.Card.Should().Be("summary_large_image");
        result.TwitterCard.Site.Should().Be("@example");
        result.TwitterCard.Creator.Should().Be("@author");
        result.TwitterCard.Title.Should().Be("Twitter Title");
        result.TwitterCard.Description.Should().Be("Twitter Desc");
        result.TwitterCard.Image.Should().Be("https://example.com/tw.jpg");
    }

    [Fact]
    public async Task ExtractAsync_NoTwitterCard_ReturnsNull()
    {
        var html = "<html><head><title>T</title></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.TwitterCard.Should().BeNull();
    }

    // --- Feed URL ---

    [Fact]
    public async Task ExtractAsync_RssFeed_ExtractsUrl()
    {
        var html = """
            <html><head>
                <title>T</title>
                <link type="application/rss+xml" href="https://example.com/feed.xml">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.FeedUrl.Should().Be("https://example.com/feed.xml");
    }

    [Fact]
    public async Task ExtractAsync_AtomFeed_ExtractsUrl()
    {
        var html = """
            <html><head>
                <title>T</title>
                <link type="application/atom+xml" href="https://example.com/atom.xml">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.FeedUrl.Should().Be("https://example.com/atom.xml");
    }

    // --- Domain ---

    [Fact]
    public async Task ExtractAsync_Domain_ExtractsFromUrl()
    {
        var html = "<html><head><title>T</title></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://www.example.com/path");

        result.Domain.Should().Be("www.example.com");
    }

    // --- Breadcrumbs JSON-LD ---

    [Fact]
    public async Task ExtractAsync_Breadcrumbs_ExtractsFromJsonLd()
    {
        var html = """
            <html><head>
                <title>T</title>
                <script type="application/ld+json">
                {
                    "@type": "BreadcrumbList",
                    "itemListElement": [
                        {"@type": "ListItem", "position": 1, "item": {"name": "Home", "@id": "/"}},
                        {"@type": "ListItem", "position": 2, "item": {"name": "Docs", "@id": "/docs"}},
                        {"@type": "ListItem", "position": 3, "item": {"name": "API", "@id": "/docs/api"}}
                    ]
                }
                </script>
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com/docs/api");

        result.SiteContext.Should().NotBeNull();
        result.SiteContext!.Breadcrumbs.Should().HaveCount(3);
        result.SiteContext.Breadcrumbs[0].Should().Be("Home");
        result.SiteContext.Breadcrumbs[1].Should().Be("Docs");
        result.SiteContext.Breadcrumbs[2].Should().Be("API");
    }

    // --- Breadcrumbs HTML ---

    [Fact]
    public async Task ExtractAsync_Breadcrumbs_ExtractsFromNavAriaLabel()
    {
        var html = """
            <html><head><title>T</title></head>
            <body>
                <nav aria-label="breadcrumb">
                    <a href="/">Home</a>
                    <a href="/docs">Docs</a>
                    <span>Current</span>
                </nav>
            </body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com/docs/current");

        result.SiteContext.Should().NotBeNull();
        result.SiteContext!.Breadcrumbs.Should().Contain("Home");
        result.SiteContext.Breadcrumbs.Should().Contain("Docs");
    }

    // --- Nav Links ---

    [Fact]
    public async Task ExtractAsync_NavLinks_ExtractsPrevNext()
    {
        var html = """
            <html><head>
                <title>T</title>
                <link rel="prev" href="/page/1">
                <link rel="next" href="/page/3">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com/page/2");

        result.SiteContext.Should().NotBeNull();
        result.SiteContext!.PreviousPage.Should().Be("/page/1");
        result.SiteContext.NextPage.Should().Be("/page/3");
    }

    // --- Url ---

    [Fact]
    public async Task ExtractAsync_SetsUrl()
    {
        var html = "<html><head><title>T</title></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com/path");

        result.Url.Should().Be("https://example.com/path");
    }

    // --- ExtractBatchAsync ---

    [Fact]
    public async Task ExtractBatchAsync_MultipleDocuments_ReturnsAll()
    {
        var docs = new List<(string html, string url, IReadOnlyDictionary<string, string>? httpHeaders)>
        {
            ("<html><head><title>Page 1</title></head><body></body></html>",
             "https://example.com/1", null),
            ("<html><head><title>Page 2</title></head><body></body></html>",
             "https://example.com/2", null)
        };

        var results = await _extractor.ExtractBatchAsync(docs);

        results.Should().HaveCount(2);
        results[0].Title.Should().Be("Page 1");
        results[1].Title.Should().Be("Page 2");
    }

    // --- Utility methods on model ---

    [Fact]
    public async Task GetEffectiveTitle_PrefersOgTitle()
    {
        var html = """
            <html><head>
                <title>Regular Title</title>
                <meta property="og:title" content="OG Title">
            </head><body></body></html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.GetEffectiveTitle().Should().Be("OG Title");
    }

    [Fact]
    public async Task GetEffectiveTitle_FallsBackToTitle()
    {
        var html = "<html><head><title>Regular Title</title></head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.GetEffectiveTitle().Should().Be("Regular Title");
    }

    [Theory]
    [InlineData("article", "Article")]
    [InlineData("product", "Product")]
    [InlineData("website", "Website")]
    [InlineData("video.movie", "Video")]
    [InlineData("music.song", "Music")]
    [InlineData("book", "Book")]
    [InlineData("profile", "Profile")]
    [InlineData(null, "General")]
    public async Task GetCategory_MapsOgTypeCorrectly(string? ogType, string expectedCategory)
    {
        var ogMeta = ogType != null
            ? $"""<meta property="og:type" content="{ogType}">"""
            : "";
        var html = $"<html><head><title>T</title>{ogMeta}</head><body></body></html>";

        var result = await _extractor.ExtractAsync(html, "https://example.com");

        result.GetCategory().Should().Be(expectedCategory);
    }

    // --- Comprehensive real-world HTML ---

    [Fact]
    public async Task ExtractAsync_CompleteHtml_ExtractsAllMetadata()
    {
        var html = """
            <!DOCTYPE html>
            <html lang="en-US">
            <head>
                <title>Complete Example - My Site</title>
                <meta name="description" content="A complete example page">
                <meta name="keywords" content="example, test, complete">
                <meta name="author" content="Test Author">
                <meta name="robots" content="index, follow">
                <link rel="canonical" href="https://mysite.com/complete">
                <meta property="og:title" content="Complete OG Title">
                <meta property="og:description" content="OG Description">
                <meta property="og:image" content="https://mysite.com/img.png">
                <meta property="og:type" content="article">
                <meta property="og:site_name" content="My Site">
                <meta property="article:published_time" content="2024-06-01T12:00:00Z">
                <meta name="twitter:card" content="summary">
                <meta name="twitter:site" content="@mysite">
                <link type="application/rss+xml" href="https://mysite.com/rss">
                <link rel="prev" href="/page/1">
                <link rel="next" href="/page/3">
                <script type="application/ld+json">
                {"@context": "https://schema.org", "@type": "Article", "headline": "Complete"}
                </script>
            </head>
            <body>
                <p>Content here</p>
            </body>
            </html>
            """;

        var result = await _extractor.ExtractAsync(html, "https://mysite.com/complete");

        // Basic
        result.Title.Should().Be("Complete Example - My Site");
        result.Description.Should().Be("A complete example page");
        result.Keywords.Should().HaveCount(3);
        result.Author.Should().Be("Test Author");
        result.Robots.Should().Be("index, follow");
        result.CanonicalUrl.Should().Be("https://mysite.com/complete");

        // OG
        result.OgTitle.Should().Be("Complete OG Title");
        result.OgType.Should().Be("article");

        // Time
        result.PublishedAt.Should().NotBeNull();

        // Schema
        result.SchemaOrgType.Should().Be("Article");

        // Language
        result.Language.Should().Be("en");
        result.LanguageDetectionMethod.Should().Be(LanguageDetectionMethod.HtmlLangAttribute);

        // Twitter
        result.TwitterCard.Should().NotBeNull();
        result.TwitterCard!.Card.Should().Be("summary");

        // Feed
        result.FeedUrl.Should().Be("https://mysite.com/rss");

        // Domain
        result.Domain.Should().Be("mysite.com");

        // Nav
        result.SiteContext!.PreviousPage.Should().Be("/page/1");
        result.SiteContext.NextPage.Should().Be("/page/3");
    }
}
