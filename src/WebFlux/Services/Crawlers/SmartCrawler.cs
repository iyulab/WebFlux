using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 지능형 크롤러 - 정적/동적 콘텐츠 자동 감지
/// 정적 페이지는 HttpClient (빠름), 동적 페이지는 Playwright (완전한 렌더링)
/// </summary>
public partial class SmartCrawler : BaseCrawler
{
    private readonly ILogger<SmartCrawler> _logger;
    private readonly PlaywrightCrawler _playwrightCrawler;

    public SmartCrawler(
        IHttpClientService httpClientService,
        IEventPublisher eventPublisher,
        ILogger<SmartCrawler> logger,
        PlaywrightCrawler playwrightCrawler)
        : base(httpClientService, eventPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _playwrightCrawler = playwrightCrawler ?? throw new ArgumentNullException(nameof(playwrightCrawler));
    }

    /// <summary>
    /// 단일 URL 크롤링 (자동 전략 선택)
    /// </summary>
    public override async Task<CrawlResult> CrawlAsync(
        string url,
        CrawlOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            LogSmartCrawling(_logger, url);

            // 1단계: HttpClient로 먼저 시도 (빠른 정적 콘텐츠 확인)
            LogTryingHttpClient(_logger);
            var httpResult = await base.CrawlAsync(url, options, cancellationToken);

            if (!httpResult.IsSuccess)
            {
                LogHttpClientFailed(_logger);
                return await _playwrightCrawler.CrawlAsync(url, options, cancellationToken);
            }

            // 2단계: HTML 분석 - JavaScript 필요 여부 감지
            var requiresJavaScript = IsJavaScriptRequired(httpResult.HtmlContent ?? string.Empty);

            if (requiresJavaScript)
            {
                LogDynamicContentDetected(_logger);
                return await _playwrightCrawler.CrawlAsync(url, options, cancellationToken);
            }

            // 3단계: 정적 콘텐츠 - HttpClient 결과 사용
            LogStaticContentDetected(_logger);

            // HtmlContent를 Content로 복사 (PlaywrightCrawler와 호환성)
            return new CrawlResult
            {
                Url = httpResult.Url,
                FinalUrl = httpResult.FinalUrl,
                Content = httpResult.HtmlContent,  // HtmlContent를 Content로 복사
                HtmlContent = httpResult.HtmlContent,
                IsSuccess = httpResult.IsSuccess,
                StatusCode = httpResult.StatusCode,
                ContentType = httpResult.ContentType,
                ContentLength = httpResult.ContentLength,
                ResponseTimeMs = httpResult.ResponseTimeMs,
                CrawledAt = httpResult.CrawledAt,
                DiscoveredLinks = httpResult.DiscoveredLinks,
                ImageUrls = httpResult.ImageUrls,
                Metadata = httpResult.Metadata,
                WebMetadata = httpResult.WebMetadata,
                Headers = httpResult.Headers,
                Encoding = httpResult.Encoding,
                ErrorMessage = httpResult.ErrorMessage,
                Exception = httpResult.Exception
            };
        }
        catch (Exception ex)
        {
            LogSmartCrawlError(_logger, ex, url);

            // 실패 시 Playwright로 대체 시도
            LogFallbackToPlaywright(_logger);
            return await _playwrightCrawler.CrawlAsync(url, options, cancellationToken);
        }
    }

    /// <summary>
    /// JavaScript 렌더링 필요 여부 감지
    /// </summary>
    private bool IsJavaScriptRequired(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return true;

        // 1. SPA 프레임워크 감지
        var spaIndicators = new[]
        {
            "react", "vue", "angular", "svelte",
            "next.js", "nuxt", "gatsby",
            "__NEXT_DATA__", "ng-app", "data-react-root"
        };

        var lowerContent = htmlContent.ToLowerInvariant();
        if (spaIndicators.Any(indicator => lowerContent.Contains(indicator)))
        {
            LogSpaFrameworkDetected(_logger);
            return true;
        }

        // 2. 빈 body 또는 거의 빈 body 감지
        var bodyMatch = Regex.Match(htmlContent, @"<body[^>]*>(.*?)</body>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (bodyMatch.Success)
        {
            var bodyContent = bodyMatch.Groups[1].Value;
            var textContent = Regex.Replace(bodyContent, @"<[^>]+>", "").Trim();

            // Body에 실제 텍스트가 100자 미만이면 동적 렌더링 가능성
            if (textContent.Length < 100)
            {
                LogEmptyBody(_logger);
                return true;
            }
        }

        // 3. 과도한 JavaScript 사용 감지
        var scriptMatches = Regex.Matches(htmlContent, @"<script[^>]*>",
            RegexOptions.IgnoreCase);

        var scriptCount = scriptMatches.Count;
        var htmlLength = htmlContent.Length;
        var scriptRatio = (double)scriptCount / (htmlLength / 1000.0); // script per KB

        if (scriptCount > 10 && scriptRatio > 5)
        {
            LogHeavyJavaScript(_logger, scriptCount);
            return true;
        }

        // 4. 클라이언트 사이드 렌더링 힌트
        var csrIndicators = new[]
        {
            "id=\"root\"", "id=\"app\"", "data-reactroot",
            "data-server-rendered", "__NUXT__"
        };

        if (csrIndicators.Any(indicator => lowerContent.Contains(indicator)))
        {
            LogCsrHintsFound(_logger);
            return true;
        }

        LogStaticContent(_logger);
        return false;
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public override void Dispose()
    {
        _playwrightCrawler?.Dispose();
        base.Dispose();
    }

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Information, Message = "Smart crawling: {Url}")]
    private static partial void LogSmartCrawling(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Trying HttpClient (fast path)...")]
    private static partial void LogTryingHttpClient(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "  HttpClient failed, trying Playwright...")]
    private static partial void LogHttpClientFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Dynamic content detected, using Playwright")]
    private static partial void LogDynamicContentDetected(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "  Static content, using HttpClient result")]
    private static partial void LogStaticContentDetected(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in smart crawling {Url}")]
    private static partial void LogSmartCrawlError(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "  Fallback to Playwright")]
    private static partial void LogFallbackToPlaywright(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  SPA framework detected")]
    private static partial void LogSpaFrameworkDetected(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Empty or minimal body content")]
    private static partial void LogEmptyBody(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Heavy JavaScript usage ({Count} scripts)")]
    private static partial void LogHeavyJavaScript(ILogger logger, int Count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Client-side rendering hints found")]
    private static partial void LogCsrHintsFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "  Static content (no JavaScript required)")]
    private static partial void LogStaticContent(ILogger logger);
}
