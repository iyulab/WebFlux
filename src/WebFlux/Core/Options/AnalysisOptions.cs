using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// 콘텐츠 분석 옵션
/// Stage 2 (Analyze) 단계 설정
/// </summary>
public class AnalysisOptions : IValidatable
{
    /// <summary>
    /// 노이즈 제거 활성화
    /// </summary>
    public bool RemoveNoise { get; set; } = true;

    /// <summary>
    /// 노이즈로 간주할 요소 (CSS 선택자 또는 태그명)
    /// </summary>
    public List<string> NoiseSelectors { get; set; } = new()
    {
        "nav",
        "header",
        "footer",
        ".advertisement",
        ".sidebar",
        "#comments"
    };

    /// <summary>
    /// 구조 분석 활성화
    /// </summary>
    public bool AnalyzeStructure { get; set; } = true;

    /// <summary>
    /// 섹션 추출 활성화
    /// </summary>
    public bool ExtractSections { get; set; } = true;

    /// <summary>
    /// 표 추출 활성화
    /// </summary>
    public bool ExtractTables { get; set; } = true;

    /// <summary>
    /// 이미지 분석 활성화
    /// </summary>
    public bool AnalyzeImages { get; set; } = true;

    /// <summary>
    /// 메타데이터 보강 활성화
    /// </summary>
    public bool EnrichMetadata { get; set; } = true;

    /// <summary>
    /// 최소 콘텐츠 품질 (0.0 ~ 1.0)
    /// 이 값 이하의 품질은 분석 실패로 처리
    /// </summary>
    public double MinContentQuality { get; set; } = 0.5;

    /// <summary>
    /// 최소 섹션 길이 (문자 수)
    /// 이보다 짧은 섹션은 병합
    /// </summary>
    public int MinSectionLength { get; set; } = 50;

    /// <summary>
    /// 최대 섹션 깊이
    /// 이보다 깊은 섹션은 평탄화
    /// </summary>
    public int MaxSectionDepth { get; set; } = 6;

    /// <summary>
    /// 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMs { get; set; } = 30000; // 30초

    /// <summary>
    /// 추가 옵션 (확장 가능)
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MinContentQuality < 0 || MinContentQuality > 1)
            errors.Add("MinContentQuality must be between 0 and 1");

        if (MinSectionLength <= 0)
            errors.Add("MinSectionLength must be greater than 0");

        if (MaxSectionDepth <= 0)
            errors.Add("MaxSectionDepth must be greater than 0");

        if (TimeoutMs <= 0)
            errors.Add("TimeoutMs must be greater than 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
