using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 콘텐츠 품질 평가 구현
/// MetadataExtractor의 기존 로직 재사용, 페이월/광고 휴리스틱 추가
/// </summary>
public partial class ContentQualityEvaluator : IContentQualityEvaluator
{
    private readonly ILogger<ContentQualityEvaluator> _logger;

    // 페이월 감지 키워드
    private static readonly string[] PaywallKeywords =
    {
        "paywall", "subscribe", "subscription", "premium", "members only",
        "sign up to read", "login to continue", "paid content", "exclusive content",
        "premium-content", "subscriber-only", "paid-article", "paywall-content",
        "구독", "유료", "프리미엄", "회원전용"
    };

    // 광고 관련 CSS 클래스/ID
    private static readonly string[] AdIndicators =
    {
        "ad-container", "ad-wrapper", "advertisement", "sponsored",
        "google-ad", "adsense", "ad-unit", "ad-slot", "banner-ad",
        "promoted-content", "sponsored-content", "native-ad"
    };

    // 로그인 필요 감지 키워드
    private static readonly string[] LoginKeywords =
    {
        "login required", "sign in to", "log in to", "create an account",
        "register to", "members only", "로그인", "회원가입"
    };

    // 콘텐츠 타입 감지 패턴
    private static readonly Dictionary<string, string[]> ContentTypePatterns = new()
    {
        ["article"] = new[] { "article", "news", "story", "post" },
        ["blog"] = new[] { "blog", "author", "posted by" },
        ["documentation"] = new[] { "docs", "documentation", "api", "reference", "guide" },
        ["product"] = new[] { "price", "buy", "cart", "add to cart", "product" },
        ["forum"] = new[] { "forum", "thread", "reply", "comment" }
    };

    public ContentQualityEvaluator(ILogger<ContentQualityEvaluator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<ContentQualityInfo> EvaluateAsync(
        ExtractedContent content,
        string? originalHtml = null,
        CancellationToken cancellationToken = default)
    {
        var html = originalHtml ?? content.OriginalHtml ?? string.Empty;
        var text = content.MainContent ?? content.Text ?? string.Empty;

        var hasPaywall = DetectPaywall(html, text);
        var requiresLogin = DetectLoginRequired(html, text);
        var contentType = ClassifyContentType(content);
        var language = DetectLanguage(text);
        var adDensity = CalculateAdDensity(html);
        var contentRatio = CalculateContentRatio(html, text);
        var wordCount = CountWords(text);
        var estimatedTokens = EstimateTokenCount(text);

        // 읽기 시간 계산 (평균 200 단어/분)
        var readingTimeMinutes = Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));

        // 전체 품질 점수 계산
        var overallScore = CalculateOverallScore(
            hasPaywall, requiresLogin, adDensity, contentRatio,
            wordCount, content.Headings?.Count ?? 0,
            content.Metadata != null);

        // LLM 적합성 점수
        var llmScore = CalculateLlmSuitabilityScore(
            contentRatio, adDensity, wordCount, estimatedTokens);

        var quality = new ContentQualityInfo
        {
            OverallScore = overallScore,
            ContentType = contentType,
            Language = language,
            EstimatedReadingTimeMinutes = readingTimeMinutes,
            WordCount = wordCount,
            HasPaywall = hasPaywall,
            RequiresLogin = requiresLogin,
            HasAgeRestriction = DetectAgeRestriction(html),
            ContentRatio = contentRatio,
            AdDensity = adDensity,
            HasMainContent = !string.IsNullOrWhiteSpace(text) && wordCount > 50,
            HasStructuredData = DetectStructuredData(html),
            HasAuthor = HasAuthorInfo(content),
            HasPublishDate = content.Metadata?.PublishedDate != null,
            PublishDate = content.Metadata?.PublishedDate,
            HasCitations = DetectCitations(text),
            IsSecure = content.Url?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ?? false,
            LlmSuitabilityScore = llmScore,
            EstimatedTokenCount = estimatedTokens,
            NoiseRatio = Math.Max(0, 1 - contentRatio)
        };

