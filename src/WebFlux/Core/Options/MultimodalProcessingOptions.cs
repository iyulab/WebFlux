namespace WebFlux.Core.Options;

/// <summary>
/// 멀티모달 처리 옵션 (Phase 5A.2)
/// </summary>
public class MultimodalProcessingOptions
{
    /// <summary>처리할 최대 이미지 수 (기본값: 10)</summary>
    public int MaxImages { get; set; } = 10;

    /// <summary>이미지-텍스트 변환 옵션</summary>
    public ImageToTextOptions? ImageToTextOptions { get; set; }

    /// <summary>텍스트 통합 전략</summary>
    public TextIntegrationStrategy TextIntegrationStrategy { get; set; } = TextIntegrationStrategy.Append;

    /// <summary>이미지 텍스트 포맷</summary>
    public ImageTextFormat ImageTextFormat { get; set; } = ImageTextFormat.Inline;

    /// <summary>최소 신뢰도 임계값 (0.0 ~ 1.0)</summary>
    public double MinimumConfidence { get; set; } = 0.3;

    /// <summary>병렬 처리 활성화 여부</summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>최대 동시 처리 이미지 수</summary>
    public int MaxConcurrentImages { get; set; } = 3;

    /// <summary>처리 타임아웃 (초)</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>이미지 우선순위 가중치</summary>
    public ImagePriorityWeights PriorityWeights { get; set; } = new();

    /// <summary>실패 시 재시도 횟수</summary>
    public int RetryCount { get; set; } = 1;

    /// <summary>품질 향상 모드 활성화</summary>
    public bool EnableQualityEnhancement { get; set; } = true;
}

/// <summary>
/// 텍스트 통합 전략
/// </summary>
public enum TextIntegrationStrategy
{
    /// <summary>이미지 텍스트를 원본 텍스트 뒤에 추가</summary>
    Append,

    /// <summary>텍스트와 이미지를 인터리브 방식으로 통합</summary>
    Interleave,

    /// <summary>구조화된 섹션으로 분리</summary>
    Structured
}

/// <summary>
/// 이미지 텍스트 포맷
/// </summary>
public enum ImageTextFormat
{
    /// <summary>인라인 형태: [Image: 설명]</summary>
    Inline,

    /// <summary>주석 포함: [Image (confidence: high): 설명]</summary>
    Annotated,

    /// <summary>구조화된 형태: ## Image Content\n설명</summary>
    Structured,

    /// <summary>원본 텍스트만</summary>
    Raw
}

/// <summary>
/// 이미지 우선순위 가중치
/// </summary>
public class ImagePriorityWeights
{
    /// <summary>컨텍스트 정보 가중치</summary>
    public double ContextWeight { get; set; } = 0.3;

    /// <summary>Alt 텍스트 가중치</summary>
    public double AltTextWeight { get; set; } = 0.2;

    /// <summary>위치 가중치 (상단 우선)</summary>
    public double PositionWeight { get; set; } = 0.2;

    /// <summary>형식 가중치</summary>
    public double FormatWeight { get; set; } = 0.1;

    /// <summary>크기 가중치</summary>
    public double SizeWeight { get; set; } = 0.2;
}