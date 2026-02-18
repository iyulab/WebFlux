using FluentAssertions;
using WebFlux.Core.Models;
using WebFlux.Services;

namespace WebFlux.Tests.Services;

public class CrawlProgressReporterTests
{
    // --- CrawlProgressReporter: StartCrawl ---

    [Fact]
    public void StartCrawl_ReturnsTracker_WithCorrectJobId()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 10);

        tracker.JobId.Should().Be("job-1");
    }

    [Fact]
    public void StartCrawl_ReturnsTracker_WithInitialProgress()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 5);

        var progress = tracker.GetCurrentProgress();
        progress.TotalUrls.Should().Be(5);
        progress.ProcessedUrls.Should().Be(0);
        progress.SuccessCount.Should().Be(0);
        progress.FailureCount.Should().Be(0);
    }

    // --- GetProgress ---

    [Fact]
    public void GetProgress_ExistingJob_ReturnsProgress()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 10);

        var progress = reporter.GetProgress("job-1");

        progress.Should().NotBeNull();
        progress!.TotalUrls.Should().Be(10);
    }

    [Fact]
    public void GetProgress_NonExistentJob_ReturnsNull()
    {
        var reporter = new CrawlProgressReporter();

        var progress = reporter.GetProgress("nonexistent");

        progress.Should().BeNull();
    }

    // --- GetAllActiveJobs ---

    [Fact]
    public void GetAllActiveJobs_Empty_ReturnsEmptyList()
    {
        var reporter = new CrawlProgressReporter();

        var jobs = reporter.GetAllActiveJobs();

        jobs.Should().BeEmpty();
    }

    [Fact]
    public void GetAllActiveJobs_MultipleJobs_ReturnsAll()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker1 = reporter.StartCrawl("job-1", 5);
        using var tracker2 = reporter.StartCrawl("job-2", 10);

        var jobs = reporter.GetAllActiveJobs();

        jobs.Should().HaveCount(2);
    }

    // --- Tracker: StartUrl ---

    [Fact]
    public void StartUrl_SetsCurrentUrl()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 3);

        tracker.StartUrl("https://example.com/page1");

        var progress = tracker.GetCurrentProgress();
        progress.CurrentUrl.Should().Be("https://example.com/page1");
    }

    // --- Tracker: CompleteUrl ---

    [Fact]
    public void CompleteUrl_IncreasesCounters()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 3);

        tracker.CompleteUrl("https://example.com/page1", chunkCount: 5);

        var progress = tracker.GetCurrentProgress();
        progress.ProcessedUrls.Should().Be(1);
        progress.SuccessCount.Should().Be(1);
        progress.TotalChunks.Should().Be(5);
    }

    [Fact]
    public void CompleteUrl_Multiple_AccumulatesChunks()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 3);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 3);
        tracker.CompleteUrl("https://a.com/2", chunkCount: 7);

        var progress = tracker.GetCurrentProgress();
        progress.ProcessedUrls.Should().Be(2);
        progress.SuccessCount.Should().Be(2);
        progress.TotalChunks.Should().Be(10);
    }

    [Fact]
    public void CompleteUrl_WithBytesDownloaded_TracksTotalBytes()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 2);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 1, bytesDownloaded: 5000);
        tracker.CompleteUrl("https://a.com/2", chunkCount: 1, bytesDownloaded: 3000);

        var progress = tracker.GetCurrentProgress();
        progress.Statistics.TotalBytesDownloaded.Should().Be(8000);
    }

    // --- Response time statistics ---

    [Fact]
    public void CompleteUrl_WithResponseTime_UpdatesMinMaxAvg()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 3);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 1, responseTimeMs: 100);
        tracker.CompleteUrl("https://a.com/2", chunkCount: 1, responseTimeMs: 300);
        tracker.CompleteUrl("https://a.com/3", chunkCount: 1, responseTimeMs: 200);

        var stats = tracker.GetCurrentProgress().Statistics;
        stats.MinResponseTimeMs.Should().Be(100);
        stats.MaxResponseTimeMs.Should().Be(300);
        stats.AverageResponseTimeMs.Should().BeApproximately(200, 0.1);
    }

    [Fact]
    public void CompleteUrl_ZeroResponseTime_DoesNotUpdateStats()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 1);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 1, responseTimeMs: 0);

        var stats = tracker.GetCurrentProgress().Statistics;
        stats.AverageResponseTimeMs.Should().Be(0);
        stats.MaxResponseTimeMs.Should().Be(0);
        stats.MinResponseTimeMs.Should().Be(double.MaxValue); // Unchanged default
    }

    // --- Domain statistics ---

    [Fact]
    public void CompleteUrl_TracksDomainStats()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 3);

        tracker.CompleteUrl("https://example.com/page1", chunkCount: 1);
        tracker.CompleteUrl("https://example.com/page2", chunkCount: 1);
        tracker.CompleteUrl("https://other.com/page1", chunkCount: 1);

        var stats = tracker.GetCurrentProgress().Statistics;
        stats.UrlsByDomain.Should().ContainKey("example.com");
        stats.UrlsByDomain["example.com"].Should().Be(2);
        stats.UrlsByDomain["other.com"].Should().Be(1);
    }

    // --- Content type statistics ---

    [Fact]
    public void CompleteUrl_TracksContentType()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 1);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 1);

        var stats = tracker.GetCurrentProgress().Statistics;
        // Default content type is "text/html"
        stats.ContentTypeCounts.Should().ContainKey("text/html");
    }

    // --- Tracker: FailUrl ---

    [Fact]
    public void FailUrl_IncreasesFailureCount()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 2);

        tracker.FailUrl("https://a.com/bad", "Timeout", "Connection timed out");

        var progress = tracker.GetCurrentProgress();
        progress.ProcessedUrls.Should().Be(1);
        progress.FailureCount.Should().Be(1);
        progress.SuccessCount.Should().Be(0);
    }

    [Fact]
    public void FailUrl_AddsErrorToList()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 2);

        tracker.FailUrl("https://a.com/bad", "NotFound", "Page not found", statusCode: 404, retryCount: 2);

        var progress = tracker.GetCurrentProgress();
        progress.Errors.Should().HaveCount(1);
        progress.Errors[0].Url.Should().Be("https://a.com/bad");
        progress.Errors[0].ErrorType.Should().Be("NotFound");
        progress.Errors[0].Message.Should().Be("Page not found");
        progress.Errors[0].StatusCode.Should().Be(404);
        progress.Errors[0].RetryCount.Should().Be(2);
    }

    [Fact]
    public void FailUrl_TracksErrorsByType()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 3);

        tracker.FailUrl("https://a.com/1", "Timeout", "timeout 1");
        tracker.FailUrl("https://a.com/2", "Timeout", "timeout 2");
        tracker.FailUrl("https://a.com/3", "NotFound", "not found");

        var stats = tracker.GetCurrentProgress().Statistics;
        stats.ErrorsByType["Timeout"].Should().Be(2);
        stats.ErrorsByType["NotFound"].Should().Be(1);
    }

    [Fact]
    public void FailUrl_TracksStatusCodes()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 2);

        tracker.FailUrl("https://a.com/1", "NotFound", "not found", statusCode: 404);
        tracker.FailUrl("https://a.com/2", "ServerError", "error", statusCode: 500);

        var stats = tracker.GetCurrentProgress().Statistics;
        stats.StatusCodeCounts[404].Should().Be(1);
        stats.StatusCodeCounts[500].Should().Be(1);
    }

    [Fact]
    public void FailUrl_NoStatusCode_DoesNotTrackStatusCode()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 1);

        tracker.FailUrl("https://a.com/1", "Timeout", "timeout");

        var stats = tracker.GetCurrentProgress().Statistics;
        stats.StatusCodeCounts.Should().BeEmpty();
    }

    // --- Tracker: Cancel ---

    [Fact]
    public void Cancel_WithReason_AddsErrorEntry()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 5);

        tracker.Cancel("User requested cancellation");

        var progress = tracker.GetCurrentProgress();
        progress.Errors.Should().HaveCount(1);
        progress.Errors[0].ErrorType.Should().Be("Cancelled");
        progress.Errors[0].Message.Should().Be("User requested cancellation");
    }

    [Fact]
    public void Cancel_NullReason_NoErrorAdded()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 5);

        tracker.Cancel();

        var progress = tracker.GetCurrentProgress();
        progress.Errors.Should().BeEmpty();
    }

    // --- ETA calculation ---

    [Fact]
    public void CompleteUrl_CalculatesEstimatedRemaining()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 10);

        // Process 5 URLs, 5 remaining
        for (var i = 0; i < 5; i++)
        {
            tracker.CompleteUrl($"https://a.com/{i}", chunkCount: 1);
        }

        var progress = tracker.GetCurrentProgress();
        progress.EstimatedRemaining.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetCurrentProgress_InitialState_NoEstimatedRemaining()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 10);

        var progress = tracker.GetCurrentProgress();
        progress.EstimatedRemaining.Should().Be(TimeSpan.Zero);
    }

    // --- Deep copy ---

    [Fact]
    public void GetCurrentProgress_ReturnsDeepCopy()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 5);

        tracker.CompleteUrl("https://example.com/1", chunkCount: 2);

        var snapshot1 = tracker.GetCurrentProgress();
        tracker.CompleteUrl("https://example.com/2", chunkCount: 3);
        var snapshot2 = tracker.GetCurrentProgress();

        // Snapshots should be independent
        snapshot1.ProcessedUrls.Should().Be(1);
        snapshot2.ProcessedUrls.Should().Be(2);
        snapshot1.TotalChunks.Should().Be(2);
        snapshot2.TotalChunks.Should().Be(5);
    }

    // --- MonitorProgressAsync ---

    [Fact]
    public async Task MonitorProgressAsync_ReceivesUpdates()
    {
        var reporter = new CrawlProgressReporter();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var tracker = reporter.StartCrawl("job-1", 2);
        var updates = new List<CrawlProgress>();

        // Start monitoring in background
        var monitorTask = Task.Run(async () =>
        {
            await foreach (var progress in reporter.MonitorProgressAsync("job-1", cts.Token))
            {
                updates.Add(progress);
                if (progress.ProcessedUrls >= 2) break;
            }
        }, cts.Token);

        // Allow monitoring to start
        await Task.Delay(50, cts.Token);

        // Process URLs
        tracker.CompleteUrl("https://a.com/1", chunkCount: 1);
        tracker.CompleteUrl("https://a.com/2", chunkCount: 1);

        await monitorTask;

        updates.Should().NotBeEmpty();
    }

    [Fact]
    public async Task MonitorProgressAsync_NonExistentJob_CompletesImmediately()
    {
        var reporter = new CrawlProgressReporter();
        var updates = new List<CrawlProgress>();

        await foreach (var progress in reporter.MonitorProgressAsync("nonexistent"))
        {
            updates.Add(progress);
        }

        updates.Should().BeEmpty();
    }

    // --- Dispose ---

    [Fact]
    public void Dispose_StopsTracking()
    {
        var reporter = new CrawlProgressReporter();
        var tracker = reporter.StartCrawl("job-1", 5);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 1);
        tracker.Dispose();

        // After dispose, elapsed should stop increasing
        var elapsed1 = tracker.GetCurrentProgress().ElapsedTime;
        Thread.Sleep(50);
        var elapsed2 = tracker.GetCurrentProgress().ElapsedTime;

        elapsed2.Should().Be(elapsed1);
    }

    // --- Mixed success/failure ---

    [Fact]
    public void MixedSuccessAndFailure_CountsCorrectly()
    {
        var reporter = new CrawlProgressReporter();
        using var tracker = reporter.StartCrawl("job-1", 5);

        tracker.CompleteUrl("https://a.com/1", chunkCount: 3);
        tracker.FailUrl("https://a.com/2", "Timeout", "timeout");
        tracker.CompleteUrl("https://a.com/3", chunkCount: 2);
        tracker.FailUrl("https://a.com/4", "NotFound", "not found", statusCode: 404);
        tracker.CompleteUrl("https://a.com/5", chunkCount: 4);

        var progress = tracker.GetCurrentProgress();
        progress.ProcessedUrls.Should().Be(5);
        progress.SuccessCount.Should().Be(3);
        progress.FailureCount.Should().Be(2);
        progress.TotalChunks.Should().Be(9);
    }

    // --- CrawlProgress computed properties ---

    [Fact]
    public void ProgressPercentage_CalculatesCorrectly()
    {
        var progress = new CrawlProgress { TotalUrls = 10, ProcessedUrls = 3 };

        progress.ProgressPercentage.Should().BeApproximately(0.3, 0.001);
    }

    [Fact]
    public void ProgressPercentage_ZeroTotal_ReturnsZero()
    {
        var progress = new CrawlProgress { TotalUrls = 0, ProcessedUrls = 0 };

        progress.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void SuccessRate_CalculatesCorrectly()
    {
        var progress = new CrawlProgress { ProcessedUrls = 10, SuccessCount = 8 };

        progress.SuccessRate.Should().BeApproximately(0.8, 0.001);
    }

    [Fact]
    public void SuccessRate_ZeroProcessed_ReturnsZero()
    {
        var progress = new CrawlProgress { ProcessedUrls = 0, SuccessCount = 0 };

        progress.SuccessRate.Should().Be(0);
    }

    [Fact]
    public void UrlsPerSecond_CalculatesCorrectly()
    {
        var progress = new CrawlProgress
        {
            ProcessedUrls = 30,
            ElapsedTime = TimeSpan.FromSeconds(10)
        };

        progress.UrlsPerSecond.Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public void UrlsPerSecond_ZeroElapsed_ReturnsZero()
    {
        var progress = new CrawlProgress
        {
            ProcessedUrls = 5,
            ElapsedTime = TimeSpan.Zero
        };

        progress.UrlsPerSecond.Should().Be(0);
    }

    [Fact]
    public void AverageChunksPerUrl_CalculatesCorrectly()
    {
        var progress = new CrawlProgress { TotalChunks = 30, SuccessCount = 6 };

        progress.AverageChunksPerUrl.Should().BeApproximately(5.0, 0.001);
    }

    [Fact]
    public void AverageChunksPerUrl_ZeroSuccess_ReturnsZero()
    {
        var progress = new CrawlProgress { TotalChunks = 10, SuccessCount = 0 };

        progress.AverageChunksPerUrl.Should().Be(0);
    }

    // --- CrawlStatisticsDetails defaults ---

    [Fact]
    public void CrawlStatisticsDetails_Defaults()
    {
        var stats = new CrawlStatisticsDetails();

        stats.TotalBytesDownloaded.Should().Be(0);
        stats.AverageResponseTimeMs.Should().Be(0);
        stats.MinResponseTimeMs.Should().Be(double.MaxValue);
        stats.MaxResponseTimeMs.Should().Be(0);
        stats.UrlsByDomain.Should().BeEmpty();
        stats.ErrorsByType.Should().BeEmpty();
        stats.StatusCodeCounts.Should().BeEmpty();
        stats.ContentTypeCounts.Should().BeEmpty();
    }

    // --- CrawlErrorTypes constants ---

    [Theory]
    [InlineData(nameof(CrawlErrorTypes.Timeout), "Timeout")]
    [InlineData(nameof(CrawlErrorTypes.ConnectionFailed), "ConnectionFailed")]
    [InlineData(nameof(CrawlErrorTypes.DnsFailure), "DnsFailure")]
    [InlineData(nameof(CrawlErrorTypes.NotFound), "NotFound")]
    [InlineData(nameof(CrawlErrorTypes.Forbidden), "Forbidden")]
    [InlineData(nameof(CrawlErrorTypes.RateLimited), "RateLimited")]
    [InlineData(nameof(CrawlErrorTypes.ServerError), "ServerError")]
    [InlineData(nameof(CrawlErrorTypes.RobotsBlocked), "RobotsBlocked")]
    [InlineData(nameof(CrawlErrorTypes.ParseError), "ParseError")]
    [InlineData(nameof(CrawlErrorTypes.Unknown), "Unknown")]
    public void CrawlErrorTypes_ConstantsMatchNames(string fieldName, string expectedValue)
    {
        var field = typeof(CrawlErrorTypes).GetField(fieldName);
        field.Should().NotBeNull();
        field!.GetValue(null).Should().Be(expectedValue);
    }
}
