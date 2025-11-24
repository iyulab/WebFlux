using WebFlux.Core.Models;
using ChunkingOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 청킹 전략 인터페이스 - Interface Provider 패턴
/// 간단하고 명확한 청킹 계약 정의
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// 전략 이름
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 전략 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 추출된 콘텐츠를 청크로 분할합니다.
    /// </summary>
    /// <param name="content">추출된 콘텐츠</param>
    /// <param name="options">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 청크 목록</returns>
    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);
}