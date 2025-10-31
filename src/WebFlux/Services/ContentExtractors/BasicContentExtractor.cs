using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.Json;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// 기본 콘텐츠 추출기 구현체
/// Interface Provider 패턴의 기본 구현체로 제공
/// </summary>
public class BasicContentExtractor : IContentExtractor
{
    private readonly IEventPublisher _eventPublisher;

    public BasicContentExtractor(IEventPublisher? eventPublisher)
    {
        _eventPublisher = eventPublisher ?? new NullEventPublisher();
    }

    public Task<ExtractedContent> ExtractFromHtmlAsync(string htmlContent, string sourceUrl, bool enableMetadataExtraction = false, CancellationToken cancellationToken = default)
    {
        var cleanText = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]*>", "");
        cleanText = System.Web.HttpUtility.HtmlDecode(cleanText);

        var extracted = new ExtractedContent
        {
            Text = cleanText.Trim(),
            MainContent = cleanText.Trim(), // 주요 콘텐츠는 추출된 텍스트와 동일하게 설정
            Url = sourceUrl,
            Title = ExtractTitle(htmlContent),
            WordCount = cleanText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length,
            CharacterCount = cleanText.Length,
            ReadingTimeMinutes = Math.Max(1, cleanText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length / 200.0)
        };

        if (enableMetadataExtraction)
        {
            extracted.Metadata = new EnrichedMetadata
            {
                Url = sourceUrl,
                Domain = !string.IsNullOrEmpty(sourceUrl) ? new Uri(sourceUrl).Host : string.Empty,
                Title = extracted.Title,
                Description = cleanText.Length > 200 ? cleanText.Substring(0, 200) + "..." : cleanText,
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
        var extracted = new ExtractedContent
        {
            Text = markdownContent,
            MainContent = markdownContent, // 주요 콘텐츠는 원본 마크다운과 동일
            Url = sourceUrl,
            Title = "Markdown Content"
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
        return contentType?.ToLower() switch
        {
            "text/html" or "application/xhtml+xml" => ExtractFromHtmlAsync(content, sourceUrl, false, cancellationToken),
            "text/markdown" => ExtractFromMarkdownAsync(content, sourceUrl, cancellationToken),
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

    private string ExtractTitle(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "Untitled";

        var titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"<title[^>]*>([^<]+)</title>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Untitled";
    }
}

/// <summary>
/// 이벤트 발행이 필요 없는 경우를 위한 Null Object 패턴 구현
/// </summary>
internal class NullEventPublisher : IEventPublisher
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

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}