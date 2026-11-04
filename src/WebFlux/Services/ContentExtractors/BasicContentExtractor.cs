using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// 기본 콘텐츠 추출기 구현체
/// Interface Provider 패턴의 기본 구현체로 제공
/// </summary>
public partial class BasicContentExtractor : IContentExtractor
{
    private static readonly char[] WordSplitChars = [' ', '\t', '\n', '\r'];
    private readonly IEventPublisher _eventPublisher;

    public BasicContentExtractor(IEventPublisher? eventPublisher)
    {
        _eventPublisher = eventPublisher ?? new NullEventPublisher();
    }

    public Task<ExtractedContent> ExtractFromHtmlAsync(string htmlContent, string sourceUrl, bool enableMetadataExtraction = false, CancellationToken cancellationToken = default)
    {
        var cleanText = HtmlToPlainText(htmlContent);

        var extracted = new ExtractedContent
        {
            Text = cleanText.Trim(),
            MainContent = cleanText.Trim(), // 주요 콘텐츠는 추출된 텍스트와 동일하게 설정
            Url = sourceUrl,
            Title = ExtractTitle(htmlContent),
            WordCount = cleanText.Split(WordSplitChars, StringSplitOptions.RemoveEmptyEntries).Length,
            CharacterCount = cleanText.Length,
            ReadingTimeMinutes = Math.Max(1, cleanText.Split(WordSplitChars, StringSplitOptions.RemoveEmptyEntries).Length / 200.0)
        };

        if (enableMetadataExtraction)
        {
            extracted.Metadata = new EnrichedMetadata
            {
                Url = sourceUrl,
                Domain = !string.IsNullOrEmpty(sourceUrl) ? new Uri(sourceUrl).Host : string.Empty,
                Title = extracted.Title,
                Description = cleanText.Length > 200 ? string.Concat(cleanText.AsSpan(0, 200), "...") : cleanText,
                Language = "en",
                Source = MetadataSource.Html,
                ExtractedAt = DateTimeOffset.UtcNow
            };

            extracted.Metadata.FieldSources["title"] = MetadataSource.Html;
            extracted.Metadata.FieldSources["description"] = MetadataSource.Html;
        }

        return Task.FromResult(extracted);
    }

    public Task<ExtractedContent> ExtractFromMarkdownAsync(string markdownContent, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var outline = ReadMarkdownOutline(markdownContent);

        var extracted = new ExtractedContent
        {
            Text = markdownContent,
            MainContent = markdownContent, // 주요 콘텐츠는 원본 마크다운과 동일
            Url = sourceUrl,
            Title = outline.Title ?? "Markdown Content",
            Headings = outline.Headings
        };

        return Task.FromResult(extracted);
    }

