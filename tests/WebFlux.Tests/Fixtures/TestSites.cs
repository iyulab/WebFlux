namespace WebFlux.Tests.Fixtures;

/// <summary>
/// Public sites the <c>Category=LiveWeb</c> facts crawl. Each one allows crawling in its robots.txt or exists for testing.
/// HTTP behaviour that does not need a real site (status codes, delays, large bodies, robots rules) is served locally by
/// <see cref="TestSupport.LocalHttpServer"/> instead — see <c>CrawlerHttpBehaviourTests</c>.
/// </summary>
public static class TestSites
{
    /// <summary>A scraping-practice site (quotes). robots.txt: allowed.</summary>
    public const string QuotesToScrape = "https://quotes.toscrape.com";

    /// <summary>IANA's example domain — one plain HTML page. robots.txt: allowed.</summary>
    public const string ExampleCom = "https://example.com";

    /// <summary>A scraping-practice e-commerce site. robots.txt: allowed.</summary>
    public const string BooksToScrape = "https://books.toscrape.com";
}
