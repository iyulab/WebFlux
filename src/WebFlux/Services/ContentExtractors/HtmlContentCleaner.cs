using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Options;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// HTML 콘텐츠 정리기
/// nav, header, footer, 광고 등 노이즈 요소를 제거하고 메인 콘텐츠를 추출
/// Firecrawl의 CSS 셀렉터 패턴 참고
/// </summary>
public partial class HtmlContentCleaner
{
    private readonly ILogger<HtmlContentCleaner>? _logger;

    /// <summary>
    /// 무조건 제거할 셀렉터 (script, style 등)
    /// </summary>
    private static readonly string[] AlwaysRemoveSelectors =
    [
        "script", "style", "noscript", "iframe", "svg", "template",
        "link[rel='stylesheet']", "meta", "comment"
    ];

    /// <summary>
    /// OnlyMainContent 활성화 시 제거할 레이아웃 셀렉터
    /// </summary>
    private static readonly string[] LayoutRemoveSelectors =
    [
        "nav", "[role='navigation']", "[role='search']",
        // 한국어 사이트 공통 패턴
        ".gnb", ".lnb", ".snb",
        "#header_area", "#footer_area"
    ];

    /// <summary>
    /// 페이지 수준 랜드마크. <c>article</c>/<c>main</c> 안의 header/footer 는 그 글의 제목 블록·출처이므로 본문이다.
    /// </summary>
    private static readonly string[] PageLandmarkSelectors =
    [
        "header", "footer", "[role='banner']", "[role='contentinfo']"
    ];

    /// <summary>
    /// <c>article</c> 옆의 aside 는 사이드바지만 <c>article</c> 안의 aside 는 주의·참고 같은 본문 콜아웃이다.
    /// </summary>
    private static readonly string[] AsideSelectors =
    [
        "aside", "[role='complementary']"
    ];

    /// <summary>
    /// 광고 및 비콘텐츠 셀렉터 (class/id 가 곧 뜻인 이름 — 어디에 있든 제거)
    /// </summary>
    private static readonly string[] BoilerplateSelectors =
    [
        // 광고
        ".advertisement", ".ads", ".ad-container", ".ad-wrapper",
        ".adsbygoogle",
        ".sponsored", ".promotion",
        // 쿠키 배너
        ".cookie-banner", ".cookie-consent", ".cookie-notice",
        ".gdpr-banner", ".consent-banner",
        // 소셜 공유
        ".social-share", ".share-buttons", ".social-links",
        ".share-bar", ".sharing-buttons",
        // 관련 기사
        ".related-posts", ".related-articles", ".recommended",
        ".you-might-like", ".more-stories",
        // 댓글
        ".comments", ".comment-section", "#comments", "#disqus_thread",
        // 뉴스레터/구독
        ".newsletter", ".subscribe", ".signup-form",
        ".email-signup", ".mailing-list",
        // 기타 비콘텐츠
        ".breadcrumb", ".breadcrumbs", ".pagination",
        ".sidebar",
        ".search-form", ".site-search",
        ".print-only", ".no-print",
        // data-ad 속성 기반 광고
        "[data-ad-slot]", "[data-ad-client]"
    ];

    /// <summary>
    /// 광고 id/class 의 둘째 이상 단어 (<c>ad-slot</c>, <c>ad-banner</c> …). <c>ad-hoc</c> 은 광고가 아니다.
    /// </summary>
    private static readonly HashSet<string> AdQualifiers = new(StringComparer.Ordinal)
    {
        "slot", "container", "wrapper", "banner", "unit", "box", "placeholder", "label", "frame",
        "space", "zone", "leaderboard", "top", "bottom", "sidebar", "inline", "block", "gpt"
    };

