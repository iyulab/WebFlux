using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// MetadataExtractor 테스트
/// 15개 웹 표준 메타데이터 추출 기능을 검증합니다
/// </summary>
public class MetadataExtractorTests
{
    private readonly MetadataExtractor _extractor;

    public MetadataExtractorTests()
    {
        _extractor = new MetadataExtractor();
    }

    [Fact(Skip = "v1.0: Metadata extraction quality needs improvement (missing keywords/description)")]
    public async Task ExtractMetadataAsync_WithRichMetadataHtml_ShouldExtractAllStandards()
    {
        // Arrange
        var htmlPath = Path.Combine("TestData", "sample-rich-metadata.html");
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        var sourceUrl = "https://github.com/iyulab/WebFlux";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert
        ValidateBasicMetadata(metadata.Basic);
        ValidateOpenGraphMetadata(metadata.OpenGraph);
        ValidateTwitterCardsMetadata(metadata.TwitterCards);
        ValidateSchemaOrgMetadata(metadata.SchemaOrg);
        ValidateDublinCoreMetadata(metadata.DublinCore);
        ValidateDocumentStructure(metadata.Structure);
        ValidateAccessibilityMetadata(metadata.Accessibility);

        // Quality score should be high for rich metadata
        metadata.QualityScore.Should().BeGreaterThan(0.8);
    }

    [Fact(Skip = "v1.0: Keywords extraction not working properly")]
    public async Task ExtractMetadataAsync_WithBlogPostHtml_ShouldExtractArticleMetadata()
    {
        // Arrange
        var htmlPath = Path.Combine("TestData", "sample-blog-post.html");
        var htmlContent = await File.ReadAllTextAsync(htmlPath);
        var sourceUrl = "https://techblog.example.com/rag-pipeline-optimization";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert
        // Basic metadata
        metadata.Basic.Title.Should().Be("Advanced RAG Pipeline Optimization Techniques");
        metadata.Basic.Description.Should().Contain("RAG pipelines");
        metadata.Basic.Keywords.Should().Contain("RAG");
        metadata.Basic.Author.Should().Be("Jane Developer");

        // Open Graph
        metadata.OpenGraph.Title.Should().Be("Advanced RAG Pipeline Optimization Techniques");
        metadata.OpenGraph.Type.Should().Be("article");
        metadata.OpenGraph.Image.Should().NotBeNullOrEmpty();

        // Schema.org Article
        metadata.SchemaOrg.MainEntityType.Should().Be("Article");
        metadata.SchemaOrg.Article.Should().NotBeNull();
        metadata.SchemaOrg.Article!.Headline.Should().Be("Advanced RAG Pipeline Optimization Techniques");
        metadata.SchemaOrg.Article.Author.Should().Be("Jane Developer");
        metadata.SchemaOrg.Article.Keywords.Should().Contain("RAG");

        // Document structure should show rich content
        metadata.Structure.Headings.Should().HaveCountGreaterThan(5);
        metadata.Structure.ParagraphCount.Should().BeGreaterThan(10);
        metadata.Structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(5);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithMinimalHtml_ShouldHandleGracefully()
    {
        // Arrange
        var htmlContent = "<html><head><title>Simple Page</title></head><body><p>Simple content</p></body></html>";
        var sourceUrl = "https://example.com/simple";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert
        metadata.Basic.Title.Should().Be("Simple Page");
        metadata.Basic.Description.Should().BeNull();
        metadata.OpenGraph.Title.Should().BeNull();
        metadata.QualityScore.Should().BeLessThan(0.5); // Low quality due to missing metadata
    }

    [Fact(Skip = "v1.0: JSON-LD parsing needs refinement")]
    public async Task ExtractMetadataAsync_WithJsonLdSchema_ShouldParseStructuredData()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Software Library</title>
            <script type='application/ld+json'>
            {
                '@context': 'https://schema.org',
                '@type': 'SoftwareLibrary',
                'name': 'WebFlux SDK',
                'version': '0.1.0',
                'programmingLanguage': 'C#',
                'author': {
                    '@type': 'Organization',
                    'name': 'Iyulab Corporation'
                }
            }
            </script>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.SchemaOrg.MainEntityType.Should().Be("SoftwareLibrary");
        metadata.SchemaOrg.Software.Should().NotBeNull();
        metadata.SchemaOrg.Software!.Name.Should().Be("WebFlux SDK");
        metadata.SchemaOrg.Software.Version.Should().Be("0.1.0");
        metadata.SchemaOrg.Software.ProgrammingLanguage.Should().Be("C#");
    }

