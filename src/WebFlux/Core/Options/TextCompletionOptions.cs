using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// WebFlux-specific text completion options extending the canonical Flux.Abstractions.TextCompletionOptions.
/// Adds validation and additional metadata support.
/// </summary>
/// <remarks>
/// Breaking change from v0.4.x: Temperature/TopP/FrequencyPenalty/PresencePenalty
/// changed from <c>double</c> to <c>float</c> (consistent with Flux.Abstractions).
/// </remarks>
public class TextCompletionOptions : Flux.Abstractions.TextCompletionOptions, IValidatable
{
    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxTokens <= 0)
            errors.Add("MaxTokens must be greater than 0");

        if (Temperature < 0 || Temperature > 2)
            errors.Add("Temperature must be between 0 and 2");

        if (TopP is < 0 or > 1)
            errors.Add("TopP must be between 0 and 1");

        if (FrequencyPenalty is < -2 or > 2)
            errors.Add("FrequencyPenalty must be between -2 and 2");

        if (PresencePenalty is < -2 or > 2)
            errors.Add("PresencePenalty must be between -2 and 2");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
