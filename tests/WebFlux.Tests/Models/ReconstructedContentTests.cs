using WebFlux.Core.Models;
using Xunit;

namespace WebFlux.Tests.Models;

/// <summary>
/// ReconstructedContent 모델 테스트
/// 100% 커버리지 목표
/// </summary>
public class ReconstructedContentTests
{
    [Fact]
    public void FromAnalyzed_ShouldCreateReconstructedContent()
    {
        // Arrange
        var analyzed = new AnalyzedContent
        {
            Url = "https://example.com",
            CleanedContent = "Test content",
            Metadata = new WebContentMetadata
            {
                Title = "Test",
                Author = "Tester"
            }
        };

        // Act
        var result = ReconstructedContent.FromAnalyzed(analyzed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(analyzed.Url, result.Url);
        Assert.Equal(analyzed.CleanedContent, result.OriginalContent);
        Assert.Equal(analyzed.CleanedContent, result.ReconstructedText);
        Assert.Equal("None", result.StrategyUsed);
        Assert.False(result.UsedLLM);
        Assert.Equal(analyzed.Metadata, result.Metadata);
    }

    [Fact]
    public void FromAnalyzed_ShouldSetReconstructedAtToCurrentTime()
    {
        // Arrange
        var analyzed = new AnalyzedContent
        {
            Url = "https://example.com",
            CleanedContent = "Test"
        };
        var before = DateTime.UtcNow;

        // Act
        var result = ReconstructedContent.FromAnalyzed(analyzed);
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(result.ReconstructedAt, before, after.AddSeconds(1));
    }

    [Fact]
    public void ReconstructedContent_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var content = new ReconstructedContent();

        // Assert
        Assert.Empty(content.Url);
        Assert.Empty(content.OriginalContent);
        Assert.Empty(content.ReconstructedText);
        Assert.Equal("None", content.StrategyUsed);
        Assert.Empty(content.Enhancements);
        Assert.NotNull(content.Metadata);
        Assert.NotNull(content.Metrics);
        Assert.False(content.UsedLLM);
        Assert.Null(content.LLMModel);
        Assert.NotNull(content.Properties);
        Assert.Empty(content.Properties);
    }

    [Fact]
    public void Enhancements_CanBeAddedAndRetrieved()
    {
        // Arrange
        var content = new ReconstructedContent();
        var enhancement = new ContentEnhancement("Summary", "Test summary");

        // Act
        content.Enhancements.Add(enhancement);

        // Assert
        Assert.Single(content.Enhancements);
        Assert.Equal("Summary", content.Enhancements[0].Type);
        Assert.Equal("Test summary", content.Enhancements[0].Content);
    }

    [Fact]
    public void Properties_CanStoreCustomData()
    {
        // Arrange
        var content = new ReconstructedContent();

        // Act
        content.Properties["CustomKey"] = "CustomValue";
        content.Properties["NumericKey"] = 42;

        // Assert
        Assert.Equal(2, content.Properties.Count);
        Assert.Equal("CustomValue", content.Properties["CustomKey"]);
        Assert.Equal(42, content.Properties["NumericKey"]);
    }

    [Fact]
    public void Metrics_CanBeSetAndRetrieved()
    {
        // Arrange
        var content = new ReconstructedContent();
        var metrics = new ReconstructMetrics
        {
            Quality = 0.95,
            CompressionRatio = 0.5,
            ProcessingTimeMs = 1500,
            LLMCallCount = 2,
            TokensUsed = 500
        };

        // Act
        content.Metrics = metrics;

        // Assert
        Assert.Equal(0.95, content.Metrics.Quality);
        Assert.Equal(0.5, content.Metrics.CompressionRatio);
        Assert.Equal(1500, content.Metrics.ProcessingTimeMs);
        Assert.Equal(2, content.Metrics.LLMCallCount);
        Assert.Equal(500, content.Metrics.TokensUsed);
    }
}

/// <summary>
/// ContentEnhancement 모델 테스트
/// </summary>
public class ContentEnhancementTests
{
    [Fact]
    public void Constructor_WithTypeAndContent_ShouldSetProperties()
    {
        // Act
        var enhancement = new ContentEnhancement("Summary", "Test content");

        // Assert
        Assert.Equal("Summary", enhancement.Type);
        Assert.Equal("Test content", enhancement.Content);
        Assert.Null(enhancement.Position);
        Assert.Equal(1.0, enhancement.Confidence);
    }

    [Fact]
    public void Constructor_WithPosition_ShouldSetPosition()
    {
        // Act
        var enhancement = new ContentEnhancement("Context", "Additional info", 100);

        // Assert
        Assert.Equal("Context", enhancement.Type);
        Assert.Equal("Additional info", enhancement.Content);
        Assert.Equal(100, enhancement.Position);
    }

    [Fact]
    public void Constructor_Default_ShouldGenerateId()
    {
        // Act
        var enhancement1 = new ContentEnhancement();
        var enhancement2 = new ContentEnhancement();

        // Assert
        Assert.NotNull(enhancement1.Id);
        Assert.NotNull(enhancement2.Id);
        Assert.NotEqual(enhancement1.Id, enhancement2.Id);
    }

    [Fact]
    public void Metadata_CanStoreCustomData()
    {
        // Arrange
        var enhancement = new ContentEnhancement("Test", "Content");

        // Act
        enhancement.Metadata["Source"] = "LLM";
        enhancement.Metadata["Model"] = "GPT-4";

        // Assert
        Assert.Equal(2, enhancement.Metadata.Count);
        Assert.Equal("LLM", enhancement.Metadata["Source"]);
        Assert.Equal("GPT-4", enhancement.Metadata["Model"]);
    }

    [Fact]
    public void Confidence_DefaultValue_ShouldBeOne()
    {
        // Act
        var enhancement = new ContentEnhancement();

        // Assert
        Assert.Equal(1.0, enhancement.Confidence);
    }
}

/// <summary>
/// ReconstructMetrics 모델 테스트
/// </summary>
public class ReconstructMetricsTests
{
    [Fact]
    public void ReconstructMetrics_DefaultValues_ShouldBeZero()
    {
        // Act
        var metrics = new ReconstructMetrics();

        // Assert
        Assert.Equal(0, metrics.Quality);
        Assert.Equal(0, metrics.CompressionRatio);
        Assert.Equal(0, metrics.EnhancementBytes);
        Assert.Equal(0, metrics.ProcessingTimeMs);
        Assert.Equal(0, metrics.LLMCallCount);
        Assert.Null(metrics.TokensUsed);
        Assert.NotNull(metrics.AdditionalMetrics);
        Assert.Empty(metrics.AdditionalMetrics);
    }

    [Fact]
    public void AdditionalMetrics_CanStoreCustomMetrics()
    {
        // Arrange
        var metrics = new ReconstructMetrics();

        // Act
        metrics.AdditionalMetrics["CustomMetric1"] = 0.85;
        metrics.AdditionalMetrics["CustomMetric2"] = 0.92;

        // Assert
        Assert.Equal(2, metrics.AdditionalMetrics.Count);
        Assert.Equal(0.85, metrics.AdditionalMetrics["CustomMetric1"]);
        Assert.Equal(0.92, metrics.AdditionalMetrics["CustomMetric2"]);
    }

    [Fact]
    public void TokensUsed_CanBeNull()
    {
        // Arrange
        var metrics = new ReconstructMetrics();

        // Act & Assert
        Assert.Null(metrics.TokensUsed);

        // Set value
        metrics.TokensUsed = 1000;
        Assert.Equal(1000, metrics.TokensUsed);
    }
}
