using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 고정 크기 청킹 전략 (단순화됨)
/// 지정된 크기로 텍스트를 분할
/// </summary>
public class FixedSizeChunkingStrategy : BaseChunkingStrategy
{
    public override string Name => "FixedSize";
    public override string Description => "고정 크기 기반 청킹 - 단순하고 예측 가능한 분할";

    public FixedSizeChunkingStrategy(IEventPublisher? eventPublisher = null)
        : base(eventPublisher)
    {
    }

    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // 동기 작업을 비동기로 래핑

        var chunkSize = options?.ChunkSize ?? 1000; // 기본 1000자
        var text = content.MainContent ?? content.Text ?? string.Empty;
        var sourceUrl = content.Url ?? content.OriginalUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        return SplitBySize(text, chunkSize, sourceUrl);
    }
}