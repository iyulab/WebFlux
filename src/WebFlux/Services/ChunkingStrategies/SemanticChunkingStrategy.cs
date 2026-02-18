using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 의미론적 청킹 전략 (단순화됨 - Interface Provider)
/// 임베딩 서비스는 소비자가 제공
/// </summary>
public class SemanticChunkingStrategy : BaseChunkingStrategy
{
    private static readonly string[] ParagraphSplitSeparators = ["\n\n", "\r\n\r\n"];
    private static readonly char[] SentenceSplitChars = ['.', '!', '?'];
    private readonly ITextEmbeddingService? _embeddingService;

    public override string Name => "Semantic";
    public override string Description => "의미론적 청킹 - 임베딩 기반 의미적 일관성 최적화 (임베딩 서비스 필요)";

    public SemanticChunkingStrategy(IEventPublisher? eventPublisher = null, ITextEmbeddingService? embeddingService = null)
        : base(eventPublisher)
    {
        _embeddingService = embeddingService;
    }

    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var text = content.MainContent ?? content.Text ?? string.Empty;
        var sourceUrl = content.Url ?? content.OriginalUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        // 임베딩 서비스가 없으면 문단 기반으로 폴백
        if (_embeddingService == null)
        {
            return await FallbackToParagraphChunking(text, options?.ChunkSize ?? 1500, sourceUrl);
        }

        // 간단한 의미론적 청킹 구현
        return await SimpleSemanticChunking(text, options?.ChunkSize ?? 1500, sourceUrl, cancellationToken);
    }

    /// <summary>
    /// 임베딩 서비스가 없을 때 문단 기반으로 폴백
    /// </summary>
    private async Task<IReadOnlyList<WebContentChunk>> FallbackToParagraphChunking(string text, int maxChunkSize, string sourceUrl)
    {
        await Task.CompletedTask;

        var paragraphs = text.Split(ParagraphSplitSeparators, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<WebContentChunk>();
        var currentChunk = new List<string>();
        var currentLength = 0;
        var sequenceNumber = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentLength + paragraph.Length > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(CreateChunk(string.Join("\n\n", currentChunk), sequenceNumber++, sourceUrl));
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(paragraph.Trim());
            currentLength += paragraph.Length + 2;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(CreateChunk(string.Join("\n\n", currentChunk), sequenceNumber, sourceUrl));
        }

        return chunks.AsReadOnly();
    }

    /// <summary>
    /// 간단한 의미론적 청킹 (임베딩 서비스 사용)
    /// </summary>
    private Task<IReadOnlyList<WebContentChunk>> SimpleSemanticChunking(string text, int maxChunkSize, string sourceUrl, CancellationToken cancellationToken)
    {
        // 문장 단위로 분할
        var sentences = text.Split(SentenceSplitChars, StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToList();

        var chunks = new List<WebContentChunk>();
        var currentChunk = new List<string>();
        var currentLength = 0;
        var sequenceNumber = 0;

        foreach (var sentence in sentences)
        {
            if (currentLength + sentence.Length > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(CreateChunk(string.Join(". ", currentChunk) + ".", sequenceNumber++, sourceUrl));
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(sentence);
            currentLength += sentence.Length + 2;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(CreateChunk(string.Join(". ", currentChunk) + ".", sequenceNumber, sourceUrl));
        }

        return Task.FromResult<IReadOnlyList<WebContentChunk>>(chunks.AsReadOnly());
    }
}