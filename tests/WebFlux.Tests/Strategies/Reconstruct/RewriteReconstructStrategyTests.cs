using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Strategies.Reconstruct;
using Xunit;
using Moq;

namespace WebFlux.Tests.Strategies.Reconstruct;

/// <summary>
/// RewriteReconstructStrategy 테스트
/// 100% 커버리지 목표
/// </summary>
public class RewriteReconstructStrategyTests
{
    private readonly Mock<ITextCompletionService> _mockLlmService;
    private readonly RewriteReconstructStrategy _strategy;

    public RewriteReconstructStrategyTests()
    {
        _mockLlmService = new Mock<ITextCompletionService>();
        _strategy = new RewriteReconstructStrategy(_mockLlmService.Object);
    }

    [Fact]
    public void Name_ShouldReturnRewrite()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("Rewrite", name);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        Assert.NotEmpty(description);
        Assert.Contains("재작성", description);
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
        var strategyWithoutLlm = new RewriteReconstructStrategy(null);
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
        var strategyWithoutLlm = new RewriteReconstructStrategy(null);
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
            RewriteStyle = "Technical",
            MaxTokens = 2500,
            Temperature = 0.4
        };

        var expectedRewrite = "This is a clear, well-structured rewrite of the original content.";
        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRewrite);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.Is<string>(p => p.Contains("rewrite", StringComparison.OrdinalIgnoreCase)),
                It.Is<TextCompletionOptions>(o => o.MaxTokens == 2500 && Math.Abs(o.Temperature - 0.4f) < 0.001f),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(expectedRewrite, result.ReconstructedText);
        Assert.True(result.UsedLLM);
        Assert.Equal("Rewrite", result.StrategyUsed);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIncludeRewriteStyleInPrompt()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            RewriteStyle = "Casual"
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rewritten content");

        // Act
        await _strategy.ApplyAsync(content, options);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.Is<string>(p => p.Contains("casual") || p.Contains("conversational")),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_ShouldIncludeContextPromptIfProvided()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var contextPrompt = "Focus on readability";
        var options = new ReconstructOptions
        {
            UseLLM = true,
            ContextPrompt = contextPrompt
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rewritten");

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
        var content = CreateSampleAnalyzedContent("Original content with some complexity.");
        var rewrite = "Simplified rewritten version.";
        var options = new ReconstructOptions { UseLLM = true };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rewrite);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.ProcessingTimeMs >= 0);
        Assert.Equal(1, result.Metrics.LLMCallCount);
        Assert.True(result.Metrics.TokensUsed > 0);
    }

    [Fact]
    public async Task ApplyAsync_ShouldCreateEnhancement()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var rewrite = "Rewritten for clarity";
        var options = new ReconstructOptions { UseLLM = true };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rewrite);

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.Single(result.Enhancements);
        Assert.Equal("Rewrite", result.Enhancements[0].Type);
        Assert.Equal(rewrite, result.Enhancements[0].Content);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellation_ShouldPassTokenToLLM()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions { UseLLM = true };
        var cts = new CancellationTokenSource();

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rewritten");

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
        var options = new ReconstructOptions();

        // Act
        var estimate = _strategy.EstimateProcessingTime(content, options);

        // Assert
        Assert.True(estimate > TimeSpan.Zero);
        Assert.True(estimate < TimeSpan.FromMinutes(2));
    }

    [Theory]
    [InlineData("Formal", "formal")]
    [InlineData("Casual", "conversational")]
    [InlineData("Technical", "technical")]
    [InlineData("Simple", "simple")]
    public async Task ApplyAsync_WithDifferentStyles_ShouldIncludeStyleInPrompt(string style, string expectedInPrompt)
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions
        {
            UseLLM = true,
            RewriteStyle = style
        };

        _mockLlmService
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Rewritten");

        // Act
        await _strategy.ApplyAsync(content, options);

        // Assert
        _mockLlmService.Verify(
            x => x.CompleteAsync(
                It.Is<string>(p => p.Contains(expectedInPrompt, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<TextCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AnalyzedContent CreateSampleAnalyzedContent(string? customContent = null)
    {
        return new AnalyzedContent
        {
            Url = "https://example.com/test",
            CleanedContent = customContent ?? "Content that may benefit from rewriting for improved clarity and structure.",
            Title = "Test Document",
            Metadata = new WebContentMetadata
            {
                Title = "Test Document"
            }
        };
    }
}
