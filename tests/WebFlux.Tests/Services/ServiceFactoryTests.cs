using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// ServiceFactory 단위 테스트
/// Interface Provider 패턴과 Keyed Services 구현 검증
/// </summary>
public class ServiceFactoryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldNotThrow()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        var factory = new ServiceFactory(serviceProvider);
        factory.Should().NotBeNull();
    }

    #endregion

    #region CreateService<T> Tests

    [Fact]
    public void CreateService_Generic_WithRegisteredService_ShouldReturnService()
    {
        // Arrange
        var mockService = Substitute.For<IAiEnhancementService>();
        var services = new ServiceCollection();
        services.AddSingleton(mockService);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateService<IAiEnhancementService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService);
    }

    [Fact]
    public void CreateService_Generic_WithUnregisteredService_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.CreateService<IAiEnhancementService>());
    }

    #endregion

    #region CreateService(Type) Tests

    [Fact]
    public void CreateService_Type_WithRegisteredService_ShouldReturnService()
    {
        // Arrange
        var mockService = Substitute.For<IAiEnhancementService>();
        var services = new ServiceCollection();
        services.AddSingleton<IAiEnhancementService>(mockService);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateService<IAiEnhancementService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService);
    }

    [Fact]
    public void CreateService_Type_WithUnregisteredService_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.CreateService<IAiEnhancementService>());
    }

    #endregion

    #region CreateNamedService<T> Tests

    [Fact]
    public void CreateNamedService_WithRegisteredKeyedService_ShouldReturnService()
    {
        // Arrange
        var mockService = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockService);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateNamedService<IContentExtractor>("Html");

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService);
    }

    [Fact]
    public void CreateNamedService_WithUnregisteredKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            factory.CreateNamedService<IContentExtractor>("NonExistent"));
    }

    #endregion

    #region CreateCrawler Tests

    [Fact]
    public void CreateCrawler_WithBreadthFirst_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("BreadthFirst", mockCrawler);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.BreadthFirst);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler);
    }

    [Fact]
    public void CreateCrawler_WithDepthFirst_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("DepthFirst", mockCrawler);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.DepthFirst);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler);
    }

    [Fact]
    public void CreateCrawler_WithIntelligent_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("Intelligent", mockCrawler);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.Intelligent);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler);
    }

    [Fact]
    public void CreateCrawler_WithSitemap_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("Sitemap", mockCrawler);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.Sitemap);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler);
    }

    [Fact]
    public void CreateCrawler_WithDynamic_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = Substitute.For<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("Dynamic", mockCrawler);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.Dynamic);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler);
    }

    [Fact]
    public void CreateCrawler_WithUnsupportedStrategy_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            factory.CreateCrawler(CrawlStrategy.Priority));
        exception.Message.Should().Contain("Unknown crawl strategy");
    }

    [Fact]
    public void CreateCrawler_WithInvalidEnumValue_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            factory.CreateCrawler((CrawlStrategy)999));
        exception.Message.Should().Contain("Unknown crawl strategy");
    }

    #endregion

    #region CreateContentExtractor Tests

    [Fact]
    public void CreateContentExtractor_WithHtml_ShouldReturnHtmlExtractor()
    {
        // Arrange
        var mockExtractor = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockExtractor);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("text/html");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor);
    }

    [Fact]
    public void CreateContentExtractor_WithPlainText_ShouldReturnTextExtractor()
    {
        // Arrange
        var mockExtractor = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Text", mockExtractor);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("text/plain");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor);
    }

    [Fact]
    public void CreateContentExtractor_WithJson_ShouldReturnJsonExtractor()
    {
        // Arrange
        var mockExtractor = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Json", mockExtractor);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("application/json");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor);
    }

    [Fact]
    public void CreateContentExtractor_WithMarkdown_ShouldReturnMarkdownExtractor()
    {
        // Arrange
        var mockExtractor = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Markdown", mockExtractor);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("text/markdown");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor);
    }

    [Fact]
    public void CreateContentExtractor_WithXml_ShouldReturnXmlExtractor()
    {
        // Arrange
        var mockExtractor = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Xml", mockExtractor);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("application/xml");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor);
    }

    [Fact]
    public void CreateContentExtractor_WithUnknownType_ShouldReturnDefaultExtractor()
    {
        // Arrange
        var mockExtractor = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Default", mockExtractor);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("application/pdf");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor);
    }

    [Theory]
    [InlineData("TEXT/HTML")]
    [InlineData("Text/Plain")]
    [InlineData("APPLICATION/JSON")]
    public void CreateContentExtractor_ShouldBeCaseInsensitive(string contentType)
    {
        // Arrange
        var mockHtml = Substitute.For<IContentExtractor>();
        var mockText = Substitute.For<IContentExtractor>();
        var mockJson = Substitute.For<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockHtml);
        services.AddKeyedSingleton<IContentExtractor>("Text", mockText);
        services.AddKeyedSingleton<IContentExtractor>("Json", mockJson);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor(contentType);

        // Assert
        extractor.Should().NotBeNull();
    }

    #endregion

    #region CreateChunkingStrategy Tests

    [Fact]
    public void CreateChunkingStrategy_WithValidKey_ShouldReturnStrategy()
    {
        // Arrange
        var mockStrategy = Substitute.For<IChunkingStrategy>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IChunkingStrategy>("FixedSize", mockStrategy);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var strategy = factory.CreateChunkingStrategy("FixedSize");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeSameAs(mockStrategy);
    }

    [Fact]
    public void CreateChunkingStrategy_WithUnregisteredKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            factory.CreateChunkingStrategy("NonExistent"));
    }

    #endregion

    #region CreateAiEnhancementService Tests

    [Fact]
    public void CreateAiEnhancementService_WithRegisteredService_ShouldReturnService()
    {
        // Arrange
        var mockService = Substitute.For<IAiEnhancementService>();
        var services = new ServiceCollection();
        services.AddSingleton(mockService);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateAiEnhancementService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService);
    }

    [Fact]
    public void CreateAiEnhancementService_WithoutRegisteredService_ShouldReturnNull()
    {
        // Arrange - 선택적 서비스이므로 등록되지 않으면 null 반환
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateAiEnhancementService();

        // Assert
        service.Should().BeNull();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ServiceFactory_CompleteIntegration_ShouldWorkWithAllServices()
    {
        // Arrange - 전체 서비스 등록
        var services = new ServiceCollection();

        // Crawlers
        services.AddKeyedSingleton<ICrawler>("BreadthFirst", Substitute.For<ICrawler>());
        services.AddKeyedSingleton<ICrawler>("DepthFirst", Substitute.For<ICrawler>());

        // Content Extractors
        services.AddKeyedSingleton<IContentExtractor>("Html", Substitute.For<IContentExtractor>());
        services.AddKeyedSingleton<IContentExtractor>("Default", Substitute.For<IContentExtractor>());

        // Chunking Strategies
        services.AddKeyedSingleton<IChunkingStrategy>("FixedSize", Substitute.For<IChunkingStrategy>());

        // Optional AI service
        services.AddSingleton<IAiEnhancementService>(Substitute.For<IAiEnhancementService>());

        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert - 모든 서비스 생성 검증
        factory.CreateCrawler(CrawlStrategy.BreadthFirst).Should().NotBeNull();
        factory.CreateContentExtractor("text/html").Should().NotBeNull();
        factory.CreateChunkingStrategy("FixedSize").Should().NotBeNull();
        factory.CreateAiEnhancementService().Should().NotBeNull();
    }

    #endregion
}
