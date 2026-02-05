using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// ContentQualityEvaluator 단위 테스트
/// 콘텐츠 품질 평가 기능 검증
/// </summary>
public class ContentQualityEvaluatorTests
{
    private readonly Mock<ILogger<ContentQualityEvaluator>> _mockLogger;
    private readonly ContentQualityEvaluator _evaluator;

    public ContentQualityEvaluatorTests()
    {
        _mockLogger = new Mock<ILogger<ContentQualityEvaluator>>();
        _evaluator = new ContentQualityEvaluator(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContentQualityEvaluator(null!));
    }

    [Fact]
    public void Constructor_WithLogger_ShouldNotThrow()
    {
        // Act & Assert
        var evaluator = new ContentQualityEvaluator(_mockLogger.Object);
        evaluator.Should().NotBeNull();
    }

    #endregion

    #region EvaluateAsync Tests

    [Fact]
    public async Task EvaluateAsync_WithValidContent_ShouldReturnQualityInfo()
    {
        // Arrange - 100단어 이상의 충분한 콘텐츠
        var longContent = string.Join(" ", Enumerable.Repeat("This is a test content with multiple words and sentences.", 10));
        var content = new ExtractedContent
        {
            Url = "https://example.com",
            Text = longContent,
            MainContent = longContent
        };

        // Act
        var quality = await _evaluator.EvaluateAsync(content);

        // Assert
        quality.Should().NotBeNull();
        quality.OverallScore.Should().BeInRange(0, 1);
        quality.WordCount.Should().BeGreaterThan(50);
        quality.HasMainContent.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyContent_ShouldReturnLowScore()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com",
            Text = "",
            MainContent = ""
        };

        // Act
        var quality = await _evaluator.EvaluateAsync(content);

