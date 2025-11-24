using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 재구성 전략 팩토리 인터페이스
/// 재구성 전략 선택 및 생성
/// </summary>
public interface IReconstructStrategyFactory
{
    /// <summary>
    /// 지원하는 모든 재구성 전략을 반환합니다
    /// </summary>
    /// <returns>전략 이름 목록</returns>
    IEnumerable<string> GetAvailableStrategies();

    /// <summary>
    /// 특정 전략의 재구성기를 생성합니다
    /// </summary>
    /// <param name="strategyName">전략 이름</param>
    /// <returns>재구성 전략 인스턴스</returns>
    IReconstructStrategy CreateStrategy(string strategyName);

    /// <summary>
    /// 콘텐츠와 옵션에 가장 적합한 전략을 자동 선택합니다
    /// </summary>
    /// <param name="content">분석된 콘텐츠</param>
    /// <param name="options">재구성 옵션</param>
    /// <returns>최적 재구성 전략</returns>
    IReconstructStrategy CreateOptimalStrategy(AnalyzedContent content, ReconstructOptions options);

    /// <summary>
    /// 전략별 특성 정보를 반환합니다
    /// </summary>
    /// <returns>전략 특성 딕셔너리</returns>
    Dictionary<string, ReconstructStrategyCharacteristics> GetStrategyCharacteristics();
}

/// <summary>
/// 재구성 전략 특성 정보
/// </summary>
public class ReconstructStrategyCharacteristics
{
    /// <summary>전략 이름</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>전략 설명</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>품질 수준</summary>
    public QualityLevel QualityLevel { get; set; }

    /// <summary>메모리 사용량</summary>
    public MemoryUsage MemoryUsage { get; set; }

    /// <summary>연산 비용</summary>
    public ComputationCost ComputationCost { get; set; }

    /// <summary>LLM 필요 여부</summary>
    public bool RequiresLLM { get; set; }

    /// <summary>권장 사용 사례</summary>
    public IEnumerable<string> RecommendedUseCases { get; set; } = new List<string>();
}

/// <summary>
/// 품질 수준
/// </summary>
public enum QualityLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// 메모리 사용량
/// </summary>
public enum MemoryUsage
{
    Low,
    Medium,
    High
}

/// <summary>
/// 연산 비용
/// </summary>
public enum ComputationCost
{
    Low,
    Medium,
    High,
    VeryHigh
}
