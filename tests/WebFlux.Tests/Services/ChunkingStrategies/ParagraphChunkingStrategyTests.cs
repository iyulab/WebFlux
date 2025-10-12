using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// ParagraphChunkingStrategy 단위 테스트
/// 문단 경계 기반 청킹 검증
/// </summary>
public class ParagraphChunkingStrategyTests
{
    private readonly ParagraphChunkingStrategy _strategy;

    public ParagraphChunkingStrategyTests()
    {
        _strategy = new ParagraphChunkingStrategy();
    }

    #region Basic Properties Tests

    [Fact]
    public void Name_ShouldBeParagraph()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        name.Should().Be("Paragraph");
    }

    [Fact]
    public void Description_ShouldContainParagraph()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("문단");
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
            Text = null,
            MainContent = null,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().BeEmpty();
    }

    #endregion

    #region Single Paragraph Tests

    [Fact]
    public async Task ChunkAsync_WithSingleParagraph_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "This is a single paragraph without any breaks.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(text);
        chunks[0].SequenceNumber.Should().Be(0);
    }

    [Fact]
    public async Task ChunkAsync_WithDefaultSize_ShouldUse2000Chars()
    {
        // Arrange
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var paragraph2 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var text = paragraph1 + "\n\n" + paragraph2;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        // Should split because each paragraph is ~1500 chars and max is 2000
        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Multiple Paragraphs Tests

    [Fact]
    public async Task ChunkAsync_WithMultipleParagraphs_ShouldSplitByParagraphBoundaries()
    {
        // Arrange
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1); // All fit in default 2000 char limit
        chunks[0].Content.Should().Contain("First paragraph");
        chunks[0].Content.Should().Contain("Second paragraph");
        chunks[0].Content.Should().Contain("Third paragraph");
    }

    [Fact]
    public async Task ChunkAsync_ShouldPreserveParagraphSeparators()
    {
        // Arrange
        var paragraph1 = "First paragraph.";
        var paragraph2 = "Second paragraph.";
        var text = paragraph1 + "\n\n" + paragraph2;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("\n\n");
    }

    [Fact]
    public async Task ChunkAsync_WithWindowsLineEndings_ShouldRecognizeParagraphs()
    {
        // Arrange
        var text = "First paragraph.\r\n\r\nSecond paragraph.\r\n\r\nThird paragraph.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("First paragraph");
        chunks[0].Content.Should().Contain("Second paragraph");
    }

    #endregion

    #region Large Content Tests

    [Fact]
    public async Task ChunkAsync_WithLongParagraphs_ShouldSplitWhenExceedingMaxSize()
    {
        // Arrange
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var paragraph2 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var paragraph3 = string.Join(" ", Enumerable.Repeat("word", 300)); // ~1500 chars
        var text = paragraph1 + "\n\n" + paragraph2 + "\n\n" + paragraph3;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().AllSatisfy(chunk =>
        {
            chunk.Content.Should().NotBeNullOrEmpty();
            chunk.SourceUrl.Should().Be("https://example.com");
        });
    }

    [Fact]
    public async Task ChunkAsync_ShouldPreserveSequenceNumbers()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 10)
            .Select(i => string.Join(" ", Enumerable.Repeat($"paragraph{i}", 200)));
        var text = string.Join("\n\n", paragraphs);
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].SequenceNumber.Should().Be(i);
        }
    }

    #endregion

    #region Custom Options Tests

    [Fact]
    public async Task ChunkAsync_WithCustomChunkSize_ShouldUseSpecifiedSize()
    {
        // Arrange
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 100)); // ~500 chars
        var paragraph2 = string.Join(" ", Enumerable.Repeat("word", 100)); // ~500 chars
        var text = paragraph1 + "\n\n" + paragraph2;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var options = new ChunkingOptions { ChunkSize = 600 };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1); // Both paragraphs fit in default 2000

        // With smaller size
        chunks = await _strategy.ChunkAsync(content, options);
        chunks.Should().HaveCountGreaterThan(1); // Should split with 600 char limit
    }

    [Fact]
    public async Task ChunkAsync_WithSmallChunkSize_ShouldCreateMoreChunks()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 5)
            .Select(i => string.Join(" ", Enumerable.Repeat("word", 100)));
        var text = string.Join("\n\n", paragraphs);
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var smallOptions = new ChunkingOptions { ChunkSize = 300 };
        var largeOptions = new ChunkingOptions { ChunkSize = 3000 };

        // Act
        var smallChunks = await _strategy.ChunkAsync(content, smallOptions);
        var largeChunks = await _strategy.ChunkAsync(content, largeOptions);

        // Assert
        smallChunks.Count.Should().BeGreaterThan(largeChunks.Count);
    }

    #endregion

    #region Paragraph Boundary Tests

    [Fact]
    public async Task ChunkAsync_ShouldTrimParagraphs()
    {
        // Arrange
        var text = "  First paragraph  \n\n  Second paragraph  ";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().NotStartWith(" ");
        chunks[0].Content.Should().NotEndWith(" ");
    }

    [Fact]
    public async Task ChunkAsync_WithSingleLineBreaks_ShouldNotSplit()
    {
        // Arrange
        var text = "First line\nSecond line\nThird line";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(text);
    }

    [Fact]
    public async Task ChunkAsync_WithMixedLineEndings_ShouldHandleCorrectly()
    {
        // Arrange
        var text = "Para 1\n\nPara 2\r\n\r\nPara 3";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("Para 1");
        chunks[0].Content.Should().Contain("Para 2");
        chunks[0].Content.Should().Contain("Para 3");
    }

    #endregion

    #region Content Priority Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreferMainContentOverText()
    {
        // Arrange
        var mainContent = "Main paragraph 1\n\nMain paragraph 2";
        var text = "Text paragraph 1\n\nText paragraph 2";
        var content = new ExtractedContent
        {
            MainContent = mainContent,
            Text = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks[0].Content.Should().Contain("Main paragraph");
        chunks[0].Content.Should().NotContain("Text paragraph");
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task ChunkAsync_ShouldSetChunkMetadata()
    {
        // Arrange
        var text = "Paragraph 1\n\nParagraph 2";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().AllSatisfy(chunk =>
        {
            chunk.Id.Should().NotBeNullOrEmpty();
            chunk.StrategyInfo.Should().NotBeNull();
            chunk.StrategyInfo.StrategyName.Should().Be("Paragraph");
            chunk.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        });
    }

    [Fact]
    public async Task ChunkAsync_ShouldGenerateUniqueIds()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 10)
            .Select(i => string.Join(" ", Enumerable.Repeat($"para{i}", 100)));
        var text = string.Join("\n\n", paragraphs);
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        var ids = chunks.Select(c => c.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task ChunkAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var text = "Paragraph 1\n\nParagraph 2";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
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
    public async Task ChunkAsync_WithSingleLongParagraph_ShouldCreateSingleChunk()
    {
        // Arrange
        var longParagraph = string.Join(" ", Enumerable.Repeat("word", 500));
        var content = new ExtractedContent
        {
            MainContent = longParagraph,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public async Task ChunkAsync_WithEmptyParagraphs_ShouldSkipThem()
    {
        // Arrange
        var text = "Paragraph 1\n\n\n\nParagraph 2";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("Paragraph 1");
        chunks[0].Content.Should().Contain("Paragraph 2");
    }

    [Fact]
    public async Task ChunkAsync_WithMultipleConsecutiveParagraphBreaks_ShouldHandleCorrectly()
    {
        // Arrange
        var text = "Para 1\n\n\n\n\n\nPara 2\n\n\n\nPara 3";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("Para 1");
        chunks[0].Content.Should().Contain("Para 2");
        chunks[0].Content.Should().Contain("Para 3");
    }

    #endregion
}
