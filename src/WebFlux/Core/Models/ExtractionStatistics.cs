namespace WebFlux.Core.Models;

/// <summary>
/// 추출 통계 정보
/// </summary>
public class ExtractionStatistics
{
    /// <summary>총 추출 시도 수</summary>
    public long TotalExtractions { get; init; }

    /// <summary>성공한 추출 수</summary>
    public long SuccessfulExtractions { get; init; }

    /// <summary>실패한 추출 수</summary>
    public long FailedExtractions { get; init; }

    /// <summary>평균 처리 시간 (밀리초)</summary>
    public double AverageProcessingTimeMs { get; init; }

    /// <summary>지원하는 콘텐츠 타입 수</summary>
    public int SupportedContentTypes { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>성공률 (%)</summary>
    public double SuccessRate => TotalExtractions > 0 ? (double)SuccessfulExtractions / TotalExtractions * 100 : 0;
}