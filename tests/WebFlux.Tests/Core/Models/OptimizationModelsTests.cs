using WebFlux.Core.Models;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Models;

/// <summary>
/// 최적화 관련 모델 단위 테스트
/// Optimization models 검증
/// </summary>
public class OptimizationModelsTests
{
    #region BottleneckInfo Tests

    [Fact]
    public void BottleneckInfo_ShouldInitializeWithDefaults()
    {
        // Act
        var bottleneck = new BottleneckInfo();

        // Assert
        bottleneck.Name.Should().Be(string.Empty);
        bottleneck.Type.Should().Be(default(BottleneckType));
        bottleneck.Severity.Should().Be(0);
        bottleneck.AverageProcessingTime.Should().Be(TimeSpan.Zero);
        bottleneck.Throughput.Should().Be(0);
    }

    [Fact]
    public void BottleneckInfo_CPU_ShouldIndicateHighSeverity()
    {
        // Arrange & Act
        var bottleneck = new BottleneckInfo
        {
            Name = "CPU-intensive parsing",
            Type = BottleneckType.CPU,
            Severity = 9,
            AverageProcessingTime = TimeSpan.FromMilliseconds(500),
            Throughput = 2.0
        };

        // Assert
        bottleneck.Type.Should().Be(BottleneckType.CPU);
        bottleneck.Severity.Should().Be(9);
        bottleneck.AverageProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
        bottleneck.Throughput.Should().Be(2.0);
    }

    [Fact]
    public void BottleneckInfo_Memory_ShouldIndicateMemoryPressure()
    {
        // Arrange & Act
        var bottleneck = new BottleneckInfo
        {
            Name = "Large file processing",
            Type = BottleneckType.Memory,
            Severity = 7,
            AverageProcessingTime = TimeSpan.FromSeconds(3),
            Throughput = 0.5
        };

        // Assert
        bottleneck.Type.Should().Be(BottleneckType.Memory);
        bottleneck.Severity.Should().BeInRange(1, 10);
    }

    [Theory]
    [InlineData(BottleneckType.CPU)]
    [InlineData(BottleneckType.Memory)]
    [InlineData(BottleneckType.IO)]
    [InlineData(BottleneckType.Network)]
    [InlineData(BottleneckType.ExternalService)]
    public void BottleneckInfo_AllTypes_ShouldBeValid(BottleneckType type)
    {
        // Arrange & Act
        var bottleneck = new BottleneckInfo
        {
            Name = $"{type} bottleneck",
            Type = type,
            Severity = 5
        };

        // Assert
        bottleneck.Type.Should().Be(type);
    }

    #endregion

    #region AppliedOptimization Tests

    [Fact]
    public void AppliedOptimization_ShouldInitializeWithDefaults()
    {
        // Act
        var optimization = new AppliedOptimization();

        // Assert
        optimization.Name.Should().Be(string.Empty);
        optimization.Type.Should().Be(default(OptimizationType));
        optimization.BeforeMetric.Should().Be(0);
        optimization.AfterMetric.Should().Be(0);
        optimization.ImprovementPercent.Should().Be(0);
    }

    [Fact]
    public void AppliedOptimization_Performance_ShouldShowImprovement()
    {
        // Arrange & Act
        var optimization = new AppliedOptimization
        {
            Name = "Parallel processing",
            Type = OptimizationType.Performance,
            BeforeMetric = 1000.0,
            AfterMetric = 600.0,
            ImprovementPercent = 40.0
        };

        // Assert
        optimization.Type.Should().Be(OptimizationType.Performance);
        optimization.BeforeMetric.Should().Be(1000.0);
        optimization.AfterMetric.Should().Be(600.0);
        optimization.ImprovementPercent.Should().Be(40.0);
        optimization.AfterMetric.Should().BeLessThan(optimization.BeforeMetric);
    }

    [Fact]
    public void AppliedOptimization_Memory_ShouldReduceUsage()
    {
        // Arrange & Act
        var optimization = new AppliedOptimization
        {
            Name = "Memory pooling",
            Type = OptimizationType.Memory,
            BeforeMetric = 1024.0 * 1024 * 500, // 500 MB
            AfterMetric = 1024.0 * 1024 * 200,  // 200 MB
            ImprovementPercent = 60.0
        };

        // Assert
        optimization.Type.Should().Be(OptimizationType.Memory);
        optimization.ImprovementPercent.Should().Be(60.0);
    }

