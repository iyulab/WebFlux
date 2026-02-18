using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 진행률 보고 서비스 인터페이스
/// 처리 과정의 실시간 진행률 추적 및 보고
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// 새로운 작업을 시작합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="description">작업 설명</param>
    /// <param name="totalSteps">전체 단계 수</param>
    /// <returns>작업 진행률 추적기</returns>
    Task<IProgressTracker> StartJobAsync(string jobId, string description, int totalSteps);

    /// <summary>
    /// 작업 진행률을 업데이트합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="progress">진행률 정보</param>
    /// <returns>업데이트 작업</returns>
    Task ReportProgressAsync(string jobId, ProgressInfo progress);

    /// <summary>
    /// 작업을 완료로 표시합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="result">작업 결과</param>
    /// <returns>완료 작업</returns>
    Task CompleteJobAsync(string jobId, object? result = null);

    /// <summary>
    /// 작업을 실패로 표시합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="exception">오류 정보</param>
    /// <returns>실패 작업</returns>
    Task FailJobAsync(string jobId, Exception exception);

    /// <summary>
    /// 작업 진행률을 실시간으로 모니터링합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>진행률 스트림</returns>
    IAsyncEnumerable<ProgressInfo> MonitorProgressAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 모든 활성 작업의 진행률을 반환합니다.
    /// </summary>
    /// <returns>활성 작업 진행률 목록</returns>
    Task<IReadOnlyList<JobProgress>> GetAllJobsAsync();

    /// <summary>
    /// 특정 작업의 현재 진행률을 반환합니다.
    /// </summary>
    /// <param name="jobId">작업 ID</param>
    /// <returns>작업 진행률</returns>
    Task<JobProgress?> GetJobProgressAsync(string jobId);

    /// <summary>
    /// 완료된 작업을 정리합니다.
    /// </summary>
    /// <param name="olderThan">지정된 시간보다 오래된 작업 정리</param>
    /// <returns>정리 작업</returns>
    Task CleanupCompletedJobsAsync(TimeSpan? olderThan = null);
}

/// <summary>
/// 진행률 추적기 인터페이스
/// </summary>
public interface IProgressTracker : IDisposable
{
    /// <summary>
    /// 작업 ID
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// 현재 단계를 업데이트합니다.
    /// </summary>
    /// <param name="stepName">단계 이름</param>
    /// <param name="stepNumber">단계 번호 (0부터 시작)</param>
    /// <param name="details">상세 정보</param>
    /// <returns>업데이트 작업</returns>
    Task UpdateStepAsync(string stepName, int stepNumber, string? details = null);

    /// <summary>
    /// 현재 단계 내의 진행률을 업데이트합니다.
    /// </summary>
    /// <param name="current">현재 진행량</param>
    /// <param name="total">전체 진행량</param>
    /// <param name="details">상세 정보</param>
    /// <returns>업데이트 작업</returns>
    Task UpdateStepProgressAsync(int current, int total, string? details = null);

    /// <summary>
    /// 메시지를 기록합니다.
    /// </summary>
    /// <param name="level">로그 레벨</param>
    /// <param name="message">메시지</param>
    /// <param name="data">추가 데이터</param>
    /// <returns>로그 작업</returns>
    Task LogAsync(ProgressLogLevel level, string message, object? data = null);

    /// <summary>
    /// 작업을 완료로 표시합니다.
    /// </summary>
    /// <param name="result">작업 결과</param>
    /// <returns>완료 작업</returns>
    Task CompleteAsync(object? result = null);

    /// <summary>
    /// 작업을 실패로 표시합니다.
    /// </summary>
    /// <param name="exception">오류 정보</param>
    /// <returns>실패 작업</returns>
    Task FailAsync(Exception exception);
}