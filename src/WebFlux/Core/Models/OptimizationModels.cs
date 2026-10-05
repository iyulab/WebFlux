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
/// <summary>
/// 캐시 전략
/// </summary>
/// <summary>
/// 처리 전략
/// </summary>
/// <summary>
/// 청킹 성능 정보
/// </summary>
/// <summary>
/// 최적화 유형
/// </summary>
/// <summary>
/// 최적화 제안
/// </summary>
/// <summary>
/// 대안 전략
/// </summary>
/// <summary>
/// 청킹 전략 추천 결과
/// </summary>
/// <summary>
/// 콘텐츠 메타데이터
/// 콘텐츠 분석 및 최적화에 필요한 메타정보
/// </summary>
/// <summary>
/// 청킹 전략 열거형
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

/// <summary>
/// 시스템 메트릭
/// </summary>
/// <summary>
/// 콘텐츠 분석 결과
/// </summary>
/// <summary>
/// 캐시 전략
/// </summary>
/// <summary>
/// 처리 전략
/// </summary>
/// <summary>
/// 청킹 성능 정보
/// </summary>
/// <summary>
/// 최적화 유형
/// </summary>
/// <summary>
/// 최적화 제안
/// </summary>
/// <summary>
/// 대안 전략
/// </summary>
/// <summary>
/// 청킹 전략 추천 결과
/// </summary>
/// <summary>
/// 콘텐츠 메타데이터
/// 콘텐츠 분석 및 최적화에 필요한 메타정보
/// </summary>
/// <summary>
/// 청킹 전략 열거형
/// </summary>
public class SystemMetrics
{
    /// <summary>CPU 사용률 (%)</summary>
    public double CpuUsage { get; set; }

    /// <summary>메모리 사용량 (바이트)</summary>
    public long MemoryUsage { get; set; }

    /// <summary>가용 메모리 (바이트)</summary>
    public long AvailableMemory { get; set; }

    /// <summary>GC 컬렉션 수</summary>
    public long GarbageCollections { get; set; }

    /// <summary>스레드 수</summary>
    public int ThreadCount { get; set; }

    /// <summary>디스크 I/O 바이트</summary>
    public long DiskIOBytes { get; set; }

    /// <summary>네트워크 I/O 바이트</summary>
    public long NetworkIOBytes { get; set; }

    /// <summary>측정 시간</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}