using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 메인 웹 콘텐츠 처리 파이프라인 인터페이스
/// 크롤링부터 청킹까지의 전체 처리 과정을 관리
/// </summary>
public interface IWebContentProcessor
{
    /// <summary>
    /// 단일 URL을 처리하여 청크 목록을 반환합니다.
    /// </summary>
    /// <param name="url">처리할 웹 페이지 URL</param>
    /// <param name="chunkingOptions">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 청크 목록</returns>
    Task<IReadOnlyList<WebContentChunk>> ProcessUrlAsync(
        string url,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 URL을 배치로 처리합니다.
    /// </summary>
    /// <param name="urls">처리할 URL 목록</param>
    /// <param name="chunkingOptions">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>각 URL별 청크 목록</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<WebContentChunk>>> ProcessUrlsBatchAsync(
        IEnumerable<string> urls,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 웹사이트를 크롤링하여 처리합니다.
    /// </summary>
    /// <param name="startUrl">시작 URL</param>
    /// <param name="crawlOptions">크롤링 옵션</param>
    /// <param name="chunkingOptions">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 모든 페이지의 청크 스트림</returns>
    IAsyncEnumerable<WebContentChunk> ProcessWebsiteAsync(
        string startUrl,
        CrawlOptions? crawlOptions = null,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HTML 콘텐츠를 직접 처리합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="chunkingOptions">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 청크 목록</returns>
    Task<IReadOnlyList<WebContentChunk>> ProcessHtmlAsync(
        string htmlContent,
        string sourceUrl,
        ChunkingOptions? chunkingOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 처리 진행률을 모니터링합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <returns>진행률 정보 스트림</returns>
    IAsyncEnumerable<ProcessingProgress> MonitorProgressAsync(string jobId);

    /// <summary>
    /// 진행 중인 작업을 취소합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <returns>취소 성공 여부</returns>
    Task<bool> CancelJobAsync(string jobId);

    /// <summary>
    /// 처리 통계를 반환합니다.
    /// </summary>
    /// <returns>처리 통계 정보</returns>
    Task<ProcessingStatistics> GetStatisticsAsync();

    /// <summary>
    /// 사용된 청킹 전략 목록을 반환합니다.
    /// </summary>
    /// <returns>사용 가능한 청킹 전략 목록</returns>
    IReadOnlyList<string> GetAvailableChunkingStrategies();
}

/// <summary>
/// 처리 진행률 정보
/// </summary>
public class ProcessingProgress
{
    /// <summary>작업 ID</summary>
#if NET8_0_OR_GREATER
    public required string JobId { get; init; }
#else
    public string JobId { get; init; } = string.Empty;
#endif

    /// <summary>진행률 (0.0 - 1.0)</summary>
    public double Progress { get; init; }

    /// <summary>현재 단계</summary>
    public string CurrentStage { get; init; } = string.Empty;

    /// <summary>처리된 페이지 수</summary>
    public int ProcessedPages { get; init; }

    /// <summary>총 페이지 수 (예상)</summary>
    public int? TotalPages { get; init; }

    /// <summary>생성된 청크 수</summary>
    public int GeneratedChunks { get; init; }

    /// <summary>처리 속도 (페이지/분)</summary>
    public double ProcessingRate { get; init; }

    /// <summary>예상 완료 시간</summary>
    public DateTimeOffset? EstimatedCompletion { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>오류 목록</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

