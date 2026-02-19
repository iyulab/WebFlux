using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 크롤러 팩토리 인터페이스
/// Interface Provider 패턴에서 크롤러 인스턴스 생성을 담당
/// </summary>
public interface ICrawlerFactory
{
    /// <summary>
    /// 크롤링 전략에 따른 크롤러 인스턴스를 생성합니다
    /// </summary>
    /// <param name="strategy">크롤링 전략</param>
    /// <returns>크롤러 인스턴스</returns>
    ICrawler CreateCrawler(CrawlStrategy strategy);
}

/// <summary>
/// 크롤러 팩토리 구현
/// </summary>
public class CrawlerFactory : ICrawlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CrawlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICrawler CreateCrawler(CrawlStrategy strategy)
    {
        return strategy switch
        {
            CrawlStrategy.BreadthFirst => (ICrawler)_serviceProvider.GetService(typeof(BreadthFirstCrawler))!,
            CrawlStrategy.DepthFirst => (ICrawler)_serviceProvider.GetService(typeof(DepthFirstCrawler))!,
            CrawlStrategy.Intelligent => (ICrawler)_serviceProvider.GetService(typeof(IntelligentCrawler))!,
            CrawlStrategy.Sitemap => (ICrawler)_serviceProvider.GetService(typeof(SitemapCrawler))!,
            CrawlStrategy.Dynamic => (ICrawler)_serviceProvider.GetService(typeof(PlaywrightCrawler))!,
            _ => throw new ArgumentException($"Unknown crawl strategy: {strategy}")
        };
    }
}