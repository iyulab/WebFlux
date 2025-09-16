using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 메타데이터 통합 발견 서비스 인터페이스
/// 모든 웹 메타데이터 표준을 통합하여 분석하고 최적화 전략 제공
/// </summary>
public interface IMetadataDiscoveryService
{
    /// <summary>
    /// 웹사이트에서 모든 메타데이터를 자동 발견하고 분석합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="options">발견 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>통합 메타데이터 발견 결과</returns>
    Task<MetadataDiscoveryResult> DiscoverAllMetadataAsync(
        string baseUrl,
        MetadataDiscoveryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 메타데이터 타입들만 발견합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="metadataTypes">발견할 메타데이터 타입 목록</param>
    /// <param name="options">발견 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>선택적 메타데이터 발견 결과</returns>
    Task<MetadataDiscoveryResult> DiscoverMetadataAsync(
        string baseUrl,
        IEnumerable<MetadataType> metadataTypes,
        MetadataDiscoveryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 발견된 메타데이터를 기반으로 크롤링 전략을 최적화합니다.
    /// </summary>
    /// <param name="discoveryResult">메타데이터 발견 결과</param>
    /// <param name="crawlOptions">기존 크롤링 옵션</param>
    /// <returns>최적화된 크롤링 옵션</returns>
    Task<CrawlOptions> OptimizeCrawlOptionsAsync(MetadataDiscoveryResult discoveryResult, CrawlOptions crawlOptions);

    /// <summary>
    /// 발견된 메타데이터를 기반으로 청킹 전략을 개선합니다.
    /// </summary>
    /// <param name="discoveryResult">메타데이터 발견 결과</param>
    /// <param name="content">추출된 콘텐츠</param>
    /// <param name="chunkingOptions">기존 청킹 옵션</param>
    /// <returns>개선된 청킹 옵션</returns>
    Task<ChunkingOptions> EnhanceChunkingOptionsAsync(
        MetadataDiscoveryResult discoveryResult,
        ExtractedContent content,
        ChunkingOptions chunkingOptions);

    /// <summary>
    /// 웹사이트의 구조적 지능 분석을 수행합니다.
    /// </summary>
    /// <param name="discoveryResult">메타데이터 발견 결과</param>
    /// <returns>구조적 지능 분석 결과</returns>
    Task<StructuralIntelligenceResult> AnalyzeStructuralIntelligenceAsync(MetadataDiscoveryResult discoveryResult);

    /// <summary>
    /// 메타데이터 품질 점수를 계산합니다.
    /// </summary>
    /// <param name="discoveryResult">메타데이터 발견 결과</param>
    /// <returns>품질 점수 (0-100)</returns>
    Task<MetadataQualityScore> EvaluateMetadataQualityAsync(MetadataDiscoveryResult discoveryResult);

    /// <summary>
    /// 발견된 메타데이터를 캐시합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="discoveryResult">발견 결과</param>
    /// <param name="ttl">캐시 만료 시간</param>
    Task CacheMetadataAsync(string baseUrl, MetadataDiscoveryResult discoveryResult, TimeSpan? ttl = null);

    /// <summary>
    /// 캐시된 메타데이터를 가져옵니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <returns>캐시된 메타데이터 또는 null</returns>
    Task<MetadataDiscoveryResult?> GetCachedMetadataAsync(string baseUrl);

    /// <summary>
    /// 메타데이터 발견 통계를 반환합니다.
    /// </summary>
    MetadataDiscoveryStatistics GetStatistics();

    /// <summary>
    /// 지원되는 메타데이터 타입 목록을 반환합니다.
    /// </summary>
    IReadOnlyList<MetadataType> GetSupportedMetadataTypes();

    /// <summary>
    /// 메타데이터 타입별 발견 가능성을 예측합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="initialProbe">초기 프로브 수행 여부</param>
    /// <returns>메타데이터 타입별 발견 가능성</returns>
    Task<Dictionary<MetadataType, double>> PredictMetadataAvailabilityAsync(string baseUrl, bool initialProbe = false);
}