    public Task<ExtractedContent> ExtractFromJsonAsync(string jsonContent, string sourceUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var text = doc.RootElement.ToString();

            var extracted = new ExtractedContent
            {
                Text = text,
                MainContent = text, // 주요 콘텐츠는 JSON 텍스트와 동일
                Url = sourceUrl,
                Title = "JSON Content"
            };

            return Task.FromResult(extracted);
        }
        catch
        {
            var extracted = new ExtractedContent
            {
                Text = jsonContent,
                MainContent = jsonContent, // 주요 콘텐츠는 원본 JSON과 동일
                Url = sourceUrl,
                Title = "JSON Content"
            };

            return Task.FromResult(extracted);
        }
    }

    public Task<ExtractedContent> ExtractFromXmlAsync(string xmlContent, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var extracted = new ExtractedContent
        {
            Text = xmlContent,
            MainContent = xmlContent, // 주요 콘텐츠는 XML 텍스트와 동일
            Url = sourceUrl,
            Title = "XML Content"
        };

        return Task.FromResult(extracted);
    }

    public Task<ExtractedContent> ExtractFromTextAsync(string textContent, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var extracted = new ExtractedContent
        {
            Text = textContent,
            MainContent = textContent, // 주요 콘텐츠는 원본 텍스트와 동일
            Url = sourceUrl,
            Title = "Text Content"
        };

        return Task.FromResult(extracted);
    }

    public Task<ExtractedContent> ExtractAutoAsync(string content, string sourceUrl, string? contentType = null, CancellationToken cancellationToken = default)
    {
        // "text/html; charset=utf-8" 처럼 매개변수가 붙은 값도 같은 형식이다
        var mediaType = contentType?.Split(';')[0].Trim().ToLowerInvariant();

        return mediaType switch
        {
            "text/html" or "application/xhtml+xml" => ExtractFromHtmlAsync(content, sourceUrl, false, cancellationToken),
            "text/markdown" or "text/x-markdown" => ExtractFromMarkdownAsync(content, sourceUrl, cancellationToken),
            "application/json" => ExtractFromJsonAsync(content, sourceUrl, cancellationToken),
            "application/xml" or "text/xml" => ExtractFromXmlAsync(content, sourceUrl, cancellationToken),
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

    private static string ExtractTitle(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "Untitled";

        var titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"<title[^>]*>([^<]+)</title>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return titleMatch.Success ? Whitespace().Replace(titleMatch.Groups[1].Value, " ").Trim() : "Untitled";
    }

    /// <summary>
    /// HTML 을 줄 단위 평문으로: script/style/noscript 본문은 글이 아니고, br 과 블록 요소의 경계는 줄바꿈,
    /// 표의 셀은 한 줄 안의 공백이다. 글자 사이의 인라인 태그는 단어를 가르지 않는다.
    /// </summary>
    private static string HtmlToPlainText(string html)
    {
        var text = NonTextElements().Replace(html, " ");
        text = BlockBoundaryTags().Replace(text, "\n");
        text = CellEndTags().Replace(text, " ");
        text = AnyTag().Replace(text, string.Empty);
        text = System.Web.HttpUtility.HtmlDecode(text);

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
            .Select(line => Whitespace().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Markdown 의 제목과 헤딩: front matter 의 title 이 먼저, 없으면 첫 "# " 헤딩(그것도 없으면 첫 헤딩).
    /// 코드 펜스 안의 # 줄과 4칸 이상 들여쓴 줄은 헤딩이 아니다. 원문은 바꾸지 않는다.
    /// </summary>
    private static (string? Title, List<string> Headings) ReadMarkdownOutline(string markdown)
    {
        var headings = new List<string>();
        string? frontMatterTitle = null;
        string? firstH1 = null;
        string? firstAny = null;

        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var start = 0;

        if (lines.Length > 1 && lines[0].TrimEnd() == "---")
        {
            var end = Array.FindIndex(lines, 1, l => l.TrimEnd() is "---" or "...");
            if (end > 0)
            {
                for (var i = 1; i < end; i++)
                {
                    var m = FrontMatterTitle().Match(lines[i]);
                    if (m.Success)
                    {
                        frontMatterTitle = m.Groups[1].Value.Trim().Trim('"', '\'');
                        break;
                    }
                }
                start = end + 1;
            }
        }

        char? fence = null;
        for (var i = start; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();
            var indent = line.Length - trimmed.Length;

            if (indent < 4 && (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal)))
            {
                if (fence is null)
                    fence = trimmed[0];
                else if (fence == trimmed[0])
                    fence = null;
                continue;
            }

            if (fence is not null)
                continue;

            var heading = AtxHeading().Match(line);
            if (!heading.Success)
                continue;

            var level = heading.Groups[1].Value.Length;
            var text = heading.Groups[2].Value.Trim();
            headings.Add(new string('#', level) + " " + text);
            firstAny ??= text;
            if (level == 1)
                firstH1 ??= text;
        }

        var title = !string.IsNullOrWhiteSpace(frontMatterTitle) ? frontMatterTitle : firstH1 ?? firstAny;
        return (title, headings);
    }

    [GeneratedRegex(@"<(script|style|noscript)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex NonTextElements();

    [GeneratedRegex(@"<br\s*/?>|</?(?:p|div|li|tr|h[1-6]|ul|ol|table|thead|tbody|section|article|header|footer|nav|aside|main|blockquote|pre|dl|dt|dd|form|figure|figcaption|details|summary|hr)\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockBoundaryTags();

    [GeneratedRegex(@"</t[dh]\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CellEndTags();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^title\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterTitle();

    [GeneratedRegex(@"^ {0,3}(#{1,6})[ \t]+(.+?)(?:[ \t]+#+)?[ \t]*$")]
    private static partial Regex AtxHeading();
}

/// <summary>
/// 이벤트 발행이 필요 없는 경우를 위한 Null Object 패턴 구현
/// </summary>
internal sealed class NullEventPublisher : IEventPublisher
{
    public Task PublishAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Publish(ProcessingEvent processingEvent)
    {
        // No-op
    }

    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : ProcessingEvent
    {
        return new NullDisposable();
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : ProcessingEvent
    {
        return new NullDisposable();
    }

    public IDisposable SubscribeAll(Func<ProcessingEvent, Task> handler)
    {
        return new NullDisposable();
    }

    public WebFlux.Core.Interfaces.EventPublishingStatistics GetStatistics()
    {
        return new WebFlux.Core.Interfaces.EventPublishingStatistics
        {
            TotalEventsPublished = 0,
            SubscriberCount = 0
        };
    }

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}