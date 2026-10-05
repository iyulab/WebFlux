using WebFlux.Core.Models;
using Xunit;
using AwesomeAssertions;

namespace WebFlux.Tests.Core.Models;

/// <summary>
/// 네비게이션 및 구조 모델 단위 테스트
/// Navigation and Structure models 검증
/// </summary>
public class NavigationStructureModelsTests
{
    #region BreadcrumbItem Tests

    [Fact]
    public void BreadcrumbItem_ShouldInitializeWithDefaults()
    {
        // Act
        var item = new BreadcrumbItem();

        // Assert
        item.Text.Should().Be(string.Empty);
        item.Url.Should().Be(string.Empty);
        item.Order.Should().Be(0);
        item.IsCurrentPage.Should().BeFalse();
    }

    [Fact]
    public void BreadcrumbItem_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var item = new BreadcrumbItem
        {
            Text = "Products",
            Url = "/products",
            Order = 2,
            IsCurrentPage = false
        };

        // Assert
        item.Text.Should().Be("Products");
        item.Url.Should().Be("/products");
        item.Order.Should().Be(2);
        item.IsCurrentPage.Should().BeFalse();
    }

    [Fact]
    public void BreadcrumbItem_CurrentPage_ShouldIndicateActiveState()
    {
        // Arrange & Act
        var item = new BreadcrumbItem
        {
            Text = "Product Details",
            Url = "/products/123",
            Order = 3,
            IsCurrentPage = true
        };

        // Assert
        item.IsCurrentPage.Should().BeTrue();
    }

    #endregion

    #region AlternateLanguage Tests

    [Fact]
    public void AlternateLanguage_ShouldInitializeWithDefaults()
    {
        // Act
        var altLang = new AlternateLanguage();

        // Assert
        altLang.Language.Should().BeNull();
        altLang.Url.Should().BeNull();
    }

    [Fact]
    public void AlternateLanguage_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var altLang = new AlternateLanguage
        {
            Language = "ko-KR",
            Url = "https://example.com/ko"
        };

        // Assert
        altLang.Language.Should().Be("ko-KR");
        altLang.Url.Should().Be("https://example.com/ko");
    }

    [Fact]
    public void AlternateLanguage_MultipleLanguages_ShouldSupportList()
    {
        // Arrange
        var languages = new List<AlternateLanguage>
        {
            new AlternateLanguage { Language = "en", Url = "https://example.com/en" },
            new AlternateLanguage { Language = "ko", Url = "https://example.com/ko" },
            new AlternateLanguage { Language = "ja", Url = "https://example.com/ja" }
        };

        // Assert
        languages.Should().HaveCount(3);
        languages.Should().Contain(x => x.Language == "ko");
    }

    #endregion

    #region NavigationLink Tests

    [Fact]
    public void NavigationLink_ShouldInitializeWithDefaults()
    {
        // Act
        var link = new NavigationLink();

        // Assert
        link.Text.Should().BeNull();
        link.Url.Should().BeNull();
        link.Title.Should().BeNull();
    }

    [Fact]
    public void NavigationLink_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var link = new NavigationLink
        {
            Text = "About Us",
            Url = "/about",
            Title = "Learn more about our company"
        };

        // Assert
        link.Text.Should().Be("About Us");
        link.Url.Should().Be("/about");
        link.Title.Should().Be("Learn more about our company");
    }

    #endregion

}
