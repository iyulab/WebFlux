using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 추출 인터페이스
/// 다양한 형식의 웹 콘텐츠에서 텍스트와 메타데이터를 추출
/// </summary>
public interface IContentExtractor
{
    /// <summary>
    /// HTML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="enableMetadataExtraction">AI 메타데이터 추출 활성화 (기본값: false)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromHtmlAsync(
        string htmlContent,
        string sourceUrl,
        bool enableMetadataExtraction = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 마크다운 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="markdownContent">마크다운 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromMarkdownAsync(
        string markdownContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// JSON 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="jsonContent">JSON 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromJsonAsync(
        string jsonContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// XML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="xmlContent">XML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromXmlAsync(
        string xmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 일반 텍스트 콘텐츠를 처리합니다.
    /// </summary>
    /// <param name="textContent">텍스트 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromTextAsync(
        string textContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 콘텐츠 유형을 자동으로 감지하여 추출합니다.
    /// </summary>
    /// <param name="content">콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="contentType">콘텐츠 타입 힌트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractAutoAsync(
        string content,
        string sourceUrl,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지원하는 콘텐츠 타입 목록을 반환합니다.
    /// </summary>
    /// <returns>지원하는 MIME 타입 목록</returns>
    IReadOnlyList<string> GetSupportedContentTypes();

    /// <summary>
    /// 추출 통계를 반환합니다.
    /// </summary>
    /// <returns>추출 통계 정보</returns>
    ExtractionStatistics GetStatistics();
}


