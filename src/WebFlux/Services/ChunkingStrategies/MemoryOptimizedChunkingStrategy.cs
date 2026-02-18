using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 메모리 최적화 청킹 전략 (단순화됨)
/// 대용량 문서 처리를 위한 메모리 효율적 분할
/// </summary>
public class MemoryOptimizedChunkingStrategy : BaseChunkingStrategy
{
    public override string Name => "MemoryOptimized";
    public override string Description => "메모리 효율성 최적화 - 대용량 문서 처리 전용";

    public MemoryOptimizedChunkingStrategy(IEventPublisher? eventPublisher = null)
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
        var chunkSize = options?.ChunkSize ?? 1000; // 기본 1000자

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        // 메모리 효율적 청킹: 스트리밍 방식으로 처리
        return StreamingChunk(text, chunkSize, sourceUrl);
    }

    /// <summary>
    /// 스트리밍 방식의 메모리 효율적 청킹
    /// </summary>
    private List<WebContentChunk> StreamingChunk(string text, int chunkSize, string sourceUrl)
    {
        var chunks = new List<WebContentChunk>();
        var sequenceNumber = 0;
        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var endIndex = Math.Min(startIndex + chunkSize, text.Length);

            // 단어 경계에서 자르기
            if (endIndex < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', endIndex);
                if (lastSpace > startIndex + chunkSize * 0.8) // 80% 이상이면 단어 경계로 자르기
                {
                    endIndex = lastSpace;
                }
            }

            var chunkText = text.Substring(startIndex, endIndex - startIndex).Trim();

            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                chunks.Add(CreateChunk(chunkText, sequenceNumber++, sourceUrl));
            }

            startIndex = endIndex;

            // 메모리 압박이 있을 수 있으므로 가비지 컬렉션을 유도
            if (sequenceNumber % 100 == 0)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        return chunks;
    }
}