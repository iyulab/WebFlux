using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 크롤러 기본 구현체
/// 모든 크롤러의 공통 기능 제공
/// </summary>
public abstract class BaseCrawler : ICrawler
{
    protected readonly IHttpClientService _httpClient;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly ConcurrentQueue<string> _urlQueue = new();
    protected readonly HashSet<string> _visitedUrls = new();
    protected readonly HashSet<string> _failedUrls = new();
    protected readonly object _lockObject = new();

    protected CrawlStatistics _statistics = new();
    protected CancellationTokenSource? _cancellationTokenSource;
    protected bool _isRunning;
    protected int _processedCount;
    protected int _successCount;
    protected int _errorCount;
    protected DateTimeOffset _startTime;

    protected BaseCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// 단일 URL을 크롤링합니다.
    /// </summary>
    /// <param name="url">크롤링할 URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 웹 페이지 정보</returns>
    public virtual async Task<CrawlResult> CrawlAsync(
        string url,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            await _eventPublisher.PublishAsync(new UrlProcessingStartedEvent
            {
                Url = url,
                Timestamp = startTime
            }, cancellationToken);

            using var response = await _httpClient.GetAsync(url, cancellationToken: cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseTime = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            var discoveredLinks = ExtractLinks(content, url);

            var result = new CrawlResult
            {
                Url = url,
                FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url,
                StatusCode = (int)response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                HtmlContent = response.IsSuccessStatusCode ? content : null,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                ContentType = response.Content.Headers.ContentType?.MediaType,
                Encoding = response.Content.Headers.ContentType?.CharSet,
                ContentLength = response.Content.Headers.ContentLength,
                ResponseTimeMs = responseTime,
                CrawledAt = DateTimeOffset.UtcNow,
                Depth = 0,
                DiscoveredLinks = discoveredLinks,
                ErrorMessage = response.IsSuccessStatusCode ? null : response.ReasonPhrase
            };

            UpdateStatistics(result);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new CrawlResult
            {
                Url = url,
                FinalUrl = url,
                StatusCode = 0,
                IsSuccess = false,
                ResponseTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds,
                CrawledAt = DateTimeOffset.UtcNow,
                Depth = 0,
                DiscoveredLinks = Array.Empty<string>(),
                ErrorMessage = ex.Message,
                Exception = ex
            };

            UpdateStatistics(errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// 웹사이트를 전체적으로 크롤링합니다.
    /// </summary>
    /// <param name="startUrl">시작 URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>발견된 URL과 크롤링 결과 스트림</returns>
    public virtual async IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
        string startUrl,
        CrawlOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(startUrl))
            throw new ArgumentException("Start URL cannot be null or empty", nameof(startUrl));

        _startTime = DateTimeOffset.UtcNow;
        var queue = new Queue<(string url, int depth)>();
        var visited = new HashSet<string>();
        var maxDepth = options?.MaxDepth ?? 3;
        var maxPages = options?.MaxPages ?? 100;

        queue.Enqueue((startUrl, 0));

        while (queue.Count > 0 && visited.Count < maxPages && !cancellationToken.IsCancellationRequested)
        {
            var (currentUrl, depth) = queue.Dequeue();

            if (visited.Contains(currentUrl) || depth > maxDepth)
                continue;

            visited.Add(currentUrl);

            var originalResult = await CrawlAsync(currentUrl, options, cancellationToken);
            var result = new CrawlResult
            {
                Url = originalResult.Url,
                FinalUrl = originalResult.FinalUrl,
                StatusCode = originalResult.StatusCode,
                IsSuccess = originalResult.IsSuccess,
                HtmlContent = originalResult.HtmlContent,
                Headers = originalResult.Headers,
                ContentType = originalResult.ContentType,
                Encoding = originalResult.Encoding,
                ContentLength = originalResult.ContentLength,
                ResponseTimeMs = originalResult.ResponseTimeMs,
                CrawledAt = originalResult.CrawledAt,
                Depth = depth, // Set the desired depth
                ParentUrl = originalResult.ParentUrl,
                DiscoveredLinks = originalResult.DiscoveredLinks,
                ErrorMessage = originalResult.ErrorMessage,
                Exception = originalResult.Exception,
                ImageUrls = originalResult.ImageUrls,
                Metadata = originalResult.Metadata,
                WebMetadata = originalResult.WebMetadata
            };

            yield return result;

            if (result.IsSuccess && depth < maxDepth)
            {
                foreach (var link in result.DiscoveredLinks)
                {
                    if (!visited.Contains(link) && ShouldCrawlUrl(link, startUrl))
                    {
                        queue.Enqueue((link, depth + 1));
                    }
                }
            }

            // 예의상 지연
            if (options?.DelayMs > 0)
            {
                await Task.Delay(options.DelayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Sitemap을 기반으로 크롤링합니다.
    /// </summary>
    /// <param name="sitemapUrl">sitemap.xml URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링 결과 스트림</returns>
    public virtual async IAsyncEnumerable<CrawlResult> CrawlSitemapAsync(
        string sitemapUrl,
        CrawlOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sitemapUrl))
            throw new ArgumentException("Sitemap URL cannot be null or empty", nameof(sitemapUrl));

        var urls = await ExtractUrlsFromSitemapAsync(sitemapUrl, cancellationToken);

        foreach (var url in urls)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return await CrawlAsync(url, options, cancellationToken);

            if (options?.DelayMs > 0)
            {
                await Task.Delay(options.DelayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// robots.txt를 확인합니다.
    /// </summary>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="userAgent">User-Agent</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>robots.txt 정보</returns>
    public virtual async Task<RobotsTxtInfo> GetRobotsTxtAsync(
        string baseUrl,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        var robotsUrl = new Uri(new Uri(baseUrl), "/robots.txt").ToString();

        try
        {
            using var response = await _httpClient.GetAsync(robotsUrl, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new RobotsTxtInfo();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRobotsTxt(content);
        }
        catch
        {
            return new RobotsTxtInfo();
        }
    }

    /// <summary>
    /// URL이 크롤링 가능한지 확인합니다.
    /// </summary>
    /// <param name="url">확인할 URL</param>
    /// <param name="userAgent">User-Agent</param>
    /// <returns>크롤링 가능 여부</returns>
    public virtual async Task<bool> IsUrlAllowedAsync(string url, string userAgent)
    {
        try
        {
            var baseUrl = $"{new Uri(url).Scheme}://{new Uri(url).Host}";
            var robotsInfo = await GetRobotsTxtAsync(baseUrl, userAgent);

            if (robotsInfo.Rules.TryGetValue(userAgent, out var rules) ||
                robotsInfo.Rules.TryGetValue("*", out rules))
            {
                var path = new Uri(url).PathAndQuery;

                // 허용된 경로 확인
                if (rules.AllowedPaths.Any(pattern => path.StartsWith(pattern)))
                    return true;

                // 금지된 경로 확인
                if (rules.DisallowedPaths.Any(pattern => path.StartsWith(pattern)))
                    return false;
            }

            return true;
        }
        catch
        {
            return true; // robots.txt 파싱 실패 시 허용
        }
    }

    /// <summary>
    /// 링크를 추출합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="baseUrl">기본 URL</param>
    /// <returns>추출된 링크 목록</returns>
    public virtual IReadOnlyList<string> ExtractLinks(string htmlContent, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return Array.Empty<string>();

        var links = new List<string>();
        var baseUri = new Uri(baseUrl);

        // 간단한 링크 추출 (실제 구현에서는 HTML 파서 사용 권장)
        var linkMatches = System.Text.RegularExpressions.Regex.Matches(
            htmlContent,
            @"href\s*=\s*[""']([^""']*)[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in linkMatches)
        {
            var href = match.Groups[1].Value;

            // javascript:, mailto:, tel: 등의 프로토콜과 anchor만 있는 링크 제외
            if (string.IsNullOrWhiteSpace(href) ||
                href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("#"))
            {
                continue;
            }

            if (Uri.TryCreate(baseUri, href, out var absoluteUri))
            {
                // http 또는 https 스킴만 허용
                if (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps)
                {
                    links.Add(absoluteUri.ToString());
                }
            }
        }

        return links.AsReadOnly();
    }

    /// <summary>
    /// 크롤링 통계를 반환합니다.
    /// </summary>
    /// <returns>크롤링 통계 정보</returns>
    public virtual CrawlStatistics GetStatistics()
    {
        return _statistics;
    }

    /// <summary>
    /// Sitemap에서 URL 추출
    /// </summary>
    /// <param name="sitemapUrl">Sitemap URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>URL 목록</returns>
    protected virtual async Task<IEnumerable<string>> ExtractUrlsFromSitemapAsync(
        string sitemapUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(sitemapUrl, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Array.Empty<string>();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // 간단한 XML 파싱 (실제로는 XDocument 사용 권장)
            var urlMatches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"<loc>([^<]+)</loc>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return urlMatches.Cast<System.Text.RegularExpressions.Match>()
                            .Select(m => m.Groups[1].Value.Trim())
                            .Where(url => !string.IsNullOrWhiteSpace(url))
                            .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// robots.txt 파싱
    /// </summary>
    /// <param name="content">robots.txt 내용</param>
    /// <returns>파싱된 robots.txt 정보</returns>
    protected virtual RobotsTxtInfo ParseRobotsTxt(string content)
    {
        var rules = new Dictionary<string, RobotRules>();
        var sitemaps = new List<string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? currentUserAgent = null;
        var allowedPaths = new List<string>();
        var disallowedPaths = new List<string>();
        int? crawlDelay = null;

        foreach (var line in lines)
        {
            var cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith('#'))
                continue;

            var colonIndex = cleanLine.IndexOf(':');
            if (colonIndex == -1) continue;

            var directive = cleanLine.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = cleanLine.Substring(colonIndex + 1).Trim();

            switch (directive)
            {
                case "user-agent":
                    // 이전 user-agent 규칙 저장
                    if (currentUserAgent != null)
                    {
                        rules[currentUserAgent] = new RobotRules
                        {
                            AllowedPaths = allowedPaths.AsReadOnly(),
                            DisallowedPaths = disallowedPaths.AsReadOnly(),
                            CrawlDelay = crawlDelay
                        };
                    }

                    currentUserAgent = value;
                    allowedPaths = new List<string>();
                    disallowedPaths = new List<string>();
                    crawlDelay = null;
                    break;

                case "allow":
                    allowedPaths.Add(value);
                    break;

                case "disallow":
                    disallowedPaths.Add(value);
                    break;

                case "crawl-delay":
                    if (int.TryParse(value, out var delay))
                        crawlDelay = delay;
                    break;

                case "sitemap":
                    sitemaps.Add(value);
                    break;
            }
        }

        // 마지막 user-agent 규칙 저장
        if (currentUserAgent != null)
        {
            rules[currentUserAgent] = new RobotRules
            {
                AllowedPaths = allowedPaths.AsReadOnly(),
                DisallowedPaths = disallowedPaths.AsReadOnly(),
                CrawlDelay = crawlDelay
            };
        }

        return new RobotsTxtInfo
        {
            Content = content,
            Rules = rules.AsReadOnly(),
            Sitemaps = sitemaps.AsReadOnly(),
            CrawlDelay = crawlDelay
        };
    }

    /// <summary>
    /// 통계 업데이트
    /// </summary>
    /// <param name="result">크롤링 결과</param>
    protected virtual void UpdateStatistics(CrawlResult result)
    {
        var domain = new Uri(result.Url).Host;
        var domainCounts = _statistics.RequestsByDomain.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        domainCounts[domain] = domainCounts.GetValueOrDefault(domain, 0) + 1;

        var statusCounts = _statistics.StatusCodeDistribution.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        statusCounts[result.StatusCode] = statusCounts.GetValueOrDefault(result.StatusCode, 0) + 1;

        var totalRequests = _statistics.TotalRequests + 1;
        var successfulRequests = _statistics.SuccessfulRequests + (result.IsSuccess ? 1 : 0);
        var failedRequests = _statistics.FailedRequests + (result.IsSuccess ? 0 : 1);
        var totalBytes = _statistics.TotalBytesProcessed + (result.ContentLength ?? 0);

        var elapsedTime = DateTimeOffset.UtcNow - _startTime;
        var requestsPerSecond = elapsedTime.TotalSeconds > 0 ? totalRequests / elapsedTime.TotalSeconds : 0;

        _statistics = new CrawlStatistics
        {
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            AverageResponseTimeMs = (_statistics.AverageResponseTimeMs * (_statistics.TotalRequests) + result.ResponseTimeMs) / totalRequests,
            TotalBytesProcessed = totalBytes,
            RequestsPerSecond = requestsPerSecond,
            RequestsByDomain = domainCounts.AsReadOnly(),
            StatusCodeDistribution = statusCounts.AsReadOnly(),
            StartTime = _startTime,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// URL 검증
    /// </summary>
    /// <param name="url">검증할 URL</param>
    /// <returns>유효 여부</returns>
    protected virtual bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// URL 크롤링 여부 결정
    /// </summary>
    /// <param name="url">URL</param>
    /// <param name="baseUrl">기준 URL</param>
    /// <returns>크롤링 여부</returns>
    protected virtual bool ShouldCrawlUrl(string url, string? baseUrl = null)
    {
        if (!IsValidUrl(url))
            return false;

        // 같은 도메인만 크롤링 (기본 정책)
        if (!string.IsNullOrEmpty(baseUrl))
        {
            var urlDomain = new Uri(url).Host;
            var baseDomain = new Uri(baseUrl).Host;

            if (!urlDomain.Equals(baseDomain, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }


    /// <summary>
    /// HTML 콘텐츠에서 제목 추출
    /// </summary>
    /// <param name="content">HTML 콘텐츠</param>
    /// <returns>추출된 제목</returns>
    protected virtual string ExtractTitle(string content)
    {
        var titleMatch = System.Text.RegularExpressions.Regex.Match(
            content,
            @"<title[^>]*>([^<]*)</title>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "No title";
    }


    /// <summary>
    /// 리소스 정리
    /// </summary>
    public virtual void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _isRunning = false;
    }
}