using Moq;
using WebFlux.Core.Interfaces;
using WebFlux.Services.ContentExtractors;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace WebFlux.Tests.Services.ContentExtractors;

/// <summary>
/// ContentExtractorFactory 단위 테스트
/// Factory 패턴과 Interface Provider 패턴 구현 검증
/// </summary>
public class ContentExtractorFactoryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldNotThrow()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        var factory = new ContentExtractorFactory(serviceProvider);
        factory.Should().NotBeNull();
    }

    #endregion

    #region CreateExtractor Tests - With Registered IContentExtractor

    [Fact]
    public void CreateExtractor_WithRegisteredExtractor_ShouldReturnRegisteredInstance()
    {
        // Arrange - 키드 서비스로 등록
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor("text/html");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Theory]
    [InlineData("text/html", "Html")]
    [InlineData("text/plain", "Text")]
    [InlineData("application/json", "Json")]
    [InlineData("application/xml", "Xml")]
    public void CreateExtractor_WithDifferentContentTypes_ShouldReturnMatchingExtractor(string contentType, string key)
    {
        // Arrange - 각 콘텐츠 타입에 맞는 키드 서비스 등록
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>(key, mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor(contentType);

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateExtractor_CalledMultipleTimes_ShouldReturnSameInstance()
    {
        // Arrange - Singleton으로 등록된 경우 같은 키면 같은 인스턴스
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Html", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor1 = factory.CreateExtractor("text/html");
        var extractor2 = factory.CreateExtractor("text/html");

        // Assert
        extractor1.Should().BeSameAs(extractor2);
    }

    #endregion

    #region CreateExtractor Tests - Fallback to BasicContentExtractor

    [Fact]
    public void CreateExtractor_WithoutRegisteredExtractor_ShouldReturnBasicContentExtractor()
    {
        // Arrange - IContentExtractor가 등록되지 않은 경우
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor("text/html");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeOfType<BasicContentExtractor>();
    }

    [Fact]
    public void CreateExtractor_WithoutRegisteredExtractor_WithEventPublisher_ShouldCreateBasicExtractor()
    {
        // Arrange - IEventPublisher는 등록되어 있는 경우
        var mockEventPublisher = new Mock<IEventPublisher>();
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IEventPublisher), mockEventPublisher.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor("text/html");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeOfType<BasicContentExtractor>();
    }

    [Fact]
    public void CreateExtractor_WithoutRegisteredExtractor_WithoutEventPublisher_ShouldCreateBasicExtractorWithNullEventPublisher()
    {
        // Arrange - IContentExtractor와 IEventPublisher 모두 등록되지 않은 경우
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor("text/html");

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeOfType<BasicContentExtractor>();
    }

    [Fact]
    public void CreateExtractor_FallbackScenario_CalledMultipleTimes_ShouldReturnNewInstances()
    {
        // Arrange - Fallback은 매번 새 인스턴스를 생성
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor1 = factory.CreateExtractor("text/html");
        var extractor2 = factory.CreateExtractor("text/html");

        // Assert
        extractor1.Should().NotBeNull();
        extractor2.Should().NotBeNull();
        extractor1.Should().NotBeSameAs(extractor2); // 매번 새 인스턴스
    }

    #endregion

    #region Interface Provider Pattern Tests

    [Fact]
    public void CreateExtractor_ShouldFollowInterfaceProviderPattern()
    {
        // Arrange - Interface Provider 패턴: 키드 서비스로 소비자가 구현체 제공
        var customExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedTransient<IContentExtractor>("Html", (sp, key) => customExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor("text/html");

        // Assert - 소비자가 제공한 구현체를 반환
        extractor.Should().BeSameAs(customExtractor.Object);
    }

    [Fact]
    public void CreateExtractor_WithTransientRegistration_ShouldReturnDifferentInstances()
    {
        // Arrange - Transient로 등록된 경우
        var services = new ServiceCollection();
        services.AddTransient<IContentExtractor, BasicContentExtractor>();
        services.AddSingleton<IEventPublisher>(new Mock<IEventPublisher>().Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor1 = factory.CreateExtractor("text/html");
        var extractor2 = factory.CreateExtractor("text/html");

        // Assert - Transient는 매번 새 인스턴스
        extractor1.Should().NotBeNull();
        extractor2.Should().NotBeNull();
        extractor1.Should().NotBeSameAs(extractor2);
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateExtractor_WithNullOrEmptyContentType_ShouldStillWork(string? contentType)
    {
        // Arrange - null/empty contentType일 때 "Default" 키로 폴백
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IContentExtractor>("Default", mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor(contentType!);

        // Assert
        extractor.Should().NotBeNull();
        extractor.Should().BeSameAs(mockExtractor.Object);
    }

    [Fact]
    public void CreateExtractor_WithUnknownContentType_ShouldStillReturnExtractor()
    {
        // Arrange - 알 수 없는 contentType도 처리
        var mockExtractor = new Mock<IContentExtractor>();
        var services = new ServiceCollection();
        services.AddSingleton(typeof(IContentExtractor), mockExtractor.Object);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ContentExtractorFactory(serviceProvider);

        // Act
        var extractor = factory.CreateExtractor("unknown/type");

        // Assert
        extractor.Should().NotBeNull();
    }

    #endregion
}
