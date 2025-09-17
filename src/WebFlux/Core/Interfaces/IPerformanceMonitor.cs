using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 성능 모니터링 인터페이스
/// Phase 5B.4: OpenTelemetry 기반 엔터프라이즈 모니터링
/// </summary>
public interface IPerformanceMonitor : IDisposable
{
    /// <summary>
    /// 활동(Activity) 시작
    /// </summary>
    /// <param name="name">활동 이름</param>
    /// <param name="tags">태그 정보</param>
    /// <returns>활동 인스턴스</returns>
    Activity? StartActivity(string name, params (string Key, object? Value)[] tags);

    /// <summary>
    /// 메트릭 카운터 증가
    /// </summary>
    /// <param name="name">메트릭 이름</param>
    /// <param name="value">증가값</param>
    /// <param name="tags">태그 정보</param>
    void IncrementCounter(string name, long value = 1, params (string Key, object? Value)[] tags);

    /// <summary>
    /// 메트릭 히스토그램 기록
    /// </summary>
    /// <param name="name">메트릭 이름</param>
    /// <param name="value">측정값</param>
    /// <param name="tags">태그 정보</param>
    void RecordHistogram(string name, double value, params (string Key, object? Value)[] tags);

    /// <summary>
    /// 메트릭 게이지 설정
    /// </summary>
    /// <param name="name">메트릭 이름</param>
    /// <param name="value">현재값</param>
    /// <param name="tags">태그 정보</param>
    void SetGauge(string name, long value, params (string Key, object? Value)[] tags);

    /// <summary>
    /// 처리 시간 측정
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <param name="tags">태그 정보</param>
    /// <returns>측정 범위</returns>
    IDisposable MeasureOperation(string operationName, params (string Key, object? Value)[] tags);

    /// <summary>
    /// 에러 기록
    /// </summary>
    /// <param name="exception">예외 정보</param>
    /// <param name="context">컨텍스트 정보</param>
    void RecordError(Exception exception, string? context = null);

    /// <summary>
    /// 청킹 품질 메트릭 기록
    /// </summary>
    /// <param name="strategy">청킹 전략</param>
    /// <param name="qualityScore">품질 점수</param>
    /// <param name="chunkCount">청크 수</param>
    /// <param name="processingTime">처리 시간</param>
    void RecordChunkingMetrics(string strategy, double qualityScore, int chunkCount, TimeSpan processingTime);

    /// <summary>
    /// 캐시 메트릭 기록
    /// </summary>
    /// <param name="operation">캐시 작업</param>
    /// <param name="hit">캐시 히트 여부</param>
    /// <param name="duration">작업 시간</param>
    void RecordCacheMetrics(string operation, bool hit, TimeSpan duration);

    /// <summary>
    /// 멀티모달 처리 메트릭 기록
    /// </summary>
    /// <param name="imageCount">처리된 이미지 수</param>
    /// <param name="confidence">평균 신뢰도</param>
    /// <param name="processingTime">처리 시간</param>
    void RecordMultimodalMetrics(int imageCount, double confidence, TimeSpan processingTime);

    /// <summary>
    /// 크롤링 메트릭 기록
    /// </summary>
    /// <param name="strategy">크롤링 전략</param>
    /// <param name="url">URL</param>
    /// <param name="success">성공 여부</param>
    /// <param name="contentLength">콘텐츠 길이</param>
    /// <param name="responseTime">응답 시간</param>
    void RecordCrawlingMetrics(string strategy, string url, bool success, long contentLength, TimeSpan responseTime);

    /// <summary>
    /// 시스템 리소스 메트릭 기록
    /// </summary>
    void RecordSystemMetrics();

    /// <summary>
    /// 현재 측정 통계 조회
    /// </summary>
    /// <returns>성능 통계</returns>
    Task<PerformanceStatistics> GetStatisticsAsync();
}

/// <summary>
/// 성능 통계 정보
/// </summary>
public class PerformanceStatistics
{
    /// <summary>시작 시간</summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>측정 기간</summary>
    public TimeSpan Duration => DateTimeOffset.UtcNow - StartTime;

    /// <summary>총 요청 수</summary>
    public long TotalRequests { get; set; }

    /// <summary>평균 응답 시간</summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>에러 율</summary>
    public double ErrorRate { get; set; }

    /// <summary>처리량 (요청/초)</summary>
    public double Throughput { get; set; }

    /// <summary>메모리 사용량 (바이트)</summary>
    public long MemoryUsage { get; set; }

    /// <summary>CPU 사용률 (%)</summary>
    public double CpuUsage { get; set; }

    /// <summary>캐시 히트율</summary>
    public double CacheHitRatio { get; set; }

    /// <summary>평균 청킹 품질 점수</summary>
    public double AverageChunkQuality { get; set; }

    /// <summary>활성 활동 수</summary>
    public int ActiveActivities { get; set; }

    /// <summary>세부 메트릭</summary>
    public Dictionary<string, object> DetailedMetrics { get; set; } = new();
}

/// <summary>
/// 작업 측정 범위
/// </summary>
public interface IOperationScope : IDisposable
{
    /// <summary>작업 이름</summary>
    string OperationName { get; }

    /// <summary>시작 시간</summary>
    DateTimeOffset StartTime { get; }

    /// <summary>태그 추가</summary>
    void AddTag(string key, object? value);

    /// <summary>에러 기록</summary>
    void RecordError(Exception exception);

    /// <summary>성공 완료</summary>
    void Complete();
}