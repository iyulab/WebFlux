using Iyu.Conventions.Testing;
using Xunit;

namespace WebFlux.Tests;

/// <summary>
/// The public surface follows the two API rules of the ecosystem: every public async method takes a
/// <see cref="CancellationToken"/>, and failure is reported by an exception rather than by a returned object carrying a
/// success flag and an error. The scans are <c>Iyu.Conventions.Testing</c>'s, over the same assemblies as the
/// operational-language scan.
/// </summary>
/// <remarks>
/// The rosters are the methods that break a rule today. Shrink them; never grow them silently. A change to a listed
/// method's parameters changes its entry, which is a roster change on purpose.
/// </remarks>
public class PublicApiConventionTests
{
    private static readonly string[] KnownUncancellable =
    [
        "WebFlux.Core.Interfaces.IChunkingStrategyFactory.CreateStrategyAsync(String)",
        "WebFlux.Core.Interfaces.IChunkingStrategyFactory.GetStrategyInfoAsync(String)",
        "WebFlux.Core.Interfaces.IContentRelationshipMapper.AnalyzeNavigationStructureAsync(ContentRelationshipAnalysisResult)",
        "WebFlux.Core.Interfaces.IContentRelationshipMapper.BuildContentHierarchyAsync(ContentRelationshipAnalysisResult)",
        "WebFlux.Core.Interfaces.IContentRelationshipMapper.GenerateRelatedContentAsync(String, ContentRelationshipAnalysisResult)",
        "WebFlux.Core.Interfaces.IContentRelationshipMapper.PerformContentClusteringAsync(ContentRelationshipAnalysisResult)",
        "WebFlux.Core.Interfaces.ICrawler.IsUrlAllowedAsync(String, String)",
        "WebFlux.Core.Interfaces.IPackageEcosystemAnalyzer.AnalyzeSecurityRisksAsync(PackageMetadata)",
        "WebFlux.Core.Interfaces.IPackageEcosystemAnalyzer.AnalyzeTechStackAsync(PackageMetadata)",
        "WebFlux.Core.Interfaces.IPackageEcosystemAnalyzer.EvaluateProjectComplexityAsync(PackageMetadata)",
        "WebFlux.Core.Interfaces.IPerformanceMonitor.GetStatisticsAsync()",
        "WebFlux.Core.Interfaces.IProcessingOptimizationService.AnalyzeResourceUsageAsync()",
        "WebFlux.Core.Interfaces.IProcessingOptimizationService.GetOptimizationStatisticsAsync()",
        "WebFlux.Core.Interfaces.IProcessingOptimizationService.OptimizeBottlenecksAsync(PipelineMetrics)",
        "WebFlux.Core.Interfaces.IProcessingOptimizationService.OptimizeCacheUsageAsync(String, String, Nullable<TimeSpan>)",
        "WebFlux.Core.Interfaces.IProcessingOptimizationService.OptimizeStrategyAsync(String, PerformanceStatistics)",
        "WebFlux.Core.Interfaces.IProcessingOptimizationService.OptimizeTokenUsageAsync(String, Int32)",
        "WebFlux.Core.Interfaces.IResilienceService.SetCircuitBreakerStateAsync(String, Boolean)",
        "WebFlux.Core.Interfaces.IRobotsTxtParser.ParseContentAsync(String, String)",
    ];

    private static readonly string[] KnownResultReturns =
    [
        "WebFlux.Core.Interfaces.ICrawler.CrawlAsync(String, CrawlOptions, CancellationToken)",
        "WebFlux.Core.Interfaces.IRobotsTxtParser.ParseFromWebsiteAsync(String, CancellationToken)",
        "WebFlux.Services.Crawlers.BaseCrawler.CreateDisallowedByRobotsResult(String)",
    ];

    [Fact]
    public void PublicAsyncMethods_TakeACancellationToken() =>
        AsyncCancellation.Scan(OptionsReachabilityRosterTests.Libraries).ShouldMatchRoster(KnownUncancellable);

    [Fact]
    public void PublicMethods_DoNotReturnResultObjects() =>
        ResultReturns.Scan(OptionsReachabilityRosterTests.Libraries).ShouldMatchRoster(KnownResultReturns);

    // Positive control: an empty roster would also pass if the scan saw no public method at all.
    [Fact]
    public void Scan_SeesThePublicSurface() =>
        Assert.True(ResultReturns.Scan(OptionsReachabilityRosterTests.Libraries).MembersRead > 0, "the scan read too few public methods");
}
