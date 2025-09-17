using WebFlux.Core.Models;
using ChunkingOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 청킹 전략 팩토리 인터페이스
/// 다양한 청킹 전략의 생성과 관리를 담당
/// </summary>
public interface IChunkingStrategyFactory
{
    /// <summary>
    /// 지정된 이름의 청킹 전략을 생성합니다.
    /// </summary>
    /// <param name="strategyName">전략 이름</param>
    /// <returns>청킹 전략 인스턴스</returns>
    Task<IChunkingStrategy> CreateStrategyAsync(string strategyName);

    /// <summary>
    /// 사용 가능한 모든 청킹 전략 목록을 반환합니다.
    /// </summary>
    /// <returns>전략 이름 목록</returns>
    IEnumerable<string> GetAvailableStrategies();

    /// <summary>
    /// 특정 전략의 정보를 반환합니다.
    /// </summary>
    /// <param name="strategyName">전략 이름</param>
    /// <returns>전략 정보</returns>
    Task<StrategyInfo> GetStrategyInfoAsync(string strategyName);

    /// <summary>
    /// 콘텐츠 특성에 따른 권장 전략을 제안합니다.
    /// </summary>
    /// <param name="content">분석할 콘텐츠</param>
    /// <param name="options">청킹 옵션</param>
    /// <returns>권장 전략 이름</returns>
    Task<string> RecommendStrategyAsync(ExtractedContent content, ChunkingOptions? options = null);
}

/// <summary>
/// 전략 정보
/// </summary>
public class StrategyInfo
{
    /// <summary>
    /// 전략 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 전략 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 성능 정보
    /// </summary>
    public PerformanceInfo PerformanceInfo { get; set; } = new();

    /// <summary>
    /// 구성 옵션
    /// </summary>
    public List<ConfigurationOption> ConfigurationOptions { get; set; } = new();

    /// <summary>
    /// 사용 사례
    /// </summary>
    public List<string> UseCases { get; set; } = new();

    /// <summary>
    /// 적합한 콘텐츠 타입
    /// </summary>
    public List<string> SuitableContentTypes { get; set; } = new();
}