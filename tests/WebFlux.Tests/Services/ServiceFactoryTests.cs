using Microsoft.Extensions.DependencyInjection;
using Moq;
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
        var mockService = new Mock<IAiEnhancementService>();
        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateService<IAiEnhancementService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService.Object);
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
        var mockService = new Mock<IAiEnhancementService>();
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IAiEnhancementService), mockService.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateService(typeof(IAiEnhancementService));

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService.Object);
    }

    [Fact]
    public void CreateService_Type_WithUnregisteredService_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => factory.CreateService(typeof(IAiEnhancementService)));
    }

    #endregion

    #region CreateNamedService<T> Tests

    [Fact]
    public void CreateNamedService_WithRegisteredKeyedService_ShouldReturnService()
    {
        // Arrange
        var mockService = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockService.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateNamedService<IContentExtractor>("Html");

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService.Object);
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
        var mockCrawler = new Mock<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("BreadthFirst", mockCrawler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.BreadthFirst);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler.Object);
    }

    [Fact]
    public void CreateCrawler_WithDepthFirst_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("DepthFirst", mockCrawler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.DepthFirst);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler.Object);
    }

    [Fact]
    public void CreateCrawler_WithIntelligent_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("Intelligent", mockCrawler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.Intelligent);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler.Object);
    }

    [Fact]
    public void CreateCrawler_WithSitemap_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("Sitemap", mockCrawler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.Sitemap);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler.Object);
    }

    [Fact]
    public void CreateCrawler_WithDynamic_ShouldReturnKeyedCrawler()
    {
        // Arrange
        var mockCrawler = new Mock<ICrawler>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ICrawler>("Dynamic", mockCrawler.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var crawler = factory.CreateCrawler(CrawlStrategy.Dynamic);

        // Assert
        crawler.Should().NotBeNull();
        crawler.Should().BeSameAs(mockCrawler.Object);
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
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("text/html");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateContentExtractor_WithPlainText_ShouldReturnTextExtractor()
    {
        // Arrange
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Text", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("text/plain");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateContentExtractor_WithJson_ShouldReturnJsonExtractor()
    {
        // Arrange
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Json", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("application/json");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateContentExtractor_WithMarkdown_ShouldReturnMarkdownExtractor()
    {
        // Arrange
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Markdown", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("text/markdown");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateContentExtractor_WithXml_ShouldReturnXmlExtractor()
    {
        // Arrange
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Xml", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("application/xml");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateContentExtractor_WithUnknownType_ShouldReturnDefaultExtractor()
    {
        // Arrange
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Default", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var extractor = factory.CreateContentExtractor("application/pdf");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Theory]
    [InlineData("TEXT/HTML")]
    [InlineData("Text/Plain")]
    [InlineData("APPLICATION/JSON")]
    public void CreateContentExtractor_ShouldBeCaseInsensitive(string contentType)
    {
        // Arrange
        var mockHtml = new Mock<IContentExtractor>();
        var mockText = new Mock<IContentExtractor>();
        var mockJson = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockHtml.Object);
        services.AddKeyedSingleton<IContentExtractor>("Text", mockText.Object);
        services.AddKeyedSingleton<IContentExtractor>("Json", mockJson.Object);
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
        var mockStrategy = new Mock<IChunkingStrategy>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IChunkingStrategy>("FixedSize", mockStrategy.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var strategy = factory.CreateChunkingStrategy("FixedSize");

        // Assert
        strategy.Should().NotBeNull();
        strategy.Should().BeSameAs(mockStrategy.Object);
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
        var mockService = new Mock<IAiEnhancementService>();
        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ServiceFactory(serviceProvider);

        // Act
        var service = factory.CreateAiEnhancementService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeSameAs(mockService.Object);
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
        services.AddKeyedSingleton<ICrawler>("BreadthFirst", new Mock<ICrawler>().Object);
        services.AddKeyedSingleton<ICrawler>("DepthFirst", new Mock<ICrawler>().Object);

        // Content Extractors
        services.AddKeyedSingleton<IContentExtractor>("Html", new Mock<IContentExtractor>().Object);
        services.AddKeyedSingleton<IContentExtractor>("Default", new Mock<IContentExtractor>().Object);

        // Chunking Strategies
        services.AddKeyedSingleton<IChunkingStrategy>("FixedSize", new Mock<IChunkingStrategy>().Object);

        // Optional AI service
        services.AddSingleton<IAiEnhancementService>(new Mock<IAiEnhancementService>().Object);

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
