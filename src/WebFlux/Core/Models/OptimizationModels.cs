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
/// 콘텐츠 메타데이터
/// 콘텐츠 분석 및 최적화에 필요한 메타정보
/// </summary>
public class ContentMetadata
{
    /// <summary>콘텐츠 유형</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>파일 크기 (바이트)</summary>
    public long FileSize { get; set; }

    /// <summary>언어</summary>
    public string Language { get; set; } = "ko";

    /// <summary>페이지 수</summary>
    public int PageCount { get; set; }

    /// <summary>이미지 포함 여부</summary>
    public bool HasImages { get; set; }

    /// <summary>기술 문서 여부</summary>
    public bool IsTechnical { get; set; }

    /// <summary>학술 문서 여부</summary>
    public bool IsAcademic { get; set; }

    /// <summary>생성 시간</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>추가 속성</summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 청킹 전략 추천 결과
/// </summary>
public class ChunkingStrategyRecommendation
{
    /// <summary>추천 전략</summary>
    public ChunkingStrategy RecommendedStrategy { get; set; }

    /// <summary>신뢰도 (0-1)</summary>
    public double Confidence { get; set; }

    /// <summary>추천 이유</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>예상 청킹 성능</summary>
    public ChunkingPerformance ExpectedPerformance { get; set; } = new();

    /// <summary>대안 전략들</summary>
    public List<AlternativeStrategy> Alternatives { get; set; } = new();

    /// <summary>최적화 제안</summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// 대안 전략
/// </summary>
public class AlternativeStrategy
{
    /// <summary>전략</summary>
    public ChunkingStrategy Strategy { get; set; }

    /// <summary>점수</summary>
    public double Score { get; set; }

    /// <summary>사용 시나리오</summary>
    public string UseCase { get; set; } = string.Empty;
}

/// <summary>
/// 최적화 제안
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>제안 유형</summary>
    public OptimizationType Type { get; set; }

    /// <summary>제안 내용</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>예상 효과</summary>
    public string ExpectedImpact { get; set; } = string.Empty;

    /// <summary>우선순위</summary>
    public int Priority { get; set; }
}

/// <summary>
/// 최적화 유형
/// </summary>
public enum OptimizationType
{
    /// <summary>성능 최적화</summary>
    Performance,
    /// <summary>품질 최적화</summary>
    Quality,
    /// <summary>메모리 최적화</summary>
    Memory,
    /// <summary>비용 최적화</summary>
    Cost
}

/// <summary>
/// 청킹 성능 정보
/// </summary>
public class ChunkingPerformance
{
    /// <summary>예상 처리 시간</summary>
    public TimeSpan EstimatedProcessingTime { get; set; }

    /// <summary>예상 메모리 사용량 (바이트)</summary>
    public long EstimatedMemoryUsage { get; set; }

    /// <summary>예상 품질 점수</summary>
    public double EstimatedQualityScore { get; set; }

    /// <summary>예상 청크 수</summary>
    public int EstimatedChunkCount { get; set; }
}

/// <summary>
/// 처리 전략
/// </summary>
public class ProcessingStrategy
{
    /// <summary>전략 이름</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>청킹 전략</summary>
    public ChunkingStrategy ChunkingStrategy { get; set; }

    /// <summary>병렬 처리 수준</summary>
    public int ParallelismLevel { get; set; } = 1;

    /// <summary>메모리 최적화 활성화</summary>
    public bool EnableMemoryOptimization { get; set; }

    /// <summary>캐시 전략</summary>
    public CacheStrategy CacheStrategy { get; set; } = CacheStrategy.Normal;

    /// <summary>구성 매개변수</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 캐시 전략
/// </summary>
public enum CacheStrategy
{
    /// <summary>캐시 사용 안함</summary>
    None,
    /// <summary>일반 캐시</summary>
    Normal,
    /// <summary>고성능 캐시</summary>
    High,
    /// <summary>메모리 최적화 캐시</summary>
    MemoryOptimized
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

/// <summary>
/// 시스템 메트릭
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

/// <summary>
/// 토큰 분석 결과
/// </summary>
public class TokenAnalysis
{
    /// <summary>모델별 토큰 수</summary>
    public Dictionary<string, int> TokenCountByModel { get; set; } = new();

