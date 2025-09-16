using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// llms.txt 메타데이터를 활용한 지능형 크롤러
/// AI 친화적 웹 표준을 통해 최적화된 크롤링 전략 제공
/// </summary>
public class IntelligentCrawler : ICrawler
{
    private readonly IHttpClientService _httpClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILlmsParser _llmsParser;
    private readonly IRobotsTxtParser _robotsTxtParser;
    private readonly ILogger<IntelligentCrawler> _logger;

    private readonly ConcurrentDictionary<string, LlmsMetadata> _siteMetadataCache = new();
    private readonly ConcurrentDictionary<string, RobotsMetadata> _robotsMetadataCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _rateLimits = new();
    private readonly CrawlStatistics _statistics = new CrawlStatistics();
    private readonly object _statsLock = new();

    public IntelligentCrawler(
        IHttpClientService httpClient,
        IServiceProvider serviceProvider,
        ILlmsParser llmsParser,
        IRobotsTxtParser robotsTxtParser,
        ILogger<IntelligentCrawler> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _llmsParser = llmsParser ?? throw new ArgumentNullException(nameof(llmsParser));
        _robotsTxtParser = robotsTxtParser ?? throw new ArgumentNullException(nameof(robotsTxtParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CrawlResult> CrawlAsync(string url, CrawlOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new CrawlOptions();

        _logger.LogInformation("Starting intelligent crawl for {Url}", url);

        // llms.txt 메타데이터 로드 시도
        var baseUrl = GetBaseUrl(url);
        var siteMetadata = await GetSiteMetadataAsync(baseUrl, cancellationToken);

        // 크롤링 옵션 최적화
        if (siteMetadata != null)
        {
            options = await _llmsParser.OptimizeCrawlOptionsAsync(siteMetadata, options);
        }

        // 실제 크롤링 수행
        var startTime = DateTimeOffset.UtcNow;
        try
        {
            // robots.txt 확인
            var robotsMetadata = await GetRobotsMetadataAsync(baseUrl, cancellationToken);
            if (robotsMetadata != null && !_robotsTxtParser.IsUrlAllowed(robotsMetadata, url, options.Headers.GetValueOrDefault("User-Agent", "*")))
            {
                _logger.LogWarning("URL blocked by robots.txt: {Url}", url);
                return new CrawlResult
                {
                    Url = url,
                    FinalUrl = url,
                    StatusCode = 403,
                    IsSuccess = false,
                    ErrorMessage = "URL blocked by robots.txt",
                    CrawledAt = DateTimeOffset.UtcNow
                };
            }

            // Rate limiting 적용
            await ApplyRateLimitAsync(baseUrl, options, siteMetadata, robotsMetadata);

            // HTTP 요청 수행
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var responseTime = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            var htmlContent = response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(cancellationToken)
                : null;

            var discoveredLinks = htmlContent != null ? ExtractLinks(htmlContent, url) : Array.Empty<string>();
            var imageUrls = htmlContent != null ? ExtractImageUrls(htmlContent, url) : Array.Empty<string>();

            // llms.txt 기반 메타데이터 향상
            var metadata = new Dictionary<string, object>();
            if (siteMetadata != null)
            {
                EnhanceWithLlmsMetadata(metadata, url, siteMetadata);
            }

            var result = new CrawlResult
            {
                Url = url,
                FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url,
                StatusCode = (int)response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                HtmlContent = htmlContent,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
                ContentType = response.Content.Headers.ContentType?.ToString(),
                Encoding = response.Content.Headers.ContentEncoding?.FirstOrDefault(),
                ContentLength = response.Content.Headers.ContentLength,
                ResponseTimeMs = responseTime,
                CrawledAt = DateTimeOffset.UtcNow,
                Depth = 0,
                DiscoveredLinks = discoveredLinks,
                ImageUrls = imageUrls,
                Metadata = metadata,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
            };

            UpdateStatistics(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling {Url}", url);

            var errorResult = new CrawlResult
            {
                Url = url,
                FinalUrl = url,
                StatusCode = 0,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Exception = ex,
                CrawledAt = DateTimeOffset.UtcNow,
                Depth = 0,
                ResponseTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };

            UpdateStatistics(errorResult);
            return errorResult;
        }
    }

    public async IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
        string startUrl,
        CrawlOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new CrawlOptions();

        _logger.LogInformation("Starting intelligent website crawl for {StartUrl}", startUrl);

        // llms.txt 메타데이터 로드
        var baseUrl = GetBaseUrl(startUrl);
        var siteMetadata = await GetSiteMetadataAsync(baseUrl, cancellationToken);

        // 크롤링 옵션 최적화
        if (siteMetadata != null)
        {
            options = await _llmsParser.OptimizeCrawlOptionsAsync(siteMetadata, options);
        }

        var visitedUrls = new ConcurrentBag<string>();
        var urlQueue = new PriorityQueue<UrlInfo, int>();

        // 시작 URL 추가
        urlQueue.Enqueue(new UrlInfo(startUrl, 0, null), GetUrlPriority(startUrl, siteMetadata));

        // llms.txt에서 중요한 페이지들 우선순위 큐에 추가
        if (siteMetadata?.ImportantPages != null)
        {
            foreach (var importantPage in siteMetadata.ImportantPages.OrderByDescending(p => p.Priority))
            {
                var fullUrl = ResolveUrl(baseUrl, importantPage.Path);
                if (!string.IsNullOrEmpty(fullUrl) && Uri.IsWellFormedUriString(fullUrl, UriKind.Absolute))
                {
                    var priority = -(importantPage.Priority * 1000); // 높은 우선순위가 먼저 처리되도록
                    urlQueue.Enqueue(new UrlInfo(fullUrl, 0, startUrl), priority);
                }
            }
        }

        var processedCount = 0;
        var maxConcurrency = Math.Min(options.ConcurrentRequests,
            siteMetadata?.CrawlingGuidelines?.MaxConcurrentConnections ?? options.ConcurrentRequests);

        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task>();

        try
        {
            while (urlQueue.Count > 0 && processedCount < options.MaxPages && !cancellationToken.IsCancellationRequested)
            {
                if (!urlQueue.TryDequeue(out var urlInfo, out _))
                    break;

                if (visitedUrls.Contains(urlInfo.Url) || urlInfo.Depth >= options.MaxDepth)
                    continue;

                visitedUrls.Add(urlInfo.Url);

                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var result = await CrawlAsync(urlInfo.Url, options, cancellationToken);

                        // 새로운 링크를 큐에 추가
                        if (result.IsSuccess && result.DiscoveredLinks.Any() && urlInfo.Depth < options.MaxDepth - 1)
                        {
                            foreach (var link in result.DiscoveredLinks.Take(10)) // 링크 수 제한
                            {
                                if (!visitedUrls.Contains(link) && IsValidUrl(link, options))
                                {
                                    var priority = GetUrlPriority(link, siteMetadata) - urlInfo.Depth;
                                    urlQueue.Enqueue(new UrlInfo(link, urlInfo.Depth + 1, urlInfo.Url), priority);
                                }
                            }
                        }

                        return result;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);

                // 완료된 작업 결과 반환
                while (tasks.Any())
                {
                    var completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);

                    if (completedTask.Status == TaskStatus.RanToCompletion)
                    {
                        var result = await completedTask;
                        processedCount++;
                        yield return result;
                    }
                }
            }

            // 남은 작업 완료 대기
            while (tasks.Any())
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);

                if (completedTask.Status == TaskStatus.RanToCompletion)
                {
                    yield return await completedTask;
                }
            }
        }
        finally
        {
            semaphore.Dispose();
        }

