namespace WebFlux.Core.Options;

/// <summary>
/// HTML 정리 옵션
/// 보일러플레이트 제거, 메인 콘텐츠 추출 설정
/// </summary>
public class HtmlCleaningOptions
{
    /// <summary>
    /// 메인 콘텐츠만 추출 (nav, header, footer 등 제거)
    /// </summary>
    public bool OnlyMainContent { get; set; } = true;

    /// <summary>
    /// 추가로 제거할 CSS 셀렉터 목록
    /// </summary>
    public List<string> AdditionalRemoveSelectors { get; set; } = new();

    /// <summary>
    /// 제거하지 않을 CSS 셀렉터 목록 (기본 제거 대상에서 보존)
    /// </summary>
    public List<string> KeepSelectors { get; set; } = new();

    /// <summary>
    /// 이미지 URL을 절대 경로로 변환
    /// </summary>
    public bool ConvertRelativeUrls { get; set; } = true;

    /// <summary>
    /// srcset에서 최적 이미지 URL 선택
    /// </summary>
    public bool OptimizeSrcset { get; set; } = true;

    /// <summary>
    /// 기본 옵션
    /// </summary>
    public static HtmlCleaningOptions Default => new();
}
