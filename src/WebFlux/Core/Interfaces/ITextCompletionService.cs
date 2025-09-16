using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// LLM 텍스트 완성 서비스 인터페이스
/// 소비 애플리케이션에서 OpenAI, Anthropic, Azure, Ollama 등의 구현체 제공
/// </summary>
public interface ITextCompletionService
{
    /// <summary>
    /// 주어진 프롬프트에 대한 텍스트 완성을 수행합니다.
    /// </summary>
    /// <param name="prompt">완성을 요청할 프롬프트</param>
    /// <param name="options">완성 옵션 (토큰 수, 온도 등)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>완성된 텍스트</returns>
    Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스트리밍 방식으로 텍스트 완성을 수행합니다.
    /// </summary>
    /// <param name="prompt">완성을 요청할 프롬프트</param>
    /// <param name="options">완성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>완성된 텍스트 스트림</returns>
    IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 프롬프트에 대한 배치 처리를 수행합니다.
    /// </summary>
    /// <param name="prompts">처리할 프롬프트 목록</param>
    /// <param name="options">완성 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>완성된 텍스트 목록</returns>
    Task<IReadOnlyList<string>> CompleteBatchAsync(
        IEnumerable<string> prompts,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 서비스의 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사용 가능 여부</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 서비스의 현재 상태 정보를 반환합니다.
    /// </summary>
    /// <returns>상태 정보</returns>
    ServiceHealthInfo GetHealthInfo();
}