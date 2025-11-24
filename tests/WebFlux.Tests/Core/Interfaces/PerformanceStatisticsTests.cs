using WebFlux.Core.Interfaces;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Interfaces;

/// <summary>
/// 성능 통계 모델 단위 테스트
/// IPerformanceMonitor 관련 통계 클래스 검증
/// </summary>
public class PerformanceStatisticsTests
{
    #region PerformanceStatistics Tests

    [Fact]
    public void PerformanceStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new PerformanceStatistics();

        // Assert
        stats.TotalRequests.Should().Be(0);
        stats.ErrorRate.Should().Be(0);
        stats.Throughput.Should().Be(0);
        stats.MemoryUsage.Should().Be(0);
        stats.CpuUsage.Should().Be(0);
        stats.CacheHitRatio.Should().Be(0);
        stats.AverageChunkQuality.Should().Be(0);
        stats.ActiveActivities.Should().Be(0);
    }

    [Fact]
    public void PerformanceStatistics_ShouldInitializeNestedObjects()
    {
        // Act
        var stats = new PerformanceStatistics();

        // Assert
        stats.SystemMetrics.Should().NotBeNull();
        stats.ChunkingStatistics.Should().NotBeNull();
        stats.ProcessingStatistics.Should().NotBeNull();
        stats.ResourceStatistics.Should().NotBeNull();
        stats.DetailedMetrics.Should().NotBeNull();
        stats.DetailedMetrics.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceStatistics_Duration_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var stats = new PerformanceStatistics
        {
            StartTime = startTime
        };

        // Act
        var duration = stats.Duration;

        // Assert
        duration.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PerformanceStatistics_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var stats = new PerformanceStatistics
        {
            StartTime = DateTimeOffset.UtcNow,
            TotalRequests = 1000,
            AverageResponseTime = TimeSpan.FromMilliseconds(150),
            ErrorRate = 0.02,
            Throughput = 100.5,
            MemoryUsage = 1024 * 1024 * 512,
            CpuUsage = 45.5,
            CacheHitRatio = 0.85,
            AverageChunkQuality = 0.92,
            ActiveActivities = 5
        };

        // Assert
        stats.TotalRequests.Should().Be(1000);
        stats.AverageResponseTime.Should().Be(TimeSpan.FromMilliseconds(150));
        stats.ErrorRate.Should().Be(0.02);
        stats.Throughput.Should().Be(100.5);
        stats.MemoryUsage.Should().Be(1024 * 1024 * 512);
        stats.CpuUsage.Should().Be(45.5);
        stats.CacheHitRatio.Should().Be(0.85);
        stats.AverageChunkQuality.Should().Be(0.92);
        stats.ActiveActivities.Should().Be(5);
    }

    [Fact]
    public void PerformanceStatistics_DetailedMetrics_ShouldAllowModification()
    {
        // Arrange
        var stats = new PerformanceStatistics();

        // Act
        stats.DetailedMetrics["custom_metric_1"] = 123.45;
        stats.DetailedMetrics["custom_metric_2"] = "test_value";

        // Assert
        stats.DetailedMetrics.Should().HaveCount(2);
        stats.DetailedMetrics["custom_metric_1"].Should().Be(123.45);
        stats.DetailedMetrics["custom_metric_2"].Should().Be("test_value");
    }

    #endregion

    #region SystemMetrics Tests

    [Fact]
    public void SystemMetrics_ShouldInitializeWithDefaults()
    {
        // Act
        var metrics = new SystemMetrics();

        // Assert
        metrics.TotalMemoryBytes.Should().Be(0);
        metrics.WorkingSetBytes.Should().Be(0);
        metrics.CpuUsagePercent.Should().Be(0);
        metrics.ActiveThreads.Should().Be(0);
    }

    [Fact]
    public void SystemMetrics_GCCollections_ShouldInitializeAsEmptyDictionary()
    {
        // Act
        var metrics = new SystemMetrics();

        // Assert
        metrics.GCCollections.Should().NotBeNull();
        metrics.GCCollections.Should().BeEmpty();
    }

    [Fact]
    public void SystemMetrics_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var metrics = new SystemMetrics
        {
            TotalMemoryBytes = 16L * 1024 * 1024 * 1024,
            WorkingSetBytes = 512L * 1024 * 1024,
            CpuUsagePercent = 35.7,
            ActiveThreads = 12
        };

        // Assert
        metrics.TotalMemoryBytes.Should().Be(16L * 1024 * 1024 * 1024);
        metrics.WorkingSetBytes.Should().Be(512L * 1024 * 1024);
        metrics.CpuUsagePercent.Should().Be(35.7);
        metrics.ActiveThreads.Should().Be(12);
    }

    [Fact]
    public void SystemMetrics_GCCollections_ShouldAllowModification()
    {
        // Arrange
        var metrics = new SystemMetrics();

        // Act
        metrics.GCCollections[0] = 100;
        metrics.GCCollections[1] = 50;
        metrics.GCCollections[2] = 10;

        // Assert
        metrics.GCCollections.Should().HaveCount(3);
        metrics.GCCollections[0].Should().Be(100);
        metrics.GCCollections[1].Should().Be(50);
        metrics.GCCollections[2].Should().Be(10);
    }

    [Fact]
    public void SystemMetrics_ShouldSupportRealisticValues()
    {
        // Arrange & Act
        var metrics = new SystemMetrics
        {
            TotalMemoryBytes = 8589934592, // 8GB
            WorkingSetBytes = 536870912,   // 512MB
            CpuUsagePercent = 42.3,
            ActiveThreads = 24
        };

        // Assert
        metrics.TotalMemoryBytes.Should().BeGreaterThan(0);
        metrics.WorkingSetBytes.Should().BeLessThan(metrics.TotalMemoryBytes);
        metrics.CpuUsagePercent.Should().BeInRange(0, 100);
        metrics.ActiveThreads.Should().BeGreaterThan(0);
    }

    #endregion

    #region ChunkingStatistics Tests

    [Fact]
    public void ChunkingStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new ChunkingStatistics();

        // Assert
        stats.TotalChunks.Should().Be(0);
        stats.AverageChunkSize.Should().Be(0);
        stats.AverageQualityScore.Should().Be(0);
    }

    [Fact]
    public void ChunkingStatistics_StrategyUsage_ShouldInitializeAsEmptyDictionary()
    {
        // Act
        var stats = new ChunkingStatistics();

        // Assert
        stats.StrategyUsage.Should().NotBeNull();
        stats.StrategyUsage.Should().BeEmpty();
    }

    [Fact]
    public void ChunkingStatistics_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var stats = new ChunkingStatistics
        {
            TotalChunks = 500,
            AverageChunkSize = 1024.5,
            AverageQualityScore = 0.87
        };

        // Assert
        stats.TotalChunks.Should().Be(500);
        stats.AverageChunkSize.Should().Be(1024.5);
        stats.AverageQualityScore.Should().Be(0.87);
    }

    [Fact]
    public void ChunkingStatistics_StrategyUsage_ShouldAllowModification()
    {
        // Arrange
        var stats = new ChunkingStatistics();

        // Act
        stats.StrategyUsage["Smart"] = 150;
        stats.StrategyUsage["Intelligent"] = 200;
        stats.StrategyUsage["FixedSize"] = 100;

        // Assert
        stats.StrategyUsage.Should().HaveCount(3);
        stats.StrategyUsage["Smart"].Should().Be(150);
        stats.StrategyUsage["Intelligent"].Should().Be(200);
        stats.StrategyUsage["FixedSize"].Should().Be(100);
    }

    [Fact]
    public void ChunkingStatistics_QualityScore_ShouldBeInValidRange()
    {
        // Arrange & Act
        var stats = new ChunkingStatistics
        {
            AverageQualityScore = 0.92
        };

        // Assert
        stats.AverageQualityScore.Should().BeInRange(0, 1);
    }

    #endregion

    #region ProcessingStatistics Tests

    [Fact]
    public void ProcessingStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new ProcessingStatistics();

        // Assert
        stats.TotalProcessingTime.Should().Be(TimeSpan.Zero);
        stats.AverageProcessingTime.Should().Be(TimeSpan.Zero);
        stats.SuccessfulOperations.Should().Be(0);
        stats.FailedOperations.Should().Be(0);
    }

    [Fact]
    public void ProcessingStatistics_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var stats = new ProcessingStatistics
        {
            TotalProcessingTime = TimeSpan.FromMinutes(30),
            AverageProcessingTime = TimeSpan.FromSeconds(2.5),
            SuccessfulOperations = 850,
            FailedOperations = 15
        };

        // Assert
        stats.TotalProcessingTime.Should().Be(TimeSpan.FromMinutes(30));
        stats.AverageProcessingTime.Should().Be(TimeSpan.FromSeconds(2.5));
        stats.SuccessfulOperations.Should().Be(850);
        stats.FailedOperations.Should().Be(15);
    }

    [Fact]
    public void ProcessingStatistics_ShouldSupportCalculations()
    {
        // Arrange
        var stats = new ProcessingStatistics
        {
            SuccessfulOperations = 950,
            FailedOperations = 50
        };

        // Act
        var totalOperations = stats.SuccessfulOperations + stats.FailedOperations;
        var successRate = stats.SuccessfulOperations / (double)totalOperations;

        // Assert
        totalOperations.Should().Be(1000);
        successRate.Should().BeApproximately(0.95, 0.001);
    }

    #endregion

    #region ResourceStatistics Tests

    [Fact]
    public void ResourceStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new ResourceStatistics();

        // Assert
        stats.PeakMemoryUsage.Should().Be(0);
        stats.AverageMemoryUsage.Should().Be(0);
        stats.PeakCpuUsage.Should().Be(0);
        stats.AverageCpuUsage.Should().Be(0);
    }

    [Fact]
    public void ResourceStatistics_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var stats = new ResourceStatistics
        {
            PeakMemoryUsage = 1024L * 1024 * 1024,
            AverageMemoryUsage = 512L * 1024 * 1024,
            PeakCpuUsage = 85.5,
            AverageCpuUsage = 45.2
        };

        // Assert
        stats.PeakMemoryUsage.Should().Be(1024L * 1024 * 1024);
        stats.AverageMemoryUsage.Should().Be(512L * 1024 * 1024);
        stats.PeakCpuUsage.Should().Be(85.5);
        stats.AverageCpuUsage.Should().Be(45.2);
    }

    [Fact]
    public void ResourceStatistics_PeakValues_ShouldBeGreaterThanOrEqualToAverage()
    {
        // Arrange & Act
        var stats = new ResourceStatistics
        {
            PeakMemoryUsage = 1024L * 1024 * 800,
            AverageMemoryUsage = 512L * 1024 * 1024,
            PeakCpuUsage = 92.3,
            AverageCpuUsage = 45.7
        };

        // Assert
        stats.PeakMemoryUsage.Should().BeGreaterThanOrEqualTo(stats.AverageMemoryUsage);
        stats.PeakCpuUsage.Should().BeGreaterThanOrEqualTo(stats.AverageCpuUsage);
    }

    [Fact]
    public void ResourceStatistics_CpuUsage_ShouldBeInValidRange()
    {
        // Arrange & Act
        var stats = new ResourceStatistics
        {
            PeakCpuUsage = 98.5,
            AverageCpuUsage = 42.3
        };

        // Assert
        stats.PeakCpuUsage.Should().BeInRange(0, 100);
        stats.AverageCpuUsage.Should().BeInRange(0, 100);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PerformanceStatistics_WithAllNestedObjects_ShouldWorkTogether()
    {
        // Arrange & Act
        var stats = new PerformanceStatistics
        {
            StartTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            TotalRequests = 5000,
            SystemMetrics = new SystemMetrics
            {
                TotalMemoryBytes = 8589934592,
                CpuUsagePercent = 55.5
            },
            ChunkingStatistics = new ChunkingStatistics
            {
                TotalChunks = 1000,
                AverageQualityScore = 0.88
            },
            ProcessingStatistics = new ProcessingStatistics
            {
                SuccessfulOperations = 4800,
                FailedOperations = 200
            },
            ResourceStatistics = new ResourceStatistics
            {
                PeakMemoryUsage = 1024L * 1024 * 1024,
                AverageCpuUsage = 48.5
            }
        };

        // Assert
        stats.Should().NotBeNull();
        stats.TotalRequests.Should().Be(5000);
        stats.SystemMetrics.CpuUsagePercent.Should().Be(55.5);
        stats.ChunkingStatistics.TotalChunks.Should().Be(1000);
        stats.ProcessingStatistics.SuccessfulOperations.Should().Be(4800);
        stats.ResourceStatistics.PeakMemoryUsage.Should().Be(1024L * 1024 * 1024);
    }

    [Fact]
    public void AllStatisticsClasses_ShouldBeIndependentlyInstantiable()
    {
        // Act
        var perfStats = new PerformanceStatistics();
        var sysMetrics = new SystemMetrics();
        var chunkStats = new ChunkingStatistics();
        var procStats = new ProcessingStatistics();
        var resStats = new ResourceStatistics();

        // Assert
        perfStats.Should().NotBeNull();
        sysMetrics.Should().NotBeNull();
        chunkStats.Should().NotBeNull();
        procStats.Should().NotBeNull();
        resStats.Should().NotBeNull();
    }

    #endregion
}
