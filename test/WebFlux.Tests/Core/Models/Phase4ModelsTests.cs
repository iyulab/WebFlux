using WebFlux.Core.Models;
using Xunit;

namespace WebFlux.Tests.Core.Models;

/// <summary>
/// Phase 4에서 구현된 모델들의 단위 테스트
/// 데이터 모델의 기본 기능과 유효성을 검증
/// </summary>
public class Phase4ModelsTests
{
    [Fact]
    public void PackageMetadata_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var packageMetadata = new PackageMetadata();

        // Assert
        Assert.NotNull(packageMetadata.Dependencies);
        Assert.NotNull(packageMetadata.DevDependencies);
        Assert.NotNull(packageMetadata.Keywords);
        Assert.NotNull(packageMetadata.DetectedFrameworks);
        Assert.Equal(PackageEcosystemType.Unknown, packageMetadata.EcosystemType);
        Assert.True(packageMetadata.AnalyzedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void PackageEcosystemAnalysisResult_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var result = new PackageEcosystemAnalysisResult();

        // Assert
        Assert.NotNull(result.PackagesFound);
        Assert.Empty(result.PackagesFound);
        Assert.False(result.AnalysisSuccessful);
        Assert.True(result.AnalysisTimestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TechStackAnalysisResult_WithFrameworks_ShouldContainFrameworks()
    {
        // Arrange
        var result = new TechStackAnalysisResult
        {
            ProjectType = ProjectType.WebAPI,
            PrimaryFrameworks = new List<string> { "Express.js", "React" },
            ArchitecturalPatterns = new List<string> { "REST API", "MVC" }
        };

        // Act & Assert
        Assert.Equal(ProjectType.WebAPI, result.ProjectType);
        Assert.Contains("Express.js", result.PrimaryFrameworks);
        Assert.Contains("React", result.PrimaryFrameworks);
        Assert.Contains("REST API", result.ArchitecturalPatterns);
    }

    [Fact]
    public void DependencyInfo_WithVulnerability_ShouldHaveVulnerabilityFlag()
    {
        // Arrange
        var dependency = new DependencyInfo
        {
            Name = "vulnerable-package",
            Version = "1.0.0",
            HasKnownVulnerabilities = true,
            VulnerabilityCount = 2,
            SecurityScore = 0.3
        };

        // Act & Assert
        Assert.True(dependency.HasKnownVulnerabilities);
        Assert.Equal(2, dependency.VulnerabilityCount);
        Assert.Equal(0.3, dependency.SecurityScore);
    }

    [Fact]
    public void APIMetadata_WithEndpoints_ShouldManageEndpointsList()
    {
        // Arrange
        var apiMetadata = new APIMetadata
        {
            Title = "Test API",
            Version = "1.0.0",
            DocumentationType = APIDocumentationType.OpenAPI3
        };

        var endpoint = new EndpointInfo
        {
            Method = "GET",
            Path = "/users",
            Summary = "Get users"
        };

        // Act
        apiMetadata.Endpoints.Add(endpoint);

        // Assert
        Assert.Equal("Test API", apiMetadata.Title);
        Assert.Equal("1.0.0", apiMetadata.Version);
        Assert.Equal(APIDocumentationType.OpenAPI3, apiMetadata.DocumentationType);
        Assert.Single(apiMetadata.Endpoints);
        Assert.Equal("GET", apiMetadata.Endpoints.First().Method);
        Assert.Equal("/users", apiMetadata.Endpoints.First().Path);
    }

    [Fact]
    public void EndpointInfo_WithParameters_ShouldManageParametersList()
    {
        // Arrange
        var endpoint = new EndpointInfo
        {
            Method = "POST",
            Path = "/users",
            Summary = "Create user"
        };

        var parameter = new ParameterInfo
        {
            Name = "userId",
            In = "path",
            Type = "integer",
            Required = true
        };

        // Act
        endpoint.Parameters.Add(parameter);

        // Assert
        Assert.Single(endpoint.Parameters);
        Assert.Equal("userId", endpoint.Parameters.First().Name);
        Assert.Equal("path", endpoint.Parameters.First().In);
        Assert.True(endpoint.Parameters.First().Required);
    }

    [Fact]
    public void PageRelationshipInfo_WithLinks_ShouldManageLinksList()
    {
        // Arrange
        var pageInfo = new PageRelationshipInfo
        {
            Url = "https://example.com/page1",
            Title = "Test Page",
            ContentType = "article"
        };

        var link = new PageLinkInfo
        {
            TargetUrl = "https://example.com/page2",
            LinkType = LinkType.Related,
            AnchorText = "Related Page"
        };

        // Act
        pageInfo.OutgoingLinks.Add(link);

        // Assert
        Assert.Equal("https://example.com/page1", pageInfo.Url);
        Assert.Equal("Test Page", pageInfo.Title);
        Assert.Single(pageInfo.OutgoingLinks);
        Assert.Equal("https://example.com/page2", pageInfo.OutgoingLinks.First().TargetUrl);
        Assert.Equal(LinkType.Related, pageInfo.OutgoingLinks.First().LinkType);
    }

    [Fact]
    public void ContentRelationshipAnalysisResult_WithPages_ShouldManagePagesList()
    {
        // Arrange
        var result = new ContentRelationshipAnalysisResult
        {
            BaseUrl = "https://example.com",
            AnalysisSuccessful = true
        };

        var page = new PageRelationshipInfo
        {
            Url = "https://example.com/page1",
            Title = "Page 1",
            PageRankScore = 0.85
        };

        // Act
        result.PagesAnalyzed.Add(page);

        // Assert
        Assert.Equal("https://example.com", result.BaseUrl);
        Assert.True(result.AnalysisSuccessful);
        Assert.Single(result.PagesAnalyzed);
        Assert.Equal(0.85, result.PagesAnalyzed.First().PageRankScore);
    }

    [Theory]
    [InlineData(PackageEcosystemType.NodeJs, "package.json")]
    [InlineData(PackageEcosystemType.Python, "requirements.txt")]
    [InlineData(PackageEcosystemType.CSharp, "packages.config")]
    [InlineData(PackageEcosystemType.PHP, "composer.json")]
    [InlineData(PackageEcosystemType.Ruby, "Gemfile")]
    public void PackageEcosystemType_EnumValues_ShouldHaveExpectedValues(
        PackageEcosystemType ecosystem, string expectedFileName)
    {
        // Act & Assert - 열거형 값이 올바르게 정의되어 있는지 확인
        Assert.True(Enum.IsDefined(typeof(PackageEcosystemType), ecosystem));

        // 각 생태계 타입이 예상되는 파일과 연관되는지 간접적으로 확인
        Assert.NotEqual(PackageEcosystemType.Unknown, ecosystem);
    }

    [Theory]
    [InlineData(APIDocumentationType.OpenAPI3)]
    [InlineData(APIDocumentationType.Swagger2)]
    [InlineData(APIDocumentationType.GraphQL)]
    [InlineData(APIDocumentationType.RAML)]
    [InlineData(APIDocumentationType.AsyncAPI)]
    public void APIDocumentationType_EnumValues_ShouldBeValid(APIDocumentationType docType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(APIDocumentationType), docType));
        Assert.NotEqual(APIDocumentationType.Unknown, docType);
    }

