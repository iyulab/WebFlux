using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 청킹 전략 기본 구현체 (단순화됨)
/// Interface Provider 패턴에 따라 핵심 기능만 제공
/// </summary>
public abstract class BaseChunkingStrategy : IChunkingStrategy
{
    protected IEventPublisher? EventPublisher { get; }

    protected BaseChunkingStrategy(IEventPublisher? eventPublisher = null)
    {
        EventPublisher = eventPublisher;
    }

    /// <summary>
    /// 청킹 전략 이름
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 청킹 전략 설명
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 콘텐츠를 청크로 분할합니다.
    /// </summary>
    /// <param name="content">분할할 콘텐츠</param>
    /// <param name="options">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>청크 목록</returns>
    public abstract Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 기본 청크 생성 헬퍼 메서드
    /// </summary>
    protected WebContentChunk CreateChunk(string text, int sequenceNumber, string sourceUrl)
    {
        return new WebContentChunk
        {
            Id = Guid.NewGuid().ToString(),
            Content = text,
            SequenceNumber = sequenceNumber,
            SourceUrl = sourceUrl,
            StrategyInfo = new ChunkingStrategyInfo
            {
                StrategyName = Name,
                ProcessingTimeMs = 0
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 텍스트를 고정 크기로 분할하는 기본 구현
    /// </summary>
    protected IReadOnlyList<WebContentChunk> SplitBySize(string text, int chunkSize, string sourceUrl)
    {
        var chunks = new List<WebContentChunk>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new List<string>();
        var currentLength = 0;
        var sequenceNumber = 0;

        foreach (var word in words)
        {
            if (currentLength + word.Length > chunkSize && currentChunk.Count > 0)
            {
                chunks.Add(CreateChunk(string.Join(" ", currentChunk), sequenceNumber++, sourceUrl));
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(word);
            currentLength += word.Length + 1; // +1 for space
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(CreateChunk(string.Join(" ", currentChunk), sequenceNumber, sourceUrl));
        }

        return chunks.AsReadOnly();
    }
}