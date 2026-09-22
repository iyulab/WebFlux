using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// 청킹 옵션을 정의하는 클래스
/// </summary>
public class ChunkingOptions : IValidatable
{
    /// <summary>
    /// 청킹 전략 유형
    /// </summary>
    public ChunkingStrategyType Strategy { get; set; } = ChunkingStrategyType.Auto;

    /// <summary>
    /// 청크의 최대 크기 (토큰 수, 기본값: 512)
    /// </summary>
    public int MaxChunkSize { get; set; } = 512;

    /// <summary>
    /// 청크 간 겹치는 부분 크기 (토큰 수, 기본값: 50)
    /// </summary>
    public int ChunkOverlap { get; set; } = 50;

    /// <summary>
    /// 최소 청크 크기 (토큰 수, 기본값: 50)
    /// </summary>
    public int MinChunkSize { get; set; } = 50;

    /// <summary>
    /// 의미론적 청킹 임계값 (코사인 유사도, 기본값: 0.7)
    /// </summary>
    public double SemanticThreshold { get; set; } = 0.7;

    /// <summary>
    /// 품질 점수 임계값 (기본값: 0.6)
    /// </summary>
    public double QualityThreshold { get; set; } = 0.6;

    /// <summary>
    /// 헤더 정보 보존 여부 (기본값: true)
    /// </summary>
    public bool PreserveHeaders { get; set; } = true;

    /// <summary>
    /// 최대 병렬 작업 수 (기본값: Environment.ProcessorCount)
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 언어별 토큰화 설정
    /// </summary>
    public string Language { get; set; } = "ko";

    /// <summary>
    /// 메모리 사용량 최소화 여부 (기본값: false). 전략 팩토리가 읽어 메모리 최적화 전략을 고른다. 0.14.0 이전의 별칭
    /// <c>UseMemoryOptimization</c> 은 제거됐다 — 이 멤버가 실제 값이다.
    /// </summary>
    public bool MinimizeMemoryUsage { get; set; }

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxChunkSize <= 0)
            errors.Add("MaxChunkSize must be greater than 0");

        if (MinChunkSize <= 0)
            errors.Add("MinChunkSize must be greater than 0");

        if (MaxChunkSize <= MinChunkSize)
            errors.Add("MaxChunkSize must be greater than MinChunkSize");

        if (ChunkOverlap < 0)
            errors.Add("ChunkOverlap must be greater than or equal to 0");

        if (ChunkOverlap >= MaxChunkSize)
            errors.Add("ChunkOverlap must be less than MaxChunkSize");

        if (SemanticThreshold < 0 || SemanticThreshold > 1)
            errors.Add("SemanticThreshold must be between 0 and 1");

        if (QualityThreshold < 0 || QualityThreshold > 1)
            errors.Add("QualityThreshold must be between 0 and 1");

        if (MaxParallelism <= 0)
            errors.Add("MaxParallelism must be greater than 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// 청킹 전략 유형 열거형 — 멤버마다 등록된 전략이 하나씩 있다(<see cref="Interfaces.IChunkingStrategyFactory.GetAvailableStrategies"/>).
/// </summary>
/// <remarks>
/// 서수는 명시한다. 0.14.0 에서 구현이 없던 <c>Intelligent</c>(5)를 지웠고 그 자리는 비워 둔다 — 숫자로 바인딩된
/// 설정(<c>"Strategy": 6</c>)이 조용히 다른 전략을 가리키지 않게 하려는 것이다.
/// </remarks>
public enum ChunkingStrategyType
{
    /// <summary>자동 선택 (콘텐츠 분석 기반)</summary>
    Auto = 0,
    /// <summary>고정 크기 분할</summary>
    FixedSize = 1,
    /// <summary>문단 기반 분할</summary>
    Paragraph = 2,
    /// <summary>구조 인식 분할 (헤더 기반)</summary>
    Smart = 3,
    /// <summary>의미론적 분할 (임베딩 기반)</summary>
    Semantic = 4,
    /// <summary>메모리 최적화 분할</summary>
    MemoryOptimized = 6
}