using FluentAssertions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Models;

public class WebDocumentMetadataTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Example",
            Language = "en"
        };

        metadata.Description.Should().BeNull();
        metadata.Keywords.Should().BeEmpty();
        metadata.Author.Should().BeNull();
        metadata.Robots.Should().BeNull();
        metadata.CanonicalUrl.Should().BeNull();
        metadata.OgTitle.Should().BeNull();
        metadata.OgDescription.Should().BeNull();
        metadata.OgImage.Should().BeNull();
        metadata.OgType.Should().BeNull();
        metadata.OgSiteName.Should().BeNull();
        metadata.OgLocale.Should().BeNull();
        metadata.PublishedAt.Should().BeNull();
        metadata.ModifiedAt.Should().BeNull();
        metadata.SchemaOrgType.Should().BeNull();
        metadata.StructuredData.Should().BeEmpty();
        metadata.JsonLdData.Should().BeEmpty();
        metadata.SiteContext.Should().BeNull();
        metadata.TwitterCard.Should().BeNull();
        metadata.FeedUrl.Should().BeNull();
        metadata.Domain.Should().BeEmpty();
        metadata.AdditionalData.Should().BeEmpty();
    }

    [Fact]
    public void GetEffectiveTitle_WithOgTitle_ShouldReturnOgTitle()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Page Title",
            Language = "en",
            OgTitle = "OG Title"
        };

        metadata.GetEffectiveTitle().Should().Be("OG Title");
    }

    [Fact]
    public void GetEffectiveTitle_WithoutOgTitle_ShouldReturnTitle()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Page Title",
            Language = "en"
        };

        metadata.GetEffectiveTitle().Should().Be("Page Title");
    }

    [Fact]
    public void GetEffectiveDescription_WithOgDescription_ShouldReturnOgDescription()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Title",
            Language = "en",
            Description = "SEO Description",
            OgDescription = "OG Description"
        };

        metadata.GetEffectiveDescription().Should().Be("OG Description");
    }

    [Fact]
    public void GetEffectiveDescription_WithoutOgDescription_ShouldReturnDescription()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Title",
            Language = "en",
            Description = "SEO Description"
        };

        metadata.GetEffectiveDescription().Should().Be("SEO Description");
    }

    [Fact]
    public void GetEffectiveDescription_WithNeitherDescription_ShouldReturnNull()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Title",
            Language = "en"
        };

        metadata.GetEffectiveDescription().Should().BeNull();
    }

    [Theory]
    [InlineData("article", "Article")]
    [InlineData("product", "Product")]
    [InlineData("video.movie", "Video")]
    [InlineData("video.episode", "Video")]
    [InlineData("music.song", "Music")]
    [InlineData("music.album", "Music")]
    [InlineData("book", "Book")]
    [InlineData("profile", "Profile")]
    [InlineData("website", "Website")]
    [InlineData("unknown", "General")]
    public void GetCategory_ShouldMapCorrectly(string? ogType, string expected)
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Title",
            Language = "en",
            OgType = ogType
        };

        metadata.GetCategory().Should().Be(expected);
    }

    [Fact]
    public void GetCategory_WithNullOgType_ShouldReturnGeneral()
    {
        var metadata = new WebDocumentMetadata
        {
            Url = "https://example.com",
            Title = "Title",
            Language = "en"
        };

        metadata.GetCategory().Should().Be("General");
    }
}

public class LanguageDetectionMethodEnumTests
{
    [Fact]
    public void ShouldHaveFourValues()
    {
        Enum.GetValues<LanguageDetectionMethod>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(LanguageDetectionMethod.HtmlLangAttribute, 0)]
    [InlineData(LanguageDetectionMethod.HttpHeader, 1)]
    [InlineData(LanguageDetectionMethod.ContentAnalysis, 2)]
    [InlineData(LanguageDetectionMethod.Unknown, 3)]
    public void ShouldHaveExpectedIntValues(LanguageDetectionMethod method, int expected)
    {
        ((int)method).Should().Be(expected);
    }
}

public class SiteContextTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var context = new SiteContext();

        context.Breadcrumbs.Should().BeEmpty();
        context.BreadcrumbItems.Should().BeEmpty();
        context.RelatedPages.Should().BeEmpty();
        context.PreviousPage.Should().BeNull();
        context.NextPage.Should().BeNull();
        context.SitemapPriority.Should().BeNull();
        context.ChangeFrequency.Should().BeNull();
    }

    [Fact]
    public void ShouldInitialize_WithAllFields()
    {
        var context = new SiteContext
        {
            Breadcrumbs = ["Docs", "API", "Search"],
            RelatedPages = ["https://example.com/related"],
            PreviousPage = "https://example.com/prev",
            NextPage = "https://example.com/next",
            SitemapPriority = 0.8,
            ChangeFrequency = "weekly"
        };

        context.Breadcrumbs.Should().HaveCount(3);
        context.RelatedPages.Should().ContainSingle();
        context.SitemapPriority.Should().Be(0.8);
    }
}
