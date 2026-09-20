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
        Assembly.Load("WebFlux.Playwright"),
    ];

    /// <summary>
    /// Options accepted as unread today. Shrink this list; never grow it silently.
    /// <para>
    /// Opening baseline (2026-09-20): 133 unread public options across 25 types, recorded as found rather than
    /// as judged - none has been investigated, so none carries a reason of its own. Recording them is what makes
    /// the gate start green and makes the *next* unread option a failure instead of silently joining a crowd.
    /// </para>
    /// <para>
    /// The assembly list above must cover every assembly this repository ships. Scanning only the main one
    /// reports options that a sibling assembly reads as unread - that mistake inflated an early baseline elsewhere threefold.
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
        // Was 19. Two were wired (FollowExternalLinks, AllowedDomains - the host scope was hardcoded
        // and both options were read by nothing, so FollowExternalLinks = true was silently ignored).
        // Eleven were removed: five described a feature that does not exist anywhere in src/
        // (StartUrls - every entry point takes the URL positionally, so no code path could consult
        // it; PriorityUrls - the frontier is a plain FIFO; DownloadImages and MaxImageSizeBytes -
        // nothing fetches image bytes, the multimodal port takes a URL; AllowedContentTypes - there
        // is no content-type gate, ExcludedExtensions is the filter), two configured a cache that
        // only exists on the ExtractOptions path (UseCache, CacheExpirationMinutes), and four were a
        // second spelling of a knob already wired on this same class (Timeout vs TimeoutMs,
        // MaxConcurrency vs ConcurrentRequests, DelayBetweenRequests vs DelayMs, Headers vs
        // CustomHeaders).
        //
        // The six below are the residue, and each has a reason rather than a shrug:
        // - CustomHeaders: nothing sends request headers yet. Wiring it needs a decision between a
        //   per-request HttpRequestMessage and mutating the shared HttpClient's defaults (which
        //   leaks across concurrent crawls). Said so in its XML doc rather than leaving the promise.
        // - The five metadata options: AIWebMetadataExtractor and HtmlMetadataExtractor implement
        //   all of this, but neither is DI-registered and neither has a call site, so the subsystem
        //   has no front door. Wiring it is a feature decision, not a knob decision.
        ["WebFlux.Core.Options.CrawlOptions"] =
        [
            "CustomHeaders", "CustomMetadataPrompt", "EnableMetadataExtraction",
            "MetadataExtractionMaxChars", "MetadataSchema", "UseHtmlMetadata",
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
