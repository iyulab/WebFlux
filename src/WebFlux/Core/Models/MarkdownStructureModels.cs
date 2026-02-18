namespace WebFlux.Core.Models;

/// <summary>
/// 마크다운 구조 정보
/// 95% 정확도 달성을 위한 상세 구조 데이터
/// </summary>
public class MarkdownStructureInfo
{
    /// <summary>원본 URL</summary>
#if NET8_0_OR_GREATER
    public required string SourceUrl { get; init; }
#else
    public string SourceUrl { get; init; } = string.Empty;
#endif

    /// <summary>분석 시간</summary>
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>문서 메타데이터</summary>
    public MarkdownDocumentMetadata Metadata { get; init; } = new();

    /// <summary>헤딩 구조</summary>
    public IReadOnlyList<MarkdownHeading> Headings { get; init; } = Array.Empty<MarkdownHeading>();

    /// <summary>목차 (Table of Contents)</summary>
    public TableOfContents TableOfContents { get; init; } = new();

    /// <summary>코드 블록</summary>
    public IReadOnlyList<MarkdownCodeBlock> CodeBlocks { get; init; } = Array.Empty<MarkdownCodeBlock>();

    /// <summary>링크 정보</summary>
    public IReadOnlyList<MarkdownLink> Links { get; init; } = Array.Empty<MarkdownLink>();

    /// <summary>이미지 정보</summary>
    public IReadOnlyList<MarkdownImage> Images { get; init; } = Array.Empty<MarkdownImage>();

    /// <summary>테이블 정보</summary>
    public IReadOnlyList<MarkdownTable> Tables { get; init; } = Array.Empty<MarkdownTable>();

    /// <summary>목록 정보</summary>
    public IReadOnlyList<MarkdownList> Lists { get; init; } = Array.Empty<MarkdownList>();

    /// <summary>인용구</summary>
    public IReadOnlyList<MarkdownQuote> Quotes { get; init; } = Array.Empty<MarkdownQuote>();

    /// <summary>수식 (LaTeX/MathJax)</summary>
    public IReadOnlyList<MarkdownMath> MathExpressions { get; init; } = Array.Empty<MarkdownMath>();

    /// <summary>각주</summary>
    public IReadOnlyList<MarkdownFootnote> Footnotes { get; init; } = Array.Empty<MarkdownFootnote>();

    /// <summary>임베드 콘텐츠 (YouTube, Twitter 등)</summary>
    public IReadOnlyList<MarkdownEmbed> Embeds { get; init; } = Array.Empty<MarkdownEmbed>();

    /// <summary>문서 통계</summary>
    public MarkdownStatistics Statistics { get; init; } = new();

    /// <summary>구조 정확도 점수 (0.0 - 1.0)</summary>
    public double StructureAccuracy { get; init; }

    /// <summary>마크다운 품질 점수 (0.0 - 1.0)</summary>
    public double QualityScore { get; init; }
}

/// <summary>
/// 마크다운 문서 메타데이터
/// </summary>
public class MarkdownDocumentMetadata
{
    /// <summary>제목 (첫 번째 H1 또는 파일명)</summary>
    public string? Title { get; init; }

    /// <summary>작성자</summary>
    public string? Author { get; init; }

    /// <summary>작성일</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>수정일</summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <summary>태그</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>카테고리</summary>
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

    /// <summary>설명</summary>
    public string? Description { get; init; }

    /// <summary>언어</summary>
    public string? Language { get; init; }

