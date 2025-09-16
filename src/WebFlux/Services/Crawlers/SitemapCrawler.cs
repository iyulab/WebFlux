using System.Xml;
using System.Threading.Channels;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Collections.Concurrent;

namespace WebFlux.Services.Crawlers;

/// <summary>
/// Sitemap 기반 크롤러
/// robots.txt와 sitemap.xml을 참조하여 효율적으로 크롤링
/// </summary>
public class SitemapCrawler : BaseCrawler
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly List<SitemapEntry> _sitemapEntries = new();
    private readonly HashSet<string> _processedSitemaps = new();

    public SitemapCrawler(IHttpClientService httpClient, IEventPublisher eventPublisher)
        : base(httpClient, eventPublisher)
    {
        _concurrencySemaphore = new SemaphoreSlim(8); // Sitemap 기반은 높은 동시성 가능
    }

    /// <summary>
    /// 내부 크롤링 로직 - Sitemap 기반
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 콘텐츠 스트림</returns>
    protected override async IAsyncEnumerable<WebContent> CrawlInternalAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 시작 URL들에서 도메인 추출 및 sitemap 수집
        await CollectSitemapsAsync(cancellationToken);

        if (!_sitemapEntries.Any())
        {
            // Sitemap이 없으면 기본 크롤링으로 폴백
            await _eventPublisher.PublishAsync(new CrawlWarningEvent
            {
                Message = "No sitemaps found, falling back to regular crawling",
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            await foreach (var content in FallbackCrawlAsync(cancellationToken))
            {
                yield return content;
            }
            yield break;
        }

        // 우선순위에 따라 sitemap 엔트리 정렬
        var prioritizedEntries = PrioritizeSitemapEntries(_sitemapEntries);

        // 동시성 제한 설정
        var maxConcurrency = Math.Min(_configuration.MaxConcurrentRequests, 10);
        _concurrencySemaphore.Release(_concurrencySemaphore.CurrentCount);
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        // 처리 채널 생성
        var channel = Channel.CreateUnbounded<WebContent>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // 백그라운드 처리 시작
        var processingTask = ProcessSitemapEntriesAsync(prioritizedEntries, writer, semaphore, cancellationToken);

        try
        {
            // 결과를 스트리밍으로 반환
            await foreach (var content in reader.ReadAllAsync(cancellationToken))
            {
                yield return content;

                // 최대 페이지 수 확인
                lock (_lockObject)
                {
                    if (_successCount >= _configuration.MaxPages)
                    {
                        await StopAsync();
                        break;
                    }
                }
            }
        }
        finally
        {
            await writer.TryCompleteAsync();
            await processingTask;
            _isRunning = false;

            await _eventPublisher.PublishAsync(new CrawlCompletedEvent
            {
                TotalProcessed = _processedCount,
                SuccessCount = _successCount,
                ErrorCount = _errorCount,
                Duration = TimeSpan.Zero,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Sitemap 수집
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task CollectSitemapsAsync(CancellationToken cancellationToken)
    {
        var domains = new HashSet<string>();

        // 시작 URL들에서 도메인 추출
        while (_urlQueue.TryDequeue(out var url))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                domains.Add($"{uri.Scheme}://{uri.Host}");
            }
        }

        // 각 도메인에서 sitemap 수집
        var tasks = domains.Select(domain => CollectDomainSitemapsAsync(domain, cancellationToken));
        await Task.WhenAll(tasks);

        await _eventPublisher.PublishAsync(new SitemapDiscoveryEvent
        {
            DomainsProcessed = domains.Count,
            SitemapsFound = _processedSitemaps.Count,
            TotalUrls = _sitemapEntries.Count,
            Timestamp = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    /// <summary>
    /// 특정 도메인의 sitemap 수집
    /// </summary>
    /// <param name="domain">도메인</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task CollectDomainSitemapsAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            // robots.txt에서 sitemap 위치 확인
            await CheckRobotsTxtAsync(domain, cancellationToken);

            // 일반적인 sitemap 위치들 확인
            var commonSitemapUrls = new[]
            {
                $"{domain}/sitemap.xml",
                $"{domain}/sitemap_index.xml",
                $"{domain}/sitemaps.xml",
                $"{domain}/sitemap/sitemap.xml"
            };

            foreach (var sitemapUrl in commonSitemapUrls)
            {
                await ProcessSitemapUrlAsync(sitemapUrl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _eventPublisher.PublishAsync(new CrawlErrorEvent
            {
                Url = domain,
                Error = $"Failed to collect sitemaps: {ex.Message}",
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    /// <summary>
    /// robots.txt 확인하여 sitemap 위치 추출
    /// </summary>
    /// <param name="domain">도메인</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task CheckRobotsTxtAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var robotsUrl = $"{domain}/robots.txt";
            var robotsContent = await _httpClient.GetStringAsync(robotsUrl, cancellationToken: cancellationToken);

            var lines = robotsContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                {
                    var sitemapUrl = trimmedLine.Substring(8).Trim();
                    await ProcessSitemapUrlAsync(sitemapUrl, cancellationToken);
                }
            }
        }
        catch
        {
            // robots.txt가 없거나 접근할 수 없는 경우는 무시
        }
    }

    /// <summary>
    /// 개별 sitemap URL 처리
    /// </summary>
    /// <param name="sitemapUrl">Sitemap URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessSitemapUrlAsync(string sitemapUrl, CancellationToken cancellationToken)
    {
        if (_processedSitemaps.Contains(sitemapUrl))
            return;

        _processedSitemaps.Add(sitemapUrl);

        try
        {
            var sitemapContent = await _httpClient.GetStringAsync(sitemapUrl, cancellationToken: cancellationToken);
            await ParseSitemapAsync(sitemapContent, sitemapUrl, cancellationToken);
        }
        catch
        {
            // Sitemap 파싱 실패는 로그만 남기고 계속 진행
        }
    }

    /// <summary>
    /// Sitemap XML 파싱
    /// </summary>
    /// <param name="sitemapContent">Sitemap 콘텐츠</param>
    /// <param name="sitemapUrl">Sitemap URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ParseSitemapAsync(string sitemapContent, string sitemapUrl, CancellationToken cancellationToken)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(sitemapContent);

            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("sm", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Sitemap index 확인
            var sitemapNodes = doc.SelectNodes("//sm:sitemap/sm:loc", namespaceManager);
            if (sitemapNodes?.Count > 0)
            {
                // Sitemap index인 경우 - 하위 sitemap들 처리
                foreach (XmlNode node in sitemapNodes)
                {
                    var childSitemapUrl = node.InnerText.Trim();
                    await ProcessSitemapUrlAsync(childSitemapUrl, cancellationToken);
                }
                return;
            }

            // URL 엔트리들 파싱
            var urlNodes = doc.SelectNodes("//sm:url", namespaceManager);
            if (urlNodes?.Count > 0)
            {
                foreach (XmlNode urlNode in urlNodes)
                {
                    var entry = ParseSitemapEntry(urlNode, namespaceManager);
                    if (entry != null && ShouldCrawlUrl(entry.Url))
                    {
                        _sitemapEntries.Add(entry);
                    }
                }
            }
        }
        catch (XmlException ex)
        {
            await _eventPublisher.PublishAsync(new CrawlErrorEvent
            {
                Url = sitemapUrl,
                Error = $"XML parsing failed: {ex.Message}",
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Sitemap 엔트리 파싱
    /// </summary>
    /// <param name="urlNode">URL XML 노드</param>
    /// <param name="nsManager">네임스페이스 매니저</param>
    /// <returns>Sitemap 엔트리</returns>
    private SitemapEntry? ParseSitemapEntry(XmlNode urlNode, XmlNamespaceManager nsManager)
    {
        var locNode = urlNode.SelectSingleNode("sm:loc", nsManager);
        if (locNode == null) return null;

        var entry = new SitemapEntry
        {
            Url = locNode.InnerText.Trim()
        };

        // 우선순위 파싱
        var priorityNode = urlNode.SelectSingleNode("sm:priority", nsManager);
        if (priorityNode != null && double.TryParse(priorityNode.InnerText, out var priority))
        {
            entry.Priority = priority;
        }

        // 마지막 수정일 파싱
        var lastModNode = urlNode.SelectSingleNode("sm:lastmod", nsManager);
        if (lastModNode != null && DateTimeOffset.TryParse(lastModNode.InnerText, out var lastMod))
        {
            entry.LastModified = lastMod;
        }

        // 변경 빈도 파싱
        var changeFreqNode = urlNode.SelectSingleNode("sm:changefreq", nsManager);
        if (changeFreqNode != null)
        {
            entry.ChangeFrequency = changeFreqNode.InnerText.Trim();
        }

        return entry;
    }

    /// <summary>
    /// Sitemap 엔트리 우선순위 결정
    /// </summary>
    /// <param name="entries">Sitemap 엔트리 목록</param>
    /// <returns>우선순위가 적용된 엔트리 목록</returns>
    private List<SitemapEntry> PrioritizeSitemapEntries(List<SitemapEntry> entries)
    {
        return entries
            .OrderByDescending(e => e.Priority) // 높은 우선순위
            .ThenByDescending(e => e.LastModified) // 최근 수정일
            .ThenBy(e => GetChangeFrequencyWeight(e.ChangeFrequency)) // 변경 빈도
            .Take(_configuration.MaxPages)
            .ToList();
    }

    /// <summary>
    /// 변경 빈도 가중치
    /// </summary>
    /// <param name="changeFreq">변경 빈도</param>
    /// <returns>가중치</returns>
    private int GetChangeFrequencyWeight(string changeFreq)
    {
        return changeFreq?.ToLowerInvariant() switch
        {
            "always" => 1,
            "hourly" => 2,
            "daily" => 3,
            "weekly" => 4,
            "monthly" => 5,
            "yearly" => 6,
            "never" => 7,
            _ => 8
        };
    }

    /// <summary>
    /// Sitemap 엔트리 처리
    /// </summary>
    /// <param name="entries">처리할 엔트리 목록</param>
    /// <param name="writer">결과 채널 작성자</param>
    /// <param name="semaphore">동시성 제어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessSitemapEntriesAsync(
        List<SitemapEntry> entries,
        ChannelWriter<WebContent> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var tasks = entries.Select(entry =>
            ProcessSitemapEntryAsync(entry, writer, semaphore, cancellationToken));

        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {
            writer.TryComplete();
        }
    }

    /// <summary>
    /// 개별 Sitemap 엔트리 처리
    /// </summary>
    /// <param name="entry">Sitemap 엔트리</param>
    /// <param name="writer">결과 채널 작성자</param>
    /// <param name="semaphore">동시성 제어</param>
    /// <param name="cancellationToken">취소 토큰</param>
    private async Task ProcessSitemapEntryAsync(
        SitemapEntry entry,
        ChannelWriter<WebContent> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var content = await ProcessUrlAsync(entry.Url, cancellationToken);

            if (content != null)
            {
                // Sitemap 메타데이터 추가
                content.Metadata.AdditionalData["SitemapPriority"] = entry.Priority;
                content.Metadata.AdditionalData["SitemapLastModified"] = entry.LastModified?.ToString() ?? string.Empty;
                content.Metadata.AdditionalData["SitemapChangeFrequency"] = entry.ChangeFrequency ?? string.Empty;
                content.Metadata.AdditionalData["CrawlerType"] = "Sitemap";

                await writer.WriteAsync(content, cancellationToken);
            }

            // 요청 간 지연
            if (_configuration.DelayBetweenRequests > TimeSpan.Zero)
            {
                await Task.Delay(_configuration.DelayBetweenRequests, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await HandleFailedUrl(entry.Url, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Sitemap이 없을 때 기본 크롤링으로 폴백
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>크롤링된 콘텐츠</returns>
    private async IAsyncEnumerable<WebContent> FallbackCrawlAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 기본 너비 우선 크롤링으로 폴백
        var fallbackCrawler = new BreadthFirstCrawler(_httpClient, _eventPublisher);

        var startUrls = new List<string>();
        while (_urlQueue.TryDequeue(out var url))
        {
            startUrls.Add(url);
        }

        var contentStream = await fallbackCrawler.CrawlAsync(startUrls, _configuration, cancellationToken);

        await foreach (var content in contentStream.WithCancellation(cancellationToken))
        {
            yield return content;
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public override void Dispose()
    {
        _concurrencySemaphore?.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Sitemap 엔트리 클래스
    /// </summary>
    private class SitemapEntry
    {
        public string Url { get; set; } = string.Empty;
        public double Priority { get; set; } = 0.5;
        public DateTimeOffset? LastModified { get; set; }
        public string? ChangeFrequency { get; set; }
    }
}

/// <summary>
/// Sitemap 발견 이벤트
/// </summary>
public class SitemapDiscoveryEvent : ProcessingEvent
{
    public override string EventType => "SitemapDiscovery";
    public int DomainsProcessed { get; set; }
    public int SitemapsFound { get; set; }
    public int TotalUrls { get; set; }
}