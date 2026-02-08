namespace WebFlux.Core.Models.Events;

/// <summary>
/// 콘텐츠 추출 관련 이벤트 통합
/// </summary>

/// <summary>
/// 이미지 처리 이벤트 (통합)
/// </summary>
public class ImageProcessedEventV2 : ProcessingEvent
{
    public override string EventType => "ImageProcessed";

    /// <summary>이미지 URL</summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>생성된 설명 길이</summary>
    public int DescriptionLength { get; init; }

    /// <summary>처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>이미지 크기 (바이트)</summary>
    public long? ImageSize { get; init; }

    /// <summary>이미지 형식</summary>
    public string? ImageFormat { get; init; }
}
