using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services.ChunkingStrategies;
using Xunit;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// ChunkingStrategyFactory 단위 테스트
/// Phase 4D: 청킹 전략 팩토리의 전략 생성 및 추천 기능 검증
/// 7가지 전략 (Auto, MemoryOptimized 포함) 관리 능력 확인
/// </summary>
public class ChunkingStrategyFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<ChunkingStrategyFactory>> _mockLogger;
    private readonly ChunkingStrategyFactory _factory;

    public ChunkingStrategyFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ChunkingStrategyFactory>>();

        SetupMockStrategies();

        _factory = new ChunkingStrategyFactory(_mockServiceProvider.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("FixedSize")]
    [InlineData("Paragraph")]
    [InlineData("Smart")]
    [InlineData("Semantic")]
    [InlineData("Auto")]
    [InlineData("MemoryOptimized")]
    public async Task CreateStrategyAsync_WithValidStrategyName_ShouldReturnStrategy(string strategyName)
    {
        // Act
        var strategy = await _factory.CreateStrategyAsync(strategyName);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(strategyName, strategy.Name);
    }

    [Fact]
    public async Task CreateStrategyAsync_WithInvalidStrategyName_ShouldFallbackToParagraph()
    {
        // Act
        var strategy = await _factory.CreateStrategyAsync("InvalidStrategy");

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal("Paragraph", strategy.Name);
    }

    [Fact]
    public async Task CreateStrategyAsync_WithEmptyStrategyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _factory.CreateStrategyAsync(string.Empty));
    }

    [Fact]
    public async Task CreateStrategyAsync_WithNullStrategyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _factory.CreateStrategyAsync(null!));
    }

    [Fact]
    public void GetAvailableStrategies_ShouldReturnAllSupportedStrategies()
    {
        // Act
        var strategies = _factory.GetAvailableStrategies().ToList();

        // Assert
        Assert.Contains("FixedSize", strategies);
        Assert.Contains("Paragraph", strategies);
        Assert.Contains("Smart", strategies);
        Assert.Contains("Semantic", strategies);
        Assert.Contains("Auto", strategies);
        Assert.Contains("MemoryOptimized", strategies);
        Assert.Equal(6, strategies.Count);
    }

    [Fact]
    public async Task GetStrategyInfoAsync_WithValidStrategy_ShouldReturnInfo()
    {
        // Act
        var info = await _factory.GetStrategyInfoAsync("Auto");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("Auto", info.Name);
        Assert.Contains("지능형 자동 전략 선택", info.Description);
        Assert.NotNull(info.PerformanceInfo);
        Assert.NotEmpty(info.UseCases);
        Assert.NotEmpty(info.SuitableContentTypes);
    }

    [Fact]
    public async Task GetStrategyInfoAsync_WithInvalidStrategy_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _factory.GetStrategyInfoAsync("InvalidStrategy"));
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithTechnicalContent_ShouldRecommendSmart()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/api-docs",
            Title = "API Documentation",
            MainContent = "This function returns a Promise. Use class decorators for validation. Example: @IsEmail() property.",
            Headings = new List<string> { "API Reference", "Code Examples", "Interface Definition" }
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Smart", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithLargeContent_ShouldRecommendMemoryOptimized()
    {
        // Arrange
        var largeContent = new string('a', 150000); // 150KB
        var content = new ExtractedContent
        {
            Url = "https://example.com/large-doc",
            Title = "Large Document",
            MainContent = largeContent
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("MemoryOptimized", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithMemoryConstraints_ShouldRecommendMemoryOptimized()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/regular",
            Title = "Regular Content",
            MainContent = "Regular content that would normally use a different strategy."
        };

        var options = new ChunkingOptions
        {
            MinimizeMemoryUsage = true
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content, options);

        // Assert
        Assert.Equal("MemoryOptimized", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithMetadataRichContent_ShouldRecommendAuto()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://github.com/user/project",
            Title = "GitHub Repository",
            MainContent = "This is a GitHub repository with rich metadata.",
            Headings = new List<string> { "README", "Documentation" }
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Auto", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithMultimodalContent_ShouldRecommendSmart()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/gallery",
            Title = "Image Gallery",
            MainContent = "This gallery contains many images with detailed descriptions and technical specifications for each image.",
            ImageUrls = new List<string>
            {
                "https://example.com/image1.jpg",
                "https://example.com/image2.png",
                "https://example.com/image3.gif"
            }
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Smart", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithLongTextDocument_ShouldRecommendSemantic()
    {
        // Arrange
        var longContent = string.Join("\n\n",
            Enumerable.Range(1, 100).Select(i => $"This is paragraph {i} with substantial content about academic research."));

        var content = new ExtractedContent
        {
            Url = "https://example.com/academic",
            Title = "Academic Paper",
            MainContent = longContent
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Semantic", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithSimpleContent_ShouldRecommendParagraph()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/blog",
            Title = "Blog Post",
            MainContent = "This is a simple blog post with regular text content."
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Paragraph", recommendation);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithExceptionDuringAnalysis_ShouldReturnParagraphFallback()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = null!, // null URL to potentially cause issues
            Title = "Problematic Content",
            MainContent = "Content that might cause analysis issues."
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Paragraph", recommendation); // Should fallback to Paragraph
    }

    [Theory]
    [InlineData("docs.microsoft.com")]
    [InlineData("stackoverflow.com")]
    [InlineData("github.com")]
    [InlineData("medium.com")]
    [InlineData("dev.to")]
    public async Task RecommendStrategyAsync_WithMetadataRichSites_ShouldRecommendAuto(string domain)
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = $"https://{domain}/some/path",
            Title = "Rich Metadata Site",
            MainContent = "Content from a site known for rich metadata."
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        Assert.Equal("Auto", recommendation);
    }

    [Fact]
    public async Task CreateStrategyAsync_WithServiceProviderFailure_ShouldThrowException()
    {
        // Arrange
        _mockServiceProvider
            .Setup(x => x.GetRequiredService<FixedSizeChunkingStrategy>())
            .Throws(new InvalidOperationException("Service not registered"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _factory.CreateStrategyAsync("FixedSize"));

        Assert.Contains("청킹 전략 'FixedSize' 생성에 실패했습니다", exception.Message);
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithNullContent_ShouldReturnParagraphFallback()
    {
        // Act
        var recommendation = await _factory.RecommendStrategyAsync(null!);

        // Assert
        Assert.Equal("Paragraph", recommendation);
    }

    private void SetupMockStrategies()
    {
        // FixedSize Strategy Mock
        var fixedSizeStrategy = new Mock<IChunkingStrategy>();
        fixedSizeStrategy.Setup(x => x.Name).Returns("FixedSize");
        _mockServiceProvider.Setup(x => x.GetRequiredService<FixedSizeChunkingStrategy>())
            .Returns(fixedSizeStrategy.Object as FixedSizeChunkingStrategy);

        // Paragraph Strategy Mock
        var paragraphStrategy = new Mock<IChunkingStrategy>();
        paragraphStrategy.Setup(x => x.Name).Returns("Paragraph");
        _mockServiceProvider.Setup(x => x.GetRequiredService<ParagraphChunkingStrategy>())
            .Returns(paragraphStrategy.Object as ParagraphChunkingStrategy);

        // Smart Strategy Mock
        var smartStrategy = new Mock<IChunkingStrategy>();
        smartStrategy.Setup(x => x.Name).Returns("Smart");
        _mockServiceProvider.Setup(x => x.GetRequiredService<SmartChunkingStrategy>())
            .Returns(smartStrategy.Object as SmartChunkingStrategy);

        // Semantic Strategy Mock
        var semanticStrategy = new Mock<IChunkingStrategy>();
        semanticStrategy.Setup(x => x.Name).Returns("Semantic");
        _mockServiceProvider.Setup(x => x.GetRequiredService<SemanticChunkingStrategy>())
            .Returns(semanticStrategy.Object as SemanticChunkingStrategy);

        // Auto Strategy Mock
        var autoStrategy = new Mock<IChunkingStrategy>();
        autoStrategy.Setup(x => x.Name).Returns("Auto");
        _mockServiceProvider.Setup(x => x.GetRequiredService<AutoChunkingStrategy>())
            .Returns(autoStrategy.Object as AutoChunkingStrategy);

        // MemoryOptimized Strategy Mock
        var memoryOptimizedStrategy = new Mock<IChunkingStrategy>();
        memoryOptimizedStrategy.Setup(x => x.Name).Returns("MemoryOptimized");
        _mockServiceProvider.Setup(x => x.GetRequiredService<MemoryOptimizedChunkingStrategy>())
            .Returns(memoryOptimizedStrategy.Object as MemoryOptimizedChunkingStrategy);
    }
}