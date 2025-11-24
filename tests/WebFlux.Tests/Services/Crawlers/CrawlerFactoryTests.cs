using Moq;
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
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly CrawlerFactory _factory;

    public CrawlerFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _factory = new CrawlerFactory(_mockServiceProvider.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldNotThrow()
    {
        // Act & Assert
        var factory = new CrawlerFactory(_mockServiceProvider.Object);
        factory.Should().NotBeNull();
    }

    #endregion

    #region CreateCrawler Tests

    [Fact]
    public void CreateCrawler_WithBreadthFirstStrategy_ShouldReturnBreadthFirstCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        mockCrawler.Setup(c => c.CrawlAsync(It.IsAny<string>(), It.IsAny<CrawlOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult { Url = "https://example.com" });

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(BreadthFirstCrawler)))
            .Returns(mockCrawler.Object);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.BreadthFirst);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BreadthFirstCrawler)), Times.Once);
    }

    [Fact]
    public void CreateCrawler_WithDepthFirstStrategy_ShouldReturnDepthFirstCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(DepthFirstCrawler)))
            .Returns(mockCrawler.Object);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.DepthFirst);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(DepthFirstCrawler)), Times.Once);
    }

    [Fact]
    public void CreateCrawler_WithSitemapStrategy_ShouldReturnSitemapCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(SitemapCrawler)))
            .Returns(mockCrawler.Object);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.Sitemap);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(SitemapCrawler)), Times.Once);
    }

    [Fact]
    public void CreateCrawler_WithIntelligentStrategy_ShouldReturnIntelligentCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IntelligentCrawler)))
            .Returns(mockCrawler.Object);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.Intelligent);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(IntelligentCrawler)), Times.Once);
    }

    [Fact]
    public void CreateCrawler_WithDynamicStrategy_ShouldReturnPlaywrightCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(PlaywrightCrawler)))
            .Returns(mockCrawler.Object);

        // Act
        var crawler = _factory.CreateCrawler(CrawlStrategy.Dynamic);

        // Assert
        crawler.Should().NotBeNull();
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(PlaywrightCrawler)), Times.Once);
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
        var mockCrawler = new Mock<ICrawler>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(BreadthFirstCrawler)))
            .Returns(mockCrawler.Object);

        // Act
        _factory.CreateCrawler(CrawlStrategy.BreadthFirst);
        _factory.CreateCrawler(CrawlStrategy.BreadthFirst);
        _factory.CreateCrawler(CrawlStrategy.BreadthFirst);

        // Assert
        _mockServiceProvider.Verify(sp => sp.GetService(typeof(BreadthFirstCrawler)), Times.Exactly(3));
    }

    [Fact]
    public void CreateCrawler_WithDifferentStrategies_ShouldReturnDifferentCrawlers()
    {
        // Arrange
        var breadthFirstCrawler = new Mock<ICrawler>().Object;
        var depthFirstCrawler = new Mock<ICrawler>().Object;

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(BreadthFirstCrawler)))
            .Returns(breadthFirstCrawler);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(DepthFirstCrawler)))
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