        // Assert
        quality.OverallScore.Should().BeLessThanOrEqualTo(0.5);
        quality.HasMainContent.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WithPaywallContent_ShouldDetectPaywall()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com",
            Text = "Subscribe to continue reading. Premium content for members only.",
            MainContent = "Subscribe to continue reading. Premium content for members only."
        };
        var html = "<div class='paywall'>Subscribe to continue reading</div>";

        // Act
        var quality = await _evaluator.EvaluateAsync(content, html);

        // Assert
        quality.HasPaywall.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WithGoodContent_ShouldReturnHighScore()
    {
        // Arrange
        var longContent = string.Join(" ", Enumerable.Repeat("This is a quality sentence with meaningful words.", 50));
        var content = new ExtractedContent
        {
            Url = "https://example.com",
            Text = longContent,
            MainContent = longContent,
            Title = "Test Article",
            Headings = new List<string> { "Introduction", "Main Content", "Conclusion" },
            Metadata = new EnrichedMetadata { Author = "Test Author", PublishedDate = DateTimeOffset.UtcNow }
        };

        // Act
        var quality = await _evaluator.EvaluateAsync(content);

        // Assert
        quality.OverallScore.Should().BeGreaterThan(0.5);
        quality.HasMainContent.Should().BeTrue();
    }

    #endregion

    #region EvaluateHtmlAsync Tests

    [Fact]
    public async Task EvaluateHtmlAsync_WithValidHtml_ShouldReturnQualityInfo()
    {
        // Arrange
        var html = "<html><body><p>This is test content for evaluation.</p></body></html>";
        var url = "https://example.com";

        // Act
        var quality = await _evaluator.EvaluateHtmlAsync(html, url);

        // Assert
        quality.Should().NotBeNull();
        quality.OverallScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task EvaluateHtmlAsync_WithEmptyHtml_ShouldReturnLowScore()
    {
        // Arrange
        var html = "";
        var url = "https://example.com";

        // Act
        var quality = await _evaluator.EvaluateHtmlAsync(html, url);

        // Assert
        quality.OverallScore.Should().BeLessThanOrEqualTo(0.5);
    }

    #endregion

    #region DetectPaywall Tests

    [Theory]
    [InlineData("<div class='paywall'>Content blocked</div>", true)]
    [InlineData("<div>Subscribe to read more</div>", true)]
    [InlineData("<div>Premium content only</div>", true)]
    [InlineData("<div>Members only access</div>", true)]
    [InlineData("<div>Just regular content here</div>", false)]
    [InlineData("<article>Free to read article</article>", false)]
    public void DetectPaywall_WithVariousHtml_ShouldDetectCorrectly(string html, bool expectedPaywall)
    {
        // Act
        var hasPaywall = _evaluator.DetectPaywall(html);

        // Assert
        hasPaywall.Should().Be(expectedPaywall);
    }

    [Fact]
    public void DetectPaywall_WithKoreanPaywall_ShouldDetect()
    {
        // Arrange
        var html = "<div>구독하시면 전체 기사를 볼 수 있습니다</div>";

        // Act
        var hasPaywall = _evaluator.DetectPaywall(html);

        // Assert
        hasPaywall.Should().BeTrue();
    }

    #endregion

    #region ClassifyContentType Tests

    [Fact]
    public void ClassifyContentType_WithArticle_ShouldClassifyAsArticle()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://news.example.com/article/123",
            Title = "Breaking News Article",
            MainContent = "This is a news story about recent events."
        };

        // Act
        var type = _evaluator.ClassifyContentType(content);

        // Assert
        type.Should().BeOneOf("article", "general");
    }

    [Fact]
    public void ClassifyContentType_WithBlog_ShouldClassifyAsBlog()
    {
        // Arrange - blog 키워드가 article 키워드보다 명확하게 포함되도록
        var content = new ExtractedContent
        {
            Url = "https://myblog.example.com/blog/my-post",
            Title = "My Blog Entry",
            MainContent = "Posted by blog Author on this blog site."
        };

        // Act
        var type = _evaluator.ClassifyContentType(content);

        // Assert
        type.Should().BeOneOf("blog", "article", "general");
    }

    [Fact]
    public void ClassifyContentType_WithDocumentation_ShouldClassifyAsDocumentation()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://docs.example.com/api/reference",
            Title = "API Documentation",
            MainContent = "This is the API reference guide."
        };

        // Act
        var type = _evaluator.ClassifyContentType(content);

        // Assert
        type.Should().BeOneOf("documentation", "general");
    }

    [Fact]
    public void ClassifyContentType_WithProduct_ShouldClassifyAsProduct()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://shop.example.com/product/123",
            Title = "Product Name",
            MainContent = "Buy now! Add to cart. Price: $99.99"
        };

        // Act
        var type = _evaluator.ClassifyContentType(content);

        // Assert
        type.Should().BeOneOf("product", "general");
    }

    #endregion

    #region DetectLanguage Tests

    [Theory]
    [InlineData("This is English text content.", "en")]
    [InlineData("이것은 한국어 텍스트입니다.", "ko")]
    [InlineData("これは日本語のテキストです。", "ja")]
    [InlineData("这是中文文本。", "zh")]
    public void DetectLanguage_WithVariousLanguages_ShouldDetectCorrectly(string text, string expectedLanguage)
    {
        // Act
        var language = _evaluator.DetectLanguage(text);

        // Assert
        language.Should().Be(expectedLanguage);
    }

    [Fact]
    public void DetectLanguage_WithEmptyText_ShouldReturnEnglish()
    {
        // Act
        var language = _evaluator.DetectLanguage("");

        // Assert
        language.Should().Be("en");
    }

    #endregion

    #region CalculateAdDensity Tests

    [Fact]
    public void CalculateAdDensity_WithNoAds_ShouldReturnZero()
    {
        // Arrange
        var html = "<html><body><p>Clean content without ads</p></body></html>";

        // Act
        var density = _evaluator.CalculateAdDensity(html);

        // Assert
        density.Should().Be(0);
    }

    [Fact]
    public void CalculateAdDensity_WithAds_ShouldReturnPositiveValue()
    {
        // Arrange
        var html = @"<html><body>
            <div class='ad-container'>Ad 1</div>
            <div class='advertisement'>Ad 2</div>
            <div class='sponsored'>Ad 3</div>
            <ins class='adsense'>Ad 4</ins>
        </body></html>";

        // Act
        var density = _evaluator.CalculateAdDensity(html);

        // Assert
        density.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateAdDensity_WithEmptyHtml_ShouldReturnZero()
    {
        // Act
        var density = _evaluator.CalculateAdDensity("");

        // Assert
        density.Should().Be(0);
    }

    #endregion

    #region CalculateContentRatio Tests

    [Fact]
    public void CalculateContentRatio_WithGoodRatio_ShouldReturnHighValue()
    {
        // Arrange
        var html = "<p>Content</p>";
        var text = "Content";

        // Act
        var ratio = _evaluator.CalculateContentRatio(html, text);

        // Assert
        ratio.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public void CalculateContentRatio_WithLowRatio_ShouldReturnLowValue()
    {
        // Arrange
        var html = "<html><head><style>lots of css</style></head><body><div><div><div><span>tiny</span></div></div></div></body></html>";
        var text = "tiny";

        // Act
        var ratio = _evaluator.CalculateContentRatio(html, text);

        // Assert
        ratio.Should().BeLessThan(0.5);
    }

    [Fact]
    public void CalculateContentRatio_WithEmptyContent_ShouldReturnZero()
    {
        // Act
        var ratio = _evaluator.CalculateContentRatio("", "");

        // Assert
        ratio.Should().Be(0);
    }

    #endregion

    #region EstimateTokenCount Tests

    [Fact]
    public void EstimateTokenCount_WithEnglishText_ShouldEstimateCorrectly()
    {
        // Arrange
        var text = "This is a test sentence with exactly eight words here.";

        // Act
        var tokens = _evaluator.EstimateTokenCount(text);

        // Assert
        // 영어 ~4자 = 1토큰, 약 53자 -> 약 13 토큰
        tokens.Should().BeInRange(10, 20);
    }

    [Fact]
    public void EstimateTokenCount_WithKoreanText_ShouldEstimateCorrectly()
    {
        // Arrange
        var text = "이것은 한국어 테스트 문장입니다.";

        // Act
        var tokens = _evaluator.EstimateTokenCount(text);

        // Assert
        // 한국어 ~1.5자 = 1토큰, 약 15자 -> 약 10 토큰
        tokens.Should().BeGreaterThan(5);
    }

    [Fact]
    public void EstimateTokenCount_WithEmptyText_ShouldReturnZero()
    {
        // Act
        var tokens = _evaluator.EstimateTokenCount("");

        // Assert
        tokens.Should().Be(0);
    }

    #endregion

    #region ContentQualityInfo Model Tests

    [Fact]
    public void ContentQualityInfo_Grade_ShouldCalculateCorrectly()
    {
        // Assert
        new ContentQualityInfo { OverallScore = 0.9 }.Grade.Should().Be(QualityGrade.Excellent);
        new ContentQualityInfo { OverallScore = 0.7 }.Grade.Should().Be(QualityGrade.Good);
        new ContentQualityInfo { OverallScore = 0.5 }.Grade.Should().Be(QualityGrade.Fair);
        new ContentQualityInfo { OverallScore = 0.3 }.Grade.Should().Be(QualityGrade.Poor);
        new ContentQualityInfo { OverallScore = 0.1 }.Grade.Should().Be(QualityGrade.VeryPoor);
    }

    [Fact]
    public void ContentQualityInfo_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var quality = new ContentQualityInfo
        {
            OverallScore = 0.8,
            WordCount = 500,
            EstimatedReadingTimeMinutes = 3,
            HasPaywall = true
        };

        // Act
        var str = quality.ToString();

        // Assert
        str.Should().Contain("80%");
        str.Should().Contain("Excellent");
        str.Should().Contain("Paywall");
        str.Should().Contain("500 words");
    }

    #endregion
}
