using Flux.Abstractions;
using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// WebFlux LLM service contract. Extends <see cref="ITextCompletionService"/> with
/// health-check capabilities for service lifecycle management.
/// </summary>
public interface IWebLlmService : ITextCompletionService
{
    /// <summary>
    /// Checks whether the service is available and ready to handle requests.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns current health and configuration status of the service.
    /// </summary>
    ServiceHealthInfo GetHealthInfo();
}
