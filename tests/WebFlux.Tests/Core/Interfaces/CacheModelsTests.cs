using WebFlux.Core.Interfaces;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Interfaces;

/// <summary>
/// 캐시 관련 모델 단위 테스트
/// CacheOperation, CacheOperationType, CacheStatistics 검증
/// </summary>
public class CacheModelsTests
{
    #region CacheOperation Tests

    [Fact]
    public void CacheOperation_ShouldInitializeWithDefaults()
    {
        // Act
        var operation = new CacheOperation();

        // Assert
        operation.Type.Should().Be(default(CacheOperationType));
        operation.Key.Should().Be(string.Empty);
        operation.Value.Should().BeNull();
        operation.Expiration.Should().BeNull();
    }

    [Fact]
    public void CacheOperation_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var operation = new CacheOperation
        {
            Type = CacheOperationType.Set,
            Key = "user:123",
            Value = new { Name = "Test User", Age = 30 },
            Expiration = TimeSpan.FromMinutes(30)
        };

        // Assert
        operation.Type.Should().Be(CacheOperationType.Set);
        operation.Key.Should().Be("user:123");
        operation.Value.Should().NotBeNull();
        operation.Expiration.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void CacheOperation_WithSetType_ShouldHaveValueAndExpiration()
    {
        // Arrange & Act
        var operation = new CacheOperation
        {
            Type = CacheOperationType.Set,
            Key = "cache:key:1",
            Value = "cached_value",
            Expiration = TimeSpan.FromHours(1)
        };

        // Assert
        operation.Type.Should().Be(CacheOperationType.Set);
        operation.Value.Should().Be("cached_value");
        operation.Expiration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void CacheOperation_WithRemoveType_ShouldOnlyHaveKey()
    {
        // Arrange & Act
        var operation = new CacheOperation
        {
            Type = CacheOperationType.Remove,
            Key = "cache:to:remove"
        };

        // Assert
        operation.Type.Should().Be(CacheOperationType.Remove);
        operation.Key.Should().Be("cache:to:remove");
        operation.Value.Should().BeNull();
        operation.Expiration.Should().BeNull();
    }

    [Fact]
    public void CacheOperation_WithExpireType_ShouldHaveExpiration()
    {
        // Arrange & Act
        var operation = new CacheOperation
        {
            Type = CacheOperationType.Expire,
            Key = "cache:to:expire",
            Expiration = TimeSpan.FromMinutes(15)
        };

        // Assert
        operation.Type.Should().Be(CacheOperationType.Expire);
        operation.Key.Should().Be("cache:to:expire");
        operation.Expiration.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void CacheOperation_ShouldSupportComplexValues()
    {
        // Arrange & Act
        var complexValue = new List<string> { "item1", "item2", "item3" };
        var operation = new CacheOperation
        {
            Type = CacheOperationType.Set,
            Key = "list:items",
            Value = complexValue
        };

        // Assert
        operation.Value.Should().BeEquivalentTo(complexValue);
    }

    #endregion

    #region CacheOperationType Tests

    [Fact]
    public void CacheOperationType_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)CacheOperationType.Set).Should().Be(0);
        ((int)CacheOperationType.Remove).Should().Be(1);
        ((int)CacheOperationType.Expire).Should().Be(2);
    }

    [Fact]
    public void CacheOperationType_ShouldBeDefined()
    {
        // Assert
        Enum.IsDefined(typeof(CacheOperationType), CacheOperationType.Set).Should().BeTrue();
        Enum.IsDefined(typeof(CacheOperationType), CacheOperationType.Remove).Should().BeTrue();
        Enum.IsDefined(typeof(CacheOperationType), CacheOperationType.Expire).Should().BeTrue();
    }

    [Theory]
    [InlineData(CacheOperationType.Set)]
    [InlineData(CacheOperationType.Remove)]
    [InlineData(CacheOperationType.Expire)]
    public void CacheOperationType_AllValuesShouldBeValid(CacheOperationType type)
    {
        // Assert
        Enum.IsDefined(typeof(CacheOperationType), type).Should().BeTrue();
    }

    #endregion

    #region CacheStatistics Tests

    [Fact]
    public void CacheStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new CacheStatistics();

        // Assert
        stats.TotalRequests.Should().Be(0);
        stats.TotalHits.Should().Be(0);
        stats.TotalMisses.Should().Be(0);
        stats.MemoryHits.Should().Be(0);
        stats.DistributedHits.Should().Be(0);
        stats.Evictions.Should().Be(0);
        stats.CurrentEntryCount.Should().Be(0);
        stats.TotalKeys.Should().Be(0);
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.MemoryUsage.Should().Be(0);
        stats.ExpiredKeys.Should().Be(0);
        stats.EvictedKeys.Should().Be(0);
    }

    [Fact]
    public void CacheStatistics_KeyStatistics_ShouldInitializeAsEmptyDictionary()
    {
        // Act
        var stats = new CacheStatistics();

        // Assert
        stats.KeyStatistics.Should().NotBeNull();
        stats.KeyStatistics.Should().BeEmpty();
    }

    [Fact]
    public void CacheStatistics_LastUpdated_ShouldBeRecentUtcTime()
    {
        // Act
        var stats = new CacheStatistics();

        // Assert
        stats.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CacheStatistics_HitRate_WithNoRequests_ShouldReturnZero()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 0,
            TotalHits = 0
        };

        // Act
        var hitRate = stats.HitRate;

        // Assert
        hitRate.Should().Be(0);
    }

    [Fact]
    public void CacheStatistics_HitRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 1000,
            TotalHits = 850
        };

        // Act
        var hitRate = stats.HitRate;

        // Assert
        hitRate.Should().BeApproximately(0.85, 0.001);
    }

    [Fact]
    public void CacheStatistics_MemoryHitRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 1000,
            MemoryHits = 700
        };

        // Act
        var memoryHitRate = stats.MemoryHitRate;

        // Assert
        memoryHitRate.Should().BeApproximately(0.70, 0.001);
    }

    [Fact]
    public void CacheStatistics_DistributedHitRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 1000,
            DistributedHits = 150
        };

        // Act
        var distributedHitRate = stats.DistributedHitRate;

        // Assert
        distributedHitRate.Should().BeApproximately(0.15, 0.001);
    }

    [Fact]
    public void CacheStatistics_HitRatio_WithNoOperations_ShouldReturnZero()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            Hits = 0,
            Misses = 0
        };

        // Act
        var hitRatio = stats.HitRatio;

        // Assert
        hitRatio.Should().Be(0);
    }

    [Fact]
    public void CacheStatistics_HitRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            Hits = 800,
            Misses = 200
        };

        // Act
        var hitRatio = stats.HitRatio;

        // Assert
        hitRatio.Should().BeApproximately(0.80, 0.001);
    }

    [Fact]
    public void CacheStatistics_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var stats = new CacheStatistics
        {
            TotalRequests = 5000,
            TotalHits = 4200,
            TotalMisses = 800,
            MemoryHits = 3500,
            DistributedHits = 700,
            Evictions = 50,
            CurrentEntryCount = 1200,
            TotalKeys = 1500,
            Hits = 4200,
            Misses = 800,
            MemoryUsage = 1024 * 1024 * 100,
            ExpiredKeys = 300,
            EvictedKeys = 50
        };

        // Assert
        stats.TotalRequests.Should().Be(5000);
        stats.TotalHits.Should().Be(4200);
        stats.TotalMisses.Should().Be(800);
        stats.MemoryHits.Should().Be(3500);
        stats.DistributedHits.Should().Be(700);
        stats.Evictions.Should().Be(50);
        stats.CurrentEntryCount.Should().Be(1200);
        stats.TotalKeys.Should().Be(1500);
        stats.Hits.Should().Be(4200);
        stats.Misses.Should().Be(800);
        stats.MemoryUsage.Should().Be(1024 * 1024 * 100);
        stats.ExpiredKeys.Should().Be(300);
        stats.EvictedKeys.Should().Be(50);
    }

    [Fact]
    public void CacheStatistics_KeyStatistics_ShouldAllowModification()
    {
        // Arrange
        var stats = new CacheStatistics();

        // Act
        stats.KeyStatistics["user_cache"] = 1500L;
        stats.KeyStatistics["session_cache"] = 800L;
        stats.KeyStatistics["temp_cache"] = 200L;

        // Assert
        stats.KeyStatistics.Should().HaveCount(3);
        stats.KeyStatistics["user_cache"].Should().Be(1500L);
        stats.KeyStatistics["session_cache"].Should().Be(800L);
        stats.KeyStatistics["temp_cache"].Should().Be(200L);
    }

    [Fact]
    public void CacheStatistics_AllHitRates_ShouldBeBetweenZeroAndOne()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 1000,
            TotalHits = 850,
            MemoryHits = 600,
            DistributedHits = 250,
            Hits = 850,
            Misses = 150
        };

        // Act & Assert
        stats.HitRate.Should().BeInRange(0, 1);
        stats.MemoryHitRate.Should().BeInRange(0, 1);
        stats.DistributedHitRate.Should().BeInRange(0, 1);
        stats.HitRatio.Should().BeInRange(0, 1);
    }

    [Fact]
    public void CacheStatistics_WithRealisticValues_ShouldCalculateRatesCorrectly()
    {
        // Arrange - Realistic cache statistics scenario
        var stats = new CacheStatistics
        {
            TotalRequests = 10000,
            TotalHits = 8500,
            TotalMisses = 1500,
            MemoryHits = 7000,
            DistributedHits = 1500,
            Hits = 8500,
            Misses = 1500,
            CurrentEntryCount = 5000,
            MemoryUsage = 1024 * 1024 * 256
        };

        // Act & Assert
        stats.HitRate.Should().BeApproximately(0.85, 0.001);
        stats.MemoryHitRate.Should().BeApproximately(0.70, 0.001);
        stats.DistributedHitRate.Should().BeApproximately(0.15, 0.001);
        stats.HitRatio.Should().BeApproximately(0.85, 0.001);
        (stats.TotalHits + stats.TotalMisses).Should().Be(stats.TotalRequests);
    }

    [Fact]
    public void CacheStatistics_PerfectHitRate_ShouldReturnOne()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 1000,
            TotalHits = 1000,
            Hits = 1000,
            Misses = 0
        };

        // Act & Assert
        stats.HitRate.Should().Be(1.0);
        stats.HitRatio.Should().Be(1.0);
    }

    [Fact]
    public void CacheStatistics_ZeroHitRate_ShouldReturnZero()
    {
        // Arrange
        var stats = new CacheStatistics
        {
            TotalRequests = 1000,
            TotalHits = 0,
            TotalMisses = 1000,
            Hits = 0,
            Misses = 1000
        };

        // Act & Assert
        stats.HitRate.Should().Be(0);
        stats.HitRatio.Should().Be(0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CacheOperation_ForBatchOperations_ShouldSupportMultipleTypes()
    {
        // Arrange
        var setOp = new CacheOperation
        {
            Type = CacheOperationType.Set,
            Key = "key1",
            Value = "value1",
            Expiration = TimeSpan.FromMinutes(10)
        };

        var removeOp = new CacheOperation
        {
            Type = CacheOperationType.Remove,
            Key = "key2"
        };

        var expireOp = new CacheOperation
        {
            Type = CacheOperationType.Expire,
            Key = "key3",
            Expiration = TimeSpan.FromMinutes(5)
        };

        // Act
        var operations = new List<CacheOperation> { setOp, removeOp, expireOp };

        // Assert
        operations.Should().HaveCount(3);
        operations.Should().Contain(op => op.Type == CacheOperationType.Set);
        operations.Should().Contain(op => op.Type == CacheOperationType.Remove);
        operations.Should().Contain(op => op.Type == CacheOperationType.Expire);
    }

    [Fact]
    public void AllCacheModels_ShouldBeIndependentlyInstantiable()
    {
        // Act
        var operation = new CacheOperation();
        var stats = new CacheStatistics();

        // Assert
        operation.Should().NotBeNull();
        stats.Should().NotBeNull();
    }

    #endregion
}
