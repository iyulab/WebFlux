using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// ChunkingStrategyFactory 단위 테스트
/// Factory 패턴과 전략 선택 로직 검증
/// </summary>
public class ChunkingStrategyFactoryTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChunkingStrategyFactory> _mockLogger;
    private readonly ChunkingStrategyFactory _factory;

    public ChunkingStrategyFactoryTests()
    {
        var services = new ServiceCollection();

        // Register all chunking strategies
        services.AddTransient<FixedSizeChunkingStrategy>();
        services.AddTransient<ParagraphChunkingStrategy>();
        services.AddTransient<SmartChunkingStrategy>();
        services.AddTransient<SemanticChunkingStrategy>();
        services.AddTransient<AutoChunkingStrategy>();
        services.AddTransient<MemoryOptimizedChunkingStrategy>();

        _serviceProvider = services.BuildServiceProvider();
        _mockLogger = Substitute.For<ILogger<ChunkingStrategyFactory>>();
        _factory = new ChunkingStrategyFactory(_serviceProvider, _mockLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChunkingStrategyFactory(null!, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ChunkingStrategyFactory(_serviceProvider, null!));
    }

    [Fact]
    public void Constructor_WithValidArguments_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new ChunkingStrategyFactory(_serviceProvider, _mockLogger);
        factory.Should().NotBeNull();
    }

    #endregion

    #region CreateStrategyAsync Tests

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
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be(strategyName);
    }

    [Theory]
    [InlineData("fixedsize")]
    [InlineData("PARAGRAPH")]
    [InlineData("Smart")]
    public async Task CreateStrategyAsync_ShouldBeCaseInsensitive(string strategyName)
    {
        // Act
        var strategy = await _factory.CreateStrategyAsync(strategyName);

        // Assert
        strategy.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateStrategyAsync_WithInvalidStrategyName_ShouldReturnParagraphAsFallback()
    {
        // Act
        var strategy = await _factory.CreateStrategyAsync("InvalidStrategy");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Name.Should().Be("Paragraph");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateStrategyAsync_WithNullOrEmptyName_ShouldThrowArgumentException(string? strategyName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _factory.CreateStrategyAsync(strategyName!));
    }

    #endregion

    #region GetAvailableStrategies Tests

    [Fact]
    public void GetAvailableStrategies_ShouldReturnAllStrategies()
    {
        // Act
        var strategies = _factory.GetAvailableStrategies().ToList();

        // Assert
        strategies.Should().HaveCount(6);
        strategies.Should().Contain("FixedSize");
        strategies.Should().Contain("Paragraph");
        strategies.Should().Contain("Smart");
        strategies.Should().Contain("Semantic");
        strategies.Should().Contain("Auto");
        strategies.Should().Contain("MemoryOptimized");
    }

    [Fact]
    public void GetAvailableStrategies_ShouldReturnNonEmptyList()
    {
        // Act
        var strategies = _factory.GetAvailableStrategies();

        // Assert
        strategies.Should().NotBeNull();
        strategies.Should().NotBeEmpty();
    }

    #endregion

    #region GetStrategyInfoAsync Tests

    [Theory]
    [InlineData("FixedSize")]
    [InlineData("Paragraph")]
    [InlineData("Smart")]
    public async Task GetStrategyInfoAsync_WithValidStrategy_ShouldReturnInfo(string strategyName)
    {
        // Act
        var info = await _factory.GetStrategyInfoAsync(strategyName);

        // Assert
        info.Should().NotBeNull();
        info.Name.Should().Be(strategyName);
        info.Description.Should().NotBeNullOrEmpty();
        info.PerformanceInfo.Should().NotBeNull();
        info.UseCases.Should().NotBeNull();
        info.SuitableContentTypes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStrategyInfoAsync_WithInvalidStrategy_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _factory.GetStrategyInfoAsync("InvalidStrategy"));
    }

    [Fact]
    public async Task GetStrategyInfoAsync_ForFixedSize_ShouldHaveCorrectMetadata()
    {
        // Act
        var info = await _factory.GetStrategyInfoAsync("FixedSize");

        // Assert
        info.Name.Should().Be("FixedSize");
        info.PerformanceInfo.MemoryUsage.Should().Be("Low");
        info.PerformanceInfo.Scalability.Should().Be("Excellent");
    }

    #endregion

    #region RecommendStrategyAsync Tests

    [Fact]
    public async Task RecommendStrategyAsync_WithVeryLargeContent_ShouldRecommendMemoryOptimized()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = new string('a', 150000), // >100000 chars
            Url = "https://example.com"
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("MemoryOptimized");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithMinimizeMemoryOption_ShouldRecommendMemoryOptimized()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Short content",
            Url = "https://example.com"
        };
        var options = new ChunkingOptions { MinimizeMemoryUsage = true };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content, options);

        // Assert
        recommendation.Should().Be("MemoryOptimized");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithMetadataRichUrl_ShouldRecommendAuto()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Some content",
            Url = "https://docs.example.com/guide",
            Title = "Example Guide"
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Auto");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithImagesAndMediumContent_ShouldRecommendSmart()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = new string('a', 6000), // >5000 chars
            Url = "https://example.com",
            ImageUrls = new List<string> { "https://example.com/image.jpg" }
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Smart");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithHeadings_ShouldRecommendAuto()
    {
        // Arrange - Headings trigger HasMetadataContext, which recommends Auto
        var content = new ExtractedContent
        {
            MainContent = "Some content",
            Url = "https://example.com",
            Headings = new List<string> { "# Heading 1", "## Heading 2" }
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Auto");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithTechnicalContent_ShouldRecommendSmart()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "class Example { function method() { return api.call(); } }",
            Url = "https://example.com"
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Smart");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithLongDocument_ShouldRecommendSemantic()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = new string('a', 12000), // >10000 chars
            Url = "https://example.com"
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Semantic");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithShortSimpleContent_ShouldRecommendParagraph()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Short simple text without any special features.",
            Url = "https://example.com"
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Paragraph");
    }

    [Fact]
    public async Task RecommendStrategyAsync_WithNullContent_ShouldReturnParagraphAsFallback()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = null!,
            Url = "https://example.com"
        };

        // Act
        var recommendation = await _factory.RecommendStrategyAsync(content);

        // Assert
        recommendation.Should().Be("Paragraph");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task IntegrationTest_CreateAndUseStrategy_ShouldWork()
    {
        // Arrange
        var content = new ExtractedContent
        {
            MainContent = "Test content for chunking",
            Url = "https://example.com"
        };

        // Act
        var strategyName = await _factory.RecommendStrategyAsync(content);
        var strategy = await _factory.CreateStrategyAsync(strategyName);
        var chunks = await strategy.ChunkAsync(content);

        // Assert
        chunks.Should().NotBeNull();
        chunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task IntegrationTest_AllStrategies_ShouldBeCreatable()
    {
        // Arrange
        var strategies = _factory.GetAvailableStrategies();

        // Act & Assert
        foreach (var strategyName in strategies)
        {
            var strategy = await _factory.CreateStrategyAsync(strategyName);
            strategy.Should().NotBeNull();
            strategy.Name.Should().Be(strategyName);
        }
    }

    #endregion
}
