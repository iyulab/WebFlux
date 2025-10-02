namespace WebFlux.Core.Models;

/// <summary>
/// 원본 콘텐츠 (Extract 단계 출력)
/// Stage 1: 추출된 원본 콘텐츠 그대로 보존
/// </summary>
public class RawContent
{
    /// <summary>
    /// 원본 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 원본 콘텐츠 (HTML, Markdown, JSON 등 그대로)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 콘텐츠 타입 (MIME type)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 웹 콘텐츠 메타데이터
    /// </summary>
    public WebContentMetadata Metadata { get; set; } = new();

    /// <summary>
    /// 이미지 URL 목록
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>
    /// 링크 URL 목록
    /// </summary>
    public List<string> Links { get; set; } = new();

    /// <summary>
    /// 추출 시간
    /// </summary>
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 추출기 정보
    /// </summary>
    public string ExtractorType { get; set; } = string.Empty;

    /// <summary>
    /// 추가 속성 (확장 가능)
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// HTTP 응답 헤더
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// HTTP 상태 코드
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 콘텐츠 길이 (바이트)
    /// </summary>
    public long ContentLength { get; set; }
}
