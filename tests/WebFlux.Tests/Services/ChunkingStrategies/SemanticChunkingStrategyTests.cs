using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Core.Interfaces;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// SemanticChunkingStrategy 단위 테스트
/// 의미론적 청킹 검증 (임베딩 서비스 없이 폴백 모드)
/// </summary>
public class SemanticChunkingStrategyTests
{
    private readonly SemanticChunkingStrategy _strategy;

    public SemanticChunkingStrategyTests()
    {
        _strategy = new SemanticChunkingStrategy(); // No embedding service - fallback mode
    }

    #region Basic Properties Tests

    [Fact]
    public void Name_ShouldBeSemantic()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        name.Should().Be("Semantic");
    }

    [Fact]
    public void Description_ShouldContainSemantic()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("의미");
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

    #region Fallback Mode Tests (Without Embedding Service)

    [Fact]
    public async Task ChunkAsync_WithoutEmbeddingService_ShouldFallbackToParagraphChunking()
    {
        // Arrange
        var text = "Paragraph 1\n\nParagraph 2\n\nParagraph 3";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks[0].StrategyInfo.StrategyName.Should().Be("Semantic");
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
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ChunkAsync_FallbackMode_WithShortText_ShouldReturnSingleChunk()
    {
        // Arrange
        var text = "This is a short text that fits in one chunk.";
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

    #endregion

    #region Custom Options Tests

    [Fact]
    public async Task ChunkAsync_WithCustomChunkSize_ShouldUseSpecifiedSize()
    {
        // Arrange
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 200));
        var paragraph2 = string.Join(" ", Enumerable.Repeat("word", 200));
        var text = paragraph1 + "\n\n" + paragraph2;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var options = new ChunkingOptions { ChunkSize = 600 };

        // Act
        var chunks = await _strategy.ChunkAsync(content, options);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ChunkAsync_WithDefaultSize_ShouldUse1500Chars()
    {
        // Arrange
        var paragraph1 = string.Join(" ", Enumerable.Repeat("word", 300));
        var paragraph2 = string.Join(" ", Enumerable.Repeat("word", 300));
        var text = paragraph1 + "\n\n" + paragraph2;
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Sequence and Metadata Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreserveSequenceNumbers()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 5)
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

    [Fact]
    public async Task ChunkAsync_ShouldSetChunkMetadata()
    {
        // Arrange
        var text = "Test paragraph 1\n\nTest paragraph 2";
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
            chunk.StrategyInfo.StrategyName.Should().Be("Semantic");
            chunk.SourceUrl.Should().Be("https://example.com");
            chunk.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        });
    }

    [Fact]
    public async Task ChunkAsync_ShouldGenerateUniqueIds()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 5)
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

    #region URL Handling Tests

    [Fact]
    public async Task ChunkAsync_ShouldPreserveSourceUrl()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test paragraph 1\n\nTest paragraph 2",
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
        var text = "Test paragraph 1\n\nTest paragraph 2";
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
        var longParagraph = string.Join(" ", Enumerable.Repeat("word", 300));
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
    public async Task ChunkAsync_WithWindowsLineEndings_ShouldHandleCorrectly()
    {
        // Arrange
        var text = "Para 1\r\n\r\nPara 2\r\n\r\nPara 3";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithMixedLineEndings_ShouldSplit()
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
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithEmbeddingService_ShouldStoreService()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();

        // Act
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("Semantic");
    }

    [Fact]
    public void Constructor_WithEventPublisher_ShouldStorePublisher()
    {
        // Arrange
        var mockEventPublisher = Substitute.For<IEventPublisher>();

        // Act
        var strategy = new SemanticChunkingStrategy(mockEventPublisher);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("Semantic");
    }

    [Fact]
    public void Constructor_WithBothServices_ShouldStoreBoth()
    {
        // Arrange
        var mockEventPublisher = Substitute.For<IEventPublisher>();
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();

        // Act
        var strategy = new SemanticChunkingStrategy(mockEventPublisher, mockEmbeddingService);

        // Assert
        strategy.Should().NotBeNull();
    }

    #endregion

    #region Semantic Mode Tests (With Embedding Service)

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldUseSentenceBasedChunking()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "Sentence one. Sentence two! Sentence three?";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("Sentence one");
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldSplitBySentencePunctuation()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        // Create long sentences that will exceed chunk size
        var sentence1 = string.Join(" ", Enumerable.Repeat("word", 300)) + ".";
        var sentence2 = string.Join(" ", Enumerable.Repeat("word", 300)) + "!";
        var sentence3 = string.Join(" ", Enumerable.Repeat("word", 300)) + "?";
        var text = sentence1 + " " + sentence2 + " " + sentence3;

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldHandlePeriod()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "First sentence. Second sentence. Third sentence.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().EndWith(".");
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldHandleExclamation()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "Exciting sentence! Another exciting one! Last one!";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        // Exclamation marks are converted to periods in the implementation
        chunks[0].Content.Should().Contain("Exciting sentence");
        chunks[0].Content.Should().EndWith(".");
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldHandleQuestion()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "Is this working? Does it handle questions? What about this?";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        // Question marks are converted to periods in the implementation
        chunks[0].Content.Should().Contain("Is this working");
        chunks[0].Content.Should().EndWith(".");
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldFilterEmptySentences()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "Valid sentence. . . Another valid one.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().NotContain(". . .");
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldRespectMaxChunkSize()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var sentences = Enumerable.Range(1, 20)
            .Select(i => string.Join(" ", Enumerable.Repeat($"word{i}", 100)) + ".");
        var text = string.Join(" ", sentences);

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };
        var options = new ChunkingOptions { ChunkSize = 800 };

        // Act
        var chunks = await strategy.ChunkAsync(content, options);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().AllSatisfy(chunk =>
            chunk.Content.Length.Should().BeLessThanOrEqualTo(1000)); // Some buffer
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldJoinSentencesWithPeriod()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "First. Second. Third.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain(". ");
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldGenerateSequenceNumbers()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var sentences = Enumerable.Range(1, 10)
            .Select(i => string.Join(" ", Enumerable.Repeat($"sentence{i}", 200)) + ".");
        var text = string.Join(" ", sentences);

        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i].SequenceNumber.Should().Be(i);
        }
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_ShouldSetCorrectMetadata()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "Test sentence one. Test sentence two.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com/test"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().AllSatisfy(chunk =>
        {
            chunk.Id.Should().NotBeNullOrEmpty();
            chunk.StrategyInfo.StrategyName.Should().Be("Semantic");
            chunk.SourceUrl.Should().Be("https://example.com/test");
        });
    }

    [Fact]
    public async Task ChunkAsync_WithEmbeddingService_AndMixedPunctuation_ShouldHandleAll()
    {
        // Arrange
        var mockEmbeddingService = Substitute.For<ITextEmbeddingService>();
        var strategy = new SemanticChunkingStrategy(null, mockEmbeddingService);

        var text = "Statement one. Question two? Exclamation three! Another statement.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCount(1);
        var combinedContent = string.Join("", chunks.Select(c => c.Content));
        combinedContent.Should().Contain("Statement one");
        combinedContent.Should().Contain("Question two");
        combinedContent.Should().Contain("Exclamation three");
        combinedContent.Should().Contain("Another statement");
    }

    #endregion

    #region Original URL Fallback Tests

    [Fact]
    public async Task ChunkAsync_WithOriginalUrl_ShouldUseOriginalWhenUrlIsNull()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test content",
            Url = null!,
            OriginalUrl = "https://original.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.All(c => c.SourceUrl == "https://original.com").Should().BeTrue();
    }

    #endregion
}
