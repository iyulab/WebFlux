using System.Globalization;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Models;

namespace WebFlux.Infrastructure.Html;

/// <summary>
/// HTML 메타데이터 추출기
/// meta 태그, OpenGraph, Twitter Card, JSON-LD를 추출합니다
/// </summary>
public class HtmlMetadataExtractor
{
    private readonly ILogger<HtmlMetadataExtractor> _logger;

    public HtmlMetadataExtractor(ILogger<HtmlMetadataExtractor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// HTML 문서에서 메타데이터를 추출합니다
    /// </summary>
    public HtmlMetadataSnapshot Extract(string html, string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        try
        {
            _logger.LogDebug("Extracting HTML metadata from URL: {Url}", url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var snapshot = new HtmlMetadataSnapshot
            {
                ExtractedAt = DateTimeOffset.UtcNow
            };

            // 1. 표준 meta 태그 추출
            ExtractMetaTags(doc, snapshot);

            // 2. OpenGraph 추출
            snapshot.OpenGraph = ExtractOpenGraph(doc);

            // 3. Twitter Card 추출
            snapshot.TwitterCard = ExtractTwitterCard(doc);

            // 4. JSON-LD 구조화 데이터 추출
            ExtractJsonLd(doc, snapshot);

            _logger.LogDebug(
                "HTML metadata extraction completed. OpenGraph: {HasOG}, TwitterCard: {HasTwitter}",
                snapshot.OpenGraph != null,
                snapshot.TwitterCard != null);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract HTML metadata from URL: {Url}", url);
            throw;
        }
    }

    // ===================================================================
    // Private Helper Methods
    // ===================================================================

    /// <summary>
    /// 표준 meta 태그 추출
    /// </summary>
    private void ExtractMetaTags(HtmlDocument doc, HtmlMetadataSnapshot snapshot)
    {
        var metaTags = doc.DocumentNode.SelectNodes("//meta[@name and @content]");
        if (metaTags == null) return;

        foreach (var meta in metaTags)
        {
            var name = meta.GetAttributeValue("name", "");
            var content = meta.GetAttributeValue("content", "");

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(content))
            {
                snapshot.MetaTags[name] = content;
            }
        }
    }

    /// <summary>
    /// OpenGraph 메타데이터 추출
    /// </summary>
    private OpenGraphData? ExtractOpenGraph(HtmlDocument doc)
    {
        var ogTags = doc.DocumentNode.SelectNodes("//meta[starts-with(@property, 'og:')]");
        if (ogTags == null || !ogTags.Any()) return null;

        var og = new OpenGraphData();

        foreach (var meta in ogTags)
        {
            var property = meta.GetAttributeValue("property", "");
            var content = meta.GetAttributeValue("content", "");

            if (string.IsNullOrWhiteSpace(content)) continue;

            switch (property)
            {
                case "og:title":
                    og.Title = content;
                    break;
                case "og:description":
                    og.Description = content;
                    break;
                case "og:type":
                    og.Type = content;
                    break;
                case "og:url":
                    og.Url = content;
                    break;
                case "og:image":
                    og.Image = content;
                    break;
                case "og:site_name":
                    og.SiteName = content;
                    break;
                case "og:locale":
                    og.Locale = content;
                    break;
                case "article:published_time":
                    if (DateTimeOffset.TryParse(content, out var published))
                    {
                        og.PublishedTime = published;
                    }
                    break;
                case "article:modified_time":
                    if (DateTimeOffset.TryParse(content, out var modified))
                    {
                        og.ModifiedTime = modified;
                    }
                    break;
                case "article:author":
                    og.Author = content;
                    break;
                case "article:section":
                    og.Section = content;
                    break;
                case "article:tag":
                    og.Tags.Add(content);
                    break;
            }
        }

        // 유효성 검증: title이 있어야 유효한 OpenGraph
        return string.IsNullOrWhiteSpace(og.Title) ? null : og;
    }

    /// <summary>
    /// Twitter Card 메타데이터 추출
    /// </summary>
    private TwitterCardData? ExtractTwitterCard(HtmlDocument doc)
    {
        var twitterTags = doc.DocumentNode.SelectNodes("//meta[starts-with(@name, 'twitter:') or starts-with(@property, 'twitter:')]");
        if (twitterTags == null || !twitterTags.Any()) return null;

        var twitter = new TwitterCardData();

        foreach (var meta in twitterTags)
        {
            var name = meta.GetAttributeValue("name", "") ?? meta.GetAttributeValue("property", "");
            var content = meta.GetAttributeValue("content", "");

            if (string.IsNullOrWhiteSpace(content)) continue;

            switch (name)
            {
                case "twitter:card":
                    twitter.Card = content;
                    break;
                case "twitter:site":
                    twitter.Site = content;
                    break;
                case "twitter:creator":
                    twitter.Creator = content;
                    break;
                case "twitter:title":
                    twitter.Title = content;
                    break;
                case "twitter:description":
                    twitter.Description = content;
                    break;
                case "twitter:image":
                    twitter.Image = content;
                    break;
            }
        }

        // 유효성 검증: card 타입이 있어야 유효한 Twitter Card
        return string.IsNullOrWhiteSpace(twitter.Card) ? null : twitter;
    }

    /// <summary>
    /// JSON-LD 구조화 데이터 추출
    /// </summary>
    private void ExtractJsonLd(HtmlDocument doc, HtmlMetadataSnapshot snapshot)
    {
        var jsonLdScripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (jsonLdScripts == null) return;

        foreach (var script in jsonLdScripts)
        {
            var jsonContent = script.InnerText?.Trim();
            if (string.IsNullOrWhiteSpace(jsonContent)) continue;

            try
            {
                var jsonData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                if (jsonData != null && jsonData.TryGetValue("@type", out var type))
                {
                    snapshot.StructuredData[type.ToString() ?? "Unknown"] = jsonData;
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON-LD structured data");
            }
        }
    }
}
