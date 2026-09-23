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
        // MarkdownConversionOptions: all eleven remaining members wired in 0.14.0 (per-call Markdig pipeline, TOC/image gates,
        // syntactic link validation); EnableCodeHighlighting and CustomSettings removed in 0.14.0.
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
        // RewriteOptions.AddExamples and SummaryOptions.TargetLanguage/FocusOnKeyPoints wired in 0.14.0 (prompt
        // instructions); ChunkingOptions.PreserveHeaders wired in 0.14.0 (FluxCurator PreserveSectionHeaders).
        // Full 114-member classification (T 73 · D 24 · A 13 · B 2 · C 2): umbrella draft ISSUE-webflux-20260922-213000.
        // Types removed whole in 0.14.0 (no library method received them, no consumer): WebFluxOptions (+Builder — a duplicate of
        // WebFluxConfiguration, which DI binds), AnalysisOptions (+IContentAnalyzer, no implementation), PipelineOptions (+PipelineStage,
        // ExtractionOptions — referenced by nothing).
        // Removed in 0.14.0 (read by nothing; migration in CHANGELOG): ChunkingOptions.ChunkSize (alias of MaxChunkSize),
        // UseMemoryOptimization (MinimizeMemoryUsage is the value), EnableParallelProcessing (Performance.MaxDegreeOfParallelism),
        // UseStreaming (the IAsyncEnumerable ProcessAsync overloads are the streaming shape), CreateHierarchy, CustomSeparators,
        // SplitCodeBlocks, SplitTables, IncludeMetadata; ExtractOptions.IncludeLinks; MarkdownConversionOptions.EnableCodeHighlighting,
        // CustomSettings; ReconstructOptions.AdditionalOptions; TextCompletionOptions.AdditionalProperties.
        // cycle-934 (2026-09-23, owner decision «dead subsystems: remove»): removed whole in 0.14.0 because nothing registered,
        // constructed or called them — only their own tests did: the site-configuration analyzer (ISiteConfigurationAnalyzer,
        // SiteConfigurationAnalyzer and its model tree — SiteConfiguration, Build/Content/Deployment/Plugin/Seo/SitePerformance
        // configuration, the nine *Config leaves), the reconstruct subsystem (IContentReconstructor, IReconstructStrategy[Factory],
        // ReconstructStrategyFactory, five strategies, ReconstructOptions, ReconstructedContent), DomStructureChunkingStrategy with
        // HtmlChunkingOptions, and ChunkingOptions.StrategySpecificOptions (its only reader was DomStructure). ChunkingStrategyType.Intelligent
        // had no registered strategy and silently became Paragraph; it is removed and an unknown strategy name now throws.
        // cycle-935 (2026-09-23, same owner decision): the multimodal subsystem removed in 0.14.0 — IImageToTextService (a port the
        // library registered helpers for and never called), IMultimodalProcessingPipeline (no implementation), their result models,
        // ImageToTextOptions, MultimodalProcessingOptions, AddWebFluxMultimodal (registered nothing), and the members that pointed at
        // them: ChunkingOptions.IncludeImageDescriptions/EnableMultimodalProcessing/MultimodalOptions and the ChunkingConfiguration twins.
        // cycle-933 (2026-09-22) widened the scan from *Options/*Config to *Configuration as well: NamedWith is EndsWith, so
        // WebFluxConfiguration and its 19 nested configuration types were never scanned. What it found — 112 members in
        // 20 types — is recorded here UNCLASSIFIED (no per-member verdict yet): the gate starts green at the honest number and the
        // next unread configuration member fails it. Classification (A/B/C/D/T, umbrella draft ISSUE-webflux-20260922-213000
        // and its successor) decides wire vs remove; several of these types (Build/Content/Deployment/Plugin/Seo) look like
        // static-site-generator configuration the library never consumes.
        // 0.15.0: classified (T 57 · A 15 · D 12 · C 3 · B 2, umbrella draft ISSUE-webflux-20260922-213000). The processor now
        // reads the registered WebFluxConfiguration: the crawler settings of CrawlingConfiguration and the size/overlap/min of
        // ChunkingConfiguration left this list, and the three ChunkingConfiguration twins were removed. The rest waits on the
        // removal decision.
        ["WebFlux.Core.Models.AiEnhancementConfiguration"] = ["MaxRetries"],
        ["WebFlux.Core.Models.AutoChunkingConfiguration"] = ["HighComplexityThreshold", "MediumComplexityThreshold"],
        ["WebFlux.Core.Models.CachingConfiguration"] = ["DefaultExpirationMinutes", "EnableCompression", "EnableMetrics", "Enabled", "MaxCacheSize", "TypeSettings"],
        ["WebFlux.Core.Models.ChunkingConfiguration"] = ["DefaultQualityThreshold", "DefaultSemanticThreshold", "LanguageSettings", "NormalizeWhitespace", "StrategyDefaults"],
        ["WebFlux.Core.Models.CrawlConfiguration"] = ["AllowedDomains", "DelayBetweenRequests", "ExcludePatterns", "MaxConcurrentRequests", "MaxDepth", "MaxPages", "StartUrls", "Strategy"],
        ["WebFlux.Core.Models.CrawlingConfiguration"] = ["DefaultAllowedContentTypes"],
        ["WebFlux.Core.Models.EventConfiguration"] = ["EnableEventPublishing", "EventBatchSize", "EventBufferSize", "EventFilters", "EventTypeEnabled", "FlushIntervalMs"],
        ["WebFlux.Core.Models.ExtractionConfiguration"] = ["IncludeLinkUrls"],
        ["WebFlux.Core.Models.LoggingConfiguration"] = ["CategoryLevels", "EnableDetailedErrorLogging", "EnableEvents", "EnablePerformanceLogging", "EnableStructuredLogging", "LogFilters", "MinimumLevel"],
        ["WebFlux.Core.Models.PerformanceConfiguration"] = ["BackpressureThreshold", "BatchSize", "EnableAutoScaling", "MaxMemoryUsageBytes", "MemoryOptimizationThreshold", "PerformanceMonitoringIntervalMs", "QueueSizeLimit"],
        ["WebFlux.Core.Models.ProcessingOptimizationConfiguration"] = ["CacheOptimization", "EnableAutoStrategySelection", "EnableBottleneckDetection", "EnableStatisticsCollection", "EnableTokenOptimization", "Enabled", "PerformanceMonitoringInterval", "ResourceThresholds"],
        ["WebFlux.Core.Models.SecurityConfiguration"] = ["AllowedDomains", "BlockedDomains", "EnableContentScanning", "EncryptApiKeys", "RateLimitPerMinute", "ValidateSslCertificates", "ValidateUserAgent"],
        ["WebFlux.Core.Models.TokenCountingConfiguration"] = ["CacheMaxSize", "EnableCaching", "EnableStatistics", "EnableTokenAnalysis", "Enabled", "ModelCosts", "SupportedModels"],
        ["WebFlux.Core.Models.WebFluxConfiguration"] = ["Caching", "CustomSettings", "DefaultTokenizerModel", "EnvironmentOverrides", "Events", "Extraction", "Logging", "ProcessingOptimization", "Security", "TokenCounting"],
    };

    [Fact]
    public void EveryPublicOption_IsRead() =>
        OptionsReachability.Scan(Libraries, OptionsTypes.NamedWith("Options", "Config", "Configuration"))
            .ShouldMatchRoster(KnownUnread);
}
