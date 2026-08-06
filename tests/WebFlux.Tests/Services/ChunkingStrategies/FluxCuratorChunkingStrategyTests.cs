using FluentAssertions;
using FluxCurator.Core.Core;
using WebFlux.Core.Models;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using CuratorChunk = FluxCurator.Core.Domain.DocumentChunk;
using CuratorOptions = FluxCurator.Core.Domain.ChunkOptions;
using CuratorStrategy = FluxCurator.Core.Domain.ChunkingStrategy;
using ChunkingOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// Chunking is delegated to FluxCurator rather than re-implemented here. These tests assert the
/// guarantees the previous in-repo strategies broke — each one corresponds to a measured defect,
/// not to a property of the implementation that happens to be in place.
///
/// <para>
/// The tests they replace asserted implementation details of the removed strategies, including
/// <c>chunk.Content.Length &lt;= 1200</c> for a declared size of 1000. That assertion is only
/// satisfiable if the size is a CHARACTER count — so the suite was pinning the unit-contract
/// violation as the contract. Deleting them is the point, not collateral.
/// </para>
/// </summary>
public class FluxCuratorChunkingStrategyTests
{
    /// <summary>Captures the options handed to FluxCurator so the mapping can be asserted directly.</summary>
    private sealed class CapturingChunker : IChunker
    {
        public CuratorOptions? Received { get; private set; }
        public string? ReceivedText { get; private set; }

        public string StrategyName => "capturing";
        public bool RequiresEmbedder => false;

        public Task<IReadOnlyList<CuratorChunk>> ChunkAsync(
            string text, CuratorOptions options, CancellationToken cancellationToken = default)
        {
            Received = options;
            ReceivedText = text;
            return Task.FromResult<IReadOnlyList<CuratorChunk>>(
            [
                new CuratorChunk { Id = "c0", Content = text, ChunkIndex = 0, TotalChunks = 1 }
            ]);
        }

        public int EstimateChunkCount(string text, CuratorOptions options) => 1;
    }

    private sealed class StubChunkerFactory : IChunkerFactory
    {
        private readonly IChunker? _chunker;
        public StubChunkerFactory(IChunker? chunker) => _chunker = chunker;

        public IChunker CreateChunker(CuratorStrategy strategy) =>
            _chunker ?? throw new ArgumentException("unavailable", nameof(strategy));

        public bool TryCreateChunker(CuratorStrategy strategy, out IChunker? chunker)
        {
            chunker = _chunker;
            return _chunker is not null;
        }

        public IReadOnlyCollection<CuratorStrategy> AvailableStrategies =>
            _chunker is null ? [] : [CuratorStrategy.Paragraph];

        public bool IsStrategyAvailable(CuratorStrategy strategy) => _chunker is not null;

        public IChunker DefaultChunker => _chunker ?? throw new InvalidOperationException("none");
    }

    private static ExtractedContent Content(string text) => new()
    {
        Text = text,
        MainContent = text,
        Url = "https://example.com"
    };

    [Fact]
    public async Task DeclaredChunkSize_ReachesTheChunkerAsItsTargetSize()
    {
        // MaxChunkSize is documented as a token count. It used to be compared against
        // string.Length, so a caller declaring 512 received 512 characters — an error whose size
        // is a characters-per-token ratio and therefore varies by language, which is why a
        // consumer could not compensate for it either.
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(Content("Some prose to split."), new ChunkingOptions { MaxChunkSize = 512 });

        chunker.Received.Should().NotBeNull();
        chunker.Received!.TargetChunkSize.Should().Be(512);
    }

    [Fact]
    public async Task DeclaredChunkSize_IsAlsoTheCeiling_NotJustTheTarget()
    {
        // FluxCurator's own MaxChunkSize defaults to 1024 and means "split above this". Mapping
        // name-to-name would have let a caller asking for 512 receive chunks up to 1024.
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(Content("Some prose."), new ChunkingOptions { MaxChunkSize = 300 });

        chunker.Received!.MaxChunkSize.Should().Be(300);
    }

    [Fact]
    public async Task ChunkOverlap_ReachesTheChunker()
    {
        // Dead config before this: none of the nine strategies referenced ChunkOverlap, so a
        // caller setting 50 got no overlap and no indication the setting was inert.
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(Content("Some prose."), new ChunkingOptions { ChunkOverlap = 50 });

        chunker.Received!.OverlapSize.Should().Be(50);
    }

