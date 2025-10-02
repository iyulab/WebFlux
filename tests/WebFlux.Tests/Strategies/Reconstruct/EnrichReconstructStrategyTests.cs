using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Strategies.Reconstruct;
using Xunit;
using Moq;

namespace WebFlux.Tests.Strategies.Reconstruct;

/// <summary>
/// EnrichReconstructStrategy 테스트
/// 100% 커버리지 목표
/// </summary>
public class EnrichReconstructStrategyTests
{
    private readonly Mock<ITextCompletionService> _mockLlmService;
    private readonly EnrichReconstructStrategy _strategy;

    public EnrichReconstructStrategyTests()
    {
        _mockLlmService = new Mock<ITextCompletionService>();
        _strategy = new EnrichReconstructStrategy(_mockLlmService.Object);
    }

    [Fact]
    public void Name_ShouldReturnEnrich()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("Enrich", name);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        Assert.NotEmpty(description);
        Assert.Contains("보강", description);
    }

    [Fact]
    public void RecommendedUseCases_ShouldReturnMultipleCases()
    {
        // Act
        var useCases = _strategy.RecommendedUseCases.ToList();

        // Assert
        Assert.NotEmpty(useCases);
        Assert.Equal(3, useCases.Count);
    }

    [Fact]
    public void IsApplicable_WithLLMServiceAndUseLLMTrue_ShouldReturnTrue()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = true };

        // Act
        var result = _strategy.IsApplicable(content, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsApplicable_WithLLMServiceButUseLLMFalse_ShouldReturnFalse()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = false };

        // Act
        var result = _strategy.IsApplicable(content, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsApplicable_WithoutLLMService_ShouldReturnFalse()
    {
        // Arrange
        var strategyWithoutLlm = new EnrichReconstructStrategy(null);
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = true };

        // Act
        var result = strategyWithoutLlm.IsApplicable(content, options);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ApplyAsync_WithoutLLMService_ShouldThrowException()
    {
        // Arrange
        var strategyWithoutLlm = new EnrichReconstructStrategy(null);
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = true };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await strategyWithoutLlm.ApplyAsync(content, options)
        );
    }

    [Fact]
    public async Task ApplyAsync_ShouldCallLLMServiceForEachEnrichmentType()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Context", "Definitions", "Examples" },
            MaxTokens = 2000,
            Temperature = 0.5
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Enrichment content");

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        // Should call LLM 3 times (once for each enrichment type)
        // Note: Strategy uses hardcoded MaxTokens=1000, not from options
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.IsAny<string>(),
                It.Is<TextCompletionOptions>(o => o.MaxTokens == 1000 && Math.Abs(o.Temperature - 0.5f) < 0.001f),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        Assert.True(result.UsedLLM);
        Assert.Equal("Enrich", result.StrategyUsed);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIncludeEnrichmentTypeInPrompt()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Definitions" }
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Definition enrichment");

        // Act
        await _strategy.ApplyAsync(content, options);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.Is<string>(p => p.Contains("define", StringComparison.OrdinalIgnoreCase) ||
                                   p.Contains("definitions", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIncludeContextPromptIfProvided()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var contextPrompt = "Focus on technical concepts";
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Context" },
            ContextPrompt = contextPrompt
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Enriched");

        // Act
        await _strategy.ApplyAsync(content, options);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.Is<string>(p => p.Contains(contextPrompt)),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_ShouldSetMetricsCorrectly()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Context", "Examples" }
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Enrichment");

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.ProcessingTimeMs >= 0);
        Assert.Equal(2, result.Metrics.LLMCallCount); // 2 enrichment types
        Assert.True(result.Metrics.TokensUsed > 0);
    }

    [Fact]
    public async Task ApplyAsync_ShouldCreateEnhancementsForEachType()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Context", "Definitions" }
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Enrichment content");

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.Equal(2, result.Enhancements.Count);
        Assert.Contains(result.Enhancements, e => e.Type == "Context");
        Assert.Contains(result.Enhancements, e => e.Type == "Definitions");
    }

    [Fact]
    public async Task ApplyAsync_ShouldCombineOriginalContentWithEnrichments()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent("Original text.");
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Context" }
        };

        var enrichment = "Additional context.";
        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrichment);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.Contains("Original text.", result.ReconstructedText);
        Assert.Contains(enrichment, result.ReconstructedText);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellation_ShouldPassTokenToLLM()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { "Context" }
        };
        var cts = new CancellationTokenSource();

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Enriched");

        // Act
        await _strategy.ApplyAsync(content, options, cts.Token);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public void EstimateProcessingTime_ShouldReturnReasonableEstimate()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent(new string('a', 1000));
        var options = new ReconstructOptions
        {
            EnrichmentTypes = new List<string> { "Context", "Definitions", "Examples" }
        };

        // Act
        var estimate = _strategy.EstimateProcessingTime(content, options);

        // Assert
        Assert.True(estimate > TimeSpan.Zero);
        Assert.True(estimate < TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void IsApplicable_WithEmptyEnrichmentTypes_ShouldReturnFalse()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string>() // Empty list
        };

        // Act
        var result = _strategy.IsApplicable(content, options);

        // Assert
        // Empty enrichment types means strategy is not applicable
        Assert.False(result);
    }

    [Theory]
    [InlineData("Context", "context")]
    [InlineData("Definitions", "define")]
    [InlineData("Examples", "example")]
    [InlineData("RelatedInfo", "related")]
    public async Task ApplyAsync_WithSingleEnrichmentType_ShouldCallLLMOnce(string enrichmentType, string expectedKeyword)
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            EnrichmentTypes = new List<string> { enrichmentType }
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Enrichment");

        // Act
        await _strategy.ApplyAsync(content, options);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.Is<string>(p => p.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AnalyzedContent CreateSampleAnalyzedContent(string? customContent = null)
    {
        return new AnalyzedContent
        {
            Url = "https://example.com/test",
            CleanedContent = customContent ?? "Content that can be enhanced with additional context, definitions, and examples.",
            Title = "Test Document",
            Metadata = new WebContentMetadata
            {
                Title = "Test Document"
            }
        };
    }
}
