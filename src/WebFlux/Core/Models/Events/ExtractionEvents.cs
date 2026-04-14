namespace WebFlux.Core.Models.Events;

/// <summary>
/// 콘텐츠 추출 시작 이벤트
/// </summary>
public class ContentExtractionStartedEvent : ProcessingEvent
{
    public override string EventType => "ContentExtractionStarted";

    /// <summary>추출 대상 URL</summary>
    public required string Url { get; init; }

    /// <summary>콘텐츠 타입</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>원본 콘텐츠 길이</summary>
    public int ContentLength { get; init; }
}

/// <summary>
/// 콘텐츠 추출 완료 이벤트
/// </summary>
public class ContentExtractionCompletedEvent : ProcessingEvent
{
    public override string EventType => "ContentExtractionCompleted";

    /// <summary>추출 대상 URL</summary>
    public required string Url { get; init; }

    /// <summary>추출된 텍스트 길이</summary>
    public int ExtractedTextLength { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public int ProcessingTimeMs { get; init; }

    /// <summary>추출 방식</summary>
    public string ExtractionMethod { get; init; } = string.Empty;
}

/// <summary>
/// 콘텐츠 추출 실패 이벤트
/// </summary>
public class ContentExtractionFailedEvent : ProcessingEvent
{
    public override string EventType => "ContentExtractionFailed";

    /// <summary>실패한 URL</summary>
    public required string Url { get; init; }

    /// <summary>오류 메시지</summary>
    public required string Error { get; init; }
}

/// <summary>
/// 이미지 처리 이벤트
/// </summary>
public class ImageProcessedEvent : ProcessingEvent
{
    public override string EventType => "ImageProcessed";

    /// <summary>이미지 URL</summary>
    public required string ImageUrl { get; init; }

    /// <summary>생성된 설명 길이</summary>
    public int DescriptionLength { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>이미지 크기 (바이트)</summary>
    public long? ImageSize { get; init; }

    /// <summary>이미지 형식</summary>
    public string? ImageFormat { get; init; }
}
