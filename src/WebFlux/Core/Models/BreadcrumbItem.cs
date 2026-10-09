namespace WebFlux.Core.Models;

/// <summary>
/// 브레드크럼 항목
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// 항목 텍스트
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 링크 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 항목 순서
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 현재 페이지 여부
    /// </summary>
    public bool IsCurrentPage { get; set; }
}
