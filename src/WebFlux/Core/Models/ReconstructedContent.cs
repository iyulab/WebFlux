namespace WebFlux.Core.Models;

/// <summary>
/// 재구성된 콘텐츠 (Reconstruct 단계 출력)
/// Stage 3: 전략에 따른 재작성 및 LLM 활용 증강
/// </summary>
public class ReconstructedContent
{
    /// <summary>
    /// 원본 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 원본 콘텐츠 (보존)
    /// </summary>
    public string OriginalContent { get; set; } = string.Empty;

    /// <summary>
    /// 재구성된 텍스트
    /// </summary>
    public string ReconstructedText { get; set; } = string.Empty;

    /// <summary>
    /// 사용된 재구성 전략
    /// </summary>
    public string StrategyUsed { get; set; } = "None";

    /// <summary>
    /// 적용된 증강 목록
    /// </summary>
    public List<ContentEnhancement> Enhancements { get; set; } = new();

    /// <summary>
    /// 웹 콘텐츠 메타데이터
    /// </summary>
    public WebContentMetadata Metadata { get; set; } = new();

    /// <summary>
    /// 재구성 품질 지표
    /// </summary>
    public ReconstructMetrics Metrics { get; set; } = new();

    /// <summary>
    /// 재구성 시간
    /// </summary>
    public DateTime ReconstructedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 재구성기 정보
    /// </summary>
    public string ReconstructorType { get; set; } = string.Empty;

    /// <summary>
    /// LLM 사용 여부
    /// </summary>
    public bool UsedLLM { get; set; }

    /// <summary>
    /// LLM 모델 정보 (사용된 경우)
    /// </summary>
    public string? LLMModel { get; set; }

    /// <summary>
    /// 추가 속성 (확장 가능)
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// 분석된 콘텐츠에서 재구성된 콘텐츠를 생성합니다 (재구성 없음)
    /// </summary>
    public static ReconstructedContent FromAnalyzed(AnalyzedContent analyzed)
    {
        return new ReconstructedContent
        {
            Url = analyzed.Url,
            OriginalContent = analyzed.CleanedContent,
            ReconstructedText = analyzed.CleanedContent,
            StrategyUsed = "None",
            Metadata = analyzed.Metadata,
            ReconstructedAt = DateTime.UtcNow,
            UsedLLM = false
        };
    }
}

/// <summary>
/// 콘텐츠 증강 정보
/// </summary>
public class ContentEnhancement
{
    /// <summary>증강 ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>증강 타입 (Summary, Expansion, Context, etc.)</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>증강 콘텐츠</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>증강 위치 (원본 콘텐츠에서의 인덱스)</summary>
    public int? Position { get; set; }

    /// <summary>신뢰도 점수 (0.0 ~ 1.0)</summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>증강 메타데이터</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public ContentEnhancement() { }

    public ContentEnhancement(string type, string content, int? position = null)
    {
        Type = type;
        Content = content;
        Position = position;
    }
}

/// <summary>
/// 재구성 품질 지표
/// </summary>
public class ReconstructMetrics
{
    /// <summary>재구성 품질 (0.0 ~ 1.0)</summary>
    public double Quality { get; set; }

    /// <summary>원본 대비 압축률 (0.0 ~ 1.0, 1.0 = 압축 없음)</summary>
    public double CompressionRatio { get; set; }

    /// <summary>증강된 정보량 (바이트)</summary>
    public long EnhancementBytes { get; set; }

    /// <summary>재구성 처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>LLM 호출 횟수</summary>
    public int LLMCallCount { get; set; }

    /// <summary>LLM 토큰 사용량</summary>
    public int? TokensUsed { get; set; }

    /// <summary>추가 지표</summary>
    public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
}