    /// <summary>최소 토큰 수</summary>
    public int MinTokens => TokenCountByModel.Values.DefaultIfEmpty(0).Min();

    /// <summary>최대 토큰 수</summary>
    public int MaxTokens => TokenCountByModel.Values.DefaultIfEmpty(0).Max();

    /// <summary>평균 토큰 수</summary>
    public double AverageTokens => TokenCountByModel.Values.DefaultIfEmpty(0).Average();

    /// <summary>추천 모델</summary>
    public string RecommendedModel { get; set; } = string.Empty;

    /// <summary>비용 분석</summary>
    public Dictionary<string, decimal> CostAnalysis { get; set; } = new();

    /// <summary>최적화 제안</summary>
    public List<string> OptimizationSuggestions { get; set; } = new();
}

/// <summary>
/// 캐시 히트 유형
/// </summary>
public enum CacheHitType
{
    /// <summary>미스</summary>
    Miss,
    /// <summary>L1 캐시 히트</summary>
    L1Hit,
    /// <summary>L2 캐시 히트</summary>
    L2Hit
}

/// <summary>
/// 캐시 키 통계
/// </summary>
public class CacheKeyStatistics
{
    /// <summary>키</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>히트 수</summary>
    public long HitCount { get; set; }

    /// <summary>미스 수</summary>
    public long MissCount { get; set; }

    /// <summary>마지막 접근 시간</summary>
    public DateTimeOffset LastAccessTime { get; set; }

    /// <summary>마지막 히트 유형</summary>
    public CacheHitType LastHitType { get; set; }

    /// <summary>평균 액세스 시간</summary>
    public TimeSpan AverageAccessTime { get; set; }

    /// <summary>데이터 크기 (바이트)</summary>
    public long DataSize { get; set; }
}

/// <summary>
/// 캐시 최적화 결과
/// </summary>
public class CacheOptimizationResult
{
    /// <summary>캐시 히트 여부</summary>
    public bool CacheHit { get; init; }

    /// <summary>최적화된 캐시 키</summary>
    public string OptimizedCacheKey { get; init; } = string.Empty;

    /// <summary>권장 TTL</summary>
    public TimeSpan RecommendedTTL { get; init; }

    /// <summary>캐시 크기 절약</summary>
    public long SpaceSavedBytes { get; init; }

    /// <summary>성능 향상 추정치</summary>
    public double PerformanceGainEstimate { get; init; }
}

/// <summary>
/// 리소스 최적화 제안
/// </summary>
public class ResourceOptimizationSuggestion
{
    /// <summary>CPU 사용률</summary>
    public double CpuUtilization { get; init; }

    /// <summary>메모리 사용률</summary>
    public double MemoryUtilization { get; init; }

    /// <summary>최적화 제안 목록</summary>
    public IReadOnlyList<OptimizationSuggestion> Suggestions { get; init; } =
        new List<OptimizationSuggestion>();

    /// <summary>예상 리소스 절약</summary>
    public ResourceSavings ExpectedSavings { get; init; } = new();
}

/// <summary>
/// 리소스 절약 정보
/// </summary>
public class ResourceSavings
{
    /// <summary>CPU 절약 비율</summary>
    public double CpuSavingsPercent { get; init; }

    /// <summary>메모리 절약 비율</summary>
    public double MemorySavingsPercent { get; init; }

    /// <summary>처리 시간 단축 비율</summary>
    public double ProcessingTimeSavingsPercent { get; init; }

    /// <summary>비용 절약 추정치</summary>
    public double EstimatedCostSavings { get; init; }
}

/// <summary>
/// 병목 최적화 결과
/// </summary>
public class BottleneckOptimizationResult
{
    /// <summary>식별된 병목 구간</summary>
    public IReadOnlyList<BottleneckInfo> IdentifiedBottlenecks { get; init; } =
        new List<BottleneckInfo>();