    [Fact]
    public async Task Overlap_CannotBeSetAtOrAboveTheChunkSize()
    {
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(
            Content("Some prose."), new ChunkingOptions { MaxChunkSize = 100, ChunkOverlap = 500 });

        chunker.Received!.OverlapSize.Should().BeLessThan(chunker.Received.TargetChunkSize);
    }

    [Fact]
    public async Task MinChunkSize_IsKeptBelowTheTarget()
    {
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(
            Content("Some prose."), new ChunkingOptions { MaxChunkSize = 100, MinChunkSize = 400 });

        chunker.Received!.MinChunkSize.Should().BeLessThan(chunker.Received.TargetChunkSize);
    }

    [Fact]
    public async Task UnsetLanguage_IsLeftForDetection_RatherThanDefaultingToKorean()
    {
        // ChunkingOptions.Language defaults to "ko" — a value nobody chose. Token sizes are
        // estimated per language, so passing that default through would size an English page on
        // Korean ratios and call it the caller's choice.
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(Content("Some prose."), new ChunkingOptions { Language = "  " });

        chunker.Received!.LanguageCode.Should().BeNull();
    }

    [Fact]
    public async Task ExplicitLanguage_IsPassedThrough()
    {
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        await strategy.ChunkAsync(Content("Some prose."), new ChunkingOptions { Language = "en" });

        chunker.Received!.LanguageCode.Should().Be("en");
    }

    [Fact]
    public async Task SemanticWithoutItsChunker_Throws_RatherThanQuietlyChunkingSomeOtherWay()
    {
        // The removed semantic strategy fell back to paragraph splitting when no embedder was
        // registered, and still labelled its output "Semantic". The caller received plausible
        // chunks and an unhonoured request, which is the failure mode with no symptom.
        var strategy = FluxCuratorChunkingStrategy.Semantic(new StubChunkerFactory(null));

        var act = async () => await strategy.ChunkAsync(Content("Some prose."));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("Semantic");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyContent_YieldsNoChunks(string text)
    {
        var chunker = new CapturingChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(chunker));

        var chunks = await strategy.ChunkAsync(Content(text));

        chunks.Should().BeEmpty();
        chunker.Received.Should().BeNull("an empty document must not reach the chunker at all");
    }

    [Fact]
    public async Task ChunkOrdering_ComesFromTheChunker_NotFromTheLoopIndex()
    {
        // Re-deriving SequenceNumber from the enumeration order would mask a chunker that returned
        // chunks out of order — the numbers would look correct while the content was misordered.
        var outOfOrder = new OutOfOrderChunker();
        var strategy = FluxCuratorChunkingStrategy.Paragraph(new StubChunkerFactory(outOfOrder));

        var chunks = await strategy.ChunkAsync(Content("a b c"));

        chunks.Select(c => c.SequenceNumber).Should().Equal(7, 3);
    }

    private sealed class OutOfOrderChunker : IChunker
    {
        public string StrategyName => "out-of-order";
        public bool RequiresEmbedder => false;

        public Task<IReadOnlyList<CuratorChunk>> ChunkAsync(
            string text, CuratorOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CuratorChunk>>(
            [
                new CuratorChunk { Id = "x", Content = "second", ChunkIndex = 7, TotalChunks = 2 },
                new CuratorChunk { Id = "y", Content = "first", ChunkIndex = 3, TotalChunks = 2 }
            ]);

        public int EstimateChunkCount(string text, CuratorOptions options) => 2;
    }

    [Theory]
    [InlineData("FixedSize")]
    [InlineData("Paragraph")]
    [InlineData("Semantic")]
    public void StrategyNames_AreUnchangedByTheDelegation(string expected)
    {
        // Consumers select a strategy by string. Renaming one during this migration would read as
        // "the strategy was removed" rather than "its implementation moved".
        var factory = new StubChunkerFactory(new CapturingChunker());

        var strategy = expected switch
        {
            "FixedSize" => FluxCuratorChunkingStrategy.FixedSize(factory),
            "Paragraph" => FluxCuratorChunkingStrategy.Paragraph(factory),
            _ => FluxCuratorChunkingStrategy.Semantic(factory)
        };

        strategy.Name.Should().Be(expected);
        strategy.Description.Should().NotBeNullOrWhiteSpace();
    }
}
