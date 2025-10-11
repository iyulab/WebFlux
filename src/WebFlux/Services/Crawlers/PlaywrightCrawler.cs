using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
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

            _logger.LogInformation("  ⏳ Initializing browser...");
            var browser = await GetBrowserAsync(cancellationToken);

            _logger.LogInformation("  ⏳ Opening new page...");
            var page = await browser.NewPageAsync();

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

                // 네비게이션 옵션
                var timeout = options?.TimeoutMs ?? 30000;
                var gotoOptions = new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = (float)timeout
                };

                // 페이지 로드
                _logger.LogInformation("  ⏳ Loading page (waiting for network idle)...");
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

                // 추가 대기 시간 (JavaScript 실행 완료)
                if (options?.DelayMs > 0)
                {
                    _logger.LogInformation("  ⏳ Waiting {DelayMs}ms for JavaScript execution...", options.DelayMs);
                    await Task.Delay(options.DelayMs, cancellationToken);
                }

                // 스크롤 처리 (Lazy Loading 콘텐츠)
                if (options?.EnableScrolling ?? true)
                {
                    _logger.LogInformation("  ⏳ Auto-scrolling page to load lazy content...");
                    await AutoScrollAsync(page, cancellationToken);
                }

                // 콘텐츠 추출
                _logger.LogInformation("  ⏳ Extracting page content...");
                var content = await page.ContentAsync();
                var title = await page.TitleAsync();

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
                await page.CloseAsync();
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
    /// 자동 스크롤 (Lazy Loading 콘텐츠 로드)
    /// </summary>
    private async Task AutoScrollAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            await page.EvaluateAsync(@"
                async () => {
                    await new Promise((resolve) => {
                        let totalHeight = 0;
                        const distance = 100;
                        const maxScrolls = 50; // 최대 스크롤 횟수
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
                        }, 100);
                    });
                }
            ");

            // 스크롤 후 추가 대기
            await Task.Delay(500, cancellationToken);

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
            _browser?.DisposeAsync().AsTask().Wait();
            _playwright?.Dispose();
            _browserLock?.Dispose();
        }
        finally
        {
            base.Dispose();
        }
    }
}
