using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 크롤링 진행률 보고 서비스
/// 배치 크롤링 시 상세 진행률 및 에러 리포팅 제공
/// </summary>
public class CrawlProgressReporter : ICrawlProgressReporter
{
    private readonly ConcurrentDictionary<string, CrawlProgressTracker> _trackers = new();
    private readonly ConcurrentDictionary<string, Channel<CrawlProgress>> _channels = new();

    /// <inheritdoc />
    public ICrawlProgressTracker StartCrawl(string jobId, int totalUrls)
    {
        var tracker = new CrawlProgressTracker(jobId, totalUrls, OnProgressUpdated);
        _trackers[jobId] = tracker;

        // 모니터링용 채널 생성
        _channels[jobId] = Channel.CreateUnbounded<CrawlProgress>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

        return tracker;
    }

    /// <inheritdoc />
    public CrawlProgress? GetProgress(string jobId)
    {
        return _trackers.TryGetValue(jobId, out var tracker)
            ? tracker.GetCurrentProgress()
            : null;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CrawlProgress> MonitorProgressAsync(
        string jobId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(jobId, out var channel))
            yield break;

        await foreach (var progress in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return progress;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CrawlProgress> GetAllActiveJobs()
    {
        return _trackers.Values
            .Select(t => t.GetCurrentProgress())
            .ToList();
    }

    private void OnProgressUpdated(string jobId, CrawlProgress progress)
    {
        if (_channels.TryGetValue(jobId, out var channel))
        {
            channel.Writer.TryWrite(progress);

            // 완료 시 채널 닫기
            if (progress.ProcessedUrls >= progress.TotalUrls)
            {
                channel.Writer.TryComplete();
            }
        }
    }

    /// <summary>
    /// 작업 정리 (완료된 작업 제거)
    /// </summary>
    /// <param name="olderThan">지정된 시간보다 오래된 작업만 제거</param>
    public void Cleanup(TimeSpan? olderThan = null)
    {
        var threshold = DateTime.UtcNow - (olderThan ?? TimeSpan.FromHours(1));

        var toRemove = _trackers
            .Where(kvp => kvp.Value.GetCurrentProgress().LastUpdatedAt < threshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var jobId in toRemove)
        {
            _trackers.TryRemove(jobId, out _);
            if (_channels.TryRemove(jobId, out var channel))
            {
                channel.Writer.TryComplete();
            }
        }
    }
}

/// <summary>
/// 크롤링 진행률 추적기 구현
/// </summary>
internal class CrawlProgressTracker : ICrawlProgressTracker
{
    private readonly CrawlProgress _progress;
    private readonly Stopwatch _stopwatch;
    private readonly Action<string, CrawlProgress> _onUpdated;
    private readonly object _lock = new();

    public CrawlProgressTracker(
        string jobId,
        int totalUrls,
        Action<string, CrawlProgress> onUpdated)
    {
        JobId = jobId;
        _onUpdated = onUpdated;
        _progress = new CrawlProgress
        {
            TotalUrls = totalUrls,
            StartedAt = DateTime.UtcNow
        };
        _stopwatch = Stopwatch.StartNew();
    }

    /// <inheritdoc />
    public string JobId { get; }

    /// <inheritdoc />
    public void StartUrl(string url)
    {
        lock (_lock)
        {
            _progress.CurrentUrl = url;
            UpdateTimings();
            NotifyUpdate();
        }
    }

    /// <inheritdoc />
    public void CompleteUrl(string url, int chunkCount, long bytesDownloaded = 0, double responseTimeMs = 0)
    {
        lock (_lock)
        {
            _progress.ProcessedUrls++;
            _progress.SuccessCount++;
            _progress.TotalChunks += chunkCount;

            // 통계 업데이트
            _progress.Statistics.TotalBytesDownloaded += bytesDownloaded;
            UpdateResponseTimeStats(responseTimeMs);
            UpdateDomainStats(url);
            UpdateContentTypeStats("text/html"); // 기본값, 실제로는 파라미터로 받아야 함

            UpdateTimings();
            NotifyUpdate();
        }
    }

    /// <inheritdoc />
    public void FailUrl(string url, string errorType, string message, int? statusCode = null, int retryCount = 0)
    {
        lock (_lock)
        {
            _progress.ProcessedUrls++;
            _progress.FailureCount++;

            var error = new CrawlError
            {
                Url = url,
                ErrorType = errorType,
                Message = message,
                StatusCode = statusCode,
                RetryCount = retryCount,
                OccurredAt = DateTime.UtcNow
            };

            _progress.Errors.Add(error);

            // 통계 업데이트
            if (!_progress.Statistics.ErrorsByType.ContainsKey(errorType))
                _progress.Statistics.ErrorsByType[errorType] = 0;
            _progress.Statistics.ErrorsByType[errorType]++;

            if (statusCode.HasValue)
            {
                if (!_progress.Statistics.StatusCodeCounts.ContainsKey(statusCode.Value))
                    _progress.Statistics.StatusCodeCounts[statusCode.Value] = 0;
                _progress.Statistics.StatusCodeCounts[statusCode.Value]++;
            }

            UpdateTimings();
            NotifyUpdate();
        }
    }

    /// <inheritdoc />
    public void Complete()
    {
        lock (_lock)
        {
            _stopwatch.Stop();
            UpdateTimings();
            NotifyUpdate();
        }
    }

    /// <inheritdoc />
    public void Cancel(string? reason = null)
    {
        lock (_lock)
        {
            _stopwatch.Stop();

            if (!string.IsNullOrEmpty(reason))
            {
                _progress.Errors.Add(new CrawlError
                {
                    Url = _progress.CurrentUrl,
                    ErrorType = "Cancelled",
                    Message = reason,
                    OccurredAt = DateTime.UtcNow
                });
            }

            UpdateTimings();
            NotifyUpdate();
        }
    }

    /// <inheritdoc />
    public CrawlProgress GetCurrentProgress()
    {
        lock (_lock)
        {
            // 깊은 복사를 반환하여 스레드 안전성 확보
            return new CrawlProgress
            {
                TotalUrls = _progress.TotalUrls,
                ProcessedUrls = _progress.ProcessedUrls,
                SuccessCount = _progress.SuccessCount,
                FailureCount = _progress.FailureCount,
                TotalChunks = _progress.TotalChunks,
                CurrentUrl = _progress.CurrentUrl,
                ElapsedTime = _progress.ElapsedTime,
                EstimatedRemaining = _progress.EstimatedRemaining,
                Errors = _progress.Errors.ToList(),
                StartedAt = _progress.StartedAt,
                LastUpdatedAt = _progress.LastUpdatedAt,
                Statistics = new CrawlStatisticsDetails
                {
                    TotalBytesDownloaded = _progress.Statistics.TotalBytesDownloaded,
                    AverageResponseTimeMs = _progress.Statistics.AverageResponseTimeMs,
                    MinResponseTimeMs = _progress.Statistics.MinResponseTimeMs,
                    MaxResponseTimeMs = _progress.Statistics.MaxResponseTimeMs,
                    UrlsByDomain = new Dictionary<string, int>(_progress.Statistics.UrlsByDomain),
                    ErrorsByType = new Dictionary<string, int>(_progress.Statistics.ErrorsByType),
                    StatusCodeCounts = new Dictionary<int, int>(_progress.Statistics.StatusCodeCounts),
                    ContentTypeCounts = new Dictionary<string, int>(_progress.Statistics.ContentTypeCounts)
                }
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Complete();
    }

    private void UpdateTimings()
    {
        _progress.ElapsedTime = _stopwatch.Elapsed;
        _progress.LastUpdatedAt = DateTime.UtcNow;

        // 예상 남은 시간 계산
        if (_progress.ProcessedUrls > 0)
        {
            var avgTimePerUrl = _progress.ElapsedTime.TotalSeconds / _progress.ProcessedUrls;
            var remainingUrls = _progress.TotalUrls - _progress.ProcessedUrls;
            _progress.EstimatedRemaining = TimeSpan.FromSeconds(avgTimePerUrl * remainingUrls);
        }
    }

    private void UpdateResponseTimeStats(double responseTimeMs)
    {
        if (responseTimeMs <= 0) return;

        var stats = _progress.Statistics;

        // 최소/최대
        if (responseTimeMs < stats.MinResponseTimeMs)
            stats.MinResponseTimeMs = responseTimeMs;
        if (responseTimeMs > stats.MaxResponseTimeMs)
            stats.MaxResponseTimeMs = responseTimeMs;

        // 평균 (이동 평균)
        var n = _progress.SuccessCount;
        stats.AverageResponseTimeMs =
            ((stats.AverageResponseTimeMs * (n - 1)) + responseTimeMs) / n;
    }

    private void UpdateDomainStats(string url)
    {
        try
        {
            var uri = new Uri(url);
            var domain = uri.Host;

            if (!_progress.Statistics.UrlsByDomain.ContainsKey(domain))
                _progress.Statistics.UrlsByDomain[domain] = 0;
            _progress.Statistics.UrlsByDomain[domain]++;
        }
        catch
        {
            // 잘못된 URL 무시
        }
    }

    private void UpdateContentTypeStats(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return;

        // MIME 타입의 주요 부분만 추출 (예: "text/html; charset=utf-8" → "text/html")
        var mainType = contentType.Split(';')[0].Trim();

        if (!_progress.Statistics.ContentTypeCounts.ContainsKey(mainType))
            _progress.Statistics.ContentTypeCounts[mainType] = 0;
        _progress.Statistics.ContentTypeCounts[mainType]++;
    }

    private void NotifyUpdate()
    {
        _onUpdated(JobId, GetCurrentProgress());
    }
}
