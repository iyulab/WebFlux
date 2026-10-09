namespace WebFlux.Core.Models;

/// <summary>
/// 청킹 전략 열거형
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>자동 선택</summary>
    Auto,
    /// <summary>고정 크기</summary>
    FixedSize,
    /// <summary>단락 기반</summary>
    Paragraph,
    /// <summary>의미 기반</summary>
    Semantic,
    /// <summary>지능형</summary>
    Smart,
    /// <summary>메모리 최적화</summary>
    MemoryOptimized,
    /// <summary>인텔리전트</summary>
    Intelligent
}

/// <summary>
/// 콘텐츠 분석 결과
/// </summary>
public class ContentAnalysis
{
    /// <summary>복잡성 점수 (0-1)</summary>
    public double ComplexityScore { get; set; }

    /// <summary>구조 점수 (0-1)</summary>
    public double StructureScore { get; set; }

    /// <summary>토큰 수</summary>
    public int TokenCount { get; set; }

    /// <summary>감지된 언어</summary>
    public string DetectedLanguage { get; set; } = string.Empty;

    /// <summary>기술적 콘텐츠 여부</summary>
    public bool IsTechnical { get; set; }

    /// <summary>다중 모달 콘텐츠 여부</summary>
    public bool IsMultimodal { get; set; }

    /// <summary>분석 신뢰도</summary>
    public double Confidence { get; set; }

    /// <summary>세부 분석 결과</summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
