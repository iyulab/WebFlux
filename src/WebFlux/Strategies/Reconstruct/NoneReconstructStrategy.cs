using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Strategies.Reconstruct;

/// <summary>
/// None 재구성 전략
/// 재구성 없이 원본 그대로 통과
/// </summary>
public class NoneReconstructStrategy : IReconstructStrategy
{
    public string Name => "None";

    public string Description => "재구성 없이 분석된 콘텐츠를 그대로 통과합니다";

    public IEnumerable<string> RecommendedUseCases => new[]
    {
        "빠른 처리가 필요한 경우",
        "원본 콘텐츠 품질이 충분한 경우",
        "LLM 비용 절감이 필요한 경우"
    };

    public bool IsApplicable(AnalyzedContent content, ReconstructOptions options)
    {
        // 항상 적용 가능 (기본 전략)
        return true;
    }

    public Task<ReconstructedContent> ApplyAsync(
        AnalyzedContent content,
        ReconstructOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = ReconstructedContent.FromAnalyzed(content);
        result.StrategyUsed = Name;
        result.Metrics = new ReconstructMetrics
        {
            Quality = 1.0, // 원본 그대로이므로 품질 유지
            CompressionRatio = 1.0,
            ProcessingTimeMs = 0
        };

        return Task.FromResult(result);
    }

    public TimeSpan EstimateProcessingTime(AnalyzedContent content, ReconstructOptions options)
    {
        return TimeSpan.Zero; // 즉시 처리
    }
}
