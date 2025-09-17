using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using WebFlux.Configuration;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// ContentRelationshipMapper 단위 테스트
/// 콘텐츠 관계 분석 및 매핑 기능의 정확성과 안정성을 검증
/// </summary>
public class ContentRelationshipMapperTests
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ILogger<ContentRelationshipMapper>> _mockLogger;
    private readonly Mock<IOptions<WebFluxConfiguration>> _mockOptions;
    private readonly ContentRelationshipMapper _mapper;

    public ContentRelationshipMapperTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockLogger = new Mock<ILogger<ContentRelationshipMapper>>();
        _mockOptions = new Mock<IOptions<WebFluxConfiguration>>();

        _mockOptions.Setup(x => x.Value).Returns(new WebFluxConfiguration());

        _mapper = new ContentRelationshipMapper(
            _mockHttpClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    [Fact]
    public async Task AnalyzeContentRelationshipsAsync_WithSimpleWebsite_ShouldMapRelationships()
    {
        // Arrange
        var homePageContent = """
        <html>
        <head><title>Home Page</title></head>
        <body>
            <nav>
                <a href="/about">About</a>
                <a href="/products">Products</a>
                <a href="/contact">Contact</a>
            </nav>
            <main>
                <h1>Welcome to Our Website</h1>
                <p>This is the home page content.</p>
                <a href="/blog">Read our blog</a>
            </main>
        </body>
        </html>
        """;

        var aboutPageContent = """
        <html>
        <head><title>About Us</title></head>
        <body>
            <nav>
                <a href="/">Home</a>
                <a href="/products">Products</a>
                <a href="/contact">Contact</a>
            </nav>
            <main>
                <h1>About Our Company</h1>
                <p>We are a leading company in our field.</p>
                <a href="/team">Meet our team</a>
            </main>
        </body>
        </html>
        """;

        var productsPageContent = """
        <html>
        <head><title>Our Products</title></head>
        <body>
            <nav>
                <a href="/">Home</a>
                <a href="/about">About</a>
                <a href="/contact">Contact</a>
            </nav>
            <main>
                <h1>Our Products</h1>
                <div class="product">
                    <h2>Product A</h2>
                    <a href="/products/product-a">Learn more</a>
                </div>
                <div class="product">
                    <h2>Product B</h2>
                    <a href="/products/product-b">Learn more</a>
                </div>
            </main>
        </body>
        </html>
        """;

        SetupHttpClientMock("https://example.com", homePageContent);
        SetupHttpClientMock("https://example.com/about", aboutPageContent);
        SetupHttpClientMock("https://example.com/products", productsPageContent);

        // Act
        var result = await _mapper.AnalyzeContentRelationshipsAsync("https://example.com", maxDepth: 2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AnalysisSuccessful);
        Assert.True(result.PagesAnalyzed.Count >= 3);

        // 홈페이지가 높은 PageRank를 가져야 함 (많은 페이지에서 링크됨)
        var homePage = result.PagesAnalyzed.FirstOrDefault(p => p.Url == "https://example.com");
        Assert.NotNull(homePage);
        Assert.True(homePage.PageRankScore > 0);

        // 링크 관계가 올바르게 매핑되어야 함
        Assert.True(result.LinkRelationships.Count > 0);
        var homeToAboutLink = result.LinkRelationships.FirstOrDefault(l =>
            l.SourceUrl == "https://example.com" && l.TargetUrl == "https://example.com/about");
        Assert.NotNull(homeToAboutLink);
        Assert.Equal(LinkType.Navigation, homeToAboutLink.LinkType);
    }

    [Fact]
    public async Task AnalyzePageRelationshipsAsync_WithSinglePage_ShouldExtractLinks()
    {
        // Arrange
        var pageContent = """
        <html>
        <head>
            <title>Blog Post - Machine Learning Trends</title>
            <meta name="description" content="Latest trends in machine learning and AI">
            <meta name="keywords" content="machine learning, AI, trends, technology">
        </head>
        <body>
            <nav class="primary-nav">
                <a href="/">Home</a>
                <a href="/blog">Blog</a>
                <a href="/about">About</a>
            </nav>
            <article>
                <h1>Machine Learning Trends in 2024</h1>
                <p>This article explores the latest trends in machine learning.</p>
                <a href="/blog/ai-fundamentals" rel="related">Related: AI Fundamentals</a>
                <a href="/blog/deep-learning" rel="related">Related: Deep Learning Guide</a>
            </article>
            <aside>
                <h3>Related Posts</h3>
                <a href="/blog/neural-networks">Neural Networks Explained</a>
                <a href="/blog/data-science">Data Science Basics</a>
            </aside>
            <footer>
                <a href="/privacy">Privacy Policy</a>
                <a href="/terms">Terms of Service</a>
            </footer>
        </body>
        </html>
        """;

        SetupHttpClientMock("https://example.com/blog/ml-trends", pageContent);

        // Act
        var result = await _mapper.AnalyzePageRelationshipsAsync("https://example.com/blog/ml-trends");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://example.com/blog/ml-trends", result.PageUrl);
        Assert.Equal("Blog Post - Machine Learning Trends", result.Title);
        Assert.Contains("machine learning", result.Keywords);
        Assert.Contains("AI", result.Keywords);

        // 네비게이션 링크 검증
        var navigationLinks = result.OutgoingLinks.Where(l => l.LinkType == LinkType.Navigation).ToList();
        Assert.True(navigationLinks.Count >= 3);
        Assert.Contains(navigationLinks, l => l.TargetUrl == "https://example.com/");
        Assert.Contains(navigationLinks, l => l.TargetUrl == "https://example.com/blog");

        // 관련 콘텐츠 링크 검증
        var relatedLinks = result.OutgoingLinks.Where(l => l.LinkType == LinkType.Related).ToList();
        Assert.True(relatedLinks.Count >= 2);
        Assert.Contains(relatedLinks, l => l.TargetUrl == "https://example.com/blog/ai-fundamentals");

        // 푸터 링크 검증
        var footerLinks = result.OutgoingLinks.Where(l => l.LinkType == LinkType.Footer).ToList();
        Assert.True(footerLinks.Count >= 2);
        Assert.Contains(footerLinks, l => l.TargetUrl == "https://example.com/privacy");
    }

    [Fact]
    public async Task AnalyzeNavigationStructureAsync_WithComplexSite_ShouldIdentifyStructure()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();

        // Act
        var result = await _mapper.AnalyzeNavigationStructureAsync(analysisResult);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.NavigationMenus.Count > 0);

        // 주 네비게이션 메뉴가 감지되어야 함
        var primaryNav = result.NavigationMenus.FirstOrDefault(m => m.MenuType == NavigationMenuType.Primary);
        Assert.NotNull(primaryNav);
        Assert.True(primaryNav.MenuItems.Count >= 3);

        // 브레드크럼이 감지되어야 함
        Assert.True(result.HasBreadcrumbs);
        Assert.True(result.BreadcrumbPaths.Count > 0);

        // 네비게이션 깊이가 계산되어야 함
        Assert.True(result.MaxNavigationDepth > 0);
        Assert.True(result.AveragePathLength > 0);
    }

    [Fact]
    public async Task BuildContentHierarchyAsync_WithCategorizedContent_ShouldCreateHierarchy()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();

        // Act
        var result = await _mapper.BuildContentHierarchyAsync(analysisResult);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RootNodes.Count > 0);

        // 계층 구조가 올바르게 생성되어야 함
        var blogSection = result.RootNodes.FirstOrDefault(n => n.ContentType == ContentNodeType.Section &&
                                                               n.Title.Contains("Blog"));
        Assert.NotNull(blogSection);
        Assert.True(blogSection.Children.Count > 0);

        // 제품 섹션이 있어야 함
        var productSection = result.RootNodes.FirstOrDefault(n => n.ContentType == ContentNodeType.Section &&
                                                                  n.Title.Contains("Products"));
        Assert.NotNull(productSection);

        // 깊이가 올바르게 설정되어야 함
        Assert.True(result.MaxDepth > 1);
        Assert.True(result.TotalNodes > result.RootNodes.Count);
    }

    [Fact]
    public async Task GenerateRelatedContentAsync_WithTargetPage_ShouldFindRelatedContent()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();
        var targetPageUrl = "https://example.com/blog/machine-learning";

        // Act
        var result = await _mapper.GenerateRelatedContentAsync(targetPageUrl, analysisResult);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetPageUrl, result.TargetPageUrl);
        Assert.True(result.RelatedPages.Count > 0);

        // 관련성 점수가 내림차순으로 정렬되어야 함
        var scores = result.RelatedPages.Select(p => p.RelatednessScore).ToList();
        for (int i = 0; i < scores.Count - 1; i++)
        {
            Assert.True(scores[i] >= scores[i + 1]);
        }

        // 카테고리별 추천이 있어야 함
        Assert.True(result.CategoryRecommendations.Count > 0);
        Assert.True(result.CategoryRecommendations.ContainsKey("Similar Topics"));
    }

    [Fact]
    public async Task PerformContentClusteringAsync_WithDiverseContent_ShouldCreateClusters()
    {
        // Arrange
        var analysisResult = CreateSampleAnalysisResult();

        // Act
        var result = await _mapper.PerformContentClusteringAsync(analysisResult);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Clusters.Count > 0);

        // 클러스터가 의미있는 크기를 가져야 함
        Assert.True(result.Clusters.All(c => c.Pages.Count > 0));

        // 클러스터에 라벨이 있어야 함
        Assert.True(result.Clusters.All(c => !string.IsNullOrEmpty(c.ClusterLabel)));

        // 응집도 점수가 계산되어야 함
        Assert.True(result.Clusters.All(c => c.CohesionScore > 0));

        // 전체 클러스터링 품질 점수가 있어야 함
        Assert.True(result.OverallClusteringQuality > 0);
    }

    [Fact]
    public async Task AnalyzeContentRelationshipsAsync_WithDeepDepth_ShouldLimitAnalysis()
    {
        // Arrange
        var homePageContent = """
        <html>
        <head><title>Home</title></head>
        <body>
            <a href="/level1">Level 1</a>
        </body>
        </html>
        """;

        SetupHttpClientMock("https://example.com", homePageContent);
        SetupHttpClientMock("https://example.com/level1",
            "<html><body><a href='/level2'>Level 2</a></body></html>");
        SetupHttpClientMock("https://example.com/level2",
            "<html><body><a href='/level3'>Level 3</a></body></html>");
        SetupHttpClientMock("https://example.com/level3",
            "<html><body><a href='/level4'>Level 4</a></body></html>");

        // Act
        var result = await _mapper.AnalyzeContentRelationshipsAsync("https://example.com", maxDepth: 2);

        // Assert
        Assert.NotNull(result);

        // maxDepth가 2이므로 level3까지만 분석되어야 함
        var analyzedUrls = result.PagesAnalyzed.Select(p => p.Url).ToList();
        Assert.Contains("https://example.com", analyzedUrls);
        Assert.Contains("https://example.com/level1", analyzedUrls);
        Assert.Contains("https://example.com/level2", analyzedUrls);
        Assert.DoesNotContain("https://example.com/level3", analyzedUrls);
    }

    [Fact]
    public async Task AnalyzePageRelationshipsAsync_WithInvalidHTML_ShouldHandleGracefully()
    {
        // Arrange
        var invalidHtml = "<html><body><h1>Title</h1><p>Content without closing tags";
        SetupHttpClientMock("https://example.com/invalid", invalidHtml);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _mapper.AnalyzePageRelationshipsAsync("https://example.com/invalid"));

        // 예외가 발생하지 않고 우아하게 처리되어야 함
        Assert.Null(exception);
    }

    [Fact]
    public async Task AnalyzeContentRelationshipsAsync_WithUnavailablePages_ShouldSkipGracefully()
    {
        // Arrange
        var homeContent = """
        <html>
        <body>
            <a href="/available">Available Page</a>
            <a href="/unavailable">Unavailable Page</a>
        </body>
        </html>
        """;

        SetupHttpClientMock("https://example.com", homeContent);
        SetupHttpClientMock("https://example.com/available", "<html><body>Available</body></html>");
        SetupHttpClientMock("https://example.com/unavailable", null, HttpStatusCode.NotFound);

        // Act
        var result = await _mapper.AnalyzeContentRelationshipsAsync("https://example.com");

        // Assert
        Assert.NotNull(result);

        // 사용 가능한 페이지만 분석 결과에 포함되어야 함
        var analyzedUrls = result.PagesAnalyzed.Select(p => p.Url).ToList();
        Assert.Contains("https://example.com", analyzedUrls);
        Assert.Contains("https://example.com/available", analyzedUrls);
        Assert.DoesNotContain("https://example.com/unavailable", analyzedUrls);
    }

    private void SetupHttpClientMock(string url, string? content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (content == null)
        {
            _mockHttpClient
                .Setup(x => x.GetStringAsync(url, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException($"Response status code does not indicate success: {(int)statusCode}"));
        }
        else
        {
            _mockHttpClient
                .Setup(x => x.GetStringAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(content);
        }
    }

    private ContentRelationshipAnalysisResult CreateSampleAnalysisResult()
    {
        var pages = new List<PageRelationshipInfo>
        {
            new()
            {
                Url = "https://example.com",
                Title = "Home Page",
                ContentType = "homepage",
                Keywords = new List<string> { "home", "main" },
                PageRankScore = 0.85,
                OutgoingLinks = new List<PageLinkInfo>
                {
                    new() { TargetUrl = "https://example.com/about", LinkType = LinkType.Navigation },
                    new() { TargetUrl = "https://example.com/products", LinkType = LinkType.Navigation },
                    new() { TargetUrl = "https://example.com/blog", LinkType = LinkType.Navigation }
                }
            },
            new()
            {
                Url = "https://example.com/blog/machine-learning",
                Title = "Machine Learning Guide",
                ContentType = "article",
                Keywords = new List<string> { "machine learning", "AI", "technology" },
                PageRankScore = 0.65,
                OutgoingLinks = new List<PageLinkInfo>
                {
                    new() { TargetUrl = "https://example.com/blog/neural-networks", LinkType = LinkType.Related }
                }
            },
            new()
            {
                Url = "https://example.com/blog/neural-networks",
                Title = "Neural Networks Explained",
                ContentType = "article",
                Keywords = new List<string> { "neural networks", "deep learning", "AI" },
                PageRankScore = 0.55
            },
            new()
            {
                Url = "https://example.com/products/ai-tool",
                Title = "AI Development Tool",
                ContentType = "product",
                Keywords = new List<string> { "AI", "tool", "development" },
                PageRankScore = 0.45
            }
        };

        var linkRelationships = new List<PageLinkRelationship>
        {
            new()
            {
                SourceUrl = "https://example.com",
                TargetUrl = "https://example.com/blog/machine-learning",
                LinkType = LinkType.Content,
                AnchorText = "Machine Learning Guide"
            },
            new()
            {
                SourceUrl = "https://example.com/blog/machine-learning",
                TargetUrl = "https://example.com/blog/neural-networks",
                LinkType = LinkType.Related,
                AnchorText = "Neural Networks"
            }
        };

        return new ContentRelationshipAnalysisResult
        {
            BaseUrl = "https://example.com",
            PagesAnalyzed = pages,
            LinkRelationships = linkRelationships,
            AnalysisSuccessful = true,
            AnalysisTimestamp = DateTimeOffset.UtcNow
        };
    }
}