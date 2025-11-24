namespace WebFlux.Core.Options;

/// <summary>
/// 텍스트 완성 옵션을 정의하는 클래스
/// </summary>
public class TextCompletionOptions
{
    /// <summary>
    /// 최대 생성 토큰 수 (기본값: 1000)
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// 생성 온도 값 (0.0 - 2.0, 기본값: 0.7)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top-p 값 (0.0 - 1.0, 기본값: 0.9)
    /// </summary>
    public double TopP { get; set; } = 0.9;

    /// <summary>
    /// 빈도 페널티 (-2.0 - 2.0, 기본값: 0.0)
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// 존재 페널티 (-2.0 - 2.0, 기본값: 0.0)
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;

    /// <summary>
    /// 시스템 프롬프트 (선택적)
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}