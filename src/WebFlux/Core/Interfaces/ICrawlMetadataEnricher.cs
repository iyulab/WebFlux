using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// The front door of the metadata subsystem on the crawl path: turns the five metadata options of
/// <see cref="CrawlOptions"/> (<see cref="CrawlOptions.UseHtmlMetadata"/>,
/// <see cref="CrawlOptions.EnableMetadataExtraction"/>, <see cref="CrawlOptions.MetadataSchema"/>,
/// <see cref="CrawlOptions.CustomMetadataPrompt"/>, <see cref="CrawlOptions.MetadataExtractionMaxChars"/>)
/// into what the crawl result carries. Before 0.13.0 the extractors behind it were implemented and
/// registered nowhere, and no code read those options.
/// </summary>
public interface ICrawlMetadataEnricher
{
    /// <summary>
    /// Enriches <paramref name="extracted"/>'s <see cref="ExtractedContent.Metadata"/> according to
    /// <paramref name="options"/>: the HTML snapshot (meta tags, OpenGraph, Twitter Card, JSON-LD) when
    /// <see cref="CrawlOptions.UseHtmlMetadata"/> is on and the content is HTML; the AI extraction when
    /// <see cref="CrawlOptions.EnableMetadataExtraction"/> is on and an AI extractor is available. Fields the
    /// extractor already filled are never overwritten by the HTML snapshot; an AI result below
    /// <see cref="CrawlOptions.MinConfidence"/> is not merged.
    /// </summary>
    /// <param name="extracted">The extracted content to enrich (returned, possibly mutated).</param>
    /// <param name="rawContent">The raw page content the crawler received (HTML for the snapshot).</param>
    /// <param name="contentType">The page's content type.</param>
    /// <param name="options">The crawl options in effect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ExtractedContent> EnrichAsync(
        ExtractedContent extracted,
        string rawContent,
        string contentType,
        CrawlOptions options,
        CancellationToken cancellationToken = default);
}
