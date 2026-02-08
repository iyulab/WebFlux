using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 메인 웹 콘텐츠 처리 파이프라인 인터페이스 (ISP 파사드)
/// IContentExtractService + IContentChunkService를 상속하며,
/// 작업 관리/진단 메서드를 직접 보유
/// </summary>
public interface IWebContentProcessor : IContentExtractService, IContentChunkService
{
    /// <summary>
    /// 처리 진행률을 모니터링합니다
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <returns>진행률 정보 스트림</returns>
    [Obsolete("Job monitoring is not yet implemented. Will be available in a future version.")]
    IAsyncEnumerable<ProcessingProgress> MonitorProgressAsync(string jobId);

    /// <summary>
    /// 진행 중인 작업을 취소합니다
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <returns>취소 성공 여부</returns>
    [Obsolete("Job cancellation is not yet implemented. Will be available in a future version.")]
    Task<bool> CancelJobAsync(string jobId);

    /// <summary>
    /// 처리 통계를 반환합니다
    /// </summary>
    /// <returns>처리 통계 정보</returns>
    [Obsolete("Statistics tracking is not yet implemented. Will be available in a future version.")]
    Task<ProcessingStatistics> GetStatisticsAsync();

    /// <summary>
    /// 사용 가능한 청킹 전략 목록을 반환합니다
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

