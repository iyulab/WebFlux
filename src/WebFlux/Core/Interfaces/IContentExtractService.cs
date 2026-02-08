using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 추출 서비스 인터페이스 (ISP 분리)
/// 청킹 없는 경량 텍스트 추출 API
/// </summary>
public interface IContentExtractService
{
    /// <summary>
    /// 단일 URL에서 콘텐츠를 추출합니다 (청킹 없음)
    /// </summary>
    Task<ProcessingResult<ExtractedContent>> ExtractContentAsync(
        string url,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 URL에서 콘텐츠를 배치 추출합니다
    /// </summary>
    Task<BatchExtractResult> ExtractBatchAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 URL에서 콘텐츠를 스트리밍으로 배치 추출합니다
    /// </summary>
    IAsyncEnumerable<ProcessingResult<ExtractedContent>> ExtractBatchStreamAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);
}
