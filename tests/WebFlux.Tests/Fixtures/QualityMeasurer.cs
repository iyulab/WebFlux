using System.Text.RegularExpressions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Fixtures;

/// <summary>
/// ExtractedContent를 분석하여 ExtractionQualityMetrics를 산출하는 헬퍼
/// </summary>
public static class QualityMeasurer
{
    /// <summary>
    /// 가중치 설정
    /// </summary>
    private const double WeightStructure = 0.25;
    private const double WeightContent = 0.35;
    private const double WeightNoise = 0.25;
    private const double WeightMarkdown = 0.15;

    /// <summary>
    /// 추출 결과와 원본 HTML을 비교하여 품질 메트릭 산출
    /// </summary>
    public static ExtractionQualityMetrics Measure(ExtractedContent result, string originalHtml)
    {
        var metrics = new ExtractionQualityMetrics();
        var markdown = result.Text ?? string.Empty;

        metrics.StructurePreservation = MeasureStructurePreservation(markdown, originalHtml, metrics.Issues);
        metrics.ContentCompleteness = MeasureContentCompleteness(markdown, originalHtml, metrics.Issues);
        metrics.NoiseRemoval = MeasureNoiseRemoval(markdown, originalHtml, metrics.Issues);
        metrics.MarkdownValidity = MeasureMarkdownValidity(markdown, metrics.Issues);

        metrics.OverallScore =
            WeightStructure * metrics.StructurePreservation +
            WeightContent * metrics.ContentCompleteness +
            WeightNoise * metrics.NoiseRemoval +
            WeightMarkdown * metrics.MarkdownValidity;

        return metrics;
    }

