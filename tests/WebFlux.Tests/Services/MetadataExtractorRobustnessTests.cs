using AwesomeAssertions;
using WebFlux.Services;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// Ordinary pages carry their language on <c>&lt;html lang&gt;</c>, escape entities in titles and descriptions,
/// and publish JSON-LD as an array or an <c>@graph</c> with array-valued <c>@type</c>. Metadata extraction must
/// read them and must never throw because of what a page contains.
/// </summary>
public class MetadataExtractorRobustnessTests
{
    private const string Url = "https://example.com/blog/post.html";
    private readonly MetadataExtractor _extractor = new();

    private async Task<WebFlux.Core.Models.WebMetadata> ExtractPage(string html)
        => await _extractor.ExtractMetadataAsync(html, Url, TestContext.Current.CancellationToken);

    #region language, entities, title

    [Fact]
    public async Task HtmlLangAttribute_IsTheLanguage()
    {
        var m = await ExtractPage("<html lang=\"ko\"><head><title>제목</title></head><body><p>본문</p></body></html>");

        m.Basic.Language.Should().Be("ko");
    }

    [Fact]
    public async Task XmlLang_IsUsedWhenThereIsNoLang_AndLangWinsOtherwise()
    {
        var onlyXml = await ExtractPage("<html xml:lang=\"fr\"><head><title>T</title></head><body></body></html>");
        var both = await ExtractPage("<html xml:lang=\"fr\" lang=\"fr-CA\"><head><title>T</title></head><body></body></html>");

        onlyXml.Basic.Language.Should().Be("fr");
        both.Basic.Language.Should().Be("fr-CA");
    }

    [Fact]
    public async Task HtmlLang_WinsOverMetaLanguage_AndMetaLanguageStillWorksAlone()
    {
        var withBoth = await ExtractPage("<html lang=\"ja\"><head><meta name=\"language\" content=\"de\"><title>T</title></head><body></body></html>");
        var metaOnly = await ExtractPage("<html><head><meta name=\"language\" content=\"de\"><title>T</title></head><body></body></html>");

        withBoth.Basic.Language.Should().Be("ja");
        metaOnly.Basic.Language.Should().Be("de");
    }

    [Fact]
    public async Task PageWithoutAnyLanguageSignal_HasNoLanguage()
    {
        var m = await ExtractPage("<html><head><title>T</title></head><body></body></html>");

        m.Basic.Language.Should().BeNull();
    }

    [Fact]
    public async Task EntitiesInTitleDescriptionAndKeywords_AreDecoded()
    {
        var m = await ExtractPage("<html><head><title>Tom &amp; Jerry &#44032; &quot;q&quot;</title>" +
                                  "<meta name=\"description\" content=\"A &amp; B &lt;c&gt;\">" +
                                  "<meta name=\"keywords\" content=\"a, b ,, c &amp; d\"></head><body></body></html>");

        m.Basic.Title.Should().Be("Tom & Jerry 가 \"q\"");
        m.Basic.Description.Should().Be("A & B <c>");
        m.Basic.Keywords.Should().Equal("a", "b", "c & d");
    }

    [Fact]
    public async Task TitleWhitespace_IsCollapsed()
    {
        var m = await ExtractPage("<html><head><title>\n  Spaced   title\n</title></head><body></body></html>");

        m.Basic.Title.Should().Be("Spaced title");
    }

    [Fact]
    public async Task OpenGraphWrittenWithNameInsteadOfProperty_IsStillRead()
    {
        var m = await ExtractPage("<html><head><meta name=\"og:title\" content=\"OG by name\"><title>T</title></head><body></body></html>");

        m.OpenGraph.Title.Should().Be("OG by name");
    }

    [Fact]
    public async Task OpenGraphWithPropertyAndName_PrefersTheProperty()
    {
        var m = await ExtractPage("<html><head><meta name=\"og:title\" content=\"by name\"><meta property=\"og:title\" content=\"by property\"><title>T</title></head><body></body></html>");

        m.OpenGraph.Title.Should().Be("by property");
    }

    #endregion

    #region canonical

    [Theory]
    [InlineData("<link rel=\"canonical\" href=\"https://example.com/c\">", "https://example.com/c")]
    [InlineData("<link rel=\"Canonical\" href=\"https://example.com/c\">", "https://example.com/c")]
    [InlineData("<link href=\"/blog/post\" rel=\"canonical\">", "https://example.com/blog/post")]
    [InlineData("<link rel=\"canonical\" href=\"post-2.html\">", "https://example.com/blog/post-2.html")]
    public async Task CanonicalLink_IsFoundAndAbsolute(string link, string expected)
    {
        var m = await ExtractPage($"<html><head>{link}<title>T</title></head><body></body></html>");

        m.Basic.CanonicalUrl.Should().Be(expected);
    }