        _logger.LogDebug(
            "Evaluated quality for {Url}: Score={Score:P0}, Paywall={Paywall}, AdDensity={AdDensity:P0}",
            content.Url, quality.OverallScore, quality.HasPaywall, quality.AdDensity);

        return Task.FromResult(quality);
    }

    /// <inheritdoc />
    public Task<ContentQualityInfo> EvaluateHtmlAsync(
        string html,
        string url,
        CancellationToken cancellationToken = default)
    {
        // 간단한 텍스트 추출
        var text = ExtractTextFromHtml(html);

        var content = new ExtractedContent
        {
            Url = url,
            Text = text,
            MainContent = text,
            OriginalHtml = html
        };

        return EvaluateAsync(content, html, cancellationToken);
    }

    /// <inheritdoc />
    public bool DetectPaywall(string html, string? text = null)
    {
        var combinedContent = (html + " " + (text ?? string.Empty)).ToLowerInvariant();

        foreach (var keyword in PaywallKeywords)
        {
            if (combinedContent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 추가 휴리스틱: 짧은 콘텐츠 + 구독 버튼
        if (text != null && text.Length < 500 && combinedContent.Contains("subscribe"))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public string ClassifyContentType(ExtractedContent content)
    {
        var combinedContent = (
            (content.Title ?? string.Empty) + " " +
            (content.MainContent ?? content.Text ?? string.Empty) + " " +
            (content.Url ?? string.Empty)
        ).ToLowerInvariant();

        foreach (var (type, patterns) in ContentTypePatterns)
        {
            foreach (var pattern in patterns)
            {
                if (combinedContent.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return type;
                }
            }
        }

        return "general";
    }

    /// <inheritdoc />
    public string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "en";
        }

        // 간단한 휴리스틱 기반 언어 감지
        var koreanCount = KoreanCharRegex().Matches(text).Count;
        var chineseCount = ChineseCharRegex().Matches(text).Count;
        var japaneseCount = JapaneseCharRegex().Matches(text).Count;
        var totalChars = text.Length;

        if (totalChars == 0) return "en";

        var koreanRatio = (double)koreanCount / totalChars;
        var chineseRatio = (double)chineseCount / totalChars;
        var japaneseRatio = (double)japaneseCount / totalChars;

        if (koreanRatio > 0.1) return "ko";
        if (japaneseRatio > 0.1) return "ja";
        if (chineseRatio > 0.1) return "zh";

        return "en";
    }

    /// <inheritdoc />
    public double CalculateAdDensity(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return 0;
        }

        var htmlLower = html.ToLowerInvariant();
        var adCount = 0;

        foreach (var indicator in AdIndicators)
        {
            adCount += CountOccurrences(htmlLower, indicator);
        }

        // 광고 태그 카운트
        adCount += AdTagRegex().Matches(html).Count;

        // 정규화 (최대 20개 광고 기준)
        return Math.Min(1.0, adCount / 20.0);
    }

    /// <inheritdoc />
    public double CalculateContentRatio(string html, string text)
    {
        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var htmlLength = html.Length;
        var textLength = text.Length;

        // 텍스트 / HTML 비율 (최대 1.0)
        return Math.Min(1.0, (double)textLength / htmlLength * 3);
    }

    /// <inheritdoc />
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // GPT-4 기준 대략적인 토큰 추정
        // 영어: ~4자 = 1토큰
        // 한국어/중국어/일본어: ~1.5자 = 1토큰
        var koreanCount = KoreanCharRegex().Matches(text).Count;
        var cjkCount = CjkCharRegex().Matches(text).Count;

        var englishChars = text.Length - cjkCount;
        var englishTokens = englishChars / 4.0;
        var cjkTokens = cjkCount / 1.5;

        return (int)Math.Ceiling(englishTokens + cjkTokens);
    }

    private bool DetectLoginRequired(string html, string text)
    {
        var combinedContent = (html + " " + text).ToLowerInvariant();

        foreach (var keyword in LoginKeywords)
        {
            if (combinedContent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DetectAgeRestriction(string html)
    {
        var htmlLower = html.ToLowerInvariant();
        return htmlLower.Contains("age-gate") ||
               htmlLower.Contains("age-verification") ||
               htmlLower.Contains("adult content") ||
               htmlLower.Contains("18+") ||
               htmlLower.Contains("성인인증");
    }

    private static bool DetectStructuredData(string html)
    {
        return html.Contains("application/ld+json", StringComparison.OrdinalIgnoreCase) ||
               html.Contains("itemtype=\"http://schema.org", StringComparison.OrdinalIgnoreCase) ||
               html.Contains("vocab=\"http://schema.org", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAuthorInfo(ExtractedContent content)
    {
        return !string.IsNullOrWhiteSpace(content.Metadata?.Author);
    }

    private static bool DetectCitations(string text)
    {
        // 인용/참조 패턴 감지
        return text.Contains("[1]") ||
               text.Contains("참고문헌") ||
               text.Contains("References") ||
               text.Contains("출처:");
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static double CalculateOverallScore(
        bool hasPaywall,
        bool requiresLogin,
        double adDensity,
        double contentRatio,
        int wordCount,
        int headingCount,
        bool hasMetadata)
    {
        var score = 0.5; // 기본 점수

        // 페이월/로그인 감점
        if (hasPaywall) score -= 0.3;
        if (requiresLogin) score -= 0.2;

        // 광고 밀도 감점 (최대 0.2)
        score -= adDensity * 0.2;

        // 콘텐츠 비율 가점 (최대 0.2)
        score += contentRatio * 0.2;

        // 단어 수 가점 (100-5000 단어 범위)
        if (wordCount >= 100 && wordCount <= 5000)
        {
            score += 0.1;
        }
        else if (wordCount > 5000)
        {
            score += 0.05; // 너무 긴 콘텐츠는 약간만 가점
        }

        // 구조화 가점
        if (headingCount >= 2) score += 0.05;
        if (hasMetadata) score += 0.05;

        return Math.Max(0, Math.Min(1, score));
    }

    private static double CalculateLlmSuitabilityScore(
        double contentRatio,
        double adDensity,
        int wordCount,
        int estimatedTokens)
    {
        var score = 0.5;

        // 콘텐츠 비율 가점
        score += contentRatio * 0.3;

        // 광고 감점
        score -= adDensity * 0.2;

        // 적정 길이 가점 (500-3000 단어)
        if (wordCount >= 500 && wordCount <= 3000)
        {
            score += 0.2;
        }
        else if (wordCount < 500)
        {
            score -= 0.1;
        }

        // 토큰 효율성 (8000 토큰 이하 권장)
        if (estimatedTokens <= 8000)
        {
            score += 0.1;
        }
        else if (estimatedTokens > 32000)
        {
            score -= 0.2;
        }

        return Math.Max(0, Math.Min(1, score));
    }

    private static string ExtractTextFromHtml(string html)
    {
        // 간단한 HTML 태그 제거
        var text = HtmlTagRegex().Replace(html, " ");
        text = MultiSpaceRegex().Replace(text, " ");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    [GeneratedRegex(@"[\uAC00-\uD7A3]")]
    private static partial Regex KoreanCharRegex();

    [GeneratedRegex(@"[\u4E00-\u9FFF]")]
    private static partial Regex ChineseCharRegex();

    [GeneratedRegex(@"[\u3040-\u309F\u30A0-\u30FF]")]
    private static partial Regex JapaneseCharRegex();

    [GeneratedRegex(@"[\uAC00-\uD7A3\u4E00-\u9FFF\u3040-\u309F\u30A0-\u30FF]")]
    private static partial Regex CjkCharRegex();

    [GeneratedRegex(@"<(ins|iframe)[^>]*(adsense|doubleclick|googlesyndication)[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex AdTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();
}
