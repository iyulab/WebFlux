using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 재구성 인터페이스
/// Stage 3: 전략에 따른 재작성 및 LLM 활용 증강
/// </summary>
public interface IContentReconstructor
{
    /// <summary>
    /// 재구성기 이름
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 재구성기 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 분석된 콘텐츠를 재구성합니다
    /// </summary>
    /// <param name="analyzedContent">분석된 콘텐츠</param>
    /// <param name="options">재구성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>재구성된 콘텐츠</returns>
    Task<ReconstructedContent> ReconstructAsync(
        AnalyzedContent analyzedContent,
        ReconstructOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스트리밍 방식으로 여러 콘텐츠를 재구성합니다
    /// </summary>
    /// <param name="analyzedContents">분석된 콘텐츠 스트림</param>
    /// <param name="options">재구성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>재구성된 콘텐츠 스트림</returns>
    IAsyncEnumerable<ReconstructedContent> ReconstructStreamAsync(
        IAsyncEnumerable<AnalyzedContent> analyzedContents,
        ReconstructOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 재구성 품질을 평가합니다
    /// </summary>
    /// <param name="reconstructedContent">재구성된 콘텐츠</param>
    /// <returns>품질 점수 (0.0 ~ 1.0)</returns>
    double EvaluateQuality(ReconstructedContent reconstructedContent);
}
