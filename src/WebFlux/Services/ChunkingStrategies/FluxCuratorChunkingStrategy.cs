using System.Diagnostics;
using FluxCurator.Core.Core;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using CuratorStrategy = FluxCurator.Core.Domain.ChunkingStrategy;
using ChunkingOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// A WebFlux chunking strategy that delegates the actual splitting to FluxCurator.
///
/// <para>
/// WebFlux used to carry its own paragraph, fixed-size and semantic chunkers. They were reduced
/// re-implementations of chunkers FluxCurator already owns, and the reduction is where the defects
/// lived — a size contract measured in the wrong unit, an overlap setting no strategy read, and a
/// "semantic" strategy that never called an embedder. None of those are visible as failures: they
/// return chunks, just not the chunks that were asked for. Whichever library ends up authoritative,
/// keeping two implementations of one convention means fixing either one silently leaves the other
/// wrong, which is why the answer is one implementation rather than a better second one.
/// </para>
///
/// <para>
/// Web-specific chunking stays in WebFlux: <see cref="DomStructureChunkingStrategy"/> splits on
/// HTML structure, which is not text chunking and has no FluxCurator counterpart. The boundary is
/// "does this need to know it came from a web page", not "is this chunking".
/// </para>
/// </summary>
public sealed class FluxCuratorChunkingStrategy : BaseChunkingStrategy
{
    private readonly IChunkerFactory _chunkerFactory;
    private readonly CuratorStrategy _strategy;

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override string Description { get; }

    /// <summary>
    /// Splits on paragraph boundaries. Replaces WebFlux's own paragraph strategy.
    /// </summary>
    public static FluxCuratorChunkingStrategy Paragraph(IChunkerFactory factory, IEventPublisher? events = null) =>
        new(factory, CuratorStrategy.Paragraph, "Paragraph",
            "문단 기반 청킹 - 자연스러운 텍스트 경계 보존", events);

    /// <summary>
    /// Splits to a consistent size. Replaces WebFlux's own fixed-size strategy, which split on
    /// character count under a name that promised tokens.
    /// </summary>
    public static FluxCuratorChunkingStrategy FixedSize(IChunkerFactory factory, IEventPublisher? events = null) =>
        new(factory, CuratorStrategy.Token, "FixedSize",
            "고정 크기 기반 청킹 - 단순하고 예측 가능한 분할", events);

    /// <summary>
    /// Splits on semantic similarity. Replaces WebFlux's own semantic strategy, which never called
    /// an embedder on either of its branches.
    /// </summary>
    public static FluxCuratorChunkingStrategy Semantic(IChunkerFactory factory, IEventPublisher? events = null) =>
        new(factory, CuratorStrategy.Semantic, "Semantic",
            "의미론적 청킹 - 임베딩 기반 의미적 일관성 최적화 (임베딩 서비스 필요)", events);

    /// <summary>
    /// Splits to a consistent size, formerly "memory optimized".
    /// </summary>
    /// <remarks>
    /// The strategy this replaces claimed streaming and did not stream: it received the whole text
    /// as a string and sliced it, so the only behaviour distinguishing it from fixed-size chunking
    /// was that it measured the slices in characters — the defect, not a feature. It also called
    /// <c>GC.Collect</c> every hundred chunks, which takes a decision that belongs to the host
    /// process and costs throughput to no measured end. The name is kept because consumers select
    /// strategies by string, and removing it would read as a capability being withdrawn.
    /// </remarks>
    public static FluxCuratorChunkingStrategy MemoryOptimized(IChunkerFactory factory, IEventPublisher? events = null) =>
        new(factory, CuratorStrategy.Token, "MemoryOptimized",
            "대용량 문서용 일관 크기 청킹 - 토큰 기준", events);

    /// <param name="chunkerFactory">FluxCurator chunker factory.</param>
    /// <param name="strategy">The FluxCurator strategy this instance delegates to.</param>
    /// <param name="name">The WebFlux-facing strategy name (unchanged from before delegation).</param>
    /// <param name="description">The WebFlux-facing description.</param>
    /// <param name="eventPublisher">Optional event publisher.</param>
    public FluxCuratorChunkingStrategy(
        IChunkerFactory chunkerFactory,
        CuratorStrategy strategy,
        string name,
        string description,
        IEventPublisher? eventPublisher = null)
        : base(eventPublisher)
    {
        _chunkerFactory = chunkerFactory ?? throw new ArgumentNullException(nameof(chunkerFactory));
        _strategy = strategy;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var text = content.MainContent ?? content.Text ?? string.Empty;
        var sourceUrl = content.Url ?? content.OriginalUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        // A strategy whose dependency is unavailable must say so rather than quietly produce
        // something else. The semantic strategy used to fall back to paragraph splitting when no
        // embedder was registered, so a caller asking for semantic chunking received paragraph
        // chunking labelled "Semantic" -- the result was plausible and the request was not honoured.
        if (!_chunkerFactory.TryCreateChunker(_strategy, out var chunker) || chunker is null)
        {
            throw new InvalidOperationException(
                $"The '{Name}' chunking strategy needs FluxCurator's {_strategy} chunker, which is not " +
                $"available in this configuration. Strategies currently available: " +
                $"{string.Join(", ", _chunkerFactory.AvailableStrategies)}. " +
                (chunkerRequiresEmbedder(_strategy)
                    ? "This strategy requires an embedder; register one, or choose a strategy that does not."
                    : "Register the FluxCurator services this strategy depends on."));
        }

        var curatorOptions = FluxCuratorChunkAdapter.ToCuratorOptions(options, _strategy);

        var stopwatch = Stopwatch.StartNew();
        var chunks = await chunker.ChunkAsync(text, curatorOptions, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return FluxCuratorChunkAdapter.ToWebContentChunks(
            chunks, sourceUrl, Name, stopwatch.ElapsedMilliseconds);

        static bool chunkerRequiresEmbedder(CuratorStrategy strategy) => strategy == CuratorStrategy.Semantic;
    }
}
