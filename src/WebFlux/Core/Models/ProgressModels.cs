namespace WebFlux.Core.Models;

/// <summary>
/// 진행률 정보
/// </summary>
public class ProgressInfo
{
    /// <summary>
    /// 작업 ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// 현재 단계 번호
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// 전체 단계 수
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// 현재 단계 이름
    /// </summary>
    public string CurrentStepName { get; set; } = string.Empty;

    /// <summary>
    /// 현재 단계 진행률 (0-100)
    /// </summary>
    public double StepProgress { get; set; }

    /// <summary>
    /// 전체 진행률 (0-100)
    /// </summary>
    public double OverallProgress { get; set; }

    /// <summary>
    /// 상세 설명
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 타임스탬프
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 작업 진행률
/// </summary>
public class JobProgress
{
    /// <summary>
    /// 작업 ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// 작업 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 작업 상태
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// 시작 시간
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 종료 시간 (해당되는 경우)
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 현재 진행률 정보
    /// </summary>
    public ProgressInfo? CurrentProgress { get; set; }

    /// <summary>
    /// 작업 결과 (완료된 경우)
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// 오류 정보 (실패한 경우)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 진행률 히스토리
    /// </summary>
    public List<ProgressInfo> History { get; set; } = new();

    /// <summary>
    /// 로그 메시지들
    /// </summary>
    public List<ProgressLogEntry> Logs { get; set; } = new();

    /// <summary>
    /// 소요 시간
    /// </summary>
    public TimeSpan ElapsedTime => EndTime?.Subtract(StartTime) ?? DateTimeOffset.UtcNow.Subtract(StartTime);

    /// <summary>
    /// 예상 남은 시간 (진행률 기반 계산)
    /// </summary>
    public TimeSpan? EstimatedRemainingTime
    {
        get
        {
            if (CurrentProgress?.OverallProgress <= 0 || CurrentProgress == null) return null;
            if (CurrentProgress.OverallProgress >= 100) return TimeSpan.Zero;

            var elapsed = ElapsedTime;
            var remainingPercentage = 100 - CurrentProgress.OverallProgress;
            var timePerPercent = elapsed.TotalSeconds / CurrentProgress.OverallProgress;

            return TimeSpan.FromSeconds(timePerPercent * remainingPercentage);
        }
    }
}

/// <summary>
/// 작업 상태
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// 대기 중
    /// </summary>
    Pending,

    /// <summary>
    /// 실행 중
    /// </summary>
    Running,

    /// <summary>
    /// 완료됨
    /// </summary>
    Completed,

    /// <summary>
    /// 실패함
    /// </summary>
    Failed,

    /// <summary>
    /// 취소됨
    /// </summary>
    Cancelled,

    /// <summary>
    /// 일시 중지됨
    /// </summary>
    Paused
}

/// <summary>
/// 진행률 로그 엔트리
/// </summary>
public class ProgressLogEntry
{
    /// <summary>
    /// 타임스탬프
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 로그 레벨
    /// </summary>
    public ProgressLogLevel Level { get; set; }

    /// <summary>
    /// 메시지
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 추가 데이터
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 단계 정보
    /// </summary>
    public string? StepName { get; set; }

    /// <summary>
    /// 단계 번호
    /// </summary>
    public int? StepNumber { get; set; }
}

/// <summary>
/// 진행률 로그 레벨
/// </summary>
public enum ProgressLogLevel
{
    /// <summary>
    /// 디버그 정보
    /// </summary>
    Debug,

    /// <summary>
    /// 일반 정보
    /// </summary>
    Info,

    /// <summary>
    /// 경고
    /// </summary>
    Warning,

    /// <summary>
    /// 오류
    /// </summary>
    Error,

    /// <summary>
    /// 중요 마일스톤
    /// </summary>
    Milestone
}

/// <summary>
/// 진행률 통계
/// </summary>
public class ProgressStatistics
{
    /// <summary>
    /// 전체 작업 수
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// 실행 중인 작업 수
    /// </summary>
    public int RunningJobs { get; set; }

    /// <summary>
    /// 완료된 작업 수
    /// </summary>
    public int CompletedJobs { get; set; }

    /// <summary>
    /// 실패한 작업 수
    /// </summary>
    public int FailedJobs { get; set; }

    /// <summary>
    /// 평균 처리 시간
    /// </summary>
    public TimeSpan AverageProcessingTime { get; set; }

    /// <summary>
    /// 성공률 (%)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 시간당 처리 작업 수
    /// </summary>
    public double JobsPerHour { get; set; }

    /// <summary>
    /// 최근 업데이트 시간
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}