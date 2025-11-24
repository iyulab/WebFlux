using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 웹 문서에서 구조화된 메타데이터를 추출하는 서비스
/// SEO, Open Graph, Schema.org, Breadcrumbs, 언어 감지 등 웹 표준 메타데이터 추출
/// </summary>
public partial class WebDocumentMetadataExtractor : IWebDocumentMetadataExtractor
{
    private readonly ILogger<WebDocumentMetadataExtractor> _logger;

    public WebDocumentMetadataExtractor(ILogger<WebDocumentMetadataExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WebDocumentMetadata> ExtractAsync(
        string html,
        string url,
        IReadOnlyDictionary<string, string>? httpHeaders = null,
        CancellationToken cancellationToken = default)
    {
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html), cancellationToken);

        var uri = new Uri(url);

        // 언어 감지
        var (language, detectionMethod) = DetectLanguage(document, httpHeaders, html);

        return new WebDocumentMetadata
        {
            Url = url,
            Title = ExtractTitle(document),
            Description = ExtractMetaContent(document, "description"),
            Keywords = ExtractKeywords(document),
            Author = ExtractMetaContent(document, "author"),
            Robots = ExtractMetaContent(document, "robots"),
            CanonicalUrl = ExtractCanonicalUrl(document),

            // Open Graph
            OgTitle = ExtractMetaProperty(document, "og:title"),
            OgDescription = ExtractMetaProperty(document, "og:description"),
            OgImage = ExtractMetaProperty(document, "og:image"),
            OgType = ExtractMetaProperty(document, "og:type"),
            OgSiteName = ExtractMetaProperty(document, "og:site_name"),
            OgLocale = ExtractMetaProperty(document, "og:locale"),

            // 시간 정보
            PublishedAt = ExtractDateTime(document, "article:published_time"),
            ModifiedAt = ExtractDateTime(document, "article:modified_time"),

            // Schema.org
            SchemaOrgType = ExtractSchemaOrgType(document),
            StructuredData = ExtractStructuredData(document),
            JsonLdData = ExtractJsonLdData(document),

            // 언어
            Language = language,
            LanguageDetectionMethod = detectionMethod,

            // 사이트 컨텍스트
            SiteContext = ExtractSiteContext(document, url),

            // Twitter Card
            TwitterCard = ExtractTwitterCard(document),

            // RSS/Atom 피드
            FeedUrl = ExtractFeedUrl(document),

            // 도메인
            Domain = uri.Host,

            ExtractedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebDocumentMetadata>> ExtractBatchAsync(
        IEnumerable<(string html, string url, IReadOnlyDictionary<string, string>? httpHeaders)> documents,
        CancellationToken cancellationToken = default)
    {
        var tasks = documents.Select(doc =>
            ExtractAsync(doc.html, doc.url, doc.httpHeaders, cancellationToken));

        return await Task.WhenAll(tasks);
    }

    // ===================================================================
    // 기본 메타데이터 추출
    // ===================================================================

    private static string ExtractTitle(IDocument document)
    {
        return document.Title?.Trim() ?? string.Empty;
    }

    private static string? ExtractMetaContent(IDocument document, string name)
    {
        var meta = document.QuerySelector($"meta[name='{name}']") as IHtmlMetaElement;
        return meta?.Content?.Trim();
    }

    private static string? ExtractMetaProperty(IDocument document, string property)
    {
        var meta = document.QuerySelector($"meta[property='{property}']") as IHtmlMetaElement;
        return meta?.Content?.Trim();
    }

    private static IReadOnlyList<string> ExtractKeywords(IDocument document)
    {
        var keywords = ExtractMetaContent(document, "keywords");
        if (string.IsNullOrWhiteSpace(keywords))
            return Array.Empty<string>();

        return keywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string? ExtractCanonicalUrl(IDocument document)
    {
        var link = document.QuerySelector("link[rel='canonical']") as IHtmlLinkElement;
        return link?.Href;
    }

    // ===================================================================
    // 언어 감지 (Issue #4)
    // ===================================================================

    private (string language, LanguageDetectionMethod method) DetectLanguage(
        IDocument document,
        IReadOnlyDictionary<string, string>? httpHeaders,
        string html)
    {
        // 1. HTML lang 속성 우선
        var htmlElement = document.DocumentElement;
        var langAttr = htmlElement?.GetAttribute("lang");
        if (!string.IsNullOrWhiteSpace(langAttr))
        {
            var lang = NormalizeLanguageCode(langAttr);
            return (lang, LanguageDetectionMethod.HtmlLangAttribute);
        }

        // 2. Content-Language HTTP 헤더
        if (httpHeaders != null &&
            httpHeaders.TryGetValue("Content-Language", out var contentLang) &&
            !string.IsNullOrWhiteSpace(contentLang))
        {
            var lang = NormalizeLanguageCode(contentLang);
            return (lang, LanguageDetectionMethod.HttpHeader);
        }

        // 3. 콘텐츠 분석 (간단한 휴리스틱)
        var detectedLang = DetectLanguageFromContent(html);
        if (!string.IsNullOrEmpty(detectedLang))
        {
            return (detectedLang, LanguageDetectionMethod.ContentAnalysis);
        }

        // 4. 기본값
        return ("unknown", LanguageDetectionMethod.Unknown);
    }

    private static string NormalizeLanguageCode(string langCode)
    {
        // "ko-KR" → "ko", "en-US" → "en"
        var normalized = langCode.Trim().Split('-', '_')[0].ToLowerInvariant();
        return normalized.Length == 2 ? normalized : langCode.ToLowerInvariant();
    }

    private string DetectLanguageFromContent(string html)
    {
        // 간단한 문자 기반 휴리스틱
        // 한글 문자 비율 체크
        var koreanCount = KoreanCharRegex().Matches(html).Count;
        var japaneseCount = JapaneseCharRegex().Matches(html).Count;
        var chineseCount = ChineseCharRegex().Matches(html).Count;

        var total = html.Length;
        if (total == 0) return string.Empty;

        var koreanRatio = (double)koreanCount / total;
        var japaneseRatio = (double)japaneseCount / total;
        var chineseRatio = (double)chineseCount / total;

        const double threshold = 0.05;

        if (koreanRatio > threshold && koreanRatio > japaneseRatio && koreanRatio > chineseRatio)
            return "ko";
        if (japaneseRatio > threshold && japaneseRatio > chineseRatio)
            return "ja";
        if (chineseRatio > threshold)
            return "zh";

        // 라틴 문자 기반 언어는 더 정교한 분석 필요 (여기서는 영어 기본값)
        return "en";
    }

    [GeneratedRegex(@"[\uAC00-\uD7AF]")]
    private static partial Regex KoreanCharRegex();

    [GeneratedRegex(@"[\u3040-\u309F\u30A0-\u30FF]")]
    private static partial Regex JapaneseCharRegex();

    [GeneratedRegex(@"[\u4E00-\u9FFF]")]
    private static partial Regex ChineseCharRegex();

    // ===================================================================
    // 시간 정보 추출
    // ===================================================================

    private static DateTime? ExtractDateTime(IDocument document, string property)
    {
        var content = ExtractMetaProperty(document, property);
        if (string.IsNullOrWhiteSpace(content))
            return null;

        if (DateTime.TryParse(content, out var dateTime))
            return dateTime.ToUniversalTime();

        return null;
    }

    // ===================================================================
    // Schema.org 추출
    // ===================================================================

    private static string? ExtractSchemaOrgType(IDocument document)
    {
        // JSON-LD에서 추출
        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");
        foreach (var script in scripts)
        {
            try
            {
                var json = script.TextContent;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("@type", out var typeElement))
                {
                    return typeElement.GetString();
                }
            }
            catch
            {
                // JSON 파싱 실패 시 무시
            }
        }

        // Microdata에서 추출
        var itemScope = document.QuerySelector("[itemtype]");
        if (itemScope != null)
        {
            var itemType = itemScope.GetAttribute("itemtype");
            if (!string.IsNullOrEmpty(itemType))
            {
                // "https://schema.org/Article" → "Article"
                return itemType.Split('/').LastOrDefault();
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> ExtractStructuredData(IDocument document)
    {
        var data = new Dictionary<string, string>();

        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");
        foreach (var script in scripts)
        {
            try
            {
                var json = script.TextContent;
                using var doc = JsonDocument.Parse(json);
                ExtractJsonProperties(doc.RootElement, data, string.Empty);
            }
            catch
            {
                // JSON 파싱 실패 시 무시
            }
        }

        return data;
    }

    private static void ExtractJsonProperties(
        JsonElement element,
        Dictionary<string, string> data,
        string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // @context, @type 등 메타 속성 제외
                    if (property.Name.StartsWith('@') && property.Name != "@type")
                        continue;

                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    if (property.Value.ValueKind == JsonValueKind.String ||
                        property.Value.ValueKind == JsonValueKind.Number ||
                        property.Value.ValueKind == JsonValueKind.True ||
                        property.Value.ValueKind == JsonValueKind.False)
                    {
                        data[key] = property.Value.ToString();
                    }
                    else
                    {
                        ExtractJsonProperties(property.Value, data, key);
                    }
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ExtractJsonProperties(item, data, $"{prefix}[{index}]");
                    index++;
                }
                break;
        }
    }

    private static IReadOnlyList<object> ExtractJsonLdData(IDocument document)
    {
        var result = new List<object>();

        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");
        foreach (var script in scripts)
        {
            try
            {
                var json = script.TextContent;
                using var doc = JsonDocument.Parse(json);
                result.Add(JsonSerializer.Deserialize<object>(json)!);
            }
            catch
            {
                // JSON 파싱 실패 시 무시
            }
        }

        return result;
    }

    // ===================================================================
    // 사이트 컨텍스트 추출 (Issue #3)
    // ===================================================================

    private SiteContext ExtractSiteContext(IDocument document, string currentUrl)
    {
        return new SiteContext
        {
            Breadcrumbs = ExtractBreadcrumbTexts(document),
            BreadcrumbItems = ExtractBreadcrumbItems(document),
            RelatedPages = ExtractRelatedPages(document, currentUrl),
            PreviousPage = ExtractNavLink(document, "prev"),
            NextPage = ExtractNavLink(document, "next")
        };
    }

    private static IReadOnlyList<string> ExtractBreadcrumbTexts(IDocument document)
    {
        // 1. Schema.org BreadcrumbList (JSON-LD)
        var breadcrumbs = ExtractBreadcrumbsFromJsonLd(document);
        if (breadcrumbs.Count > 0)
            return breadcrumbs;

        // 2. aria-label="breadcrumb" 또는 일반적인 breadcrumb 클래스
        var nav = document.QuerySelector("nav[aria-label*='breadcrumb'], .breadcrumb, .breadcrumbs, #breadcrumb");
        if (nav != null)
        {
            var items = nav.QuerySelectorAll("li, a, span")
                .Select(e => e.TextContent.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t) && t != ">" && t != "/")
                .ToList();

            if (items.Count > 0)
                return items;
        }

        // 3. ol/ul 기반 breadcrumb
        var list = document.QuerySelector("ol.breadcrumb, ul.breadcrumb");
        if (list != null)
        {
            return list.QuerySelectorAll("li")
                .Select(li => li.TextContent.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ExtractBreadcrumbsFromJsonLd(IDocument document)
    {
        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");
        foreach (var script in scripts)
        {
            try
            {
                var json = script.TextContent;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("@type", out var typeElement) &&
                    typeElement.GetString() == "BreadcrumbList" &&
                    root.TryGetProperty("itemListElement", out var items))
                {
                    return items.EnumerateArray()
                        .OrderBy(item =>
                            item.TryGetProperty("position", out var pos) ? pos.GetInt32() : 0)
                        .Select(item =>
                            item.TryGetProperty("item", out var itemObj) &&
                            itemObj.TryGetProperty("name", out var name)
                                ? name.GetString() ?? string.Empty
                                : item.TryGetProperty("name", out var nameAlt)
                                    ? nameAlt.GetString() ?? string.Empty
                                    : string.Empty)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToList();
                }
            }
            catch
            {
                // JSON 파싱 실패 시 무시
            }
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<BreadcrumbItem> ExtractBreadcrumbItems(IDocument document)
    {
        var items = new List<BreadcrumbItem>();

        // JSON-LD에서 추출
        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");
        foreach (var script in scripts)
        {
            try
            {
                var json = script.TextContent;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("@type", out var typeElement) &&
                    typeElement.GetString() == "BreadcrumbList" &&
                    root.TryGetProperty("itemListElement", out var listItems))
                {
                    foreach (var item in listItems.EnumerateArray())
                    {
                        var position = item.TryGetProperty("position", out var pos) ? pos.GetInt32() : 0;
                        string? name = null;
                        string? url = null;

                        if (item.TryGetProperty("item", out var itemObj))
                        {
                            name = itemObj.TryGetProperty("name", out var n) ? n.GetString() : null;
                            url = itemObj.TryGetProperty("@id", out var id) ? id.GetString() : null;
                        }
                        else
                        {
                            name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            items.Add(new BreadcrumbItem
                            {
                                Text = name,
                                Url = url ?? string.Empty,
                                Order = position
                            });
                        }
                    }

                    return items.OrderBy(i => i.Order).ToList();
                }
            }
            catch
            {
                // JSON 파싱 실패 시 무시
            }
        }

        // HTML에서 추출
        var nav = document.QuerySelector("nav[aria-label*='breadcrumb'], .breadcrumb, .breadcrumbs");
        if (nav != null)
        {
            var position = 1;
            foreach (var link in nav.QuerySelectorAll("a"))
            {
                var anchor = link as IHtmlAnchorElement;
                items.Add(new BreadcrumbItem
                {
                    Text = link.TextContent.Trim(),
                    Url = anchor?.Href ?? string.Empty,
                    Order = position++
                });
            }
        }

        return items;
    }

    private static IReadOnlyList<string> ExtractRelatedPages(IDocument document, string currentUrl)
    {
        var currentUri = new Uri(currentUrl);
        var relatedPages = new HashSet<string>();

        // 내부 링크만 추출
        var links = document.QuerySelectorAll("a[href]");
        foreach (var link in links.OfType<IHtmlAnchorElement>())
        {
            try
            {
                if (string.IsNullOrEmpty(link.Href))
                    continue;

                var linkUri = new Uri(link.Href, UriKind.RelativeOrAbsolute);
                if (!linkUri.IsAbsoluteUri)
                    linkUri = new Uri(currentUri, linkUri);

                // 같은 도메인의 내부 링크만
                if (linkUri.Host == currentUri.Host &&
                    linkUri.AbsolutePath != currentUri.AbsolutePath)
                {
                    relatedPages.Add(linkUri.GetLeftPart(UriPartial.Path));
                }
            }
            catch
            {
                // 잘못된 URL 무시
            }
        }

        return relatedPages.Take(20).ToList();
    }

    private static string? ExtractNavLink(IDocument document, string rel)
    {
        var link = document.QuerySelector($"link[rel='{rel}'], a[rel='{rel}']");
        return link?.GetAttribute("href");
    }

    // ===================================================================
    // Twitter Card 추출
    // ===================================================================

    private static TwitterCardData? ExtractTwitterCard(IDocument document)
    {
        var card = ExtractMetaContent(document, "twitter:card") ??
                   ExtractMetaProperty(document, "twitter:card");

        if (string.IsNullOrEmpty(card))
            return null;

        return new TwitterCardData
        {
            Card = card,
            Site = ExtractMetaContent(document, "twitter:site") ??
                   ExtractMetaProperty(document, "twitter:site"),
            Creator = ExtractMetaContent(document, "twitter:creator") ??
                      ExtractMetaProperty(document, "twitter:creator"),
            Title = ExtractMetaContent(document, "twitter:title") ??
                    ExtractMetaProperty(document, "twitter:title"),
            Description = ExtractMetaContent(document, "twitter:description") ??
                          ExtractMetaProperty(document, "twitter:description"),
            Image = ExtractMetaContent(document, "twitter:image") ??
                    ExtractMetaProperty(document, "twitter:image")
        };
    }

    // ===================================================================
    // RSS/Atom 피드 추출
    // ===================================================================

    private static string? ExtractFeedUrl(IDocument document)
    {
        var rss = document.QuerySelector("link[type='application/rss+xml']") as IHtmlLinkElement;
        if (!string.IsNullOrEmpty(rss?.Href))
            return rss.Href;

        var atom = document.QuerySelector("link[type='application/atom+xml']") as IHtmlLinkElement;
        return atom?.Href;
    }
}
