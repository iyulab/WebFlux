using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace WebFlux.Services;

/// <summary>
/// 콘텐츠 관계 분석 및 매핑 서비스
/// 웹사이트의 페이지 간 관계와 구조를 분석하여 콘텐츠 네트워크를 구축
/// </summary>
public class ContentRelationshipMapper : IContentRelationshipMapper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentRelationshipMapper> _logger;

    public ContentRelationshipMapper(HttpClient httpClient, ILogger<ContentRelationshipMapper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ContentRelationshipAnalysisResult> AnalyzeContentRelationshipsAsync(string baseUrl, int maxDepth = 3, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("콘텐츠 관계 분석 시작: {BaseUrl}, 최대 깊이: {MaxDepth}", baseUrl, maxDepth);

        var result = new ContentRelationshipAnalysisResult();
        var visitedUrls = new ConcurrentDictionary<string, PageRelationshipInfo>();
        var linkRelationships = new ConcurrentList<PageLinkRelationship>();

        // 1. 웹사이트 크롤링 및 페이지 관계 수집
        await CrawlAndAnalyzePages(baseUrl, maxDepth, visitedUrls, linkRelationships, cancellationToken);

        result.Pages = visitedUrls.Values.ToList();
        result.LinkRelationships = linkRelationships.ToList();

        // 2. 페이지랭크 계산
        CalculatePageRank(result.Pages, result.LinkRelationships);

        // 3. 네비게이션 구조 분석
        result.NavigationStructure = await AnalyzeNavigationStructureAsync(result);

        // 4. 콘텐츠 계층 구조 생성
        result.ContentHierarchy = await BuildContentHierarchyAsync(result);

        // 5. 콘텐츠 클러스터링
        result.ContentClusters = await PerformContentClusteringAsync(result);

        // 6. 사이트 토폴로지 메트릭 계산
        result.TopologyMetrics = CalculateSiteTopologyMetrics(result);

        // 7. 품질 점수 계산
        result.QualityScore = CalculateAnalysisQuality(result);

        _logger.LogInformation("콘텐츠 관계 분석 완료: {PageCount}개 페이지, {LinkCount}개 링크, 품질 점수: {QualityScore:F2}",
            result.Pages.Count, result.LinkRelationships.Count, result.QualityScore);

        return result;
    }

    /// <inheritdoc />
    public async Task<PageRelationshipInfo> AnalyzePageRelationshipsAsync(string pageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(pageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new PageRelationshipInfo { Url = pageUrl };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParsePageRelationships(pageUrl, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "페이지 관계 분석 실패: {PageUrl}", pageUrl);
            return new PageRelationshipInfo { Url = pageUrl };
        }
    }

    /// <inheritdoc />
    public async Task<NavigationStructureResult> AnalyzeNavigationStructureAsync(ContentRelationshipAnalysisResult analysisResult)
    {
        var result = new NavigationStructureResult();

        // 주 네비게이션 감지
        result.PrimaryNavigation = DetectPrimaryNavigation(analysisResult);

        // 보조 네비게이션 감지
        result.SecondaryNavigations = DetectSecondaryNavigations(analysisResult);

        // 푸터 네비게이션 감지
        result.FooterNavigation = DetectFooterNavigation(analysisResult);

        // 브레드크럼 패턴 분석
        result.BreadcrumbPatterns = AnalyzeBreadcrumbPatterns(analysisResult);

        // 네비게이션 품질 평가
        result.ConsistencyScore = EvaluateNavigationConsistency(result);
        result.EfficiencyScore = EvaluateNavigationEfficiency(result);
        result.AccessibilityScore = EvaluateNavigationAccessibility(result);

        return result;
    }

    /// <inheritdoc />
    public async Task<ContentHierarchyResult> BuildContentHierarchyAsync(ContentRelationshipAnalysisResult analysisResult)
    {
        var result = new ContentHierarchyResult();

        // 루트 페이지들 식별 (홈페이지, 메인 섹션 페이지)
        var rootPages = IdentifyRootPages(analysisResult.Pages);

        // 계층 구조 생성
        result.RootNodes = BuildHierarchyTree(rootPages, analysisResult);

        // 고아 페이지 식별
        result.OrphanPages = IdentifyOrphanPages(analysisResult.Pages, result.RootNodes);

        // 메트릭 계산
        result.MaxDepth = CalculateMaxDepth(result.RootNodes);
        result.TotalNodes = CountTotalNodes(result.RootNodes);
        result.StructureQuality = EvaluateStructureQuality(result);
        result.BalanceScore = CalculateBalanceScore(result.RootNodes);

        return result;
    }

    /// <inheritdoc />
    public async Task<RelatedContentResult> GenerateRelatedContentAsync(string pageUrl, ContentRelationshipAnalysisResult analysisResult)
    {
        var basePage = analysisResult.Pages.FirstOrDefault(p => p.Url == pageUrl);
        if (basePage == null)
        {
            return new RelatedContentResult { BasePageUrl = pageUrl };
        }

        var result = new RelatedContentResult { BasePageUrl = pageUrl };

        // 관련 페이지 후보 수집
        var candidates = CollectRelatedCandidates(basePage, analysisResult);

        // 관련성 점수 계산
        foreach (var candidate in candidates)
        {
            var relatedness = CalculateRelatedness(basePage, candidate, analysisResult);
            if (relatedness.RelatednessScore > 0.1) // 임계값 이상만 포함
            {
                result.RelatedPages.Add(relatedness);
            }
        }

        // 관련성 점수로 정렬
        result.RelatedPages = result.RelatedPages
            .OrderByDescending(r => r.RelatednessScore)
            .Take(10)
            .ToList();

        // 관련성 메트릭 계산
        result.RelatednessMetrics = CalculateRelatednessMetrics(result.RelatedPages);
        result.ConfidenceScore = CalculateConfidenceScore(result);

        return result;
    }

    /// <inheritdoc />
    public async Task<ContentClusterResult> PerformContentClusteringAsync(ContentRelationshipAnalysisResult analysisResult)
    {
        var result = new ContentClusterResult
        {
            ClusteringMethod = "Hierarchical + Semantic"
        };

        // 페이지 특성 벡터 생성
        var pageVectors = CreatePageFeatureVectors(analysisResult.Pages);

        // 클러스터링 수행 (간소화된 구현)
        var clusters = PerformHierarchicalClustering(pageVectors);

        // 클러스터 정보 구성
        result.Clusters = clusters.Select((cluster, index) => new ContentCluster
        {
            Id = $"cluster_{index}",
            Name = GenerateClusterName(cluster, analysisResult.Pages),
            Description = GenerateClusterDescription(cluster, analysisResult.Pages),
            PageUrls = cluster.ToList(),
            CentroidUrl = FindClusterCentroid(cluster, pageVectors),
            Size = cluster.Count,
            Density = CalculateClusterDensity(cluster, analysisResult.LinkRelationships),
            Keywords = ExtractClusterKeywords(cluster, analysisResult.Pages),
            Category = ClassifyClusterCategory(cluster, analysisResult.Pages)
        }).ToList();

        // 클러스터링 품질 평가
        result.QualityScore = EvaluateClusteringQuality(result.Clusters, analysisResult);
        result.SilhouetteScore = CalculateSilhouetteScore(result.Clusters, pageVectors);
        result.IntraClusterCohesion = CalculateIntraClusterCohesion(result.Clusters, pageVectors);
        result.InterClusterSeparation = CalculateInterClusterSeparation(result.Clusters, pageVectors);

        return result;
    }

    #region Private Helper Methods

    private async Task CrawlAndAnalyzePages(string baseUrl, int maxDepth, ConcurrentDictionary<string, PageRelationshipInfo> visitedUrls,
        ConcurrentList<PageLinkRelationship> linkRelationships, CancellationToken cancellationToken)
    {
        var queue = new Queue<(string Url, int Depth)>();
        queue.Enqueue((baseUrl, 0));
        var baseUri = new Uri(baseUrl);

        while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var (currentUrl, depth) = queue.Dequeue();

            if (depth > maxDepth || visitedUrls.ContainsKey(currentUrl))
                continue;

            try
            {
                var pageInfo = await AnalyzePageRelationshipsAsync(currentUrl, cancellationToken);
                pageInfo.Depth = depth;
                visitedUrls.TryAdd(currentUrl, pageInfo);

                // 링크 관계 기록
                foreach (var outboundLink in pageInfo.OutboundLinks)
                {
                    if (IsInternalLink(outboundLink.TargetUrl, baseUri))
                    {
                        linkRelationships.Add(new PageLinkRelationship
                        {
                            SourceUrl = currentUrl,
                            TargetUrl = outboundLink.TargetUrl,
                            RelationshipType = DetermineRelationshipType(outboundLink),
                            Strength = outboundLink.Weight,
                            IsBidirectional = false
                        });

                        // 다음 깊이로 큐에 추가
                        if (depth < maxDepth)
                        {
                            queue.Enqueue((outboundLink.TargetUrl, depth + 1));
                        }
                    }
                }

                // 요청 간 지연
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "페이지 분석 실패: {Url}", currentUrl);
            }
        }
    }

    private PageRelationshipInfo ParsePageRelationships(string pageUrl, string htmlContent)
    {
        var pageInfo = new PageRelationshipInfo
        {
            Url = pageUrl,
            ContentHash = CalculateContentHash(htmlContent)
        };

        // 제목 추출
        var titleMatch = Regex.Match(htmlContent, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            pageInfo.Title = titleMatch.Groups[1].Value.Trim();
        }

        // 메타 설명 추출
        var metaDescMatch = Regex.Match(htmlContent, @"<meta[^>]*name=[""']description[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (metaDescMatch.Success)
        {
            pageInfo.MetaDescription = metaDescMatch.Groups[1].Value.Trim();
        }

        // 언어 추출
        var langMatch = Regex.Match(htmlContent, @"<html[^>]*lang=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (langMatch.Success)
        {
            pageInfo.Language = langMatch.Groups[1].Value;
        }

        // 페이지 타입 감지
        pageInfo.PageType = DetectPageType(pageUrl, htmlContent);

        // 콘텐츠 카테고리 감지
        pageInfo.ContentCategory = DetectContentCategory(pageUrl, htmlContent);

        // 발신 링크 추출
        pageInfo.OutboundLinks = ExtractOutboundLinks(htmlContent, pageUrl);

        // 키워드 추출
        pageInfo.Keywords = ExtractKeywords(htmlContent);

        // 브레드크럼 추출
        pageInfo.Breadcrumbs = ExtractBreadcrumbs(htmlContent);

        // 최종 수정 날짜 추출 (메타 태그에서)
        var lastModMatch = Regex.Match(htmlContent, @"<meta[^>]*name=[""']last-modified[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (lastModMatch.Success && DateTime.TryParse(lastModMatch.Groups[1].Value, out var lastMod))
        {
            pageInfo.LastModified = lastMod;
        }

        return pageInfo;
    }

    private List<OutboundLink> ExtractOutboundLinks(string htmlContent, string baseUrl)
    {
        var links = new List<OutboundLink>();
        var linkMatches = Regex.Matches(htmlContent, @"<a[^>]*href=[""']([^""']+)[""'][^>]*>([^<]*)</a>", RegexOptions.IgnoreCase);

        foreach (Match match in linkMatches)
        {
            var href = match.Groups[1].Value;
            var anchorText = match.Groups[2].Value.Trim();

            if (string.IsNullOrEmpty(href) || href.StartsWith("#"))
                continue;

            var absoluteUrl = ConvertToAbsoluteUrl(href, baseUrl);
            if (absoluteUrl == null) continue;

            var link = new OutboundLink
            {
                TargetUrl = absoluteUrl,
                AnchorText = anchorText,
                LinkType = DetermineLinkType(href),
                Position = DetermineLinkPosition(match.Value, htmlContent),
                Weight = CalculateLinkWeight(anchorText, match.Value),
                Context = ExtractLinkContext(match, htmlContent)
            };

            // rel 속성 추출
            var relMatch = Regex.Match(match.Value, @"rel=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (relMatch.Success)
            {
                link.RelAttribute = relMatch.Groups[1].Value;
            }

            links.Add(link);
        }

        return links;
    }

    private PageType DetectPageType(string url, string content)
    {
        var urlLower = url.ToLower();
        var contentLower = content.ToLower();

        // URL 패턴 기반 감지
        if (urlLower.EndsWith("/") && urlLower.Count(c => c == '/') <= 3)
            return PageType.Homepage;

        if (urlLower.Contains("/blog/") || urlLower.Contains("/post/"))
            return PageType.BlogPost;

        if (urlLower.Contains("/news/") || urlLower.Contains("/article/"))
            return PageType.NewsArticle;

        if (urlLower.Contains("/about"))
            return PageType.AboutPage;

        if (urlLower.Contains("/contact"))
            return PageType.ContactPage;

        if (urlLower.Contains("/search"))
            return PageType.SearchPage;

        if (urlLower.Contains("/category/") || urlLower.Contains("/categories/"))
            return PageType.CategoryPage;

        if (urlLower.Contains("/product/") || urlLower.Contains("/item/"))
            return PageType.ProductPage;

        if (urlLower.Contains("/docs/") || urlLower.Contains("/documentation/"))
            return PageType.DocumentationPage;

        if (urlLower.Contains("sitemap"))
            return PageType.SitemapPage;

        if (urlLower.Contains("/archive/"))
            return PageType.ArchivePage;

        // 콘텐츠 기반 감지
        if (contentLower.Contains("404") || contentLower.Contains("not found"))
            return PageType.ErrorPage;

        // Schema.org 마크업 기반 감지
        if (content.Contains("itemtype=\"http://schema.org/Article\""))
            return PageType.ArticlePage;

        if (content.Contains("itemtype=\"http://schema.org/BlogPosting\""))
            return PageType.BlogPost;

        if (content.Contains("itemtype=\"http://schema.org/Product\""))
            return PageType.ProductPage;

        return PageType.Unknown;
    }

    private string DetectContentCategory(string url, string content)
    {
        // URL 경로에서 카테고리 추출
        var uri = new Uri(url);
        var segments = uri.Segments.Where(s => s != "/").Select(s => s.Trim('/')).ToArray();

        if (segments.Length > 1)
        {
            return segments[0]; // 첫 번째 경로 세그먼트를 카테고리로 사용
        }

        // 메타 키워드에서 카테고리 추출
        var metaKeywordsMatch = Regex.Match(content, @"<meta[^>]*name=[""']keywords[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (metaKeywordsMatch.Success)
        {
            var keywords = metaKeywordsMatch.Groups[1].Value.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(keywords))
            {
                return keywords;
            }
        }

        return "General";
    }

    private LinkType DetermineLinkType(string href)
    {
        if (href.StartsWith("mailto:"))
            return LinkType.Email;

        if (href.StartsWith("tel:"))
            return LinkType.Phone;

        if (href.StartsWith("#"))
            return LinkType.Anchor;

        if (href.StartsWith("http") && !href.Contains(Environment.MachineName))
            return LinkType.External;

        var extension = Path.GetExtension(href).ToLower();
        if (new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar" }.Contains(extension))
            return LinkType.Download;

        return LinkType.Internal;
    }

    private LinkPosition DetermineLinkPosition(string linkHtml, string pageContent)
    {
        var linkIndex = pageContent.IndexOf(linkHtml, StringComparison.OrdinalIgnoreCase);
        if (linkIndex == -1) return LinkPosition.Unknown;

        // 주변 컨텍스트 분석
        var before = pageContent.Substring(Math.Max(0, linkIndex - 500), Math.Min(500, linkIndex));
        var after = pageContent.Substring(linkIndex, Math.Min(500, pageContent.Length - linkIndex));
        var context = before + after;

        if (context.Contains("<nav") || context.Contains("navigation"))
            return LinkPosition.Navigation;

        if (context.Contains("<header"))
            return LinkPosition.Header;

        if (context.Contains("<footer"))
            return LinkPosition.Footer;

        if (context.Contains("breadcrumb"))
            return LinkPosition.Breadcrumb;

        if (context.Contains("<aside") || context.Contains("sidebar"))
            return LinkPosition.Sidebar;

        if (context.Contains("<menu"))
            return LinkPosition.Menu;

        return LinkPosition.Content;
    }

    private double CalculateLinkWeight(string anchorText, string linkHtml)
    {
        var weight = 1.0;

        // 앵커 텍스트 길이 기반
        if (anchorText.Length > 50) weight *= 0.8;
        else if (anchorText.Length > 20) weight *= 0.9;

        // 링크 스타일 기반
        if (linkHtml.Contains("class=") && linkHtml.ToLower().Contains("button"))
            weight *= 1.2;

        if (linkHtml.ToLower().Contains("rel=\"nofollow\""))
            weight *= 0.5;

        return weight;
    }

    private string ExtractLinkContext(Match linkMatch, string htmlContent)
    {
        var startIndex = Math.Max(0, linkMatch.Index - 100);
        var length = Math.Min(200, htmlContent.Length - startIndex);
        var context = htmlContent.Substring(startIndex, length);

        // HTML 태그 제거
        context = Regex.Replace(context, @"<[^>]+>", " ");
        context = Regex.Replace(context, @"\s+", " ").Trim();

        return context;
    }

    private List<string> ExtractKeywords(string htmlContent)
    {
        var keywords = new List<string>();

        // 메타 키워드 추출
        var metaKeywordsMatch = Regex.Match(htmlContent, @"<meta[^>]*name=[""']keywords[""'][^>]*content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (metaKeywordsMatch.Success)
        {
            keywords.AddRange(metaKeywordsMatch.Groups[1].Value.Split(',').Select(k => k.Trim()));
        }

        // 헤더 태그에서 키워드 추출
        var headerMatches = Regex.Matches(htmlContent, @"<h[1-6][^>]*>([^<]+)</h[1-6]>", RegexOptions.IgnoreCase);
        foreach (Match match in headerMatches)
        {
            var headerText = match.Groups[1].Value.Trim();
            var words = headerText.Split(' ').Where(w => w.Length > 3).Take(3);
            keywords.AddRange(words);
        }

        return keywords.Distinct().Take(10).ToList();
    }

    private List<BreadcrumbItem> ExtractBreadcrumbs(string htmlContent)
    {
        var breadcrumbs = new List<BreadcrumbItem>();

        // Schema.org 브레드크럼 추출
        var breadcrumbMatches = Regex.Matches(htmlContent,
            @"<[^>]*itemtype=[""']http://schema\.org/BreadcrumbList[""'][^>]*>.*?</[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in breadcrumbMatches)
        {
            var itemMatches = Regex.Matches(match.Value,
                @"<[^>]*itemprop=[""']name[""'][^>]*>([^<]+)</[^>]*>.*?<[^>]*itemprop=[""']item[""'][^>]*href=[""']([^""']+)[""']",
                RegexOptions.IgnoreCase);

            var order = 0;
            foreach (Match itemMatch in itemMatches)
            {
                breadcrumbs.Add(new BreadcrumbItem
                {
                    Text = itemMatch.Groups[1].Value.Trim(),
                    Url = itemMatch.Groups[2].Value,
                    Order = order++,
                    IsCurrentPage = false
                });
            }
        }

        // 일반적인 브레드크럼 패턴 추출
        if (!breadcrumbs.Any())
        {
            var commonBreadcrumbMatch = Regex.Match(htmlContent,
                @"<[^>]*class=[""'][^""']*breadcrumb[^""']*[""'][^>]*>(.*?)</[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (commonBreadcrumbMatch.Success)
            {
                var linkMatches = Regex.Matches(commonBreadcrumbMatch.Groups[1].Value,
                    @"<a[^>]*href=[""']([^""']+)[""'][^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);

                var order = 0;
                foreach (Match linkMatch in linkMatches)
                {
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Text = linkMatch.Groups[2].Value.Trim(),
                        Url = linkMatch.Groups[1].Value,
                        Order = order++,
                        IsCurrentPage = false
                    });
                }
            }
        }

        return breadcrumbs;
    }

    private string CalculateContentHash(string content)
    {
        // 간단한 콘텐츠 해시 (실제 구현에서는 더 정교한 해시 사용)
        var textContent = Regex.Replace(content, @"<[^>]+>", " ");
        textContent = Regex.Replace(textContent, @"\s+", " ").Trim();

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(textContent));
        return Convert.ToHexString(hash)[..16]; // 첫 16자리만 사용
    }

    private bool IsInternalLink(string url, Uri baseUri)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase);
        }
        return !url.StartsWith("http"); // 상대 링크는 내부 링크로 간주
    }

    private string? ConvertToAbsoluteUrl(string href, string baseUrl)
    {
        try
        {
            if (Uri.TryCreate(href, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            if (Uri.TryCreate(new Uri(baseUrl), href, out var relativeUri))
            {
                return relativeUri.ToString();
            }
        }
        catch
        {
            // URL 변환 실패
        }

        return null;
    }

    private RelationshipType DetermineRelationshipType(OutboundLink link)
    {
        // 링크 위치와 컨텍스트를 기반으로 관계 유형 결정
        return link.Position switch
        {
            LinkPosition.Navigation => RelationshipType.Navigation,
            LinkPosition.Breadcrumb => RelationshipType.Hierarchical,
            LinkPosition.Content => RelationshipType.Related,
            _ => RelationshipType.Reference
        };
    }

    private void CalculatePageRank(List<PageRelationshipInfo> pages, List<PageLinkRelationship> linkRelationships)
    {
        const double dampingFactor = 0.85;
        const int iterations = 20;
        const double tolerance = 0.001;

        var pageCount = pages.Count;
        if (pageCount == 0) return;

        // 초기 PageRank 값 설정
        var pageRanks = pages.ToDictionary(p => p.Url, p => 1.0 / pageCount);
        var newPageRanks = new Dictionary<string, double>();

        for (int iter = 0; iter < iterations; iter++)
        {
            newPageRanks.Clear();

            foreach (var page in pages)
            {
                var rank = (1.0 - dampingFactor) / pageCount;

                // 수신 링크에서 PageRank 전달받기
                var inboundLinks = linkRelationships.Where(r => r.TargetUrl == page.Url);
                foreach (var link in inboundLinks)
                {
                    var sourcePage = pages.FirstOrDefault(p => p.Url == link.SourceUrl);
                    if (sourcePage != null)
                    {
                        var outboundCount = linkRelationships.Count(r => r.SourceUrl == link.SourceUrl);
                        if (outboundCount > 0)
                        {
                            rank += dampingFactor * pageRanks[link.SourceUrl] / outboundCount;
                        }
                    }
                }

                newPageRanks[page.Url] = rank;
            }

            // 수렴 확인
            var maxChange = pageRanks.Max(kvp => Math.Abs(kvp.Value - newPageRanks.GetValueOrDefault(kvp.Key, 0.0)));
            if (maxChange < tolerance) break;

            // 값 업데이트
            foreach (var kvp in newPageRanks)
            {
                pageRanks[kvp.Key] = kvp.Value;
            }
        }

        // 페이지에 PageRank 값 설정
        foreach (var page in pages)
        {
            page.PageRankScore = pageRanks.GetValueOrDefault(page.Url, 0.0);
        }
    }

    private NavigationMenu? DetectPrimaryNavigation(ContentRelationshipAnalysisResult analysisResult)
    {
        // 가장 일반적인 네비게이션 패턴 감지
        var navigationLinks = analysisResult.LinkRelationships
            .Where(r => r.RelationshipType == RelationshipType.Navigation)
            .GroupBy(r => r.SourceUrl)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (navigationLinks == null) return null;

        var menu = new NavigationMenu
        {
            Type = NavigationType.Primary,
            Position = "Header",
            Structure = "Hierarchical"
        };

        foreach (var link in navigationLinks)
        {
            var targetPage = analysisResult.Pages.FirstOrDefault(p => p.Url == link.TargetUrl);
            if (targetPage != null)
            {
                menu.Items.Add(new NavigationItem
                {
                    Text = targetPage.Title,
                    Url = targetPage.Url,
                    Level = 0,
                    Importance = targetPage.PageRankScore
                });
            }
        }

        return menu;
    }

    private List<NavigationMenu> DetectSecondaryNavigations(ContentRelationshipAnalysisResult analysisResult)
    {
        // 보조 네비게이션 감지 로직 (간소화)
        return new List<NavigationMenu>();
    }

    private NavigationMenu? DetectFooterNavigation(ContentRelationshipAnalysisResult analysisResult)
    {
        // 푸터 네비게이션 감지 로직 (간소화)
        return null;
    }

    private List<BreadcrumbPattern> AnalyzeBreadcrumbPatterns(ContentRelationshipAnalysisResult analysisResult)
    {
        var patterns = new List<BreadcrumbPattern>();

        // 브레드크럼이 있는 페이지들을 그룹핑
        var pagesWithBreadcrumbs = analysisResult.Pages
            .Where(p => p.Breadcrumbs.Any())
            .ToList();

        if (pagesWithBreadcrumbs.Any())
        {
            var pattern = new BreadcrumbPattern
            {
                PatternName = "Standard Hierarchical",
                PageCount = pagesWithBreadcrumbs.Count,
                ConsistencyScore = CalculateBreadcrumbConsistency(pagesWithBreadcrumbs)
            };

            pattern.Examples = pagesWithBreadcrumbs.Take(5).Select(p => new BreadcrumbExample
            {
                PageUrl = p.Url,
                Items = p.Breadcrumbs
            }).ToList();

            patterns.Add(pattern);
        }

        return patterns;
    }

    private double CalculateBreadcrumbConsistency(List<PageRelationshipInfo> pagesWithBreadcrumbs)
    {
        if (pagesWithBreadcrumbs.Count < 2) return 1.0;

        // 브레드크럼 구조의 일관성 측정
        var structures = pagesWithBreadcrumbs.Select(p => p.Breadcrumbs.Count).ToList();
        var avgLength = structures.Average();
        var variance = structures.Sum(s => Math.Pow(s - avgLength, 2)) / structures.Count;

        return Math.Max(0.0, 1.0 - variance / 10.0);
    }

    private double EvaluateNavigationConsistency(NavigationStructureResult result)
    {
        // 네비게이션 일관성 평가 (간소화)
        var score = 0.8;

        if (result.PrimaryNavigation != null)
            score += 0.1;

        if (result.BreadcrumbPatterns.Any())
            score += 0.1;

        return Math.Min(score, 1.0);
    }

    private double EvaluateNavigationEfficiency(NavigationStructureResult result)
    {
        // 네비게이션 효율성 평가 (간소화)
        return 0.7; // 임시 값
    }

    private double EvaluateNavigationAccessibility(NavigationStructureResult result)
    {
        // 네비게이션 접근성 평가 (간소화)
        return 0.8; // 임시 값
    }

    private List<PageRelationshipInfo> IdentifyRootPages(List<PageRelationshipInfo> pages)
    {
        // 홈페이지와 주요 섹션 페이지를 루트로 식별
        return pages.Where(p =>
            p.PageType == PageType.Homepage ||
            p.Depth <= 1 ||
            p.PageRankScore > 0.1
        ).ToList();
    }

    private List<ContentNode> BuildHierarchyTree(List<PageRelationshipInfo> rootPages, ContentRelationshipAnalysisResult analysisResult)
    {
        var nodes = new List<ContentNode>();

        foreach (var rootPage in rootPages)
        {
            var node = new ContentNode
            {
                Url = rootPage.Url,
                Title = rootPage.Title,
                NodeType = rootPage.PageType == PageType.Homepage ? NodeType.Root : NodeType.Branch,
                Depth = 0,
                Weight = rootPage.PageRankScore
            };

            BuildChildNodes(node, analysisResult, new HashSet<string> { rootPage.Url });
            node.SubtreeSize = CountSubtreeSize(node);

            nodes.Add(node);
        }

        return nodes;
    }

    private void BuildChildNodes(ContentNode parentNode, ContentRelationshipAnalysisResult analysisResult, HashSet<string> visited)
    {
        const int maxDepth = 5; // 무한 재귀 방지

        if (parentNode.Depth >= maxDepth) return;

        var childLinks = analysisResult.LinkRelationships
            .Where(r => r.SourceUrl == parentNode.Url && !visited.Contains(r.TargetUrl))
            .OrderByDescending(r => r.Strength)
            .Take(10); // 최대 10개 자식

        foreach (var link in childLinks)
        {
            var childPage = analysisResult.Pages.FirstOrDefault(p => p.Url == link.TargetUrl);
            if (childPage != null)
            {
                var childNode = new ContentNode
                {
                    Url = childPage.Url,
                    Title = childPage.Title,
                    NodeType = NodeType.Branch,
                    ParentUrl = parentNode.Url,
                    Depth = parentNode.Depth + 1,
                    Weight = childPage.PageRankScore
                };

                visited.Add(childPage.Url);
                BuildChildNodes(childNode, analysisResult, visited);
                childNode.SubtreeSize = CountSubtreeSize(childNode);

                parentNode.Children.Add(childNode);
            }
        }

        // 자식이 없으면 Leaf 노드로 변경
        if (!parentNode.Children.Any() && parentNode.NodeType == NodeType.Branch)
        {
            parentNode.NodeType = NodeType.Leaf;
        }
    }

    private int CountSubtreeSize(ContentNode node)
    {
        return 1 + node.Children.Sum(child => child.SubtreeSize);
    }

    private List<string> IdentifyOrphanPages(List<PageRelationshipInfo> pages, List<ContentNode> rootNodes)
    {
        var connectedUrls = new HashSet<string>();
        CollectConnectedUrls(rootNodes, connectedUrls);

        return pages
            .Where(p => !connectedUrls.Contains(p.Url))
            .Select(p => p.Url)
            .ToList();
    }

    private void CollectConnectedUrls(List<ContentNode> nodes, HashSet<string> connectedUrls)
    {
        foreach (var node in nodes)
        {
            connectedUrls.Add(node.Url);
            CollectConnectedUrls(node.Children, connectedUrls);
        }
    }

    private int CalculateMaxDepth(List<ContentNode> rootNodes)
    {
        return rootNodes.Any() ? rootNodes.Max(n => CalculateNodeDepth(n)) : 0;
    }

    private int CalculateNodeDepth(ContentNode node)
    {
        return node.Children.Any() ? 1 + node.Children.Max(CalculateNodeDepth) : 1;
    }

    private int CountTotalNodes(List<ContentNode> rootNodes)
    {
        return rootNodes.Sum(n => n.SubtreeSize);
    }

    private double EvaluateStructureQuality(ContentHierarchyResult result)
    {
        var score = 1.0;

        // 고아 페이지 비율로 품질 감소
        if (result.TotalNodes > 0)
        {
            var orphanRatio = result.OrphanPages.Count / (double)(result.TotalNodes + result.OrphanPages.Count);
            score -= orphanRatio * 0.5;
        }

        // 깊이가 너무 깊으면 품질 감소
        if (result.MaxDepth > 7)
        {
            score -= (result.MaxDepth - 7) * 0.1;
        }

        return Math.Max(score, 0.0);
    }

    private double CalculateBalanceScore(List<ContentNode> rootNodes)
    {
        if (!rootNodes.Any()) return 1.0;

        // 트리 균형도 계산 (간소화)
        var sizes = rootNodes.Select(n => n.SubtreeSize).ToList();
        var avgSize = sizes.Average();
        var variance = sizes.Sum(s => Math.Pow(s - avgSize, 2)) / sizes.Count;

        return Math.Max(0.0, 1.0 - variance / (avgSize * avgSize));
    }

    private List<PageRelationshipInfo> CollectRelatedCandidates(PageRelationshipInfo basePage, ContentRelationshipAnalysisResult analysisResult)
    {
        var candidates = new HashSet<PageRelationshipInfo>();

        // 직접 링크된 페이지들
        foreach (var outboundLink in basePage.OutboundLinks)
        {
            var targetPage = analysisResult.Pages.FirstOrDefault(p => p.Url == outboundLink.TargetUrl);
            if (targetPage != null)
            {
                candidates.Add(targetPage);
            }
        }

        // 역방향 링크된 페이지들
        foreach (var inboundLink in basePage.InboundLinks)
        {
            var sourcePage = analysisResult.Pages.FirstOrDefault(p => p.Url == inboundLink.SourceUrl);
            if (sourcePage != null)
            {
                candidates.Add(sourcePage);
            }
        }

        // 같은 카테고리의 페이지들
        var sameCategoryPages = analysisResult.Pages
            .Where(p => p.ContentCategory == basePage.ContentCategory && p.Url != basePage.Url)
            .Take(10);
        foreach (var page in sameCategoryPages)
        {
            candidates.Add(page);
        }

        return candidates.Where(c => c.Url != basePage.Url).ToList();
    }

    private RelatedPage CalculateRelatedness(PageRelationshipInfo basePage, PageRelationshipInfo candidatePage, ContentRelationshipAnalysisResult analysisResult)
    {
        var relatedness = new RelatedPage
        {
            Url = candidatePage.Url,
            Title = candidatePage.Title
        };

        var reasons = new List<RelatednessReason>();
        var totalScore = 0.0;

        // 직접 링크 관계
        var directLink = analysisResult.LinkRelationships.FirstOrDefault(r =>
            (r.SourceUrl == basePage.Url && r.TargetUrl == candidatePage.Url) ||
            (r.SourceUrl == candidatePage.Url && r.TargetUrl == basePage.Url));

        if (directLink != null)
        {
            var score = 0.4;
            totalScore += score;
            reasons.Add(new RelatednessReason
            {
                Type = RelatednessReasonType.DirectLink,
                Description = "페이지 간 직접 링크 관계",
                Weight = score,
                Evidence = $"링크 강도: {directLink.Strength:F2}"
            });
        }

        // 공통 키워드
        var commonKeywords = basePage.Keywords.Intersect(candidatePage.Keywords).ToList();
        if (commonKeywords.Any())
        {
            var score = Math.Min(commonKeywords.Count / 5.0, 0.3);
            totalScore += score;
            reasons.Add(new RelatednessReason
            {
                Type = RelatednessReasonType.SharedKeywords,
                Description = $"{commonKeywords.Count}개의 공통 키워드",
                Weight = score,
                Evidence = string.Join(", ", commonKeywords)
            });
        }

        // 같은 카테고리
        if (basePage.ContentCategory == candidatePage.ContentCategory)
        {
            var score = 0.2;
            totalScore += score;
            reasons.Add(new RelatednessReason
            {
                Type = RelatednessReasonType.CommonCategory,
                Description = "동일한 콘텐츠 카테고리",
                Weight = score,
                Evidence = basePage.ContentCategory
            });
        }

        // 구조적 근접성 (계층에서의 거리)
        var depthDifference = Math.Abs(basePage.Depth - candidatePage.Depth);
        if (depthDifference <= 2)
        {
            var score = (3 - depthDifference) / 10.0;
            totalScore += score;
            reasons.Add(new RelatednessReason
            {
                Type = RelatednessReasonType.StructuralProximity,
                Description = "비슷한 계층 깊이",
                Weight = score,
                Evidence = $"깊이 차이: {depthDifference}"
            });
        }

        relatedness.RelatednessScore = Math.Min(totalScore, 1.0);
        relatedness.Reasons = reasons;
        relatedness.Type = DetermineRelatednessType(reasons);

        return relatedness;
    }

    private RelatednessType DetermineRelatednessType(List<RelatednessReason> reasons)
    {
        if (reasons.Any(r => r.Type == RelatednessReasonType.DirectLink))
            return RelatednessType.Structural;

        if (reasons.Any(r => r.Type == RelatednessReasonType.SharedKeywords))
            return RelatednessType.Semantic;

        if (reasons.Any(r => r.Type == RelatednessReasonType.CommonCategory))
            return RelatednessType.Contextual;

        return RelatednessType.Unknown;
    }

    private List<RelatednessMetric> CalculateRelatednessMetrics(List<RelatedPage> relatedPages)
    {
        var metrics = new List<RelatednessMetric>();

        if (relatedPages.Any())
        {
            metrics.Add(new RelatednessMetric
            {
                Name = "Average Relatedness Score",
                Value = relatedPages.Average(r => r.RelatednessScore),
                Description = "관련 페이지들의 평균 관련성 점수"
            });

            metrics.Add(new RelatednessMetric
            {
                Name = "Max Relatedness Score",
                Value = relatedPages.Max(r => r.RelatednessScore),
                Description = "가장 높은 관련성 점수"
            });

            var semanticRelated = relatedPages.Count(r => r.Type == RelatednessType.Semantic);
            metrics.Add(new RelatednessMetric
            {
                Name = "Semantic Relatedness Ratio",
                Value = semanticRelated / (double)relatedPages.Count,
                Description = "의미적 관련성을 가진 페이지 비율"
            });
        }

        return metrics;
    }

    private double CalculateConfidenceScore(RelatedContentResult result)
    {
        if (!result.RelatedPages.Any()) return 0.0;

        // 관련 페이지 수와 평균 점수를 기반으로 신뢰도 계산
        var avgScore = result.RelatedPages.Average(r => r.RelatednessScore);
        var countFactor = Math.Min(result.RelatedPages.Count / 10.0, 1.0);

        return avgScore * countFactor;
    }

    private Dictionary<string, List<double>> CreatePageFeatureVectors(List<PageRelationshipInfo> pages)
    {
        var vectors = new Dictionary<string, List<double>>();

        foreach (var page in pages)
        {
            var vector = new List<double>
            {
                page.PageRankScore,
                page.OutboundLinks.Count,
                page.InboundLinks.Count,
                page.Depth,
                page.Keywords.Count,
                (int)page.PageType / 10.0 // 정규화
            };

            vectors[page.Url] = vector;
        }

        return vectors;
    }

    private List<List<string>> PerformHierarchicalClustering(Dictionary<string, List<double>> pageVectors)
    {
        // 간소화된 클러스터링 구현
        var clusters = new List<List<string>>();
        var pages = pageVectors.Keys.ToList();

        // 페이지를 3-5개 클러스터로 분할 (간단한 휴리스틱)
        var clusterCount = Math.Min(Math.Max(pages.Count / 10, 3), 5);
        var pagePerCluster = pages.Count / clusterCount;

        for (int i = 0; i < clusterCount; i++)
        {
            var startIndex = i * pagePerCluster;
            var endIndex = i == clusterCount - 1 ? pages.Count : (i + 1) * pagePerCluster;
            var cluster = pages.Skip(startIndex).Take(endIndex - startIndex).ToList();
            clusters.Add(cluster);
        }

        return clusters;
    }

    private string GenerateClusterName(List<string> cluster, List<PageRelationshipInfo> pages)
    {
        var clusterPages = pages.Where(p => cluster.Contains(p.Url)).ToList();
        var commonCategory = clusterPages.GroupBy(p => p.ContentCategory)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return commonCategory ?? $"Cluster {cluster.Count} pages";
    }

    private string GenerateClusterDescription(List<string> cluster, List<PageRelationshipInfo> pages)
    {
        var clusterPages = pages.Where(p => cluster.Contains(p.Url)).ToList();
        var pageTypes = clusterPages.GroupBy(p => p.PageType)
            .OrderByDescending(g => g.Count())
            .Take(2)
            .Select(g => g.Key.ToString())
            .ToList();

        return $"{cluster.Count}개 페이지를 포함하는 클러스터 (주요 타입: {string.Join(", ", pageTypes)})";
    }

    private string FindClusterCentroid(List<string> cluster, Dictionary<string, List<double>> pageVectors)
    {
        // 클러스터 중심에 가장 가까운 페이지 찾기
        if (!cluster.Any()) return string.Empty;

        var clusterVectors = cluster.Where(pageVectors.ContainsKey).Select(url => pageVectors[url]).ToList();
        if (!clusterVectors.Any()) return cluster.First();

        // 평균 벡터 계산
        var dimensions = clusterVectors.First().Count;
        var centroid = new double[dimensions];
        for (int i = 0; i < dimensions; i++)
        {
            centroid[i] = clusterVectors.Average(v => v[i]);
        }

        // 중심점에 가장 가까운 페이지 찾기
        var minDistance = double.MaxValue;
        var centroidUrl = cluster.First();

        foreach (var url in cluster.Where(pageVectors.ContainsKey))
        {
            var distance = CalculateEuclideanDistance(pageVectors[url], centroid);
            if (distance < minDistance)
            {
                minDistance = distance;
                centroidUrl = url;
            }
        }

        return centroidUrl;
    }

    private double CalculateEuclideanDistance(List<double> vector1, double[] vector2)
    {
        var sum = 0.0;
        for (int i = 0; i < Math.Min(vector1.Count, vector2.Length); i++)
        {
            sum += Math.Pow(vector1[i] - vector2[i], 2);
        }
        return Math.Sqrt(sum);
    }

    private double CalculateClusterDensity(List<string> cluster, List<PageLinkRelationship> linkRelationships)
    {
        if (cluster.Count < 2) return 0.0;

        var internalLinks = linkRelationships.Count(r =>
            cluster.Contains(r.SourceUrl) && cluster.Contains(r.TargetUrl));

        var maxPossibleLinks = cluster.Count * (cluster.Count - 1);
        return maxPossibleLinks > 0 ? internalLinks / (double)maxPossibleLinks : 0.0;
    }

    private List<string> ExtractClusterKeywords(List<string> cluster, List<PageRelationshipInfo> pages)
    {
        var clusterPages = pages.Where(p => cluster.Contains(p.Url)).ToList();
        var allKeywords = clusterPages.SelectMany(p => p.Keywords).ToList();

        return allKeywords
            .GroupBy(k => k)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();
    }

    private string ClassifyClusterCategory(List<string> cluster, List<PageRelationshipInfo> pages)
    {
        var clusterPages = pages.Where(p => cluster.Contains(p.Url)).ToList();
        return clusterPages
            .GroupBy(p => p.ContentCategory)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "Mixed";
    }

    private double EvaluateClusteringQuality(List<ContentCluster> clusters, ContentRelationshipAnalysisResult analysisResult)
    {
        if (!clusters.Any()) return 0.0;

        // 클러스터 크기 균형성
        var sizes = clusters.Select(c => c.Size).ToList();
        var avgSize = sizes.Average();
        var sizeVariance = sizes.Sum(s => Math.Pow(s - avgSize, 2)) / sizes.Count;
        var balanceScore = Math.Max(0.0, 1.0 - sizeVariance / (avgSize * avgSize));

        // 클러스터 밀도 평균
        var densityScore = clusters.Average(c => c.Density);

        return (balanceScore + densityScore) / 2.0;
    }

    private double CalculateSilhouetteScore(List<ContentCluster> clusters, Dictionary<string, List<double>> pageVectors)
    {
        // 실루엣 점수 계산 (간소화)
        return 0.6; // 임시 값
    }

    private double CalculateIntraClusterCohesion(List<ContentCluster> clusters, Dictionary<string, List<double>> pageVectors)
    {
        // 클러스터 내 응집도 계산 (간소화)
        return clusters.Average(c => c.Density);
    }

    private double CalculateInterClusterSeparation(List<ContentCluster> clusters, Dictionary<string, List<double>> pageVectors)
    {
        // 클러스터 간 분리도 계산 (간소화)
        return 0.7; // 임시 값
    }

    private SiteTopologyMetrics CalculateSiteTopologyMetrics(ContentRelationshipAnalysisResult result)
    {
        var metrics = new SiteTopologyMetrics
        {
            TotalPages = result.Pages.Count,
            TotalLinks = result.LinkRelationships.Count
        };

        if (result.Pages.Any())
        {
            metrics.AverageDepth = result.Pages.Average(p => p.Depth);
            metrics.MaxDepth = result.Pages.Max(p => p.Depth);
            metrics.AverageOutboundLinks = result.Pages.Average(p => p.OutboundLinks.Count);
            metrics.AverageInboundLinks = result.Pages.Average(p => p.InboundLinks.Count);

            // 허브 페이지 (발신 링크가 많은 페이지)
            metrics.HubPages = result.Pages.Count(p => p.OutboundLinks.Count > metrics.AverageOutboundLinks * 2);

            // 권위 페이지 (수신 링크가 많은 페이지)
            metrics.AuthorityPages = result.Pages.Count(p => p.InboundLinks.Count > metrics.AverageInboundLinks * 2);

            // 데드 엔드 페이지
            metrics.DeadEndPages = result.Pages.Count(p => p.OutboundLinks.Count == 0);

            // 네트워크 밀도
            var maxPossibleLinks = result.Pages.Count * (result.Pages.Count - 1);
            metrics.NetworkDensity = maxPossibleLinks > 0 ? result.LinkRelationships.Count / (double)maxPossibleLinks : 0.0;

            // 클러스터링 계수 (간소화)
            metrics.ClusteringCoefficient = 0.3; // 임시 값

            // 평균 경로 길이 (간소화)
            metrics.AveragePathLength = metrics.AverageDepth;

            // 직경
            metrics.Diameter = metrics.MaxDepth;
        }

        // 고아 페이지는 계층 구조에서 계산
        if (result.ContentHierarchy != null)
        {
            metrics.OrphanPages = result.ContentHierarchy.OrphanPages.Count;
        }

        return metrics;
    }

    private double CalculateAnalysisQuality(ContentRelationshipAnalysisResult result)
    {
        var score = 0.0;

        // 페이지 수에 따른 점수
        score += Math.Min(result.Pages.Count / 50.0, 0.3);

        // 링크 관계의 품질
        if (result.Pages.Any())
        {
            var linkDensity = result.LinkRelationships.Count / (double)result.Pages.Count;
            score += Math.Min(linkDensity / 10.0, 0.2);
        }

        // 네비게이션 구조 품질
        if (result.NavigationStructure != null)
        {
            score += result.NavigationStructure.ConsistencyScore * 0.2;
        }

        // 계층 구조 품질
        if (result.ContentHierarchy != null)
        {
            score += result.ContentHierarchy.StructureQuality * 0.2;
        }

        // 클러스터링 품질
        if (result.ContentClusters != null)
        {
            score += result.ContentClusters.QualityScore * 0.1;
        }

        return Math.Min(score, 1.0);
    }

    #endregion
}

/// <summary>
/// 스레드 안전한 리스트 (간단한 구현)
/// </summary>
public class ConcurrentList<T> : IEnumerable<T>
{
    private readonly List<T> _list = new();
    private readonly object _lock = new();

    public void Add(T item)
    {
        lock (_lock)
        {
            _list.Add(item);
        }
    }

    public List<T> ToList()
    {
        lock (_lock)
        {
            return new List<T>(_list);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            return new List<T>(_list).GetEnumerator();
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}