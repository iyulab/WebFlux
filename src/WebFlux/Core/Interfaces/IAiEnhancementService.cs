using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// AI 기반 콘텐츠 증강 서비스 인터페이스
/// 요약, 재작성, 메타데이터 추출 등의 AI 처리 제공
/// </summary>
public interface IAiEnhancementService
{
    /// <summary>
    /// 콘텐츠를 요약합니다.
    /// </summary>
    /// <param name="content">요약할 콘텐츠</param>
    /// <param name="options">요약 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>요약된 콘텐츠</returns>
    Task<string> SummarizeAsync(
        string content,
        SummaryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 콘텐츠를 가독성 향상을 위해 재작성합니다.
    /// </summary>
    /// <param name="content">재작성할 콘텐츠</param>
    /// <param name="options">재작성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>재작성된 콘텐츠</returns>
    Task<string> RewriteAsync(
        string content,
        RewriteOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// AI를 활용하여 풍부한 메타데이터를 추출합니다 (HTML + AI 융합).
    /// </summary>
    /// <param name="content">메타데이터를 추출할 콘텐츠</param>
    /// <param name="options">메타데이터 추출 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 풍부한 메타데이터</returns>
    Task<EnrichedMetadata> ExtractMetadataAsync(
        string content,
        MetadataExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 콘텐츠를 증강합니다 (요약 + 재작성 + 메타데이터).
    /// 활성화된 옵션에 따라 선택적으로 처리합니다.
    /// </summary>
    /// <param name="content">증강할 콘텐츠</param>
    /// <param name="options">증강 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>증강된 콘텐츠</returns>
    Task<EnhancedContent> EnhanceAsync(
        string content,
        EnhancementOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 콘텐츠를 배치로 증강합니다.
    /// </summary>
    /// <param name="contents">증강할 콘텐츠 목록</param>
    /// <param name="options">증강 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>증강된 콘텐츠 목록</returns>
    Task<IReadOnlyList<EnhancedContent>> EnhanceBatchAsync(
        IEnumerable<string> contents,
        EnhancementOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 서비스의 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사용 가능 여부</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
