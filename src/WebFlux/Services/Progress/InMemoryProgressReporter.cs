using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services.Progress;

/// <summary>
/// 메모리 기반 진행률 보고 서비스
/// 실시간 진행률 추적 및 이벤트 스트리밍 지원
/// </summary>
public class InMemoryProgressReporter : IProgressReporter, IDisposable
{
    private readonly ILogger<InMemoryProgressReporter> _logger;
    private readonly ConcurrentDictionary<string, JobProgress> _jobs = new();
    private readonly ConcurrentDictionary<string, List<TaskCompletionSource<ProgressInfo>>> _subscribers = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public InMemoryProgressReporter(ILogger<InMemoryProgressReporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 5분마다 오래된 완료된 작업 정리
        _cleanupTimer = new Timer(async _ => await CleanupCompletedJobsAsync(TimeSpan.FromHours(1)),
            null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 새로운 작업 시작
    /// </summary>
    public async Task<IProgressTracker> StartJobAsync(string jobId, string description, int totalSteps)
    {
        var jobProgress = new JobProgress
        {
            JobId = jobId,
            Description = description,
            Status = JobStatus.Running,
            StartTime = DateTimeOffset.UtcNow,
            CurrentProgress = new ProgressInfo
            {
                JobId = jobId,
                CurrentStep = 0,
                TotalSteps = totalSteps,
                CurrentStepName = "시작",
                StepProgress = 0,
                OverallProgress = 0,
                Details = "작업 시작 중...",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _jobs[jobId] = jobProgress;

        _logger.LogInformation("작업 시작: {JobId} - {Description}, 총 {TotalSteps}단계",
            jobId, description, totalSteps);

        await NotifySubscribersAsync(jobId, jobProgress.CurrentProgress);

        return new ProgressTracker(this, jobId);
    }

    /// <summary>
    /// 진행률 업데이트
    /// </summary>
    public async Task ReportProgressAsync(string jobId, ProgressInfo progress)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogWarning("존재하지 않는 작업에 대한 진행률 업데이트: {JobId}", jobId);
            return;
        }

        progress.JobId = jobId;
        progress.Timestamp = DateTimeOffset.UtcNow;

        // 전체 진행률 계산 (단계 기반)
        if (progress.TotalSteps > 0)
        {
            var stepProgress = (double)progress.CurrentStep / progress.TotalSteps * 100;
            var withinStepProgress = progress.StepProgress / progress.TotalSteps;
            progress.OverallProgress = Math.Min(100, stepProgress + withinStepProgress);
        }

        job.CurrentProgress = progress;
        job.History.Add(progress);

        _logger.LogDebug("진행률 업데이트: {JobId} - {Step}/{TotalSteps} ({Progress:F1}%)",
            jobId, progress.CurrentStep + 1, progress.TotalSteps, progress.OverallProgress);

        await NotifySubscribersAsync(jobId, progress);
    }

    /// <summary>
    /// 작업 완료
    /// </summary>
    public async Task CompleteJobAsync(string jobId, object? result = null)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogWarning("존재하지 않는 작업 완료 시도: {JobId}", jobId);
            return;
        }

        job.Status = JobStatus.Completed;
        job.EndTime = DateTimeOffset.UtcNow;
        job.Result = result;

        if (job.CurrentProgress != null)
        {
            job.CurrentProgress.OverallProgress = 100;
            job.CurrentProgress.Details = "작업 완료";
            job.CurrentProgress.Timestamp = DateTimeOffset.UtcNow;

            await NotifySubscribersAsync(jobId, job.CurrentProgress);
        }

        _logger.LogInformation("작업 완료: {JobId} - 소요시간: {Duration}",
            jobId, job.ElapsedTime);

        // 완료 알림 후 구독자 정리
        CompleteSubscribers(jobId);
    }

