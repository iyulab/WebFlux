using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using WebFlux.Configuration;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// PackageEcosystemAnalyzer 단위 테스트
/// 패키지 생태계 분석 기능의 정확성과 안정성을 검증
/// </summary>
public class PackageEcosystemAnalyzerTests
{
    private readonly Mock<IHttpClientService> _mockHttpClient;
    private readonly Mock<ILogger<PackageEcosystemAnalyzer>> _mockLogger;
    private readonly Mock<IOptions<WebFluxConfiguration>> _mockOptions;
    private readonly PackageEcosystemAnalyzer _analyzer;

    public PackageEcosystemAnalyzerTests()
    {
        _mockHttpClient = new Mock<IHttpClientService>();
        _mockLogger = new Mock<ILogger<PackageEcosystemAnalyzer>>();
        _mockOptions = new Mock<IOptions<WebFluxConfiguration>>();

        _mockOptions.Setup(x => x.Value).Returns(new WebFluxConfiguration());

        _analyzer = new PackageEcosystemAnalyzer(
            _mockHttpClient.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    [Fact]
    public async Task AnalyzeFromWebsiteAsync_WithValidNodeJsPackage_ShouldReturnAnalysis()
    {
        // Arrange
        var packageJsonContent = """
        {
            "name": "test-project",
            "version": "1.0.0",
            "description": "Test project",
            "main": "index.js",
            "scripts": {
                "start": "node index.js",
                "test": "jest"
            },
            "dependencies": {
                "express": "^4.18.0",
                "lodash": "^4.17.21"
            },
            "devDependencies": {
                "jest": "^29.0.0",
                "@types/node": "^18.0.0"
            },
            "keywords": ["web", "api"],
            "license": "MIT"
        }
        """;

        var packageXmlContent = """
        <?xml version="1.0" encoding="utf-8"?>
        <packages>
            <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net9.0" />
            <package id="Microsoft.AspNetCore.Mvc" version="8.0.0" targetFramework="net9.0" />
        </packages>
        """;

        SetupHttpClientMock("https://example.com/package.json", packageJsonContent);
        SetupHttpClientMock("https://example.com/packages.config", packageXmlContent);
        SetupHttpClientMock("https://example.com/requirements.txt", "requests==2.28.0\nflask==2.1.0");

        // Act
        var result = await _analyzer.AnalyzeFromWebsiteAsync("https://example.com");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PackagesFound.Count > 0);

        var nodeJsPackage = result.PackagesFound.FirstOrDefault(p => p.EcosystemType == PackageEcosystemType.NodeJs);
        Assert.NotNull(nodeJsPackage);
        Assert.Equal("test-project", nodeJsPackage.Name);
        Assert.Equal("1.0.0", nodeJsPackage.Version);
        Assert.Equal(2, nodeJsPackage.Dependencies.Count);
        Assert.Equal(2, nodeJsPackage.DevDependencies.Count);

        // 프레임워크 감지 검증
        Assert.Contains(nodeJsPackage.DetectedFrameworks, f => f.Contains("Express"));
    }

    [Fact]
    public async Task AnalyzeTechStackAsync_WithExpressProject_ShouldDetectWebFramework()
    {
        // Arrange
        var packageMetadata = new PackageMetadata
        {
            Name = "web-api",
            EcosystemType = PackageEcosystemType.NodeJs,
            Dependencies = new Dictionary<string, string>
            {
                { "express", "^4.18.0" },
                { "cors", "^2.8.5" },
                { "helmet", "^6.0.0" }
            },
            DevDependencies = new Dictionary<string, string>
            {
                { "nodemon", "^2.0.20" },
                { "jest", "^29.0.0" }
            }
        };

        // Act
        var result = await _analyzer.AnalyzeTechStackAsync(packageMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProjectType.WebAPI, result.ProjectType);
        Assert.Contains("Express.js", result.PrimaryFrameworks);
        Assert.Contains("Security (helmet, cors)", result.ArchitecturalPatterns);
        Assert.True(result.ModernityScore > 0.7); // Express는 현대적인 프레임워크
    }

    [Fact]
    public async Task EvaluateProjectComplexityAsync_WithLargeProject_ShouldReturnHighComplexity()
    {
        // Arrange
        var packageMetadata = new PackageMetadata
        {
            Name = "enterprise-app",
            EcosystemType = PackageEcosystemType.NodeJs,
            Dependencies = CreateLargeDependencyList(50), // 50개 의존성
            DevDependencies = CreateLargeDependencyList(20), // 20개 개발 의존성
            Keywords = new List<string> { "enterprise", "microservices", "api", "database" }
        };

        // Act
        var result = await _analyzer.EvaluateProjectComplexityAsync(packageMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ComplexityLevel.High, result.OverallComplexity);
        Assert.True(result.DependencyComplexity > 0.7);
        Assert.True(result.ProjectScope > 0.8);
        Assert.Contains("Large dependency footprint", result.ComplexityFactors);
    }

    [Fact]
    public async Task AnalyzeSecurityRisksAsync_WithKnownVulnerabilities_ShouldIdentifyRisks()
    {
        // Arrange
        var packageMetadata = new PackageMetadata
        {
            Name = "vulnerable-app",
            EcosystemType = PackageEcosystemType.NodeJs,
            Dependencies = new Dictionary<string, string>
            {
                { "lodash", "4.17.15" }, // 알려진 취약점이 있는 구버전
                { "express", "4.16.0" }, // 구버전
                { "request", "2.88.0" } // deprecated 패키지
            }
        };

        // Act
        var result = await _analyzer.AnalyzeSecurityRisksAsync(packageMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.VulnerabilityCount > 0);
        Assert.True(result.SecurityScore < 0.8); // 보안 점수가 낮아야 함
        Assert.Contains(result.IdentifiedVulnerabilities, v => v.PackageName == "lodash");
        Assert.Contains("Outdated dependencies", result.SecurityRecommendations);
    }

    [Fact]
    public async Task AnalyzeFromWebsiteAsync_WithUnavailablePackageFiles_ShouldReturnEmptyResult()
    {
        // Arrange
        SetupHttpClientMock("https://example.com/package.json", null, HttpStatusCode.NotFound);
        SetupHttpClientMock("https://example.com/requirements.txt", null, HttpStatusCode.NotFound);
        SetupHttpClientMock("https://example.com/composer.json", null, HttpStatusCode.NotFound);

        // Act
        var result = await _analyzer.AnalyzeFromWebsiteAsync("https://example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.PackagesFound);
        Assert.False(result.AnalysisSuccessful);
    }

    [Fact]
    public async Task AnalyzeFromWebsiteAsync_WithMalformedJson_ShouldHandleGracefully()
    {
        // Arrange
        var malformedJson = "{ invalid json content";
        SetupHttpClientMock("https://example.com/package.json", malformedJson);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _analyzer.AnalyzeFromWebsiteAsync("https://example.com"));

        // 예외가 발생하지 않고 우아하게 처리되어야 함
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(PackageEcosystemType.NodeJs, "package.json")]
    [InlineData(PackageEcosystemType.Python, "requirements.txt")]
    [InlineData(PackageEcosystemType.CSharp, "packages.config")]
    [InlineData(PackageEcosystemType.PHP, "composer.json")]
    [InlineData(PackageEcosystemType.Ruby, "Gemfile")]
    [InlineData(PackageEcosystemType.Go, "go.mod")]
    [InlineData(PackageEcosystemType.Rust, "Cargo.toml")]
    public void GetPackageFileNames_ForEachEcosystem_ShouldReturnCorrectFileNames(
        PackageEcosystemType ecosystem, string expectedFileName)
    {
        // Act
        var fileNames = PackageEcosystemAnalyzer.GetPackageFileNames(ecosystem);

        // Assert
        Assert.Contains(expectedFileName, fileNames);
    }

    [Fact]
    public async Task AnalyzeTechStackAsync_WithMicroservicesProject_ShouldDetectArchitecture()
    {
        // Arrange
        var packageMetadata = new PackageMetadata
        {
            Name = "microservice",
            EcosystemType = PackageEcosystemType.NodeJs,
            Dependencies = new Dictionary<string, string>
            {
                { "express", "^4.18.0" },
                { "consul", "^0.40.0" },
                { "redis", "^4.0.0" },
                { "amqplib", "^0.10.0" }
            },
            Keywords = new List<string> { "microservice", "api", "distributed" }
        };

        // Act
        var result = await _analyzer.AnalyzeTechStackAsync(packageMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Microservices", result.ArchitecturalPatterns);
        Assert.Contains("Message Queue (AMQP)", result.ArchitecturalPatterns);
        Assert.Contains("Service Discovery (Consul)", result.ArchitecturalPatterns);
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

    private Dictionary<string, string> CreateLargeDependencyList(int count)
    {
        var dependencies = new Dictionary<string, string>();
        for (int i = 0; i < count; i++)
        {
            dependencies[$"package-{i}"] = $"^{i % 10 + 1}.0.0";
        }
        return dependencies;
    }
}