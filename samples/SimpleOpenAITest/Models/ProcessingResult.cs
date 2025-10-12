namespace WebFlux.SimpleTest.Models;

/// <summary>
/// URL 처리 결과를 저장하는 모델
/// </summary>
public class ProcessingResult
{
    public required string Url { get; init; }
    public required string UrlId { get; init; }  // url-001, url-002 등
    public required DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public TimeSpan? ProcessingTime => EndTime.HasValue ? EndTime.Value - StartTime : null;

    // HTTP 응답
    public int? HttpStatusCode { get; init; }
    public required string OriginalHtml { get; init; }

    // 텍스트 추출
    public required string ExtractedText { get; init; }
    public required string TruncatedText { get; init; }

    // AI 요약
    public string? Summary { get; init; }
    public string? ErrorMessage { get; init; }

    // 통계
    public int OriginalLength => OriginalHtml.Length;
    public int ExtractedLength => ExtractedText.Length;
    public int TruncatedLength => TruncatedText.Length;
    public int SummaryLength => Summary?.Length ?? 0;
    public double ProcessingRate => ProcessingTime.HasValue && ProcessingTime.Value.TotalMinutes > 0
        ? ExtractedLength / ProcessingTime.Value.TotalMinutes
        : 0;

    public bool IsSuccess => ExtractedLength > 0 && string.IsNullOrEmpty(ErrorMessage);
}
