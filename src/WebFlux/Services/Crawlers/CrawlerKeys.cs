using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// Keys under which crawlers are registered. They are part of the contract between WebFlux and any
/// package that supplies a crawler it does not implement itself, so they are named once here rather
/// than repeated as literals at every registration and resolution site.
/// </summary>
public static class CrawlerKeys
{
    /// <summary>너비 우선 탐색 크롤러 키</summary>
    public const string BreadthFirst = "BreadthFirst";

    /// <summary>깊이 우선 탐색 크롤러 키</summary>
    public const string DepthFirst = "DepthFirst";

    /// <summary>Sitemap 기반 크롤러 키</summary>
    public const string Sitemap = "Sitemap";

    /// <summary>llms.txt 기반 지능형 크롤러 키</summary>
    public const string Intelligent = "Intelligent";

    /// <summary>
    /// 동적 렌더링 크롤러 키. WebFlux 본체는 이 키의 구현을 제공하지 않는다 —
    /// <c>WebFlux.Playwright</c> 패키지가 등록한다.
    /// </summary>
    public const string Dynamic = "Dynamic";
}

/// <summary>
/// Resolves the dynamic renderer, which WebFlux declares but does not implement.
/// </summary>
/// <remarks>
/// Three call paths can ask for dynamic rendering — the two crawler factories and the
/// <c>UseDynamicRendering</c> option, which routes there without the caller ever naming the strategy.
/// Left to the container, each fails differently: one returns null and faults later with no mention
/// of rendering, another reports a missing service key. Whichever path a consumer happens to be on,
/// the answer to "why did this fail" is the same, so the message is produced in one place.
/// </remarks>
public static class DynamicCrawlerResolver
{
    internal const string MissingPackageMessage =
        "Dynamic rendering was requested but no dynamic crawler is registered. " +
        "Install the WebFlux.Playwright package and call services.AddWebFluxPlaywright() after AddWebFlux(), " +
        "or choose a crawl strategy other than Dynamic (and leave CrawlOptions.UseDynamicRendering false).";

    /// <summary>
    /// 등록된 동적 크롤러를 반환합니다. 등록돼 있지 않으면 어느 패키지가 빠졌는지 알려주는
    /// <see cref="InvalidOperationException"/>을 던집니다.
    /// </summary>
    public static ICrawler Resolve(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var crawler = serviceProvider.GetKeyedService<ICrawler>(CrawlerKeys.Dynamic);
        return crawler ?? throw new InvalidOperationException(MissingPackageMessage);
    }
}
