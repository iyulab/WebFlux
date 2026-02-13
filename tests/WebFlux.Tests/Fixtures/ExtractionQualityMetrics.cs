namespace WebFlux.Tests.Fixtures;

/// <summary>
/// 추출 결과의 품질을 정량적으로 측정하는 메트릭 모델
/// </summary>
public class ExtractionQualityMetrics
{
    /// <summary>
    /// 구조 보존율: 헤딩/테이블/리스트/코드블록 보존 비율 (0.0~1.0)
    /// </summary>
    public double StructurePreservation { get; set; }

    /// <summary>
    /// 핵심 콘텐츠 포함율: article/main 내부 텍스트 포함 비율 (0.0~1.0)
    /// </summary>
    public double ContentCompleteness { get; set; }

    /// <summary>
    /// 노이즈 제거율: nav/footer/광고/댓글 등 비콘텐츠 제거 비율 (0.0~1.0)
    /// </summary>
    public double NoiseRemoval { get; set; }

    /// <summary>
    /// Markdown 유효성: 유효한 Markdown 문법 비율 (0.0~1.0)
    /// </summary>
    public double MarkdownValidity { get; set; }

    /// <summary>
    /// 가중 종합 점수 (0.0~1.0)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// 발견된 문제점 목록
    /// </summary>
    public List<string> Issues { get; set; } = new();

    public override string ToString()
    {
        return $"Quality[Overall={OverallScore:F2}, Structure={StructurePreservation:F2}, " +
               $"Content={ContentCompleteness:F2}, Noise={NoiseRemoval:F2}, " +
               $"Markdown={MarkdownValidity:F2}, Issues={Issues.Count}]";
    }
}
