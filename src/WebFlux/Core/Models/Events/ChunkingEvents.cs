namespace WebFlux.Core.Models.Events;

/// <summary>
/// 청킹 시작 이벤트
/// </summary>
public class ChunkingStartedEvent : ProcessingEvent
{
    public override string EventType => "ChunkingStarted";

    /// <summary>소스 URL</summary>
    public required string SourceUrl { get; init; }

    /// <summary>청킹 전략</summary>
    public required string Strategy { get; init; }

    /// <summary>콘텐츠 크기 (문자 수)</summary>
    public int ContentLength { get; init; }

    /// <summary>청킹 옵션</summary>
    public required object ChunkingOptions { get; init; }
}

/// <summary>
/// 청킹 완료 이벤트
/// </summary>
public class ChunkingCompletedEvent : ProcessingEvent
{
    public override string EventType => "ChunkingCompleted";

    /// <summary>소스 URL</summary>
    public required string SourceUrl { get; init; }

    /// <summary>생성된 청크 수</summary>
    public int GeneratedChunks { get; init; }

    /// <summary>평균 청크 크기</summary>
    public double AverageChunkSize { get; init; }

    /// <summary>평균 품질 점수</summary>
    public double AverageQualityScore { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }
}

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

    /// <summary>청크 크기 (토큰 수)</summary>
    public int ChunkSize { get; init; }

    /// <summary>품질 점수</summary>
    public double QualityScore { get; init; }

    /// <summary>청크 타입</summary>
    public string ChunkType { get; init; } = "Text";

    /// <summary>시퀀스 번호</summary>
    public int SequenceNumber { get; init; }
}
