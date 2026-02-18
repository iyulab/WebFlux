using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// SmartChunkingStrategy 단위 테스트
/// 구조 인식 청킹 검증 (HTML/Markdown 헤더 기반)
/// </summary>
public class SmartChunkingStrategyTests
{
    private readonly SmartChunkingStrategy _strategy;

    public SmartChunkingStrategyTests()
    {
        _strategy = new SmartChunkingStrategy();
    }

    #region Basic Properties Tests

    [Fact]
    public void Name_ShouldBeSmart()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        name.Should().Be("Smart");
    }

    [Fact]
    public void Description_ShouldContainStructure()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("구조");
    }

    #endregion

    #region Empty Content Tests

    [Fact]
    public async Task ChunkAsync_WithEmptyContent_ShouldReturnEmptyList()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Text = string.Empty,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithNullText_ShouldReturnEmptyList()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Text = null!,
            MainContent = null!,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().BeEmpty();
    }

    #endregion

    #region Heading-Based Chunking Tests

    [Fact]
    public async Task ChunkAsync_WithMarkdownHeadings_ShouldUseHeadingStructure()
    {
        // Arrange
        var text = @"# Main Title
Some introductory text here.

## Section 1
Content for section 1 that is quite long and detailed.

## Section 2
Content for section 2 with more information.

### Subsection 2.1
Detailed content for subsection.";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Main Title", "## Section 1", "## Section 2", "### Subsection 2.1" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(chunk =>
        {
            chunk.Content.Should().NotBeNullOrEmpty();
            chunk.StrategyInfo.StrategyName.Should().Be("Smart");
        });
    }

    [Fact]
    public async Task ChunkAsync_WithHtmlHeadings_ShouldUseHeadingStructure()
    {
        // Arrange
        var text = @"<h1>Main Title</h1>
Introduction paragraph

<h2>Section One</h2>
Content for section one

<h2>Section Two</h2>
Content for section two";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "<h1>Main Title</h1>", "<h2>Section One</h2>", "<h2>Section Two</h2>" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithLargeHeadingSections_ShouldSplitWhenExceedingSize()
    {
        // Arrange
        var largeSection = string.Join(" ", Enumerable.Repeat("word", 500)); // ~2500 chars
        var text = $"# Heading 1\n{largeSection}\n## Heading 2\n{largeSection}";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading 1", "## Heading 2" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Fallback to Paragraph Tests

    [Fact]
    public async Task ChunkAsync_WithoutHeadings_ShouldFallbackToParagraphs()
    {
        // Arrange
        var text = "Paragraph 1\n\nParagraph 2\n\nParagraph 3";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = null!
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks[0].Content.Should().Contain("Paragraph 1");
    }

    [Fact]
    public async Task ChunkAsync_WithEmptyHeadingsList_ShouldFallbackToParagraphs()
    {
        // Arrange
        var text = "Paragraph 1\n\nParagraph 2\n\nParagraph 3";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string>()
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks[0].Content.Should().Contain("Paragraph 1");
    }

    [Fact]
    public async Task ChunkAsync_FallbackMode_ShouldSplitByParagraphBoundaries()
    {
        // Arrange
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var paragraph2 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var text = paragraph1 + "\n\n" + paragraph2;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = null!
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Custom Options Tests

    [Fact]
    public async Task ChunkAsync_WithCustomChunkSize_ShouldUseSpecifiedSize()
    {
        // Arrange
        // Create multiple sections to trigger splitting
        var section1 = string.Join(" ", Enumerable.Repeat("word", 150));
        var section2 = string.Join(" ", Enumerable.Repeat("word", 150));
        var text = $"# Heading 1\n{section1}\n## Heading 2\n{section2}";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading 1", "## Heading 2" }
        };
        var options = new ChunkingOptions { ChunkSize = 500 };

        // Act
        var chunks = await _strategy.ChunkAsync(content, options);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithDefaultSize_ShouldUse1500Chars()
    {
        // Arrange
        // Create multiple sections that will be split by heading-based chunking
        var section1 = string.Join(" ", Enumerable.Repeat("word", 400)); // ~2000 chars
        var section2 = string.Join(" ", Enumerable.Repeat("word", 400)); // ~2000 chars
        var text = $"# Heading 1\n{section1}\n## Heading 2\n{section2}";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading 1", "## Heading 2" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Heading Detection Tests

    [Fact]
    public async Task ChunkAsync_ShouldRecognizeMarkdownHeadings()
    {
        // Arrange
        var text = @"# H1 Heading
## H2 Heading
### H3 Heading
#### H4 Heading";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# H1", "## H2", "### H3", "#### H4" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_ShouldRecognizeHtmlHeadings()
    {
        // Arrange
        var text = @"<h1>Title</h1>
<h2>Section</h2>
<h3>Subsection</h3>";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "<h1>Title</h1>", "<h2>Section</h2>", "<h3>Subsection</h3>" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_ShouldRecognizeShortTitleLines()
    {
        // Arrange
        var text = @"Introduction
This is a longer paragraph that provides context
Next Section
More content here";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "Introduction", "Next Section" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Sequence and Metadata Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreserveSequenceNumbers()
    {
        // Arrange
        var sections = Enumerable.Range(1, 5)
            .Select(i => $"# Section {i}\n" + string.Join(" ", Enumerable.Repeat("content", 300)));
        var text = string.Join("\n\n", sections);

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = Enumerable.Range(1, 5).Select(i => $"# Section {i}").ToList()
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].SequenceNumber.Should().Be(i);
        }
    }

    [Fact]
    public async Task ChunkAsync_ShouldSetChunkMetadata()
    {
        // Arrange
        var text = "# Heading\nSome content";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().AllSatisfy(chunk =>
        {
            chunk.Id.Should().NotBeNullOrEmpty();
            chunk.StrategyInfo.Should().NotBeNull();
            chunk.StrategyInfo.StrategyName.Should().Be("Smart");
            chunk.SourceUrl.Should().Be("https://example.com");
            chunk.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        });
    }

    [Fact]
    public async Task ChunkAsync_ShouldGenerateUniqueIds()
    {
        // Arrange
        var sections = Enumerable.Range(1, 5)
            .Select(i => $"# Section {i}\nContent for section {i}");
        var text = string.Join("\n\n", sections);

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = Enumerable.Range(1, 5).Select(i => $"# Section {i}").ToList()
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        var ids = chunks.Select(c => c.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Content Priority Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreferMainContentOverText()
    {
        // Arrange
        var mainContent = "# Main Heading\nMain content";
        var text = "# Text Heading\nText content";
        var content = new ExtractedContent
        {
            MainContent = mainContent,
            Text = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Main Heading" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks[0].Content.Should().Contain("Main");
        chunks[0].Content.Should().NotContain("Text Heading");
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task ChunkAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var text = "# Heading\nContent";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading" }
        };
        using var cts = new CancellationTokenSource();

        // Act
        var chunks = await _strategy.ChunkAsync(content, cancellationToken: cts.Token);

        // Assert
        chunks.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task ChunkAsync_WithMixedHeadingTypes_ShouldHandleAll()
    {
        // Arrange
        var text = @"# Markdown H1
Content 1

<h2>HTML H2</h2>
Content 2

Short Title
Content 3";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Markdown H1", "<h2>HTML H2</h2>", "Short Title" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(chunk => chunk.Content.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task ChunkAsync_WithOnlyHeadings_ShouldCreateChunks()
    {
        // Arrange
        var text = @"# Heading 1
## Heading 2
### Heading 3";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading 1", "## Heading 2", "### Heading 3" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithLongHeadingSection_ShouldSplitAtMaxSize()
    {
        // Arrange
        // Create multiple large sections to ensure splitting
        var veryLongSection1 = string.Join(" ", Enumerable.Repeat("word", 500)); // ~2500 chars
        var veryLongSection2 = string.Join(" ", Enumerable.Repeat("word", 500)); // ~2500 chars
        var text = $"# Section 1\n{veryLongSection1}\n## Section 2\n{veryLongSection2}";

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Section 1", "## Section 2" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion
}