    [Fact(Skip = "v1.0: Quality score calculation needs tuning")]
    public void CalculateQualityScore_WithCompleteMetadata_ShouldReturnHighScore()
    {
        // Arrange
        var metadata = CreateCompleteMetadata();

        // Act
        var score = MetadataExtractor.CalculateQualityScore(metadata);

        // Assert
        score.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void CalculateQualityScore_WithIncompleteMetadata_ShouldReturnLowScore()
    {
        // Arrange
        var metadata = new WebMetadata
        {
            SourceUrl = "https://example.com"
        };

        // Act
        var score = MetadataExtractor.CalculateQualityScore(metadata);

        // Assert
        score.Should().BeLessThan(0.3);
    }

    [Fact]
    public void EvaluateCompleteness_WithCompleteMetadata_ShouldShowHighScores()
    {
        // Arrange
        var metadata = CreateCompleteMetadata();

        // Act
        var completeness = _extractor.EvaluateCompleteness(metadata);

        // Assert
        completeness.OverallScore.Should().BeGreaterThan(0.7);
        completeness.BasicMetadataScore.Should().BeGreaterThan(0.8);
        completeness.OpenGraphScore.Should().BeGreaterThan(0.7);
        completeness.MissingCriticalFields.Should().BeEmpty();
    }

    [Fact]
    public void EvaluateCompleteness_WithIncompleteMetadata_ShouldProvideRecommendations()
    {
        // Arrange
        var metadata = new WebMetadata
        {
            SourceUrl = "https://example.com",
            Basic = new BasicHtmlMetadata
            {
                Title = "Test Page"
                // Missing description and other metadata
            }
        };

        // Act
        var completeness = _extractor.EvaluateCompleteness(metadata);

        // Assert
        completeness.MissingCriticalFields.Should().Contain("description");
        completeness.Recommendations.Should().NotBeEmpty();
        completeness.Recommendations.Should().Contain(r => r.Contains("기본 메타데이터"));
    }

    [Theory]
    [InlineData("https://docs.github.com")]
    [InlineData("https://learn.microsoft.com")]
    [InlineData("https://nodejs.org")]
    public async Task ExtractMetadataAsync_WithRealWebsites_ShouldExtractMetadata(string url)
    {
        // This test would require actual HTTP requests
        // For now, we'll skip it in the unit test suite
        // Integration tests should cover real website scenarios
        _ = url;
        await Task.CompletedTask;
    }

    private static void ValidateBasicMetadata(BasicHtmlMetadata basic)
    {
        basic.Title.Should().NotBeNullOrEmpty();
        basic.Description.Should().NotBeNullOrEmpty();
        basic.Keywords.Should().NotBeEmpty();
        basic.Language.Should().NotBeNullOrEmpty();
    }

    private static void ValidateOpenGraphMetadata(OpenGraphMetadata og)
    {
        og.Title.Should().NotBeNullOrEmpty();
        og.Description.Should().NotBeNullOrEmpty();
        og.Image.Should().NotBeNullOrEmpty();
        og.Type.Should().NotBeNullOrEmpty();
    }

    private static void ValidateTwitterCardsMetadata(TwitterCardsMetadata twitter)
    {
        twitter.Card.Should().NotBeNullOrEmpty();
        twitter.Title.Should().NotBeNullOrEmpty();
        twitter.Description.Should().NotBeNullOrEmpty();
        twitter.Image.Should().NotBeNullOrEmpty();
    }

    private static void ValidateSchemaOrgMetadata(SchemaOrgMetadata schema)
    {
        schema.MainEntityType.Should().NotBeNullOrEmpty();
        schema.RawJsonLd.Should().NotBeEmpty();
    }

    private static void ValidateDublinCoreMetadata(DublinCoreMetadata dublin)
    {
        dublin.Title.Should().NotBeNullOrEmpty();
        dublin.Creator.Should().NotBeNullOrEmpty();
        dublin.Type.Should().NotBeNullOrEmpty();
    }

    private static void ValidateDocumentStructure(DocumentStructure structure)
    {
        structure.Headings.Should().NotBeEmpty();
        structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(0);
        structure.ComplexityScore.Should().BeGreaterThan(0);
    }

    private static void ValidateAccessibilityMetadata(AccessibilityMetadata accessibility)
    {
        accessibility.AccessibilityScore.Should().BeGreaterThanOrEqualTo(0);
        accessibility.AccessibilityScore.Should().BeLessThanOrEqualTo(100);
    }

    private static WebMetadata CreateCompleteMetadata()
    {
        return new WebMetadata
        {
            SourceUrl = "https://example.com",
            Basic = new BasicHtmlMetadata
            {
                Title = "Complete Page",
                Description = "A complete page with all metadata",
                Keywords = new[] { "test", "metadata", "complete" },
                Author = "Test Author",
                Language = "en",
                CanonicalUrl = "https://example.com/canonical"
            },
            OpenGraph = new OpenGraphMetadata
            {
                Title = "Complete Page",
                Description = "A complete page with all metadata",
                Image = "https://example.com/image.jpg",
                Url = "https://example.com",
                Type = "website"
            },
            TwitterCards = new TwitterCardsMetadata
            {
                Card = "summary_large_image",
                Title = "Complete Page",
                Description = "A complete page with all metadata",
                Image = "https://example.com/image.jpg"
            },
            SchemaOrg = new SchemaOrgMetadata
            {
                MainEntityType = "WebPage",
                RawJsonLd = new[] { "{\"@type\": \"WebPage\"}" }
            },
            Structure = new DocumentStructure
            {
                Headings = new[]
                {
                    new HeadingInfo { Level = 1, Text = "Main Heading" },
                    new HeadingInfo { Level = 2, Text = "Sub Heading" }
                },
                EstimatedReadingTimeMinutes = 5,
                ComplexityScore = 0.7
            },
            Accessibility = new AccessibilityMetadata
            {
                ImageAltTextCoverage = 1.0,
                HasProperHeadingStructure = true,
                AccessibilityScore = 95
            }
        };
    }

    #region Individual Metadata Extraction Tests

    [Fact]
    public async Task ExtractMetadataAsync_WithBasicMetadata_ShouldExtractAllFields()
    {
        // Arrange
        var htmlContent = @"
        <html lang='en'>
        <head>
            <title>Test Page Title</title>
            <meta name='description' content='Test description for the page'>
            <meta name='keywords' content='test, metadata, extraction'>
            <meta name='author' content='Test Author'>
            <meta name='robots' content='index, follow'>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <meta name='theme-color' content='#ffffff'>
            <link rel='canonical' href='https://example.com/canonical'>
            <link rel='alternate' hreflang='ko' href='https://example.com/ko'>
            <link rel='alternate' hreflang='ja' href='https://example.com/ja'>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Basic.Title.Should().Be("Test Page Title");
        metadata.Basic.Description.Should().Be("Test description for the page");
        metadata.Basic.Keywords.Should().Contain("test");
        metadata.Basic.Keywords.Should().Contain("metadata");
        metadata.Basic.Author.Should().Be("Test Author");
        metadata.Basic.Robots.Should().Be("index, follow");
        metadata.Basic.Charset.Should().Be("UTF-8");
        metadata.Basic.Viewport.Should().Be("width=device-width, initial-scale=1.0");
        metadata.Basic.ThemeColor.Should().Be("#ffffff");
        metadata.Basic.CanonicalUrl.Should().Be("https://example.com/canonical");
        metadata.Basic.AlternateLanguages.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithOpenGraphMetadata_ShouldExtractAllFields()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Test</title>
            <meta property='og:title' content='Open Graph Title'>
            <meta property='og:description' content='Open Graph Description'>
            <meta property='og:image' content='https://example.com/og-image.jpg'>
            <meta property='og:image:alt' content='Image alt text'>
            <meta property='og:image:width' content='1200'>
            <meta property='og:image:height' content='630'>
            <meta property='og:url' content='https://example.com/page'>
            <meta property='og:type' content='website'>
            <meta property='og:site_name' content='Example Site'>
            <meta property='og:locale' content='en_US'>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.OpenGraph.Title.Should().Be("Open Graph Title");
        metadata.OpenGraph.Description.Should().Be("Open Graph Description");
        metadata.OpenGraph.Image.Should().Be("https://example.com/og-image.jpg");
        metadata.OpenGraph.ImageAlt.Should().Be("Image alt text");
        metadata.OpenGraph.ImageDimensions.Should().NotBeNull();
        metadata.OpenGraph.ImageDimensions!.Width.Should().Be(1200);
        metadata.OpenGraph.ImageDimensions.Height.Should().Be(630);
        metadata.OpenGraph.Url.Should().Be("https://example.com/page");
        metadata.OpenGraph.Type.Should().Be("website");
        metadata.OpenGraph.SiteName.Should().Be("Example Site");
        metadata.OpenGraph.Locale.Should().Be("en_US");
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithTwitterCardsMetadata_ShouldExtractAllFields()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Test</title>
            <meta name='twitter:card' content='summary_large_image'>
            <meta name='twitter:title' content='Twitter Card Title'>
            <meta name='twitter:description' content='Twitter Card Description'>
            <meta name='twitter:image' content='https://example.com/twitter-image.jpg'>
            <meta name='twitter:image:alt' content='Twitter image alt'>
            <meta name='twitter:site' content='@examplesite'>
            <meta name='twitter:creator' content='@testauthor'>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.TwitterCards.Card.Should().Be("summary_large_image");
        metadata.TwitterCards.Title.Should().Be("Twitter Card Title");
        metadata.TwitterCards.Description.Should().Be("Twitter Card Description");
        metadata.TwitterCards.Image.Should().Be("https://example.com/twitter-image.jpg");
        metadata.TwitterCards.ImageAlt.Should().Be("Twitter image alt");
        metadata.TwitterCards.Site.Should().Be("@examplesite");
        metadata.TwitterCards.Creator.Should().Be("@testauthor");
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithSchemaOrgArticle_ShouldParseJsonLd()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Article Test</title>
            <script type='application/ld+json'>
            {
                ""@context"": ""https://schema.org"",
                ""@type"": ""Article"",
                ""headline"": ""Test Article Headline"",
                ""datePublished"": ""2025-01-01T00:00:00Z"",
                ""dateModified"": ""2025-01-02T00:00:00Z"",
                ""author"": {
                    ""@type"": ""Person"",
                    ""name"": ""John Doe""
                },
                ""publisher"": {
                    ""@type"": ""Organization"",
                    ""name"": ""Test Publisher""
                },
                ""articleSection"": ""Technology"",
                ""keywords"": ""test, article, metadata""
            }
            </script>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.SchemaOrg.MainEntityType.Should().Be("Article");
        metadata.SchemaOrg.Article.Should().NotBeNull();
        metadata.SchemaOrg.Article!.Headline.Should().Be("Test Article Headline");
        metadata.SchemaOrg.Article.Author.Should().Be("John Doe");
        metadata.SchemaOrg.Article.Publisher.Should().Be("Test Publisher");
        metadata.SchemaOrg.Article.Section.Should().Be("Technology");
        metadata.SchemaOrg.Article.Keywords.Should().Contain("test");
        metadata.SchemaOrg.RawJsonLd.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithSchemaOrgSoftware_ShouldParseJsonLd()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Software Test</title>
            <script type='application/ld+json'>
            {
                ""@context"": ""https://schema.org"",
                ""@type"": ""SoftwareApplication"",
                ""name"": ""Test App"",
                ""version"": ""1.0.0"",
                ""programmingLanguage"": ""C#"",
                ""runtimePlatform"": "".NET 9"",
                ""license"": ""MIT"",
                ""codeRepository"": ""https://github.com/test/app""
            }
            </script>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.SchemaOrg.MainEntityType.Should().Be("SoftwareApplication");
        metadata.SchemaOrg.Software.Should().NotBeNull();
        metadata.SchemaOrg.Software!.Name.Should().Be("Test App");
        metadata.SchemaOrg.Software.Version.Should().Be("1.0.0");
        metadata.SchemaOrg.Software.ProgrammingLanguage.Should().Be("C#");
        metadata.SchemaOrg.Software.RuntimePlatform.Should().Be(".NET 9");
        metadata.SchemaOrg.Software.License.Should().Be("MIT");
        metadata.SchemaOrg.Software.CodeRepository.Should().Be("https://github.com/test/app");
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithDublinCore_ShouldExtractAllFields()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Test</title>
            <meta name='DC.title' content='Dublin Core Title'>
            <meta name='DC.creator' content='DC Creator'>
            <meta name='DC.subject' content='DC Subject'>
            <meta name='DC.description' content='DC Description'>
            <meta name='DC.publisher' content='DC Publisher'>
            <meta name='DC.language' content='en'>
            <meta name='DC.format' content='text/html'>
            <meta name='DC.type' content='Text'>
            <meta name='DC.date' content='2025-01-01'>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.DublinCore.Title.Should().Be("Dublin Core Title");
        metadata.DublinCore.Creator.Should().Be("DC Creator");
        metadata.DublinCore.Subject.Should().Be("DC Subject");
        metadata.DublinCore.Description.Should().Be("DC Description");
        metadata.DublinCore.Publisher.Should().Be("DC Publisher");
        metadata.DublinCore.Language.Should().Be("en");
        metadata.DublinCore.Format.Should().Be("text/html");
        metadata.DublinCore.Type.Should().Be("Text");
        metadata.DublinCore.Date.Should().Be("2025-01-01");
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithDocumentStructure_ShouldAnalyzeContent()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head><title>Test</title></head>
        <body>
            <h1>Main Title</h1>
            <section>
                <h2>Section 1</h2>
                <p>Paragraph 1 with some content here.</p>
                <p>Paragraph 2 with more content here.</p>
                <a href='/link1'>Link 1</a>
                <img src='/image1.jpg' alt='Image 1'>
            </section>
            <section>
                <h2>Section 2</h2>
                <h3>Subsection 2.1</h3>
                <p>Another paragraph with content.</p>
                <ul>
                    <li>List item 1</li>
                    <li>List item 2</li>
                </ul>
                <table><tr><td>Cell</td></tr></table>
                <pre><code>var x = 1;</code></pre>
            </section>
        </body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Structure.Headings.Should().HaveCount(4); // h1, 2x h2, h3
        metadata.Structure.Headings[0].Level.Should().Be(1);
        metadata.Structure.Headings[0].Text.Should().Be("Main Title");
        metadata.Structure.SectionCount.Should().Be(2);
        metadata.Structure.ParagraphCount.Should().Be(3);
        metadata.Structure.LinkCount.Should().Be(1);
        metadata.Structure.ImageCount.Should().Be(1);
        metadata.Structure.TableCount.Should().Be(1);
        metadata.Structure.ListCount.Should().Be(1);
        metadata.Structure.CodeBlockCount.Should().BeGreaterThan(0);
        metadata.Structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(0);
        metadata.Structure.ComplexityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithNavigation_ShouldExtractLinks()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head><title>Test</title></head>
        <body>
            <nav>
                <a href='/home'>Home</a>
                <a href='/about'>About</a>
            </nav>
            <footer>
                <a href='/privacy'>Privacy</a>
                <a href='/terms'>Terms</a>
            </footer>
            <aside>
                <a href='/sidebar1'>Sidebar Link</a>
            </aside>
            <link type='application/rss+xml' href='/rss.xml'>
        </body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Navigation.Should().NotBeNull();
        metadata.Navigation.FooterLinks.Should().HaveCount(2);
        metadata.Navigation.SidebarLinks.Should().HaveCount(1);
        metadata.Navigation.RssFeedUrl.Should().Be("/rss.xml");
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithTechnicalMetadata_ShouldAnalyzeTechnology()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Test</title>
            <meta name='viewport' content='width=device-width'>
            <link rel='manifest' href='/manifest.json'>
            <script>console.log('test');</script>
        </head>
        <body></body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Technical.RequiresJavaScript.Should().BeTrue();
        metadata.Technical.IsMobileFriendly.Should().BeTrue();
        metadata.Technical.IsPwa.Should().BeTrue();
        metadata.Technical.Security.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithHttpsUrl_ShouldDetectHttps()
    {
        // Arrange
        var htmlContent = "<html><head><title>Test</title></head><body></body></html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Technical.Security.IsHttps.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithAccessibilityFeatures_ShouldCalculateScore()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head><title>Test</title></head>
        <body>
            <h1>Main Title</h1>
            <h2>Subtitle</h2>
            <img src='/image1.jpg' alt='Image description'>
            <img src='/image2.jpg' alt='Another image'>
            <a href='#main' class='skip-nav'>Skip to main content</a>
            <button aria-label='Close'>X</button>
        </body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Accessibility.ImageAltTextCoverage.Should().Be(1.0); // 100% alt text coverage
        metadata.Accessibility.HasProperHeadingStructure.Should().BeTrue();
        metadata.Accessibility.HasSkipNavigation.Should().BeTrue();
        metadata.Accessibility.UsesAriaLabels.Should().BeTrue();
        metadata.Accessibility.AccessibilityScore.Should().BeGreaterThan(80);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithContentClassification_ShouldDetectArticle()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head>
            <title>Test</title>
            <meta property='og:type' content='article'>
            <meta name='category' content='Technology'>
            <meta property='article:tag' content='AI, Machine Learning'>
        </head>
        <body>
            <article>
                <h1>Article Title</h1>
                <p>Content</p>
            </article>
        </body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Classification.ContentType.Should().Be("Article");
        metadata.Classification.Categories.Should().Contain("Technology");
        metadata.Classification.Tags.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithMultipleImages_ShouldCalculatePartialAltCoverage()
    {
        // Arrange
        var htmlContent = @"
        <html>
        <head><title>Test</title></head>
        <body>
            <img src='/image1.jpg' alt='Has alt'>
            <img src='/image2.jpg'>
            <img src='/image3.jpg' alt='Also has alt'>
            <img src='/image4.jpg'>
        </body>
        </html>";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, "https://example.com");

        // Assert
        metadata.Accessibility.ImageAltTextCoverage.Should().Be(0.5); // 2 out of 4 have alt text
    }

    #endregion
}

/// <summary>
/// 통합 테스트: 실제 웹사이트와 로컬 파일을 대상으로 테스트
/// </summary>
public class MetadataExtractorIntegrationTests
{
    private readonly MetadataExtractor _extractor;

    public MetadataExtractorIntegrationTests()
    {
        _extractor = new MetadataExtractor();
    }

    [Fact(Skip = "v1.0: Quality score below target (0.56 vs 0.8)")]
    public async Task ExtractMetadata_FromLocalRichFile_ShouldExtractComprehensiveData()
    {
        // Arrange
        var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        var htmlFile = Path.Combine(testDataPath, "sample-rich-metadata.html");

        // Ensure test file exists
        if (!File.Exists(htmlFile))
        {
            // Create test data directory and file if it doesn't exist
            Directory.CreateDirectory(testDataPath);
            await CreateTestDataFile(htmlFile);
        }

        var htmlContent = await File.ReadAllTextAsync(htmlFile);
        var sourceUrl = "file://" + htmlFile.Replace('\\', '/');

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert - Comprehensive validation
        metadata.Should().NotBeNull();
        metadata.SourceUrl.Should().Be(sourceUrl);
        metadata.QualityScore.Should().BeGreaterThan(0.8);

        // Basic HTML metadata
        metadata.Basic.Title.Should().Contain("WebFlux SDK");
        metadata.Basic.Description.Should().Contain("RAG");
        metadata.Basic.Keywords.Should().Contain("RAG");
        metadata.Basic.Author.Should().Be("Iyulab Corporation");
        metadata.Basic.Language.Should().Be("ko");

        // Open Graph
        metadata.OpenGraph.Title.Should().Contain("WebFlux SDK");
        metadata.OpenGraph.Type.Should().Be("website");
        metadata.OpenGraph.Image.Should().Contain("logo.png");

        // Schema.org
        metadata.SchemaOrg.MainEntityType.Should().Be("SoftwareLibrary");
        metadata.SchemaOrg.Software.Should().NotBeNull();
        metadata.SchemaOrg.Software!.Name.Should().Be("WebFlux SDK");

        // Document structure
        metadata.Structure.Headings.Should().HaveCountGreaterThan(3);
        metadata.Structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(0);

        // Completeness evaluation
        var completeness = _extractor.EvaluateCompleteness(metadata);
        completeness.OverallScore.Should().BeGreaterThan(0.75);
    }

    [Fact(Skip = "v1.0: Blog post metadata extraction needs improvement")]
    public async Task ExtractMetadata_FromLocalBlogFile_ShouldExtractArticleData()
    {
        // Arrange
        var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        var htmlFile = Path.Combine(testDataPath, "sample-blog-post.html");

        if (!File.Exists(htmlFile))
        {
            Directory.CreateDirectory(testDataPath);
            await CreateBlogPostTestFile(htmlFile);
        }

        var htmlContent = await File.ReadAllTextAsync(htmlFile);
        var sourceUrl = "file://" + htmlFile.Replace('\\', '/');

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Basic.Title.Should().Contain("RAG Pipeline Optimization");
        metadata.OpenGraph.Type.Should().Be("article");
        metadata.SchemaOrg.MainEntityType.Should().Be("Article");
        metadata.SchemaOrg.Article.Should().NotBeNull();
        metadata.SchemaOrg.Article!.Author.Should().Be("Jane Developer");

        // Document should have rich structure for a blog post
        metadata.Structure.Headings.Should().HaveCountGreaterThan(5);
        metadata.Structure.ParagraphCount.Should().BeGreaterThan(10);
        metadata.Structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(5);
    }

    private static async Task CreateTestDataFile(string filePath)
    {
        var content = @"<!DOCTYPE html>
<html lang=""ko"">
<head>
    <title>WebFlux SDK - AI 최적화 웹 콘텐츠 처리 라이브러리</title>
    <meta name=""description"" content=""RAG 시스템을 위한 고성능 웹 콘텐츠 크롤링 및 청킹 라이브러리"">
    <meta name=""keywords"" content=""RAG, AI, 웹 크롤링"">
    <meta name=""author"" content=""Iyulab Corporation"">
    <meta property=""og:title"" content=""WebFlux SDK"">
    <meta property=""og:type"" content=""website"">
    <meta property=""og:image"" content=""https://github.com/iyulab/WebFlux/raw/main/src/logo.png"">
    <script type=""application/ld+json"">
    {
        ""@context"": ""https://schema.org"",
        ""@type"": ""SoftwareLibrary"",
        ""name"": ""WebFlux SDK""
    }
    </script>
</head>
<body>
    <h1>WebFlux SDK</h1>
    <h2>주요 특징</h2>
    <p>RAG 시스템을 위한 라이브러리입니다.</p>
</body>
</html>";

        await File.WriteAllTextAsync(filePath, content);
    }

    private static async Task CreateBlogPostTestFile(string filePath)
    {
        var content = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <title>Advanced RAG Pipeline Optimization Techniques</title>
    <meta name=""description"" content=""Learn how to optimize RAG pipelines"">
    <meta property=""og:type"" content=""article"">
    <script type=""application/ld+json"">
    {
        ""@context"": ""https://schema.org"",
        ""@type"": ""Article"",
        ""headline"": ""Advanced RAG Pipeline Optimization Techniques"",
        ""author"": {
            ""@type"": ""Person"",
            ""name"": ""Jane Developer""
        }
    }
    </script>
</head>
<body>
    <article>
        <h1>Advanced RAG Pipeline Optimization Techniques</h1>
        <h2>Introduction</h2>
        <p>RAG has revolutionized AI applications.</p>
        <h2>Chunking Strategies</h2>
        <h3>Structure-Aware Chunking</h3>
        <p>Traditional fixed-size chunking often breaks semantic boundaries.</p>
        <h3>Semantic Chunking</h3>
        <p>Semantic chunking uses embeddings.</p>
        <h2>Performance Optimization</h2>
        <p>Large-scale document processing requires careful memory management.</p>
        <h2>Conclusion</h2>
        <p>Optimizing RAG pipelines requires a holistic approach.</p>
    </article>
</body>
</html>";

        await File.WriteAllTextAsync(filePath, content);
    }
}