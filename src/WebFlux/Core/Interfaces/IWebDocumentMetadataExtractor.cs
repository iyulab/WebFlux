using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 웹 문서에서 구조화된 메타데이터를 추출하는 인터페이스
/// SEO, Open Graph, Schema.org, Breadcrumbs 등 웹 표준 메타데이터 추출
/// </summary>
public interface IWebDocumentMetadataExtractor
{
    /// <summary>
    /// HTML 문서에서 메타데이터 추출
    /// </summary>
    /// <param name="html">HTML 콘텐츠</param>
    /// <param name="url">문서 URL</param>
    /// <param name="httpHeaders">HTTP 응답 헤더 (Content-Language 등)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 웹 문서 메타데이터</returns>
    Task<WebDocumentMetadata> ExtractAsync(
        string html,
        string url,
        IReadOnlyDictionary<string, string>? httpHeaders = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 HTML 문서에서 메타데이터 배치 추출
    /// </summary>
    /// <param name="documents">HTML 문서 목록 (html, url 쌍)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 웹 문서 메타데이터 목록</returns>
    Task<IReadOnlyList<WebDocumentMetadata>> ExtractBatchAsync(
        IEnumerable<(string html, string url, IReadOnlyDictionary<string, string>? httpHeaders)> documents,
        CancellationToken cancellationToken = default);
}
