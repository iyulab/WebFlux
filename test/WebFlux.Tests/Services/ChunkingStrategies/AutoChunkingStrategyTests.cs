using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services.ChunkingStrategies;
using Xunit;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// AutoChunkingStrategy 단위 테스트
/// Phase 4D: 지능형 자동 전략 선택 기능의 정확성과 안정성을 검증
/// </summary>
public class AutoChunkingStrategyTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IChunkingStrategyFactory> _mockStrategyFactory;
    private readonly Mock<IMetadataDiscoveryService> _mockMetadataService;
    private readonly Mock<ILogger<AutoChunkingStrategy>> _mockLogger;
    private readonly AutoChunkingStrategy _autoStrategy;

    public AutoChunkingStrategyTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockStrategyFactory = new Mock<IChunkingStrategyFactory>();
        _mockMetadataService = new Mock<IMetadataDiscoveryService>();
        _mockLogger = new Mock<ILogger<AutoChunkingStrategy>>();

        _mockServiceProvider
            .Setup(x => x.GetRequiredService<IChunkingStrategyFactory>())
            .Returns(_mockStrategyFactory.Object);

        _mockServiceProvider
            .Setup(x => x.GetRequiredService<IMetadataDiscoveryService>())
            .Returns(_mockMetadataService.Object);

        _autoStrategy = new AutoChunkingStrategy(
            _mockServiceProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessAsync_WithTechnicalContent_ShouldSelectSmartStrategy()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/api-docs",
            Title = "API Documentation",
            MainContent = "This function returns a Promise<User> object. The parameter must be validated using class decorators. Example: @IsEmail() email: string",
            Headings = new List<string> { "API Reference", "Code Examples" }
        };

        var mockSmartStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Technical chunk 1", Index = 0 },
            new() { Content = "Technical chunk 2", Index = 1 }
        };

        SetupStrategyMock("Smart", mockSmartStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("smart");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Chunks.Count);
        Assert.Contains("Smart", result.StrategyUsed);
        VerifyStrategySelection("Smart");
    }

    [Fact]
    public async Task ProcessAsync_WithLargeDocument_ShouldSelectMemoryOptimizedStrategy()
    {
        // Arrange
        var largeContent = new string('a', 150000); // 150KB 문서
        var content = new ExtractedContent
        {
            Url = "https://example.com/large-document",
            Title = "Large Document",
            MainContent = largeContent
        };

        var mockMemoryStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Memory optimized chunk", Index = 0 }
        };

        SetupStrategyMock("MemoryOptimized", mockMemoryStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("memory-optimized");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("MemoryOptimized", result.StrategyUsed);
        VerifyStrategySelection("MemoryOptimized");
    }

    [Fact]
    public async Task ProcessAsync_WithAiTxtMetadata_ShouldFollowAiTxtStrategy()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/ai-friendly",
            Title = "AI-Friendly Site",
            MainContent = "This site provides AI-friendly content structure."
        };

        var aiTxtMetadata = new AiTxtMetadata
        {
            ChunkingStrategy = "semantic",
            ChunkSize = 1000,
            OverlapSize = 200,
            Priority = ChunkingPriority.Quality
        };

        var mockSemanticStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Semantic chunk", Index = 0 }
        };

        SetupStrategyMock("Semantic", mockSemanticStrategy, expectedChunks);
        _mockMetadataService
            .Setup(x => x.GetAiTxtMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiTxtMetadata);

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("Semantic", result.StrategyUsed);
        VerifyStrategySelection("Semantic");
    }

    [Fact]
    public async Task ProcessAsync_WithMemoryConstraints_ShouldSelectMemoryOptimized()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/regular-doc",
            Title = "Regular Document",
            MainContent = "Regular content that would normally use Smart strategy."
        };

        var options = new ChunkingOptions
        {
            MinimizeMemoryUsage = true,
            MaxChunkSize = 500
        };

        var mockMemoryStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Memory constrained chunk", Index = 0 }
        };

        SetupStrategyMock("MemoryOptimized", mockMemoryStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("memory-optimized");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("MemoryOptimized", result.StrategyUsed);
        VerifyStrategySelection("MemoryOptimized");
    }

    [Fact]
    public async Task ProcessAsync_WithMultimodalContent_ShouldSelectSmartStrategy()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/gallery",
            Title = "Image Gallery",
            MainContent = "This gallery contains various images with descriptions and technical specifications.",
            ImageUrls = new List<string>
            {
                "https://example.com/image1.jpg",
                "https://example.com/image2.png",
                "https://example.com/image3.gif"
            }
        };

        var mockSmartStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Multimodal chunk with images", Index = 0 }
        };

        SetupStrategyMock("Smart", mockSmartStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("smart");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("Smart", result.StrategyUsed);
        VerifyStrategySelection("Smart");
    }

    [Fact]
    public async Task ProcessAsync_WithSimpleText_ShouldSelectParagraphStrategy()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/blog",
            Title = "Simple Blog Post",
            MainContent = "This is a simple blog post with normal text content. It has multiple paragraphs but no technical complexity."
        };

        var mockParagraphStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Simple paragraph chunk", Index = 0 }
        };

        SetupStrategyMock("Paragraph", mockParagraphStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("paragraph");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("Paragraph", result.StrategyUsed);
        VerifyStrategySelection("Paragraph");
    }

    [Fact]
    public async Task ProcessAsync_WithLongAcademicContent_ShouldSelectSemanticStrategy()
    {
        // Arrange
        var longAcademicContent = string.Join("\n\n",
            Enumerable.Range(1, 50).Select(i =>
                $"This is paragraph {i} of an academic paper discussing complex theoretical concepts and research methodologies."));

        var content = new ExtractedContent
        {
            Url = "https://example.com/research",
            Title = "Academic Research Paper",
            MainContent = longAcademicContent
        };

        var mockSemanticStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Semantic academic chunk", Index = 0 }
        };

        SetupStrategyMock("Semantic", mockSemanticStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("semantic");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("Semantic", result.StrategyUsed);
        VerifyStrategySelection("Semantic");
    }

    [Fact]
    public async Task ProcessAsync_WithStrategyCreationFailure_ShouldFallbackToParagraph()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/technical",
            Title = "Technical Content",
            MainContent = "Technical content that should use Smart strategy but fails to create."
        };

        _mockStrategyFactory
            .Setup(x => x.CreateStrategyAsync("Smart"))
            .ThrowsAsync(new InvalidOperationException("Strategy creation failed"));

        var mockParagraphStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Fallback paragraph chunk", Index = 0 }
        };

        SetupStrategyMock("Paragraph", mockParagraphStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("smart");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Chunks.Count);
        Assert.Contains("Paragraph", result.StrategyUsed);
        VerifyStrategySelection("Paragraph");
    }

    [Fact]
    public async Task ProcessAsync_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _autoStrategy.ProcessAsync(null!, null, CancellationToken.None));
    }

    [Theory]
    [InlineData("function", "class", "interface", "import")] // 기술적 키워드
    [InlineData("API", "endpoint", "parameter", "response")] // API 관련 키워드
    [InlineData("```", "code", "example", "method")] // 코드 관련 키워드
    public async Task ProcessAsync_WithTechnicalKeywords_ShouldSelectSmartStrategy(params string[] keywords)
    {
        // Arrange
        var technicalContent = string.Join(" ", keywords.Select(k => $"This content contains {k} examples."));
        var content = new ExtractedContent
        {
            Url = "https://example.com/tech",
            Title = "Technical Documentation",
            MainContent = technicalContent
        };

        var mockSmartStrategy = new Mock<IChunkingStrategy>();
        var expectedChunks = new List<Chunk>
        {
            new() { Content = "Technical smart chunk", Index = 0 }
        };

        SetupStrategyMock("Smart", mockSmartStrategy, expectedChunks);
        SetupMetadataDiscoveryMock("smart");

        // Act
        var result = await _autoStrategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Smart", result.StrategyUsed);
        VerifyStrategySelection("Smart");
    }

    private void SetupStrategyMock(string strategyName, Mock<IChunkingStrategy> mockStrategy, List<Chunk> chunks)
    {
        var chunkResult = new ChunkResult
        {
            Chunks = chunks,
            TotalChunks = chunks.Count,
            StrategyUsed = strategyName,
            ProcessingTimeMs = 100
        };

        mockStrategy
            .Setup(x => x.ProcessAsync(It.IsAny<ExtractedContent>(), It.IsAny<ChunkingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chunkResult);

        _mockStrategyFactory
            .Setup(x => x.CreateStrategyAsync(strategyName))
            .ReturnsAsync(mockStrategy.Object);
    }

    private void SetupMetadataDiscoveryMock(string recommendedStrategy)
    {
        _mockMetadataService
            .Setup(x => x.GetAiTxtMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiTxtMetadata?)null);
    }

    private void VerifyStrategySelection(string strategyName)
    {
        _mockStrategyFactory.Verify(
            x => x.CreateStrategyAsync(strategyName),
            Times.Once,
            $"Expected {strategyName} strategy to be selected");
    }
}