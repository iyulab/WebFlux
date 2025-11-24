namespace WebFlux.Core.Models;

/// <summary>
/// 크롤링 전략 열거형
/// </summary>
public enum CrawlStrategy
{
    /// <summary>
    /// 깊이 우선 크롤링
    /// </summary>
    DepthFirst,

    /// <summary>
    /// 너비 우선 크롤링
    /// </summary>
    BreadthFirst,

    /// <summary>
    /// Sitemap 기반 크롤링
    /// </summary>
    Sitemap,

    /// <summary>
    /// 단일 페이지 크롤링
    /// </summary>
    SinglePage,

    /// <summary>
    /// 적응형 크롤링 (자동 선택)
    /// </summary>
    Adaptive
}