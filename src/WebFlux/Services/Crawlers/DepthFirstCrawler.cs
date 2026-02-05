using System.Runtime.CompilerServices;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// 깊이 우선 크롤링 전략 구현
/// Stack 기반으로 깊이 우선 탐색을 수행합니다.
/// </summary>
public class DepthFirstCrawler : BaseCrawler
{
    public DepthFirstCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher)
    {
    }

    /// <summary>
    /// 깊이 우선 탐색으로 웹사이트를 크롤링합니다.
    /// Queue 대신 Stack을 사용하여 가장 최근에 발견된 링크를 먼저 탐색합니다.
    /// </summary>
    public override async IAsyncEnumerable<CrawlResult> CrawlWebsiteAsync(
        string startUrl,
        CrawlOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(startUrl))
            throw new ArgumentException("Start URL cannot be null or empty", nameof(startUrl));

        var stack = new Stack<(string url, int depth)>();
        var visited = new HashSet<string>();
        var maxDepth = options?.MaxDepth ?? 3;
        var maxPages = options?.MaxPages ?? 100;

        stack.Push((startUrl, 0));

        while (stack.Count > 0 && visited.Count < maxPages && !cancellationToken.IsCancellationRequested)
        {
            var (currentUrl, depth) = stack.Pop();

            if (visited.Contains(currentUrl) || depth > maxDepth)
                continue;

            visited.Add(currentUrl);

            var originalResult = await CrawlAsync(currentUrl, options, cancellationToken);
            var result = new CrawlResult
            {
                Url = originalResult.Url,
                FinalUrl = originalResult.FinalUrl,
                StatusCode = originalResult.StatusCode,
                IsSuccess = originalResult.IsSuccess,
                HtmlContent = originalResult.HtmlContent,
                Headers = originalResult.Headers,
                ContentType = originalResult.ContentType,
                Encoding = originalResult.Encoding,
                ContentLength = originalResult.ContentLength,
                ResponseTimeMs = originalResult.ResponseTimeMs,
                CrawledAt = originalResult.CrawledAt,
                Depth = depth,
                ParentUrl = originalResult.ParentUrl,
                DiscoveredLinks = originalResult.DiscoveredLinks,
                ErrorMessage = originalResult.ErrorMessage,
                Exception = originalResult.Exception,
                ImageUrls = originalResult.ImageUrls,
                Metadata = originalResult.Metadata,
                WebMetadata = originalResult.WebMetadata
            };

            yield return result;

            if (result.IsSuccess && depth < maxDepth)
            {
                // 깊이 우선: 링크들을 역순으로 스택에 추가하여 첫 번째 링크가 먼저 탐색되도록 함
                var linksToAdd = result.DiscoveredLinks
                    .Where(link => !visited.Contains(link) && ShouldCrawlUrl(link, startUrl))
                    .Reverse()
                    .ToList();

                foreach (var link in linksToAdd)
                {
                    stack.Push((link, depth + 1));
                }
            }

            // 예의상 지연
            if (options?.DelayMs > 0)
            {
                await Task.Delay(options.DelayMs, cancellationToken);
            }
        }
    }
}
