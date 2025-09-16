namespace WebFlux.Core.Models;

/// <summary>
/// 웹 콘텐츠 청크를 나타내는 클래스
/// RAG 시스템에서 사용할 구조화된 텍스트 조각
/// </summary>
public class WebContentChunk
{
    /// <summary>
    /// 청크의 고유 식별자
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// 청크의 텍스트 내용
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 청크의 제목 또는 헤더 정보
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// 원본 웹 페이지 URL
    /// </summary>
    public required string SourceUrl { get; init; }

    /// <summary>
    /// 청크가 생성된 시간
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 청크의 메타데이터
    /// </summary>
    public WebContentMetadata Metadata { get; init; } = new();

    /// <summary>
    /// 청크 내 순서 (문서 내에서의 위치)
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// 청킹 전략 정보
    /// </summary>
    public required ChunkingStrategyInfo StrategyInfo { get; init; }

    /// <summary>
    /// 청크의 품질 점수 (0.0 - 1.0)
    /// </summary>
    public double QualityScore { get; init; }

    /// <summary>
    /// 청크 유형 (텍스트, 이미지 설명, 표 등)
    /// </summary>
    public ChunkType Type { get; init; } = ChunkType.Text;

    /// <summary>
    /// 부모 청크 ID (계층 구조가 있는 경우)
    /// </summary>
    public string? ParentChunkId { get; init; }

    /// <summary>
    /// 자식 청크 ID 목록
    /// </summary>
    public IReadOnlyList<string> ChildChunkIds { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 관련 이미지 URL 목록
    /// </summary>
    public IReadOnlyList<string> RelatedImageUrls { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 태그 목록 (카테고리, 키워드 등)
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 청킹 전략 정보
/// </summary>
public class ChunkingStrategyInfo
{
    /// <summary>
    /// 사용된 청킹 전략 이름
    /// </summary>
    public required string StrategyName { get; init; }

    /// <summary>
    /// 전략별 설정값
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 처리 시간 (밀리초)
    /// </summary>
    public long ProcessingTimeMs { get; init; }
}

/// <summary>
/// 청크 유형 열거형
/// </summary>
public enum ChunkType
{
    /// <summary>일반 텍스트</summary>
    Text,
    /// <summary>이미지 설명</summary>
    ImageDescription,
    /// <summary>표 데이터</summary>
    Table,
    /// <summary>코드 블록</summary>
    Code,
    /// <summary>헤더/제목</summary>
    Header,
    /// <summary>목록</summary>
    List,
    /// <summary>링크 모음</summary>
    Links,
    /// <summary>메타데이터</summary>
    Metadata
}