    /// <summary>Front Matter (YAML/JSON)</summary>
    public IReadOnlyDictionary<string, object> FrontMatter { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// 마크다운 헤딩 정보
/// </summary>
public class MarkdownHeading
{
    /// <summary>헤딩 레벨 (1-6)</summary>
    public int Level { get; init; }

    /// <summary>헤딩 텍스트</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>앵커 ID</summary>
    public string? Id { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>문서 내 위치 (문자 오프셋)</summary>
    public int Position { get; init; }

    /// <summary>하위 헤딩들</summary>
    public IReadOnlyList<MarkdownHeading> Children { get; init; } = Array.Empty<MarkdownHeading>();
}

/// <summary>
/// 목차 (Table of Contents)
/// </summary>
public class TableOfContents
{
    /// <summary>목차 항목들</summary>
    public IReadOnlyList<TocItem> Items { get; init; } = Array.Empty<TocItem>();

    /// <summary>최대 깊이</summary>
    public int MaxDepth { get; init; }

    /// <summary>총 항목 수</summary>
    public int TotalItems { get; init; }

    /// <summary>목차 마크다운</summary>
    public string MarkdownContent { get; init; } = string.Empty;
}

/// <summary>
/// 목차 항목
/// </summary>
public class TocItem
{
    /// <summary>제목</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>링크 (앵커)</summary>
    public string Link { get; init; } = string.Empty;

    /// <summary>레벨</summary>
    public int Level { get; init; }

    /// <summary>하위 항목들</summary>
    public IReadOnlyList<TocItem> Children { get; init; } = Array.Empty<TocItem>();
}

/// <summary>
/// 마크다운 코드 블록
/// </summary>
public class MarkdownCodeBlock
{
    /// <summary>코드 내용</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>언어</summary>
    public string? Language { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>라인 수</summary>
    public int LineCount { get; init; }

    /// <summary>파일명 (지정된 경우)</summary>
    public string? FileName { get; init; }

    /// <summary>하이라이트 라인</summary>
    public IReadOnlyList<int> HighlightLines { get; init; } = Array.Empty<int>();

    /// <summary>인라인 코드 여부</summary>
    public bool IsInline { get; init; }
}

/// <summary>
/// 마크다운 링크
/// </summary>
public class MarkdownLink
{
    /// <summary>링크 텍스트</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>URL</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>제목 (title 속성)</summary>
    public string? Title { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>링크 타입</summary>
    public MarkdownLinkType Type { get; init; }

    /// <summary>참조 라벨 (참조 링크인 경우)</summary>
    public string? ReferenceLabel { get; init; }

    /// <summary>유효성 검사 결과</summary>
    public LinkValidationResult? ValidationResult { get; init; }
}

/// <summary>
/// 마크다운 링크 타입
/// </summary>
public enum MarkdownLinkType
{
    /// <summary>인라인 링크</summary>
    Inline,
    /// <summary>참조 링크</summary>
    Reference,
    /// <summary>자동 링크</summary>
    AutoLink,
    /// <summary>이메일 링크</summary>
    Email,
    /// <summary>내부 링크 (앵커)</summary>
    Internal
}

/// <summary>
/// 링크 유효성 검사 결과
/// </summary>
public class LinkValidationResult
{
    /// <summary>유효 여부</summary>
    public bool IsValid { get; init; }

    /// <summary>응답 코드 (HTTP 링크인 경우)</summary>
    public int? StatusCode { get; init; }

    /// <summary>오류 메시지</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>검사 시간</summary>
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 마크다운 이미지
/// </summary>
public class MarkdownImage
{
    /// <summary>대체 텍스트</summary>
    public string AltText { get; init; } = string.Empty;

    /// <summary>이미지 URL</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>제목</summary>
    public string? Title { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>이미지 크기 (감지된 경우)</summary>
    public ImageDimensions? Dimensions { get; init; }

    /// <summary>파일 크기 (감지된 경우)</summary>
    public long? FileSize { get; init; }

    /// <summary>MIME 타입</summary>
    public string? MimeType { get; init; }
}

/// <summary>
/// 마크다운 테이블
/// </summary>
public class MarkdownTable
{
    /// <summary>헤더 행</summary>
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();

    /// <summary>데이터 행들</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();

    /// <summary>열 정렬</summary>
    public IReadOnlyList<TableColumnAlignment> ColumnAlignments { get; init; } = Array.Empty<TableColumnAlignment>();

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>열 수</summary>
    public int ColumnCount { get; init; }

    /// <summary>행 수 (헤더 제외)</summary>
    public int RowCount { get; init; }

    /// <summary>캡션</summary>
    public string? Caption { get; init; }
}

/// <summary>
/// 테이블 열 정렬
/// </summary>
public enum TableColumnAlignment
{
    /// <summary>정렬 없음</summary>
    None,
    /// <summary>왼쪽 정렬</summary>
    Left,
    /// <summary>가운데 정렬</summary>
    Center,
    /// <summary>오른쪽 정렬</summary>
    Right
}

/// <summary>
/// 마크다운 목록
/// </summary>
public class MarkdownList
{
    /// <summary>목록 항목들</summary>
    public IReadOnlyList<MarkdownListItem> Items { get; init; } = Array.Empty<MarkdownListItem>();

    /// <summary>순서 있는 목록 여부</summary>
    public bool IsOrdered { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>들여쓰기 레벨</summary>
    public int IndentLevel { get; init; }

    /// <summary>마커 타입 (*, -, +, 1., a. 등)</summary>
    public string MarkerType { get; init; } = string.Empty;
}

/// <summary>
/// 마크다운 목록 항목
/// </summary>
public class MarkdownListItem
{
    /// <summary>항목 텍스트</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>체크박스 상태 (작업 목록인 경우)</summary>
    public bool? IsChecked { get; init; }

    /// <summary>하위 목록</summary>
    public MarkdownList? SubList { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }
}

/// <summary>
/// 마크다운 인용구
/// </summary>
public class MarkdownQuote
{
    /// <summary>인용 내용</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>인용 레벨 (중첩 정도)</summary>
    public int Level { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>라인 수</summary>
    public int LineCount { get; init; }

    /// <summary>저자 (명시된 경우)</summary>
    public string? Author { get; init; }
}

/// <summary>
/// 마크다운 수식
/// </summary>
public class MarkdownMath
{
    /// <summary>수식 내용</summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>인라인 수식 여부</summary>
    public bool IsInline { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>수식 타입 (LaTeX, MathJax 등)</summary>
    public string? MathType { get; init; }
}

/// <summary>
/// 마크다운 각주
/// </summary>
public class MarkdownFootnote
{
    /// <summary>각주 ID</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>각주 내용</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>참조 라인 번호</summary>
    public int ReferenceLineNumber { get; init; }

    /// <summary>정의 라인 번호</summary>
    public int DefinitionLineNumber { get; init; }
}

/// <summary>
/// 마크다운 임베드 콘텐츠
/// </summary>
public class MarkdownEmbed
{
    /// <summary>임베드 타입</summary>
    public MarkdownEmbedType Type { get; init; }

    /// <summary>URL</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>제목</summary>
    public string? Title { get; init; }

    /// <summary>설명</summary>
    public string? Description { get; init; }

    /// <summary>썸네일 URL</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>라인 번호</summary>
    public int LineNumber { get; init; }

    /// <summary>임베드 메타데이터</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// 마크다운 임베드 타입
/// </summary>
public enum MarkdownEmbedType
{
    /// <summary>YouTube 동영상</summary>
    YouTube,
    /// <summary>Vimeo 동영상</summary>
    Vimeo,
    /// <summary>Twitter 트윗</summary>
    Twitter,
    /// <summary>GitHub Gist</summary>
    GitHubGist,
    /// <summary>CodePen</summary>
    CodePen,
    /// <summary>일반 임베드</summary>
    Generic
}

/// <summary>
/// 마크다운 문서 통계
/// </summary>
public class MarkdownStatistics
{
    /// <summary>총 라인 수</summary>
    public int TotalLines { get; init; }

    /// <summary>콘텐츠 라인 수 (빈 줄 제외)</summary>
    public int ContentLines { get; init; }

    /// <summary>단어 수</summary>
    public int WordCount { get; init; }

    /// <summary>문자 수</summary>
    public int CharacterCount { get; init; }

    /// <summary>문자 수 (공백 제외)</summary>
    public int CharacterCountNoSpaces { get; init; }

    /// <summary>문단 수</summary>
    public int ParagraphCount { get; init; }

    /// <summary>추정 읽기 시간 (분)</summary>
    public int EstimatedReadingTimeMinutes { get; init; }

    /// <summary>복잡도 점수 (0.0 - 1.0)</summary>
    public double ComplexityScore { get; init; }

    /// <summary>가독성 점수 (0.0 - 1.0)</summary>
    public double ReadabilityScore { get; init; }
}

/// <summary>
/// 마크다운 변환 결과
/// </summary>
public class MarkdownConversionResult
{
    /// <summary>변환된 HTML</summary>
    public string Html { get; init; } = string.Empty;

    /// <summary>구조 정보</summary>
    public MarkdownStructureInfo StructureInfo { get; init; } = new() { SourceUrl = string.Empty };

    /// <summary>변환 옵션</summary>
    public MarkdownConversionOptions Options { get; init; } = new();

    /// <summary>변환 시간</summary>
    public DateTimeOffset ConvertedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>변환 성능 정보</summary>
    public ConversionPerformanceInfo Performance { get; init; } = new();
}

/// <summary>
/// 마크다운 변환 옵션
/// </summary>
public class MarkdownConversionOptions
{
    /// <summary>확장 기능 활성화</summary>
    public bool EnableExtensions { get; init; } = true;

    /// <summary>목차 생성</summary>
    public bool GenerateTableOfContents { get; init; } = true;

    /// <summary>코드 하이라이팅</summary>
    public bool EnableCodeHighlighting { get; init; } = true;

    /// <summary>수식 지원</summary>
    public bool EnableMath { get; init; } = true;

    /// <summary>각주 지원</summary>
    public bool EnableFootnotes { get; init; } = true;

    /// <summary>작업 목록 지원</summary>
    public bool EnableTaskLists { get; init; } = true;

    /// <summary>테이블 지원</summary>
    public bool EnableTables { get; init; } = true;

    /// <summary>자동 링크</summary>
    public bool EnableAutoLinks { get; init; } = true;

    /// <summary>이모지 변환</summary>
    public bool EnableEmojis { get; init; }

    /// <summary>링크 검증</summary>
    public bool ValidateLinks { get; init; }

    /// <summary>이미지 정보 추출</summary>
    public bool ExtractImageInfo { get; init; } = true;

    /// <summary>앵커 ID 생성</summary>
    public bool GenerateAnchorIds { get; init; } = true;

    /// <summary>사용자 정의 설정</summary>
    public IReadOnlyDictionary<string, object> CustomSettings { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// 변환 성능 정보
/// </summary>
public class ConversionPerformanceInfo
{
    /// <summary>파싱 시간 (밀리초)</summary>
    public long ParsingTimeMs { get; init; }

    /// <summary>분석 시간 (밀리초)</summary>
    public long AnalysisTimeMs { get; init; }

    /// <summary>변환 시간 (밀리초)</summary>
    public long ConversionTimeMs { get; init; }

    /// <summary>총 처리 시간 (밀리초)</summary>
    public long TotalTimeMs { get; init; }

    /// <summary>메모리 사용량 (바이트)</summary>
    public long MemoryUsedBytes { get; init; }

    /// <summary>처리된 요소 수</summary>
    public int ProcessedElementCount { get; init; }
}

/// <summary>
/// 마크다운 품질 평가
/// </summary>
public class MarkdownQualityAssessment
{
    /// <summary>전체 품질 점수 (0.0 - 1.0)</summary>
    public double OverallScore { get; init; }

    /// <summary>구조 품질 점수</summary>
    public double StructuralQuality { get; init; }

    /// <summary>콘텐츠 품질 점수</summary>
    public double ContentQuality { get; init; }

    /// <summary>가독성 점수</summary>
    public double ReadabilityScore { get; init; }

    /// <summary>접근성 점수</summary>
    public double AccessibilityScore { get; init; }

    /// <summary>SEO 친화성 점수</summary>
    public double SeoFriendliness { get; init; }

    /// <summary>문제점 목록</summary>
    public IReadOnlyList<QualityIssue> Issues { get; init; } = Array.Empty<QualityIssue>();

    /// <summary>개선 권장사항</summary>
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();

    /// <summary>강점</summary>
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 품질 문제
/// </summary>
public class QualityIssue
{
    /// <summary>문제 타입</summary>
    public QualityIssueType Type { get; init; }

    /// <summary>심각도</summary>
    public QualityIssueSeverity Severity { get; init; }

    /// <summary>문제 설명</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>라인 번호</summary>
    public int? LineNumber { get; init; }

    /// <summary>해결 방법</summary>
    public string? Solution { get; init; }
}

/// <summary>
/// 품질 문제 타입
/// </summary>
public enum QualityIssueType
{
    /// <summary>헤딩 구조</summary>
    HeadingStructure,
    /// <summary>링크 문제</summary>
    BrokenLink,
    /// <summary>이미지 문제</summary>
    ImageIssue,
    /// <summary>접근성 문제</summary>
    Accessibility,
    /// <summary>SEO 문제</summary>
    Seo,
    /// <summary>문법 오류</summary>
    Syntax,
    /// <summary>가독성 문제</summary>
    Readability
}

/// <summary>
/// 품질 문제 심각도
/// </summary>
public enum QualityIssueSeverity
{
    /// <summary>정보</summary>
    Info,
    /// <summary>경고</summary>
    Warning,
    /// <summary>오류</summary>
    Error,
    /// <summary>치명적</summary>
    Critical
}