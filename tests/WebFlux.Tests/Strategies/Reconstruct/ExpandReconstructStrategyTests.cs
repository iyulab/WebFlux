using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Strategies.Reconstruct;
using Xunit;
using NSubstitute;

namespace WebFlux.Tests.Strategies.Reconstruct;

/// <summary>
/// ExpandReconstructStrategy 테스트
/// 100% 커버리지 목표
/// </summary>
public class ExpandReconstructStrategyTests
{
    private readonly ITextCompletionService _mockLlmService;
    private readonly ExpandReconstructStrategy _strategy;

    public ExpandReconstructStrategyTests()
    {
        _mockLlmService = Substitute.For<ITextCompletionService>();
        _strategy = new ExpandReconstructStrategy(_mockLlmService);
    }

    [Fact]
    public void Name_ShouldReturnExpand()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("Expand", name);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        Assert.NotEmpty(description);
        Assert.Contains("확장", description);
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
        var strategyWithoutLlm = new ExpandReconstructStrategy(null);
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
        var strategyWithoutLlm = new ExpandReconstructStrategy(null);
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = true };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await strategyWithoutLlm.ApplyAsync(content, options)
        );
    }

    [Fact]
    public async Task ApplyAsync_ShouldCallLLMService()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            ExpansionRatio = 1.5,
            MaxTokens = 3000,
            Temperature = 0.5
        };

        var expectedExpansion = "This is an expanded version with more details, examples, and comprehensive explanations of the original content.";
        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedExpansion);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains("expand") || p.Contains("Expand")),
                Arg.Is<TextCompletionOptions>(o => o.MaxTokens == 3000 && o.Temperature == 0.5),
                Arg.Any<CancellationToken>());

        Assert.Equal(expectedExpansion, result.ReconstructedText);
        Assert.True(result.UsedLLM);
        Assert.Equal("Expand", result.StrategyUsed);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIncludeContextPromptIfProvided()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var contextPrompt = "Add more technical examples";
        var options = new ReconstructOptions
        {
            UseLLM = true,
            ContextPrompt = contextPrompt
        };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("Expanded content");

        // Act
        await _strategy.ApplyAsync(content, options);

        // Assert
        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains(contextPrompt)),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_ShouldSetMetricsCorrectly()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent("Short original content.");
        var expansion = "This is a much longer expanded version with detailed explanations, comprehensive examples, and thorough analysis.";
        var options = new ReconstructOptions { UseLLM = true };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(expansion);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.ProcessingTimeMs >= 0);
        Assert.Equal(1, result.Metrics.LLMCallCount);
        Assert.True(result.Metrics.TokensUsed > 0);
        Assert.True(result.Metrics.CompressionRatio > 1.0); // 확장되었으므로 > 1
    }

    [Fact]
    public async Task ApplyAsync_ShouldCreateEnhancement()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var expansion = "Expanded text with more details";
        var options = new ReconstructOptions { UseLLM = true };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(expansion);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.Single(result.Enhancements);
        Assert.Equal("Expansion", result.Enhancements[0].Type);
        Assert.Equal(expansion, result.Enhancements[0].Content);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellation_ShouldPassTokenToLLM()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = true };
        var cts = new CancellationTokenSource();

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("Expanded");

        // Act
        await _strategy.ApplyAsync(content, options, cts.Token);

        // Assert
        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                cts.Token);
    }

    [Fact]
    public void EstimateProcessingTime_ShouldReturnReasonableEstimate()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent(new string('a', 1000));
        var options = new ReconstructOptions();

        // Act
        var estimate = _strategy.EstimateProcessingTime(content, options);

        // Assert
        Assert.True(estimate > TimeSpan.Zero);
        Assert.True(estimate < TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task ApplyAsync_WithDifferentExpansionRatios_ShouldAdjustTargetLength()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent(new string('a', 1000));

        var options1 = new ReconstructOptions { UseLLM = true, ExpansionRatio = 1.2 };
        var options2 = new ReconstructOptions { UseLLM = true, ExpansionRatio = 2.0 };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("Expanded");

        // Act
        await _strategy.ApplyAsync(content, options1);
        await _strategy.ApplyAsync(content, options2);

        // Assert
        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains("1200")), // 1000 * 1.2
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>());

        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains("2000")), // 1000 * 2.0
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>());
    }

    private static AnalyzedContent CreateSampleAnalyzedContent(string? customContent = null)
    {
        return new AnalyzedContent
        {
            Url = "https://example.com/test",
            CleanedContent = customContent ?? "Brief content that needs expansion with more details and examples.",
            Title = "Test Document",
            Metadata = new WebContentMetadata
            {
                Title = "Test Document"
            }
        };
    }
}
