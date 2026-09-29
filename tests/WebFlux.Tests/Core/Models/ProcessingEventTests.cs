using WebFlux.Core.Models;
using WebFlux.Core.Models.Events;
using Xunit;
using AwesomeAssertions;

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
    private sealed class TestProcessingEvent : ProcessingEvent
    {
        public override string EventType => "Test";
    }

    #endregion
}
