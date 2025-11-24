using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// FixedSizeChunkingStrategy 단위 테스트
/// 고정 크기 기반 청킹 검증
/// </summary>
public class FixedSizeChunkingStrategyTests
{
    private readonly FixedSizeChunkingStrategy _strategy;

    public FixedSizeChunkingStrategyTests()
    {
        _strategy = new FixedSizeChunkingStrategy();
    }

    #region Basic Properties Tests

    [Fact]
    public void Name_ShouldBeFixedSize()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        name.Should().Be("FixedSize");
    }

    [Fact]
    public void Description_ShouldContainFixedSize()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("고정 크기");
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

    [Fact]
    public async Task ChunkAsync_WithWhitespaceOnly_ShouldReturnEmptyList()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Text = "   \t\n  ",
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().BeEmpty();
    }

    #endregion

    #region Single Chunk Tests

    [Fact]
    public async Task ChunkAsync_WithShortText_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "This is a short text that should fit in one chunk.";
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
        chunks[0].SourceUrl.Should().Be("https://example.com");
    }

    [Fact]
    public async Task ChunkAsync_WithDefaultSize_ShouldUse1000Chars()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("word", 500)); // ~2500 chars
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.All(c => c.Content.Length <= 1200).Should().BeTrue(); // 1000 + some buffer for word boundaries
    }

    #endregion

    #region Multiple Chunks Tests

    [Fact]
    public async Task ChunkAsync_WithLongText_ShouldSplitIntoMultipleChunks()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("word", 500)); // ~2500 chars
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
        var text = string.Join(" ", Enumerable.Repeat("word", 500));
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

    [Fact]
    public async Task ChunkAsync_ShouldSplitOnWordBoundaries()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("testword", 300)); // Ensure splits
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
            // Each chunk should start and end with complete words (no partial words)
            chunk.Content.Should().NotStartWith(" ");
            chunk.Content.Should().NotEndWith(" ");
        });
    }

    #endregion

    #region Custom Options Tests

    [Fact]
    public async Task ChunkAsync_WithCustomChunkSize_ShouldUseSpecifiedSize()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("word", 200));
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var options = new ChunkingOptions { ChunkSize = 200 };

        // Act
        var chunks = await _strategy.ChunkAsync(content, options);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.All(c => c.Content.Length <= 250).Should().BeTrue(); // 200 + buffer
    }

    [Fact]
    public async Task ChunkAsync_WithLargeChunkSize_ShouldCreateFewerChunks()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("word", 300));
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var smallOptions = new ChunkingOptions { ChunkSize = 200 };
        var largeOptions = new ChunkingOptions { ChunkSize = 2000 };

        // Act
        var smallChunks = await _strategy.ChunkAsync(content, smallOptions);
        var largeChunks = await _strategy.ChunkAsync(content, largeOptions);

        // Assert
        largeChunks.Count.Should().BeLessThan(smallChunks.Count);
    }

    #endregion

    #region Content Priority Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreferMainContentOverText()
    {
        // Arrange
        var mainContent = "This is main content";
        var text = "This is regular text";
        var content = new ExtractedContent
        {
            MainContent = mainContent,
            Text = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(mainContent);
    }

    [Fact]
    public async Task ChunkAsync_ShouldUseTextWhenMainContentIsNull()
    {
        // Arrange
        var text = "This is regular text";
        var content = new ExtractedContent
        {
            MainContent = null,
            Text = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(text);
    }

    #endregion

    #region URL Handling Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreserveSourceUrl()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test content",
            Url = "https://example.com/page"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.All(c => c.SourceUrl == "https://example.com/page").Should().BeTrue();
    }

    [Fact]
    public async Task ChunkAsync_WithNullUrl_ShouldUseEmptyString()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test content",
            Url = null,
            OriginalUrl = null
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.All(c => c.SourceUrl == string.Empty).Should().BeTrue();
    }

    [Fact]
    public async Task ChunkAsync_ShouldPreferUrlOverOriginalUrl()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test content",
            Url = "https://example.com/current",
            OriginalUrl = "https://example.com/original"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.All(c => c.SourceUrl == "https://example.com/current").Should().BeTrue();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task ChunkAsync_ShouldSetChunkMetadata()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test content",
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().AllSatisfy(chunk =>
        {
            chunk.Id.Should().NotBeNullOrEmpty();
            chunk.StrategyInfo.Should().NotBeNull();
            chunk.StrategyInfo.StrategyName.Should().Be("FixedSize");
            chunk.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        });
    }

    [Fact]
    public async Task ChunkAsync_ShouldGenerateUniqueIds()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("word", 500));
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
        var content = new ExtractedContent
        {
            MainContent = "Test content",
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
    public async Task ChunkAsync_WithSingleLongWord_ShouldCreateChunk()
    {
        // Arrange
        var longWord = new string('a', 2000);
        var content = new ExtractedContent
        {
            MainContent = longWord,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(longWord);
    }

    [Fact]
    public async Task ChunkAsync_WithMultipleLongWords_ShouldSplitCorrectly()
    {
        // Arrange
        var longWords = string.Join(" ", Enumerable.Repeat(new string('a', 500), 10));
        var content = new ExtractedContent
        {
            MainContent = longWords,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().AllSatisfy(chunk => chunk.Content.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task ChunkAsync_WithExactChunkSizeText_ShouldCreateSingleChunk()
    {
        // Arrange
        var text = new string('a', 1000);
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
    }

    #endregion
}
