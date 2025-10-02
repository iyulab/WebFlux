namespace WebFlux.Core.Models;

/// <summary>
/// 분석된 콘텐츠 (Analyze 단계 출력)
/// Stage 2: 가공 + 원본 유지 + 불필요한 요소 제거
/// </summary>
public class AnalyzedContent
{
    /// <summary>
    /// 원본 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 원본 콘텐츠 (보존)
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// 정리된 콘텐츠 (노이즈 제거, 구조 정리)
    /// </summary>
    public string CleanedContent { get; set; } = string.Empty;

    /// <summary>
    /// 콘텐츠 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 콘텐츠 섹션 목록
    /// </summary>
    public List<ContentSection> Sections { get; set; } = new();

    /// <summary>
    /// 표 데이터 목록
    /// </summary>
    public List<TableData> Tables { get; set; } = new();

    /// <summary>
    /// 이미지 데이터 목록
    /// </summary>
    public List<ImageData> Images { get; set; } = new();

    /// <summary>
    /// 구조 정보
    /// </summary>
    public StructureInfo Structure { get; set; } = new();

    /// <summary>
    /// 웹 콘텐츠 메타데이터
    /// </summary>
    public WebContentMetadata Metadata { get; set; } = new();

    /// <summary>
    /// 분석 품질 지표
    /// </summary>
    public AnalysisMetrics Metrics { get; set; } = new();

    /// <summary>
    /// 분석 시간
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 분석기 정보
    /// </summary>
    public string AnalyzerType { get; set; } = string.Empty;

    /// <summary>
    /// 추가 속성 (확장 가능)
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 콘텐츠 섹션
/// </summary>
public class ContentSection
{
    /// <summary>섹션 ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>섹션 제목</summary>
    public string Heading { get; set; } = string.Empty;

    /// <summary>섹션 레벨 (H1=1, H2=2, ...)</summary>
    public int Level { get; set; }

    /// <summary>섹션 콘텐츠</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>시작 위치 (문자 인덱스)</summary>
    public int StartPosition { get; set; }

    /// <summary>종료 위치 (문자 인덱스)</summary>
    public int EndPosition { get; set; }

    /// <summary>하위 섹션 목록</summary>
    public List<ContentSection> SubSections { get; set; } = new();

    /// <summary>섹션 메타데이터</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 표 데이터
/// </summary>
public class TableData
{
    /// <summary>표 ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>표 제목</summary>
    public string Caption { get; set; } = string.Empty;

    /// <summary>헤더 행</summary>
    public List<string> Headers { get; set; } = new();

    /// <summary>데이터 행 목록</summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>표 위치</summary>
    public int Position { get; set; }

    /// <summary>표 메타데이터</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 이미지 데이터
/// </summary>
public class ImageData
{
    /// <summary>이미지 ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>이미지 URL</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>대체 텍스트</summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>이미지 제목</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>이미지 설명 (LLM 생성)</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>이미지 위치</summary>
    public int Position { get; set; }

    /// <summary>이미지 크기 (너비x높이)</summary>
    public string? Dimensions { get; set; }

    /// <summary>이미지 메타데이터</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 구조 정보
/// </summary>
public class StructureInfo
{
    /// <summary>총 섹션 수</summary>
    public int TotalSections { get; set; }

    /// <summary>최대 섹션 깊이</summary>
    public int MaxDepth { get; set; }

    /// <summary>총 표 수</summary>
    public int TotalTables { get; set; }

    /// <summary>총 이미지 수</summary>
    public int TotalImages { get; set; }

    /// <summary>총 단어 수</summary>
    public int TotalWords { get; set; }

    /// <summary>총 문자 수</summary>
    public int TotalCharacters { get; set; }

    /// <summary>구조 복잡도 (0.0 ~ 1.0)</summary>
    public double Complexity { get; set; }

    /// <summary>추가 구조 정보</summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// 분석 품질 지표
/// </summary>
public class AnalysisMetrics
{
    /// <summary>콘텐츠 품질 (0.0 ~ 1.0)</summary>
    public double ContentQuality { get; set; }

    /// <summary>구조 복잡도 (0.0 ~ 1.0)</summary>
    public double StructureComplexity { get; set; }

    /// <summary>제거된 노이즈 요소 수</summary>
    public int NoiseElementsRemoved { get; set; }

    /// <summary>식별된 섹션 수</summary>
    public int SectionsIdentified { get; set; }

    /// <summary>분석 처리 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>추가 지표</summary>
    public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
}
