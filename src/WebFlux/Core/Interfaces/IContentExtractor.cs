using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 추출 인터페이스
/// 다양한 형식의 웹 콘텐츠에서 텍스트와 메타데이터를 추출
/// </summary>
public interface IContentExtractor
{
    /// <summary>
    /// HTML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromHtmlAsync(
        string htmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 마크다운 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="markdownContent">마크다운 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromMarkdownAsync(
        string markdownContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// JSON 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="jsonContent">JSON 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromJsonAsync(
        string jsonContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// XML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="xmlContent">XML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromXmlAsync(
        string xmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 일반 텍스트 콘텐츠를 처리합니다.
    /// </summary>
    /// <param name="textContent">텍스트 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractFromTextAsync(
        string textContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 콘텐츠 유형을 자동으로 감지하여 추출합니다.
    /// </summary>
    /// <param name="content">콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="contentType">콘텐츠 타입 힌트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    Task<ExtractedContent> ExtractAutoAsync(
        string content,
        string sourceUrl,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지원하는 콘텐츠 타입 목록을 반환합니다.
    /// </summary>
    /// <returns>지원하는 MIME 타입 목록</returns>
    IReadOnlyList<string> GetSupportedContentTypes();

    /// <summary>
    /// 추출 통계를 반환합니다.
    /// </summary>
    /// <returns>추출 통계 정보</returns>
    ExtractionStatistics GetStatistics();
}

/// <summary>
/// 추출된 콘텐츠
/// </summary>
public class ExtractedContent
{
    /// <summary>추출된 메인 텍스트</summary>
    public required string MainText { get; init; }

    /// <summary>원본 URL</summary>
    public required string SourceUrl { get; init; }

    /// <summary>메타데이터</summary>
    public WebContentMetadata Metadata { get; init; } = new();

    /// <summary>구조화된 데이터 (헤더, 목록, 표 등)</summary>
    public IReadOnlyList<StructuredElement> StructuredElements { get; init; } =
        Array.Empty<StructuredElement>();

    /// <summary>이미지 정보 목록</summary>
    public IReadOnlyList<ImageInfo> Images { get; init; } = Array.Empty<ImageInfo>();

    /// <summary>링크 정보 목록</summary>
    public IReadOnlyList<LinkInfo> Links { get; init; } = Array.Empty<LinkInfo>();

    /// <summary>추출 품질 점수 (0.0 - 1.0)</summary>
    public double QualityScore { get; init; }

    /// <summary>추출 시간 (밀리초)</summary>
    public long ExtractionTimeMs { get; init; }

    /// <summary>추출된 시간</summary>
    public DateTimeOffset ExtractedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>언어 감지 결과</summary>
    public string? DetectedLanguage { get; init; }

    /// <summary>콘텐츠 유형</summary>
    public ContentType ContentType { get; init; }

    /// <summary>경고 메시지 목록</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
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
/// 링크 유형 열거형
/// </summary>
public enum LinkType
{
    /// <summary>내부 링크</summary>
    Internal,
    /// <summary>외부 링크</summary>
    External,
    /// <summary>앵커 링크</summary>
    Anchor,
    /// <summary>이메일</summary>
    Email,
    /// <summary>전화번호</summary>
    Phone,
    /// <summary>파일 다운로드</summary>
    Download
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

    /// <summary>통계 수집 시작 시간</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}