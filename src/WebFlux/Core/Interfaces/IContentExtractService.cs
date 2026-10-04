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
    /// <returns>추출된 콘텐츠.</returns>
    /// <exception cref="WebExtractionException">추출 실패 — 종류는 <see cref="WebExtractionException.ErrorCode"/>
    /// (<see cref="ExtractErrorCodes"/>: 잘못된 URL · robots.txt 거부 · 타임아웃 · HTTP 오류 · 빈 콘텐츠 …).</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/>이 취소됨.</exception>
    Task<ExtractedContent> ExtractContentAsync(
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
    /// 여러 URL에서 콘텐츠를 스트리밍으로 배치 추출합니다 — 끝나는 순서대로 URL마다 하나의 항목(성공이면
    /// <see cref="ExtractStreamItem.Content"/>, 실패면 <see cref="ExtractStreamItem.Failure"/>).
    /// </summary>
    IAsyncEnumerable<ExtractStreamItem> ExtractBatchStreamAsync(
        IEnumerable<string> urls,
        ExtractOptions? options = null,
        CancellationToken cancellationToken = default);
}
