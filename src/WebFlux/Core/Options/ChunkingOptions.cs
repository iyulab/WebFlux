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
    /// Semantic 전략의 경계 민감도 — 인접 문장의 코사인 유사도가 이 값보다 낮으면 경계를 둔다(기본값: 0.5).
    /// FluxCurator 의 <c>SemanticSimilarityThreshold</c> 로 전달된다. 다른 전략은 읽지 않는다.
    /// </summary>
    /// <remarks>0.19.0 전에는 어디에도 전달되지 않았다(기본 0.7 은 효력이 없었고 실제 경계는 FluxCurator 기본 0.5 였다).</remarks>
    public double SemanticThreshold { get; set; } = 0.5;

    /// <summary>
    /// 헤더 정보 보존 여부 (기본값: true)
    /// </summary>
    public bool PreserveHeaders { get; set; } = true;

    /// <summary>
    /// 언어별 토큰화 설정
    /// </summary>
    public string Language { get; set; } = "ko";

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