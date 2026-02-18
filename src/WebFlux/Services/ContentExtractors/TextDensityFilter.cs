using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// 텍스트 밀도 기반 보일러플레이트 필터
/// Crawl4AI의 PruningContentFilter 개념을 .NET으로 구현
/// DOM 노드별 점수를 계산하여 낮은 점수의 노드를 제거
/// </summary>
public partial class TextDensityFilter
{
    private readonly ILogger<TextDensityFilter>? _logger;

    /// <summary>
    /// 텍스트 밀도 임계값 (이 값 미만인 노드는 제거 후보)
    /// </summary>
    public double TextDensityThreshold { get; set; } = 0.1;

    /// <summary>
    /// 링크 밀도 임계값 (이 값 초과인 노드는 네비게이션으로 간주)
    /// </summary>
    public double LinkDensityThreshold { get; set; } = 0.5;

    /// <summary>
    /// 최소 텍스트 길이 (이 길이 미만인 블록 노드는 제거 후보)
    /// </summary>
    public int MinTextLength { get; set; } = 25;

    /// <summary>
    /// 콘텐츠로 간주하는 태그 가중치
    /// </summary>
    private static readonly Dictionary<string, double> TagWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["article"] = 2.0,
        ["main"] = 2.0,
        ["section"] = 1.5,
        ["div"] = 1.0,
        ["p"] = 1.5,
        ["pre"] = 1.5,
        ["code"] = 1.5,
        ["blockquote"] = 1.3,
        ["table"] = 1.2,
        ["ul"] = 1.0,
        ["ol"] = 1.0,
        ["li"] = 1.0,
        ["h1"] = 1.5,
        ["h2"] = 1.5,
        ["h3"] = 1.3,
        ["h4"] = 1.2,
        ["h5"] = 1.1,
        ["h6"] = 1.1,
        ["figure"] = 1.2,
        ["figcaption"] = 1.2,
        ["details"] = 1.1,
        ["summary"] = 1.1
    };

    /// <summary>
    /// 제거 대상으로 간주되는 태그
    /// </summary>
    private static readonly HashSet<string> NoiseTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "nav", "footer", "header", "aside", "form"
    };

    public TextDensityFilter(ILogger<TextDensityFilter>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// HTML 콘텐츠에서 보일러플레이트를 제거하고 핵심 콘텐츠만 반환
    /// </summary>
    /// <param name="html">정리된 HTML (HtmlContentCleaner 후)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>핵심 콘텐츠 HTML</returns>
    public async Task<string> FilterAsync(string html, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var parser = context.GetService<IHtmlParser>()!;
        var document = await parser.ParseDocumentAsync(html, cancellationToken).ConfigureAwait(false);

        var body = document.Body;
        if (body == null)
            return html;

        // 블록 레벨 요소에 대해 점수 계산 후 낮은 점수 노드 제거
        var candidates = CollectBlockElements(body);
        var removals = new List<IElement>();

        foreach (var element in candidates)
        {
            var score = CalculateScore(element);
            if (score < 0)
            {
                removals.Add(element);
            }
        }

        // 점수가 낮은 노드 제거 (자식->부모 순서로 제거하여 중복 방지)
        foreach (var element in removals.OrderByDescending(e => GetDepth(e)))
        {
            // 이미 제거된 노드의 자식이면 건너뜀
            if (element.Parent == null)
                continue;

            element.Remove();
        }

        var result = body.InnerHtml;

        if (_logger != null) LogDensityFilterCompleted(_logger, removals.Count);

        return result;
    }

    /// <summary>
    /// 블록 레벨 요소 수집
    /// </summary>
    private static List<IElement> CollectBlockElements(IElement root)
    {
        var blockTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "div", "section", "article", "aside", "nav", "header", "footer",
            "main", "form", "table", "ul", "ol", "dl", "figure",
            "details", "fieldset", "address"
        };

        return root.QuerySelectorAll("*")
            .Where(e => blockTags.Contains(e.LocalName))
            .ToList();
    }

    /// <summary>
    /// 요소의 콘텐츠 점수를 계산
    /// 양수: 유용한 콘텐츠, 음수: 보일러플레이트
    /// </summary>
    private double CalculateScore(IElement element)
    {
        var totalTextLength = element.TextContent.Trim().Length;
        var linkTextLength = GetLinkTextLength(element);
        var innerHtmlLength = element.InnerHtml.Length;

        // 텍스트가 없는 노드는 중립 (제거하지 않음 - 이미지 등 포함 가능)
        if (totalTextLength == 0)
            return 0;

        // 이미지가 포함된 노드 보호 (figure, img 자식 요소)
        var hasImages = element.QuerySelectorAll("img, figure").Length > 0;
        if (hasImages && totalTextLength < MinTextLength)
            return 0.5; // 이미지 위주 노드는 보호

        // 텍스트 밀도: 전체 텍스트 / 전체 HTML (태그 마크업 대비 실제 텍스트 비율)
        var textDensity = innerHtmlLength > 0 ? (double)totalTextLength / innerHtmlLength : 0;

        // 링크 밀도: 링크 텍스트 / 전체 텍스트
        var linkDensity = totalTextLength > 0 ? (double)linkTextLength / totalTextLength : 0;

        // 태그 가중치
        var tagWeight = TagWeights.GetValueOrDefault(element.LocalName, 0.8);

        // 노이즈 태그 패널티
        if (NoiseTags.Contains(element.LocalName))
            tagWeight = 0.3;

        // id/class 기반 가중치 조정
        var classIdBonus = CalculateClassIdBonus(element);

        // 최종 점수 계산
        var score = 0.0;

        // 리스트 컨테이너(ul/ol)는 자식 li 텍스트가 짧을 수 있으므로 완화된 임계값 적용
        var effectiveMinTextLength = element.LocalName is "ul" or "ol"
            ? Math.Max(MinTextLength / 3, 8)
            : MinTextLength;

        // 텍스트가 짧고 링크 밀도가 높으면 네비게이션으로 간주
        if (totalTextLength < effectiveMinTextLength && linkDensity > LinkDensityThreshold)
        {
            score = -1.0;
        }
        // 링크 밀도가 매우 높으면 네비게이션
        else if (linkDensity > 0.8)
        {
            score = -0.5;
        }
        // 텍스트가 매우 짧고 텍스트 밀도가 낮으면 보일러플레이트
        else if (textDensity < TextDensityThreshold && totalTextLength < effectiveMinTextLength)
        {
            score = -0.3;
        }
        else
        {
            // 기본 점수 = 텍스트 밀도 * 태그 가중치 + class/id 보너스
            score = textDensity * tagWeight + classIdBonus;
        }

        return score;
    }

    /// <summary>
    /// 직접 텍스트 길이 (자식 요소의 텍스트 제외)
    /// </summary>
    private static int GetDirectTextLength(IElement element)
    {
        var length = 0;
        foreach (var child in element.ChildNodes)
        {
            if (child is IText textNode)
            {
                length += textNode.Data.Trim().Length;
            }
        }
        return length;
    }

    /// <summary>
    /// 링크 텍스트 길이
    /// </summary>
    private static int GetLinkTextLength(IElement element)
    {
        return element.QuerySelectorAll("a")
            .Sum(a => a.TextContent.Trim().Length);
    }

    /// <summary>
    /// class/id 속성 기반 가중치 조정
    /// 단어 경계 매칭으로 false positive 방지 (예: "content-warning" -> "content" 오매칭 방지)
    /// </summary>
    private static double CalculateClassIdBonus(IElement element)
    {
        var bonus = 0.0;
        var classAndId = $"{element.ClassName ?? ""} {element.Id ?? ""}".ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(classAndId))
            return bonus;

        // 콘텐츠 관련 키워드 (단어 경계 매칭)
        string[] positiveKeywords = ["content", "article", "body", "post", "text", "entry", "main", "story"];
        string[] negativeKeywords = ["comment", "sidebar", "widget", "menu", "nav", "footer", "header",
            "ad", "social", "share", "related", "promo", "banner"];

        foreach (var keyword in positiveKeywords)
        {
            if (MatchesWordBoundary(classAndId, keyword))
                bonus += 0.2;
        }

        foreach (var keyword in negativeKeywords)
        {
            if (MatchesWordBoundary(classAndId, keyword))
                bonus -= 0.3;
        }

        return bonus;
    }

    /// <summary>
    /// 단어 경계 매칭: CSS 클래스명에서 하이픈/언더스코어를 구분자로 사용
    /// "article-content" -> "article" 매칭, "ad-hoc" -> "ad" 비매칭
    /// </summary>
    private static bool MatchesWordBoundary(string text, string keyword)
    {
        // CSS 클래스에서의 단어 경계: 시작, 끝, 하이픈, 언더스코어, 공백
        var index = 0;
        while ((index = text.IndexOf(keyword, index, StringComparison.Ordinal)) >= 0)
        {
            var before = index > 0 ? text[index - 1] : ' ';
            var after = index + keyword.Length < text.Length ? text[index + keyword.Length] : ' ';

            var beforeIsBoundary = before == ' ' || before == '-' || before == '_';
            var afterIsBoundary = after == ' ' || after == '-' || after == '_';

            if (beforeIsBoundary && afterIsBoundary)
                return true;

            index += keyword.Length;
        }

        return false;
    }

    /// <summary>
    /// DOM 트리에서의 깊이 계산
    /// </summary>
    private static int GetDepth(IElement element)
    {
        var depth = 0;
        var current = element.ParentElement;
        while (current != null)
        {
            depth++;
            current = current.ParentElement;
        }
        return depth;
    }

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Debug, Message = "Text density filtering completed: {RemovedCount} nodes removed")]
    private static partial void LogDensityFilterCompleted(ILogger logger, int RemovedCount);
}
