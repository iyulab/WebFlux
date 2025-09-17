namespace WebFlux.Core.Models;

/// <summary>
/// 크롤링된 웹 콘텐츠 모델
/// </summary>
public class WebContent
{
    /// <summary>웹 페이지 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>콘텐츠 내용</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>콘텐츠 타입</summary>
    public string ContentType { get; set; } = "text/html";

    /// <summary>메타데이터</summary>
    public WebContentMetadata? Metadata { get; set; }

    /// <summary>크롤링 시간</summary>
    public DateTimeOffset CrawledAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 추출된 콘텐츠 모델
/// </summary>
public class ExtractedContent
{
    /// <summary>추출된 텍스트</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>추출된 메타데이터</summary>
    public ExtractedMetadata? Metadata { get; set; }

    /// <summary>원본 URL</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>원본 콘텐츠 타입</summary>
    public string OriginalContentType { get; set; } = string.Empty;

    /// <summary>추출 방법</summary>
    public string ExtractionMethod { get; set; } = string.Empty;

    /// <summary>추출 시간</summary>
    public DateTimeOffset ExtractionTimestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>처리 시간 (밀리초)</summary>
    public int ProcessingTimeMs { get; set; }
}

/// <summary>
/// 추출된 메타데이터 모델
/// </summary>
public class ExtractedMetadata
{
    /// <summary>제목</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>설명</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>작성자</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>언어</summary>
    public string Language { get; set; } = "unknown";

    /// <summary>단어 수</summary>
    public int WordCount { get; set; }

    /// <summary>문자 수</summary>
    public int CharacterCount { get; set; }

    /// <summary>읽기 시간 (분)</summary>
    public double ReadingTimeMinutes { get; set; }

    /// <summary>키워드</summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>원본 메타데이터</summary>
    public Dictionary<string, object> OriginalMetadata { get; set; } = new();
}

/// <summary>
/// 크롤링 구성
/// </summary>
public class CrawlConfiguration
{
    /// <summary>시작 URL들</summary>
    public List<string> StartUrls { get; set; } = new();

    /// <summary>크롤링 전략</summary>
    public CrawlStrategy Strategy { get; set; } = CrawlStrategy.BreadthFirst;

    /// <summary>최대 페이지 수</summary>
    public int MaxPages { get; set; } = 100;

    /// <summary>최대 깊이</summary>
    public int MaxDepth { get; set; } = 3;

    /// <summary>최대 동시 요청 수</summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>요청 간 지연</summary>
    public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>허용 도메인</summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>제외 패턴</summary>
    public List<string>? ExcludePatterns { get; set; }
}

/// <summary>
/// 추출 구성
/// </summary>
public class ExtractionConfiguration
{
    /// <summary>링크 URL 포함 여부</summary>
    public bool IncludeLinkUrls { get; set; } = false;

    /// <summary>공백 정규화 여부</summary>
    public bool NormalizeWhitespace { get; set; } = true;

    /// <summary>줄바꿈 정규화 여부</summary>
    public bool NormalizeLineBreaks { get; set; } = true;

    /// <summary>최소 텍스트 길이</summary>
    public int MinTextLength { get; set; } = 50;

    /// <summary>최대 텍스트 길이 (0은 제한 없음)</summary>
    public int MaxTextLength { get; set; } = 0;
}


/// <summary>
/// 크롤 상태
/// </summary>
public class CrawlStatus
{
    /// <summary>실행 중 여부</summary>
    public bool IsRunning { get; set; }

    /// <summary>처리된 수</summary>
    public int ProcessedCount { get; set; }

    /// <summary>성공 수</summary>
    public int SuccessCount { get; set; }

    /// <summary>오류 수</summary>
    public int ErrorCount { get; set; }

    /// <summary>대기 중인 수</summary>
    public int QueuedCount { get; set; }

    /// <summary>방문한 URL 수</summary>
    public int VisitedUrlCount { get; set; }

    /// <summary>실패한 URL 수</summary>
    public int FailedUrlCount { get; set; }

    /// <summary>마지막 활동 시간</summary>
    public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>추가 정보</summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// 구조화된 요소
/// </summary>
public class StructuredElement
{
    /// <summary>요소 유형</summary>
    public required ElementType Type { get; init; }

