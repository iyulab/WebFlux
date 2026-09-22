using AwesomeAssertions;
using Flux.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.AiEnhancement;
using WebFlux.Services.ChunkingStrategies;
using Xunit;

namespace WebFlux.Tests.Services.AiEnhancement;

/// <summary>
/// Options that were declared and read by nothing until 0.14.0 — each fact runs both ways so a literal that
/// happens to match the default cannot pass it.
/// </summary>
public sealed class EnhancementOptionWiringTests
{
    private static (BasicAiEnhancementService Service, List<string> Prompts) Recording()
    {
        var prompts = new List<string>();
        var llm = Substitute.For<ITextCompletionService>();
        llm.CompleteAsync(Arg.Do<string>(prompts.Add), Arg.Any<Flux.Abstractions.TextCompletionOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("answer"));
        return (new BasicAiEnhancementService(llm, NullLogger<BasicAiEnhancementService>.Instance), prompts);
    }

    [Fact]
    public async Task SummaryOptions_TargetLanguage_and_FocusOnKeyPoints_reach_the_summary_prompt()
    {
        var (service, prompts) = Recording();

        await service.SummarizeAsync("content", new SummaryOptions { TargetLanguage = "ko", FocusOnKeyPoints = true }, TestContext.Current.CancellationToken);
        await service.SummarizeAsync("content", new SummaryOptions { TargetLanguage = null, FocusOnKeyPoints = false }, TestContext.Current.CancellationToken);

        prompts.Should().HaveCount(2);
        prompts[0].Should().Contain("language 'ko'").And.Contain("Keep only the key points");
        prompts[1].Should().NotContain("language '").And.NotContain("Keep only the key points");
    }

    [Fact]
    public async Task RewriteOptions_AddExamples_reaches_the_rewrite_prompt()
    {
        var (service, prompts) = Recording();

        await service.RewriteAsync("content", new RewriteOptions { AddExamples = true }, TestContext.Current.CancellationToken);
        await service.RewriteAsync("content", new RewriteOptions { AddExamples = false }, TestContext.Current.CancellationToken);

        prompts[0].Should().Contain("concrete example");
        prompts[1].Should().NotContain("concrete example");
    }

    [Fact]
    public void The_crawl_path_maps_every_enhancement_setting_from_the_configuration()
    {
        var summary = new SummaryOptions { TargetLanguage = "de" };
        var rewrite = new RewriteOptions { AddExamples = true };
        var configuration = new AiEnhancementConfiguration
        {
            EnableSummary = false, EnableMetadata = false, EnableRewrite = true, EnableParallelProcessing = false,
            TimeoutMs = 1234, Summary = summary, Rewrite = rewrite,
        };

        var options = WebContentProcessor.ToEnhancementOptions(configuration);

        options.EnableSummary.Should().BeFalse();
        options.EnableMetadata.Should().BeFalse();
        options.EnableRewrite.Should().BeTrue("EnableRewrite was hard-coded to false on this path before 0.14.0");
        options.EnableParallelProcessing.Should().BeFalse();
        options.TimeoutMs.Should().Be(1234);
        options.SummaryOptions.Should().BeSameAs(summary);
        options.RewriteOptions.Should().BeSameAs(rewrite);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ChunkingOptions_PreserveHeaders_reaches_FluxCurators_PreserveSectionHeaders(bool preserve)
    {
        var curator = FluxCuratorChunkAdapter.ToCuratorOptions(new ChunkingOptions { PreserveHeaders = preserve }, FluxCurator.Core.Domain.ChunkingStrategy.Hierarchical);

        curator.PreserveSectionHeaders.Should().Be(preserve);
    }
}