    /// <summary>적용된 최적화</summary>
    public IReadOnlyList<AppliedOptimization> AppliedOptimizations { get; init; } =
        new List<AppliedOptimization>();

    /// <summary>예상 성능 향상</summary>
    public double ExpectedPerformanceImprovement { get; init; }
}

/// <summary>
/// 병목 구간 정보
/// </summary>
public class BottleneckInfo
{
    /// <summary>병목 구간 이름</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>병목 타입</summary>
    public BottleneckType Type { get; init; }

    /// <summary>심각도 (1-10)</summary>
    public int Severity { get; init; }

    /// <summary>평균 처리 시간</summary>
    public TimeSpan AverageProcessingTime { get; init; }

    /// <summary>처리량</summary>
    public double Throughput { get; init; }
}

/// <summary>
/// 병목 타입
/// </summary>
public enum BottleneckType
{
    /// <summary>CPU 병목</summary>
    CPU,
    /// <summary>메모리 병목</summary>
    Memory,
    /// <summary>I/O 병목</summary>
    IO,
    /// <summary>네트워크 병목</summary>
    Network,
    /// <summary>외부 서비스 병목</summary>
    ExternalService
}

/// <summary>
/// 적용된 최적화 정보
/// </summary>
public class AppliedOptimization
{
    /// <summary>최적화 이름</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>최적화 타입</summary>
    public OptimizationType Type { get; init; }

    /// <summary>적용 전 메트릭</summary>
    public double BeforeMetric { get; init; }

    /// <summary>적용 후 메트릭</summary>
    public double AfterMetric { get; init; }

    /// <summary>향상 비율</summary>
    public double ImprovementPercent { get; init; }
}

/// <summary>
/// 토큰 최적화 결과
/// </summary>
public class TokenOptimizationResult
{
    /// <summary>원본 토큰 수</summary>
    public int OriginalTokenCount { get; init; }

    /// <summary>최적화된 토큰 수</summary>
    public int OptimizedTokenCount { get; init; }

    /// <summary>절약된 토큰 수</summary>
    public int TokensSaved { get; init; }

    /// <summary>절약 비율</summary>
    public double SavingsPercent { get; init; }

    /// <summary>최적화된 텍스트</summary>
    public string OptimizedText { get; init; } = string.Empty;

    /// <summary>품질 유지 점수 (0.0 - 1.0)</summary>
    public double QualityRetentionScore { get; init; }
}

/// <summary>
/// 최적화 통계
/// </summary>
public class OptimizationStatistics
{
    /// <summary>총 최적화 요청 수</summary>
    public long TotalOptimizationRequests { get; init; }

    /// <summary>성공한 최적화 수</summary>
    public long SuccessfulOptimizations { get; init; }

    /// <summary>평균 성능 향상</summary>
    public double AveragePerformanceImprovement { get; init; }

    /// <summary>총 리소스 절약</summary>
    public ResourceSavings TotalResourceSavings { get; init; } = new();

    /// <summary>최적화 성공률</summary>
    public double SuccessRate => TotalOptimizationRequests > 0
        ? (double)SuccessfulOptimizations / TotalOptimizationRequests
        : 0.0;

    /// <summary>통계 수집 기간</summary>
    public TimeSpan CollectionPeriod { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 파이프라인 메트릭
/// </summary>
public class PipelineMetrics
{
    /// <summary>단계별 처리 시간</summary>
    public IReadOnlyDictionary<string, TimeSpan> StageProcessingTimes { get; init; } =
        new Dictionary<string, TimeSpan>();

    /// <summary>단계별 처리량</summary>
    public IReadOnlyDictionary<string, double> StageThroughput { get; init; } =
        new Dictionary<string, double>();

    /// <summary>단계별 메모리 사용량</summary>
    public IReadOnlyDictionary<string, long> StageMemoryUsage { get; init; } =
        new Dictionary<string, long>();

    /// <summary>오류율</summary>
    public double ErrorRate { get; init; }

    /// <summary>전체 처리 시간</summary>
    public TimeSpan TotalProcessingTime { get; init; }
}