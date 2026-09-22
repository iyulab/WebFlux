using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Infrastructure.Html;

namespace WebFlux.Services.MetadataEnrichment;

/// <summary>
/// Default <see cref="ICrawlMetadataEnricher"/>: the HTML snapshot comes from
/// <see cref="HtmlMetadataExtractor"/> (no AI needed); the AI extraction from the
/// <see cref="IWebMetadataExtractor"/> the container provides, or none — in which case
/// <see cref="CrawlOptions.EnableMetadataExtraction"/> is honoured with a warning rather than silently.
/// </summary>
public sealed partial class CrawlMetadataEnricher : ICrawlMetadataEnricher
{
    private readonly HtmlMetadataExtractor _html;
    private readonly IWebMetadataExtractor? _ai;
    private readonly ILogger<CrawlMetadataEnricher> _logger;
    private int _warnedNoExtractor;

    /// <param name="html">The HTML metadata extractor.</param>
    /// <param name="ai">The AI metadata extractor, or null when no AI service is registered.</param>
    /// <param name="logger">Logger.</param>
    public CrawlMetadataEnricher(HtmlMetadataExtractor html, IWebMetadataExtractor? ai, ILogger<CrawlMetadataEnricher>? logger = null)
    {
        _html = html ?? throw new ArgumentNullException(nameof(html));
        _ai = ai;
        _logger = logger ?? NullLogger<CrawlMetadataEnricher>.Instance;
    }

    /// <summary>Whether an AI extractor is available to this enricher.</summary>
    public bool HasAiExtractor => _ai is not null;

    /// <inheritdoc />
    public async Task<ExtractedContent> EnrichAsync(
        ExtractedContent extracted,
        string rawContent,
        string contentType,
        CrawlOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(extracted);
        ArgumentNullException.ThrowIfNull(options);

        var url = string.IsNullOrEmpty(extracted.Url) ? extracted.OriginalUrl : extracted.Url;

        HtmlMetadataSnapshot? snapshot = null;
        if (options.UseHtmlMetadata && IsHtml(contentType) && !string.IsNullOrWhiteSpace(rawContent) && !string.IsNullOrWhiteSpace(url))
        {
            snapshot = _html.Extract(rawContent, url);
            extracted.Metadata ??= NewMetadata(extracted, url);
            // Attached, never overwriting: the extractor's own title/description stay what they were.
            extracted.Metadata.HtmlMetadata = snapshot;
            if (string.IsNullOrWhiteSpace(extracted.Metadata.Title) && !string.IsNullOrWhiteSpace(snapshot.OpenGraph?.Title))
            {
                extracted.Metadata.Title = snapshot.OpenGraph!.Title;
                extracted.Metadata.FieldSources["title"] = MetadataSource.Html;
            }
            if (string.IsNullOrWhiteSpace(extracted.Metadata.Description) && !string.IsNullOrWhiteSpace(snapshot.OpenGraph?.Description))
            {
                extracted.Metadata.Description = snapshot.OpenGraph!.Description;
                extracted.Metadata.FieldSources["description"] = MetadataSource.Html;
            }
        }

        if (!options.EnableMetadataExtraction)
            return extracted;

        if (_ai is null)
        {
            // Enabled with nobody to do it: say so once, do not pretend.
            if (Interlocked.Exchange(ref _warnedNoExtractor, 1) == 0)
                LogNoAiExtractor(_logger);
            return extracted;
        }

        var text = string.IsNullOrWhiteSpace(extracted.MainContent) ? extracted.Text : extracted.MainContent;
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(url))
            return extracted;

        var sample = MetadataContentSampler.Sample(extracted.Title, extracted.Headings, text, options.MetadataExtractionMaxChars);
        var enriched = await _ai.ExtractAsync(sample, url, snapshot, options.MetadataSchema, options.CustomMetadataPrompt, cancellationToken).ConfigureAwait(false);

        if (enriched.OverallConfidence < options.MinConfidence)
        {
            LogBelowConfidence(_logger, url, enriched.OverallConfidence, options.MinConfidence);
            return extracted;
        }

        extracted.Metadata = MergeInto(extracted.Metadata, enriched);
        return extracted;
    }

    private static bool IsHtml(string? contentType) =>
        contentType is null
        || contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
        || contentType.Contains("xhtml", StringComparison.OrdinalIgnoreCase);

    private static EnrichedMetadata NewMetadata(ExtractedContent extracted, string url) => new()
    {
        Url = url,
        Domain = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : string.Empty,
        Title = string.IsNullOrWhiteSpace(extracted.Title) ? null : extracted.Title,
        Source = MetadataSource.Html,
        ExtractedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// The AI result becomes the metadata; what the extractor had and the AI did not answer is kept.
    /// </summary>
    private static EnrichedMetadata MergeInto(EnrichedMetadata? existing, EnrichedMetadata enriched)
    {
        if (existing is null)
            return enriched;

        enriched.Title ??= existing.Title;
        enriched.Description ??= existing.Description;
        enriched.Language ??= existing.Language;
        enriched.Author ??= existing.Author;
        enriched.PublishedDate ??= existing.PublishedDate;
        enriched.ModifiedDate ??= existing.ModifiedDate;
        if (enriched.Keywords.Count == 0 && existing.Keywords.Count > 0)
        {
            enriched.Keywords = existing.Keywords;
            enriched.FieldSources["keywords"] = existing.FieldSources.TryGetValue("keywords", out var ks) ? ks : MetadataSource.Html;
        }
        foreach (var (field, source) in existing.FieldSources)
        {
            enriched.FieldSources.TryAdd(field, source);
        }
        enriched.HtmlMetadata ??= existing.HtmlMetadata;
        return enriched;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "CrawlOptions.EnableMetadataExtraction is true but no IWebMetadataExtractor is available (register an ITextCompletionService or an IWebMetadataExtractor); crawl results carry HTML metadata only")]
    private static partial void LogNoAiExtractor(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AI metadata for {Url} discarded: confidence {Confidence} below CrawlOptions.MinConfidence {MinConfidence}")]
    private static partial void LogBelowConfidence(ILogger logger, string Url, float Confidence, float MinConfidence);
}

/// <summary>
/// What <see cref="CrawlOptions.MetadataExtractionMaxChars"/> promises: a long document is sampled as
/// title + headings + the first N characters of the body, so the prompt stays within the budget.
/// </summary>
public static class MetadataContentSampler
{
    /// <summary>
    /// Builds the sample. The title and headings come first (each on its own line); the body is cut so
    /// the whole sample is at most <paramref name="maxChars"/> characters. A non-positive
    /// <paramref name="maxChars"/> means no sampling: the body is used whole.
    /// </summary>
    public static string Sample(string? title, IReadOnlyList<string>? headings, string body, int maxChars)
    {
        body ??= string.Empty;
        if (maxChars <= 0)
            return body;

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title))
            sb.Append(title.Trim()).Append('\n');
        if (headings is { Count: > 0 })
        {
            foreach (var h in headings)
            {
                if (string.IsNullOrWhiteSpace(h))
                    continue;
                if (sb.Length + h.Length + 1 > maxChars)
                    break;
                sb.Append(h.Trim()).Append('\n');
            }
        }

        var remaining = maxChars - sb.Length;
        if (remaining > 0)
            sb.Append(body.Length > remaining ? body.AsSpan(0, remaining) : body.AsSpan());

        return sb.Length > maxChars ? sb.ToString(0, maxChars) : sb.ToString();
    }
}
