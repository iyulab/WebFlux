using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 크롤링 진행률 보고 인터페이스
/// 배치 크롤링 시 상세 진행률 및 에러 리포팅
/// </summary>
public interface ICrawlProgressReporter
{
    /// <summary>
    /// 크롤링 작업 시작
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="totalUrls">총 URL 수</param>
    /// <returns>크롤링 진행률 추적기</returns>
    ICrawlProgressTracker StartCrawl(string jobId, int totalUrls);

    /// <summary>
    /// 현재 진행률 조회
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <returns>현재 크롤링 진행률</returns>
    CrawlProgress? GetProgress(string jobId);

    /// <summary>
    /// 진행률 스트림 구독
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>진행률 스트림</returns>
    IAsyncEnumerable<CrawlProgress> MonitorProgressAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 모든 활성 크롤링 작업 조회
    /// </summary>
    /// <returns>활성 크롤링 진행률 목록</returns>
    IReadOnlyList<CrawlProgress> GetAllActiveJobs();
}

/// <summary>
/// 크롤링 진행률 추적기 인터페이스
/// </summary>
public interface ICrawlProgressTracker : IDisposable
{
    /// <summary>
    /// 작업 ID
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// URL 처리 시작
    /// </summary>
    /// <param name="url">처리 중인 URL</param>
    void StartUrl(string url);

    /// <summary>
    /// URL 처리 성공
    /// </summary>
    /// <param name="url">처리된 URL</param>
    /// <param name="chunkCount">생성된 청크 수</param>
    /// <param name="bytesDownloaded">다운로드 바이트 수</param>
    /// <param name="responseTimeMs">응답 시간 (밀리초)</param>
    void CompleteUrl(string url, int chunkCount, long bytesDownloaded = 0, double responseTimeMs = 0);

    /// <summary>
    /// URL 처리 실패
    /// </summary>
    /// <param name="url">실패한 URL</param>
    /// <param name="errorType">에러 유형</param>
    /// <param name="message">에러 메시지</param>
    /// <param name="statusCode">HTTP 상태 코드 (해당하는 경우)</param>
    /// <param name="retryCount">재시도 횟수</param>
    void FailUrl(string url, string errorType, string message, int? statusCode = null, int retryCount = 0);

    /// <summary>
    /// 크롤링 완료
    /// </summary>
    void Complete();

    /// <summary>
    /// 크롤링 취소
    /// </summary>
    /// <param name="reason">취소 사유</param>
    void Cancel(string? reason = null);

    /// <summary>
    /// 현재 진행률 조회
    /// </summary>
    /// <returns>현재 크롤링 진행률</returns>
    CrawlProgress GetCurrentProgress();
}
