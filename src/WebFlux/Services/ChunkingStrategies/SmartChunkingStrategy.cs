using FluxCurator.Core.Core;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using CuratorStrategy = FluxCurator.Core.Domain.ChunkingStrategy;
using ChunkingOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 구조 인식 청킹 전략 - HTML/Markdown 헤딩 경계를 우선 보존한다.
///
/// <para>
/// Heading detection is what this strategy is for and stays here: it reads the headings the
/// extractor found on the page, which is web knowledge FluxCurator has no reason to hold. What does
/// NOT stay here is deciding whether a section is too large. That decision used to compare
/// <c>ChunkingOptions.MaxChunkSize</c> — documented as a token count — against
/// <c>string.Length</c>, so a caller declaring 512 got sections cut at 512 characters, and the size
/// of the error varied by language. Sizing is now delegated: this strategy answers "where are the
/// seams", FluxCurator answers "is this too big".
/// </para>
/// </summary>
public class SmartChunkingStrategy : BaseChunkingStrategy
{
    private static readonly string[] ParagraphSplitSeparators = ["\n\n", "\r\n\r\n"];

    private readonly IChunkerFactory _chunkerFactory;

    public override string Name => "Smart";
    public override string Description => "구조 인식 청킹 - HTML/Markdown 헤더 기반 맥락 보존";

    /// <param name="chunkerFactory">
    /// FluxCurator chunker factory, used to split any section that exceeds the declared size.
    /// Required rather than optional: without it this strategy would fall back to measuring
    /// characters, which is the defect it exists to not have.
    /// </param>
    /// <param name="eventPublisher">Optional event publisher.</param>
    public SmartChunkingStrategy(IChunkerFactory chunkerFactory, IEventPublisher? eventPublisher = null)
        : base(eventPublisher)
    {
        _chunkerFactory = chunkerFactory ?? throw new ArgumentNullException(nameof(chunkerFactory));
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

        // Sections come from page structure where the page has any, and from paragraph breaks where
        // it does not. Either way they are candidate seams, not final chunks.
        var sections = content.Headings?.Count > 0
            ? SplitAtHeadings(text)
            : text.Split(ParagraphSplitSeparators, StringSplitOptions.RemoveEmptyEntries)
                  .Select(p => p.Trim())
                  .Where(p => !string.IsNullOrWhiteSpace(p))
                  .ToList();

        return await SizeSectionsAsync(sections, options, sourceUrl, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Splits the text at heading lines. Every heading opens a section; whether that section is
    /// then too large is not decided here.
    /// </summary>
    /// <remarks>
    /// This used to open a new section only once the accumulated text already exceeded the size,
    /// which conflated "there is a seam here" with "we have enough text" — headings inside a short
    /// document were ignored entirely and the strategy degenerated into paragraph splitting under
    /// a name that promised structure awareness.
    /// </remarks>
    private static List<string> SplitAtHeadings(string text)
    {
        var sections = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var line in text.Split('\n'))
        {
            if (IsHeading(line) && current.Length > 0)
            {
                var section = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(section))
                {
                    sections.Add(section);
                }
                current.Clear();
            }

            current.Append(line).Append('\n');
        }

        var last = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(last))
        {
            sections.Add(last);
        }

        return sections;
    }

    /// <summary>
    /// Emits each section as a chunk, handing any section over the declared size to FluxCurator to
    /// be split further. Sections that already fit are passed through whole — which is the point of
    /// detecting seams in the first place.
    /// </summary>
    private async Task<IReadOnlyList<WebContentChunk>> SizeSectionsAsync(
        List<string> sections,
        ChunkingOptions? options,
        string sourceUrl,
        CancellationToken cancellationToken)
    {
        var curatorOptions = FluxCuratorChunkAdapter.ToCuratorOptions(options, CuratorStrategy.Paragraph);

        if (!_chunkerFactory.TryCreateChunker(CuratorStrategy.Paragraph, out var chunker) || chunker is null)
        {
            throw new InvalidOperationException(
                "The 'Smart' chunking strategy sizes its sections with FluxCurator's Paragraph chunker, " +
                "which is not available in this configuration. Strategies currently available: " +
                $"{string.Join(", ", _chunkerFactory.AvailableStrategies)}.");
        }

        var chunks = new List<WebContentChunk>();
        var sequenceNumber = 0;

        foreach (var section in sections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // "Would this be split?" is asked of the component that owns the units, rather than
            // re-derived here in whatever unit happened to be convenient.
            if (chunker.EstimateChunkCount(section, curatorOptions) <= 1)
            {
                chunks.Add(CreateChunk(section, sequenceNumber++, sourceUrl));
                continue;
            }

            var split = await chunker.ChunkAsync(section, curatorOptions, cancellationToken).ConfigureAwait(false);
            foreach (var piece in split)
            {
                chunks.Add(CreateChunk(piece.Content, sequenceNumber++, sourceUrl));
            }
        }

        return chunks;
    }

    /// <summary>
    /// 라인이 헤딩인지 판단
    /// </summary>
    private static bool IsHeading(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith('#') || // Markdown 헤딩
               trimmed.StartsWith("<h", StringComparison.Ordinal) || // HTML 헤딩
               (trimmed.Length > 0 && trimmed.Length < 100 && !trimmed.Contains('.')); // 짧은 제목 라인
    }
}
