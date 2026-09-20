using System.Reflection;
using Iyu.Conventions.Testing;
using Xunit;

namespace WebFlux.Tests;

/// <summary>
/// Every public option in this library is read by the library. An option nothing reads is a promise it does not keep:
/// a caller sets it, and nothing changes and nothing is reported. The roster fails both ways — a new unread option,
/// and a listed one that has since been wired — so each change is recorded on purpose.
/// </summary>
public class OptionsReachabilityRosterTests
{
    private static readonly Assembly[] Libraries =
    [
        Assembly.Load("WebFlux"),
    ];

    /// <summary>
    /// Options accepted as unread today. Shrink this list; never grow it silently.
    /// <para>
    /// Everything below is the roster's opening baseline (2026-09-20), recorded as found rather than as
    /// judged: this library had no reachability roster, and its first run reported 138 unread public
    /// options across 25 types. None has been investigated, so none carries a reason of its own —
    /// recording them here is what makes the gate start green and makes the *next* unread option a
    /// failure instead of silently joining a crowd. Working through them is tracked in the umbrella's
    /// issue draft.
    /// </para>
    /// <para>
    /// Two are already gone: <c>CrawlOptions.RespectRobotsTxt</c> is read by the crawl loop as of
    /// 0.8.0 — the README promised it twice and nothing read it — and wiring it also made
    /// <c>CrawlOptions.UserAgent</c> reachable, which the roster caught in the other direction
    /// (listed as unread, now read). The roster fails both ways, so leaving either listed here would
    /// itself be the failure.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string[]> KnownUnread = new()
    {
        ["WebFlux.Configuration.WebFluxOptions"] =
        [
            "DevelopmentMode", "EnableMetrics", "EnableProfiling", "EnableVerboseLogging",
        ],
        ["WebFlux.Core.Models.CacheConfig"] =
        [
            "TtlSeconds", "Type",
        ],
        ["WebFlux.Core.Models.CdnConfig"] =
        [
            "AssetsUrl", "Url",
        ],
        ["WebFlux.Core.Models.CollectionConfig"] =
        [
            "Output", "Permalink",
        ],
        ["WebFlux.Core.Models.DefaultConfig"] =
        [
            "Scope", "Values",
        ],
        ["WebFlux.Core.Models.GitHubPagesConfig"] =
        [
            "Branch", "CustomDomain", "Folder",
        ],
        ["WebFlux.Core.Models.MarkdownConversionOptions"] =
        [
            "CustomSettings", "EnableAutoLinks", "EnableCodeHighlighting", "EnableEmojis", "EnableExtensions",
            "EnableFootnotes", "EnableMath", "EnableTables", "EnableTaskLists", "ExtractImageInfo",
            "GenerateAnchorIds", "GenerateTableOfContents", "ValidateLinks",
        ],
        ["WebFlux.Core.Models.NetlifyConfig"] =
        [
            "BuildCommand", "Environment", "PublishDirectory",
        ],
        ["WebFlux.Core.Models.SassConfig"] = ["Directory"],
        ["WebFlux.Core.Models.SocialMediaConfig"] =
        [
            "Facebook", "GitHub", "LinkedIn", "Twitter",
        ],
        ["WebFlux.Core.Models.VercelConfig"] =
        [
            "BuildCommand", "Functions", "OutputDirectory",
        ],
        ["WebFlux.Core.Options.AnalysisOptions"] =
        [
            "AdditionalOptions", "AnalyzeImages", "AnalyzeStructure", "EnrichMetadata", "ExtractSections",
            "ExtractTables", "MaxSectionDepth", "MinContentQuality", "MinSectionLength", "NoiseSelectors",
            "RemoveNoise", "TimeoutMs",
        ],
        ["WebFlux.Core.Options.ChunkingOptions"] =
        [
            "ChunkSize", "CreateHierarchy", "CustomSeparators", "EnableMultimodalProcessing",
            "EnableParallelProcessing", "IncludeImageDescriptions", "IncludeMetadata", "MultimodalOptions",
            "PreserveHeaders", "SplitCodeBlocks", "SplitTables", "UseMemoryOptimization", "UseStreaming",
        ],
        ["WebFlux.Core.Options.CrawlOptions"] =
        [
            "AllowedContentTypes", "AllowedDomains", "CacheExpirationMinutes", "CustomHeaders",
            "CustomMetadataPrompt", "DelayBetweenRequests", "DownloadImages", "EnableMetadataExtraction",
            "EnableScrolling", "FollowExternalLinks", "Headers", "MaxConcurrency", "MaxImageSizeBytes",
            "MetadataExtractionMaxChars", "MetadataSchema", "PriorityUrls", "StartUrls",
            "Timeout", "TimeoutMs", "UseCache", "UseHtmlMetadata", "WaitForSelector",
        ],
        ["WebFlux.Core.Options.EnhancementOptions"] = ["TimeoutMs"],
        ["WebFlux.Core.Options.ExtractOptions"] = ["IncludeLinks"],
        ["WebFlux.Core.Options.ExtractionOptions"] =
        [
            "AdditionalOptions", "CollectImageUrls", "CollectLinks", "ExtractMetadata", "HttpTimeoutMs",
            "Strategy", "UserAgent",
        ],
        ["WebFlux.Core.Options.HtmlChunkingOptions"] =
        [
            "IncludeDomPath", "PreserveDomStructure", "PreserveHeadingHierarchy",
        ],
        ["WebFlux.Core.Options.ImageToTextOptions"] =
        [
            "Context", "DetailLevel", "ExcludeElements", "IncludeElements", "Language", "MaxDescriptionLength",
            "Perspective",
        ],
        ["WebFlux.Core.Options.MultimodalProcessingOptions"] =
        [
            "EnableParallelProcessing", "EnableQualityEnhancement", "ImageTextFormat", "ImageToTextOptions",
            "MaxConcurrentImages", "MaxImages", "MinimumConfidence", "PriorityWeights", "RetryCount",
            "TextIntegrationStrategy", "TimeoutSeconds",
        ],
        ["WebFlux.Core.Options.PipelineOptions"] =
        [
            "AdditionalOptions", "Analysis", "Chunking", "Crawl", "EnabledStages", "Extraction", "MaxConcurrency",
            "ProgressReportIntervalMs", "Reconstruction", "TotalTimeoutMs",
        ],
        ["WebFlux.Core.Options.ReconstructOptions"] =
        [
            "AdditionalOptions", "MaxLength", "MinLength", "PreserveOriginal", "QualityTarget", "TimeoutMs",
        ],
        ["WebFlux.Core.Options.RewriteOptions"] = ["AddExamples"],
        ["WebFlux.Core.Options.SummaryOptions"] =
        [
            "FocusOnKeyPoints", "TargetLanguage",
        ],
        ["WebFlux.Core.Options.TextCompletionOptions"] = ["AdditionalProperties"],
    };

    [Fact]
    public void EveryPublicOption_IsRead() =>
        OptionsReachability.Scan(Libraries, OptionsTypes.NamedWith("Options", "Config"))
            .ShouldMatchRoster(KnownUnread);
}
