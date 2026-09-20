namespace WebFlux.Core.Models;

/// <summary>
/// 크롤링 전략 열거형
/// </summary>
/// <remarks>
/// Values are explicit and 3 and 4 are left empty on purpose: they belonged to two members that were
/// removed because no crawler implemented them. Reusing those numbers would silently change the
/// meaning of a configuration that stores the strategy as a number.
/// </remarks>
public enum CrawlStrategy
{
    /// <summary>너비 우선 탐색</summary>
    BreadthFirst = 0,
    /// <summary>깊이 우선 탐색</summary>
    DepthFirst = 1,
    /// <summary>Sitemap 기반</summary>
    Sitemap = 2,
    /// <summary>
    /// 동적 렌더링 크롤링 (SPA 지원). <b>WebFlux.Playwright 패키지가 필요하다</b> —
    /// 브라우저 런타임을 끌어오므로 본체에 포함하지 않는다. 등록 없이 요청하면 어느 패키지가
    /// 빠졌는지 알려주는 <see cref="InvalidOperationException"/>으로 실패한다.
    /// </summary>
    Dynamic = 5
}