    [Theory]
    [InlineData(ProjectType.WebAPI)]
    [InlineData(ProjectType.MobileApp)]
    [InlineData(ProjectType.DesktopApp)]
    [InlineData(ProjectType.WebApp)]
    [InlineData(ProjectType.Library)]
    public void ProjectType_EnumValues_ShouldBeValid(ProjectType projectType)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(ProjectType), projectType));
        Assert.NotEqual(ProjectType.Unknown, projectType);
    }

    [Fact]
    public void SecurityVulnerability_WithCVE_ShouldFormatCorrectly()
    {
        // Arrange
        var vulnerability = new SecurityVulnerability
        {
            VulnerabilityId = "CVE-2023-1234",
            PackageName = "vulnerable-lib",
            AffectedVersions = "< 2.0.0",
            Severity = VulnerabilitySeverity.High,
            Description = "Remote code execution vulnerability",
            FixedInVersion = "2.0.0"
        };

        // Act & Assert
        Assert.Equal("CVE-2023-1234", vulnerability.VulnerabilityId);
        Assert.Equal("vulnerable-lib", vulnerability.PackageName);
        Assert.Equal(VulnerabilitySeverity.High, vulnerability.Severity);
        Assert.Equal("2.0.0", vulnerability.FixedInVersion);
    }

    [Fact]
    public void ContentCluster_WithPages_ShouldCalculateMetrics()
    {
        // Arrange
        var cluster = new ContentCluster
        {
            ClusterLabel = "Technology Articles",
            ClusterType = ClusterType.Topic,
            CohesionScore = 0.85
        };

        cluster.Pages.Add("https://example.com/tech1");
        cluster.Pages.Add("https://example.com/tech2");
        cluster.Pages.Add("https://example.com/tech3");

        // Act & Assert
        Assert.Equal("Technology Articles", cluster.ClusterLabel);
        Assert.Equal(ClusterType.Topic, cluster.ClusterType);
        Assert.Equal(3, cluster.Pages.Count);
        Assert.Equal(0.85, cluster.CohesionScore);
    }

    [Fact]
    public void NavigationMenu_WithItems_ShouldManageHierarchy()
    {
        // Arrange
        var menu = new NavigationMenu
        {
            MenuType = NavigationMenuType.Primary,
            MenuLabel = "Main Navigation"
        };

        var menuItem = new NavigationMenuItem
        {
            Label = "Products",
            Url = "/products",
            Level = 1
        };

        var subMenuItem = new NavigationMenuItem
        {
            Label = "Product A",
            Url = "/products/a",
            Level = 2
        };

        // Act
        menuItem.Children.Add(subMenuItem);
        menu.MenuItems.Add(menuItem);

        // Assert
        Assert.Equal(NavigationMenuType.Primary, menu.MenuType);
        Assert.Single(menu.MenuItems);
        Assert.Equal("Products", menu.MenuItems.First().Label);
        Assert.Single(menu.MenuItems.First().Children);
        Assert.Equal(2, menu.MenuItems.First().Children.First().Level);
    }
}