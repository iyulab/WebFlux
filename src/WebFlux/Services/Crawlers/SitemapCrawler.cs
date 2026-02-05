using WebFlux.Core.Interfaces;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 사이트맵 기반 크롤링 전략 구현
/// sitemap.xml에서 URL을 추출하여 크롤링합니다.
/// BaseCrawler의 CrawlSitemapAsync를 그대로 사용합니다.
/// </summary>
public class SitemapCrawler : BaseCrawler
{
    public SitemapCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher)
    {
    }

    // BaseCrawler.CrawlSitemapAsync가 이미 sitemap.xml 파싱 및 크롤링을 구현합니다.
    // 추가적인 오버라이드가 필요하지 않습니다.
}
