using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 청킹 전략 인터페이스
/// 다양한 청킹 알고리즘 구현을 위한 계약 정의
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// 전략 이름
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 전략 설명
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 추출된 콘텐츠를 청크로 분할합니다.
    /// </summary>
    /// <param name="content">추출된 콘텐츠</param>
    /// <param name="options">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 청크 목록</returns>
    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 스트리밍 방식으로 청크를 생성합니다.
    /// </summary>
    /// <param name="content">추출된 콘텐츠</param>
    /// <param name="options">청킹 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>청크 스트림</returns>
    IAsyncEnumerable<WebContentChunk> ChunkStreamAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 콘텐츠가 이 전략에 적합한지 평가합니다.
    /// </summary>
    /// <param name="content">평가할 콘텐츠</param>
    /// <param name="options">청킹 옵션</param>
    /// <returns>적합성 점수 (0.0 - 1.0)</returns>
    Task<double> EvaluateSuitabilityAsync(
        ExtractedContent content,
        ChunkingOptions? options = null);

    /// <summary>
    /// 전략의 성능 특성을 반환합니다.
    /// </summary>
    /// <returns>성능 특성 정보</returns>
    StrategyPerformanceInfo GetPerformanceInfo();

    /// <summary>
    /// 전략의 설정 옵션을 반환합니다.
    /// </summary>
    /// <returns>설정 가능한 옵션 목록</returns>
    IReadOnlyList<StrategyOption> GetConfigurationOptions();

    /// <summary>
    /// 청크 품질을 평가합니다.
    /// </summary>
    /// <param name="chunk">평가할 청크</param>
    /// <param name="context">평가 컨텍스트</param>
    /// <returns>품질 점수 (0.0 - 1.0)</returns>
    Task<double> EvaluateChunkQualityAsync(
        WebContentChunk chunk,
        ChunkEvaluationContext? context = null);

    /// <summary>
    /// 청킹 통계를 반환합니다.
    /// </summary>
    /// <returns>청킹 통계 정보</returns>
    ChunkingStatistics GetStatistics();
}

/// <summary>
/// 전략 성능 정보
/// </summary>
public class StrategyPerformanceInfo
{
    /// <summary>평균 처리 시간 (밀리초/MB)</summary>
    public double AverageProcessingTimePerMb { get; init; }

    /// <summary>메모리 사용량 배수 (입력 대비)</summary>
    public double MemoryUsageMultiplier { get; init; }

    /// <summary>품질 점수 범위</summary>
    public (double Min, double Max, double Average) QualityScoreRange { get; init; }

    /// <summary>확장성 수준</summary>
    public ScalabilityLevel Scalability { get; init; }

    /// <summary>복잡도 수준</summary>
    public ComplexityLevel Complexity { get; init; }

    /// <summary>권장 사용 사례</summary>
    public IReadOnlyList<string> RecommendedUseCases { get; init; } = Array.Empty<string>();

    /// <summary>제한 사항</summary>
    public IReadOnlyList<string> Limitations { get; init; } = Array.Empty<string>();

    /// <summary>최소 콘텐츠 크기 (문자 수)</summary>
    public int MinContentLength { get; init; }

    /// <summary>최대 콘텐츠 크기 (문자 수)</summary>
    public int? MaxContentLength { get; init; }

    /// <summary>지원하는 언어 목록</summary>
    public IReadOnlyList<string> SupportedLanguages { get; init; } = Array.Empty<string>();

    /// <summary>의존성 목록 (외부 서비스 등)</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 전략 설정 옵션
/// </summary>
public class StrategyOption
{
    /// <summary>옵션 키</summary>
    public required string Key { get; init; }

    /// <summary>옵션 이름</summary>
    public required string Name { get; init; }

    /// <summary>옵션 설명</summary>
    public required string Description { get; init; }

    /// <summary>옵션 유형</summary>
    public required Type OptionType { get; init; }

    /// <summary>기본값</summary>
    public object? DefaultValue { get; init; }

    /// <summary>필수 여부</summary>
    public bool IsRequired { get; init; }

    /// <summary>가능한 값 목록 (열거형인 경우)</summary>
    public IReadOnlyList<object> PossibleValues { get; init; } = Array.Empty<object>();

    /// <summary>값 범위 (숫자인 경우)</summary>
    public (object? Min, object? Max) ValueRange { get; init; }

    /// <summary>검증 규칙</summary>
    public string? ValidationRule { get; init; }
}

/// <summary>
/// 청크 평가 컨텍스트
/// </summary>
public class ChunkEvaluationContext
{
    /// <summary>원본 콘텐츠</summary>
    public ExtractedContent? OriginalContent { get; init; }

    /// <summary>이전 청크</summary>
    public WebContentChunk? PreviousChunk { get; init; }

    /// <summary>다음 청크</summary>
    public WebContentChunk? NextChunk { get; init; }

    /// <summary>전체 청크 목록에서의 인덱스</summary>
    public int ChunkIndex { get; init; }

