namespace WebFlux.Core.Options;

/// <summary>
/// 청킹 옵션을 정의하는 클래스
/// </summary>
public class ChunkingOptions
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
    /// 청크 크기 (MaxChunkSize의 별칭)
    /// </summary>
    public int ChunkSize
    {
        get => MaxChunkSize;
        set => MaxChunkSize = value;
    }

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
    /// 테이블 분할 여부 (기본값: false)
    /// </summary>
    public bool SplitTables { get; set; } = false;

    /// <summary>
    /// 코드 블록 분할 여부 (기본값: false)
    /// </summary>
    public bool SplitCodeBlocks { get; set; } = false;

    /// <summary>
    /// 이미지 설명 포함 여부 (기본값: true)
    /// </summary>
    public bool IncludeImageDescriptions { get; set; } = true;

    /// <summary>
    /// 메타데이터 포함 여부 (기본값: true)
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// 계층 구조 생성 여부 (기본값: false)
    /// </summary>
    public bool CreateHierarchy { get; set; } = false;

    /// <summary>
    /// 병렬 처리 여부 (기본값: true)
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// 최대 병렬 작업 수 (기본값: Environment.ProcessorCount)
    /// </summary>
    public int MaxParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 언어별 토큰화 설정
    /// </summary>
    public string Language { get; set; } = "ko";

    /// <summary>
    /// 커스텀 구분자 목록
    /// </summary>
    public IList<string> CustomSeparators { get; set; } = new List<string>();

    /// <summary>
    /// 전략별 추가 설정
    /// </summary>
    public IDictionary<string, object> StrategySpecificOptions { get; set; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 메모리 최적화 모드 사용 여부 (기본값: false)
    /// </summary>
    public bool UseMemoryOptimization { get; set; } = false;

    /// <summary>
    /// 메모리 사용량 최소화 여부 (UseMemoryOptimization의 별칭)
    /// </summary>
    public bool MinimizeMemoryUsage
    {
        get => UseMemoryOptimization;
        set => UseMemoryOptimization = value;
    }

    /// <summary>
    /// 스트리밍 처리 사용 여부 (기본값: true)
    /// </summary>
    public bool UseStreaming { get; set; } = true;

    /// <summary>
    /// 멀티모달 처리 활성화 여부 (Phase 5A.3 재설계)
    /// 모든 청킹 전략에 선택적으로 적용 가능
    /// </summary>
    public bool EnableMultimodalProcessing { get; set; } = false;

    /// <summary>
    /// 멀티모달 처리 옵션
    /// EnableMultimodalProcessing이 true일 때만 사용됨
    /// </summary>
    public MultimodalProcessingOptions? MultimodalOptions { get; set; }
}

/// <summary>
/// 청킹 전략 유형 열거형
/// </summary>
public enum ChunkingStrategyType
{
    /// <summary>자동 선택 (콘텐츠 분석 기반)</summary>
    Auto,
    /// <summary>고정 크기 분할</summary>
    FixedSize,
    /// <summary>문단 기반 분할</summary>
    Paragraph,
    /// <summary>구조 인식 분할 (헤더 기반)</summary>
    Smart,
    /// <summary>의미론적 분할 (임베딩 기반)</summary>
    Semantic,
    /// <summary>지능형 분할 (LLM 기반)</summary>
    Intelligent,
    /// <summary>메모리 최적화 분할</summary>
    MemoryOptimized
}