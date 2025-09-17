using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// EnhancedMetadataExtractor 테스트
/// 15개 웹 표준 메타데이터 추출 기능을 검증합니다
/// </summary>
public class EnhancedMetadataExtractorTests
{
    private readonly EnhancedMetadataExtractor _extractor;

    public EnhancedMetadataExtractorTests()
    {
        _extractor = new EnhancedMetadataExtractor();
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void CalculateQualityScore_WithCompleteMetadata_ShouldReturnHighScore()
    {
        // Arrange
        var metadata = CreateCompleteMetadata();

        // Act
        var score = _extractor.CalculateQualityScore(metadata);

        // Assert
        score.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public void CalculateQualityScore_WithIncompleteMetadata_ShouldReturnLowScore()
    {
        // Arrange
        var metadata = new EnhancedWebMetadata
        {
            SourceUrl = "https://example.com"
        };

        // Act
        var score = _extractor.CalculateQualityScore(metadata);

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
        var metadata = new EnhancedWebMetadata
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

        // Note: This test should be moved to integration test suite
        // as it requires network access
        await Task.CompletedTask;
    }

    private void ValidateBasicMetadata(BasicHtmlMetadata basic)
    {
        basic.Title.Should().NotBeNullOrEmpty();
        basic.Description.Should().NotBeNullOrEmpty();
        basic.Keywords.Should().NotBeEmpty();
        basic.Language.Should().NotBeNullOrEmpty();
    }

    private void ValidateOpenGraphMetadata(OpenGraphMetadata og)
    {
        og.Title.Should().NotBeNullOrEmpty();
        og.Description.Should().NotBeNullOrEmpty();
        og.Image.Should().NotBeNullOrEmpty();
        og.Type.Should().NotBeNullOrEmpty();
    }

    private void ValidateTwitterCardsMetadata(TwitterCardsMetadata twitter)
    {
        twitter.Card.Should().NotBeNullOrEmpty();
        twitter.Title.Should().NotBeNullOrEmpty();
        twitter.Description.Should().NotBeNullOrEmpty();
        twitter.Image.Should().NotBeNullOrEmpty();
    }

    private void ValidateSchemaOrgMetadata(SchemaOrgMetadata schema)
    {
        schema.MainEntityType.Should().NotBeNullOrEmpty();
        schema.RawJsonLd.Should().NotBeEmpty();
    }

    private void ValidateDublinCoreMetadata(DublinCoreMetadata dublin)
    {
        dublin.Title.Should().NotBeNullOrEmpty();
        dublin.Creator.Should().NotBeNullOrEmpty();
        dublin.Type.Should().NotBeNullOrEmpty();
    }

    private void ValidateDocumentStructure(DocumentStructure structure)
    {
        structure.Headings.Should().NotBeEmpty();
        structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(0);
        structure.ComplexityScore.Should().BeGreaterThan(0);
    }

    private void ValidateAccessibilityMetadata(AccessibilityMetadata accessibility)
    {
        accessibility.AccessibilityScore.Should().BeGreaterThanOrEqualTo(0);
        accessibility.AccessibilityScore.Should().BeLessThanOrEqualTo(100);
    }

    private EnhancedWebMetadata CreateCompleteMetadata()
    {
        return new EnhancedWebMetadata
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
}

/// <summary>
/// 통합 테스트: 실제 웹사이트와 로컬 파일을 대상으로 테스트
/// </summary>
public class EnhancedMetadataExtractorIntegrationTests
{
    private readonly EnhancedMetadataExtractor _extractor;

    public EnhancedMetadataExtractorIntegrationTests()
    {
        _extractor = new EnhancedMetadataExtractor();
    }

    [Fact]
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

    [Fact]
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

    private async Task CreateTestDataFile(string filePath)
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

    private async Task CreateBlogPostTestFile(string filePath)
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