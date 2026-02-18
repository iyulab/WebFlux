using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// MemoryOptimizedChunkingStrategy 단위 테스트
/// 메모리 효율적 청킹 검증
/// </summary>
public class MemoryOptimizedChunkingStrategyTests
{
    private readonly MemoryOptimizedChunkingStrategy _strategy;

    public MemoryOptimizedChunkingStrategyTests()
    {
        _strategy = new MemoryOptimizedChunkingStrategy();
    }

    #region Basic Properties Tests

    [Fact]
    public void Name_ShouldBeMemoryOptimized()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        name.Should().Be("MemoryOptimized");
    }

    [Fact]
    public void Description_ShouldContainMemory()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("메모리");
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

    #region Streaming Chunking Tests

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
    }

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
    public async Task ChunkAsync_ShouldSplitOnWordBoundaries()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("testword", 300));
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
            // Each chunk should be trimmed and not have leading/trailing spaces
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
        var text = string.Join(" ", Enumerable.Repeat("word", 300));
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var options = new ChunkingOptions { ChunkSize = 500 };

        // Act
        var chunks = await _strategy.ChunkAsync(content, options);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.All(c => c.Content.Length <= 600).Should().BeTrue(); // 500 + buffer for word boundaries
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
        chunks.All(c => c.Content.Length <= 1200).Should().BeTrue(); // 1000 + buffer
    }

    [Fact]
    public async Task ChunkAsync_WithSmallChunkSize_ShouldCreateMoreChunks()
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
        smallChunks.Count.Should().BeGreaterThan(largeChunks.Count);
    }

    #endregion

    #region Sequence and Metadata Tests

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
    public async Task ChunkAsync_ShouldSetChunkMetadata()
    {
        // Arrange
        var text = "Test content for memory optimized chunking";
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
            chunk.StrategyInfo.StrategyName.Should().Be("MemoryOptimized");
            chunk.SourceUrl.Should().Be("https://example.com");
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

    #region Content Priority Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreferMainContentOverText()
    {
        // Arrange
        var mainContent = string.Join(" ", Enumerable.Repeat("main", 100));
        var text = string.Join(" ", Enumerable.Repeat("text", 100));
        var content = new ExtractedContent
        {
            MainContent = mainContent,
            Text = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks[0].Content.Should().Contain("main");
        chunks[0].Content.Should().NotContain("text");
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
            Url = null!,
            OriginalUrl = null!
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.All(c => c.SourceUrl == string.Empty).Should().BeTrue();
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task ChunkAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var text = "Test content";
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
    public async Task ChunkAsync_WithVeryLongText_ShouldHandleEfficiently()
    {
        // Arrange
        var veryLongText = string.Join(" ", Enumerable.Repeat("word", 2000)); // ~10000 chars
        var content = new ExtractedContent
        {
            MainContent = veryLongText,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(5);
        chunks.Should().AllSatisfy(chunk => chunk.Content.Should().NotBeNullOrEmpty());
    }

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
        chunks.Should().HaveCountGreaterThan(1);
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

    [Fact]
    public async Task ChunkAsync_ShouldTrimWhitespace()
    {
        // Arrange
        var text = "   " + string.Join(" ", Enumerable.Repeat("word", 500)) + "   ";
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
            chunk.Content.Should().NotStartWith(" ");
            chunk.Content.Should().NotEndWith(" ");
        });
    }

    #endregion
}
