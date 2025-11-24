using System.Runtime.CompilerServices;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 지능형 크롤링 전략 기본 구현
/// Interface Provider 패턴에 따라 기본 구현만 제공
/// </summary>
public class IntelligentCrawler : ICrawler
{
    public Task<CrawlResult> CrawlAsync(string url, CrawlOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = new CrawlResult
        {
            Url = url,
            IsSuccessful = true,
            Content = $"Basic Intelligent crawl result for {url}",
            ContentType = "text/html",
            StatusCode = 200
        };
        return Task.FromResult(result);
    }

    public async IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(string startUrl, CrawlOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return await CrawlAsync(startUrl, options, cancellationToken);
    }

    public async IAsyncEnumerable<CrawlResult> CrawlSitemapAsync(string sitemapUrl, CrawlOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return await CrawlAsync(sitemapUrl, options, cancellationToken);
    }

    public Task<RobotsTxtInfo> GetRobotsTxtAsync(string baseUrl, string userAgent, CancellationToken cancellationToken = default)
    {
        var robotsInfo = new RobotsTxtInfo
        {
            Content = "# Basic robots.txt"
        };
        return Task.FromResult(robotsInfo);
    }

    public Task<bool> IsUrlAllowedAsync(string url, string userAgent)
    {
        return Task.FromResult(true);
    }

    public IReadOnlyList<string> ExtractLinks(string htmlContent, string baseUrl)
    {
        return new List<string>();
    }

    public CrawlStatistics GetStatistics()
    {
        return new CrawlStatistics
        {
            TotalRequests = 0,
            SuccessfulRequests = 0,
            FailedRequests = 0
        };
    }
}