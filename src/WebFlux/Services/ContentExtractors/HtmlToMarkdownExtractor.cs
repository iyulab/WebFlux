using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ReverseMarkdown;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// HTML→Markdown 변환 추출기
/// ReverseMarkdown 기반으로 HTML 구조를 보존하는 Markdown 변환 수행
/// 파이프라인: HtmlContentCleaner → ReverseMarkdown → TextDensityFilter
/// </summary>
public class HtmlToMarkdownExtractor : IContentExtractor
{
    private readonly ILogger<HtmlToMarkdownExtractor>? _logger;
    private readonly HtmlContentCleaner _cleaner;
    private readonly TextDensityFilter _densityFilter;
    private readonly Converter _markdownConverter;
    private readonly BasicContentExtractor _fallbackExtractor;

    public HtmlToMarkdownExtractor(
        IEventPublisher? eventPublisher,
        ILogger<HtmlToMarkdownExtractor>? logger = null,
        ILogger<HtmlContentCleaner>? cleanerLogger = null,
        ILogger<TextDensityFilter>? filterLogger = null)
    {
        _logger = logger;
        _cleaner = new HtmlContentCleaner(cleanerLogger);
        _densityFilter = new TextDensityFilter(filterLogger);
        _fallbackExtractor = new BasicContentExtractor(eventPublisher);

        _markdownConverter = new Converter(new ReverseMarkdown.Config
        {
            GithubFlavored = true,
            SmartHrefHandling = true,
            RemoveComments = true,
            UnknownTags = Config.UnknownTagsOption.Bypass
        });
    }

    /// <summary>
    /// HTML 콘텐츠에서 Markdown으로 변환하여 추출
    /// </summary>
    public async Task<ExtractedContent> ExtractFromHtmlAsync(
        string htmlContent,
        string sourceUrl,
        bool enableMetadataExtraction = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return new ExtractedContent
            {
                Text = string.Empty,
                MainContent = string.Empty,
                Url = sourceUrl,
                Title = "Untitled"
            };
        }

