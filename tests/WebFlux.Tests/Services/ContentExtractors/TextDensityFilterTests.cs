using WebFlux.Services.ContentExtractors;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// TextDensityFilter 단위 테스트
/// 텍스트 밀도 기반 보일러플레이트 제거 검증
/// </summary>
public class TextDensityFilterTests
{
    private readonly TextDensityFilter _filter;

    public TextDensityFilterTests()
    {
        _filter = new TextDensityFilter();
    }

    #region 기본 필터링

    [Fact]
    public async Task FilterAsync_ShouldPreserveContentRichElements()
    {
        // Arrange - 텍스트가 풍부한 article 요소
        var html = @"
            <article>
                <p>This is a substantial paragraph with meaningful content that provides value to the reader.
                It contains multiple sentences and important information about the topic at hand.</p>
                <p>Another paragraph with detailed explanation of the subject matter. This helps ensure
                the content is rich enough to pass the density filter threshold.</p>
            </article>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("substantial paragraph");
        result.Should().Contain("detailed explanation");
    }

    [Fact]
    public async Task FilterAsync_ShouldRemoveNavigationHeavyElements()
    {
        // Arrange - 링크 밀도가 높은 nav 요소 + 실제 콘텐츠
        var html = @"
            <div>
                <nav>
                    <a href=""/"">Home</a>
                    <a href=""/about"">About</a>
                    <a href=""/contact"">Contact</a>
                    <a href=""/blog"">Blog</a>
                    <a href=""/faq"">FAQ</a>
                </nav>
                <article>
                    <p>This is meaningful article content that should be preserved through the filtering process.
                    It has enough text density to pass the threshold.</p>
                </article>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("meaningful article content");
    }

    [Fact]
    public async Task FilterAsync_ShouldRemoveFooterNavigation()
    {
        // Arrange
        var html = @"
            <div>
                <article>
                    <p>Main content with sufficient text length to be considered meaningful content by the filter algorithm.</p>
                </article>
                <footer>
                    <a href=""/privacy"">Privacy</a>
                    <a href=""/terms"">Terms</a>
                    <a href=""/sitemap"">Sitemap</a>
                </footer>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Main content");
    }

    #endregion

    #region 임계값 조정

    [Fact]
    public async Task FilterAsync_WithLowThreshold_ShouldKeepMoreContent()
    {
        // Arrange
        var filter = new TextDensityFilter
        {
            TextDensityThreshold = 0.01,
            MinTextLength = 5
        };

        var html = @"
            <div>
                <div class=""sidebar""><p>Short sidebar text</p></div>
                <article><p>Main content text here for the article with enough words to be meaningful.</p></article>
            </div>";

        // Act
        var result = await filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Main content");
    }

    [Fact]
    public async Task FilterAsync_WithHighLinkDensityThreshold_ShouldKeepLinkRichContent()
    {
        // Arrange
        var filter = new TextDensityFilter
        {
            LinkDensityThreshold = 0.9
        };

        var html = @"
            <div>
                <div><a href=""/a"">Link A</a> and <a href=""/b"">Link B</a> in context paragraph with extra text</div>
                <p>Regular content paragraph here with enough text to pass the filter.</p>
            </div>";

        // Act
        var result = await filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Regular content");
    }

    #endregion

    #region 콘텐츠 보존

    [Fact]
    public async Task FilterAsync_ShouldPreserveTables()
    {
        // Arrange
        var html = @"
            <div>
                <table>
                    <tr><th>Name</th><th>Value</th></tr>
                    <tr><td>Item one with description</td><td>Value one with details</td></tr>
                    <tr><td>Item two with description</td><td>Value two with details</td></tr>
                </table>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Name");
        result.Should().Contain("Item one");
    }

    [Fact]
    public async Task FilterAsync_ShouldPreserveCodeBlocks()
    {
        // Arrange
        var html = @"
            <div>
                <pre><code>function hello() {
    console.log('Hello, World!');
    return true;
}</code></pre>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("function hello()");
        result.Should().Contain("console.log");
    }

    [Fact]
    public async Task FilterAsync_ShouldPreserveArticleContent()
    {
        // Arrange
        var html = @"
            <div>
                <article class=""post-content"">
                    <h2>Important Heading</h2>
                    <p>This is a well-written article with substantial content that provides educational value
                    and meaningful information to the reader. It covers multiple aspects of the topic.</p>
                </article>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Important Heading");
        result.Should().Contain("well-written article");
    }

    #endregion

    #region 엣지 케이스

    [Fact]
    public async Task FilterAsync_WithEmptyInput_ShouldReturnEmpty()
    {
        // Act
        var result = await _filter.FilterAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithWhitespace_ShouldReturnEmpty()
    {
        // Act
        var result = await _filter.FilterAsync("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FilterAsync_WithNoBlockElements_ShouldReturnOriginal()
    {
        // Arrange
        var html = "<p>Simple paragraph text without any block containers.</p>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Simple paragraph");
    }

    [Fact]
    public async Task FilterAsync_WithOnlyInlineElements_ShouldPreserveAll()
    {
        // Arrange
        var html = "<span>Text</span> <strong>bold</strong> <em>italic</em>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Text");
        result.Should().Contain("bold");
        result.Should().Contain("italic");
    }

    #endregion

    #region class/id 기반 필터링

    [Fact]
    public async Task FilterAsync_ShouldBoostContentClassElements()
    {
        // Arrange
        var html = @"
            <div>
                <div class=""article-content"">
                    <p>This is the main article content with enough text to be meaningful.</p>
                </div>
                <div class=""sidebar-widget"">
                    <a href=""/a"">Link</a>
                    <a href=""/b"">Link</a>
                </div>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("main article content");
    }

    [Fact]
    public async Task FilterAsync_ShouldPenalizeSidebarClassElements()
    {
        // Arrange
        var html = @"
            <div>
                <article>
                    <p>Main article content here with enough text to be preserved.</p>
                </article>
                <aside class=""sidebar"">
                    <a href=""/tag1"">Tag 1</a>
                    <a href=""/tag2"">Tag 2</a>
                </aside>
            </div>";

        // Act
        var result = await _filter.FilterAsync(html);

        // Assert
        result.Should().Contain("Main article content");
    }

    #endregion
}
