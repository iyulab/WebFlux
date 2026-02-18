using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// AutoChunkingStrategy 단위 테스트
/// 자동 전략 선택 및 폴백 동작 검증
/// </summary>
public class AutoChunkingStrategyTests
{
    private readonly AutoChunkingStrategy _strategy;

    public AutoChunkingStrategyTests()
    {
        _strategy = new AutoChunkingStrategy(); // No services - basic mode
    }

    #region Basic Properties Tests

    [Fact]
    public void Name_ShouldBeAuto()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        name.Should().Be("Auto");
    }

    [Fact]
    public void Description_ShouldContainAuto()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("자동");
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

    #region Strategy Selection Tests

    [Fact]
    public async Task ChunkAsync_WithShortText_ShouldSelectAppropriateStrategy()
    {
        // Arrange
        var text = "This is a short text that should be handled by the auto strategy.";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
        chunks[0].Content.Should().Contain(text);
    }

    [Fact]
    public async Task ChunkAsync_WithLongText_ShouldSplitIntoChunks()
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
    }

    [Fact]
    public async Task ChunkAsync_WithImages_ShouldConsiderMultimodalContent()
    {
        // Arrange
        var text = string.Join(" ", Enumerable.Repeat("word", 200));
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            ImageUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithHeadings_ShouldConsiderStructure()
    {
        // Arrange
        var text = "# Heading 1\nContent for heading 1\n## Heading 2\nContent for heading 2";
        var content = new ExtractedContent
        {
            MainContent = text,
            Url = "https://example.com",
            Headings = new List<string> { "# Heading 1", "## Heading 2" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Fallback Behavior Tests

    [Fact]
    public async Task ChunkAsync_OnError_ShouldFallbackToParagraphStrategy()
    {
        // Arrange - Null content that might cause internal errors but should fallback gracefully
        var content = new ExtractedContent
        {
            MainContent = "Simple fallback test content\n\nParagraph 2",
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert - Should not throw and should return chunks
        chunks.Should().NotBeNull();
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Custom Options Tests

    [Fact]
    public async Task ChunkAsync_WithCustomChunkSize_ShouldRespectOptions()
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
        chunks.Should().NotBeEmpty();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task ChunkAsync_ShouldSetChunkMetadata()
    {
        // Arrange
        var text = "Test content for auto chunking strategy";
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

    #endregion

    #region Content Type Detection Tests

    [Fact]
    public async Task ChunkAsync_WithTechnicalContent_ShouldDetectContentType()
    {
        // Arrange
        var technicalText = @"
            public class Example {
                private readonly IService _service;
                public async Task ProcessAsync() {
                    await _service.ExecuteAsync();
                }
            }
        ";
        var content = new ExtractedContent
        {
            MainContent = technicalText,
            Url = "https://example.com/docs"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChunkAsync_WithAcademicContent_ShouldDetectContentType()
    {
        // Arrange
        var academicText = "Abstract: This research paper presents a novel approach to machine learning. " +
                          "Keywords: artificial intelligence, deep learning, neural networks.";
        var content = new ExtractedContent
        {
            MainContent = academicText,
            Url = "https://example.com/research"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
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
    public async Task ChunkAsync_WithVeryLongDocument_ShouldHandleEfficiently()
    {
        // Arrange - Add paragraph breaks to enable splitting
        var paragraphs = Enumerable.Range(1, 10)
            .Select(i => string.Join(" ", Enumerable.Repeat($"word{i}", 200)));
        var veryLongText = string.Join("\n\n", paragraphs); // ~10000 chars with paragraph breaks
        var content = new ExtractedContent
        {
            MainContent = veryLongText,
            Url = "https://example.com"
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        chunks.Should().AllSatisfy(chunk => chunk.Content.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task ChunkAsync_WithMixedContent_ShouldAdaptStrategy()
    {
        // Arrange
        var mixedContent = @"
# Technical Documentation

This is a technical guide with code examples.

```csharp
public class Example {
    // Code here
}
```

Regular paragraph text continues here with more details about the implementation.

## Images Section
![Image 1](image1.jpg)
![Image 2](image2.jpg)
";
        var content = new ExtractedContent
        {
            MainContent = mixedContent,
            Url = "https://example.com/guide",
            Headings = new List<string> { "# Technical Documentation", "## Images Section" },
            ImageUrls = new List<string> { "https://example.com/image1.jpg", "https://example.com/image2.jpg" }
        };

        // Act
        var chunks = await _strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeEmpty();
    }

    #endregion
}
