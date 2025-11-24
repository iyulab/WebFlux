using WebFlux.Core.Models;
using Xunit;
using FluentAssertions;

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

    #region BreadcrumbExample Tests

    [Fact]
    public void BreadcrumbExample_ShouldInitializeWithDefaults()
    {
        // Act
        var example = new BreadcrumbExample();

        // Assert
        example.PageUrl.Should().Be(string.Empty);
        example.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void BreadcrumbExample_ShouldContainOrderedItems()
    {
        // Arrange
        var items = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", Order = 1 },
            new BreadcrumbItem { Text = "Products", Url = "/products", Order = 2 },
            new BreadcrumbItem { Text = "Laptops", Url = "/products/laptops", Order = 3, IsCurrentPage = true }
        };

        // Act
        var example = new BreadcrumbExample
        {
            PageUrl = "/products/laptops",
            Items = items
        };

        // Assert
        example.PageUrl.Should().Be("/products/laptops");
        example.Items.Should().HaveCount(3);
        example.Items.Should().BeInAscendingOrder(x => x.Order);
        example.Items.Last().IsCurrentPage.Should().BeTrue();
    }

    #endregion

    #region BreadcrumbPattern Tests

    [Fact]
    public void BreadcrumbPattern_ShouldInitializeWithDefaults()
    {
        // Act
        var pattern = new BreadcrumbPattern();

        // Assert
        pattern.PatternName.Should().Be(string.Empty);
        pattern.Examples.Should().NotBeNull().And.BeEmpty();
        pattern.ConsistencyScore.Should().Be(0);
        pattern.PageCount.Should().Be(0);
    }

    [Fact]
    public void BreadcrumbPattern_WithExamples_ShouldTrackConsistency()
    {
        // Arrange
        var examples = new List<BreadcrumbExample>
        {
            new BreadcrumbExample
            {
                PageUrl = "/products/laptops/dell",
                Items = new List<BreadcrumbItem>
                {
                    new BreadcrumbItem { Text = "Home", Url = "/", Order = 1 },
                    new BreadcrumbItem { Text = "Products", Url = "/products", Order = 2 },
                    new BreadcrumbItem { Text = "Laptops", Url = "/products/laptops", Order = 3 }
                }
            }
        };

        // Act
        var pattern = new BreadcrumbPattern
        {
            PatternName = "E-commerce product hierarchy",
            Examples = examples,
            ConsistencyScore = 0.95,
            PageCount = 150
        };

        // Assert
        pattern.PatternName.Should().Be("E-commerce product hierarchy");
        pattern.Examples.Should().HaveCount(1);
        pattern.ConsistencyScore.Should().BeInRange(0, 1);
        pattern.PageCount.Should().BeGreaterThan(0);
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

    #region NavigationItem Tests

    [Fact]
    public void NavigationItem_ShouldInitializeWithDefaults()
    {
        // Act
        var item = new NavigationItem();

        // Assert
        item.Text.Should().Be(string.Empty);
        item.Url.Should().Be(string.Empty);
        item.Children.Should().NotBeNull().And.BeEmpty();
        item.Level.Should().Be(0);
        item.HasActiveState.Should().BeFalse();
        item.Importance.Should().Be(0);
    }

    [Fact]
    public void NavigationItem_WithChildren_ShouldSupportHierarchy()
    {
        // Arrange
        var subItems = new List<NavigationItem>
        {
            new NavigationItem { Text = "Smartphones", Url = "/products/smartphones", Level = 2 },
            new NavigationItem { Text = "Tablets", Url = "/products/tablets", Level = 2 }
        };

        // Act
        var item = new NavigationItem
        {
            Text = "Products",
            Url = "/products",
            Children = subItems,
            Level = 1,
            HasActiveState = true,
            Importance = 0.9
        };

        // Assert
        item.Text.Should().Be("Products");
        item.Children.Should().HaveCount(2);
        item.Level.Should().Be(1);
        item.Children.All(c => c.Level > item.Level).Should().BeTrue();
        item.Importance.Should().BeInRange(0, 1);
    }

    #endregion

    #region NavigationMenu Tests

    [Fact]
    public void NavigationMenu_ShouldInitializeWithDefaults()
    {
        // Act
        var menu = new NavigationMenu();

        // Assert
        menu.Type.Should().Be(default(NavigationType));
        menu.Items.Should().NotBeNull().And.BeEmpty();
        menu.Position.Should().Be(string.Empty);
        menu.Structure.Should().Be(string.Empty);
        menu.IsResponsive.Should().BeFalse();
    }

    [Fact]
    public void NavigationMenu_Primary_ShouldHaveMainItems()
    {
        // Arrange
        var items = new List<NavigationItem>
        {
            new NavigationItem { Text = "Home", Url = "/", Level = 1, Importance = 1.0 },
            new NavigationItem { Text = "Products", Url = "/products", Level = 1, Importance = 0.9 },
            new NavigationItem { Text = "About", Url = "/about", Level = 1, Importance = 0.7 }
        };

        // Act
        var menu = new NavigationMenu
        {
            Type = NavigationType.Primary,
            Items = items,
            Position = "header",
            Structure = "hierarchical",
            IsResponsive = true
        };

        // Assert
        menu.Type.Should().Be(NavigationType.Primary);
        menu.Items.Should().HaveCount(3);
        menu.Position.Should().Be("header");
        menu.Structure.Should().Be("hierarchical");
        menu.IsResponsive.Should().BeTrue();
    }

    [Fact]
    public void NavigationMenu_Footer_ShouldSupportFlatStructure()
    {
        // Arrange & Act
        var menu = new NavigationMenu
        {
            Type = NavigationType.Footer,
            Items = new List<NavigationItem>
            {
                new NavigationItem { Text = "Privacy", Url = "/privacy" },
                new NavigationItem { Text = "Terms", Url = "/terms" },
                new NavigationItem { Text = "Contact", Url = "/contact" }
            },
            Position = "footer",
            Structure = "flat",
            IsResponsive = true
        };

        // Assert
        menu.Type.Should().Be(NavigationType.Footer);
        menu.Structure.Should().Be("flat");
    }

    #endregion

    #region NavigationStructureResult Tests

    [Fact]
    public void NavigationStructureResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new NavigationStructureResult();

        // Assert
        result.PrimaryNavigation.Should().BeNull();
        result.SecondaryNavigations.Should().NotBeNull().And.BeEmpty();
        result.FooterNavigation.Should().BeNull();
        result.BreadcrumbPatterns.Should().NotBeNull().And.BeEmpty();
        result.ConsistencyScore.Should().Be(0);
        result.EfficiencyScore.Should().Be(0);
        result.AccessibilityScore.Should().Be(0);
    }

    [Fact]
    public void NavigationStructureResult_Complete_ShouldHaveAllComponents()
    {
        // Arrange
        var primaryNav = new NavigationMenu
        {
            Type = NavigationType.Primary,
            Items = new List<NavigationItem>
            {
                new NavigationItem { Text = "Home", Url = "/" },
                new NavigationItem { Text = "Products", Url = "/products" }
            }
        };

        var footerNav = new NavigationMenu
        {
            Type = NavigationType.Footer,
            Items = new List<NavigationItem>
            {
                new NavigationItem { Text = "Privacy", Url = "/privacy" }
            }
        };

        var breadcrumbPattern = new BreadcrumbPattern
        {
            PatternName = "Product hierarchy",
            ConsistencyScore = 0.9,
            PageCount = 100
        };

        // Act
        var result = new NavigationStructureResult
        {
            PrimaryNavigation = primaryNav,
            FooterNavigation = footerNav,
            BreadcrumbPatterns = new List<BreadcrumbPattern> { breadcrumbPattern },
            ConsistencyScore = 0.95,
            EfficiencyScore = 0.88,
            AccessibilityScore = 0.92
        };

        // Assert
        result.PrimaryNavigation.Should().NotBeNull();
        result.FooterNavigation.Should().NotBeNull();
        result.BreadcrumbPatterns.Should().HaveCount(1);
        result.ConsistencyScore.Should().BeInRange(0, 1);
        result.EfficiencyScore.Should().BeInRange(0, 1);
        result.AccessibilityScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public void NavigationStructureResult_HighQualityScores_ShouldIndicateGoodStructure()
    {
        // Arrange & Act
        var result = new NavigationStructureResult
        {
            ConsistencyScore = 0.95,
            EfficiencyScore = 0.90,
            AccessibilityScore = 0.92
        };

        // Assert
        result.ConsistencyScore.Should().BeGreaterThan(0.9);
        result.EfficiencyScore.Should().BeGreaterThan(0.85);
        result.AccessibilityScore.Should().BeGreaterThan(0.9);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AllNavigationModels_ShouldBeIndependentlyInstantiable()
    {
        // Act
        var breadcrumbItem = new BreadcrumbItem();
        var breadcrumbExample = new BreadcrumbExample();
        var breadcrumbPattern = new BreadcrumbPattern();
        var alternateLanguage = new AlternateLanguage();
        var navigationLink = new NavigationLink();
        var navigationItem = new NavigationItem();
        var navigationMenu = new NavigationMenu();
        var navigationStructureResult = new NavigationStructureResult();

        // Assert
        breadcrumbItem.Should().NotBeNull();
        breadcrumbExample.Should().NotBeNull();
        breadcrumbPattern.Should().NotBeNull();
        alternateLanguage.Should().NotBeNull();
        navigationLink.Should().NotBeNull();
        navigationItem.Should().NotBeNull();
        navigationMenu.Should().NotBeNull();
        navigationStructureResult.Should().NotBeNull();
    }

    [Fact]
    public void NavigationWorkflow_ShouldBuildCompleteStructure()
    {
        // Arrange - Build breadcrumb trail
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new BreadcrumbItem { Text = "Home", Url = "/", Order = 1 },
            new BreadcrumbItem { Text = "Products", Url = "/products", Order = 2 },
            new BreadcrumbItem { Text = "Laptops", Url = "/products/laptops", Order = 3, IsCurrentPage = true }
        };

        var breadcrumbExample = new BreadcrumbExample
        {
            PageUrl = "/products/laptops",
            Items = breadcrumbs
        };

        var breadcrumbPattern = new BreadcrumbPattern
        {
            PatternName = "Product hierarchy",
            Examples = new List<BreadcrumbExample> { breadcrumbExample },
            ConsistencyScore = 0.95,
            PageCount = 100
        };

        // Build navigation menu
        var primaryNavItems = new List<NavigationItem>
        {
            new NavigationItem
            {
                Text = "Products",
                Url = "/products",
                Level = 1,
                Children = new List<NavigationItem>
                {
                    new NavigationItem { Text = "Laptops", Url = "/products/laptops", Level = 2 },
                    new NavigationItem { Text = "Phones", Url = "/products/phones", Level = 2 }
                }
            }
        };

        var primaryNav = new NavigationMenu
        {
            Type = NavigationType.Primary,
            Items = primaryNavItems,
            Position = "header",
            Structure = "hierarchical",
            IsResponsive = true
        };

        // Act - Create complete navigation structure
        var navigationStructure = new NavigationStructureResult
        {
            PrimaryNavigation = primaryNav,
            BreadcrumbPatterns = new List<BreadcrumbPattern> { breadcrumbPattern },
            ConsistencyScore = 0.95,
            EfficiencyScore = 0.90,
            AccessibilityScore = 0.92
        };

        // Assert
        navigationStructure.PrimaryNavigation.Should().NotBeNull();
        navigationStructure.PrimaryNavigation!.Items.Should().HaveCount(1);
        navigationStructure.PrimaryNavigation.Items[0].Children.Should().HaveCount(2);
        navigationStructure.BreadcrumbPatterns.Should().HaveCount(1);
        navigationStructure.BreadcrumbPatterns[0].Examples[0].Items.Should().HaveCount(3);
        navigationStructure.ConsistencyScore.Should().BeGreaterThan(0.9);
    }

    #endregion
}
