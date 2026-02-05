using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 품질 평가 인터페이스
/// AI 리서치용 콘텐츠 필터링 및 우선순위 결정에 사용
/// </summary>
public interface IContentQualityEvaluator
{
    /// <summary>
    /// 추출된 콘텐츠의 품질을 평가합니다
    /// </summary>
    /// <param name="content">추출된 콘텐츠</param>
    /// <param name="originalHtml">원본 HTML (선택적, 상세 분석에 사용)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>품질 정보</returns>
    Task<ContentQualityInfo> EvaluateAsync(
        ExtractedContent content,
        string? originalHtml = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HTML 콘텐츠의 품질을 평가합니다
    /// </summary>
    /// <param name="html">HTML 콘텐츠</param>
    /// <param name="url">소스 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>품질 정보</returns>
    Task<ContentQualityInfo> EvaluateHtmlAsync(
        string html,
        string url,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 페이월 존재 여부를 감지합니다
    /// </summary>
    /// <param name="html">HTML 콘텐츠</param>
    /// <param name="text">추출된 텍스트</param>
    /// <returns>페이월 감지 여부</returns>
    bool DetectPaywall(string html, string? text = null);

    /// <summary>
    /// 콘텐츠 타입을 분류합니다
    /// </summary>
    /// <param name="content">추출된 콘텐츠</param>
    /// <returns>콘텐츠 타입 (article, blog, documentation, product, etc.)</returns>
    string ClassifyContentType(ExtractedContent content);

    /// <summary>
    /// 언어를 감지합니다
    /// </summary>
    /// <param name="text">텍스트 콘텐츠</param>
    /// <returns>ISO 639-1 언어 코드</returns>
    string DetectLanguage(string text);

    /// <summary>
    /// 광고 밀도를 계산합니다
    /// </summary>
    /// <param name="html">HTML 콘텐츠</param>
    /// <returns>광고 밀도 (0.0 - 1.0)</returns>
    double CalculateAdDensity(string html);

    /// <summary>
    /// 콘텐츠 비율을 계산합니다 (텍스트 / HTML)
    /// </summary>
    /// <param name="html">HTML 콘텐츠</param>
    /// <param name="text">추출된 텍스트</param>
    /// <returns>콘텐츠 비율 (0.0 - 1.0)</returns>
    double CalculateContentRatio(string html, string text);

    /// <summary>
    /// 예상 토큰 수를 계산합니다 (GPT-4 기준)
    /// </summary>
    /// <param name="text">텍스트 콘텐츠</param>
    /// <returns>예상 토큰 수</returns>
    int EstimateTokenCount(string text);
}