    public HtmlContentCleaner(ILogger<HtmlContentCleaner>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// HTML 콘텐츠를 정리하여 메인 콘텐츠만 추출
    /// </summary>
    /// <param name="html">원본 HTML</param>
    /// <param name="sourceUrl">원본 URL (상대 경로 변환용)</param>
    /// <param name="options">정리 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>정리된 HTML</returns>
    public async Task<string> CleanAsync(
        string html,
        string? sourceUrl = null,
        HtmlCleaningOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        options ??= HtmlCleaningOptions.Default;

        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var parser = context.GetService<IHtmlParser>()!;
        var document = await parser.ParseDocumentAsync(html, cancellationToken).ConfigureAwait(false);

        // 1. 무조건 제거 (script, style 등)
        RemoveElements(document, AlwaysRemoveSelectors);

        // HTML 주석 제거
        RemoveComments(document);

        // 2. OnlyMainContent 시 레이아웃 요소 제거
        if (options.OnlyMainContent)
        {
            RemoveElements(document, LayoutRemoveSelectors, options.KeepSelectors);
            RemoveScopedLandmarks(document, options.KeepSelectors);
        }

        // 3. 보일러플레이트 제거
        RemoveElements(document, BoilerplateSelectors, options.KeepSelectors);
        RemoveAmbiguousBoilerplate(document, options.KeepSelectors);

        // 4. 추가 사용자 정의 셀렉터 제거 (호출자가 직접 지목한 것이므로 보호하지 않는다)
        if (options.AdditionalRemoveSelectors.Count > 0)
        {
            RemoveElements(document, options.AdditionalRemoveSelectors.ToArray(), protectContent: false);
        }

        // 5. URL 절대 경로 변환
        if (options.ConvertRelativeUrls && !string.IsNullOrEmpty(sourceUrl))
        {
            ConvertRelativeUrls(document, sourceUrl);
        }

        // 6. srcset 최적화
        if (options.OptimizeSrcset)
        {
            OptimizeImageSrcset(document);
        }

        // body 콘텐츠만 반환 (body가 없으면 전체 HTML)
        var body = document.Body;
        var result = body?.InnerHtml ?? document.DocumentElement?.OuterHtml ?? html;

        if (_logger != null) LogHtmlCleaningCompleted(_logger, html.Length, result.Length);

        return result;
    }

    /// <summary>
    /// CSS 셀렉터 목록에 해당하는 요소를 제거
    /// </summary>
    private static void RemoveElements(IDocument document, string[] selectors, IEnumerable<string>? keepSelectors = null, bool protectContent = true)
    {
        var keepSet = keepSelectors != null ? new HashSet<string>(keepSelectors) : null;

        foreach (var selector in selectors)
        {
            try
            {
                var elements = document.QuerySelectorAll(selector).ToList();
                foreach (var element in elements)
                {
                    // keepSelectors에 해당하면 건너뜀
                    if (keepSet != null && ShouldKeep(element, keepSet))
                        continue;

                    // 페이지 자신(body/html)이나 main 을 품은 컨테이너는 어떤 셀렉터도 지울 수 없다
                    if (protectContent && IsPageOrMainContainer(element))
                        continue;

                    element.Remove();
                }
            }
            catch
            {
                // 잘못된 셀렉터는 무시
            }
        }
    }

    /// <summary>
    /// header/footer/aside 중 article·main 안에 있는 것은 본문이므로 남기고, 페이지 수준의 것만 제거한다.
    /// </summary>
    private static void RemoveScopedLandmarks(IDocument document, IEnumerable<string>? keepSelectors)
    {
        var keepSet = keepSelectors != null ? new HashSet<string>(keepSelectors) : null;

        RemoveWhere(document, PageLandmarkSelectors, keepSet, e => IsInside(e, "article, main"));
        RemoveWhere(document, AsideSelectors, keepSet, e => IsInside(e, "article"));
    }

    /// <summary>
    /// 이름이 모호한 class/id (cookie · modal · popup · overlay · ad-… · menu · widget). 본문을 담은 컨테이너의 class 에
    /// 우연히 들어 있을 수 있으므로 article/main 밖에서, 페이지 본문의 대부분이 아닌 요소에만 적용한다.
    /// </summary>
    private static void RemoveAmbiguousBoilerplate(IDocument document, IEnumerable<string>? keepSelectors)
    {
        var keepSet = keepSelectors != null ? new HashSet<string>(keepSelectors) : null;
        var bodyTextLength = document.Body?.TextContent.Trim().Length ?? 0;

        var candidates = document.QuerySelectorAll("[class],[id]").Where(LooksLikeAmbiguousBoilerplate).ToList();
        foreach (var element in candidates)
        {
            if (element.Parent == null)
                continue;
            if (keepSet != null && ShouldKeep(element, keepSet))
                continue;
            if (IsPageOrMainContainer(element) || IsInside(element, "article, main") || element.QuerySelector("article") != null)
                continue;

            // 본문 텍스트의 절반 이상을 담은 요소는 배너가 아니라 페이지 자신이다
            var length = element.TextContent.Trim().Length;
            if (length >= MajorityMinTextLength && length * 2 >= bodyTextLength)
                continue;

            element.Remove();
        }
    }

    private static void RemoveWhere(IDocument document, string[] selectors, HashSet<string>? keepSet, Func<IElement, bool> isContent)
    {
        foreach (var selector in selectors)
        {
            try
            {
                foreach (var element in document.QuerySelectorAll(selector).ToList())
                {
                    if (keepSet != null && ShouldKeep(element, keepSet))
                        continue;
                    if (isContent(element) || IsPageOrMainContainer(element))
                        continue;

                    element.Remove();
                }
            }
            catch
            {
                // 잘못된 셀렉터는 무시
            }
        }
    }

    /// <summary>한 요소가 페이지 본문의 절반 이상이면서 이 길이 이상이면 보일러플레이트로 보지 않는다.</summary>
    private const int MajorityMinTextLength = 200;

    private static bool IsInside(IElement element, string selector)
        => element.ParentElement?.Closest(selector) != null;

    private static bool IsPageOrMainContainer(IElement element)
        => element.LocalName is "body" or "html" or "main"
           || element.QuerySelector("main") != null;

    /// <summary>
    /// 모호한 이름을 단어 단위로 읽는다 — <c>modal-open</c> 은 modal, <c>modality-table</c> 은 아니다.
    /// </summary>
    private static bool LooksLikeAmbiguousBoilerplate(IElement element)
    {
        var classTokens = element.ClassList.ToArray();
        var id = element.Id;

        foreach (var token in classTokens)
        {
            var words = Words(token);
            if (words.Contains("modal") || words.Contains("popup") || words.Contains("overlay"))
                return true;
            if (words.Contains("cookie") && words.Contains("policy"))
                return true;
            if (IsAdName(words))
                return true;
            // 한 단어 class 가 곧 뜻인 경우
            if (words.Length == 1 && words[0] is "menu" or "widget")
                return true;
        }

        if (!string.IsNullOrEmpty(id))
        {
            var words = Words(id);
            if (words.Contains("cookie") || words.Contains("cookies"))
                return true;
            if (IsAdName(words))
                return true;
        }

        return false;
    }

    private static string[] Words(string token)
        => token.ToLowerInvariant().Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);