        try
        {
            return await ExecutePipelineAsync(htmlContent, sourceUrl, enableMetadataExtraction, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "HtmlToMarkdown conversion failed, falling back to BasicContentExtractor for {Url}", sourceUrl);
            return await _fallbackExtractor.ExtractFromHtmlAsync(htmlContent, sourceUrl, enableMetadataExtraction, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 변환 파이프라인 실행
    /// </summary>
    private async Task<ExtractedContent> ExecutePipelineAsync(
        string htmlContent,
        string sourceUrl,
        bool enableMetadataExtraction,
        CancellationToken cancellationToken)
    {
        // 제목 먼저 추출 (원본 HTML에서)
        var title = ExtractTitle(htmlContent);

        // [1] HtmlContentCleaner — nav/header/footer/광고 제거, URL 절대경로 변환
        var cleanedHtml = await _cleaner.CleanAsync(
            htmlContent, sourceUrl, HtmlCleaningOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        // [2] ReverseMarkdown.Convert() — HTML→Markdown (GFM)
        var rawMarkdown = ConvertToMarkdown(cleanedHtml);

        // → RawMarkdown에 저장
        string? fitMarkdown = null;

        // [3] TextDensityFilter — 보일러플레이트 제거 (선택적)
        try
        {
            var filteredHtml = await _densityFilter.FilterAsync(cleanedHtml, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(filteredHtml))
            {
                fitMarkdown = ConvertToMarkdown(filteredHtml);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "TextDensityFilter failed, using RawMarkdown as FitMarkdown");
        }

        // [4] 메타데이터 추출
        var mainText = fitMarkdown ?? rawMarkdown;
        var headings = ExtractHeadings(mainText);
        var imageUrls = ExtractImageUrls(mainText);

        var extracted = new ExtractedContent
        {
            // Text: 하위 호환성 (FitMarkdown이 있으면 사용, 없으면 RawMarkdown)
            Text = NormalizeMarkdown(mainText),
            MainContent = NormalizeMarkdown(mainText),
            Url = sourceUrl,
            Title = title,
            Headings = headings,
            ImageUrls = imageUrls,
            RawMarkdown = rawMarkdown,
            FitMarkdown = fitMarkdown,
            OriginalHtml = htmlContent,
            ExtractionMethod = "HtmlToMarkdown",
            ExtractionTimestamp = DateTimeOffset.UtcNow,
            WordCount = CountWords(mainText),
            CharacterCount = mainText.Length,
            ReadingTimeMinutes = Math.Max(1, Math.Round(CountWords(mainText) / 200.0, 1))
        };

        if (enableMetadataExtraction)
        {
            extracted.Metadata = new EnrichedMetadata
            {
                Url = sourceUrl,
                Domain = !string.IsNullOrEmpty(sourceUrl) ? new Uri(sourceUrl).Host : string.Empty,
                Title = title,
                Description = ExtractDescription(mainText),
                Language = "en",
                Source = MetadataSource.Html,
                ExtractedAt = DateTimeOffset.UtcNow
            };

            extracted.Metadata.FieldSources["title"] = MetadataSource.Html;
            extracted.Metadata.FieldSources["description"] = MetadataSource.Html;
        }

        _logger?.LogInformation(
            "HTML-to-Markdown extraction completed for {Url}: RawMarkdown={RawLength}, FitMarkdown={FitLength}",
            sourceUrl, rawMarkdown.Length, fitMarkdown?.Length ?? 0);

        return extracted;
    }

    /// <summary>
    /// HTML을 Markdown으로 변환
    /// </summary>
    private string ConvertToMarkdown(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _markdownConverter.Convert(html);
    }

    /// <summary>
    /// Markdown 정규화 (연속 빈 줄 제거, 앞뒤 공백 정리)
    /// </summary>
    private static string NormalizeMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // 연속 빈 줄을 2줄로 제한
        var normalized = Regex.Replace(markdown, @"\n{3,}", "\n\n");
        return normalized.Trim();
    }

    /// <summary>
    /// HTML에서 제목 추출
    /// article/main 내부의 h1이 title보다 정확한 경우가 많으므로 우선 사용
    /// </summary>
    private static string ExtractTitle(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "Untitled";

        // article/main 내부의 <h1>이 가장 정확한 제목
        var articleH1Match = Regex.Match(html,
            @"<(?:article|main)[\s>][\s\S]*?<h1[^>]*>([\s\S]*?)</h1>",
            RegexOptions.IgnoreCase);
        if (articleH1Match.Success)
        {
            var h1Text = Regex.Replace(articleH1Match.Groups[1].Value, @"<[^>]+>", "").Trim();
            if (!string.IsNullOrWhiteSpace(h1Text))
                return System.Web.HttpUtility.HtmlDecode(h1Text);
        }

        // <title> 태그에서 추출
        var titleMatch = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
            return System.Web.HttpUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());

        // 일반 <h1> 태그에서 추출 시도
        var h1Match = Regex.Match(html, @"<h1[^>]*>([\s\S]*?)</h1>", RegexOptions.IgnoreCase);
        if (h1Match.Success)
        {
            var h1Text = Regex.Replace(h1Match.Groups[1].Value, @"<[^>]+>", "").Trim();
            if (!string.IsNullOrWhiteSpace(h1Text))
                return System.Web.HttpUtility.HtmlDecode(h1Text);
        }

        return "Untitled";
    }

    /// <summary>
    /// Markdown에서 헤딩 추출
    /// </summary>
    private static List<string> ExtractHeadings(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<string>();

        return Regex.Matches(markdown, @"^(#{1,6})\s+(.+)$", RegexOptions.Multiline)
            .Select(m => m.Value.Trim())
            .ToList();
    }

    /// <summary>
    /// Markdown에서 이미지 URL 추출
    /// </summary>
    private static List<string> ExtractImageUrls(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<string>();

        return Regex.Matches(markdown, @"!\[.*?\]\((.+?)\)")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 설명 추출 (첫 2문장)
    /// </summary>
    private static string ExtractDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Markdown 헤딩 제거 후 첫 문단 추출
        var lines = text.Split('\n')
            .Where(l => !l.TrimStart().StartsWith('#') && !string.IsNullOrWhiteSpace(l))
            .Take(3);

        var description = string.Join(" ", lines).Trim();
        return description.Length > 300 ? description[..300] + "..." : description;
    }

    /// <summary>
    /// 단어 수 계산
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    #region IContentExtractor 나머지 메서드 (BasicContentExtractor에 위임)

    public Task<ExtractedContent> ExtractFromMarkdownAsync(string markdownContent, string sourceUrl, CancellationToken cancellationToken = default)
        => _fallbackExtractor.ExtractFromMarkdownAsync(markdownContent, sourceUrl, cancellationToken);

    public Task<ExtractedContent> ExtractFromJsonAsync(string jsonContent, string sourceUrl, CancellationToken cancellationToken = default)
        => _fallbackExtractor.ExtractFromJsonAsync(jsonContent, sourceUrl, cancellationToken);

    public Task<ExtractedContent> ExtractFromXmlAsync(string xmlContent, string sourceUrl, CancellationToken cancellationToken = default)
        => _fallbackExtractor.ExtractFromXmlAsync(xmlContent, sourceUrl, cancellationToken);

    public Task<ExtractedContent> ExtractFromTextAsync(string textContent, string sourceUrl, CancellationToken cancellationToken = default)
        => _fallbackExtractor.ExtractFromTextAsync(textContent, sourceUrl, cancellationToken);

    public Task<ExtractedContent> ExtractAutoAsync(string content, string sourceUrl, string? contentType = null, CancellationToken cancellationToken = default)
    {
        return contentType?.ToLowerInvariant() switch
        {
            var ct when ct != null && (ct.Contains("html") || ct.Contains("xhtml"))
                => ExtractFromHtmlAsync(content, sourceUrl, false, cancellationToken),
            "text/markdown" => ExtractFromMarkdownAsync(content, sourceUrl, cancellationToken),
            "application/json" => ExtractFromJsonAsync(content, sourceUrl, cancellationToken),
            var ct when ct != null && ct.Contains("xml")
                => ExtractFromXmlAsync(content, sourceUrl, cancellationToken),
            _ => ExtractFromTextAsync(content, sourceUrl, cancellationToken)
        };
    }

    public IReadOnlyList<string> GetSupportedContentTypes()
    {
        return new[]
        {
            "text/html",
            "application/xhtml+xml",
            "text/markdown",
            "application/json",
            "application/xml",
            "text/xml",
            "text/plain"
        };
    }

    public ExtractionStatistics GetStatistics()
    {
        return new ExtractionStatistics
        {
            TotalExtractions = 0,
            SuccessfulExtractions = 0,
            FailedExtractions = 0,
            AverageProcessingTimeMs = 0,
            SupportedContentTypes = GetSupportedContentTypes().Count
        };
    }

    #endregion
}
