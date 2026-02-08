using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// 통합 파이프라인 옵션
/// 모든 4단계 파이프라인 설정 통합
/// </summary>
public class PipelineOptions : IValidatable
{
    /// <summary>
    /// 크롤링 옵션 (Extract 단계 일부)
    /// </summary>
    public CrawlOptions? Crawl { get; set; }

    /// <summary>
    /// 추출 옵션 (Extract 단계)
    /// </summary>
    public ExtractionOptions? Extraction { get; set; }

    /// <summary>
    /// 분석 옵션 (Analyze 단계)
    /// </summary>
    public AnalysisOptions? Analysis { get; set; }

    /// <summary>
    /// 재구성 옵션 (Reconstruct 단계)
    /// </summary>
    public ReconstructOptions? Reconstruction { get; set; }

    /// <summary>
    /// 청킹 옵션 (Chunk 단계)
    /// </summary>
    public ChunkingOptions? Chunking { get; set; }

    /// <summary>
    /// 파이프라인 단계 활성화/비활성화
    /// </summary>
    public PipelineStageFlags EnabledStages { get; set; } = PipelineStageFlags.All;

    /// <summary>
    /// 진행률 보고 간격 (밀리초)
    /// </summary>
    public int ProgressReportIntervalMs { get; set; } = 1000;

    /// <summary>
    /// 최대 병렬 처리 수
    /// </summary>
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 전체 타임아웃 (밀리초)
    /// </summary>
    public int TotalTimeoutMs { get; set; } = 300000; // 5분

    /// <summary>
    /// 추가 옵션 (확장 가능)
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxConcurrency <= 0)
            errors.Add("MaxConcurrency must be greater than 0");

        if (TotalTimeoutMs <= 0)
            errors.Add("TotalTimeoutMs must be greater than 0");

        if (ProgressReportIntervalMs <= 0)
            errors.Add("ProgressReportIntervalMs must be greater than 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// 파이프라인 단계 플래그
/// </summary>
[Flags]
public enum PipelineStageFlags
{
    /// <summary>모든 단계 비활성화</summary>
    None = 0,

    /// <summary>Extract 단계</summary>
    Extract = 1,

    /// <summary>Analyze 단계</summary>
    Analyze = 2,

    /// <summary>Reconstruct 단계</summary>
    Reconstruct = 4,

    /// <summary>Chunk 단계</summary>
    Chunk = 8,

    /// <summary>모든 단계 활성화</summary>
    All = Extract | Analyze | Reconstruct | Chunk,

    /// <summary>Reconstruct 제외 (빠른 처리)</summary>
    WithoutReconstruct = Extract | Analyze | Chunk
}

/// <summary>
/// 추출 옵션 (Extract 단계)
/// </summary>
public class ExtractionOptions
{
    /// <summary>
    /// 추출 전략
    /// </summary>
    public string Strategy { get; set; } = "Auto";

    /// <summary>
    /// HTTP 타임아웃 (밀리초)
    /// </summary>
    public int HttpTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// User-Agent 문자열
    /// </summary>
    public string UserAgent { get; set; } = "WebFlux/1.0 (RAG Preprocessor)";

    /// <summary>
    /// 메타데이터 추출 활성화
    /// </summary>
    public bool ExtractMetadata { get; set; } = true;

    /// <summary>
    /// 이미지 URL 수집 활성화
    /// </summary>
    public bool CollectImageUrls { get; set; } = true;

    /// <summary>
    /// 링크 수집 활성화
    /// </summary>
    public bool CollectLinks { get; set; } = true;

    /// <summary>
    /// 추가 옵션
    /// </summary>
    public Dictionary<string, object> AdditionalOptions { get; set; } = new();
}