    private static bool IsAdName(string[] words)
        => words.Length >= 2 && words[0] == "ad"
           && words.Skip(1).Any(w => AdQualifiers.Contains(w) || w.All(char.IsAsciiDigit));

    /// <summary>
    /// 요소가 keepSelectors 중 하나에 매칭되는지 확인
    /// </summary>
    private static bool ShouldKeep(IElement element, HashSet<string> keepSelectors)
    {
        foreach (var selector in keepSelectors)
        {
            try
            {
                if (element.Matches(selector))
                    return true;
            }
            catch
            {
                // 잘못된 셀렉터는 무시
            }
        }
        return false;
    }

    /// <summary>
    /// HTML 주석 노드 제거
    /// </summary>
    private static void RemoveComments(IDocument document)
    {
        var comments = document.Descendants()
            .OfType<IComment>()
            .ToList();

        foreach (var comment in comments)
        {
            comment.Remove();
        }
    }

    /// <summary>
    /// 상대 URL을 절대 URL로 변환
    /// </summary>
    private static void ConvertRelativeUrls(IDocument document, string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var baseUri))
            return;

        // <a href> 변환
        foreach (var element in document.QuerySelectorAll("a[href]"))
        {
            var href = element.GetAttribute("href");
            if (!string.IsNullOrEmpty(href) && Uri.TryCreate(baseUri, href, out var absoluteUri))
            {
                element.SetAttribute("href", absoluteUri.ToString());
            }
        }

        // <img src> 변환
        foreach (var element in document.QuerySelectorAll("img[src]"))
        {
            var src = element.GetAttribute("src");
            if (!string.IsNullOrEmpty(src) && Uri.TryCreate(baseUri, src, out var absoluteUri))
            {
                element.SetAttribute("src", absoluteUri.ToString());
            }
        }

        // <source srcset> 변환
        foreach (var element in document.QuerySelectorAll("[srcset]"))
        {
            var srcset = element.GetAttribute("srcset");
            if (!string.IsNullOrEmpty(srcset))
            {
                var converted = ConvertSrcsetUrls(srcset, baseUri);
                element.SetAttribute("srcset", converted);
            }
        }
    }

    /// <summary>
    /// srcset 속성의 URL을 절대 경로로 변환
    /// </summary>
    private static string ConvertSrcsetUrls(string srcset, Uri baseUri)
    {
        var parts = srcset.Split(',');
        var converted = new List<string>();

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var spaceIndex = trimmed.IndexOf(' ');
            if (spaceIndex > 0)
            {
                var url = trimmed[..spaceIndex];
                var descriptor = trimmed[spaceIndex..];
                if (Uri.TryCreate(baseUri, url, out var absoluteUri))
                {
                    converted.Add(absoluteUri.ToString() + descriptor);
                }
                else
                {
                    converted.Add(trimmed);
                }
            }
            else if (Uri.TryCreate(baseUri, trimmed, out var absoluteUri))
            {
                converted.Add(absoluteUri.ToString());
            }
            else
            {
                converted.Add(trimmed);
            }
        }

        return string.Join(", ", converted);
    }

    /// <summary>
    /// img 요소의 srcset에서 최적 이미지 URL을 선택하여 src에 설정
    /// </summary>
    private static void OptimizeImageSrcset(IDocument document)
    {
        foreach (var img in document.QuerySelectorAll("img[srcset]"))
        {
            var srcset = img.GetAttribute("srcset");
            if (string.IsNullOrEmpty(srcset))
                continue;

            var bestUrl = SelectBestImageFromSrcset(srcset);
            if (!string.IsNullOrEmpty(bestUrl))
            {
                img.SetAttribute("src", bestUrl);
                img.RemoveAttribute("srcset");
            }
        }
    }

    /// <summary>
    /// srcset에서 최적 이미지 URL 선택 (가장 큰 해상도 선택)
    /// </summary>
    private static string? SelectBestImageFromSrcset(string srcset)
    {
        var candidates = srcset.Split(',')
            .Select(part =>
            {
                var trimmed = part.Trim();
                var spaceIndex = trimmed.IndexOf(' ');
                if (spaceIndex <= 0)
                    return (Url: trimmed, Width: 0);

                var url = trimmed[..spaceIndex];
                var descriptor = trimmed[(spaceIndex + 1)..].Trim();

                // "800w" 또는 "2x" 형태 파싱
                if (descriptor.EndsWith('w') && int.TryParse(descriptor[..^1], out var width))
                    return (Url: url, Width: width);
                if (descriptor.EndsWith('x') && double.TryParse(descriptor[..^1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var multiplier))
                    return (Url: url, Width: (int)(multiplier * 1000));

                return (Url: url, Width: 0);
            })
            .Where(c => !string.IsNullOrEmpty(c.Url))
            .OrderByDescending(c => c.Width)
            .ToList();

        return candidates.FirstOrDefault().Url;
    }

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Debug, Message = "HTML cleaning completed: original {OriginalLength} chars -> cleaned {CleanedLength} chars")]
    private static partial void LogHtmlCleaningCompleted(ILogger logger, int OriginalLength, int CleanedLength);
}
