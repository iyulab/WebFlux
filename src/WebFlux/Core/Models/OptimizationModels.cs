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

