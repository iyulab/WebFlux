namespace WebFlux.Core.Models;

/// <summary>
/// 웹 콘텐츠 메타데이터를 나타내는 클래스
/// </summary>
public class WebContentMetadata
{
    /// <summary>
    /// 페이지 제목
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 페이지 설명
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 키워드 목록
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 작성자 정보
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// 게시 날짜
    /// </summary>
    public DateTimeOffset? PublishedDate { get; init; }

    /// <summary>
    /// 수정 날짜
    /// </summary>
    public DateTimeOffset? ModifiedDate { get; init; }

    /// <summary>
    /// 언어 코드 (예: "ko", "en")
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// 콘텐츠 유형 (예: "article", "blog", "documentation")
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// 도메인 정보
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// 페이지 경로
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// HTTP 상태 코드
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Content-Type 헤더
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// 문자 인코딩
    /// </summary>
    public string? Encoding { get; init; }

    /// <summary>
    /// 페이지 크기 (바이트)
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// 크롤링 시간
    /// </summary>
    public DateTimeOffset CrawledAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 구조적 데이터 (JSON-LD, Microdata 등)
    /// </summary>
    public IReadOnlyDictionary<string, object> StructuredData { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// OpenGraph 메타 태그
    /// </summary>
    public OpenGraphData? OpenGraph { get; init; }

    /// <summary>
    /// Twitter Card 메타 태그
    /// </summary>
    public TwitterCardData? TwitterCard { get; init; }

    /// <summary>
    /// 사용자 정의 메타데이터
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomProperties { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 추가 데이터 (호환성을 위한 속성)
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalData => CustomProperties;
}

/// <summary>
/// OpenGraph 메타데이터
/// </summary>
public class OpenGraphData
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }
    public string? Image { get; set; }
    public string? SiteName { get; set; }
    public string? Locale { get; set; }
    public DateTimeOffset? PublishedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
    public string? Author { get; set; }
    public string? Section { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Twitter Card 메타데이터
/// </summary>
public class TwitterCardData
{
    public string? Card { get; set; }
    public string? Site { get; set; }
    public string? Creator { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
}