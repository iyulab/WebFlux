using WebFlux.Core.Interfaces;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// What is worth asking again. One definition, used by whichever layer owns the retries for an
/// entry point - the crawler for a direct crawl, the extract path for a single URL - so the two
/// never nest and never disagree.
/// </summary>
internal static class CrawlRetryPolicy
{
    /// <summary>
    /// True for a status a second request can change: 408, 429 and 5xx. A 404, 403 or 410 is an
    /// answer, not a failure to get one.
    /// </summary>
    public static bool IsRetryableStatus(int statusCode) =>
        statusCode is 408 or 429 || statusCode is >= 500 and < 600;

    /// <summary>
    /// True for a result a second attempt can change: a retryable status, or no response at all
    /// (a transport error). A robots.txt refusal, a timeout and a cancellation are final.
    /// </summary>
    public static bool IsRetryable(CrawlResult result)
    {
        if (result.IsSuccess || result.DisallowedByRobotsTxt || result.TimedOut) return false;
        if (result.Exception is OperationCanceledException) return false;
        return result.StatusCode == 0 || IsRetryableStatus(result.StatusCode);
    }

    /// <summary>Exponential backoff: 1 s, 2 s, 4 s ...</summary>
    public static TimeSpan Backoff(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt));
}
