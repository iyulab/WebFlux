using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Strategies.Reconstruct;
using Xunit;

namespace WebFlux.Tests.Strategies.Reconstruct;

/// <summary>
/// NoneReconstructStrategy 테스트
/// 100% 커버리지 목표
/// </summary>
public class NoneReconstructStrategyTests
{
    private readonly NoneReconstructStrategy _strategy;

    public NoneReconstructStrategyTests()
    {
        _strategy = new NoneReconstructStrategy();
    }

    [Fact]
    public void Name_ShouldReturnNone()
    {
        // Act
        var name = _strategy.Name;

        // Assert
        Assert.Equal("None", name);
    }

    [Fact]
    public void Description_ShouldNotBeEmpty()
    {
        // Act
        var description = _strategy.Description;

        // Assert
        Assert.NotEmpty(description);
        Assert.Contains("재구성 없이", description);
    }

    [Fact]
    public void RecommendedUseCases_ShouldReturnMultipleCases()
    {
        // Act
        var useCases = _strategy.RecommendedUseCases.ToList();

        // Assert
        Assert.NotEmpty(useCases);
        Assert.Equal(3, useCases.Count);
        Assert.Contains(useCases, uc => uc.Contains("빠른 처리"));
        Assert.Contains(useCases, uc => uc.Contains("원본 콘텐츠 품질"));
        Assert.Contains(useCases, uc => uc.Contains("LLM 비용"));
    }

    [Fact]
    public void IsApplicable_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions();

        // Act
        var result = _strategy.IsApplicable(content, options);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ApplyAsync_ShouldReturnOriginalContent()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions();

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content.Url, result.Url);
        Assert.Equal(content.CleanedContent, result.ReconstructedText);
        Assert.Equal(content.CleanedContent, result.OriginalContent);
        Assert.Equal("None", result.StrategyUsed);
        Assert.False(result.UsedLLM);
    }

    [Fact]
    public async Task ApplyAsync_ShouldSetMetrics()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions();

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result.Metrics);
        Assert.Equal(1.0, result.Metrics.Quality);
        Assert.Equal(1.0, result.Metrics.CompressionRatio);
        Assert.Equal(0, result.Metrics.ProcessingTimeMs);
        Assert.Equal(0, result.Metrics.LLMCallCount);
    }

    [Fact]
    public async Task ApplyAsync_WithCancellation_ShouldComplete()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _strategy.ApplyAsync(content, options, cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void EstimateProcessingTime_ShouldReturnZero()
    {
        // Arrange
        var content = CreateSampleAnalyzedContent();
        var options = new ReconstructOptions();

        // Act
        var estimate = _strategy.EstimateProcessingTime(content, options);

        // Assert
        Assert.Equal(TimeSpan.Zero, estimate);
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyContent_ShouldHandleGracefully()
    {
        // Arrange
        var content = new AnalyzedContent
        {
            Url = "https://example.com",
            CleanedContent = "",
            Metadata = new WebContentMetadata()
        };
        var options = new ReconstructOptions();

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ReconstructedText);
    }

    [Fact]
    public async Task ApplyAsync_WithLongContent_ShouldPreserveAll()
    {
        // Arrange
        var longText = string.Join("\n", Enumerable.Repeat("This is a test paragraph with meaningful content.", 100));
        var content = new AnalyzedContent
        {
            Url = "https://example.com/long",
            CleanedContent = longText,
            Metadata = new WebContentMetadata()
        };
        var options = new ReconstructOptions();

        // Act
        var result = await _strategy.ApplyAsync(content, options);

        // Assert
        Assert.Equal(longText, result.ReconstructedText);
        Assert.Equal(1.0, result.Metrics.CompressionRatio);
    }

    private static AnalyzedContent CreateSampleAnalyzedContent()
    {
        return new AnalyzedContent
        {
            Url = "https://example.com/test",
            RawContent = "<html><body>Test content</body></html>",
            CleanedContent = "Test content for reconstruction",
            Title = "Test Document",
            Sections = new List<ContentSection>
            {
                new ContentSection
                {
                    Heading = "Introduction",
                    Level = 1,
                    Content = "This is test content"
                }
            },
            Metadata = new WebContentMetadata
            {
                Title = "Test Document",
                Author = "Test Author"
            },
            Metrics = new AnalysisMetrics
            {
                ContentQuality = 0.9
            }
        };
    }
}