    #endregion

    #region JSON-LD shapes

    private static string JsonLd(string json) =>
        $"<html><head><title>T</title><script type=\"application/ld+json\">{json}</script></head><body></body></html>";

    [Fact]
    public async Task JsonLdArray_DoesNotThrow_AndIsRead()
    {
        var m = await ExtractPage(JsonLd("[{\"@type\":\"Article\",\"headline\":\"Array head\"}]"));

        m.SchemaOrg.MainEntityType.Should().Be("Article");
        m.SchemaOrg.Article!.Headline.Should().Be("Array head");
    }

    [Fact]
    public async Task JsonLdGraph_YieldsTheTypedEntities()
    {
        var m = await ExtractPage(JsonLd("{\"@context\":\"https://schema.org\",\"@graph\":[" +
                                         "{\"@type\":\"WebSite\",\"name\":\"Site\",\"url\":\"https://example.com\"}," +
                                         "{\"@type\":\"Organization\",\"name\":\"Example Org\"}," +
                                         "{\"@type\":\"Article\",\"headline\":\"Graph head\"}]}"));

        m.SchemaOrg.MainEntityType.Should().Be("Article");
        m.SchemaOrg.Article!.Headline.Should().Be("Graph head");
        m.SchemaOrg.WebSite!.Name.Should().Be("Site");
        m.SchemaOrg.Organization!.Name.Should().Be("Example Org");
    }

    [Fact]
    public async Task JsonLdTypeArray_DoesNotThrow_AndUsesTheKnownType()
    {
        var m = await ExtractPage(JsonLd("{\"@type\":[\"Thing\",\"NewsArticle\"],\"headline\":\"News head\"}"));

        m.SchemaOrg.Article!.Headline.Should().Be("News head");
        m.SchemaOrg.MainEntityType.Should().Be("NewsArticle");
    }

    [Fact]
    public async Task JsonLdWithUnexpectedValueShapes_DoesNotThrow_AndOtherEntitiesSurvive()
    {
        var m = await ExtractPage(JsonLd("[{\"@type\":\"Organization\",\"name\":[\"not\",\"a string\"],\"logo\":{\"url\":\"x\"}}," +
                                         "{\"@type\":\"Article\",\"headline\":\"Survivor\"}]"));

        m.SchemaOrg.Article!.Headline.Should().Be("Survivor");
    }

    [Fact]
    public async Task JsonLdBlocks_FirstEntityOfATypeWins_AndTheArticleIsTheMainEntity()
    {
        var html = "<html><head><title>T</title>" +
                   "<script type=\"application/ld+json\">{\"@type\":\"Organization\",\"name\":\"First org\"}</script>" +
                   "<script type=\"application/ld+json\">{\"@type\":\"Organization\",\"name\":\"Second org\"}</script>" +
                   "<script type=\"application/ld+json\">{\"@type\":\"BlogPosting\",\"headline\":\"Post\"}</script></head><body></body></html>";

        var m = await ExtractPage(html);

        m.SchemaOrg.Organization!.Name.Should().Be("First org");
        m.SchemaOrg.MainEntityType.Should().Be("BlogPosting");
    }

    [Fact]
    public async Task InvalidJsonLd_IsSkipped_AndTheRawTextIsKept()
    {
        var m = await ExtractPage(JsonLd("{ not json "));

        m.SchemaOrg.MainEntityType.Should().BeNull();
        m.SchemaOrg.RawJsonLd.Should().ContainSingle();
    }

    [Fact]
    public async Task SingleObjectJsonLd_StillWorksAsBefore()
    {
        var m = await ExtractPage(JsonLd("{\"@context\":\"https://schema.org\",\"@type\":\"Article\",\"headline\":\"JSON-LD head\"," +
                                         "\"author\":{\"@type\":\"Person\",\"name\":\"Jane\"},\"datePublished\":\"2026-01-02T03:04:05Z\"}"));

        m.SchemaOrg.MainEntityType.Should().Be("Article");
        m.SchemaOrg.Article!.Headline.Should().Be("JSON-LD head");
        m.SchemaOrg.Article.Author.Should().Be("Jane");
    }

    #endregion
}
