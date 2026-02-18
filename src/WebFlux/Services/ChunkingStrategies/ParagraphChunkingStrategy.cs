using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 문단 기반 청킹 전략 (단순화됨)
/// 문단 경계를 기준으로 텍스트를 분할
/// </summary>
public class ParagraphChunkingStrategy : BaseChunkingStrategy
{
    private static readonly string[] ParagraphSplitSeparators = ["\n\n", "\r\n\r\n"];

    public override string Name => "Paragraph";
    public override string Description => "문단 기반 청킹 - 자연스러운 텍스트 경계 보존";

    public ParagraphChunkingStrategy(IEventPublisher? eventPublisher = null)
        : base(eventPublisher)
    {
    }

    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // 동기 작업을 비동기로 래핑

        var text = content.MainContent ?? content.Text ?? string.Empty;
        var sourceUrl = content.Url ?? content.OriginalUrl ?? string.Empty;
        var maxChunkSize = options?.ChunkSize ?? 2000; // 기본 2000자

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        return SplitByParagraphs(text, maxChunkSize, sourceUrl);
    }

    /// <summary>
    /// 문단 단위로 텍스트를 분할
    /// </summary>
    private List<WebContentChunk> SplitByParagraphs(string text, int maxChunkSize, string sourceUrl)
    {
        var chunks = new List<WebContentChunk>();
        var paragraphs = text.Split(ParagraphSplitSeparators, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new List<string>();
        var currentLength = 0;
        var sequenceNumber = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphLength = paragraph.Length;

            // 현재 청크에 추가하면 최대 크기를 초과하는 경우
            if (currentLength + paragraphLength > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(CreateChunk(string.Join("\n\n", currentChunk), sequenceNumber++, sourceUrl));
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(paragraph.Trim());
            currentLength += paragraphLength + 2; // +2 for paragraph separator
        }

        // 마지막 청크 추가
        if (currentChunk.Count > 0)
        {
            chunks.Add(CreateChunk(string.Join("\n\n", currentChunk), sequenceNumber, sourceUrl));
        }

        return chunks;
    }
}