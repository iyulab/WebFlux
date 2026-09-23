using AwesomeAssertions;
using Flux.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WebFlux.Core.Options;
using WebFlux.Services.AiEnhancement;
using Xunit;

namespace WebFlux.Tests.Services.AiEnhancement;

/// <summary>
/// A rewrite replaces the content, so a rewrite cut off at the output token limit must not stand in for it.
/// </summary>
public sealed class RewriteTruncationTests
{
    [Fact]
    public async Task RewriteAsync_asks_the_completion_port_to_report_truncation()
    {
        Flux.Abstractions.TextCompletionOptions? sent = null;
        var llm = Substitute.For<ITextCompletionService>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Do<Flux.Abstractions.TextCompletionOptions?>(o => sent = o), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("rewritten"));

        await new BasicAiEnhancementService(llm, NullLogger<BasicAiEnhancementService>.Instance)
            .RewriteAsync("content", cancellationToken: TestContext.Current.CancellationToken);

        sent!.ThrowOnTruncation.Should().BeTrue();
    }

    [Fact]
    public async Task EnhanceAsync_keeps_the_original_when_the_rewrite_is_truncated()
    {
        var llm = Substitute.For<ITextCompletionService>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Is<Flux.Abstractions.TextCompletionOptions?>(o => o != null && o.ThrowOnTruncation), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new TextCompletionTruncatedException(4000));
        llm.CompleteAsync(Arg.Any<string>(), Arg.Is<Flux.Abstractions.TextCompletionOptions?>(o => o == null || !o.ThrowOnTruncation), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("summary"));
        var service = new BasicAiEnhancementService(llm, NullLogger<BasicAiEnhancementService>.Instance);

        var result = await service.EnhanceAsync(
            "original content",
            new EnhancementOptions { EnableSummary = true, EnableRewrite = true, EnableMetadata = false },
            TestContext.Current.CancellationToken);

        result.OriginalContent.Should().Be("original content");
        result.RewrittenContent.Should().BeNull();
        result.Summary.Should().Be("summary", "the other passes still complete");
    }
}
