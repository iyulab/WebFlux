using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Models.Events;
using WebFlux.Core.Options;
using WebFlux.Core.Utilities;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 크롤러 기본 구현체
/// 모든 크롤러의 공통 기능 제공
/// </summary>
public abstract class BaseCrawler : ICrawler
{
    /// <summary>
    /// robots.txt per host and user agent, so honouring the option costs one fetch per host rather
    /// than one per URL. Keyed by scheme+host+user-agent; the task is cached so concurrent workers
    /// share a single fetch.
    /// </summary>
    private readonly ConcurrentDictionary<string, Task<RobotsTxtInfo>> _robotsCache = new(StringComparer.OrdinalIgnoreCase);

    protected IHttpClientService HttpClient { get; }
    protected IEventPublisher EventPublisher { get; }
    protected ConcurrentQueue<string> UrlQueue { get; } = new();
    protected HashSet<string> VisitedUrls { get; } = new();
    protected HashSet<string> FailedUrls { get; } = new();
    protected object LockObject { get; } = new();

    protected CrawlStatistics Statistics { get; set; } = new();
    protected CancellationTokenSource? CancellationTokenSourceInstance { get; set; }
    protected bool IsRunning { get; set; }
    protected int ProcessedCount { get; set; }
    protected int SuccessCount { get; set; }
    protected int ErrorCount { get; set; }
    protected DateTimeOffset StartTime { get; set; }

    protected BaseCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        EventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// 단일 URL을 크롤링합니다.
    /// 지수 백오프 재시도 및 HTTP 429 Rate Limiting을 지원합니다.
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
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        // The single-URL entry point honours robots.txt too: a caller that fetches pages one at a
        // time never goes through the crawl loop, so a gate that lives only there never sees them.
        if (!await IsAllowedByRobotsAsync(url, options, cancellationToken))
            return CreateDisallowedByRobotsResult(url);

