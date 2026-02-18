using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Strategies.Reconstruct;
using Xunit;
using NSubstitute;

namespace WebFlux.Tests.Strategies.Reconstruct;

/// <summary>
/// SummarizeReconstructStrategy 테스트
/// 100% 커버리지 목표
/// </summary>
public class SummarizeReconstructStrategyTests
{
    private readonly ITextCompletionService _mockLlmService;
    private readonly SummarizeReconstructStrategy _strategy;

    public SummarizeReconstructStrategyTests()
    {
        _mockLlmService = Substitute.For<ITextCompletionService>();
        _strategy = new SummarizeReconstructStrategy(_mockLlmService);
    }

    [Fact]
    public void Name_ShouldReturnSummarize()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("Summarize", name);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        Assert.NotEmpty(description);
        Assert.Contains("요약", description);
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
        var strategyWithoutLlm = new SummarizeReconstructStrategy(null);
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
        var strategyWithoutLlm = new SummarizeReconstructStrategy(null);
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
            SummaryRatio = 0.3,
            MaxTokens = 2000,
            Temperature = 0.3
        };

        var expectedSummary = "This is a concise summary of the content.";
        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedSummary);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains("summarize") || p.Contains("Summary")),
                Arg.Is<TextCompletionOptions>(o => o.MaxTokens == 2000),
                Arg.Any<CancellationToken>());

        Assert.Equal(expectedSummary, result.ReconstructedText);
        Assert.True(result.UsedLLM);
        Assert.Equal("Summarize", result.StrategyUsed);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIncludeContextPromptIfProvided()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var contextPrompt = "Focus on technical details";
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
            .Returns("Summary");

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
        var content = CreateSampleAnalyzedContent("This is original content with 100 characters exactly for testing purposes and metrics validation.");
        var summary = "Short summary."; // 15 characters
        var options = new ReconstructOptions { UseLLM = true };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(summary);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.ProcessingTimeMs >= 0);
        Assert.Equal(1, result.Metrics.LLMCallCount);
        Assert.True(result.Metrics.TokensUsed > 0);
        Assert.True(result.Metrics.CompressionRatio < 1.0); // 요약되었으므로
    }

    [Fact]
    public async Task ApplyAsync_ShouldCreateEnhancement()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var summary = "Test summary";
        var options = new ReconstructOptions { UseLLM = true };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(summary);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.Single(result.Enhancements);
        Assert.Equal("Summary", result.Enhancements[0].Type);
        Assert.Equal(summary, result.Enhancements[0].Content);
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
            .Returns("Summary");

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
        var content = CreateSampleAnalyzedContent(new string('a', 1000)); // 1000 characters
        var options = new ReconstructOptions();

        // Act
        var estimate = _strategy.EstimateProcessingTime(content, options);

        // Assert
        Assert.True(estimate > TimeSpan.Zero);
        Assert.True(estimate < TimeSpan.FromMinutes(1)); // Should be reasonable
    }

    [Fact]
    public async Task ApplyAsync_WithDifferentSummaryRatios_ShouldAdjustTargetLength()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent(new string('a', 1000));

        var options1 = new ReconstructOptions { UseLLM = true, SummaryRatio = 0.1 };
        var options2 = new ReconstructOptions { UseLLM = true, SummaryRatio = 0.5 };

        _mockLlmService
            .CompleteAsync(
                Arg.Any<string>(),
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("Summary");

        // Act
        await _strategy.ApplyAsync(content, options1);
        await _strategy.ApplyAsync(content, options2);

        // Assert
        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains("100")), // 1000 * 0.1
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>());

        await _mockLlmService.Received(1)
            .CompleteAsync(
                Arg.Is<string>(p => p.Contains("500")), // 1000 * 0.5
                Arg.Any<TextCompletionOptions>(),
                Arg.Any<CancellationToken>());
    }

    private static AnalyzedContent CreateSampleAnalyzedContent(string? customContent = null)
    {
        return new AnalyzedContent
        {
            Url = "https://example.com/test",
            CleanedContent = customContent ?? "Test content for summarization that needs to be made shorter while preserving key information.",
            Title = "Test Document",
            Metadata = new WebContentMetadata
            {
                Title = "Test Document"
            }
        };
    }
}
