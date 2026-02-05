using WebFlux.Core.Interfaces;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 너비 우선 크롤링 전략 구현
/// BaseCrawler의 기본 CrawlWebsiteAsync가 이미 너비 우선(Queue 기반)이므로
/// 별도의 오버라이드 없이 그대로 사용합니다.
/// </summary>
public class BreadthFirstCrawler : BaseCrawler
{
    public BreadthFirstCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher)
    {
    }

    // BaseCrawler.CrawlWebsiteAsync는 이미 Queue 기반 너비 우선 탐색을 사용합니다.
    // 추가적인 오버라이드가 필요하지 않습니다.
}
