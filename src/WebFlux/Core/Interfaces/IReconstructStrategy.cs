using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 재구성 전략 인터페이스
/// 다양한 콘텐츠 재구성 알고리즘 지원
/// </summary>
public interface IReconstructStrategy
{
    /// <summary>
    /// 전략 이름
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 전략 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 권장 사용 사례
    /// </summary>
    IEnumerable<string> RecommendedUseCases { get; }

    /// <summary>
    /// 이 전략이 주어진 콘텐츠에 적합한지 확인
    /// </summary>
    /// <param name="content">분석된 콘텐츠</param>
    /// <param name="options">재구성 옵션</param>
    /// <returns>적용 가능 여부</returns>
    bool IsApplicable(AnalyzedContent content, ReconstructOptions options);

    /// <summary>
    /// 콘텐츠에 재구성 전략을 적용합니다
    /// </summary>
    /// <param name="content">분석된 콘텐츠</param>
    /// <param name="options">재구성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>재구성된 콘텐츠</returns>
    Task<ReconstructedContent> ApplyAsync(
        AnalyzedContent content,
        ReconstructOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 예상 처리 시간을 추정합니다
    /// </summary>
    /// <param name="content">분석된 콘텐츠</param>
    /// <param name="options">재구성 옵션</param>
    /// <returns>예상 처리 시간</returns>
    TimeSpan EstimateProcessingTime(AnalyzedContent content, ReconstructOptions options);
}
