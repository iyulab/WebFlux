using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 멀티모달 처리 파이프라인 인터페이스 (Phase 5A.2)
/// 텍스트와 이미지를 통합하여 고품질 RAG용 콘텐츠로 변환
/// </summary>
public interface IMultimodalProcessingPipeline
{
    /// <summary>
    /// 추출된 콘텐츠를 멀티모달 처리하여 통합 텍스트로 변환합니다.
    /// </summary>
    /// <param name="content">처리할 추출된 콘텐츠</param>
    /// <param name="options">멀티모달 처리 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>멀티모달 처리 결과</returns>
    Task<MultimodalProcessingResult> ProcessAsync(
        ExtractedContent content,
        MultimodalProcessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 처리 통계를 반환합니다.
    /// </summary>
    /// <returns>멀티모달 처리 통계</returns>
    MultimodalStatistics GetStatistics();
}