    /// <summary>
    /// 구조 보존율 측정: 원본 HTML의 구조 요소가 Markdown에 보존되었는지 확인
    /// </summary>
    private static double MeasureStructurePreservation(string markdown, string html, List<string> issues)
    {
        var checks = 0;
        var passed = 0;

        // 헤딩 보존 확인
        var htmlHeadings = Regex.Matches(html, @"<h([1-6])[^>]*>(.+?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (htmlHeadings.Count > 0)
        {
            checks++;
            var mdHeadings = Regex.Matches(markdown, @"^#{1,6}\s+.+$", RegexOptions.Multiline);
            // article/main 내부 헤딩만 카운트하기 어려우므로 존재 여부만 확인
            if (mdHeadings.Count > 0)
                passed++;
            else
                issues.Add("STRUCTURE: No headings found in Markdown output");
        }

        // 테이블 보존 확인
        var htmlTables = Regex.Matches(html, @"<table[\s>]", RegexOptions.IgnoreCase);
        // article/main 내 테이블만 카운트 (단순 근사)
        var mainTableCount = CountTablesInMainContent(html);
        if (mainTableCount > 0)
        {
            checks++;
            var mdTables = Regex.Matches(markdown, @"\|.*\|.*\|");
            if (mdTables.Count > 0)
                passed++;
            else
                issues.Add("STRUCTURE: Tables in main content not preserved in Markdown");
        }

        // 리스트 보존 확인
        var htmlLists = Regex.Matches(html, @"<[uo]l[\s>]", RegexOptions.IgnoreCase);
        if (htmlLists.Count > 0)
        {
            checks++;
            // Markdown 리스트: -, *, 또는 1. 형태
            var mdLists = Regex.Matches(markdown, @"^[\s]*[-*]\s+.+|^[\s]*\d+\.\s+.+", RegexOptions.Multiline);
            if (mdLists.Count > 0)
                passed++;
            else
                issues.Add("STRUCTURE: Lists not preserved in Markdown");
        }

        // 코드 블록 보존 확인
        var htmlCodeBlocks = Regex.Matches(html, @"<pre[\s>].*?<code[\s>]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (htmlCodeBlocks.Count > 0)
        {
            checks++;
            // Markdown 코드 블록: ``` 또는 들여쓰기 4칸
            var mdCodeBlocks = Regex.Matches(markdown, @"```[\s\S]*?```|^    .+", RegexOptions.Multiline);
            if (mdCodeBlocks.Count > 0)
                passed++;
            else
                issues.Add("STRUCTURE: Code blocks not preserved in Markdown");
        }

        // 인용문 보존 확인
        var htmlBlockquotes = Regex.Matches(html, @"<blockquote[\s>]", RegexOptions.IgnoreCase);
        if (htmlBlockquotes.Count > 0)
        {
            checks++;
            var mdBlockquotes = Regex.Matches(markdown, @"^>\s+.+", RegexOptions.Multiline);
            if (mdBlockquotes.Count > 0)
                passed++;
            else
                issues.Add("STRUCTURE: Blockquotes not preserved in Markdown");
        }

        // 이미지 보존 확인 (메인 콘텐츠 내)
        var hasMainImages = Regex.IsMatch(html, @"<(article|main)[\s>][\s\S]*?<img\s", RegexOptions.IgnoreCase);
        if (hasMainImages)
        {
            checks++;
            var mdImages = Regex.Matches(markdown, @"!\[.*?\]\(.+?\)");
            if (mdImages.Count > 0)
                passed++;
            else
                issues.Add("STRUCTURE: Images in main content not preserved in Markdown");
        }

        return checks > 0 ? (double)passed / checks : 1.0;
    }

    /// <summary>
    /// 핵심 콘텐츠 포함율 측정
    /// </summary>
    private static double MeasureContentCompleteness(string markdown, string html, List<string> issues)
    {
        // article/main 태그 내부의 텍스트 추출
        var mainContentTexts = ExtractMainContentTexts(html);

        if (mainContentTexts.Count == 0)
        {
            // article/main 태그가 없으면 body 전체 텍스트 사용
            var bodyText = ExtractTextFromHtml(html);
            if (string.IsNullOrWhiteSpace(bodyText))
                return 1.0; // 빈 페이지

            // 텍스트가 있으면 일부라도 포함되는지 확인
            var words = bodyText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Take(20)
                .ToList();

            if (words.Count == 0)
                return 1.0;

            var found = words.Count(w => markdown.Contains(w, StringComparison.OrdinalIgnoreCase));
            return (double)found / words.Count;
        }

        var totalSamples = 0;
        var foundSamples = 0;

        foreach (var text in mainContentTexts)
        {
            // 주요 단어 샘플링 (4글자 이상 단어)
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !w.StartsWith('<'))
                .Take(10)
                .ToList();

            totalSamples += words.Count;
            foundSamples += words.Count(w => markdown.Contains(w, StringComparison.OrdinalIgnoreCase));
        }

        var completeness = totalSamples > 0 ? (double)foundSamples / totalSamples : 1.0;

        if (completeness < 0.5)
            issues.Add($"CONTENT: Only {completeness:P0} of main content words found in Markdown");

        return completeness;
    }

    /// <summary>
    /// 노이즈 제거율 측정
    /// </summary>
    private static double MeasureNoiseRemoval(string markdown, string html, List<string> issues)
    {
        var noiseChecks = 0;
        var noiseRemoved = 0;

        // 네비게이션 텍스트 제거 확인
        var navTexts = ExtractTextsFromTag(html, "nav");
        foreach (var navText in navTexts.Take(3))
        {
            if (string.IsNullOrWhiteSpace(navText) || navText.Length < 5)
                continue;
            noiseChecks++;
            if (!markdown.Contains(navText, StringComparison.OrdinalIgnoreCase))
                noiseRemoved++;
        }

        // 푸터 텍스트 제거 확인
        var footerTexts = ExtractTextsFromTag(html, "footer");
        foreach (var footerText in footerTexts.Take(3))
        {
            var cleanText = Regex.Replace(footerText, @"<[^>]+>", "").Trim();
            if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 5)
                continue;
            noiseChecks++;
            if (!markdown.Contains(cleanText, StringComparison.OrdinalIgnoreCase))
                noiseRemoved++;
        }

        // 광고 클래스 콘텐츠 제거 확인
        var adPatterns = new[] { "advertisement", "ad-container", "sponsored", "cookie-banner", "cookie-consent", "social-share" };
        foreach (var pattern in adPatterns)
        {
            var adContent = ExtractTextFromClassOrId(html, pattern);
            if (string.IsNullOrWhiteSpace(adContent) || adContent.Length < 5)
                continue;
            noiseChecks++;
            if (!markdown.Contains(adContent, StringComparison.OrdinalIgnoreCase))
                noiseRemoved++;
        }

        // 댓글 섹션 제거 확인
        var commentContent = ExtractTextFromClassOrId(html, "comments")
            ?? ExtractTextFromClassOrId(html, "comment-section");
        if (!string.IsNullOrWhiteSpace(commentContent) && commentContent.Length > 10)
        {
            noiseChecks++;
            if (!markdown.Contains(commentContent, StringComparison.OrdinalIgnoreCase))
                noiseRemoved++;
        }

        if (noiseChecks == 0)
            return 1.0;

        var removal = (double)noiseRemoved / noiseChecks;

        if (removal < 0.5)
            issues.Add($"NOISE: Only {removal:P0} of noise elements were removed");

        return removal;
    }

