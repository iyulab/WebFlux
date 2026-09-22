using WebFlux.Core.Interfaces;
using Xunit;
using AwesomeAssertions;

namespace WebFlux.Tests.Core.Interfaces;

/// <summary>
/// 이벤트 및 설정 관련 모델 단위 테스트
/// EventPublishingStatistics 검증 (IncompatibleSetting 은 0.14.0 에서 사이트 설정 분석기와 함께 제거)
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
}
