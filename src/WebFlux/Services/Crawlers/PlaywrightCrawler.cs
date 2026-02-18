using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// Playwright 기반 동적 렌더링 크롤러
/// JavaScript로 렌더링되는 SPA (React, Vue, Angular) 지원
/// </summary>
public partial class PlaywrightCrawler : BaseCrawler, IAsyncDisposable
{
    private static readonly string[] ChromiumArgs =
    [
        "--disable-blink-features=AutomationControlled", // 자동화 감지 우회
        "--disable-dev-shm-usage",
        "--no-sandbox"
    ];
    private readonly ILogger<PlaywrightCrawler> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private readonly ConcurrentBag<IPage> _pagePool = new();
    private readonly SemaphoreSlim _pageSemaphore = new(5, 5); // 최대 5개 페이지 동시 사용
    private bool _disposed;

    public PlaywrightCrawler(
        IHttpClientService httpClientService,
        IEventPublisher eventPublisher,
        ILogger<PlaywrightCrawler> logger)
        : base(httpClientService, eventPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 단일 URL 크롤링 (동적 렌더링 지원)
    /// </summary>
    public override async Task<CrawlResult> CrawlAsync(
        string url,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            LogCrawlingWithPlaywright(_logger, url);

            LogGettingPageFromPool(_logger);
            var page = await GetPageFromPoolAsync(cancellationToken);

            try
            {
                // User-Agent 설정
                if (!string.IsNullOrEmpty(options?.UserAgent))
                {
                    await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                    {
                        ["User-Agent"] = options.UserAgent
                    });
                }

                // 네비게이션 옵션 - 성능 최적화: DOMContentLoaded 사용
                var timeout = options?.TimeoutMs ?? 15000; // 30초 -> 15초로 단축
                var gotoOptions = new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded, // NetworkIdle -> DOMContentLoaded (2-4초 절감)
                    Timeout = (float)timeout
                };

                // 페이지 로드
                LogLoadingPage(_logger);
                var response = await page.GotoAsync(url, gotoOptions);

                if (response == null)
                {
                    throw new InvalidOperationException($"Failed to load page: {url}");
                }

                LogPageLoaded(_logger, response.Status);

                // 특정 셀렉터 대기 (설정된 경우)
                if (!string.IsNullOrEmpty(options?.WaitForSelector))
                {
                    LogWaitingForSelector(_logger, options.WaitForSelector);
                    await page.WaitForSelectorAsync(options.WaitForSelector, new()
                    {
                        Timeout = (float)timeout
                    });
                }

                // 추가 대기 시간 (JavaScript 실행 완료) - 명시적으로 요청한 경우만
                // 기본 대기 시간 제거로 500ms 절감
                if (options?.DelayMs > 0)
                {
                    LogWaitingForJavaScript(_logger, options.DelayMs);
                    await Task.Delay(options.DelayMs, cancellationToken);
                }
                // 기본 대기 (300ms) - DOM 안정화 및 네비게이션 완료 대기
                else
                {
                    await Task.Delay(300, cancellationToken);
                }

                // 스크롤 처리 (Lazy Loading 콘텐츠) - 명시적으로 활성화된 경우만
                // 기본값을 false로 변경하여 불필요한 스크롤 제거 (최대 5초 절감)
                if (options?.EnableScrolling == true)
                {
                    LogAutoScrolling(_logger);
                    await AutoScrollAsync(page, cancellationToken);
                }

                // 콘텐츠 추출 (재시도 로직 포함)
                LogExtractingContent(_logger);
                string content = string.Empty;
                string title = string.Empty;

                // 네비게이션 완료 대기 (최대 3회 재시도)
                for (int retryCount = 0; retryCount < 3; retryCount++)
                {
                    try
                    {
                        content = await page.ContentAsync();
                        title = await page.TitleAsync();
                        break;
                    }
                    catch (PlaywrightException ex) when (ex.Message.Contains("navigating"))
                    {
                        if (retryCount >= 2) throw;

                        LogPageStillNavigating(_logger, retryCount + 1);
                        await Task.Delay(500, cancellationToken);
                    }
                }

                // 이미지 URL 추출
                var imageUrls = await page.EvaluateAsync<string[]>(
                    @"() => Array.from(document.images).map(img => img.src)"
                );

                // 링크 추출
                var links = await page.EvaluateAsync<string[]>(
                    @"() => Array.from(document.links).map(a => a.href)"
                );

                var responseTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

                var result = new CrawlResult
                {
                    Url = url,
                    FinalUrl = page.Url,
                    Content = content,
                    IsSuccess = true,
                    StatusCode = response.Status,
                    ContentType = "text/html",
                    ContentLength = content.Length,
                    ResponseTimeMs = responseTimeMs,
                    CrawledAt = DateTimeOffset.UtcNow,
                    ImageUrls = imageUrls,
                    DiscoveredLinks = links.Where(link => IsValidUrl(link)).ToList(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["Title"] = title,
                        ["RenderedWith"] = "Playwright",
                        ["Browser"] = "Chromium",
                        ["DynamicRendering"] = true
                    }
                };

                UpdateStatistics(result);

                LogCrawlSuccess(_logger, url, responseTimeMs);

                return result;
            }
            finally
            {
                await ReturnPageToPoolAsync(page);
            }
        }
        catch (PlaywrightException ex)
        {
            LogPlaywrightError(_logger, ex, url);

            var errorResult = new CrawlResult
            {
                Url = url,
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = $"Playwright error: {ex.Message}",
                Exception = ex,
                CrawledAt = DateTimeOffset.UtcNow
            };

            UpdateStatistics(errorResult);
            return errorResult;
        }
        catch (Exception ex)
        {
            LogCrawlError(_logger, ex, url);

            var errorResult = new CrawlResult
            {
                Url = url,
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = ex.Message,
                Exception = ex,
                CrawledAt = DateTimeOffset.UtcNow
            };

            UpdateStatistics(errorResult);
            return errorResult;
        }
    }

    /// <summary>
    /// 자동 스크롤 (Lazy Loading 콘텐츠 로드) - 성능 최적화 버전
    /// </summary>
    private async Task AutoScrollAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            await page.EvaluateAsync(@"
                async () => {
                    await new Promise((resolve) => {
                        let totalHeight = 0;
                        const distance = 200; // 100 -> 200 (스크롤 속도 2배)
                        const maxScrolls = 20; // 50 -> 20 (최대 스크롤 횟수 60% 감소)
                        let scrolls = 0;

                        const timer = setInterval(() => {
                            const scrollHeight = document.body.scrollHeight;
                            window.scrollBy(0, distance);
                            totalHeight += distance;
                            scrolls++;

                            if (totalHeight >= scrollHeight || scrolls >= maxScrolls) {
                                clearInterval(timer);
                                resolve();
                            }
                        }, 50); // 100ms -> 50ms (스크롤 간격 50% 감소)
                    });
                }
            ");

            // 스크롤 후 추가 대기 (500ms -> 200ms)
            await Task.Delay(200, cancellationToken);

            LogAutoScrollCompleted(_logger);
        }
        catch (Exception ex)
        {
            LogAutoScrollFailed(_logger, ex);
        }
    }

    /// <summary>
    /// 브라우저 인스턴스 가져오기 (재사용)
    /// </summary>
    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser != null && _browser.IsConnected)
        {
            return _browser;
        }

        await _browserLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check 패턴
            if (_browser != null && _browser.IsConnected)
            {
                return _browser;
            }

            LogInitializingBrowser(_logger);

            // Playwright 초기화
            _playwright ??= await Playwright.CreateAsync();

            // Chromium 브라우저 시작
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ChromiumArgs
            });

            LogBrowserInitialized(_logger);

            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// 페이지 풀에서 페이지 가져오기 (재사용 최적화)
    /// </summary>
    private async Task<IPage> GetPageFromPoolAsync(CancellationToken cancellationToken)
    {
        await _pageSemaphore.WaitAsync(cancellationToken);

        try
        {
            // 풀에서 재사용 가능한 페이지 가져오기
            if (_pagePool.TryTake(out var page))
            {
                if (!page.IsClosed)
                {
                    LogReusingPage(_logger);
                    return page;
                }
            }

            // 풀에 없으면 새로 생성
            var browser = await GetBrowserAsync(cancellationToken);
            var newPage = await browser.NewPageAsync();
            LogCreatedNewPage(_logger);
            return newPage;
        }
        catch
        {
            _pageSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// 페이지를 풀에 반환 (재사용을 위한 초기화)
    /// </summary>
    private async Task ReturnPageToPoolAsync(IPage page)
    {
        try
        {
            if (!page.IsClosed)
            {
                // 페이지 상태 초기화 (쿠키, 스토리지 등 정리)
                await page.Context.ClearCookiesAsync();

                // 빈 페이지로 이동 (메모리 정리)
                await page.GotoAsync("about:blank", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 5000
                });

                _pagePool.Add(page);
                LogReturnedPage(_logger);
            }
        }
        catch (Exception ex)
        {
            LogReturnPageFailed(_logger, ex);
            try
            {
                await page.CloseAsync();
            }
            catch
            {
                // Ignore close errors
            }
        }
        finally
        {
            _pageSemaphore.Release();
        }
    }

    /// <summary>
    /// URL 유효성 검사
    /// </summary>
    protected override bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// 비동기 리소스 정리 (권장)
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            while (_pagePool.TryTake(out var page))
            {
                try
                {
                    await page.CloseAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Ignore close errors during disposal
                }
            }

            if (_browser is not null)
            {
                await _browser.DisposeAsync().ConfigureAwait(false);
            }

            _playwright?.Dispose();
            _browserLock?.Dispose();
            _pageSemaphore?.Dispose();
        }
        finally
        {
            base.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 동기 리소스 정리 (레거시 호환)
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            while (_pagePool.TryTake(out var page))
            {
                try
                {
                    page.CloseAsync().Wait();
                }
                catch
                {
                    // Ignore close errors during disposal
                }
            }

            _browser?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _playwright?.Dispose();
            _browserLock?.Dispose();
            _pageSemaphore?.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Information, Message = "Crawling URL with Playwright: {Url}")]
    private static partial void LogCrawlingWithPlaywright(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Getting page from pool...")]
    private static partial void LogGettingPageFromPool(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Loading page (DOM ready)...")]
    private static partial void LogLoadingPage(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Page loaded (Status: {StatusCode})")]
    private static partial void LogPageLoaded(ILogger logger, int StatusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Waiting for selector: {Selector}")]
    private static partial void LogWaitingForSelector(ILogger logger, string Selector);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Waiting {DelayMs}ms for JavaScript execution...")]
    private static partial void LogWaitingForJavaScript(ILogger logger, int DelayMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Auto-scrolling page to load lazy content...")]
    private static partial void LogAutoScrolling(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Extracting page content...")]
    private static partial void LogExtractingContent(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Page still navigating, waiting... (retry {RetryCount}/3)")]
    private static partial void LogPageStillNavigating(ILogger logger, int RetryCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully crawled {Url} with Playwright in {ResponseTime}ms")]
    private static partial void LogCrawlSuccess(ILogger logger, string Url, long ResponseTime);

    [LoggerMessage(Level = LogLevel.Error, Message = "Playwright error while crawling {Url}")]
    private static partial void LogPlaywrightError(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error crawling {Url} with Playwright")]
    private static partial void LogCrawlError(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Auto-scroll completed")]
    private static partial void LogAutoScrollCompleted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Auto-scroll failed, continuing anyway")]
    private static partial void LogAutoScrollFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Initializing Playwright browser")]
    private static partial void LogInitializingBrowser(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Playwright browser initialized successfully")]
    private static partial void LogBrowserInitialized(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reusing page from pool")]
    private static partial void LogReusingPage(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created new page for pool")]
    private static partial void LogCreatedNewPage(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returned page to pool")]
    private static partial void LogReturnedPage(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to return page to pool, will be discarded")]
    private static partial void LogReturnPageFailed(ILogger logger, Exception ex);
}
