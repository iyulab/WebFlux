namespace WebFlux.Core.Interfaces;

/// <summary>
/// 토큰 계산 서비스 인터페이스
/// 텍스트의 토큰 수를 계산하고 청킹 크기 최적화를 지원합니다
/// </summary>
public interface ITokenCountService
{
    /// <summary>
    /// 텍스트의 토큰 수를 계산합니다
    /// </summary>
    /// <param name="text">토큰 수를 계산할 텍스트</param>
    /// <returns>토큰 수</returns>
    int CountTokens(string text);

    /// <summary>
    /// 텍스트의 토큰 수를 비동기로 계산합니다
    /// </summary>
    /// <param name="text">토큰 수를 계산할 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>토큰 수</returns>
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 텍스트의 토큰 수를 일괄 계산합니다
    /// </summary>
    /// <param name="texts">텍스트 목록</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>각 텍스트의 토큰 수</returns>
    Task<int[]> CountTokensBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// 최대 토큰 수에 맞도록 텍스트를 자릅니다
    /// </summary>
    /// <param name="text">원본 텍스트</param>
    /// <param name="maxTokens">최대 토큰 수</param>
    /// <returns>잘린 텍스트</returns>
    string TruncateToTokenLimit(string text, int maxTokens);

    /// <summary>
    /// 사용 중인 토크나이저 모델명
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// 예상 토큰 수 (정확한 계산 전 빠른 추정)
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <returns>예상 토큰 수</returns>
    int EstimateTokens(string text);
}