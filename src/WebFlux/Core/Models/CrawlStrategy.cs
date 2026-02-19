namespace WebFlux.Core.Models;

/// <summary>
/// 크롤링 전략 열거형
/// </summary>
public enum CrawlStrategy
{
    /// <summary>너비 우선 탐색</summary>
    BreadthFirst,
    /// <summary>깊이 우선 탐색</summary>
    DepthFirst,
    /// <summary>Sitemap 기반</summary>
    Sitemap,
    /// <summary>우선순위 기반</summary>
    Priority,
    /// <summary>llms.txt 메타데이터 기반 지능형 크롤링</summary>
    Intelligent,
    /// <summary>Playwright 기반 동적 렌더링 크롤링 (SPA 지원)</summary>
    Dynamic
}
