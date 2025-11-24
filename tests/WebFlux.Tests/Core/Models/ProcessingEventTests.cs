using WebFlux.Core.Models;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Models;

/// <summary>
/// ProcessingEvent 및 이벤트 클래스 단위 테스트
/// 이벤트 데이터 모델 및 속성 검증
/// </summary>
public class ProcessingEventTests
{
    #region ProcessingEvent Base Class Tests

    [Fact]
    public void ProcessingEvent_ShouldAutoGenerateEventId()
    {
        // Arrange & Act
        var event1 = new TestProcessingEvent();
        var event2 = new TestProcessingEvent();

        // Assert
        event1.EventId.Should().NotBeNullOrEmpty();
        event2.EventId.Should().NotBeNullOrEmpty();
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void ProcessingEvent_ShouldAutoGenerateTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var evt = new TestProcessingEvent();

        // Assert
        var after = DateTimeOffset.UtcNow;
        evt.Timestamp.Should().BeOnOrAfter(before);
        evt.Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void ProcessingEvent_ShouldHaveDefaultSeverityInfo()
    {
        // Arrange & Act
        var evt = new TestProcessingEvent();

        // Assert
        evt.Severity.Should().Be(EventSeverity.Info);
    }

    [Fact]
    public void ProcessingEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new TestProcessingEvent
        {
            JobId = "job-123",
            Severity = EventSeverity.Warning,
            Message = "Test message",
            Data = new Dictionary<string, object> { ["key"] = "value" },
            Source = "TestSource",
            RelatedResource = "https://test.com",
            UserId = "user-456",
            CorrelationId = "correlation-789"
        };

        // Assert
        evt.JobId.Should().Be("job-123");
        evt.Severity.Should().Be(EventSeverity.Warning);
        evt.Message.Should().Be("Test message");
        evt.Data.Should().ContainKey("key");
        evt.Source.Should().Be("TestSource");
        evt.RelatedResource.Should().Be("https://test.com");
        evt.UserId.Should().Be("user-456");
        evt.CorrelationId.Should().Be("correlation-789");
    }

    #endregion

    #region CrawlingStartedEvent Tests

    [Fact]
    public void CrawlingStartedEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new CrawlingStartedEvent
        {
            StartUrl = "https://example.com",
            CrawlOptions = new object()
        };

