namespace WebFlux.Core.Options;

/// <summary>
/// 이미지-텍스트 변환 옵션을 정의하는 클래스
/// </summary>
public class ImageToTextOptions
{
    /// <summary>
    /// 설명의 세부 정도 (Low, Medium, High)
    /// </summary>
    public DetailLevel DetailLevel { get; set; } = DetailLevel.Medium;

    /// <summary>
    /// 최대 설명 길이 (토큰 수, 기본값: 500)
    /// </summary>
    public int MaxDescriptionLength { get; set; } = 500;

    /// <summary>
    /// 특정 관점에서의 설명 요청 (예: "기술적", "예술적", "비즈니스")
    /// </summary>
    public string? Perspective { get; set; }

    /// <summary>
    /// 포함할 요소들 (텍스트, 차트, 다이어그램 등)
    /// </summary>
    public List<string> IncludeElements { get; set; } = new();

    /// <summary>
    /// 제외할 요소들
    /// </summary>
    public List<string> ExcludeElements { get; set; } = new();

    /// <summary>
    /// 언어 설정 (기본값: 한국어)
    /// </summary>
    public string Language { get; set; } = "ko";

    /// <summary>
    /// 컨텍스트 정보 (이미지가 포함된 문서의 제목, 섹션 등)
    /// </summary>
    public string? Context { get; set; }
}

/// <summary>
/// 설명의 세부 정도를 나타내는 열거형
/// </summary>
public enum DetailLevel
{
    /// <summary>낮음 - 간단한 요약</summary>
    Low,
    /// <summary>보통 - 표준 설명</summary>
    Medium,
    /// <summary>높음 - 상세한 설명</summary>
    High
}