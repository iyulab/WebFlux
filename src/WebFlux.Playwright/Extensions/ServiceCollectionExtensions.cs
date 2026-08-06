using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;
using WebFlux.Core.Interfaces;
using WebFlux.Services.Crawlers;

namespace WebFlux.Extensions;

/// <summary>
/// Registers the Playwright-backed dynamic renderer. WebFlux resolves it through the
/// <c>Dynamic</c> crawler key, so nothing else in a consumer's code changes when this is added.
/// </summary>
public static class PlaywrightServiceCollectionExtensions
{
    /// <summary>
    /// Playwright 관련 서비스를 등록합니다. <c>AddWebFlux</c> 이후에 호출하십시오.
    /// </summary>
    /// <remarks>
    /// 이 호출이 없으면 <see cref="WebFlux.Core.Models.CrawlStrategy.Dynamic"/> 요청과
    /// <c>UseDynamicRendering</c> 옵션은 어느 패키지가 빠졌는지 알려주는 예외로 실패합니다.
    /// </remarks>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxPlaywright(this IServiceCollection services)
    {
        // Playwright 인스턴스를 Singleton으로 등록 (애플리케이션당 하나)
        // NOTE: 동기 대기(sync-over-async)다. 등록 시점 1회라 실害는 없으나 정석은 아니며,
        //       이 분리와 별개 사안이라 형태를 그대로 옮겼다.
        services.TryAddSingleton<IPlaywright>(_ =>
            Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult());

        services.TryAddTransient<PlaywrightCrawler>();
        services.AddKeyedTransient<ICrawler, PlaywrightCrawler>(CrawlerKeys.Dynamic);

        return services;
    }
}
