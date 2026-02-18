using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// 콘텐츠 재구성 옵션
/// Stage 3 (Reconstruct) 단계 설정
/// </summary>
public class ReconstructOptions : IValidatable
{
    /// <summary>
    /// 재구성 전략
    /// 옵션: None, Summarize, Expand, Rewrite, Enrich
    /// </summary>
    public string Strategy { get; set; } = "None";

    /// <summary>
    /// LLM 사용 여부
    /// </summary>
    public bool UseLLM { get; set; }

    /// <summary>
    /// 최대 재구성 길이 (문자 수)
    /// null이면 제한 없음
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 최소 재구성 길이 (문자 수)
    /// null이면 제한 없음
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// 재구성 품질 목표 (0.0 ~ 1.0)
    /// </summary>
    public double QualityTarget { get; set; } = 0.8;

    /// <summary>
    /// 추가 컨텍스트 프롬프트
    /// LLM에 제공될 추가 지시사항
    /// </summary>
    public string? ContextPrompt { get; set; }

    /// <summary>
    /// 요약 비율 (Summarize 전략 사용 시)
    /// 0.0 ~ 1.0, 기본값 0.3 (30% 길이로 요약)
    /// </summary>
    public double SummaryRatio { get; set; } = 0.3;

    /// <summary>
    /// 확장 비율 (Expand 전략 사용 시)
    /// 1.0 이상, 기본값 1.5 (150% 길이로 확장)
    /// </summary>
    public double ExpansionRatio { get; set; } = 1.5;

    /// <summary>
    /// 재작성 스타일 (Rewrite 전략 사용 시)
    /// 옵션: Formal, Casual, Technical, Simple
    /// </summary>
    public string RewriteStyle { get; set; } = "Technical";

    /// <summary>
    /// 증강 타입 목록 (Enrich 전략 사용 시)
    /// 옵션: Context, Definitions, Examples, RelatedInfo
    /// </summary>
    public List<string> EnrichmentTypes { get; set; } = new()
    {
        "Context",
        "Definitions"
    };

    /// <summary>
    /// 원본 콘텐츠 유지 여부
    /// </summary>
    public bool PreserveOriginal { get; set; } = true;

    /// <summary>
    /// 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMs { get; set; } = 60000; // 60초 (LLM 호출 고려)

    /// <summary>
    /// LLM 최대 토큰 수
    /// </summary>
    public int? MaxTokens { get; set; } = 2000;

    /// <summary>
    /// LLM Temperature (창의성 조절)
    /// </summary>
    public double Temperature { get; set; } = 0.3;

    /// <summary>
    /// 추가 옵션 (확장 가능)
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (QualityTarget < 0 || QualityTarget > 1)
            errors.Add("QualityTarget must be between 0 and 1");

        if (SummaryRatio <= 0 || SummaryRatio > 1)
            errors.Add("SummaryRatio must be between 0 (exclusive) and 1 (inclusive)");

        if (ExpansionRatio < 1.0)
            errors.Add("ExpansionRatio must be greater than or equal to 1.0");

        if (Temperature < 0 || Temperature > 2)
            errors.Add("Temperature must be between 0 and 2");

        if (TimeoutMs <= 0)
            errors.Add("TimeoutMs must be greater than 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
