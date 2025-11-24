using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 마크다운 구조 분석 인터페이스
/// 95% 구조 정확도 달성을 목표로 합니다
/// </summary>
public interface IMarkdownStructureAnalyzer
{
    /// <summary>
    /// 마크다운 콘텐츠에서 구조 정보를 추출합니다
    /// </summary>
    /// <param name="markdownContent">마크다운 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>마크다운 구조 정보</returns>
    Task<MarkdownStructureInfo> AnalyzeStructureAsync(
        string markdownContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 마크다운을 HTML로 변환하면서 구조 정보를 유지합니다
    /// </summary>
    /// <param name="markdownContent">마크다운 콘텐츠</param>
    /// <param name="options">변환 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTML과 구조 정보</returns>
    Task<MarkdownConversionResult> ConvertToHtmlWithStructureAsync(
        string markdownContent,
        MarkdownConversionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 마크다운 구조의 정확도를 검증합니다
    /// </summary>
    /// <param name="structureInfo">구조 정보</param>
    /// <returns>정확도 점수 (0.0 - 1.0)</returns>
    double ValidateStructureAccuracy(MarkdownStructureInfo structureInfo);

    /// <summary>
    /// 마크다운 콘텐츠의 품질을 평가합니다
    /// </summary>
    /// <param name="structureInfo">구조 정보</param>
    /// <returns>품질 평가 결과</returns>
    MarkdownQualityAssessment AssessQuality(MarkdownStructureInfo structureInfo);
}