    /// <summary>요소 내용</summary>
    public required string Content { get; init; }

    /// <summary>요소 레벨 (헤더의 경우 h1=1, h2=2 등)</summary>
    public int? Level { get; init; }

    /// <summary>요소 속성</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } =
        new Dictionary<string, string>();

    /// <summary>요소 위치 (문서 내 순서)</summary>
    public int Position { get; init; }

    /// <summary>자식 요소 목록</summary>
    public IReadOnlyList<StructuredElement> Children { get; init; } =
        Array.Empty<StructuredElement>();
}

/// <summary>
/// 이미지 정보
/// </summary>
public class ImageInfo
{
    /// <summary>이미지 URL</summary>
    public required string Url { get; init; }

    /// <summary>대체 텍스트</summary>
    public string? AltText { get; init; }

    /// <summary>이미지 제목</summary>
    public string? Title { get; init; }

    /// <summary>이미지 크기 (바이트)</summary>
    public long? SizeBytes { get; init; }

    /// <summary>이미지 차원 (width x height)</summary>
    public string? Dimensions { get; init; }

    /// <summary>이미지 형식</summary>
    public string? Format { get; init; }

    /// <summary>문서 내 위치</summary>
    public int Position { get; init; }

    /// <summary>컨텍스트 정보 (주변 텍스트)</summary>
    public string? Context { get; init; }
}

/// <summary>
/// 링크 정보
/// </summary>
public class LinkInfo
{
    /// <summary>링크 URL</summary>
    public required string Url { get; init; }

    /// <summary>링크 텍스트</summary>
    public string? Text { get; init; }

    /// <summary>링크 제목</summary>
    public string? Title { get; init; }

    /// <summary>링크 유형 (내부/외부)</summary>
    public LinkType Type { get; init; }

    /// <summary>문서 내 위치</summary>
    public int Position { get; init; }
}

/// <summary>
/// 요소 유형 열거형
/// </summary>
public enum ElementType
{
    /// <summary>헤더</summary>
    Header,
    /// <summary>문단</summary>
    Paragraph,
    /// <summary>목록</summary>
    List,
    /// <summary>목록 항목</summary>
    ListItem,
    /// <summary>표</summary>
    Table,
    /// <summary>표 행</summary>
    TableRow,
    /// <summary>표 셀</summary>
    TableCell,
    /// <summary>코드 블록</summary>
    CodeBlock,
    /// <summary>인용문</summary>
    Blockquote,
    /// <summary>구분선</summary>
    Divider,
    /// <summary>기타</summary>
    Other
}

/// <summary>
/// 콘텐츠 유형 열거형
/// </summary>
public enum ContentType
{
    /// <summary>HTML</summary>
    Html,
    /// <summary>마크다운</summary>
    Markdown,
    /// <summary>JSON</summary>
    Json,
    /// <summary>XML</summary>
    Xml,
    /// <summary>일반 텍스트</summary>
    PlainText,
    /// <summary>알 수 없음</summary>
    Unknown
}


/// <summary>
/// 추출 통계
/// </summary>
public class ExtractionStatistics
{
    /// <summary>총 추출 요청 수</summary>
    public long TotalExtractions { get; init; }

    /// <summary>성공한 추출 수</summary>
    public long SuccessfulExtractions { get; init; }

    /// <summary>실패한 추출 수</summary>
    public long FailedExtractions { get; init; }

    /// <summary>평균 추출 시간 (밀리초)</summary>
    public double AverageExtractionTimeMs { get; init; }

    /// <summary>평균 품질 점수</summary>
    public double AverageQualityScore { get; init; }

    /// <summary>콘텐츠 유형별 분포</summary>
    public IReadOnlyDictionary<ContentType, long> ContentTypeDistribution { get; init; } =
        new Dictionary<ContentType, long>();

    /// <summary>추출된 총 텍스트 길이</summary>
    public long TotalTextLength { get; init; }

    /// <summary>추출된 총 이미지 수</summary>
    public long TotalImages { get; init; }

    /// <summary>추출된 총 링크 수</summary>
    public long TotalLinks { get; init; }

    /// <summary>통계 수집 시간</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}