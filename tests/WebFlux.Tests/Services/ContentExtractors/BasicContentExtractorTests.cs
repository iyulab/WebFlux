using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services.ContentExtractors;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// BasicContentExtractor 단위 테스트
/// 다양한 콘텐츠 타입에서 텍스트 추출 기능 검증
/// </summary>
public class BasicContentExtractorTests
{
    private readonly BasicContentExtractor _extractor;

    public BasicContentExtractorTests()
    {
        _extractor = new BasicContentExtractor(null);
    }

    #region ExtractFromHtmlAsync Tests

    [Fact]
    public async Task ExtractFromHtmlAsync_WithSimpleHtml_ShouldExtractText()
    {
        // Arrange
        var html = "<html><head><title>Test Page</title></head><body><p>Hello World</p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Contain("Hello World");
        result.Title.Should().Be("Test Page");
        result.Url.Should().Be(url);
        result.MainContent.Should().Contain("Hello World");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithHtmlEntities_ShouldDecode()
    {
        // Arrange
        var html = "<html><body>&lt;p&gt;&amp;&nbsp;&copy;</body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("<p>");
        result.Text.Should().Contain("&");
        result.Text.Should().Contain("©");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithNoTitle_ShouldReturnUntitled()
    {
        // Arrange
        var html = "<html><body>Content without title</body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Title.Should().Be("Untitled");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithComplexHtml_ShouldRemoveAllTags()
    {
        // Arrange
        var html = @"<html>
            <head><title>Complex</title></head>
            <body>
                <div class='header'><h1>Title</h1></div>
                <article>
                    <p>Paragraph 1</p>
                    <p>Paragraph 2</p>
                </article>
            </body>
        </html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().NotContain("<");
        result.Text.Should().NotContain(">");
        result.Text.Should().Contain("Title");
        result.Text.Should().Contain("Paragraph 1");
        result.Text.Should().Contain("Paragraph 2");
    }

    #endregion

    #region ExtractFromMarkdownAsync Tests

    [Fact]
    public async Task ExtractFromMarkdownAsync_ShouldPreserveMarkdown()
    {
        // Arrange
        var markdown = "# Heading\n\nThis is **bold** text.";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromMarkdownAsync(markdown, url);

        // Assert
        result.Text.Should().Be(markdown);
        result.MainContent.Should().Be(markdown);
        result.Title.Should().Be("Markdown Content");
        result.Url.Should().Be(url);
    }

    [Fact]
    public async Task ExtractFromMarkdownAsync_WithComplexMarkdown_ShouldPreserveAll()
    {
        // Arrange
        var markdown = @"# Title

## Subtitle

- Item 1
- Item 2

```csharp
var code = ""example"";
```";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromMarkdownAsync(markdown, url);

        // Assert
        result.Text.Should().Be(markdown);
        result.MainContent.Should().Be(markdown);
    }

    #endregion

    #region ExtractFromJsonAsync Tests

    [Fact]
    public async Task ExtractFromJsonAsync_WithValidJson_ShouldParseSuccessfully()
    {
        // Arrange
        var json = "{\"name\":\"John\",\"age\":30}";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromJsonAsync(json, url);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Contain("John");
        result.Title.Should().Be("JSON Content");
        result.Url.Should().Be(url);
    }

    [Fact]
    public async Task ExtractFromJsonAsync_WithInvalidJson_ShouldReturnOriginalContent()
    {
        // Arrange
        var json = "{invalid json}";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromJsonAsync(json, url);

        // Assert
        result.Text.Should().Be(json);
        result.MainContent.Should().Be(json);
        result.Title.Should().Be("JSON Content");
    }

    [Fact]
    public async Task ExtractFromJsonAsync_WithNestedJson_ShouldParseAll()
    {
        // Arrange
        var json = "{\"user\":{\"name\":\"Alice\",\"address\":{\"city\":\"NYC\"}}}";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromJsonAsync(json, url);

        // Assert
        result.Text.Should().Contain("Alice");
        result.Text.Should().Contain("NYC");
    }

    #endregion

    #region ExtractFromXmlAsync Tests

    [Fact]
    public async Task ExtractFromXmlAsync_ShouldPreserveXml()
    {
        // Arrange
        var xml = "<?xml version=\"1.0\"?><root><item>Value</item></root>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromXmlAsync(xml, url);

        // Assert
        result.Text.Should().Be(xml);
        result.MainContent.Should().Be(xml);
        result.Title.Should().Be("XML Content");
        result.Url.Should().Be(url);
    }

    #endregion

    #region ExtractFromTextAsync Tests

    [Fact]
    public async Task ExtractFromTextAsync_ShouldPreserveText()
    {
        // Arrange
        var text = "Plain text content\nWith multiple lines.";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromTextAsync(text, url);

        // Assert
        result.Text.Should().Be(text);
        result.MainContent.Should().Be(text);
        result.Title.Should().Be("Text Content");
        result.Url.Should().Be(url);
    }

    #endregion

    #region ExtractAutoAsync Tests

    [Theory]
    [InlineData("text/html", "html")]
    [InlineData("application/xhtml+xml", "html")]
    [InlineData("text/markdown", "markdown")]
    [InlineData("application/json", "json")]
    [InlineData("application/xml", "xml")]
    [InlineData("text/xml", "xml")]
    [InlineData("text/plain", "text")]
    [InlineData(null, "text")]
    public async Task ExtractAutoAsync_WithContentType_ShouldSelectCorrectExtractor(string? contentType, string expectedExtractorType)
    {
        // Arrange
        var content = "<test>content</test>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractAutoAsync(content, url, contentType);

        // Assert
        result.Should().NotBeNull();
        result.Url.Should().Be(url);

        // Title을 통해 어떤 추출기가 사용되었는지 확인
        switch (expectedExtractorType)
        {
            case "markdown":
                result.Title.Should().Be("Markdown Content");
                break;
            case "json":
                result.Title.Should().Be("JSON Content");
                break;
            case "xml":
                result.Title.Should().Be("XML Content");
                break;
            case "text":
                result.Title.Should().Be("Text Content");
                break;
        }
    }

    [Fact]
    public async Task ExtractAutoAsync_WithHtmlContentType_ShouldRemoveTags()
    {
        // Arrange
        var html = "<html><body><p>Text content</p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractAutoAsync(html, url, "text/html");

        // Assert
        result.Text.Should().NotContain("<");
        result.Text.Should().Contain("Text content");
    }

    #endregion

    #region GetSupportedContentTypes Tests

    [Fact]
    public void GetSupportedContentTypes_ShouldReturnAllSupportedTypes()
    {
        // Act
        var types = _extractor.GetSupportedContentTypes();

        // Assert
        types.Should().NotBeNull();
        types.Should().HaveCount(7);
        types.Should().Contain("text/html");
        types.Should().Contain("application/xhtml+xml");
        types.Should().Contain("text/markdown");
        types.Should().Contain("application/json");
        types.Should().Contain("application/xml");
        types.Should().Contain("text/xml");
        types.Should().Contain("text/plain");
    }

    #endregion

    #region GetStatistics Tests

    [Fact]
    public void GetStatistics_ShouldReturnEmptyStatistics()
    {
        // Act
        var stats = _extractor.GetStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalExtractions.Should().Be(0);
        stats.SuccessfulExtractions.Should().Be(0);
        stats.FailedExtractions.Should().Be(0);
        stats.AverageProcessingTimeMs.Should().Be(0);
        stats.SupportedContentTypes.Should().Be(7);
    }

    [Fact]
    public async Task GetStatistics_AfterExtraction_ShouldStillReturnZero()
    {
        // Arrange
        await _extractor.ExtractFromHtmlAsync("<html></html>", "https://example.com");

        // Act
        var stats = _extractor.GetStatistics();

        // Assert
        stats.TotalExtractions.Should().Be(0); // Basic implementation doesn't track statistics
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task ExtractFromHtmlAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var html = "<html><body>Test</body></html>";
        var url = "https://example.com";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url, false, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExtractAutoAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var content = "Test content";
        var url = "https://example.com";
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _extractor.ExtractAutoAsync(content, url, "text/plain", cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