    #endregion

    #region BottleneckOptimizationResult Tests

    [Fact]
    public void BottleneckOptimizationResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new BottleneckOptimizationResult();

        // Assert
        result.IdentifiedBottlenecks.Should().NotBeNull().And.BeEmpty();
        result.AppliedOptimizations.Should().NotBeNull().And.BeEmpty();
        result.ExpectedPerformanceImprovement.Should().Be(0);
    }

    [Fact]
    public void BottleneckOptimizationResult_WithBottlenecks_ShouldProposeOptimizations()
    {
        // Arrange
        var bottlenecks = new List<BottleneckInfo>
        {
            new BottleneckInfo
            {
                Name = "CPU parsing",
                Type = BottleneckType.CPU,
                Severity = 8
            },
            new BottleneckInfo
            {
                Name = "Memory allocation",
                Type = BottleneckType.Memory,
                Severity = 6
            }
        };

        var optimizations = new List<AppliedOptimization>
        {
            new AppliedOptimization
            {
                Name = "Parallel processing",
                Type = OptimizationType.Performance,
                ImprovementPercent = 35.0
            }
        };

        // Act
        var result = new BottleneckOptimizationResult
        {
            IdentifiedBottlenecks = bottlenecks,
            AppliedOptimizations = optimizations,
            ExpectedPerformanceImprovement = 35.0
        };

        // Assert
        result.IdentifiedBottlenecks.Should().HaveCount(2);
        result.AppliedOptimizations.Should().HaveCount(1);
        result.ExpectedPerformanceImprovement.Should().Be(35.0);
    }

    #endregion

    #region CacheOptimizationResult Tests

    [Fact]
    public void CacheOptimizationResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new CacheOptimizationResult();

        // Assert
        result.CacheHit.Should().BeFalse();
        result.OptimizedCacheKey.Should().Be(string.Empty);
        result.RecommendedTTL.Should().Be(TimeSpan.Zero);
        result.SpaceSavedBytes.Should().Be(0);
        result.PerformanceGainEstimate.Should().Be(0);
    }

    [Fact]
    public void CacheOptimizationResult_CacheHit_ShouldIndicateOptimization()
    {
        // Arrange & Act
        var result = new CacheOptimizationResult
        {
            CacheHit = true,
            OptimizedCacheKey = "optimized:user:12345",
            RecommendedTTL = TimeSpan.FromMinutes(30),
            SpaceSavedBytes = 1024 * 50,
            PerformanceGainEstimate = 0.85
        };

        // Assert
        result.CacheHit.Should().BeTrue();
        result.OptimizedCacheKey.Should().NotBeEmpty();
        result.RecommendedTTL.Should().Be(TimeSpan.FromMinutes(30));
        result.SpaceSavedBytes.Should().BeGreaterThan(0);
        result.PerformanceGainEstimate.Should().BeInRange(0, 1);
    }

    [Fact]
    public void CacheOptimizationResult_CacheMiss_ShouldSuggestCaching()
    {
        // Arrange & Act
        var result = new CacheOptimizationResult
        {
            CacheHit = false,
            OptimizedCacheKey = "suggested:cache:key",
            RecommendedTTL = TimeSpan.FromHours(1),
            SpaceSavedBytes = 0,
            PerformanceGainEstimate = 0.75
        };

        // Assert
        result.CacheHit.Should().BeFalse();
        result.PerformanceGainEstimate.Should().BeGreaterThan(0);
    }

    #endregion

    #region OptimizationStatistics Tests

    [Fact]
    public void OptimizationStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new OptimizationStatistics();

        // Assert
        stats.TotalOptimizationRequests.Should().Be(0);
        stats.SuccessfulOptimizations.Should().Be(0);
        stats.AveragePerformanceImprovement.Should().Be(0);
        stats.TotalResourceSavings.Should().NotBeNull();
        stats.SuccessRate.Should().Be(0);
        stats.CollectionPeriod.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void OptimizationStatistics_SuccessRate_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var stats = new OptimizationStatistics
        {
            TotalOptimizationRequests = 100,
            SuccessfulOptimizations = 85,
            AveragePerformanceImprovement = 42.5,
            CollectionPeriod = TimeSpan.FromDays(7)
        };

        // Assert
        stats.SuccessRate.Should().BeApproximately(0.85, 0.001);
        stats.AveragePerformanceImprovement.Should().Be(42.5);
    }

    [Fact]
    public void OptimizationStatistics_ZeroRequests_ShouldHaveZeroSuccessRate()
    {
        // Arrange & Act
        var stats = new OptimizationStatistics
        {
            TotalOptimizationRequests = 0,
            SuccessfulOptimizations = 0
        };

        // Assert
        stats.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void OptimizationStatistics_WithResourceSavings_ShouldTrackBenefits()
    {
        // Arrange & Act
        var stats = new OptimizationStatistics
        {
            TotalOptimizationRequests = 1000,
            SuccessfulOptimizations = 950,
            AveragePerformanceImprovement = 35.5,
            TotalResourceSavings = new ResourceSavings
            {
                CpuSavingsPercent = 25.0,
                MemorySavingsPercent = 40.0,
                ProcessingTimeSavingsPercent = 30.0,
                EstimatedCostSavings = 1500.00
            },
            CollectionPeriod = TimeSpan.FromDays(30)
        };

        // Assert
        stats.SuccessRate.Should().BeApproximately(0.95, 0.001);
        stats.TotalResourceSavings.CpuSavingsPercent.Should().Be(25.0);
        stats.TotalResourceSavings.MemorySavingsPercent.Should().Be(40.0);
        stats.TotalResourceSavings.EstimatedCostSavings.Should().Be(1500.00);
    }

    #endregion

    #region OptimizationSuggestion Tests

    [Fact]
    public void OptimizationSuggestion_ShouldInitializeWithDefaults()
    {
        // Act
        var suggestion = new OptimizationSuggestion();

        // Assert
        suggestion.Type.Should().Be(default(OptimizationType));
        suggestion.Description.Should().Be(string.Empty);
        suggestion.ExpectedImpact.Should().Be(string.Empty);
        suggestion.Priority.Should().Be(0);
    }

    [Fact]
    public void OptimizationSuggestion_Performance_ShouldHaveHighPriority()
    {
        // Arrange & Act
        var suggestion = new OptimizationSuggestion
        {
            Type = OptimizationType.Performance,
            Description = "Enable parallel processing for large documents",
            ExpectedImpact = "40% reduction in processing time",
            Priority = 1
        };

        // Assert
        suggestion.Type.Should().Be(OptimizationType.Performance);
        suggestion.Priority.Should().Be(1);
        suggestion.ExpectedImpact.Should().Contain("40%");
    }

    [Theory]
    [InlineData(OptimizationType.Performance, "Speed up processing", 1)]
    [InlineData(OptimizationType.Quality, "Improve chunk quality", 2)]
    [InlineData(OptimizationType.Memory, "Reduce memory usage", 3)]
    [InlineData(OptimizationType.Cost, "Lower API costs", 4)]
    public void OptimizationSuggestion_AllTypes_ShouldBeValid(
        OptimizationType type, string description, int priority)
    {
        // Arrange & Act
        var suggestion = new OptimizationSuggestion
        {
            Type = type,
            Description = description,
            Priority = priority
        };

        // Assert
        suggestion.Type.Should().Be(type);
        suggestion.Description.Should().Be(description);
        suggestion.Priority.Should().Be(priority);
    }

    #endregion

    #region AlternativeStrategy Tests

    [Fact]
    public void AlternativeStrategy_ShouldInitializeWithDefaults()
    {
        // Act
        var alternative = new AlternativeStrategy();

        // Assert
        alternative.Strategy.Should().Be(default(ChunkingStrategy));
        alternative.Score.Should().Be(0);
        alternative.UseCase.Should().Be(string.Empty);
    }

    [Fact]
    public void AlternativeStrategy_ShouldProposeAlternatives()
    {
        // Arrange & Act
        var alternative = new AlternativeStrategy
        {
            Strategy = ChunkingStrategy.Semantic,
            Score = 0.85,
            UseCase = "Technical documents with complex terminology"
        };

        // Assert
        alternative.Strategy.Should().Be(ChunkingStrategy.Semantic);
        alternative.Score.Should().BeInRange(0, 1);
        alternative.UseCase.Should().NotBeEmpty();
    }

    [Fact]
    public void AlternativeStrategy_MultipleOptions_ShouldBeComparable()
    {
        // Arrange
        var option1 = new AlternativeStrategy
        {
            Strategy = ChunkingStrategy.Smart,
            Score = 0.90,
            UseCase = "General purpose"
        };

        var option2 = new AlternativeStrategy
        {
            Strategy = ChunkingStrategy.MemoryOptimized,
            Score = 0.75,
            UseCase = "Large files"
        };

        // Assert
        option1.Score.Should().BeGreaterThan(option2.Score);
    }

    #endregion

    #region ResourceOptimizationSuggestion Tests

    [Fact]
    public void ResourceOptimizationSuggestion_ShouldInitializeWithDefaults()
    {
        // Act
        var suggestion = new ResourceOptimizationSuggestion();

        // Assert
        suggestion.CpuUtilization.Should().Be(0);
        suggestion.MemoryUtilization.Should().Be(0);
        suggestion.Suggestions.Should().NotBeNull().And.BeEmpty();
        suggestion.ExpectedSavings.Should().NotBeNull();
    }

    [Fact]
    public void ResourceOptimizationSuggestion_HighUtilization_ShouldSuggestOptimizations()
    {
        // Arrange
        var suggestions = new List<OptimizationSuggestion>
        {
            new OptimizationSuggestion
            {
                Type = OptimizationType.Performance,
                Description = "Reduce CPU usage",
                Priority = 1
            },
            new OptimizationSuggestion
            {
                Type = OptimizationType.Memory,
                Description = "Optimize memory allocation",
                Priority = 2
            }
        };

        // Act
        var resourceSuggestion = new ResourceOptimizationSuggestion
        {
            CpuUtilization = 85.5,
            MemoryUtilization = 75.0,
            Suggestions = suggestions,
            ExpectedSavings = new ResourceSavings
            {
                CpuSavingsPercent = 20.0,
                MemorySavingsPercent = 30.0,
                ProcessingTimeSavingsPercent = 25.0,
                EstimatedCostSavings = 500.00
            }
        };

        // Assert
        resourceSuggestion.CpuUtilization.Should().Be(85.5);
        resourceSuggestion.MemoryUtilization.Should().Be(75.0);
        resourceSuggestion.Suggestions.Should().HaveCount(2);
        resourceSuggestion.ExpectedSavings.CpuSavingsPercent.Should().Be(20.0);
    }

    #endregion

    #region TokenOptimizationResult Tests

    [Fact]
    public void TokenOptimizationResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new TokenOptimizationResult();

        // Assert
        result.OriginalTokenCount.Should().Be(0);
        result.OptimizedTokenCount.Should().Be(0);
        result.TokensSaved.Should().Be(0);
        result.SavingsPercent.Should().Be(0);
        result.OptimizedText.Should().Be(string.Empty);
        result.QualityRetentionScore.Should().Be(0);
    }

    [Fact]
    public void TokenOptimizationResult_ShouldReduceTokens()
    {
        // Arrange & Act
        var result = new TokenOptimizationResult
        {
            OriginalTokenCount = 1000,
            OptimizedTokenCount = 700,
            TokensSaved = 300,
            SavingsPercent = 30.0,
            OptimizedText = "Optimized content...",
            QualityRetentionScore = 0.95
        };

        // Assert
        result.OriginalTokenCount.Should().Be(1000);
        result.OptimizedTokenCount.Should().Be(700);
        result.TokensSaved.Should().Be(300);
        result.SavingsPercent.Should().Be(30.0);
        result.QualityRetentionScore.Should().BeInRange(0, 1);
        result.OptimizedTokenCount.Should().BeLessThan(result.OriginalTokenCount);
    }

    [Fact]
    public void TokenOptimizationResult_HighQualityRetention_ShouldMaintainContent()
    {
        // Arrange & Act
        var result = new TokenOptimizationResult
        {
            OriginalTokenCount = 500,
            OptimizedTokenCount = 450,
            TokensSaved = 50,
            SavingsPercent = 10.0,
            OptimizedText = "High quality optimized text",
            QualityRetentionScore = 0.98
        };

        // Assert
        result.QualityRetentionScore.Should().BeGreaterThan(0.95);
        result.SavingsPercent.Should().BePositive();
    }

    #endregion

    #region ResourceSavings Tests

    [Fact]
    public void ResourceSavings_ShouldInitializeWithDefaults()
    {
        // Act
        var savings = new ResourceSavings();

        // Assert
        savings.CpuSavingsPercent.Should().Be(0);
        savings.MemorySavingsPercent.Should().Be(0);
        savings.ProcessingTimeSavingsPercent.Should().Be(0);
        savings.EstimatedCostSavings.Should().Be(0);
    }

    [Fact]
    public void ResourceSavings_ShouldTrackMultipleSavings()
    {
        // Arrange & Act
        var savings = new ResourceSavings
        {
            CpuSavingsPercent = 25.5,
            MemorySavingsPercent = 35.0,
            ProcessingTimeSavingsPercent = 40.0,
            EstimatedCostSavings = 1200.50
        };

        // Assert
        savings.CpuSavingsPercent.Should().Be(25.5);
        savings.MemorySavingsPercent.Should().Be(35.0);
        savings.ProcessingTimeSavingsPercent.Should().Be(40.0);
        savings.EstimatedCostSavings.Should().Be(1200.50);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AllOptimizationModels_ShouldBeIndependentlyInstantiable()
    {
        // Act
        var bottleneck = new BottleneckInfo();
        var optimization = new AppliedOptimization();
        var bottleneckResult = new BottleneckOptimizationResult();
        var cacheResult = new CacheOptimizationResult();
        var stats = new OptimizationStatistics();
        var suggestion = new OptimizationSuggestion();
        var alternative = new AlternativeStrategy();
        var resourceSuggestion = new ResourceOptimizationSuggestion();
        var tokenResult = new TokenOptimizationResult();
        var savings = new ResourceSavings();

        // Assert
        bottleneck.Should().NotBeNull();
        optimization.Should().NotBeNull();
        bottleneckResult.Should().NotBeNull();
        cacheResult.Should().NotBeNull();
        stats.Should().NotBeNull();
        suggestion.Should().NotBeNull();
        alternative.Should().NotBeNull();
        resourceSuggestion.Should().NotBeNull();
        tokenResult.Should().NotBeNull();
        savings.Should().NotBeNull();
    }

    [Fact]
    public void OptimizationWorkflow_ShouldCombineMultipleResults()
    {
        // Arrange - Identify bottlenecks
        var bottlenecks = new List<BottleneckInfo>
        {
            new BottleneckInfo
            {
                Name = "CPU parsing",
                Type = BottleneckType.CPU,
                Severity = 8,
                AverageProcessingTime = TimeSpan.FromMilliseconds(500),
                Throughput = 2.0
            }
        };

        // Apply optimizations
        var optimizations = new List<AppliedOptimization>
        {
            new AppliedOptimization
            {
                Name = "Parallel processing",
                Type = OptimizationType.Performance,
                BeforeMetric = 500.0,
                AfterMetric = 300.0,
                ImprovementPercent = 40.0
            }
        };

        // Act - Create optimization result
        var result = new BottleneckOptimizationResult
        {
            IdentifiedBottlenecks = bottlenecks,
            AppliedOptimizations = optimizations,
            ExpectedPerformanceImprovement = 40.0
        };

        // Assert
        result.IdentifiedBottlenecks.Should().HaveCount(1);
        result.AppliedOptimizations.Should().HaveCount(1);
        result.ExpectedPerformanceImprovement.Should().BeGreaterThan(0);
    }

    #endregion
}
