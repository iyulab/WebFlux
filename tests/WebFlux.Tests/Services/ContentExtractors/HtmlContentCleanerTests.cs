using WebFlux.Core.Options;
using WebFlux.Services.ContentExtractors;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// HtmlContentCleaner 단위 테스트
/// HTML 콘텐츠 정리 (노이즈 제거, URL 변환) 검증
/// </summary>
public class HtmlContentCleanerTests
{
    private readonly HtmlContentCleaner _cleaner;

    public HtmlContentCleanerTests()
    {
        _cleaner = new HtmlContentCleaner();
    }

    #region 기본 정리

    [Fact]
    public async Task CleanAsync_ShouldRemoveScriptTags()
    {
        // Arrange
        var html = @"<html><body>
            <p>Content</p>
            <script>alert('xss');</script>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Content");
        result.Should().NotContain("script");
        result.Should().NotContain("alert");
    }

    [Fact]
    public async Task CleanAsync_ShouldRemoveStyleTags()
    {
        // Arrange
        var html = @"<html><body>
            <style>.hidden { display: none; }</style>
            <p>Visible content</p>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Visible content");
        result.Should().NotContain("display: none");
    }

    [Fact]
    public async Task CleanAsync_ShouldRemoveNoscriptTags()
    {
        // Arrange
        var html = @"<html><body>
            <p>Content</p>
            <noscript>Enable JavaScript</noscript>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Content");
        result.Should().NotContain("Enable JavaScript");
    }

    #endregion

    #region OnlyMainContent

    [Fact]
    public async Task CleanAsync_WithOnlyMainContent_ShouldRemoveNav()
    {
        // Arrange
        var html = @"<html><body>
            <nav><a href=""/"">Home</a></nav>
            <p>Main content here</p>
        </body></html>";
        var options = new HtmlCleaningOptions { OnlyMainContent = true };

        // Act
        var result = await _cleaner.CleanAsync(html, options: options);

        // Assert
        result.Should().Contain("Main content here");
        result.Should().NotContain("<nav>");
    }

    [Fact]
    public async Task CleanAsync_WithOnlyMainContent_ShouldRemoveHeader()
    {
        // Arrange
        var html = @"<html><body>
            <header><h1>Site Header</h1></header>
            <article><p>Article content</p></article>
        </body></html>";
        var options = new HtmlCleaningOptions { OnlyMainContent = true };

        // Act
        var result = await _cleaner.CleanAsync(html, options: options);

        // Assert
        result.Should().Contain("Article content");
        result.Should().NotContain("<header>");
    }

    [Fact]
    public async Task CleanAsync_WithOnlyMainContent_ShouldRemoveFooter()
    {
        // Arrange
        var html = @"<html><body>
            <p>Content</p>
            <footer><p>Copyright 2024</p></footer>
        </body></html>";
        var options = new HtmlCleaningOptions { OnlyMainContent = true };

        // Act
        var result = await _cleaner.CleanAsync(html, options: options);

        // Assert
        result.Should().Contain("Content");
        result.Should().NotContain("Copyright 2024");
    }

    [Fact]
    public async Task CleanAsync_WithOnlyMainContentDisabled_ShouldKeepNav()
    {
        // Arrange
        var html = @"<html><body>
            <nav><a href=""/"">Home</a></nav>
            <p>Content</p>
        </body></html>";
        var options = new HtmlCleaningOptions { OnlyMainContent = false };

        // Act
        var result = await _cleaner.CleanAsync(html, options: options);

        // Assert
        result.Should().Contain("Home");
    }

    #endregion

    #region 광고/보일러플레이트 제거

    [Fact]
    public async Task CleanAsync_ShouldRemoveAdvertisement()
    {
        // Arrange
        var html = @"<html><body>
            <p>Real content</p>
            <div class=""advertisement"">Ad content</div>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Real content");
        result.Should().NotContain("Ad content");
    }

    [Fact]
    public async Task CleanAsync_ShouldRemoveCookieBanner()
    {
        // Arrange
        var html = @"<html><body>
            <p>Page content</p>
            <div class=""cookie-banner"">Accept cookies?</div>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Page content");
        result.Should().NotContain("Accept cookies");
    }

    [Fact]
    public async Task CleanAsync_ShouldRemoveSocialShareButtons()
    {
        // Arrange
        var html = @"<html><body>
            <article><p>Article text</p></article>
            <div class=""social-share""><button>Share on Twitter</button></div>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Article text");
        result.Should().NotContain("Share on Twitter");
    }

    [Fact]
    public async Task CleanAsync_ShouldRemoveRelatedPosts()
    {
        // Arrange
        var html = @"<html><body>
            <article><p>Main article</p></article>
            <div class=""related-posts""><a href=""/other"">Other post</a></div>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Main article");
        result.Should().NotContain("Other post");
    }