        var maxRetries = options?.MaxRetries ?? 3;
        var startTime = DateTimeOffset.UtcNow;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt == 0)
                {
                    await EventPublisher.PublishAsync(new UrlProcessingStartedEvent
                    {
                        Url = url,
                        Timestamp = startTime
                    }, cancellationToken);
                }

                using var response = await HttpClient.GetAsync(url, cancellationToken: cancellationToken);

                // HTTP 429 Too Many Requests 처리
                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxRetries)
                {
                    var retryAfter = GetRetryAfterDelay(response);
                    await Task.Delay(retryAfter, cancellationToken);
                    continue;
                }

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
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                lastException = ex;
                // 지수 백오프: 1초, 2초, 4초, 8초...
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < maxRetries)
            {
                // 타임아웃으로 인한 취소 (사용자 취소가 아닌 경우)
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        // 모든 재시도 실패
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
            ErrorMessage = lastException?.Message ?? "Unknown error after retries",
            Exception = lastException
        };

        UpdateStatistics(errorResult);
        return errorResult;
    }

    /// <summary>
    /// HTTP 응답에서 Retry-After 헤더를 파싱하여 대기 시간을 반환합니다.
    /// </summary>
    /// <param name="response">HTTP 응답</param>
    /// <returns>대기 시간 (기본값: 60초)</returns>
    protected virtual TimeSpan GetRetryAfterDelay(HttpResponseMessage response)
    {
        // Retry-After 헤더가 있는 경우
        if (response.Headers.RetryAfter != null)
        {
            // Delta (초 단위) 형식
            if (response.Headers.RetryAfter.Delta.HasValue)
            {
                return response.Headers.RetryAfter.Delta.Value;
            }

            // Date 형식
            if (response.Headers.RetryAfter.Date.HasValue)
            {
                var delay = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    return delay;
                }
            }
        }

        // 기본값: 60초
        return TimeSpan.FromSeconds(60);
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

        StartTime = DateTimeOffset.UtcNow;
        var queue = new Queue<(string url, int depth)>();
        var visited = new HashSet<string>();
        var maxDepth = options?.MaxDepth ?? 3;
        var maxPages = options?.MaxPages ?? 100;

        queue.Enqueue((startUrl, 0));

        while (queue.Count > 0 && visited.Count < maxPages && !cancellationToken.IsCancellationRequested)
        {
            var (currentUrl, depth) = queue.Dequeue();

            if (visited.Contains(UrlNormalizer.Normalize(currentUrl)) || depth > maxDepth)
                continue;

            visited.Add(UrlNormalizer.Normalize(currentUrl));

            if (!await IsAllowedByRobotsAsync(currentUrl, options, cancellationToken))
                continue;

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
                    if (!visited.Contains(UrlNormalizer.Normalize(link)) && ShouldCrawlUrl(link, startUrl, options))
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
    /// 웹사이트를 병렬로 크롤링합니다.
    /// ConcurrentRequests 옵션을 사용하여 동시 요청 수를 제어합니다.
    /// </summary>
    /// <param name="startUrl">시작 URL</param>
    /// <param name="options">크롤링 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링 결과 스트림 (완료 순서대로 반환)</returns>
    public virtual async IAsyncEnumerable<CrawlResult> CrawlWebsiteParallelAsync(
        string startUrl,
        CrawlOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(startUrl))
            throw new ArgumentException("Start URL cannot be null or empty", nameof(startUrl));

        StartTime = DateTimeOffset.UtcNow;

        var maxDepth = options?.MaxDepth ?? 3;
        var maxPages = options?.MaxPages ?? 100;
        var concurrency = options?.ConcurrentRequests ?? 3;
        var delayMs = options?.DelayMs ?? 0;

        // 결과 채널 (bounded로 메모리 관리)
        var channel = Channel.CreateBounded<CrawlResult>(new BoundedChannelOptions(maxPages)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        // 방문한 URL 및 대기열 관리
        var visited = new ConcurrentDictionary<string, bool>();
        var pendingUrls = new ConcurrentQueue<(string url, int depth)>();
        var semaphore = new SemaphoreSlim(concurrency, concurrency);
        var activeWorkers = 0;
        var completedCount = 0;

        pendingUrls.Enqueue((startUrl, 0));

        // Producer 작업 시작
        var producerTask = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // 대기열에서 URL 가져오기
                    if (!pendingUrls.TryDequeue(out var item))
                    {
                        // 대기열이 비어있고 활성 워커가 없으면 종료
                        if (Volatile.Read(ref activeWorkers) == 0)
                        {
                            break;
                        }

                        // 잠시 대기 후 다시 확인
                        await Task.Delay(10, cancellationToken);
                        continue;
                    }

                    var (url, depth) = item;

                    // 이미 방문했거나 최대 페이지 수 초과 시 스킵
                    if (!visited.TryAdd(UrlNormalizer.Normalize(url), true) || Volatile.Read(ref completedCount) >= maxPages)
                    {
                        continue;
                    }

                    // 깊이 제한 확인
                    if (depth > maxDepth)
                    {
                        continue;
                    }

                    // robots.txt 확인 (RespectRobotsTxt 가 켜진 경우)
                    if (!await IsAllowedByRobotsAsync(url, options, cancellationToken))
                    {
                        continue;
                    }

                    // 동시성 제한
                    await semaphore.WaitAsync(cancellationToken);
                    Interlocked.Increment(ref activeWorkers);

                    // 비동기로 크롤링 수행
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var originalResult = await CrawlAsync(url, options, cancellationToken);
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
                                Depth = depth,
                                ParentUrl = originalResult.ParentUrl,
                                DiscoveredLinks = originalResult.DiscoveredLinks,
                                ErrorMessage = originalResult.ErrorMessage,
                                Exception = originalResult.Exception,
                                ImageUrls = originalResult.ImageUrls,
                                Metadata = originalResult.Metadata,
                                WebMetadata = originalResult.WebMetadata
                            };

                            // 채널에 결과 쓰기
                            await channel.Writer.WriteAsync(result, cancellationToken);
                            Interlocked.Increment(ref completedCount);

                            // 새로운 링크 추가
                            if (result.IsSuccess && depth < maxDepth && Volatile.Read(ref completedCount) < maxPages)
                            {
                                foreach (var link in result.DiscoveredLinks)
                                {
                                    if (!visited.ContainsKey(UrlNormalizer.Normalize(link)) && ShouldCrawlUrl(link, startUrl, options))
                                    {
                                        pendingUrls.Enqueue((link, depth + 1));
                                    }
                                }
                            }

                            // 예의상 지연
                            if (delayMs > 0)
                            {
                                await Task.Delay(delayMs, cancellationToken);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // 취소됨 - 정상 종료
                        }
                        catch
                        {
                            // 개별 URL 오류는 무시 (이미 CrawlAsync에서 처리됨)
                        }
                        finally
                        {
                            semaphore.Release();
                            Interlocked.Decrement(ref activeWorkers);
                        }
                    }, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소됨 - 정상 종료
            }
            finally
            {
                // 모든 활성 워커가 완료될 때까지 대기
                while (Volatile.Read(ref activeWorkers) > 0)
                {
                    await Task.Delay(10, CancellationToken.None);
                }

                channel.Writer.Complete();
                semaphore.Dispose();
            }
        }, cancellationToken);

        // Consumer: 채널에서 결과 읽기
        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }

        // Producer 완료 대기
        await producerTask;
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
            using var response = await HttpClient.GetAsync(robotsUrl, cancellationToken: cancellationToken);

            // RFC 9309 section 2.3.1.4: a 5xx means the file is undefined, and an undefined
            // robots.txt is a complete disallow — not the same as a site that has no rules.
            if ((int)response.StatusCode >= 500)
            {
                return new RobotsTxtInfo { Access = RobotsTxtAccess.Unreachable };
            }

            // Section 2.3.1.3: any 4xx means the file is unavailable, and every resource is allowed.
            if (!response.IsSuccessStatusCode)
            {
                return new RobotsTxtInfo { Access = RobotsTxtAccess.Unavailable };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseRobotsTxt(content);
        }
        catch
        {
            // A transport failure (DNS, TLS, timeout) is treated as unavailable rather than
            // unreachable. Section 2.3.1.4 lets a crawler that has been unable to reach the file
            // for a long time fall back to "unavailable"; this library holds no state across calls,
            // so it cannot tell a first failure from a persistent one, and refusing every page of a
            // site because its robots.txt timed out once is the worse of the two errors.
            return new RobotsTxtInfo { Access = RobotsTxtAccess.Unavailable };
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
            var baseUrl = new Uri(url).GetLeftPart(UriPartial.Authority);
            return IsPathAllowed(await GetRobotsTxtAsync(baseUrl, userAgent), url, userAgent);
        }
        catch
        {
            return true; // robots.txt 파싱 실패 시 허용
        }
    }

    /// <summary>
    /// Applies a fetched robots.txt to one URL. Split out so the public per-call check and the
    /// crawl loop's cached check cannot drift on what the rules mean — only on when they are fetched.
    /// </summary>
    private static bool IsPathAllowed(RobotsTxtInfo robotsInfo, string url, string userAgent)
        => RobotsTxt.IsAllowed(robotsInfo, url, userAgent);

    /// <summary>
    /// The robots.txt gate, honouring <see cref="CrawlOptions.RespectRobotsTxt"/>. Both
    /// <see cref="CrawlAsync"/> and the crawl loop go through it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ShouldCrawlUrl</c> is synchronous and robots.txt needs a fetch, which is why the option
    /// went unwired: the library shipped <c>IsUrlAllowedAsync</c>, the README promised robots.txt
    /// handling and set <c>RespectRobotsTxt = true</c> in its example, and nothing in the crawl path
    /// ever called it.
    /// </para>
    /// <para>
    /// robots.txt is fetched once per host per crawl rather than once per URL — the public
    /// <see cref="IsUrlAllowedAsync"/> keeps its per-call fetch, since a caller asking about a single
    /// URL is not in a loop.
    /// </para>
    /// </remarks>
    protected virtual async Task<bool> IsAllowedByRobotsAsync(
        string url,
        CrawlOptions? options,
        CancellationToken cancellationToken = default)
    {
        // No options means the defaults, and the default is to respect robots.txt.
        if (options is { RespectRobotsTxt: false })
            return true;

        try
        {
            var uri = new Uri(url);
            var userAgent = string.IsNullOrWhiteSpace(options?.UserAgent) ? "*" : options.UserAgent;
            // Authority, not Host: robots.txt belongs to scheme + host + port (RFC 9309 section 2.3).
            var origin = uri.GetLeftPart(UriPartial.Authority);
            var key = $"{origin}|{userAgent}";
            var robotsInfo = await _robotsCache.GetOrAdd(
                key,
                _ => GetRobotsTxtAsync(origin, userAgent, cancellationToken));

            return IsPathAllowed(robotsInfo, url, userAgent);
        }
        catch
        {
            return true; // robots.txt 를 읽을 수 없으면 허용 (IsUrlAllowedAsync 와 같은 정책)
        }
    }

    /// <summary>
    /// The result <see cref="CrawlAsync"/> returns for a URL robots.txt disallows: nothing was
    /// fetched, and <see cref="CrawlResult.DisallowedByRobotsTxt"/> says why.
    /// </summary>
    protected static CrawlResult CreateDisallowedByRobotsResult(string url) => new()
    {
        Url = url,
        FinalUrl = url,
        StatusCode = 0,
        IsSuccess = false,
        DisallowedByRobotsTxt = true,
        CrawledAt = DateTimeOffset.UtcNow,
        Depth = 0,
        DiscoveredLinks = Array.Empty<string>(),
        ErrorMessage = "Disallowed by robots.txt"
    };

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
                href.StartsWith('#'))
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
        return Statistics;
    }

    /// <summary>
    /// Sitemap에서 URL 추출
    /// XDocument 기반 파싱으로 namespace, CDATA를 지원합니다.
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
            using var response = await HttpClient.GetAsync(sitemapUrl, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Array.Empty<string>();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseSitemapXml(content);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Sitemap XML을 파싱하여 URL 목록을 추출합니다.
    /// namespace, CDATA, sitemap index를 지원합니다.
    /// </summary>
    /// <param name="xmlContent">Sitemap XML 콘텐츠</param>
    /// <returns>추출된 URL 목록</returns>
    protected virtual IEnumerable<string> ParseSitemapXml(string xmlContent)
    {
        var urls = new List<string>();

        try
        {
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;

            if (root == null)
                return urls;

            // 네임스페이스 처리 (표준 sitemap namespace)
            XNamespace? ns = root.GetDefaultNamespace();

            // sitemap index 파일인 경우 (<sitemapindex>)
            var sitemapElements = ns != null
                ? root.Descendants(ns + "sitemap")
                : root.Descendants("sitemap");

            foreach (var sitemapElement in sitemapElements)
            {
                var locElement = ns != null
                    ? sitemapElement.Element(ns + "loc")
                    : sitemapElement.Element("loc");

                if (locElement != null)
                {
                    var url = locElement.Value.Trim();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            // 일반 URL 항목 (<url>)
            var urlElements = ns != null
                ? root.Descendants(ns + "url")
                : root.Descendants("url");

            foreach (var urlElement in urlElements)
            {
                var locElement = ns != null
                    ? urlElement.Element(ns + "loc")
                    : urlElement.Element("loc");

                if (locElement != null)
                {
                    var url = locElement.Value.Trim();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            return urls;
        }
        catch
        {
            // XML 파싱 실패 시 정규식 폴백
            return ParseSitemapWithRegex(xmlContent);
        }
    }

    /// <summary>
    /// Sitemap XML을 정규식으로 파싱합니다 (폴백용).
    /// </summary>
    /// <param name="content">Sitemap 콘텐츠</param>
    /// <returns>추출된 URL 목록</returns>
    private static List<string> ParseSitemapWithRegex(string content)
    {
        var urlMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"<loc>([^<]+)</loc>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return urlMatches.Cast<System.Text.RegularExpressions.Match>()
                        .Select(m => m.Groups[1].Value.Trim())
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .ToList();
    }

    /// <summary>
    /// robots.txt 파싱
    /// </summary>
    /// <param name="content">robots.txt 내용</param>
    /// <returns>파싱된 robots.txt 정보</returns>
    protected virtual RobotsTxtInfo ParseRobotsTxt(string content)
        => RobotsTxt.Parse(content);

    /// <summary>
    /// 통계 업데이트
    /// </summary>
    /// <param name="result">크롤링 결과</param>
    protected virtual void UpdateStatistics(CrawlResult result)
    {
        var domain = new Uri(result.Url).Host;
        var domainCounts = Statistics.RequestsByDomain.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        domainCounts[domain] = domainCounts.GetValueOrDefault(domain, 0) + 1;

        var statusCounts = Statistics.StatusCodeDistribution.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        statusCounts[result.StatusCode] = statusCounts.GetValueOrDefault(result.StatusCode, 0) + 1;

        var totalRequests = Statistics.TotalRequests + 1;
        var successfulRequests = Statistics.SuccessfulRequests + (result.IsSuccess ? 1 : 0);
        var failedRequests = Statistics.FailedRequests + (result.IsSuccess ? 0 : 1);
        var totalBytes = Statistics.TotalBytesProcessed + (result.ContentLength ?? 0);

        var elapsedTime = DateTimeOffset.UtcNow - StartTime;
        var requestsPerSecond = elapsedTime.TotalSeconds > 0 ? totalRequests / elapsedTime.TotalSeconds : 0;

        Statistics = new CrawlStatistics
        {
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            AverageResponseTimeMs = (Statistics.AverageResponseTimeMs * (Statistics.TotalRequests) + result.ResponseTimeMs) / totalRequests,
            TotalBytesProcessed = totalBytes,
            RequestsPerSecond = requestsPerSecond,
            RequestsByDomain = domainCounts.AsReadOnly(),
            StatusCodeDistribution = statusCounts.AsReadOnly(),
            StartTime = StartTime,
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
    /// <param name="options">크롤링 옵션 (패턴 필터링용)</param>
    /// <returns>크롤링 여부</returns>
    protected virtual bool ShouldCrawlUrl(string url, string? baseUrl = null, CrawlOptions? options = null)
    {
        if (!IsValidUrl(url))
            return false;

        // 호스트 범위. 기본값은 시작 URL 과 같은 호스트만이고, 소비자가 그것을 넓히는 길이 둘 있다:
        // AllowedDomains 에 호스트를 나열하거나, FollowExternalLinks 로 제한을 푸는 것.
        // 둘 다 기본값에서는 아무 효과가 없으므로 이 배선은 기존 동작을 바꾸지 않는다.
        if (!string.IsNullOrEmpty(baseUrl) && !(options?.FollowExternalLinks ?? false))
        {
            var urlDomain = new Uri(url).Host;
            var baseDomain = new Uri(baseUrl).Host;

            if (!urlDomain.Equals(baseDomain, StringComparison.OrdinalIgnoreCase)
                && !IsAllowedDomain(urlDomain, options))
                return false;
        }

        if (options != null)
        {
            // 제외 확장자 확인
            var path = new Uri(url).AbsolutePath;
            var extension = Path.GetExtension(path)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(extension) && options.ExcludedExtensions.Contains(extension))
                return false;

            // 포함 URL 패턴 확인 (설정된 경우, 하나 이상 매치 필요)
            if (options.IncludeUrlPatterns?.Count > 0)
            {
                if (!options.IncludeUrlPatterns.Any(pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)))
                    return false;
            }

            // 제외 URL 패턴 확인 (매치되면 제외)
            if (options.ExcludeUrlPatterns?.Count > 0)
            {
                if (options.ExcludeUrlPatterns.Any(pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// <see cref="CrawlOptions.AllowedDomains"/> 가 <paramref name="host"/> 를 덮는지 본다.
    /// 목록이 비어 있으면(기본값) 아무것도 넓히지 않는다 - 판정은 호출자의 같은-호스트 규칙에 맡긴다.
    /// 항목은 호스트 그 자체이거나 그 하위 도메인을 매치한다(<c>example.com</c> 이 <c>docs.example.com</c> 을 덮는다).
    /// </summary>
    private static bool IsAllowedDomain(string host, CrawlOptions? options)
    {
        if (options?.AllowedDomains is not { Count: > 0 } allowed)
            return false;

        foreach (var domain in allowed)
        {
            if (string.IsNullOrWhiteSpace(domain))
                continue;

            var candidate = domain.Trim().TrimStart('.');

            if (host.Equals(candidate, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
        CancellationTokenSourceInstance?.Cancel();
        CancellationTokenSourceInstance?.Dispose();
        IsRunning = false;
    }
}