        // Assert
        evt.EventType.Should().Be("CrawlingStarted");
    }

    [Fact]
    public void CrawlingStartedEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new CrawlingStartedEvent
        {
            StartUrl = "https://example.com",
            CrawlOptions = new object()
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    [Fact]
    public void CrawlingStartedEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var options = new { MaxDepth = 3, MaxPages = 100 };
        var evt = new CrawlingStartedEvent
        {
            StartUrl = "https://example.com",
            CrawlOptions = options,
            EstimatedPageCount = 50
        };

        // Assert
        evt.StartUrl.Should().Be("https://example.com");
        evt.CrawlOptions.Should().Be(options);
        evt.EstimatedPageCount.Should().Be(50);
    }

    #endregion

    #region CrawlingCompletedEvent Tests

    [Fact]
    public void CrawlingCompletedEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new CrawlingCompletedEvent();

        // Assert
        evt.EventType.Should().Be("CrawlingCompleted");
    }

    [Fact]
    public void CrawlingCompletedEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new CrawlingCompletedEvent
        {
            ProcessedPages = 100,
            SuccessfulPages = 95,
            FailedPages = 5,
            TotalProcessingTimeMs = 60000,
            AverageProcessingTimeMs = 600.5
        };

        // Assert
        evt.ProcessedPages.Should().Be(100);
        evt.SuccessfulPages.Should().Be(95);
        evt.FailedPages.Should().Be(5);
        evt.TotalProcessingTimeMs.Should().Be(60000);
        evt.AverageProcessingTimeMs.Should().Be(600.5);
    }

    [Fact]
    public void CrawlingCompletedEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new CrawlingCompletedEvent();

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region PageCrawledEvent Tests

    [Fact]
    public void PageCrawledEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new PageCrawledEvent
        {
            Url = "https://example.com"
        };

        // Assert
        evt.EventType.Should().Be("PageCrawled");
    }

    [Fact]
    public void PageCrawledEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new PageCrawledEvent
        {
            Url = "https://example.com/page1",
            StatusCode = 200,
            ProcessingTimeMs = 1500,
            ContentSize = 50000,
            DiscoveredLinks = 25,
            Depth = 2
        };

        // Assert
        evt.Url.Should().Be("https://example.com/page1");
        evt.StatusCode.Should().Be(200);
        evt.ProcessingTimeMs.Should().Be(1500);
        evt.ContentSize.Should().Be(50000);
        evt.DiscoveredLinks.Should().Be(25);
        evt.Depth.Should().Be(2);
    }

    [Fact]
    public void PageCrawledEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new PageCrawledEvent
        {
            Url = "https://example.com"
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region ChunkingStartedEvent Tests

    [Fact]
    public void ChunkingStartedEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new ChunkingStartedEvent
        {
            SourceUrl = "https://example.com",
            Strategy = "Smart",
            ChunkingOptions = new object()
        };

        // Assert
        evt.EventType.Should().Be("ChunkingStarted");
    }

    [Fact]
    public void ChunkingStartedEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var options = new { ChunkSize = 1000, Overlap = 100 };
        var evt = new ChunkingStartedEvent
        {
            SourceUrl = "https://example.com",
            Strategy = "FixedSize",
            ContentLength = 50000,
            ChunkingOptions = options
        };

        // Assert
        evt.SourceUrl.Should().Be("https://example.com");
        evt.Strategy.Should().Be("FixedSize");
        evt.ContentLength.Should().Be(50000);
        evt.ChunkingOptions.Should().Be(options);
    }

    [Fact]
    public void ChunkingStartedEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new ChunkingStartedEvent
        {
            SourceUrl = "https://example.com",
            Strategy = "Smart",
            ChunkingOptions = new object()
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region ChunkingCompletedEvent Tests

    [Fact]
    public void ChunkingCompletedEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new ChunkingCompletedEvent
        {
            SourceUrl = "https://example.com"
        };

        // Assert
        evt.EventType.Should().Be("ChunkingCompleted");
    }

    [Fact]
    public void ChunkingCompletedEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new ChunkingCompletedEvent
        {
            SourceUrl = "https://example.com",
            GeneratedChunks = 50,
            AverageChunkSize = 850.5,
            AverageQualityScore = 0.85,
            ProcessingTimeMs = 2000
        };

        // Assert
        evt.SourceUrl.Should().Be("https://example.com");
        evt.GeneratedChunks.Should().Be(50);
        evt.AverageChunkSize.Should().Be(850.5);
        evt.AverageQualityScore.Should().Be(0.85);
        evt.ProcessingTimeMs.Should().Be(2000);
    }

    [Fact]
    public void ChunkingCompletedEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new ChunkingCompletedEvent
        {
            SourceUrl = "https://example.com"
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region ChunkGeneratedEvent Tests

    [Fact]
    public void ChunkGeneratedEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new ChunkGeneratedEvent
        {
            ChunkId = "chunk-123",
            SourceUrl = "https://example.com"
        };

        // Assert
        evt.EventType.Should().Be("ChunkGenerated");
    }

    [Fact]
    public void ChunkGeneratedEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new ChunkGeneratedEvent
        {
            ChunkId = "chunk-123",
            SourceUrl = "https://example.com",
            ChunkSize = 800,
            QualityScore = 0.9,
            ChunkType = "Paragraph",
            SequenceNumber = 5
        };

        // Assert
        evt.ChunkId.Should().Be("chunk-123");
        evt.SourceUrl.Should().Be("https://example.com");
        evt.ChunkSize.Should().Be(800);
        evt.QualityScore.Should().Be(0.9);
        evt.ChunkType.Should().Be("Paragraph");
        evt.SequenceNumber.Should().Be(5);
    }

    [Fact]
    public void ChunkGeneratedEvent_ShouldHaveDefaultChunkType()
    {
        // Arrange & Act
        var evt = new ChunkGeneratedEvent
        {
            ChunkId = "chunk-123",
            SourceUrl = "https://example.com"
        };

        // Assert
        evt.ChunkType.Should().Be("Text");
    }

    [Fact]
    public void ChunkGeneratedEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new ChunkGeneratedEvent
        {
            ChunkId = "chunk-123",
            SourceUrl = "https://example.com"
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region ImageProcessedEvent Tests

    [Fact]
    public void ImageProcessedEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new ImageProcessedEvent
        {
            ImageUrl = "https://example.com/image.jpg"
        };

        // Assert
        evt.EventType.Should().Be("ImageProcessed");
    }

    [Fact]
    public void ImageProcessedEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new ImageProcessedEvent
        {
            ImageUrl = "https://example.com/image.jpg",
            DescriptionLength = 250,
            ProcessingTimeMs = 3000,
            ImageSize = 150000,
            ImageFormat = "JPEG"
        };

        // Assert
        evt.ImageUrl.Should().Be("https://example.com/image.jpg");
        evt.DescriptionLength.Should().Be(250);
        evt.ProcessingTimeMs.Should().Be(3000);
        evt.ImageSize.Should().Be(150000);
        evt.ImageFormat.Should().Be("JPEG");
    }

    [Fact]
    public void ImageProcessedEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new ImageProcessedEvent
        {
            ImageUrl = "https://example.com/image.jpg"
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region ErrorOccurredEvent Tests

    [Fact]
    public void ErrorOccurredEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new ErrorOccurredEvent
        {
            ErrorCode = "ERR-001"
        };

        // Assert
        evt.EventType.Should().Be("ErrorOccurred");
    }

    [Fact]
    public void ErrorOccurredEvent_ShouldHaveErrorSeverityByDefault()
    {
        // Arrange & Act
        var evt = new ErrorOccurredEvent
        {
            ErrorCode = "ERR-001"
        };

        // Assert
        evt.Severity.Should().Be(EventSeverity.Error);
    }

    [Fact]
    public void ErrorOccurredEvent_ShouldHaveDefaultErrorCategory()
    {
        // Arrange & Act
        var evt = new ErrorOccurredEvent
        {
            ErrorCode = "ERR-001"
        };

        // Assert
        evt.ErrorCategory.Should().Be("General");
    }

    [Fact]
    public void ErrorOccurredEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var evt = new ErrorOccurredEvent
        {
            ErrorCode = "ERR-404",
            ErrorCategory = "Network",
            StackTrace = "at System.Net.Http...",
            IsRetryable = true,
            AffectedResource = "https://example.com"
        };

        // Assert
        evt.ErrorCode.Should().Be("ERR-404");
        evt.ErrorCategory.Should().Be("Network");
        evt.StackTrace.Should().Be("at System.Net.Http...");
        evt.IsRetryable.Should().BeTrue();
        evt.AffectedResource.Should().Be("https://example.com");
    }

    [Fact]
    public void ErrorOccurredEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new ErrorOccurredEvent
        {
            ErrorCode = "ERR-001"
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region PerformanceMetricsEvent Tests

    [Fact]
    public void PerformanceMetricsEvent_ShouldHaveCorrectEventType()
    {
        // Arrange & Act
        var evt = new PerformanceMetricsEvent
        {
            MetricName = "ResponseTime"
        };

        // Assert
        evt.EventType.Should().Be("PerformanceMetrics");
    }

    [Fact]
    public void PerformanceMetricsEvent_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var tags = new Dictionary<string, string>
        {
            ["endpoint"] = "/api/crawl",
            ["method"] = "GET"
        };

        var evt = new PerformanceMetricsEvent
        {
            MetricName = "ResponseTime",
            Value = 125.5,
            Unit = "ms",
            MeasurementPeriodMs = 60000,
            Tags = tags
        };

        // Assert
        evt.MetricName.Should().Be("ResponseTime");
        evt.Value.Should().Be(125.5);
        evt.Unit.Should().Be("ms");
        evt.MeasurementPeriodMs.Should().Be(60000);
        evt.Tags.Should().ContainKey("endpoint");
        evt.Tags.Should().ContainKey("method");
    }

    [Fact]
    public void PerformanceMetricsEvent_ShouldHaveEmptyTagsByDefault()
    {
        // Arrange & Act
        var evt = new PerformanceMetricsEvent
        {
            MetricName = "CpuUsage"
        };

        // Assert
        evt.Tags.Should().NotBeNull();
        evt.Tags.Should().BeEmpty();
    }

    [Fact]
    public void PerformanceMetricsEvent_ShouldInheritFromProcessingEvent()
    {
        // Arrange & Act
        var evt = new PerformanceMetricsEvent
        {
            MetricName = "ResponseTime"
        };

        // Assert
        evt.Should().BeAssignableTo<ProcessingEvent>();
    }

    #endregion

    #region EventSeverity Enum Tests

    [Fact]
    public void EventSeverity_ShouldHaveAllExpectedValues()
    {
        // Assert
        Enum.GetValues<EventSeverity>().Should().Contain(EventSeverity.Debug);
        Enum.GetValues<EventSeverity>().Should().Contain(EventSeverity.Info);
        Enum.GetValues<EventSeverity>().Should().Contain(EventSeverity.Warning);
        Enum.GetValues<EventSeverity>().Should().Contain(EventSeverity.Error);
        Enum.GetValues<EventSeverity>().Should().Contain(EventSeverity.Critical);
    }

    [Fact]
    public void EventSeverity_ShouldHaveCorrectOrder()
    {
        // Assert - Severity should increase from Debug to Critical
        ((int)EventSeverity.Debug).Should().BeLessThan((int)EventSeverity.Info);
        ((int)EventSeverity.Info).Should().BeLessThan((int)EventSeverity.Warning);
        ((int)EventSeverity.Warning).Should().BeLessThan((int)EventSeverity.Error);
        ((int)EventSeverity.Error).Should().BeLessThan((int)EventSeverity.Critical);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// ProcessingEvent를 테스트하기 위한 구체 클래스
    /// </summary>
    private class TestProcessingEvent : ProcessingEvent
    {
        public override string EventType => "Test";
    }

    #endregion
}
