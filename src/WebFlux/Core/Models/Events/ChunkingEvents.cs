namespace WebFlux.Core.Models.Events;

/// <summary>
/// 청크 생성 이벤트
/// </summary>
public class ChunkGeneratedEvent : ProcessingEvent
{
    public override string EventType => "ChunkGenerated";

    /// <summary>청크 ID</summary>
    public required string ChunkId { get; init; }

    /// <summary>소스 URL</summary>
    public required string SourceUrl { get; init; }

    /// <summary>청크 크기 (문자 수, <c>Content.Length</c>)</summary>
    public int ChunkSize { get; init; }

    /// <summary>품질 점수</summary>
    public double QualityScore { get; init; }

    /// <summary>청크 타입</summary>
    public string ChunkType { get; init; } = "Text";

    /// <summary>시퀀스 번호</summary>
    public int SequenceNumber { get; init; }
}