    /// <summary>
    /// 작업 실패
    /// </summary>
    public async Task FailJobAsync(string jobId, Exception error)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            _logger.LogWarning("존재하지 않는 작업 실패 시도: {JobId}", jobId);
            return;
        }

        job.Status = JobStatus.Failed;
        job.EndTime = DateTimeOffset.UtcNow;
        job.ErrorMessage = error.Message;

        if (job.CurrentProgress != null)
        {
            job.CurrentProgress.Details = $"작업 실패: {error.Message}";
            job.CurrentProgress.Timestamp = DateTimeOffset.UtcNow;

            await NotifySubscribersAsync(jobId, job.CurrentProgress);
        }

        // 오류 로그 추가
        job.Logs.Add(new ProgressLogEntry
        {
            Level = ProgressLogLevel.Error,
            Message = error.Message,
            Data = error,
            Timestamp = DateTimeOffset.UtcNow
        });

        _logger.LogError(error, "작업 실패: {JobId} - 소요시간: {Duration}",
            jobId, job.ElapsedTime);

        // 실패 알림 후 구독자 정리
        CompleteSubscribers(jobId);
    }

    /// <summary>
    /// 실시간 진행률 모니터링
    /// </summary>
    public async IAsyncEnumerable<ProgressInfo> MonitorProgressAsync(string jobId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscribers = _subscribers.GetOrAdd(jobId, _ => new List<TaskCompletionSource<ProgressInfo>>());

        // 현재 진행률이 있으면 즉시 반환
        if (_jobs.TryGetValue(jobId, out var job) && job.CurrentProgress != null)
        {
            yield return job.CurrentProgress;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var tcs = new TaskCompletionSource<ProgressInfo>();

            lock (subscribers)
            {
                subscribers.Add(tcs);
            }

            // 취소 토큰 등록
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

            try
            {
                var progress = await tcs.Task;
                yield return progress;

                // 작업이 완료되면 스트림 종료
                if (_jobs.TryGetValue(jobId, out var currentJob) &&
                    (currentJob.Status == JobStatus.Completed || currentJob.Status == JobStatus.Failed))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            finally
            {
                lock (subscribers)
                {
                    subscribers.Remove(tcs);
                }
            }
        }
    }

    /// <summary>
    /// 모든 작업 진행률 조회
    /// </summary>
    public Task<IReadOnlyList<JobProgress>> GetAllJobsAsync()
    {
        var jobs = _jobs.Values.ToList().AsReadOnly();
        return Task.FromResult((IReadOnlyList<JobProgress>)jobs);
    }

    /// <summary>
    /// 특정 작업 진행률 조회
    /// </summary>
    public Task<JobProgress?> GetJobProgressAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    /// <summary>
    /// 완료된 작업 정리
    /// </summary>
    public Task CleanupCompletedJobsAsync(TimeSpan? olderThan = null)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(olderThan ?? TimeSpan.FromHours(1));
        var toRemove = new List<string>();

        foreach (var kvp in _jobs)
        {
            var job = kvp.Value;
            if ((job.Status == JobStatus.Completed || job.Status == JobStatus.Failed) &&
                job.EndTime.HasValue && job.EndTime.Value < cutoffTime)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var jobId in toRemove)
        {
            _jobs.TryRemove(jobId, out _);
            _subscribers.TryRemove(jobId, out _);
        }

        if (toRemove.Count > 0)
        {
            _logger.LogInformation("완료된 작업 정리: {Count}개 작업", toRemove.Count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 로그 메시지 추가 (내부 사용)
    /// </summary>
    internal async Task AddLogAsync(string jobId, ProgressLogLevel level, string message, object? data = null)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return;

        var logEntry = new ProgressLogEntry
        {
            Level = level,
            Message = message,
            Data = data,
            Timestamp = DateTimeOffset.UtcNow,
            StepName = job.CurrentProgress?.CurrentStepName,
            StepNumber = job.CurrentProgress?.CurrentStep
        };

        job.Logs.Add(logEntry);

        // 중요한 로그는 진행률 업데이트로도 전송
        if (level >= ProgressLogLevel.Warning && job.CurrentProgress != null)
        {
            job.CurrentProgress.Details = message;
            job.CurrentProgress.Timestamp = DateTimeOffset.UtcNow;
            await NotifySubscribersAsync(jobId, job.CurrentProgress);
        }

        _logger.Log(ConvertLogLevel(level), "작업 로그 [{JobId}]: {Message}", jobId, message);
    }

    /// <summary>
    /// 구독자들에게 진행률 알림
    /// </summary>
    private async Task NotifySubscribersAsync(string jobId, ProgressInfo progress)
    {
        if (!_subscribers.TryGetValue(jobId, out var subscribers))
            return;

        var toNotify = new List<TaskCompletionSource<ProgressInfo>>();

        lock (subscribers)
        {
            toNotify.AddRange(subscribers);
            subscribers.Clear();
        }

        foreach (var subscriber in toNotify)
        {
            subscriber.TrySetResult(progress);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 구독자 완료 처리
    /// </summary>
    private void CompleteSubscribers(string jobId)
    {
        if (!_subscribers.TryRemove(jobId, out var subscribers))
            return;

        lock (subscribers)
        {
            foreach (var subscriber in subscribers)
            {
                subscriber.TrySetCanceled();
            }
            subscribers.Clear();
        }
    }

    /// <summary>
    /// 로그 레벨 변환
    /// </summary>
    private static LogLevel ConvertLogLevel(ProgressLogLevel level)
    {
        return level switch
        {
            ProgressLogLevel.Debug => LogLevel.Debug,
            ProgressLogLevel.Info => LogLevel.Information,
            ProgressLogLevel.Warning => LogLevel.Warning,
            ProgressLogLevel.Error => LogLevel.Error,
            ProgressLogLevel.Milestone => LogLevel.Information,
            _ => LogLevel.Information
        };
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _cleanupTimer?.Dispose();

        // 모든 구독자 취소
        foreach (var subscribers in _subscribers.Values)
        {
            lock (subscribers)
            {
                foreach (var subscriber in subscribers)
                {
                    subscriber.TrySetCanceled();
                }
            }
        }

        _jobs.Clear();
        _subscribers.Clear();
        _disposed = true;
    }
}

/// <summary>
/// 진행률 추적기 구현
/// </summary>
internal class ProgressTracker : IProgressTracker
{
    private readonly InMemoryProgressReporter _reporter;
    private readonly string _jobId;
    private bool _disposed;

    public string JobId => _jobId;

    internal ProgressTracker(InMemoryProgressReporter reporter, string jobId)
    {
        _reporter = reporter;
        _jobId = jobId;
    }

    public async Task UpdateStepAsync(string stepName, int stepNumber, string? details = null)
    {
        if (_disposed) return;

        var progress = new ProgressInfo
        {
            JobId = _jobId,
            CurrentStep = stepNumber,
            CurrentStepName = stepName,
            StepProgress = 0,
            Details = details ?? stepName
        };

        await _reporter.ReportProgressAsync(_jobId, progress);
    }

    public async Task UpdateStepProgressAsync(int current, int total, string? details = null)
    {
        if (_disposed) return;

        var job = await _reporter.GetJobProgressAsync(_jobId);
        if (job?.CurrentProgress == null) return;

        var stepProgress = total > 0 ? (double)current / total * 100 : 0;

        var progress = new ProgressInfo
        {
            JobId = _jobId,
            CurrentStep = job.CurrentProgress.CurrentStep,
            TotalSteps = job.CurrentProgress.TotalSteps,
            CurrentStepName = job.CurrentProgress.CurrentStepName,
            StepProgress = stepProgress,
            Details = details ?? job.CurrentProgress.Details
        };

        await _reporter.ReportProgressAsync(_jobId, progress);
    }

    public async Task LogAsync(ProgressLogLevel level, string message, object? data = null)
    {
        if (_disposed) return;
        await _reporter.AddLogAsync(_jobId, level, message, data);
    }

    public async Task CompleteAsync(object? result = null)
    {
        if (_disposed) return;
        await _reporter.CompleteJobAsync(_jobId, result);
        _disposed = true;
    }

    public async Task FailAsync(Exception error)
    {
        if (_disposed) return;
        await _reporter.FailJobAsync(_jobId, error);
        _disposed = true;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}