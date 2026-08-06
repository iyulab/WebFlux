using WebFlux.Core.Models;
using CuratorChunk = FluxCurator.Core.Domain.DocumentChunk;
using CuratorOptions = FluxCurator.Core.Domain.ChunkOptions;
using WebFluxOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// Translates between WebFlux's chunking surface and FluxCurator's.
///
/// <para>
/// This is where the unit contract is actually honoured. <c>ChunkingOptions.MaxChunkSize</c> is
/// documented as a token count, and the strategies that used to implement chunking here compared it
/// against <c>string.Length</c> — so a caller declaring 512 got 512 <b>characters</b>. Because the
/// error is a characters-per-token ratio, its size varies by language: at the same declared 512,
/// the same English document came out as five chunks where a token-aware chunker produced one,
/// while Korean came out as two. A consumer could not correct for it either, since the correction
/// factor depended on the text.
/// </para>
///
/// <para>
/// FluxCurator's <c>ChunkOptions</c> documents its sizes as estimated tokens with per-language
/// ratios, which is the contract WebFlux was already advertising. Mapping onto it is therefore not
/// a behaviour change dressed as a refactor — it is the first time the declared contract holds.
/// </para>
/// </summary>
internal static class FluxCuratorChunkAdapter
{
    /// <summary>
    /// Maps WebFlux chunking options onto FluxCurator's.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MaxChunkSize</c> becomes <c>TargetChunkSize</c>, not <c>MaxChunkSize</c>. The two libraries
    /// mean different things by the word: WebFlux's is the size a caller is asking for, FluxCurator's
    /// is a hard ceiling above which a chunk is split. Mapping name-to-name would silently double the
    /// requested size, because FluxCurator's target would stay at its own default of 512 regardless
    /// of what the caller declared. The ceiling is set from the same value so a chunk never exceeds
    /// what the caller asked for.
    /// </para>
    /// <para>
    /// <c>ChunkOverlap</c> reaches <c>OverlapSize</c>. Nothing consumed it before — none of the nine
    /// strategies referenced it — so a caller setting 50 received chunks with no overlap at all and
    /// no indication that the setting was inert.
    /// </para>
    /// </remarks>
    internal static CuratorOptions ToCuratorOptions(WebFluxOptions? options, FluxCurator.Core.Domain.ChunkingStrategy strategy)
    {
        var source = options ?? new WebFluxOptions();

        // MinChunkSize must stay below the target or FluxCurator merges everything back together.
        // WebFlux defaults Min to 50 against a target of 512, so the clamp is inert in the common
        // case; it exists for callers who raise Min without lowering the target.
        var target = Math.Max(1, source.MaxChunkSize);
        var min = Math.Clamp(source.MinChunkSize, 1, Math.Max(1, target - 1));

        return new CuratorOptions
        {
            Strategy = strategy,
            TargetChunkSize = target,
            MaxChunkSize = target,
            MinChunkSize = min,
            OverlapSize = Math.Clamp(source.ChunkOverlap, 0, Math.Max(0, target - 1)),

            // WebFlux defaults Language to "ko" rather than leaving it unset, so an English page
            // chunked with defaults would be sized on Korean token ratios. Blank is FluxCurator's
            // "detect it", which is the honest reading of a value nobody chose.
            LanguageCode = string.IsNullOrWhiteSpace(source.Language) ? null : source.Language
        };
    }

    /// <summary>
    /// Converts FluxCurator chunks into WebFlux chunks, preserving the ordering FluxCurator produced.
    /// </summary>
    internal static IReadOnlyList<WebContentChunk> ToWebContentChunks(
        IReadOnlyList<CuratorChunk> chunks,
        string sourceUrl,
        string strategyName,
        long processingTimeMs)
    {
        var result = new List<WebContentChunk>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var source = chunks[i];

            result.Add(new WebContentChunk
            {
                Id = string.IsNullOrEmpty(source.Id) ? Guid.NewGuid().ToString() : source.Id,
                Content = source.Content,

                // FluxCurator's ChunkIndex is authoritative; it is the order the text was split in.
                // Re-deriving it from the loop would hide a chunker that returned them out of order.
                SequenceNumber = source.ChunkIndex,
                SourceUrl = sourceUrl,
                StrategyInfo = new ChunkingStrategyInfo
                {
                    StrategyName = strategyName,
                    ProcessingTimeMs = processingTimeMs
                },
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return result;
    }
}
