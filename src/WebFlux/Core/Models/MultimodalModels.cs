namespace WebFlux.Core.Models;

/// <summary>
/// 이미지-텍스트 변환 결과 모델 (Phase 5A.2 멀티모달 파이프라인)
/// </summary>
public class ImageToTextResult
{
    /// <summary>이미지에서 추출된 텍스트</summary>
    public string ExtractedText { get; set; } = string.Empty;

    /// <summary>변환 신뢰도 (0.0 ~ 1.0)</summary>
    public double Confidence { get; set; }

    /// <summary>변환 성공 여부</summary>
    public bool IsSuccess { get; set; }

    /// <summary>원본 이미지 URL</summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>처리 시간 (밀리초)</summary>
    public int ProcessingTimeMs { get; set; }

    /// <summary>사용된 모델 정보</summary>
    public string? ModelUsed { get; set; }

    /// <summary>오류 메시지 (실패 시)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>추가 메타데이터</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>감지된 언어</summary>
    public string? DetectedLanguage { get; set; }

    /// <summary>이미지 분석 결과 (객체, 텍스트, 장면 등)</summary>
    public ImageAnalysis? Analysis { get; set; }
}

/// <summary>
/// 이미지 분석 결과 상세 정보
/// </summary>
public class ImageAnalysis
{
    /// <summary>감지된 객체 목록</summary>
    public List<string> DetectedObjects { get; set; } = new();

    /// <summary>장면 설명</summary>
    public string? SceneDescription { get; set; }

    /// <summary>텍스트 영역 정보</summary>
    public List<TextRegion> TextRegions { get; set; } = new();

    /// <summary>이미지 품질 점수 (0.0 ~ 1.0)</summary>
    public double QualityScore { get; set; }

    /// <summary>이미지 복잡도 (0.0 ~ 1.0)</summary>
    public double ComplexityScore { get; set; }
}

/// <summary>
/// 텍스트 영역 정보
/// </summary>
public class TextRegion
{
    /// <summary>텍스트 내용</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>신뢰도</summary>
    public double Confidence { get; set; }

    /// <summary>경계 상자 (x, y, width, height)</summary>
    public BoundingBox? BoundingBox { get; set; }

    /// <summary>텍스트 유형 (제목, 본문, 캡션 등)</summary>
    public string? TextType { get; set; }
}

/// <summary>
/// 경계 상자 정보
/// </summary>
public class BoundingBox
{
    /// <summary>X 좌표</summary>
    public double X { get; set; }

    /// <summary>Y 좌표</summary>
    public double Y { get; set; }

    /// <summary>너비</summary>
    public double Width { get; set; }

    /// <summary>높이</summary>
    public double Height { get; set; }
}

/// <summary>
/// 멀티모달 처리 파이프라인 결과
/// </summary>
public class MultimodalProcessingResult
{
    /// <summary>처리된 텍스트 (원본 + 이미지 텍스트 통합)</summary>
    public string CombinedText { get; set; } = string.Empty;

    /// <summary>원본 텍스트</summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>이미지에서 추출된 텍스트 목록</summary>
    public List<string> ImageTexts { get; set; } = new();

    /// <summary>처리된 이미지 정보</summary>
    public List<ProcessedImageInfo> ProcessedImages { get; set; } = new();

    /// <summary>전체 처리 시간 (밀리초)</summary>
    public int TotalProcessingTimeMs { get; set; }

    /// <summary>성공한 이미지 수</summary>
    public int SuccessfulImages { get; set; }

    /// <summary>실패한 이미지 수</summary>
    public int FailedImages { get; set; }

    /// <summary>품질 점수 (0.0 ~ 1.0)</summary>
    public double QualityScore { get; set; }
}

/// <summary>
/// 처리된 이미지 정보
/// </summary>
public class ProcessedImageInfo
{
    /// <summary>이미지 URL</summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>추출된 텍스트</summary>
    public string ExtractedText { get; set; } = string.Empty;

    /// <summary>처리 성공 여부</summary>
    public bool IsSuccess { get; set; }

    /// <summary>신뢰도</summary>
    public double Confidence { get; set; }

    /// <summary>오류 메시지 (실패 시)</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>처리 시간 (밀리초)</summary>
    public int ProcessingTimeMs { get; set; }
}

/// <summary>
/// 멀티모달 처리 통계
/// </summary>
public class MultimodalStatistics
{
    public int TotalProcessed { get; set; }
    public int ImagesProcessed { get; set; }
    public int SuccessfulConversions { get; set; }
    public int ErrorCount { get; set; }
    public double AverageQualityScore { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
}