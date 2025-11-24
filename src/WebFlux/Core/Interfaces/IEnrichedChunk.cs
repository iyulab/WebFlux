namespace WebFlux.Core.Interfaces;

/// <summary>
/// FluxIndex 통합을 위한 표준 청크 인터페이스
/// RAG 시스템에서 사용할 구조화된 청크 계약
/// </summary>
public interface IEnrichedChunk
{
    /// <summary>
    /// 청크의 텍스트 내용
    /// </summary>
    string Content { get; }

    // ===================================================================
    // 식별 정보
    // ===================================================================

    /// <summary>
    /// 청크의 고유 식별자
    /// </summary>
    string ChunkId { get; }

    /// <summary>
    /// 문서 내 청크 순서 (0-based index)
    /// </summary>
    int ChunkIndex { get; }

    // ===================================================================
    // 구조 정보
    // ===================================================================

    /// <summary>
    /// 헤딩 계층 구조
    /// 예: ["Introduction", "Getting Started", "Installation"]
    /// </summary>
    IReadOnlyList<string> HeadingPath { get; }

    /// <summary>
    /// 섹션 제목 (현재 청크가 속한 섹션)
    /// </summary>
    string? SectionTitle { get; }

    // ===================================================================
    // 품질 메트릭
    // ===================================================================

    /// <summary>
    /// 청크 품질 점수 (0.0 - 1.0)
    /// 의미적 완결성, 정보 밀도 등을 반영
    /// </summary>
    double Quality { get; }

    /// <summary>
    /// 컨텍스트 의존도 (0.0 - 1.0)
    /// 0에 가까울수록 독립적, 1에 가까울수록 전후 맥락 필요
    /// </summary>
    double ContextDependency { get; }

    // ===================================================================
    // 소스 참조
    // ===================================================================

    /// <summary>
    /// 소스 문서 메타데이터
    /// </summary>
    ISourceMetadata Source { get; }
}

/// <summary>
/// 소스 문서 메타데이터 인터페이스
/// 청크의 원본 문서 정보를 제공
/// </summary>
public interface ISourceMetadata
{
    // ===================================================================
    // 기본 식별
    // ===================================================================

    /// <summary>
    /// 소스 문서의 고유 식별자
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// 소스 유형
    /// 예: "url", "html", "file", "api"
    /// </summary>
    string SourceType { get; }

    /// <summary>
    /// 문서 제목
    /// </summary>
    string Title { get; }

    /// <summary>
    /// 소스 URL (웹 문서인 경우)
    /// </summary>
    string? Url { get; }

    /// <summary>
    /// 문서 처리/크롤링 시간
    /// </summary>
    DateTime CreatedAt { get; }

    // ===================================================================
    // 콘텐츠 정보
    // ===================================================================

    /// <summary>
    /// 문서 언어 코드 (ISO 639-1)
    /// 예: "ko", "en", "ja"
    /// </summary>
    string Language { get; }

    /// <summary>
    /// 총 단어 수
    /// </summary>
    int WordCount { get; }

    /// <summary>
    /// 총 청크 수
    /// </summary>
    int ChunkCount { get; }

    // ===================================================================
    // 웹 전용 메타데이터
    // ===================================================================

    /// <summary>
    /// 콘텐츠 발행일 (article:published_time 등)
    /// </summary>
    DateTime? PublishedAt { get; }

    /// <summary>
    /// 작성자
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// 키워드 목록
    /// </summary>
    IReadOnlyList<string> Keywords { get; }
}
