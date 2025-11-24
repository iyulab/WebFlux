using Microsoft.Extensions.Logging;
using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Strategies.Reconstruct;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// ReconstructStrategyFactory 단위 테스트
/// Factory 패턴과 자동 전략 선택 로직 검증
/// </summary>
public class ReconstructStrategyFactoryTests
{
    private readonly Mock<ITextCompletionService> _mockLlmService;
    private readonly Mock<ILogger<ReconstructStrategyFactory>> _mockLogger;

    public ReconstructStrategyFactoryTests()
    {
        _mockLlmService = new Mock<ITextCompletionService>();
        _mockLogger = new Mock<ILogger<ReconstructStrategyFactory>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithLlmService_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithoutLlmService_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new ReconstructStrategyFactory(null, _mockLogger.Object);
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldUseNullLogger()
    {
        // Act & Assert
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, null);
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNoParameters_ShouldUseDefaults()
    {
        // Act & Assert
        var factory = new ReconstructStrategyFactory();
        factory.Should().NotBeNull();
    }

    #endregion

    #region GetAvailableStrategies Tests

    [Fact]
    public void GetAvailableStrategies_ShouldReturnAllFiveStrategies()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategies = factory.GetAvailableStrategies().ToList();

        // Assert
        strategies.Should().HaveCount(5);
        strategies.Should().Contain(new[] { "None", "Summarize", "Expand", "Rewrite", "Enrich" });
    }

    [Fact]
    public void GetAvailableStrategies_WithoutLlmService_ShouldStillReturnAllStrategies()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(null, _mockLogger.Object);

        // Act
        var strategies = factory.GetAvailableStrategies().ToList();

