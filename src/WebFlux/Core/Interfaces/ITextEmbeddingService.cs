namespace WebFlux.Core.Interfaces;

/// <summary>
/// 텍스트 임베딩 서비스 인터페이스
/// 텍스트를 벡터 임베딩으로 변환하여 의미적 유사성 계산 지원
/// </summary>
public interface ITextEmbeddingService
{
    /// <summary>
    /// 텍스트를 임베딩 벡터로 변환합니다.
    /// </summary>
    /// <param name="text">임베딩할 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>임베딩 벡터</returns>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 텍스트를 배치로 임베딩 벡터로 변환합니다.
    /// </summary>
    /// <param name="texts">임베딩할 텍스트 목록</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>임베딩 벡터 목록</returns>
    Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// 지원하는 최대 토큰 수를 반환합니다.
    /// </summary>
    int MaxTokens { get; }

    /// <summary>
    /// 임베딩 벡터의 차원 수를 반환합니다.
    /// </summary>
    int EmbeddingDimension { get; }
}