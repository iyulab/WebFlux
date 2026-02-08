namespace WebFlux.Core.Models.Events;

/// <summary>
/// 파이프라인 처리 관련 이벤트 통합
/// </summary>

/// <summary>
/// 처리 시작 이벤트 (통합)
/// </summary>
public class ProcessingStartedEventV2 : ProcessingEvent
{
    public override string EventType => "ProcessingStarted";

    /// <summary>구성 정보</summary>
    public WebFluxConfiguration Configuration { get; set; } = new();

    /// <summary>시작 URL 목록</summary>
    public List<string> StartUrls { get; set; } = new();
}

/// <summary>
/// 처리 진행률 이벤트 (통합)
/// </summary>
public class ProcessingProgressEventV2 : ProcessingEvent
{
    public override string EventType => "ProcessingProgress";

    /// <summary>처리된 수</summary>
    public int ProcessedCount { get; set; }

    /// <summary>경과 시간</summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>예상 남은 시간</summary>
    public TimeSpan EstimatedRemaining { get; set; }

    /// <summary>현재 단계</summary>
    public string CurrentStage { get; set; } = string.Empty;
}

/// <summary>
/// 처리 완료 이벤트 (통합)
/// </summary>
public class ProcessingCompletedEventV2 : ProcessingEvent
{
    public override string EventType => "ProcessingCompleted";

    /// <summary>처리된 청크 수</summary>
    public int ProcessedChunkCount { get; set; }

    /// <summary>총 처리 시간</summary>
    public TimeSpan TotalProcessingTime { get; set; }

    /// <summary>평균 처리 속도</summary>
    public double AverageProcessingRate { get; set; }
}

/// <summary>
/// 처리 실패 이벤트 (통합)
/// </summary>
public class ProcessingFailedEventV2 : ProcessingEvent
{
    public override string EventType => "ProcessingFailed";

    /// <summary>오류 메시지</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>처리된 수</summary>
    public int ProcessedCount { get; set; }
}
