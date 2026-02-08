using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 청킹 서비스 인터페이스 (ISP 분리)
/// URL/HTML 콘텐츠를 청킹까지 처리하는 API
/// </summary>
public interface IContentChunkService
{
    /// <summary>
    /// 단일 URL을 처리하여 청크 목록을 반환합니다
    /// </summary>
    Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
        string url,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 URL을 배치로 처리합니다
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
        IEnumerable<string> urls,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 웹사이트를 크롤링하여 처리합니다
    /// </summary>
    IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HTML 콘텐츠를 직접 처리합니다
    /// </summary>
    Task<IReadOnlyList<WebContentChunk>> ProcessHtmlAsync(
        string htmlContent,
        string sourceUrl,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);
}
