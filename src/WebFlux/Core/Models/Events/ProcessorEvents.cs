using WebFlux.Configuration;

namespace WebFlux.Core.Models.Events;

/// <summary>
/// 파이프라인 처리 시작 이벤트
/// </summary>
public class ProcessingStartedEvent : ProcessingEvent
{
    public override string EventType => "ProcessingStarted";

    /// <summary>구성 정보</summary>
    public required WebFluxConfiguration Configuration { get; init; }

    /// <summary>시작 URL 목록</summary>
    public IReadOnlyList<string> StartUrls { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 파이프라인 처리 진행률 이벤트
/// </summary>
public class ProcessingProgressEvent : ProcessingEvent
{
    public override string EventType => "ProcessingProgress";

    /// <summary>처리된 수</summary>
    public int ProcessedCount { get; init; }

    /// <summary>경과 시간</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>예상 남은 시간</summary>
    public TimeSpan EstimatedRemaining { get; init; }

    /// <summary>현재 단계</summary>
    public string CurrentStage { get; init; } = string.Empty;
}

/// <summary>
/// 파이프라인 처리 완료 이벤트
/// </summary>
public class ProcessingCompletedEvent : ProcessingEvent
{
    public override string EventType => "ProcessingCompleted";

    /// <summary>처리된 청크 수</summary>
    public int ProcessedChunkCount { get; init; }

    /// <summary>총 처리 시간</summary>
    public TimeSpan TotalProcessingTime { get; init; }

    /// <summary>평균 처리 속도 (청크/초)</summary>
    public double AverageProcessingRate { get; init; }
}

/// <summary>
/// 파이프라인 처리 실패 이벤트
/// </summary>
public class ProcessingFailedEvent : ProcessingEvent
{
    public override string EventType => "ProcessingFailed";

    /// <summary>오류 메시지</summary>
    public required string Error { get; init; }

    /// <summary>처리된 수</summary>
    public int ProcessedCount { get; init; }
}