        // Assert
        strategies.Should().HaveCount(5);
    }

    #endregion

    #region CreateStrategy Tests

    [Fact]
    public void CreateStrategy_WithNone_ShouldReturnNoneStrategy()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("None");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<NoneReconstructStrategy>();
    }

    [Fact]
    public void CreateStrategy_WithSummarize_ShouldReturnSummarizeStrategy()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("Summarize");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<SummarizeReconstructStrategy>();
    }

    [Fact]
    public void CreateStrategy_WithExpand_ShouldReturnExpandStrategy()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("Expand");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<ExpandReconstructStrategy>();
    }

    [Fact]
    public void CreateStrategy_WithRewrite_ShouldReturnRewriteStrategy()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("Rewrite");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<RewriteReconstructStrategy>();
    }

    [Fact]
    public void CreateStrategy_WithEnrich_ShouldReturnEnrichStrategy()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("Enrich");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<EnrichReconstructStrategy>();
    }

    [Theory]
    [InlineData("none")]
    [InlineData("SUMMARIZE")]
    [InlineData("Expand")]
    public void CreateStrategy_ShouldBeCaseInsensitive(string strategyName)
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy(strategyName);

        // Assert
        strategy.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateStrategy_WithNullOrEmptyName_ShouldDefaultToNone(string? strategyName)
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy(strategyName!);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<NoneReconstructStrategy>();
    }

    [Fact]
    public void CreateStrategy_WithUnknownName_ShouldDefaultToNone()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("UnknownStrategy");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<NoneReconstructStrategy>();
    }

    [Fact]
    public void CreateStrategy_LlmStrategyWithoutService_ShouldStillCreate()
    {
        // Arrange - LLM 서비스 없이 LLM 전략 생성
        var factory = new ReconstructStrategyFactory(null, _mockLogger.Object);

        // Act
        var strategy = factory.CreateStrategy("Summarize");

        // Assert - 생성은 되지만 경고 로그
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<SummarizeReconstructStrategy>();
    }

    #endregion

    #region GetStrategyCharacteristics Tests

    [Fact]
    public void GetStrategyCharacteristics_ShouldReturnAllFiveStrategies()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var characteristics = factory.GetStrategyCharacteristics();

        // Assert
        characteristics.Should().HaveCount(5);
        characteristics.Should().ContainKeys("None", "Summarize", "Expand", "Rewrite", "Enrich");
    }

    [Fact]
    public void GetStrategyCharacteristics_NoneStrategy_ShouldNotRequireLLM()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var characteristics = factory.GetStrategyCharacteristics();

        // Assert
        characteristics["None"].RequiresLLM.Should().BeFalse();
    }

    [Theory]
    [InlineData("Summarize")]
    [InlineData("Expand")]
    [InlineData("Rewrite")]
    [InlineData("Enrich")]
    public void GetStrategyCharacteristics_LlmStrategies_ShouldRequireLLM(string strategyName)
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var characteristics = factory.GetStrategyCharacteristics();

        // Assert
        characteristics[strategyName].RequiresLLM.Should().BeTrue();
    }

    [Fact]
    public void GetStrategyCharacteristics_AllStrategies_ShouldHaveCompleteMetadata()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var characteristics = factory.GetStrategyCharacteristics();

        // Assert
        foreach (var (key, value) in characteristics)
        {
            value.Name.Should().NotBeNullOrEmpty();
            value.Description.Should().NotBeNullOrEmpty();
            value.RecommendedUseCases.Should().NotBeNull();
            value.RecommendedUseCases.Should().NotBeEmpty();
        }
    }

    #endregion

    #region CreateOptimalStrategy Tests - Explicit Strategy

    [Fact]
    public void CreateOptimalStrategy_WithExplicitStrategy_ShouldUseSpecifiedStrategy()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent { CleanedContent = "Test content" };
        var options = new ReconstructOptions { Strategy = "Summarize" };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<SummarizeReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithExplicitNoneStrategy_ShouldUseNone()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent { CleanedContent = "Test content" };
        var options = new ReconstructOptions { Strategy = "None" };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<NoneReconstructStrategy>();
    }

    #endregion

    #region CreateOptimalStrategy Tests - Auto Selection

    [Fact]
    public void CreateOptimalStrategy_WithoutLlmService_ShouldReturnNone()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(null, _mockLogger.Object);
        var content = new AnalyzedContent { CleanedContent = "Test content" };
        var options = new ReconstructOptions { Strategy = "Auto" };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<NoneReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithUseLLMFalse_ShouldReturnNone()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent { CleanedContent = "Test content" };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = false };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<NoneReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithVeryLongContent_ShouldSelectSummarize()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = new string('a', 15000) // >10000 chars
        };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<SummarizeReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithLowQualityContent_ShouldSelectRewrite()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = "Medium length content",
            Metrics = new AnalysisMetrics { ContentQuality = 0.5 } // <0.6
        };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<RewriteReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithShortContent_ShouldSelectExpand()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = "Short", // <500 chars
            Metrics = new AnalysisMetrics { ContentQuality = 0.8 }
        };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<ExpandReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithImages_ShouldSelectEnrich()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = new string('a', 2000), // medium length
            Images = new List<ImageData> { new ImageData { Url = "image.jpg" } },
            Metrics = new AnalysisMetrics { ContentQuality = 0.8 }
        };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<EnrichReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithManySections_ShouldSelectEnrich()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = new string('a', 2000),
            Sections = new List<ContentSection>
            {
                new ContentSection(), new ContentSection(), new ContentSection(),
                new ContentSection(), new ContentSection(), new ContentSection() // >5 sections
            },
            Metrics = new AnalysisMetrics { ContentQuality = 0.8 }
        };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<EnrichReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithMediumContent_ShouldSelectRewrite()
    {
        // Arrange - 기본값 테스트
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = new string('a', 2000), // medium length
            Metrics = new AnalysisMetrics { ContentQuality = 0.8 }
        };
        var options = new ReconstructOptions { Strategy = "Auto", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert - 기본값은 Rewrite
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<RewriteReconstructStrategy>();
    }

    [Fact]
    public void CreateOptimalStrategy_WithEmptyStrategyOption_ShouldAutoSelect()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);
        var content = new AnalyzedContent
        {
            CleanedContent = new string('a', 15000)
        };
        var options = new ReconstructOptions { Strategy = "", UseLLM = true };

        // Act
        var strategy = factory.CreateOptimalStrategy(content, options);

        // Assert - Auto 선택 로직 동작
        strategy.Should().NotBeNull();
        strategy.Should().BeOfType<SummarizeReconstructStrategy>();
    }

    #endregion

    #region Multiple Creation Tests

    [Fact]
    public void CreateStrategy_CalledMultipleTimes_ShouldReturnNewInstances()
    {
        // Arrange
        var factory = new ReconstructStrategyFactory(_mockLlmService.Object, _mockLogger.Object);

        // Act
        var strategy1 = factory.CreateStrategy("Summarize");
        var strategy2 = factory.CreateStrategy("Summarize");

        // Assert - 매번 새 인스턴스 생성
        strategy1.Should().NotBeNull();
        strategy2.Should().NotBeNull();
        strategy1.Should().NotBeSameAs(strategy2);
    }

    #endregion
}
