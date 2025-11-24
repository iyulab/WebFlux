using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 분석 인터페이스
/// Stage 2: 가공 + 원본 유지 + 불필요한 요소 제거
/// </summary>
public interface IContentAnalyzer
{
    /// <summary>
    /// 분석기 이름
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 분석기 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 원본 콘텐츠를 분석하여 구조화합니다
    /// </summary>
    /// <param name="rawContent">추출된 원본 콘텐츠</param>
    /// <param name="options">분석 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>분석된 콘텐츠</returns>
    Task<AnalyzedContent> AnalyzeAsync(
        RawContent rawContent,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스트리밍 방식으로 여러 콘텐츠를 분석합니다
    /// </summary>
    /// <param name="rawContents">원본 콘텐츠 스트림</param>
    /// <param name="options">분석 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>분석된 콘텐츠 스트림</returns>
    IAsyncEnumerable<AnalyzedContent> AnalyzeStreamAsync(
        IAsyncEnumerable<RawContent> rawContents,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 분석 품질을 평가합니다
    /// </summary>
    /// <param name="analyzedContent">분석된 콘텐츠</param>
    /// <returns>품질 점수 (0.0 ~ 1.0)</returns>
    double EvaluateQuality(AnalyzedContent analyzedContent);
}