    /// <summary>
    /// Markdown 유효성 측정
    /// </summary>
    private static double MeasureMarkdownValidity(string markdown, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return 0.0;

        var checks = 0;
        var passed = 0;

        // HTML 태그 잔여 확인 (적을수록 좋음)
        checks++;
        var remainingHtmlTags = Regex.Matches(markdown, @"<(?!!)/?[a-z][^>]*>", RegexOptions.IgnoreCase);
        // img, br 등 self-closing은 허용
        var problematicTags = remainingHtmlTags.Count(m =>
            !m.Value.StartsWith("<img", StringComparison.OrdinalIgnoreCase) &&
            !m.Value.StartsWith("<br", StringComparison.OrdinalIgnoreCase));
        if (problematicTags <= 2)
            passed++;
        else
            issues.Add($"MARKDOWN: {problematicTags} HTML tags remain in Markdown output");

        // 깨진 Markdown 링크 확인
        checks++;
        var brokenLinks = Regex.Matches(markdown, @"\[([^\]]*)\]\(\s*\)");
        if (brokenLinks.Count == 0)
            passed++;
        else
            issues.Add($"MARKDOWN: {brokenLinks.Count} broken Markdown links found");

        // 연속 빈 줄 제한 (3줄 이상 연속은 비정상)
        checks++;
        var excessiveBlankLines = Regex.Matches(markdown, @"\n{4,}");
        if (excessiveBlankLines.Count == 0)
            passed++;
        else
            issues.Add($"MARKDOWN: {excessiveBlankLines.Count} excessive blank line sequences");

        // 최소 텍스트 길이 (너무 짧으면 추출 실패)
        checks++;
        if (markdown.Length >= 50)
            passed++;
        else
            issues.Add($"MARKDOWN: Output too short ({markdown.Length} chars)");

        return checks > 0 ? (double)passed / checks : 1.0;
    }

    #region HTML 파싱 유틸리티

    private static int CountTablesInMainContent(string html)
    {
        // article 또는 main 태그 내의 table 수
        var mainMatch = Regex.Match(html, @"<(article|main)[\s>][\s\S]*?</\1>", RegexOptions.IgnoreCase);
        if (!mainMatch.Success)
            return 0;

        return Regex.Count(mainMatch.Value, @"<table[\s>]", RegexOptions.IgnoreCase);
    }

    private static List<string> ExtractMainContentTexts(string html)
    {
        var texts = new List<string>();

        // article 태그 내부 p 태그의 텍스트 추출
        var mainMatches = Regex.Matches(html,
            @"<(article|main)[\s>][\s\S]*?</\1>",
            RegexOptions.IgnoreCase);

        foreach (Match mainMatch in mainMatches)
        {
            var paragraphs = Regex.Matches(mainMatch.Value,
                @"<p[^>]*>([\s\S]*?)</p>",
                RegexOptions.IgnoreCase);

            foreach (Match p in paragraphs)
            {
                var text = Regex.Replace(p.Groups[1].Value, @"<[^>]+>", " ").Trim();
                if (text.Length > 10)
                    texts.Add(text);
            }
        }

        return texts;
    }

    private static string ExtractTextFromHtml(string html)
    {
        var bodyMatch = Regex.Match(html, @"<body[^>]*>([\s\S]*?)</body>", RegexOptions.IgnoreCase);
        var content = bodyMatch.Success ? bodyMatch.Groups[1].Value : html;
        return Regex.Replace(content, @"<[^>]+>", " ").Trim();
    }

    private static List<string> ExtractTextsFromTag(string html, string tagName)
    {
        var texts = new List<string>();
        var matches = Regex.Matches(html,
            $@"<{tagName}[\s>][\s\S]*?</{tagName}>",
            RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var text = Regex.Replace(match.Value, @"<[^>]+>", " ").Trim();
            // 주요 단어만 추출 (최소 단위)
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
                texts.Add(string.Join(" ", words.Take(5)));
        }

        return texts;
    }

    private static string? ExtractTextFromClassOrId(string html, string classOrId)
    {
        // class 또는 id에서 텍스트 추출
        var pattern = $@"<[^>]*(class\s*=\s*""[^""]*{Regex.Escape(classOrId)}[^""]*""|id\s*=\s*""{Regex.Escape(classOrId)}"")[^>]*>([\s\S]*?)</[^>]+>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        var text = Regex.Replace(match.Groups[2].Value, @"<[^>]+>", " ").Trim();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 2 ? string.Join(" ", words.Take(5)) : null;
    }

    #endregion
}
