using System.Collections.Concurrent;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

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

    protected CrawlConfiguration _configuration = new();
    protected CancellationTokenSource? _cancellationTokenSource;
    protected bool _isRunning;
    protected int _processedCount;
    protected int _successCount;
    protected int _errorCount;

    protected BaseCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// 크롤링 시작
    /// </summary>
    /// <param name="startUrls">시작 URL 목록</param>
    /// <param name="configuration">크롤링 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 웹 콘텐츠</returns>
    public virtual async Task<IAsyncEnumerable<WebContent>> CrawlAsync(
        IEnumerable<string> startUrls,
        CrawlConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            throw new InvalidOperationException("Crawler is already running");

        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _isRunning = true;
            Reset();

            // 시작 URL들을 큐에 추가
            foreach (var url in startUrls)
            {
                if (IsValidUrl(url))
                {
                    _urlQueue.Enqueue(url);
                }
            }

            await _eventPublisher.PublishAsync(new CrawlStartedEvent
            {
                StartUrls = startUrls.ToList(),
                Configuration = _configuration,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            return CrawlInternalAsync(_cancellationTokenSource.Token);
        }
        catch
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            throw;
        }
    }

    /// <summary>
    /// 크롤링 중지
    /// </summary>
    public virtual Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 크롤링 상태 조회
    /// </summary>
    /// <returns>크롤링 상태</returns>
    public virtual CrawlStatus GetStatus()
    {
        lock (_lockObject)
        {
            return new CrawlStatus
            {
                IsRunning = _isRunning,
                ProcessedCount = _processedCount,
                SuccessCount = _successCount,
                ErrorCount = _errorCount,
                QueuedCount = _urlQueue.Count,
                VisitedUrlCount = _visitedUrls.Count,
                FailedUrlCount = _failedUrls.Count,
                LastActivity = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// 내부 크롤링 로직 (파생 클래스에서 구현)
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 콘텐츠 스트림</returns>
    protected abstract IAsyncEnumerable<WebContent> CrawlInternalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 단일 URL 처리
    /// </summary>
    /// <param name="url">처리할 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>웹 콘텐츠</returns>
    protected virtual async Task<WebContent?> ProcessUrlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            // 방문 체크 및 기록
            lock (_lockObject)
            {
                if (_visitedUrls.Contains(url))
                    return null;

                _visitedUrls.Add(url);
                _processedCount++;
            }

            await _eventPublisher.PublishAsync(new UrlProcessingStartedEvent
            {
                Url = url,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            // HTTP 요청 수행
            using var response = await _httpClient.GetAsync(url, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleFailedUrl(url, $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/html";

            var webContent = new WebContent
            {
                Url = url,
                Content = content,
                ContentType = contentType,
                Metadata = new WebContentMetadata
                {
                    Title = ExtractTitle(content),
                    CrawledAt = DateTimeOffset.UtcNow,
                    Source = GetType().Name,
                    ResponseHeaders = response.Headers.ToDictionary(
                        h => h.Key,
                        h => string.Join(", ", h.Value)
                    ),
                    StatusCode = (int)response.StatusCode,
                    ContentLength = content.Length
                }
            };

            // 추가 URL 발견 및 큐에 추가
            var discoveredUrls = await DiscoverUrlsAsync(content, url);
            foreach (var discoveredUrl in discoveredUrls)
            {
                if (ShouldCrawlUrl(discoveredUrl))
                {
                    _urlQueue.Enqueue(discoveredUrl);
                }
            }

            lock (_lockObject)
            {
                _successCount++;
            }

            await _eventPublisher.PublishAsync(new UrlProcessedEvent
            {
                Url = url,
                ContentLength = content.Length,
                ContentType = contentType,
                DiscoveredUrlCount = discoveredUrls.Count(),
                ProcessingTimeMs = 0, // 실제 구현에서는 측정 필요
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            return webContent;
        }
        catch (Exception ex)
        {
            await HandleFailedUrl(url, ex.Message);
            return null;
        }
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
    /// <returns>크롤링 여부</returns>
    protected virtual bool ShouldCrawlUrl(string url)
    {
        if (!IsValidUrl(url))
            return false;

        lock (_lockObject)
        {
            if (_visitedUrls.Contains(url) || _failedUrls.Contains(url))
                return false;

            if (_visitedUrls.Count >= _configuration.MaxPages)
                return false;
        }

        // 도메인 필터링
        if (_configuration.AllowedDomains?.Any() == true)
        {
            var uri = new Uri(url);
            if (!_configuration.AllowedDomains.Contains(uri.Host))
                return false;
        }

        // 제외 패턴 확인
        if (_configuration.ExcludePatterns?.Any() == true)
        {
            if (_configuration.ExcludePatterns.Any(pattern => url.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 콘텐츠에서 URL 발견
    /// </summary>
    /// <param name="content">HTML 콘텐츠</param>
    /// <param name="baseUrl">기준 URL</param>
    /// <returns>발견된 URL들</returns>
    protected virtual Task<IEnumerable<string>> DiscoverUrlsAsync(string content, string baseUrl)
    {
        var urls = new List<string>();
        var baseUri = new Uri(baseUrl);

        // 간단한 링크 추출 (실제 구현에서는 HTML 파서 사용 권장)
        var linkMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"href\s*=\s*[""']([^""']*)[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in linkMatches)
        {
            var href = match.Groups[1].Value;

            if (Uri.TryCreate(baseUri, href, out var absoluteUri))
            {
                urls.Add(absoluteUri.ToString());
            }
        }

        return Task.FromResult(urls.AsEnumerable());
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
    /// 실패한 URL 처리
    /// </summary>
    /// <param name="url">실패한 URL</param>
    /// <param name="error">오류 메시지</param>
    protected virtual async Task HandleFailedUrl(string url, string error)
    {
        lock (_lockObject)
        {
            _failedUrls.Add(url);
            _errorCount++;
        }

        await _eventPublisher.PublishAsync(new UrlProcessingFailedEvent
        {
            Url = url,
            Error = error,
            Timestamp = DateTimeOffset.UtcNow
        }, _cancellationTokenSource?.Token ?? default);
    }

    /// <summary>
    /// 상태 초기화
    /// </summary>
    protected virtual void Reset()
    {
        lock (_lockObject)
        {
            _visitedUrls.Clear();
            _failedUrls.Clear();
            _processedCount = 0;
            _successCount = 0;
            _errorCount = 0;
        }
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