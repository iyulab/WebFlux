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
public class PlaywrightCrawler : BaseCrawler
{
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
            _logger.LogInformation("🌐 Crawling URL with Playwright: {Url}", url);

            _logger.LogInformation("  ⏳ Getting page from pool...");
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
                var timeout = options?.TimeoutMs ?? 15000; // 30초 → 15초로 단축
                var gotoOptions = new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded, // NetworkIdle → DOMContentLoaded (2-4초 절감)
                    Timeout = (float)timeout
                };

                // 페이지 로드
                _logger.LogInformation("  ⏳ Loading page (DOM ready)...");
                var response = await page.GotoAsync(url, gotoOptions);

                if (response == null)
                {
                    throw new InvalidOperationException($"Failed to load page: {url}");
                }

                _logger.LogInformation("  ✅ Page loaded (Status: {StatusCode})", response.Status);

                // 특정 셀렉터 대기 (설정된 경우)
                if (!string.IsNullOrEmpty(options?.WaitForSelector))
                {
                    _logger.LogInformation("  ⏳ Waiting for selector: {Selector}", options.WaitForSelector);
                    await page.WaitForSelectorAsync(options.WaitForSelector, new()
                    {
                        Timeout = (float)timeout
                    });
                }

                // 추가 대기 시간 (JavaScript 실행 완료) - 명시적으로 요청한 경우만
                // 기본 대기 시간 제거로 500ms 절감
                if (options?.DelayMs > 0)
                {
                    _logger.LogInformation("  ⏳ Waiting {DelayMs}ms for JavaScript execution...", options.DelayMs);
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
                    _logger.LogInformation("  ⏳ Auto-scrolling page to load lazy content...");
                    await AutoScrollAsync(page, cancellationToken);
                }

                // 콘텐츠 추출 (재시도 로직 포함)
                _logger.LogInformation("  ⏳ Extracting page content...");
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

                        _logger.LogDebug("  ⏳ Page still navigating, waiting... (retry {RetryCount}/3)", retryCount + 1);
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

                _logger.LogInformation(
                    "Successfully crawled {Url} with Playwright in {ResponseTime}ms",
                    url, responseTimeMs);

                return result;
            }
            finally
            {
                await ReturnPageToPoolAsync(page);
            }
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright error while crawling {Url}", url);

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
            _logger.LogError(ex, "Error crawling {Url} with Playwright", url);

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
                        const distance = 200; // 100 → 200 (스크롤 속도 2배)
                        const maxScrolls = 20; // 50 → 20 (최대 스크롤 횟수 60% 감소)
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
                        }, 50); // 100ms → 50ms (스크롤 간격 50% 감소)
                    });
                }
            ");

            // 스크롤 후 추가 대기 (500ms → 200ms)
            await Task.Delay(200, cancellationToken);

            _logger.LogDebug("Auto-scroll completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-scroll failed, continuing anyway");
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

            _logger.LogInformation("Initializing Playwright browser");

            // Playwright 초기화
            _playwright ??= await Playwright.CreateAsync();

            // Chromium 브라우저 시작
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled", // 자동화 감지 우회
                    "--disable-dev-shm-usage",
                    "--no-sandbox"
                }
            });

            _logger.LogInformation("Playwright browser initialized successfully");

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
                    _logger.LogDebug("Reusing page from pool");
                    return page;
                }
            }

            // 풀에 없으면 새로 생성
            var browser = await GetBrowserAsync(cancellationToken);
            var newPage = await browser.NewPageAsync();
            _logger.LogDebug("Created new page for pool");
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
                _logger.LogDebug("Returned page to pool");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to return page to pool, will be discarded");
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
    /// 리소스 정리
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // 페이지 풀의 모든 페이지 닫기
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

            _browser?.DisposeAsync().GetAwaiter().GetResult();
            _playwright?.Dispose();
            _browserLock?.Dispose();
            _pageSemaphore?.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }
}
