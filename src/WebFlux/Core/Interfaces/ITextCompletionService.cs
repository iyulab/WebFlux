using Flux.Abstractions;
using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// WebFlux text completion service extending the canonical Flux.Abstractions contract.
/// Adds health check and availability methods specific to WebFlux's service lifecycle.
/// </summary>
public interface ITextCompletionService : Flux.Abstractions.ITextCompletionService
{
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
