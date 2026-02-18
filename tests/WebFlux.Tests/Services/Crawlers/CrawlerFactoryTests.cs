using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// CrawlerFactory 단위 테스트
/// Factory 패턴 구현 검증
/// </summary>
public class CrawlerFactoryTests
{
    private readonly IServiceProvider _mockServiceProvider;
    private readonly CrawlerFactory _factory;

    public CrawlerFactoryTests()
    {
        _mockServiceProvider = Substitute.For<IServiceProvider>();
        _factory = new CrawlerFactory(_mockServiceProvider);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new CrawlerFactory(_mockServiceProvider);
        factory.Should().NotBeNull();
    }

    #endregion

    #region CreateCrawler Tests

    [Fact]
    public void CreateCrawler_WithBreadthFirstStrategy_ShouldReturnBreadthFirstCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        _mockServiceProvider.GetService(typeof(BreadthFirstCrawler))
            .Returns(mockCrawler);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.BreadthFirst);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Received(1).GetService(typeof(BreadthFirstCrawler));
    }

    [Fact]
    public void CreateCrawler_WithDepthFirstStrategy_ShouldReturnDepthFirstCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        _mockServiceProvider.GetService(typeof(DepthFirstCrawler))
            .Returns(mockCrawler);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.DepthFirst);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Received(1).GetService(typeof(DepthFirstCrawler));
    }

    [Fact]
    public void CreateCrawler_WithSitemapStrategy_ShouldReturnSitemapCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        _mockServiceProvider.GetService(typeof(SitemapCrawler))
            .Returns(mockCrawler);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.Sitemap);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Received(1).GetService(typeof(SitemapCrawler));
    }

    [Fact]
    public void CreateCrawler_WithIntelligentStrategy_ShouldReturnIntelligentCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        _mockServiceProvider.GetService(typeof(IntelligentCrawler))
            .Returns(mockCrawler);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.Intelligent);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Received(1).GetService(typeof(IntelligentCrawler));
    }

    [Fact]
    public void CreateCrawler_WithDynamicStrategy_ShouldReturnPlaywrightCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        _mockServiceProvider.GetService(typeof(PlaywrightCrawler))
            .Returns(mockCrawler);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.Dynamic);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Received(1).GetService(typeof(PlaywrightCrawler));
    }

    [Fact]
    public void CreateCrawler_WithUnsupportedStrategy_ShouldThrowArgumentException()
    {
        // Arrange
        var unsupportedStrategy = CrawlStrategy.Priority; // Priority is not implemented in factory

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _factory.CreateCrawler(unsupportedStrategy));
        exception.Message.Should().Contain("Unknown crawl strategy");
    }

    [Fact]
    public void CreateCrawler_WithInvalidEnumValue_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidStrategy = (CrawlStrategy)999; // Invalid enum value

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _factory.CreateCrawler(invalidStrategy));
        exception.Message.Should().Contain("Unknown crawl strategy");
    }

    #endregion

    #region Multiple Creation Tests

    [Fact]
    public void CreateCrawler_CalledMultipleTimes_ShouldCallServiceProvider()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        _mockServiceProvider.GetService(typeof(BreadthFirstCrawler))
            .Returns(mockCrawler);

        // Act
        _factory.CreateCrawler(CrawlStrategy.BreadthFirst);
        _factory.CreateCrawler(CrawlStrategy.BreadthFirst);
        _factory.CreateCrawler(CrawlStrategy.BreadthFirst);

        // Assert
        _mockServiceProvider.Received(3).GetService(typeof(BreadthFirstCrawler));
    }

    [Fact]
    public void CreateCrawler_WithDifferentStrategies_ShouldReturnDifferentCrawlers()
    {
        // Arrange
        var breadthFirstCrawler = Substitute.For<ICrawler>();
        var depthFirstCrawler = Substitute.For<ICrawler>();

        _mockServiceProvider.GetService(typeof(BreadthFirstCrawler))
            .Returns(breadthFirstCrawler);
        _mockServiceProvider.GetService(typeof(DepthFirstCrawler))
            .Returns(depthFirstCrawler);

        // Act
        var crawler1 = _factory.CreateCrawler(CrawlStrategy.BreadthFirst);
        var crawler2 = _factory.CreateCrawler(CrawlStrategy.DepthFirst);

        // Assert
        crawler1.Should().NotBeNull();
        crawler2.Should().NotBeNull();
        crawler1.Should().NotBeSameAs(crawler2);
    }

    #endregion
}
