using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using AwesomeAssertions;

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

        // Register all chunking strategies. FixedSize/Paragraph/Semantic are no longer types of
        // their own: the factory builds them from FluxCurator's chunker factory, so that is what
        // has to be resolvable here.
        services.AddSingleton<FluxCurator.Core.Core.IChunkerFactory>(
            new FluxCurator.Infrastructure.Chunking.ChunkerFactory());
        services.AddTransient<SmartChunkingStrategy>();
        services.AddTransient<AutoChunkingStrategy>();

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
        var strategy = await _factory.CreateStrategyAsync(strategyName, cancellationToken: TestContext.Current.CancellationToken);

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
        var strategy = await _factory.CreateStrategyAsync(strategyName, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        strategy.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateStrategyAsync_WithUnknownStrategyName_ThrowsAndNamesTheAvailableOnes()
    {
        // A caller that asks for a strategy with no implementation must not silently get a different one.
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _factory.CreateStrategyAsync("InvalidStrategy", cancellationToken: TestContext.Current.CancellationToken));

        ex.Message.Should().Contain("InvalidStrategy").And.Contain("Paragraph");
    }

    [Fact]
    public async Task EveryChunkingStrategyTypeMember_HasARegisteredStrategy()
    {
        // The enum is the typed front door (ChunkingOptions.Strategy -> ToString() -> this factory). A member
        // with no registered strategy is a promise nothing keeps — Intelligent was one until 0.14.0.
        foreach (var member in Enum.GetValues<WebFlux.Core.Options.ChunkingStrategyType>())
        {
            var strategy = await _factory.CreateStrategyAsync(member.ToString(), cancellationToken: TestContext.Current.CancellationToken);
            strategy.Should().NotBeNull(because: $"{member} must map to a registered strategy");
        }
    }

    [Fact]
    public void ChunkingStrategyType_Ordinals_AreFixed()
    {
        // Numeric configuration binds by ordinal; removing Intelligent (5) must not shift MemoryOptimized.
        ((int)WebFlux.Core.Options.ChunkingStrategyType.Semantic).Should().Be(4);
        ((int)WebFlux.Core.Options.ChunkingStrategyType.MemoryOptimized).Should().Be(6);
        Enum.IsDefined(typeof(WebFlux.Core.Options.ChunkingStrategyType), 5).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateStrategyAsync_WithNullOrEmptyName_ShouldThrowArgumentException(string? strategyName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _factory.CreateStrategyAsync(strategyName!, cancellationToken: TestContext.Current.CancellationToken));
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
        var info = await _factory.GetStrategyInfoAsync(strategyName, cancellationToken: TestContext.Current.CancellationToken);

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
            await _factory.GetStrategyInfoAsync("InvalidStrategy", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetStrategyInfoAsync_ForFixedSize_ShouldHaveCorrectMetadata()
    {
        // Act
        var info = await _factory.GetStrategyInfoAsync("FixedSize", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        info.Name.Should().Be("FixedSize");
        info.PerformanceInfo.MemoryUsage.Should().Be("Low");
        info.PerformanceInfo.Scalability.Should().Be("Excellent");
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
        var strategy = await _factory.CreateStrategyAsync("Auto", cancellationToken: TestContext.Current.CancellationToken);
        var chunks = await strategy.ChunkAsync(content, cancellationToken: TestContext.Current.CancellationToken);

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
            var strategy = await _factory.CreateStrategyAsync(strategyName, cancellationToken: TestContext.Current.CancellationToken);
            strategy.Should().NotBeNull();
            strategy.Name.Should().Be(strategyName);
        }
    }

    #endregion
}