    #endregion

    #region URL 절대경로 변환

    [Fact]
    public async Task CleanAsync_ShouldConvertRelativeLinks()
    {
        // Arrange
        var html = @"<html><body>
            <a href=""/about"">About</a>
        </body></html>";
        var sourceUrl = "https://example.com/page";

        // Act
        var result = await _cleaner.CleanAsync(html, sourceUrl);

        // Assert
        result.Should().Contain("https://example.com/about");
    }

    [Fact]
    public async Task CleanAsync_ShouldConvertRelativeImageSrc()
    {
        // Arrange
        var html = @"<html><body>
            <img src=""/images/photo.jpg"" alt=""Photo""/>
        </body></html>";
        var sourceUrl = "https://example.com/page";

        // Act
        var result = await _cleaner.CleanAsync(html, sourceUrl);

        // Assert
        result.Should().Contain("https://example.com/images/photo.jpg");
    }

    [Fact]
    public async Task CleanAsync_ShouldKeepAbsoluteLinks()
    {
        // Arrange
        var html = @"<html><body>
            <a href=""https://other.com/page"">External</a>
        </body></html>";
        var sourceUrl = "https://example.com";

        // Act
        var result = await _cleaner.CleanAsync(html, sourceUrl);

        // Assert
        result.Should().Contain("https://other.com/page");
    }

    [Fact]
    public async Task CleanAsync_WithDisabledUrlConversion_ShouldKeepRelativeUrls()
    {
        // Arrange
        var html = @"<html><body>
            <a href=""/about"">About</a>
        </body></html>";
        var options = new HtmlCleaningOptions { ConvertRelativeUrls = false };

        // Act
        var result = await _cleaner.CleanAsync(html, "https://example.com", options);

        // Assert
        result.Should().Contain("/about");
    }

    #endregion

    #region srcset 최적화

    [Fact]
    public async Task CleanAsync_ShouldOptimizeSrcset()
    {
        // Arrange
        var html = @"<html><body>
            <img srcset=""small.jpg 400w, medium.jpg 800w, large.jpg 1200w"" src=""small.jpg"" alt=""Test""/>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        // 최적 이미지 (가장 큰 해상도)가 src에 설정되어야 함
        result.Should().Contain("large.jpg");
        result.Should().NotContain("srcset");
    }

    #endregion

    #region KeepSelectors

    [Fact]
    public async Task CleanAsync_WithKeepSelectors_ShouldPreserveMarkedElements()
    {
        // Arrange
        var html = @"<html><body>
            <nav id=""toc""><a href=""#section1"">Section 1</a></nav>
            <p>Content</p>
        </body></html>";
        var options = new HtmlCleaningOptions
        {
            OnlyMainContent = true,
            KeepSelectors = new List<string> { "#toc" }
        };

        // Act
        var result = await _cleaner.CleanAsync(html, options: options);

        // Assert
        result.Should().Contain("Section 1");
    }

    #endregion

    #region AdditionalRemoveSelectors

    [Fact]
    public async Task CleanAsync_WithAdditionalSelectors_ShouldRemoveCustomElements()
    {
        // Arrange
        var html = @"<html><body>
            <p>Content</p>
            <div class=""custom-noise"">Noise</div>
        </body></html>";
        var options = new HtmlCleaningOptions
        {
            AdditionalRemoveSelectors = new List<string> { ".custom-noise" }
        };

        // Act
        var result = await _cleaner.CleanAsync(html, options: options);

        // Assert
        result.Should().Contain("Content");
        result.Should().NotContain("Noise");
    }

    #endregion

    #region 엣지 케이스

    [Fact]
    public async Task CleanAsync_WithEmptyInput_ShouldReturnEmpty()
    {
        // Act
        var result = await _cleaner.CleanAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanAsync_WithWhitespace_ShouldReturnEmpty()
    {
        // Act
        var result = await _cleaner.CleanAsync("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanAsync_ShouldRemoveHtmlComments()
    {
        // Arrange
        var html = @"<html><body>
            <!-- This is a comment -->
            <p>Content</p>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("Content");
        result.Should().NotContain("This is a comment");
    }

    [Fact]
    public async Task CleanAsync_ShouldPreserveMainContentStructure()
    {
        // Arrange
        var html = @"<html><body>
            <article>
                <h1>Title</h1>
                <p>Paragraph 1</p>
                <p>Paragraph 2</p>
            </article>
        </body></html>";

        // Act
        var result = await _cleaner.CleanAsync(html);

        // Assert
        result.Should().Contain("<h1>");
        result.Should().Contain("Title");
        result.Should().Contain("Paragraph 1");
        result.Should().Contain("Paragraph 2");
    }

    #endregion
}
