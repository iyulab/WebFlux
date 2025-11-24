using WebFlux.Core.Interfaces;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Interfaces;

/// <summary>
/// 이벤트 및 설정 관련 모델 단위 테스트
/// EventPublishingStatistics, IncompatibleSetting 검증
/// </summary>
public class EventAndConfigModelsTests
{
    #region EventPublishingStatistics Tests

    [Fact]
    public void EventPublishingStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new EventPublishingStatistics();

        // Assert
        stats.TotalEventsPublished.Should().Be(0);
        stats.SubscriberCount.Should().Be(0);
        stats.AveragePublishTimeMs.Should().Be(0);
        stats.PublishErrors.Should().Be(0);
    }

    [Fact]
    public void EventPublishingStatistics_EventsByType_ShouldInitializeAsEmptyDictionary()
    {
        // Act
        var stats = new EventPublishingStatistics();

        // Assert
        stats.EventsByType.Should().NotBeNull();
        stats.EventsByType.Should().BeEmpty();
    }

    [Fact]
    public void EventPublishingStatistics_LastUpdated_ShouldBeRecentUtcTime()
    {
        // Act
        var stats = new EventPublishingStatistics();

        // Assert
        stats.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EventPublishingStatistics_ShouldAllowInitOnlyPropertyAssignment()
    {
        // Arrange
        var eventsByType = new Dictionary<string, long>
        {
            { "CrawlingStarted", 100 },
            { "CrawlingCompleted", 95 },
            { "ChunkGenerated", 500 },
            { "ErrorOccurred", 5 }
        };

        // Act
        var stats = new EventPublishingStatistics
        {
            TotalEventsPublished = 700,
            EventsByType = eventsByType,
            SubscriberCount = 12,
            AveragePublishTimeMs = 2.5,
            PublishErrors = 5,
            LastUpdated = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Assert
        stats.TotalEventsPublished.Should().Be(700);
        stats.EventsByType.Should().HaveCount(4);
        stats.EventsByType["CrawlingStarted"].Should().Be(100);
        stats.EventsByType["ChunkGenerated"].Should().Be(500);
        stats.SubscriberCount.Should().Be(12);
        stats.AveragePublishTimeMs.Should().Be(2.5);
        stats.PublishErrors.Should().Be(5);
        stats.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(-5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EventPublishingStatistics_WithRealisticValues_ShouldStoreCorrectly()
    {
        // Arrange
        var eventsByType = new Dictionary<string, long>
        {
            { "CrawlingStarted", 1000 },
            { "CrawlingCompleted", 980 },
            { "PageCrawled", 5000 },
            { "ChunkingStarted", 1000 },
            { "ChunkingCompleted", 995 },
            { "ChunkGenerated", 50000 },
            { "ImageProcessed", 2000 },
            { "ErrorOccurred", 20 }
        };

        // Act
        var stats = new EventPublishingStatistics
        {
            TotalEventsPublished = 60995,
            EventsByType = eventsByType,
            SubscriberCount = 25,
            AveragePublishTimeMs = 1.2,
            PublishErrors = 20
        };

        // Assert
        stats.TotalEventsPublished.Should().Be(60995);
        stats.EventsByType.Should().HaveCount(8);
        stats.SubscriberCount.Should().Be(25);
        stats.AveragePublishTimeMs.Should().BeGreaterThan(0);
        stats.PublishErrors.Should().Be(20);
    }

    [Fact]
    public void EventPublishingStatistics_ErrorRate_ShouldBeCalculable()
    {
        // Arrange
        var stats = new EventPublishingStatistics
        {
            TotalEventsPublished = 1000,
            PublishErrors = 10
        };

        // Act
        var errorRate = stats.PublishErrors / (double)stats.TotalEventsPublished;

        // Assert
        errorRate.Should().BeApproximately(0.01, 0.001);
    }

    [Fact]
    public void EventPublishingStatistics_EventsByType_ShouldBeReadOnly()
    {
        // Arrange
        var eventsByType = new Dictionary<string, long>
        {
            { "EventType1", 100 }
        };

        var stats = new EventPublishingStatistics
        {
            EventsByType = eventsByType
        };

        // Act & Assert
        stats.EventsByType.Should().BeAssignableTo<IReadOnlyDictionary<string, long>>();
        stats.EventsByType.Should().HaveCount(1);
    }

    [Fact]
    public void EventPublishingStatistics_WithZeroErrors_ShouldIndicatePerfectHealth()
    {
        // Arrange & Act
        var stats = new EventPublishingStatistics
        {
            TotalEventsPublished = 10000,
            PublishErrors = 0,
            AveragePublishTimeMs = 0.8
        };

        // Assert
        stats.PublishErrors.Should().Be(0);
        stats.TotalEventsPublished.Should().BeGreaterThan(0);
    }

    #endregion

    #region IncompatibleSetting Tests

    [Fact]
    public void IncompatibleSetting_ShouldInitializeWithDefaults()
    {
        // Act
        var setting = new IncompatibleSetting();

        // Assert
        setting.Key.Should().Be(string.Empty);
        setting.CurrentValue.Should().Be(string.Empty);
        setting.Reason.Should().Be(string.Empty);
        setting.Alternative.Should().BeNull();
        setting.RequiresManualWork.Should().BeFalse();
    }

    [Fact]
    public void IncompatibleSetting_ShouldAllowInitOnlyPropertyAssignment()
    {
        // Arrange & Act
        var setting = new IncompatibleSetting
        {
            Key = "baseURL",
            CurrentValue = "http://example.com",
            Reason = "HTTP is deprecated, HTTPS is required",
            Alternative = "https://example.com",
            RequiresManualWork = false
        };

        // Assert
        setting.Key.Should().Be("baseURL");
        setting.CurrentValue.Should().Be("http://example.com");
        setting.Reason.Should().Be("HTTP is deprecated, HTTPS is required");
        setting.Alternative.Should().Be("https://example.com");
        setting.RequiresManualWork.Should().BeFalse();
    }

    [Fact]
    public void IncompatibleSetting_WithNoAlternative_ShouldIndicateManualWork()
    {
        // Arrange & Act
        var setting = new IncompatibleSetting
        {
            Key = "customPlugin",
            CurrentValue = "old-plugin-v1.0",
            Reason = "Plugin is no longer supported",
            Alternative = null,
            RequiresManualWork = true
        };

        // Assert
        setting.Alternative.Should().BeNull();
        setting.RequiresManualWork.Should().BeTrue();
    }

    [Fact]
    public void IncompatibleSetting_ConfigMigrationScenario_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var setting = new IncompatibleSetting
        {
            Key = "config.theme.layout",
            CurrentValue = "legacy-sidebar",
            Reason = "Legacy layout is not compatible with new theme system",
            Alternative = "modern-sidebar",
            RequiresManualWork = false
        };

        // Assert
        setting.Key.Should().Be("config.theme.layout");
        setting.CurrentValue.Should().Be("legacy-sidebar");
        setting.Alternative.Should().Be("modern-sidebar");
        setting.RequiresManualWork.Should().BeFalse();
    }

    [Fact]
    public void IncompatibleSetting_ComplexMigrationScenario_ShouldIndicateManualWork()
    {
        // Arrange & Act
        var setting = new IncompatibleSetting
        {
            Key = "database.connectionString",
            CurrentValue = "Server=localhost;Database=OldDB",
            Reason = "Database schema has changed significantly",
            Alternative = "Requires custom migration script",
            RequiresManualWork = true
        };

        // Assert
        setting.RequiresManualWork.Should().BeTrue();
        setting.Alternative.Should().Contain("migration");
    }

    [Theory]
    [InlineData("api.endpoint", "v1/api", "API v1 is deprecated", "v2/api", false)]
    [InlineData("security.protocol", "TLS1.0", "TLS1.0 is insecure", "TLS1.3", false)]
    [InlineData("custom.feature", "enabled", "Feature removed", null, true)]
    public void IncompatibleSetting_WithVariousScenarios_ShouldStoreCorrectly(
        string key, string currentValue, string reason, string? alternative, bool requiresManualWork)
    {
        // Arrange & Act
        var setting = new IncompatibleSetting
        {
            Key = key,
            CurrentValue = currentValue,
            Reason = reason,
            Alternative = alternative,
            RequiresManualWork = requiresManualWork
        };

        // Assert
        setting.Key.Should().Be(key);
        setting.CurrentValue.Should().Be(currentValue);
        setting.Reason.Should().Be(reason);
        setting.Alternative.Should().Be(alternative);
        setting.RequiresManualWork.Should().Be(requiresManualWork);
    }

    [Fact]
    public void IncompatibleSetting_EmptyKey_ShouldBeAllowed()
    {
        // Arrange & Act
        var setting = new IncompatibleSetting
        {
            Key = string.Empty,
            CurrentValue = "some_value",
            Reason = "Test reason"
        };

        // Assert
        setting.Key.Should().Be(string.Empty);
    }

    [Fact]
    public void IncompatibleSetting_WithLongReasonText_ShouldStore()
    {
        // Arrange
        var longReason = "This setting is incompatible because the underlying framework has been updated " +
                        "and no longer supports this configuration option. The new framework requires " +
                        "a different approach to achieve the same functionality.";

        // Act
        var setting = new IncompatibleSetting
        {
            Key = "framework.option",
            CurrentValue = "old_value",
            Reason = longReason
        };

        // Assert
        setting.Reason.Should().Be(longReason);
        setting.Reason.Length.Should().BeGreaterThan(100);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EventPublishingStatistics_AndIncompatibleSetting_ShouldBeIndependentlyInstantiable()
    {
        // Act
        var eventStats = new EventPublishingStatistics();
        var incompatibleSetting = new IncompatibleSetting();

        // Assert
        eventStats.Should().NotBeNull();
        incompatibleSetting.Should().NotBeNull();
    }

    [Fact]
    public void EventPublishingStatistics_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var stats1 = new EventPublishingStatistics
        {
            TotalEventsPublished = 100,
            SubscriberCount = 5
        };

        var stats2 = new EventPublishingStatistics
        {
            TotalEventsPublished = 200,
            SubscriberCount = 10
        };

        // Assert
        stats1.TotalEventsPublished.Should().NotBe(stats2.TotalEventsPublished);
        stats1.SubscriberCount.Should().NotBe(stats2.SubscriberCount);
    }

    [Fact]
    public void IncompatibleSetting_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var setting1 = new IncompatibleSetting
        {
            Key = "setting1",
            CurrentValue = "value1"
        };

        var setting2 = new IncompatibleSetting
        {
            Key = "setting2",
            CurrentValue = "value2"
        };

        // Assert
        setting1.Key.Should().NotBe(setting2.Key);
        setting1.CurrentValue.Should().NotBe(setting2.CurrentValue);
    }

    #endregion
}