    /// <summary>전체 청크 수</summary>
    public int TotalChunks { get; init; }

    /// <summary>목표 품질 기준</summary>
    public QualityCriteria? QualityCriteria { get; init; }

    /// <summary>추가 컨텍스트 정보</summary>
    public IReadOnlyDictionary<string, object> AdditionalContext { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// 품질 기준
/// </summary>
public class QualityCriteria
{
    /// <summary>최소 의미적 일관성 점수</summary>
    public double MinSemanticCoherence { get; init; } = 0.7;

    /// <summary>최소 정보 밀도</summary>
    public double MinInformationDensity { get; init; } = 0.5;

    /// <summary>최대 중복도</summary>
    public double MaxRedundancy { get; init; } = 0.3;

    /// <summary>구조적 완전성 필요 여부</summary>
    public bool RequireStructuralIntegrity { get; init; } = true;

    /// <summary>컨텍스트 보존 필요 여부</summary>
    public bool RequireContextPreservation { get; init; } = true;

    /// <summary>사용자 정의 품질 메트릭</summary>
    public IReadOnlyDictionary<string, double> CustomMetrics { get; init; } =
        new Dictionary<string, double>();
}

/// <summary>
/// 청킹 통계
/// </summary>
public class ChunkingStatistics
{
    /// <summary>총 처리된 콘텐츠 수</summary>
    public long TotalProcessedContents { get; init; }

    /// <summary>총 생성된 청크 수</summary>
    public long TotalGeneratedChunks { get; init; }

    /// <summary>평균 청크 크기 (토큰 수)</summary>
    public double AverageChunkSize { get; init; }

    /// <summary>평균 처리 시간 (밀리초)</summary>
    public double AverageProcessingTimeMs { get; init; }

    /// <summary>평균 품질 점수</summary>
    public double AverageQualityScore { get; init; }

    /// <summary>메모리 사용량 통계</summary>
    public MemoryUsageStatistics MemoryUsage { get; init; } = new();

    /// <summary>언어별 처리 통계</summary>
    public IReadOnlyDictionary<string, LanguageProcessingStats> LanguageStats { get; init; } =
        new Dictionary<string, LanguageProcessingStats>();

    /// <summary>콘텐츠 유형별 통계</summary>
    public IReadOnlyDictionary<ContentType, ContentTypeStats> ContentTypeStats { get; init; } =
        new Dictionary<ContentType, ContentTypeStats>();

    /// <summary>오류 통계</summary>
    public IReadOnlyDictionary<string, long> ErrorStatistics { get; init; } =
        new Dictionary<string, long>();

    /// <summary>통계 수집 기간</summary>
    public DateTimeOffset StatsPeriodStart { get; init; }

    /// <summary>통계 수집 종료</summary>
    public DateTimeOffset StatsPeriodEnd { get; init; }
}

/// <summary>
/// 메모리 사용량 통계
/// </summary>
public class MemoryUsageStatistics
{
    /// <summary>평균 메모리 사용량 (바이트)</summary>
    public long AverageMemoryUsage { get; init; }

    /// <summary>최대 메모리 사용량 (바이트)</summary>
    public long PeakMemoryUsage { get; init; }

    /// <summary>메모리 효율성 (출력/입력 비율)</summary>
    public double MemoryEfficiency { get; init; }
}

/// <summary>
/// 언어별 처리 통계
/// </summary>
public class LanguageProcessingStats
{
    /// <summary>처리된 콘텐츠 수</summary>
    public long ProcessedCount { get; init; }

    /// <summary>평균 품질 점수</summary>
    public double AverageQuality { get; init; }

    /// <summary>평균 처리 시간 (밀리초)</summary>
    public double AverageProcessingTime { get; init; }
}

/// <summary>
/// 콘텐츠 유형별 통계
/// </summary>
public class ContentTypeStats
{
    /// <summary>처리된 콘텐츠 수</summary>
    public long ProcessedCount { get; init; }

    /// <summary>평균 청크 수</summary>
    public double AverageChunkCount { get; init; }

    /// <summary>평균 품질 점수</summary>
    public double AverageQuality { get; init; }
}

/// <summary>
/// 확장성 수준 열거형
/// </summary>
public enum ScalabilityLevel
{
    /// <summary>낮음 - 작은 문서에만 적합</summary>
    Low,
    /// <summary>보통 - 중간 크기 문서에 적합</summary>
    Medium,
    /// <summary>높음 - 대용량 문서 처리 가능</summary>
    High,
    /// <summary>매우 높음 - 엔터프라이즈급 확장성</summary>
    VeryHigh
}

/// <summary>
/// 복잡도 수준 열거형
/// </summary>
public enum ComplexityLevel
{
    /// <summary>단순 - 기본적인 알고리즘</summary>
    Simple,
    /// <summary>보통 - 일반적인 복잡도</summary>
    Moderate,
    /// <summary>복잡 - 고급 알고리즘 사용</summary>
    Complex,
    /// <summary>매우 복잡 - AI/ML 기반</summary>
    VeryComplex
}