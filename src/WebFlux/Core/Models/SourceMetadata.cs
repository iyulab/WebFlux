using WebFlux.Core.Interfaces;

namespace WebFlux.Core.Models;

/// <summary>
/// ISourceMetadata 구현체
/// 소스 문서의 메타데이터를 포함
/// </summary>
public class SourceMetadata : ISourceMetadata
{
    /// <inheritdoc />
    public required string SourceId { get; init; }

    /// <inheritdoc />
    public required string SourceType { get; init; }

    /// <inheritdoc />
    public required string Title { get; init; }

    /// <inheritdoc />
    public string? Url { get; init; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public required string Language { get; init; }

    /// <inheritdoc />
    public int WordCount { get; init; }

    /// <inheritdoc />
    public int ChunkCount { get; init; }

    /// <inheritdoc />
    public DateTime? PublishedAt { get; init; }

    /// <inheritdoc />
    public string? Author { get; init; }

    /// <inheritdoc />
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// WebContentMetadata에서 SourceMetadata 생성
    /// </summary>
    public static SourceMetadata FromWebContentMetadata(
        WebContentMetadata metadata,
        string sourceId,
        string url,
        int wordCount,
        int chunkCount)
    {
        return new SourceMetadata
        {
            SourceId = sourceId,
            SourceType = "url",
            Title = metadata.Title ?? string.Empty,
            Url = url,
            CreatedAt = metadata.CrawledAt.UtcDateTime,
            Language = metadata.Language ?? "unknown",
            WordCount = wordCount,
            ChunkCount = chunkCount,
            PublishedAt = metadata.PublishedDate?.UtcDateTime,
            Author = metadata.Author,
            Keywords = metadata.Keywords
        };
    }

    /// <summary>
    /// EnrichedMetadata에서 SourceMetadata 생성
    /// </summary>
    public static SourceMetadata FromEnrichedMetadata(
        EnrichedMetadata metadata,
        string sourceId,
        int wordCount,
        int chunkCount)
    {
        return new SourceMetadata
        {
            SourceId = sourceId,
            SourceType = "url",
            Title = metadata.Title ?? string.Empty,
            Url = metadata.Url,
            CreatedAt = metadata.ExtractedAt.UtcDateTime,
            Language = metadata.Language ?? "unknown",
            WordCount = wordCount,
            ChunkCount = chunkCount,
            PublishedAt = metadata.PublishedDate?.UtcDateTime,
            Author = metadata.Author,
            Keywords = metadata.Keywords.ToList()
        };
    }
}
