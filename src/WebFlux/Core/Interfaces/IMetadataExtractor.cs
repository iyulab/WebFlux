using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 메타데이터 추출 인터페이스
/// 웹 표준 메타데이터를 추출합니다
/// </summary>
public interface IMetadataExtractor
{
    /// <summary>
    /// HTML 콘텐츠에서 메타데이터를 추출합니다
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 메타데이터</returns>
    Task<WebMetadata> ExtractMetadataAsync(
        string htmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 메타데이터 완성도를 평가합니다
    /// </summary>
    /// <param name="metadata">메타데이터</param>
    /// <returns>완성도 정보</returns>
    MetadataCompleteness EvaluateCompleteness(WebMetadata metadata);
}

/// <summary>
/// 메타데이터 완성도 정보
/// </summary>
public class MetadataCompleteness
{
    /// <summary>전체 완성도 점수 (0.0 - 1.0)</summary>
    public double OverallScore { get; init; }

    /// <summary>기본 메타데이터 완성도</summary>
    public double BasicMetadataScore { get; init; }

    /// <summary>Open Graph 완성도</summary>
    public double OpenGraphScore { get; init; }

    /// <summary>Twitter Cards 완성도</summary>
    public double TwitterCardsScore { get; init; }

    /// <summary>Schema.org 완성도</summary>
    public double SchemaOrgScore { get; init; }

    /// <summary>누락된 중요 메타데이터 목록</summary>
    public IReadOnlyList<string> MissingCriticalFields { get; init; } = Array.Empty<string>();

    /// <summary>권장 개선사항</summary>
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
}