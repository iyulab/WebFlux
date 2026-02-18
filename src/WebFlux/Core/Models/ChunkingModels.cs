namespace WebFlux.Core.Models;


/// <summary>
/// 청킹 결과
/// </summary>
public class ChunkResult
{
    /// <summary>
    /// 생성된 청크 목록
    /// </summary>
    public List<WebContentChunk> Chunks { get; set; } = new();

    /// <summary>
    /// 사용된 청킹 전략
    /// </summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// 처리 통계
    /// </summary>
    public ChunkingStatistics Statistics { get; set; } = new();

    /// <summary>
    /// 결과 메타데이터
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 처리 성공 여부
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 처리 시간
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}


/// <summary>
/// 청킹 통계
/// </summary>
public class ChunkingStatistics
{
    /// <summary>
    /// 처리된 총 청크 수
    /// </summary>
    public int TotalChunksProcessed { get; set; }

    /// <summary>
    /// 평균 청크 크기
    /// </summary>
    public double AverageChunkSize { get; set; }

    /// <summary>
    /// 최소 청크 크기
    /// </summary>
    public int MinChunkSize { get; set; }

    /// <summary>
    /// 최대 청크 크기
    /// </summary>
    public int MaxChunkSize { get; set; }

    /// <summary>
    /// 평균 처리 시간
    /// </summary>
    public TimeSpan AverageProcessingTime { get; set; }

    /// <summary>
    /// 성공률 (0-1)
    /// </summary>
    public double SuccessRate { get; set; } = 1.0;

    /// <summary>
    /// 평균 품질 점수 (0-1)
    /// </summary>
    public double QualityScore { get; set; } = 0.8;

    /// <summary>
    /// 메모리 사용량 (바이트)
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// 중복 제거된 청크 수
    /// </summary>
    public int DeduplicatedChunks { get; set; }
}

/// <summary>
/// 성능 정보
/// </summary>
public class PerformanceInfo
{
    /// <summary>
    /// 평균 처리 시간
    /// </summary>
    public TimeSpan AverageProcessingTime { get; set; }

    /// <summary>
    /// 메모리 사용량 설명
    /// </summary>
    public string MemoryUsage { get; set; } = "Low";

    /// <summary>
    /// CPU 집약도
    /// </summary>
    public string CPUIntensity { get; set; } = "Low";

    /// <summary>
    /// 확장성
    /// </summary>
    public string Scalability { get; set; } = "High";

    /// <summary>
    /// 처리량 (청크/초)
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// 최적 문서 크기 범위
    /// </summary>
    public string OptimalDocumentSize { get; set; } = "Any";

    /// <summary>
    /// 권장 사용 시나리오
    /// </summary>
    public List<string> RecommendedScenarios { get; set; } = new();
}

/// <summary>
/// 구성 옵션
/// </summary>
public class ConfigurationOption
{
    /// <summary>
    /// 옵션 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 옵션 키
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 기본값
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 데이터 타입
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// 필수 여부
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 유효한 값 목록
    /// </summary>
    public List<string> ValidValues { get; set; } = new();

    /// <summary>
    /// 최소값 (숫자 타입인 경우)
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// 최대값 (숫자 타입인 경우)
    /// </summary>
    public double? MaxValue { get; set; }
}



