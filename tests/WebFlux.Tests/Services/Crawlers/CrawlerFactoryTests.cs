using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;
using AwesomeAssertions;

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
    public void CreateCrawler_WithDynamicStrategy_ShouldReturnTheRegisteredDynamicCrawler()
    {
        // Arrange - the dynamic renderer arrives from a separate package under a known key, so the
        // factory resolves it by key rather than by a concrete type it can no longer name.
        var mockCrawler = Substitute.For<ICrawler>();
        var provider = new ServiceCollection()
            .AddKeyedTransient(CrawlerKeys.Dynamic, (_, _) => mockCrawler)
            .BuildServiceProvider();

        // Act
        var crawler = new CrawlerFactory(provider).CreateCrawler(CrawlStrategy.Dynamic);

        // Assert
        crawler.Should().BeSameAs(mockCrawler);
    }

    // Every public member of the enum must be a strategy the factory can build. Two members used to
    // exist that it could not: one threw, the other returned a stub that fetched nothing and
    // reported success. Registering every crawler here means a member without a switch arm fails.
    [Theory]
    [MemberData(nameof(EveryStrategy))]
    public void CreateCrawler_HandlesEveryPublicStrategy(CrawlStrategy strategy)
    {
        var provider = new ServiceCollection()
            .AddSingleton(new BreadthFirstCrawler(Substitute.For<IHttpClientService>(), Substitute.For<IEventPublisher>()))
            .AddSingleton(new DepthFirstCrawler(Substitute.For<IHttpClientService>(), Substitute.For<IEventPublisher>()))
            .AddSingleton(new SitemapCrawler(Substitute.For<IHttpClientService>(), Substitute.For<IEventPublisher>()))
            .AddKeyedTransient(CrawlerKeys.Dynamic, (_, _) => Substitute.For<ICrawler>())
            .BuildServiceProvider();

        new CrawlerFactory(provider).CreateCrawler(strategy).Should().NotBeNull();
    }

    public static TheoryData<CrawlStrategy> EveryStrategy()
    {
        var data = new TheoryData<CrawlStrategy>();
        foreach (var strategy in Enum.GetValues<CrawlStrategy>())
            data.Add(strategy);
        return data;
    }

    // The numbers are part of the contract: a configuration may store the strategy as a number, and
    // 3 and 4 stay empty so that removing members did not renumber the ones that remain.
    [Fact]
    public void CrawlStrategy_KeepsItsNumericValues()
    {
        ((int)CrawlStrategy.BreadthFirst).Should().Be(0);
        ((int)CrawlStrategy.DepthFirst).Should().Be(1);
        ((int)CrawlStrategy.Sitemap).Should().Be(2);
        ((int)CrawlStrategy.Dynamic).Should().Be(5);
        Enum.GetValues<CrawlStrategy>().Should().HaveCount(4);
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
