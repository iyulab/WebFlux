using WebFlux.Services.ContentExtractors;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// HtmlToMarkdownExtractor 단위 테스트
/// HTML→Markdown 변환 파이프라인의 전체 동작 검증
/// </summary>
public class HtmlToMarkdownExtractorTests
{
    private readonly HtmlToMarkdownExtractor _extractor;

    public HtmlToMarkdownExtractorTests()
    {
        _extractor = new HtmlToMarkdownExtractor(null);
    }

    #region 기본 HTML→Markdown 변환

    [Fact]
    public async Task ExtractFromHtmlAsync_WithSimpleHtml_ShouldConvertToMarkdown()
    {
        // Arrange
        var html = "<html><head><title>Test Page</title></head><body><h1>Hello</h1><p>World</p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Contain("Hello");
        result.Text.Should().Contain("World");
        result.Title.Should().Be("Test Page");
        result.Url.Should().Be(url);
        result.ExtractionMethod.Should().Be("HtmlToMarkdown");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithHeadings_ShouldConvertToMarkdownHeadings()
    {
        // Arrange
        var html = @"<html><body>
            <h1>Title 1</h1>
            <h2>Subtitle</h2>
            <h3>Section</h3>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("# Title 1");
        result.Text.Should().Contain("## Subtitle");
        result.Text.Should().Contain("### Section");
        result.Headings.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithTable_ShouldConvertToMarkdownTable()
    {
        // Arrange
        var html = @"<html><body>
            <table>
                <thead><tr><th>Name</th><th>Age</th></tr></thead>
                <tbody><tr><td>Alice</td><td>30</td></tr></tbody>
            </table>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("Name");
        result.Text.Should().Contain("Age");
        result.Text.Should().Contain("Alice");
        result.Text.Should().Contain("30");
        // GFM 테이블은 | 문자를 포함
        result.Text.Should().Contain("|");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithCodeBlock_ShouldConvertToMarkdownCode()
    {
        // Arrange
        var html = @"<html><body>
            <pre><code>var x = 42;
console.log(x);</code></pre>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("var x = 42;");
        result.Text.Should().Contain("console.log(x);");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithLinks_ShouldConvertToMarkdownLinks()
    {
        // Arrange
        var html = @"<html><body>
            <p>Visit <a href=""https://example.com"">Example</a> for more info.</p>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("[Example]");
        result.Text.Should().Contain("https://example.com");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithLists_ShouldConvertToMarkdownLists()
    {
        // Arrange
        var html = @"<html><body>
            <ul>
                <li>Item 1</li>
                <li>Item 2</li>
                <li>Item 3</li>
            </ul>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("Item 1");
        result.Text.Should().Contain("Item 2");
        result.Text.Should().Contain("Item 3");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithImages_ShouldExtractImageUrls()
    {
        // Arrange
        var html = @"<html><body>
            <img src=""https://example.com/image.png"" alt=""Test image""/>
            <p>Some text</p>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("![Test image]");
        result.ImageUrls.Should().Contain("https://example.com/image.png");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithBoldAndItalic_ShouldConvertToMarkdown()
    {
        // Arrange
        var html = @"<html><body>
            <p>This is <strong>bold</strong> and <em>italic</em> text.</p>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("**bold**");
        result.Text.Should().Contain("*italic*");
    }

    #endregion

    #region RawMarkdown / FitMarkdown 다중 출력

    [Fact]
    public async Task ExtractFromHtmlAsync_ShouldPopulateRawMarkdown()
    {
        // Arrange
        var html = @"<html><body><article><p>Main content here.</p></article></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.RawMarkdown.Should().NotBeNullOrEmpty();
        result.RawMarkdown.Should().Contain("Main content here.");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_TextShouldUseFitMarkdownOrRawMarkdown()
    {
        // Arrange
        var html = @"<html><body><article><p>Main content.</p></article></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        // Text는 FitMarkdown 또는 RawMarkdown 중 하나를 사용해야 함
        result.Text.Should().NotBeNullOrEmpty();
        var expectedText = result.FitMarkdown ?? result.RawMarkdown;
        // 정규화 때문에 정확히 동일하지 않을 수 있으나 콘텐츠는 포함해야 함
        result.Text.Should().Contain("Main content.");
    }

    #endregion

    #region 보일러플레이트 제거

    [Fact]
    public async Task ExtractFromHtmlAsync_WithNavAndFooter_ShouldRemoveBoilerplate()
    {
        // Arrange
        var html = @"<html><body>
            <nav><a href=""/home"">Home</a><a href=""/about"">About</a></nav>
            <article><p>This is the main article content that should be preserved.</p></article>
            <footer><p>Copyright 2024</p></footer>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("main article content");
        // nav/footer 콘텐츠는 제거되어야 함
        result.Text.Should().NotContain("Copyright 2024");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithAdvertisement_ShouldRemoveAds()
    {
        // Arrange
        var html = @"<html><body>
            <article><p>Real content paragraph with enough text to be meaningful.</p></article>
            <div class=""advertisement""><p>Buy our product!</p></div>
        </body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.Text.Should().Contain("Real content");
        result.Text.Should().NotContain("Buy our product");
    }

    #endregion

    #region 메타데이터 추출

    [Fact]
    public async Task ExtractFromHtmlAsync_WithMetadataEnabled_ShouldExtractMetadata()
    {
        // Arrange
        var html = @"<html><head><title>Test Title</title></head>
            <body><p>Some content text here for testing purposes.</p></body></html>";
        var url = "https://example.com/page";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url, enableMetadataExtraction: true);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Title.Should().Be("Test Title");
        result.Metadata.Domain.Should().Be("example.com");
        result.Metadata.Url.Should().Be(url);
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_ShouldCalculateStatistics()
    {
        // Arrange
        var html = @"<html><body><p>Word one two three four five six seven eight nine ten.</p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.WordCount.Should().BeGreaterThan(0);
        result.CharacterCount.Should().BeGreaterThan(0);
        result.ReadingTimeMinutes.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region 빈 입력 및 엣지 케이스

    [Fact]
    public async Task ExtractFromHtmlAsync_WithEmptyHtml_ShouldReturnEmptyContent()
    {
        // Act
        var result = await _extractor.ExtractFromHtmlAsync("", "https://example.com");

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithWhitespaceOnly_ShouldReturnEmptyContent()
    {
        // Act
        var result = await _extractor.ExtractFromHtmlAsync("   ", "https://example.com");

        // Assert
        result.Text.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_WithNoTitle_ShouldReturnUntitled()
    {
        // Arrange
        var html = "<html><body><p>Content</p></body></html>";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, "https://example.com");

        // Assert
        result.Title.Should().Be("Untitled");
    }

    [Fact]
    public async Task ExtractFromHtmlAsync_ShouldPreserveOriginalHtml()
    {
        // Arrange
        var html = "<html><body><p>Content</p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        result.OriginalHtml.Should().Be(html);
    }

    #endregion

    #region IContentExtractor 위임 메서드

    [Fact]
    public async Task ExtractFromMarkdownAsync_ShouldDelegateToBasicExtractor()
    {
        // Arrange
        var markdown = "# Heading\n\nParagraph text.";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromMarkdownAsync(markdown, url);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be(markdown);
    }

    [Fact]
    public async Task ExtractFromJsonAsync_ShouldDelegateToBasicExtractor()
    {
        // Arrange
        var json = "{\"key\":\"value\"}";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromJsonAsync(json, url);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Contain("value");
    }

    [Fact]
    public async Task ExtractAutoAsync_WithHtmlType_ShouldUseMarkdownConversion()
    {
        // Arrange
        var html = "<html><body><h1>Title</h1><p>Content</p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractAutoAsync(html, url, "text/html");

        // Assert
        result.ExtractionMethod.Should().Be("HtmlToMarkdown");
        result.Text.Should().Contain("Title");
    }

    [Fact]
    public async Task ExtractAutoAsync_WithTextType_ShouldDelegateToBasicExtractor()
    {
        // Arrange
        var text = "Plain text content.";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractAutoAsync(text, url, "text/plain");

        // Assert
        result.Text.Should().Be(text);
    }

    [Fact]
    public void GetSupportedContentTypes_ShouldReturnAllTypes()
    {
        // Act
        var types = _extractor.GetSupportedContentTypes();

        // Assert
        types.Should().Contain("text/html");
        types.Should().Contain("application/xhtml+xml");
        types.Should().Contain("text/markdown");
        types.Should().Contain("application/json");
    }

    #endregion

    #region 폴백 테스트

    [Fact]
    public async Task ExtractFromHtmlAsync_WithMalformedHtml_ShouldStillWork()
    {
        // Arrange - 매우 손상된 HTML
        var html = "<html><body><p>Unclosed paragraph<div>Nested <b>badly</div></p></body></html>";
        var url = "https://example.com";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);

        // Assert
        // 어떤 형태로든 텍스트가 추출되어야 함
        result.Should().NotBeNull();
        result.Text.Should().NotBeNull();
    }

    #endregion

    #region 통합 파이프라인 테스트

    [Fact]
    public async Task ExtractFromHtmlAsync_FullPipeline_WithComplexPage()
    {
        // Arrange - 실제 웹페이지와 유사한 구조
        var html = @"<!DOCTYPE html>
<html>
<head><title>Blog Post Title</title></head>
<body>
    <nav>
        <a href=""/"">Home</a>
        <a href=""/blog"">Blog</a>
        <a href=""/about"">About</a>
    </nav>
    <header>
        <h1>Blog Post Title</h1>
        <span class=""date"">2024-01-15</span>
    </header>
    <article>
        <h2>Introduction</h2>
        <p>This is the introduction paragraph with <strong>important</strong> information.</p>
        <h2>Main Section</h2>
        <p>Here is the main content with a <a href=""https://example.com/link"">useful link</a>.</p>
        <ul>
            <li>Point one</li>
            <li>Point two</li>
            <li>Point three</li>
        </ul>
        <table>
            <tr><th>Feature</th><th>Status</th></tr>
            <tr><td>Markdown</td><td>Supported</td></tr>
        </table>
        <pre><code>var example = ""code"";</code></pre>
    </article>
    <aside class=""sidebar"">
        <h3>Related Posts</h3>
        <ul><li><a href=""/other"">Other Post</a></li></ul>
    </aside>
    <footer>
        <p>Copyright 2024. All rights reserved.</p>
    </footer>
</body>
</html>";
        var url = "https://example.com/blog/post";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url, enableMetadataExtraction: true);

        // Assert
        result.Title.Should().Be("Blog Post Title");
        result.ExtractionMethod.Should().Be("HtmlToMarkdown");

        // 본문 콘텐츠 보존
        result.Text.Should().Contain("Introduction");
        result.Text.Should().Contain("Main Section");
        result.Text.Should().Contain("**important**");
        result.Text.Should().Contain("Point one");

        // 구조 보존 (테이블, 코드 블록)
        result.Text.Should().Contain("|");
        result.Text.Should().Contain("code");

        // 다중 출력
        result.RawMarkdown.Should().NotBeNullOrEmpty();

        // 메타데이터
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Domain.Should().Be("example.com");
    }

    #endregion
}
