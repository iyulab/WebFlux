using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 사이트맵 분석 서비스
/// sitemap.xml, sitemap.txt, sitemap index 파일을 파싱하고 분석
/// </summary>
public class SitemapAnalyzer : ISitemapAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SitemapAnalyzer> _logger;
    private readonly Dictionary<string, SitemapAnalysisResult> _cache;
    private readonly SitemapAnalysisStatistics _statistics;
    private readonly object _cacheLock = new();
    private readonly object _statsLock = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(2);

    // 사이트맵 자동 감지를 위한 일반적인 경로들
    private static readonly string[] CommonSitemapPaths =
    {
        "/sitemap.xml",
        "/sitemap_index.xml",
        "/sitemap.txt",
        "/sitemaps.xml",
        "/sitemap/sitemap.xml",
        "/sitemap/index.xml",
        "/wp-sitemap.xml",
        "/feeds/all.sitemap.xml"
    };

    public SitemapAnalyzer(HttpClient httpClient, ILogger<SitemapAnalyzer> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = new Dictionary<string, SitemapAnalysisResult>();
        _statistics = new SitemapAnalysisStatistics();
    }

    public async Task<SitemapAnalysisResult> AnalyzeFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        lock (_statsLock)
        {
            _statistics.TotalAnalysisAttempts++;
        }

        // 캐시 확인
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(baseUrl, out var cachedResult) &&
                DateTimeOffset.UtcNow - cachedResult.AnalyzedAt < CacheExpiry)
            {
                return cachedResult;
            }
        }

        try
        {
            _logger.LogInformation("Starting sitemap analysis for {BaseUrl}", baseUrl);

            var discoveredSitemaps = new List<string>();
            var sitemapMetadataList = new List<SitemapMetadata>();

            // 1. robots.txt에서 사이트맵 URL 찾기 (다른 서비스에서 제공)
            // 2. 일반적인 경로에서 사이트맵 찾기
            foreach (var path in CommonSitemapPaths)
            {
                var sitemapUrl = new Uri(new Uri(baseUrl), path).ToString();

                try
                {
                    var metadata = await TryParseSitemapAsync(sitemapUrl, cancellationToken);
                    if (metadata != null)
                    {
                        discoveredSitemaps.Add(sitemapUrl);
                        sitemapMetadataList.Add(metadata);
                        _logger.LogDebug("Found sitemap at {SitemapUrl}", sitemapUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse sitemap at {SitemapUrl}", sitemapUrl);
                }
            }

            // 통합 사이트맵 메타데이터 생성
            var mergedMetadata = sitemapMetadataList.Any()
                ? await MergeSitemapsAsync(sitemapMetadataList)
                : null;

            // URL 패턴 분석
            var urlPatterns = mergedMetadata != null
                ? await AnalyzeUrlPatternsAsync(mergedMetadata)
                : null;

            var analysisTime = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            var result = new SitemapAnalysisResult
            {
                IsSuccess = sitemapMetadataList.Any(),
                SitemapFound = sitemapMetadataList.Any(),
                DiscoveredSitemaps = discoveredSitemaps,
                Metadata = mergedMetadata,
                UrlPatterns = urlPatterns,
                AnalyzedAt = DateTimeOffset.UtcNow,
                AnalysisTimeMs = analysisTime
            };

            // 통계 업데이트
            lock (_statsLock)
            {
                if (result.IsSuccess)
                {
                    _statistics.SuccessfulAnalyses++;
                }
                if (result.SitemapFound)
                {
                    _statistics.SitesWithSitemap++;
                }
                _statistics.AverageAnalysisTime = (_statistics.AverageAnalysisTime * (_statistics.TotalAnalysisAttempts - 1) + analysisTime) / _statistics.TotalAnalysisAttempts;
            }

            // 캐시 저장
            lock (_cacheLock)
            {
                _cache[baseUrl] = result;
            }

            _logger.LogInformation("Completed sitemap analysis for {BaseUrl}. Found {Count} sitemaps", baseUrl, discoveredSitemaps.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sitemap analysis failed for {BaseUrl}", baseUrl);

            lock (_statsLock)
            {
                var errorType = ex.GetType().Name;
                if (_statistics.CommonErrors.ContainsKey(errorType))
                    _statistics.CommonErrors[errorType]++;
                else
                    _statistics.CommonErrors[errorType] = 1;
            }

            return new SitemapAnalysisResult
            {
                IsSuccess = false,
                SitemapFound = false,
                ErrorMessage = ex.Message,
                AnalyzedAt = DateTimeOffset.UtcNow,
                AnalysisTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async Task<SitemapAnalysisResult> AnalyzeSitemapAsync(string sitemapUrl, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var metadata = await TryParseSitemapAsync(sitemapUrl, cancellationToken);
            var urlPatterns = metadata != null ? await AnalyzeUrlPatternsAsync(metadata) : null;

            return new SitemapAnalysisResult
            {
                IsSuccess = metadata != null,
                SitemapFound = metadata != null,
                DiscoveredSitemaps = metadata != null ? new List<string> { sitemapUrl } : new List<string>(),
                Metadata = metadata,
                UrlPatterns = urlPatterns,
                AnalyzedAt = DateTimeOffset.UtcNow,
                AnalysisTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze sitemap {SitemapUrl}", sitemapUrl);
            return new SitemapAnalysisResult
            {
                IsSuccess = false,
                SitemapFound = false,
                ErrorMessage = ex.Message,
                AnalyzedAt = DateTimeOffset.UtcNow,
                AnalysisTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async Task<SitemapMetadata> ParseContentAsync(string content, string baseUrl, SitemapFormat format)
    {
        var metadata = new SitemapMetadata
        {
            BaseUrl = baseUrl,
            Format = format,
            ParsedAt = DateTimeOffset.UtcNow
        };

        try
        {
            switch (format)
            {
                case SitemapFormat.Xml:
                case SitemapFormat.SitemapIndex:
                    await ParseXmlSitemapAsync(content, metadata);
                    break;

                case SitemapFormat.Text:
                    await ParseTextSitemapAsync(content, metadata);
                    break;

                case SitemapFormat.Rss:
                    await ParseRssSitemapAsync(content, metadata);
                    break;

                case SitemapFormat.Atom:
                    await ParseAtomSitemapAsync(content, metadata);
                    break;

                default:
                    throw new NotSupportedException($"Sitemap format {format} is not supported");
            }

            metadata.TotalUrls = metadata.UrlEntries.Count;

            lock (_statsLock)
            {
                if (_statistics.FormatStatistics.ContainsKey(format))
                    _statistics.FormatStatistics[format]++;
                else
                    _statistics.FormatStatistics[format] = 1;
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse sitemap content");
            throw;
        }
    }

    public async Task<SitemapMetadata> MergeSitemapsAsync(IEnumerable<SitemapMetadata> sitemaps)
    {
        var sitemapList = sitemaps.ToList();
        if (!sitemapList.Any())
            throw new ArgumentException("No sitemaps provided for merging");

        var first = sitemapList.First();
        var merged = new SitemapMetadata
        {
            BaseUrl = first.BaseUrl,
            Format = SitemapFormat.Xml, // 통합된 형식
            ParsedAt = DateTimeOffset.UtcNow
        };

        // URL 중복 제거를 위한 HashSet
        var urlSet = new HashSet<string>();

        foreach (var sitemap in sitemapList)
        {
            merged.SitemapUrls.AddRange(sitemap.SitemapUrls);

            // URL 엔트리 병합 (중복 제거)
            foreach (var entry in sitemap.UrlEntries)
            {
                if (urlSet.Add(entry.Url))
                {
                    merged.UrlEntries.Add(entry);
                }
            }

            // 이미지, 비디오, 뉴스 엔트리 병합
            merged.ImageEntries.AddRange(sitemap.ImageEntries);
            merged.VideoEntries.AddRange(sitemap.VideoEntries);
            merged.NewsEntries.AddRange(sitemap.NewsEntries);

            // 네임스페이스 병합
            foreach (var ns in sitemap.Namespaces)
            {
                merged.Namespaces.TryAdd(ns.Key, ns.Value);
            }

            // 최신 수정 시간 설정
            if (sitemap.LastModified > merged.LastModified)
            {
                merged.LastModified = sitemap.LastModified;
            }
        }

        merged.TotalUrls = merged.UrlEntries.Count;

        lock (_statsLock)
        {
            _statistics.AverageUrlsPerSitemap = (_statistics.AverageUrlsPerSitemap * (_statistics.SuccessfulAnalyses - 1) + merged.TotalUrls) / _statistics.SuccessfulAnalyses;
        }

        return merged;
    }

    public int CalculateCrawlPriority(SitemapMetadata metadata, string targetUrl)
    {
        var entry = metadata.UrlEntries.FirstOrDefault(e => e.Url.Equals(targetUrl, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
            return 5; // 기본 우선순위

        var priority = 5; // 기본값

        // 사이트맵 우선순위 사용
        if (entry.Priority.HasValue)
        {
            priority = (int)Math.Round(entry.Priority.Value * 10);
        }

        // 변경 빈도에 따른 조정
        if (entry.ChangeFrequency.HasValue)
        {
            priority += entry.ChangeFrequency switch
            {
                ChangeFrequency.Always => 3,
                ChangeFrequency.Hourly => 2,
                ChangeFrequency.Daily => 1,
                ChangeFrequency.Weekly => 0,
                ChangeFrequency.Monthly => -1,
                ChangeFrequency.Yearly => -2,
                ChangeFrequency.Never => -3,
                _ => 0
            };
        }

        // 최신성에 따른 조정
        if (entry.LastModified.HasValue)
        {
            var daysSinceLastMod = (DateTimeOffset.UtcNow - entry.LastModified.Value).Days;
            if (daysSinceLastMod <= 1) priority += 2;
            else if (daysSinceLastMod <= 7) priority += 1;
            else if (daysSinceLastMod > 365) priority -= 1;
        }

        // 이미지나 비디오가 있으면 우선순위 증가
        if (entry.Images.Any() || entry.Videos.Any())
        {
            priority += 1;
        }

        return Math.Max(1, Math.Min(10, priority));
    }

    public async Task<UrlPatternAnalysis> AnalyzeUrlPatternsAsync(SitemapMetadata metadata)
    {
        var analysis = new UrlPatternAnalysis();
        var urlPaths = metadata.UrlEntries.Select(e => new Uri(e.Url).AbsolutePath).ToList();

        // 패턴 감지
        var patterns = await DetectUrlPatternsAsync(urlPaths);
        analysis.DetectedPatterns = patterns;

        // 깊이 분석
        foreach (var path in urlPaths)
        {
            var depth = path.Count(c => c == '/') - 1; // 루트 슬래시 제외
            if (analysis.DepthDistribution.ContainsKey(depth))
                analysis.DepthDistribution[depth]++;
            else
                analysis.DepthDistribution[depth] = 1;
        }

        // 카테고리 분석 (첫 번째 세그먼트 기준)
        foreach (var path in urlPaths.Where(p => p.Length > 1))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                var category = segments[0];
                if (analysis.CategoryDistribution.ContainsKey(category))
                    analysis.CategoryDistribution[category]++;
                else
                    analysis.CategoryDistribution[category] = 1;
            }
        }

        // 아키텍처 타입 결정
        analysis.ArchitectureType = DetermineArchitectureType(analysis);

        // 일반적인 구조 식별
        analysis.CommonStructures = IdentifyCommonStructures(urlPaths);

        return analysis;
    }

    public IReadOnlyList<SitemapFormat> GetSupportedFormats()
    {
        return new List<SitemapFormat>
        {
            SitemapFormat.Xml,
            SitemapFormat.Text,
            SitemapFormat.SitemapIndex,
            SitemapFormat.Rss,
            SitemapFormat.Atom
        }.AsReadOnly();
    }

    public SitemapAnalysisStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            _statistics.LastUpdated = DateTimeOffset.UtcNow;
            return _statistics;
        }
    }

    private async Task<SitemapMetadata?> TryParseSitemapAsync(string sitemapUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(sitemapUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
                return null;

            // 형식 감지
            var format = DetectSitemapFormat(content, sitemapUrl);

            return await ParseContentAsync(content, sitemapUrl, format);
        }
        catch
        {
            return null;
        }
    }

    private SitemapFormat DetectSitemapFormat(string content, string url)
    {
        var trimmed = content.TrimStart();

        if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<"))
        {
            if (content.Contains("<sitemapindex"))
                return SitemapFormat.SitemapIndex;
            if (content.Contains("<rss") || content.Contains("<channel"))
                return SitemapFormat.Rss;
            if (content.Contains("<feed") || content.Contains("atom"))
                return SitemapFormat.Atom;
            return SitemapFormat.Xml;
        }

        if (url.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
            content.Split('\n').All(line => string.IsNullOrWhiteSpace(line) || Uri.IsWellFormedUriString(line.Trim(), UriKind.Absolute)))
        {
            return SitemapFormat.Text;
        }

        return SitemapFormat.Xml; // 기본값
    }

    private async Task ParseXmlSitemapAsync(string content, SitemapMetadata metadata)
    {
        var doc = new XmlDocument();
        doc.LoadXml(content);

        // 네임스페이스 추출
        ExtractNamespaces(doc, metadata);

        // 사이트맵 인덱스인지 확인
        var sitemapNodes = doc.SelectNodes("//*[local-name()='sitemap']");
        if (sitemapNodes?.Count > 0)
        {
            metadata.Format = SitemapFormat.SitemapIndex;
            foreach (XmlNode sitemapNode in sitemapNodes)
            {
                var locNode = sitemapNode.SelectSingleNode("*[local-name()='loc']");
                if (locNode != null)
                {
                    metadata.SitemapUrls.Add(locNode.InnerText);
                }
            }
            return;
        }

        // URL 엔트리 파싱
        var urlNodes = doc.SelectNodes("//*[local-name()='url']");
        if (urlNodes != null)
        {
            foreach (XmlNode urlNode in urlNodes)
            {
                var entry = ParseUrlEntry(urlNode);
                if (entry != null)
                {
                    metadata.UrlEntries.Add(entry);
                }
            }
        }
    }

    private async Task ParseTextSitemapAsync(string content, SitemapMetadata metadata)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var url = line.Trim();
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                metadata.UrlEntries.Add(new SitemapUrlEntry
                {
                    Url = url
                });
            }
        }
    }

    private async Task ParseRssSitemapAsync(string content, SitemapMetadata metadata)
    {
        var doc = new XmlDocument();
        doc.LoadXml(content);

        var itemNodes = doc.SelectNodes("//item");
        if (itemNodes != null)
        {
            foreach (XmlNode itemNode in itemNodes)
            {
                var linkNode = itemNode.SelectSingleNode("link");
                if (linkNode != null)
                {
                    var entry = new SitemapUrlEntry
                    {
                        Url = linkNode.InnerText
                    };

                    // 발행 날짜 추출
                    var pubDateNode = itemNode.SelectSingleNode("pubDate");
                    if (pubDateNode != null && DateTimeOffset.TryParse(pubDateNode.InnerText, out var pubDate))
                    {
                        entry.LastModified = pubDate;
                    }

                    metadata.UrlEntries.Add(entry);
                }
            }
        }
    }

    private async Task ParseAtomSitemapAsync(string content, SitemapMetadata metadata)
    {
        var doc = new XmlDocument();
        doc.LoadXml(content);

        var entryNodes = doc.SelectNodes("//*[local-name()='entry']");
        if (entryNodes != null)
        {
            foreach (XmlNode entryNode in entryNodes)
            {
                var linkNode = entryNode.SelectSingleNode("*[local-name()='link']");
                if (linkNode?.Attributes?["href"] != null)
                {
                    var entry = new SitemapUrlEntry
                    {
                        Url = linkNode.Attributes["href"].Value
                    };

                    // 업데이트 날짜 추출
                    var updatedNode = entryNode.SelectSingleNode("*[local-name()='updated']");
                    if (updatedNode != null && DateTimeOffset.TryParse(updatedNode.InnerText, out var updated))
                    {
                        entry.LastModified = updated;
                    }

                    metadata.UrlEntries.Add(entry);
                }
            }
        }
    }

    private void ExtractNamespaces(XmlDocument doc, SitemapMetadata metadata)
    {
        var rootElement = doc.DocumentElement;
        if (rootElement?.Attributes != null)
        {
            foreach (XmlAttribute attr in rootElement.Attributes)
            {
                if (attr.Name.StartsWith("xmlns"))
                {
                    metadata.Namespaces[attr.Name] = attr.Value;
                }
            }
        }
    }

    private SitemapUrlEntry? ParseUrlEntry(XmlNode urlNode)
    {
        var locNode = urlNode.SelectSingleNode("*[local-name()='loc']");
        if (locNode == null)
            return null;

        var entry = new SitemapUrlEntry
        {
            Url = locNode.InnerText
        };

        // 마지막 수정 날짜
        var lastModNode = urlNode.SelectSingleNode("*[local-name()='lastmod']");
        if (lastModNode != null && DateTimeOffset.TryParse(lastModNode.InnerText, out var lastMod))
        {
            entry.LastModified = lastMod;
        }

        // 변경 빈도
        var changeFreqNode = urlNode.SelectSingleNode("*[local-name()='changefreq']");
        if (changeFreqNode != null && Enum.TryParse<ChangeFrequency>(changeFreqNode.InnerText, true, out var changeFreq))
        {
            entry.ChangeFrequency = changeFreq;
        }

        // 우선순위
        var priorityNode = urlNode.SelectSingleNode("*[local-name()='priority']");
        if (priorityNode != null && double.TryParse(priorityNode.InnerText, out var priority))
        {
            entry.Priority = Math.Max(0.0, Math.Min(1.0, priority));
        }

        // 이미지 파싱
        var imageNodes = urlNode.SelectNodes("*[local-name()='image']");
        if (imageNodes != null)
        {
            foreach (XmlNode imageNode in imageNodes)
            {
                var imageEntry = ParseImageEntry(imageNode);
                if (imageEntry != null)
                {
                    entry.Images.Add(imageEntry);
                }
            }
        }

        // 비디오 파싱
        var videoNodes = urlNode.SelectNodes("*[local-name()='video']");
        if (videoNodes != null)
        {
            foreach (XmlNode videoNode in videoNodes)
            {
                var videoEntry = ParseVideoEntry(videoNode);
                if (videoEntry != null)
                {
                    entry.Videos.Add(videoEntry);
                }
            }
        }

        // 뉴스 파싱
        var newsNode = urlNode.SelectSingleNode("*[local-name()='news']");
        if (newsNode != null)
        {
            entry.News = ParseNewsEntry(newsNode);
        }

        return entry;
    }

    private SitemapImageEntry? ParseImageEntry(XmlNode imageNode)
    {
        var locNode = imageNode.SelectSingleNode("*[local-name()='loc']");
        if (locNode == null)
            return null;

        return new SitemapImageEntry
        {
            Url = locNode.InnerText,
            Title = imageNode.SelectSingleNode("*[local-name()='title']")?.InnerText,
            Caption = imageNode.SelectSingleNode("*[local-name()='caption']")?.InnerText,
            GeoLocation = imageNode.SelectSingleNode("*[local-name()='geo_location']")?.InnerText,
            LicenseUrl = imageNode.SelectSingleNode("*[local-name()='license']")?.InnerText
        };
    }

    private SitemapVideoEntry? ParseVideoEntry(XmlNode videoNode)
    {
        var thumbnailNode = videoNode.SelectSingleNode("*[local-name()='thumbnail_loc']");
        var titleNode = videoNode.SelectSingleNode("*[local-name()='title']");
        var descriptionNode = videoNode.SelectSingleNode("*[local-name()='description']");

        if (thumbnailNode == null || titleNode == null || descriptionNode == null)
            return null;

        var video = new SitemapVideoEntry
        {
            ThumbnailUrl = thumbnailNode.InnerText,
            Title = titleNode.InnerText,
            Description = descriptionNode.InnerText,
            ContentUrl = videoNode.SelectSingleNode("*[local-name()='content_loc']")?.InnerText,
            PlayerUrl = videoNode.SelectSingleNode("*[local-name()='player_loc']")?.InnerText
        };

        // 재생 시간
        var durationNode = videoNode.SelectSingleNode("*[local-name()='duration']");
        if (durationNode != null && int.TryParse(durationNode.InnerText, out var duration))
        {
            video.Duration = duration;
        }

        // 만료 날짜
        var expirationNode = videoNode.SelectSingleNode("*[local-name()='expiration_date']");
        if (expirationNode != null && DateTimeOffset.TryParse(expirationNode.InnerText, out var expiration))
        {
            video.ExpirationDate = expiration;
        }

        // 평점
        var ratingNode = videoNode.SelectSingleNode("*[local-name()='rating']");
        if (ratingNode != null && double.TryParse(ratingNode.InnerText, out var rating))
        {
            video.Rating = Math.Max(0.0, Math.Min(5.0, rating));
        }

        // 조회수
        var viewCountNode = videoNode.SelectSingleNode("*[local-name()='view_count']");
        if (viewCountNode != null && int.TryParse(viewCountNode.InnerText, out var viewCount))
        {
            video.ViewCount = viewCount;
        }

        // 게시 날짜
        var publicationNode = videoNode.SelectSingleNode("*[local-name()='publication_date']");
        if (publicationNode != null && DateTimeOffset.TryParse(publicationNode.InnerText, out var publication))
        {
            video.PublicationDate = publication;
        }

        // 가족 친화적 여부
        var familyFriendlyNode = videoNode.SelectSingleNode("*[local-name()='family_friendly']");
        if (familyFriendlyNode != null && bool.TryParse(familyFriendlyNode.InnerText, out var familyFriendly))
        {
            video.FamilyFriendly = familyFriendly;
        }

        // 태그
        var tagNodes = videoNode.SelectNodes("*[local-name()='tag']");
        if (tagNodes != null)
        {
            foreach (XmlNode tagNode in tagNodes)
            {
                video.Tags.Add(tagNode.InnerText);
            }
        }

        video.Category = videoNode.SelectSingleNode("*[local-name()='category']")?.InnerText;
        video.GalleryUrl = videoNode.SelectSingleNode("*[local-name()='gallery_loc']")?.InnerText;

        return video;
    }

    private SitemapNewsEntry? ParseNewsEntry(XmlNode newsNode)
    {
        var publicationNode = newsNode.SelectSingleNode("*[local-name()='publication']/*[local-name()='name']");
        var languageNode = newsNode.SelectSingleNode("*[local-name()='publication']/*[local-name()='language']");
        var publicationDateNode = newsNode.SelectSingleNode("*[local-name()='publication_date']");
        var titleNode = newsNode.SelectSingleNode("*[local-name()='title']");

        if (publicationNode == null || languageNode == null || publicationDateNode == null || titleNode == null)
            return null;

        if (!DateTimeOffset.TryParse(publicationDateNode.InnerText, out var pubDate))
            return null;

        var news = new SitemapNewsEntry
        {
            Publication = publicationNode.InnerText,
            Language = languageNode.InnerText,
            PublicationDate = pubDate,
            Title = titleNode.InnerText
        };

        // 키워드
        var keywordsNode = newsNode.SelectSingleNode("*[local-name()='keywords']");
        if (keywordsNode != null)
        {
            var keywords = keywordsNode.InnerText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim()).ToList();
            news.Keywords.AddRange(keywords);
        }

        // 스톡 티커
        var stockTickersNode = newsNode.SelectSingleNode("*[local-name()='stock_tickers']");
        if (stockTickersNode != null)
        {
            var tickers = stockTickersNode.InnerText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim()).ToList();
            news.StockTickers.AddRange(tickers);
        }

        return news;
    }

    private async Task<List<UrlPattern>> DetectUrlPatternsAsync(List<string> urlPaths)
    {
        var patterns = new List<UrlPattern>();

        // 날짜 패턴 감지 (/2024/01/01/, /2024-01-01/, 등)
        var datePattern = new Regex(@"/\d{4}[/-]\d{1,2}[/-]\d{1,2}/", RegexOptions.IgnoreCase);
        var dateMatches = urlPaths.Where(p => datePattern.IsMatch(p)).ToList();
        if (dateMatches.Any())
        {
            patterns.Add(new UrlPattern
            {
                Pattern = "/YYYY/MM/DD/ or /YYYY-MM-DD/",
                Type = PatternType.Date,
                MatchCount = dateMatches.Count,
                ExampleUrls = dateMatches.Take(3).ToList(),
                Importance = 8
            });
        }

        // ID 패턴 감지 (/post/123, /article/456, 등)
        var idPattern = new Regex(@"/\w+/\d+/?", RegexOptions.IgnoreCase);
        var idMatches = urlPaths.Where(p => idPattern.IsMatch(p) && !datePattern.IsMatch(p)).ToList();
        if (idMatches.Any())
        {
            patterns.Add(new UrlPattern
            {
                Pattern = "/category/id",
                Type = PatternType.Id,
                MatchCount = idMatches.Count,
                ExampleUrls = idMatches.Take(3).ToList(),
                Importance = 7
            });
        }

        // 슬러그 패턴 감지 (/blog/my-post-title, 등)
        var slugPattern = new Regex(@"/\w+/[\w-]+/?", RegexOptions.IgnoreCase);
        var slugMatches = urlPaths.Where(p => slugPattern.IsMatch(p) && !idPattern.IsMatch(p) && !datePattern.IsMatch(p)).ToList();
        if (slugMatches.Any())
        {
            patterns.Add(new UrlPattern
            {
                Pattern = "/category/slug",
                Type = PatternType.Slug,
                MatchCount = slugMatches.Count,
                ExampleUrls = slugMatches.Take(3).ToList(),
                Importance = 6
            });
        }

        // 언어 패턴 감지 (/en/, /ko/, /fr/, 등)
        var languagePattern = new Regex(@"/[a-z]{2}(-[a-z]{2})?/", RegexOptions.IgnoreCase);
        var languageMatches = urlPaths.Where(p => languagePattern.IsMatch(p)).ToList();
        if (languageMatches.Any())
        {
            patterns.Add(new UrlPattern
            {
                Pattern = "/lang/",
                Type = PatternType.Language,
                MatchCount = languageMatches.Count,
                ExampleUrls = languageMatches.Take(3).ToList(),
                Importance = 5
            });
        }

        return patterns.OrderByDescending(p => p.Importance).ThenByDescending(p => p.MatchCount).ToList();
    }

    private SiteArchitectureType DetermineArchitectureType(UrlPatternAnalysis analysis)
    {
        var maxDepth = analysis.DepthDistribution.Keys.DefaultIfEmpty(0).Max();
        var avgDepth = analysis.DepthDistribution.Any()
            ? analysis.DepthDistribution.Sum(kvp => kvp.Key * kvp.Value) / (double)analysis.DepthDistribution.Values.Sum()
            : 0;

        // 플랫 구조 (대부분 깊이 1-2)
        if (maxDepth <= 2 && avgDepth < 2)
            return SiteArchitectureType.Flat;

        // 날짜 기반 구조
        if (analysis.DetectedPatterns.Any(p => p.Type == PatternType.Date && p.MatchCount > analysis.DetectedPatterns.Sum(pp => pp.MatchCount) * 0.3))
            return SiteArchitectureType.DateBased;

        // 카테고리 기반 구조
        if (analysis.CategoryDistribution.Count >= 3 && avgDepth >= 2)
            return SiteArchitectureType.CategoryBased;

        // 계층적 구조
        if (maxDepth >= 4 && avgDepth >= 3)
            return SiteArchitectureType.Hierarchical;

        // 혼합 구조
        if (analysis.DetectedPatterns.Count >= 3)
            return SiteArchitectureType.Hybrid;

        return SiteArchitectureType.Unknown;
    }

    private List<string> IdentifyCommonStructures(List<string> urlPaths)
    {
        var structures = new Dictionary<string, int>();

        foreach (var path in urlPaths)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
            {
                // 첫 번째 세그먼트만 고려
                var structure = $"/{segments[0]}/";
                if (structures.ContainsKey(structure))
                    structures[structure]++;
                else
                    structures[structure] = 1;

                // 두 개 세그먼트 구조
                if (segments.Length > 1)
                {
                    var twoLevelStructure = $"/{segments[0]}/{segments[1]}/";
                    if (structures.ContainsKey(twoLevelStructure))
                        structures[twoLevelStructure]++;
                    else
                        structures[twoLevelStructure] = 1;
                }
            }
        }

        return structures
            .Where(kvp => kvp.Value >= Math.Max(2, urlPaths.Count * 0.1)) // 최소 2개 또는 전체의 10%
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}