        _logger.LogInformation("Completed intelligent website crawl. Processed {ProcessedCount} pages", processedCount);
    }

    public async IAsyncEnumerable<CrawlResult> CrawlSitemapAsync(
        string sitemapUrl,
        CrawlOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new CrawlOptions();

        _logger.LogInformation("Starting intelligent sitemap crawl for {SitemapUrl}", sitemapUrl);

        try
        {
            var response = await _httpClient.GetAsync(sitemapUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch sitemap from {SitemapUrl}: {StatusCode}", sitemapUrl, response.StatusCode);
                yield break;
            }

            var sitemapContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var urls = ExtractUrlsFromSitemap(sitemapContent);

            // llms.txt 메타데이터로 URL 우선순위 조정
            var baseUrl = GetBaseUrl(sitemapUrl);
            var siteMetadata = await GetSiteMetadataAsync(baseUrl, cancellationToken);

            var urlsWithPriority = urls.Select(url => new UrlInfo(url, 0, null))
                                      .OrderByDescending(u => GetUrlPriority(u.Url, siteMetadata))
                                      .Take(options.MaxPages);

            foreach (var urlInfo in urlsWithPriority)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var result = await CrawlAsync(urlInfo.Url, options, cancellationToken);
                yield return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling sitemap {SitemapUrl}", sitemapUrl);
        }
    }

    public async Task<RobotsTxtInfo> GetRobotsTxtAsync(string baseUrl, string userAgent, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken); // Make it async
        return new RobotsTxtInfo(); // Simplified implementation
    }

    public async Task<bool> IsUrlAllowedAsync(string url, string userAgent)
    {
        await Task.Delay(1); // Make it async
        return true; // Simplified - always allow for this implementation
    }

    public IReadOnlyList<string> ExtractLinks(string htmlContent, string baseUrl)
    {
        var links = new HashSet<string>();

        // href 속성을 가진 a 태그에서 링크 추출
        var linkMatches = Regex.Matches(htmlContent,
            @"<a[^>]+href\s*=\s*[""']([^""']*)[""'][^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in linkMatches)
        {
            var href = match.Groups[1].Value;
            var resolvedUrl = ResolveUrl(baseUrl, href);

            if (!string.IsNullOrEmpty(resolvedUrl) &&
                Uri.IsWellFormedUriString(resolvedUrl, UriKind.Absolute))
            {
                links.Add(resolvedUrl);
            }
        }

        return links.ToList();
    }

    public CrawlStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            return _statistics;
        }
    }

    private async Task<LlmsMetadata?> GetSiteMetadataAsync(string baseUrl, CancellationToken cancellationToken)
    {
        if (_siteMetadataCache.TryGetValue(baseUrl, out var cachedMetadata))
        {
            return cachedMetadata;
        }

        try
        {
            var parseResult = await _llmsParser.ParseFromWebsiteAsync(baseUrl, cancellationToken);

            if (parseResult?.IsSuccess == true && parseResult.FileFound && parseResult.Metadata != null)
            {
                _siteMetadataCache.TryAdd(baseUrl, parseResult.Metadata);
                return parseResult.Metadata;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load llms.txt metadata for {BaseUrl}", baseUrl);
        }

        return null;
    }

    private async Task<RobotsMetadata?> GetRobotsMetadataAsync(string baseUrl, CancellationToken cancellationToken)
    {
        if (_robotsMetadataCache.TryGetValue(baseUrl, out var cachedMetadata))
        {
            return cachedMetadata;
        }

        try
        {
            var parseResult = await _robotsTxtParser.ParseFromWebsiteAsync(baseUrl, cancellationToken);

            if (parseResult?.IsSuccess == true && parseResult.FileFound && parseResult.Metadata != null)
            {
                _robotsMetadataCache.TryAdd(baseUrl, parseResult.Metadata);
                return parseResult.Metadata;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load robots.txt metadata for {BaseUrl}", baseUrl);
        }

        return null;
    }

    private async Task ApplyRateLimitAsync(string baseUrl, CrawlOptions options, LlmsMetadata? siteMetadata, RobotsMetadata? robotsMetadata)
    {
        var domain = new Uri(baseUrl).Host;

        if (_rateLimits.TryGetValue(domain, out var lastRequest))
        {
            TimeSpan delay;

            // robots.txt Crawl-Delay를 우선 사용
            if (robotsMetadata?.CrawlDelay != null)
            {
                delay = robotsMetadata.CrawlDelay.Value;
            }
            // llms.txt에서 지정된 요청 속도 제한 사용
            else if (siteMetadata?.CrawlingGuidelines?.RecommendedRateLimit != null)
            {
                delay = TimeSpan.FromSeconds(1.0 / siteMetadata.CrawlingGuidelines.RecommendedRateLimit.Value);
            }
            // 옵션에서 지정된 지연 시간 사용
            else if (options.DelayBetweenRequests != null)
            {
                delay = options.DelayBetweenRequests.Value;
            }
            else
            {
                delay = TimeSpan.FromMilliseconds(options.DelayBetweenRequestsMs);
            }

            var timeSinceLastRequest = DateTime.UtcNow - lastRequest;
            if (timeSinceLastRequest < delay)
            {
                var waitTime = delay - timeSinceLastRequest;
                await Task.Delay(waitTime);
            }
        }

        _rateLimits.TryAdd(domain, DateTime.UtcNow);
        _rateLimits[domain] = DateTime.UtcNow;
    }

    private int GetUrlPriority(string url, LlmsMetadata? siteMetadata)
    {
        if (siteMetadata?.ImportantPages == null)
            return 5; // 기본 우선순위

        var urlPath = new Uri(url).AbsolutePath;

        // 중요한 페이지 목록에서 우선순위 찾기
        var importantPage = siteMetadata.ImportantPages.FirstOrDefault(p =>
            urlPath.StartsWith(p.Path, StringComparison.OrdinalIgnoreCase));

        if (importantPage != null)
            return importantPage.Priority;

        // 섹션 기반 우선순위
        var relatedSection = siteMetadata.Sections?.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.Path) && urlPath.StartsWith(s.Path, StringComparison.OrdinalIgnoreCase));

        return relatedSection?.Priority ?? 5;
    }

    private void EnhanceWithLlmsMetadata(Dictionary<string, object> metadata, string url, LlmsMetadata siteMetadata)
    {
        metadata["llms_site_name"] = siteMetadata.SiteName;
        metadata["llms_description"] = siteMetadata.Description;
        metadata["crawling_strategy"] = "intelligent_llms_guided";

        // 관련 섹션 정보 추가
        var relatedSection = FindRelatedSection(url, siteMetadata);
        if (relatedSection != null)
        {
            metadata["llms_section"] = relatedSection.Name;
            metadata["llms_section_description"] = relatedSection.Description;
            metadata["llms_content_type"] = relatedSection.ContentType;
            metadata["llms_section_priority"] = relatedSection.Priority;
        }

        // 중요한 페이지 정보 추가
        var importantPage = siteMetadata.ImportantPages?.FirstOrDefault(p =>
            url.EndsWith(p.Path, StringComparison.OrdinalIgnoreCase));

        if (importantPage != null)
        {
            metadata["llms_important_page"] = true;
            metadata["llms_page_type"] = importantPage.PageType;
            metadata["llms_page_description"] = importantPage.Description;
        }
    }

    private LlmsSection? FindRelatedSection(string url, LlmsMetadata siteMetadata)
    {
        if (siteMetadata.Sections?.Any() != true)
            return null;

        var urlPath = new Uri(url).AbsolutePath;

        return siteMetadata.Sections
            .Where(s => !string.IsNullOrEmpty(s.Path))
            .OrderByDescending(s => CalculatePathSimilarity(urlPath, s.Path))
            .FirstOrDefault();
    }

    private int CalculatePathSimilarity(string urlPath, string sectionPath)
    {
        var urlSegments = urlPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var sectionSegments = sectionPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var matchingSegments = 0;
        var minLength = Math.Min(urlSegments.Length, sectionSegments.Length);

        for (int i = 0; i < minLength; i++)
        {
            if (string.Equals(urlSegments[i], sectionSegments[i], StringComparison.OrdinalIgnoreCase))
                matchingSegments++;
            else
                break;
        }

        return matchingSegments;
    }

    private IReadOnlyList<string> ExtractImageUrls(string htmlContent, string baseUrl)
    {
        var images = new HashSet<string>();

        var imgMatches = Regex.Matches(htmlContent,
            @"<img[^>]+src\s*=\s*[""']([^""']*)[""'][^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in imgMatches)
        {
            var src = match.Groups[1].Value;
            var resolvedUrl = ResolveUrl(baseUrl, src);

            if (!string.IsNullOrEmpty(resolvedUrl))
            {
                images.Add(resolvedUrl);
            }
        }

        return images.ToList();
    }

    private IReadOnlyList<string> ExtractUrlsFromSitemap(string sitemapContent)
    {
        var urls = new List<string>();

        var urlMatches = Regex.Matches(sitemapContent,
            @"<loc>(.*?)</loc>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (Match match in urlMatches)
        {
            var url = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                urls.Add(url);
            }
        }

        return urls;
    }

    // ParseRobotsTxt method removed for simplified implementation

    private void UpdateStatistics(CrawlResult result)
    {
        lock (_statsLock)
        {
            // Statistics 업데이트 로직은 실제 구현에서 필요할 때 추가
        }
    }

    private bool IsValidUrl(string url, CrawlOptions options)
    {
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return false;

        var uri = new Uri(url);

        // 도메인 제한 확인
        if (options.AllowedDomains.Any() && !options.AllowedDomains.Contains(uri.Host))
            return false;

        // 확장자 제한 확인
        var extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();
        if (options.ExcludedExtensions.Contains(extension))
            return false;

        // URL 패턴 확인
        if (options.ExcludeUrlPatterns.Any(pattern => Regex.IsMatch(url, pattern)))
            return false;

        if (options.IncludeUrlPatterns.Any() && !options.IncludeUrlPatterns.Any(pattern => Regex.IsMatch(url, pattern)))
            return false;

        return true;
    }

    private string GetBaseUrl(string url)
    {
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
    }

    private string? ResolveUrl(string baseUrl, string relativePath)
    {
        try
        {
            if (Uri.IsWellFormedUriString(relativePath, UriKind.Absolute))
                return relativePath;

            var baseUri = new Uri(baseUrl);
            var resolvedUri = new Uri(baseUri, relativePath);
            return resolvedUri.ToString();
        }
        catch
        {
            return null;
        }
    }

    private record UrlInfo(string Url, int Depth, string? ParentUrl);
}