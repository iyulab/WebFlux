using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 정적/동적 처리 최적화 서비스 인터페이스
/// 콘텐츠 분석과 성능 메트릭을 기반으로 최적의 처리 전략을 동적으로 선택
/// </summary>
public interface IProcessingOptimizationService
{
    /// <summary>
    /// 콘텐츠 분석을 통해 최적의 청킹 전략을 추천합니다
    /// </summary>
    /// <param name="content">분석할 콘텐츠</param>
    /// <param name="metadata">콘텐츠 메타데이터</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>최적화된 청킹 전략 추천</returns>
    Task<ChunkingStrategyRecommendation> RecommendChunkingStrategyAsync(
        string content,
        ContentMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 실시간 성능 메트릭을 기반으로 처리 전략을 동적으로 조정합니다
    /// </summary>
    /// <param name="currentStrategy">현재 사용 중인 전략</param>
    /// <param name="performanceMetrics">성능 메트릭</param>
    /// <returns>조정된 처리 전략</returns>
    Task<ProcessingStrategy> OptimizeStrategyAsync(
        string currentStrategy,
        PerformanceStatistics performanceMetrics);

    /// <summary>
    /// 캐시 최적화를 통해 처리 성능을 향상시킵니다
    /// </summary>
    /// <param name="cacheKey">캐시 키</param>
    /// <param name="contentHash">콘텐츠 해시</param>
    /// <param name="ttl">캐시 생존 시간</param>
    /// <returns>캐시 최적화 결과</returns>
    Task<CacheOptimizationResult> OptimizeCacheUsageAsync(
        string cacheKey,
        string contentHash,
        TimeSpan? ttl = null);

    /// <summary>
    /// 리소스 사용량을 모니터링하고 최적화 제안을 제공합니다
    /// </summary>
    /// <returns>리소스 최적화 제안</returns>
    Task<ResourceOptimizationSuggestion> AnalyzeResourceUsageAsync();

    /// <summary>
    /// 처리 파이프라인의 병목 구간을 식별하고 최적화합니다
    /// </summary>
    /// <param name="pipelineMetrics">파이프라인 메트릭</param>
    /// <returns>병목 최적화 결과</returns>
    Task<BottleneckOptimizationResult> OptimizeBottlenecksAsync(
        PipelineMetrics pipelineMetrics);

    /// <summary>
    /// 토큰 사용량을 최적화하여 비용을 절감합니다
    /// </summary>
    /// <param name="text">분석할 텍스트</param>
    /// <param name="targetTokenLimit">목표 토큰 제한</param>
    /// <returns>토큰 최적화 결과</returns>
    Task<TokenOptimizationResult> OptimizeTokenUsageAsync(
        string text,
        int targetTokenLimit);

    /// <summary>
    /// 최적화 통계 및 성과를 가져옵니다
    /// </summary>
    /// <returns>최적화 통계</returns>
    Task<OptimizationStatistics> GetOptimizationStatisticsAsync();
}