using Microsoft.Extensions.Configuration;
using WebFlux.Configuration;
using WebFlux.Core.Models;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Configuration;

/// <summary>
/// WebFluxOptions, ValidationResult, WebFluxOptionsBuilder 단위 테스트
/// 구성 관리 및 검증 로직 검증
/// </summary>
public class WebFluxOptionsTests
{
    #region WebFluxOptions - Constructor and Default Values Tests

    [Fact]
    public void WebFluxOptions_Constructor_ShouldSetDefaultValues()
    {
        // Act
        var options = new WebFluxOptions();

        // Assert
        options.DevelopmentMode.Should().BeFalse();
        options.EnableVerboseLogging.Should().BeFalse();
        options.EnableMetrics.Should().BeTrue();
        options.EnableProfiling.Should().BeFalse();
        options.DefaultUserAgent.Should().Be("WebFlux/1.0 (+https://github.com/webflux/webflux)");
        options.MaxConcurrentRequests.Should().Be(5);
        options.DefaultTimeoutSeconds.Should().Be(30);
        options.Configuration.Should().NotBeNull();
    }

    [Fact]
    public void WebFluxOptions_SectionName_ShouldBeWebFlux()
    {
        // Assert
        WebFluxOptions.SectionName.Should().Be("WebFlux");
    }

    #endregion

    #region WebFluxOptions - Validate Tests

    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnValidResult()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            MaxConcurrentRequests = 10,
            DefaultTimeoutSeconds = 60,
            DefaultUserAgent = "CustomAgent/1.0"
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithZeroMaxConcurrentRequests_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            MaxConcurrentRequests = 0
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("MaxConcurrentRequests must be greater than 0");
    }

    [Fact]
    public void Validate_WithNegativeMaxConcurrentRequests_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            MaxConcurrentRequests = -1
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("MaxConcurrentRequests must be greater than 0");
    }

    [Fact]
    public void Validate_WithZeroDefaultTimeoutSeconds_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            DefaultTimeoutSeconds = 0
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("DefaultTimeoutSeconds must be greater than 0");
    }

    [Fact]
    public void Validate_WithNegativeDefaultTimeoutSeconds_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            DefaultTimeoutSeconds = -1
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("DefaultTimeoutSeconds must be greater than 0");
    }

    [Fact]
    public void Validate_WithEmptyUserAgent_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            DefaultUserAgent = ""
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("DefaultUserAgent cannot be empty");
    }

    [Fact]
    public void Validate_WithNullUserAgent_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            DefaultUserAgent = null!
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("DefaultUserAgent cannot be empty");
    }

    [Fact]
    public void Validate_WithWhitespaceUserAgent_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            DefaultUserAgent = "   "
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("DefaultUserAgent cannot be empty");
    }

    [Fact]
    public void Validate_WithInvalidCrawlingMaxConcurrentRequests_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Crawling.MaxConcurrentRequests = 0;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Crawling.MaxConcurrentRequests must be greater than 0");
    }

    [Fact]
    public void Validate_WithInvalidCrawlingDefaultTimeoutSeconds_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Crawling.DefaultTimeoutSeconds = 0;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Crawling.DefaultTimeoutSeconds must be greater than 0");
    }

    [Fact]
    public void Validate_WithInvalidChunkingDefaultMaxChunkSize_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Chunking.DefaultMaxChunkSize = 0;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Chunking.DefaultMaxChunkSize must be greater than 0");
    }

    [Fact]
    public void Validate_WithInvalidChunkingDefaultMinChunkSize_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Chunking.DefaultMinChunkSize = 0;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Chunking.DefaultMinChunkSize must be greater than 0");
    }

    [Fact]
    public void Validate_WithMaxChunkSizeLessThanMinChunkSize_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Chunking.DefaultMinChunkSize = 1000;
        options.Configuration.Chunking.DefaultMaxChunkSize = 500;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Chunking.DefaultMaxChunkSize must be greater than DefaultMinChunkSize");
    }

    [Fact]
    public void Validate_WithMaxChunkSizeEqualToMinChunkSize_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Chunking.DefaultMinChunkSize = 1000;
        options.Configuration.Chunking.DefaultMaxChunkSize = 1000;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Chunking.DefaultMaxChunkSize must be greater than DefaultMinChunkSize");
    }

    [Fact]
    public void Validate_WithInvalidPerformanceMaxDegreeOfParallelism_ShouldReturnError()
    {
        // Arrange
        var options = new WebFluxOptions();
        options.Configuration.Performance.MaxDegreeOfParallelism = 0;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Performance.MaxDegreeOfParallelism must be greater than 0");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var options = new WebFluxOptions
        {
            MaxConcurrentRequests = 0,
            DefaultTimeoutSeconds = 0,
            DefaultUserAgent = ""
        };
        options.Configuration.Crawling.MaxConcurrentRequests = 0;

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Constructor_ShouldInitializeCollections()
    {
        // Act
        var result = new ValidationResult();

        // Assert
        result.Errors.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().NotBeNull();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_IsValid_WithNoErrors_ShouldBeTrue()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = true
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_IsValid_WithErrors_ShouldBeFalse()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Error 1", "Error 2" }
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void ValidationResult_Warnings_ShouldBeIndependentOfErrors()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = true,
            Warnings = new List<string> { "Warning 1" }
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().HaveCount(1);
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region WebFluxOptionsBuilder - Basic Builder Pattern Tests

    [Fact]
    public void Builder_EnableDevelopmentMode_ShouldSetDevelopmentMode()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableDevelopmentMode(true)
            .Build();

        // Assert
        options.DevelopmentMode.Should().BeTrue();
    }

    [Fact]
    public void Builder_EnableDevelopmentMode_DefaultTrue_ShouldSetDevelopmentMode()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableDevelopmentMode()
            .Build();

        // Assert
        options.DevelopmentMode.Should().BeTrue();
    }

    [Fact]
    public void Builder_EnableVerboseLogging_ShouldSetVerboseLogging()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableVerboseLogging(true)
            .Build();

        // Assert
        options.EnableVerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void Builder_EnableVerboseLogging_DefaultTrue_ShouldSetVerboseLogging()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableVerboseLogging()
            .Build();

        // Assert
        options.EnableVerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void Builder_EnableMetrics_ShouldSetMetrics()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableMetrics(false)
            .Build();

        // Assert
        options.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void Builder_EnableMetrics_DefaultTrue_ShouldSetMetrics()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableMetrics()
            .Build();

        // Assert
        options.EnableMetrics.Should().BeTrue();
    }

    #endregion

    #region WebFluxOptionsBuilder - Configuration Methods Tests

    [Fact]
    public void Builder_ConfigureCrawling_ShouldConfigureCrawlingSettings()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .ConfigureCrawling(crawling =>
            {
                crawling.MaxConcurrentRequests = 20;
                crawling.DefaultTimeoutSeconds = 120;
            })
            .Build();

        // Assert
        options.Configuration.Crawling.MaxConcurrentRequests.Should().Be(20);
        options.Configuration.Crawling.DefaultTimeoutSeconds.Should().Be(120);
    }

    [Fact]
    public void Builder_ConfigureChunking_ShouldConfigureChunkingSettings()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .ConfigureChunking(chunking =>
            {
                chunking.DefaultMaxChunkSize = 2000;
                chunking.DefaultMinChunkSize = 100;
            })
            .Build();

        // Assert
        options.Configuration.Chunking.DefaultMaxChunkSize.Should().Be(2000);
        options.Configuration.Chunking.DefaultMinChunkSize.Should().Be(100);
    }

    [Fact]
    public void Builder_ConfigurePerformance_ShouldConfigurePerformanceSettings()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .ConfigurePerformance(performance =>
            {
                performance.MaxDegreeOfParallelism = 8;
            })
            .Build();

        // Assert
        options.Configuration.Performance.MaxDegreeOfParallelism.Should().Be(8);
    }

    #endregion

    #region WebFluxOptionsBuilder - Fluent Interface Tests

    [Fact]
    public void Builder_FluentInterface_ShouldAllowMethodChaining()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .EnableDevelopmentMode()
            .EnableVerboseLogging()
            .EnableMetrics(false)
            .ConfigureCrawling(c => c.MaxConcurrentRequests = 15)
            .ConfigureChunking(c => c.DefaultMaxChunkSize = 3000)
            .ConfigurePerformance(p => p.MaxDegreeOfParallelism = 4)
            .Build();

        // Assert
        options.DevelopmentMode.Should().BeTrue();
        options.EnableVerboseLogging.Should().BeTrue();
        options.EnableMetrics.Should().BeFalse();
        options.Configuration.Crawling.MaxConcurrentRequests.Should().Be(15);
        options.Configuration.Chunking.DefaultMaxChunkSize.Should().Be(3000);
        options.Configuration.Performance.MaxDegreeOfParallelism.Should().Be(4);
    }

    [Fact]
    public void Builder_MultipleConfigureCalls_ShouldAccumulateChanges()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .ConfigureCrawling(c => c.MaxConcurrentRequests = 10)
            .ConfigureCrawling(c => c.DefaultTimeoutSeconds = 90)
            .Build();

        // Assert
        options.Configuration.Crawling.MaxConcurrentRequests.Should().Be(10);
        options.Configuration.Crawling.DefaultTimeoutSeconds.Should().Be(90);
    }

    #endregion

    #region WebFluxOptionsBuilder - LoadFromConfiguration Tests

    [Fact]
    public void Builder_LoadFromConfiguration_ShouldLoadSettings()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["WebFlux:DevelopmentMode"] = "true",
            ["WebFlux:EnableVerboseLogging"] = "true",
            ["WebFlux:MaxConcurrentRequests"] = "15",
            ["WebFlux:DefaultTimeoutSeconds"] = "60"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .LoadFromConfiguration(configuration)
            .Build();

        // Assert
        options.DevelopmentMode.Should().BeTrue();
        options.EnableVerboseLogging.Should().BeTrue();
        options.MaxConcurrentRequests.Should().Be(15);
        options.DefaultTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void Builder_LoadFromConfiguration_WithCustomSectionName_ShouldLoadSettings()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["CustomSection:DevelopmentMode"] = "true",
            ["CustomSection:MaxConcurrentRequests"] = "20"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .LoadFromConfiguration(configuration, "CustomSection")
            .Build();

        // Assert
        options.DevelopmentMode.Should().BeTrue();
        options.MaxConcurrentRequests.Should().Be(20);
    }

    [Fact]
    public void Builder_LoadFromConfiguration_ThenOverride_ShouldUseOverriddenValues()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["WebFlux:MaxConcurrentRequests"] = "10"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder
            .LoadFromConfiguration(configuration)
            .ConfigureCrawling(c => c.MaxConcurrentRequests = 25)
            .Build();

        // Assert - Builder override should take precedence
        options.Configuration.Crawling.MaxConcurrentRequests.Should().Be(25);
    }

    #endregion

    #region WebFluxOptionsBuilder - Build Validation Tests

    [Fact]
    public void Builder_Build_WithValidConfiguration_ShouldReturnOptions()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();

        // Act
        var options = builder.Build();

        // Assert
        options.Should().NotBeNull();
        options.Should().BeOfType<WebFluxOptions>();
    }

    [Fact]
    public void Builder_Build_WithInvalidConfiguration_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();
        builder.ConfigureCrawling(c => c.MaxConcurrentRequests = 0);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        exception.Message.Should().Contain("Invalid WebFlux configuration");
        exception.Message.Should().Contain("Crawling.MaxConcurrentRequests must be greater than 0");
    }

    [Fact]
    public void Builder_Build_WithMultipleValidationErrors_ShouldThrowWithAllErrors()
    {
        // Arrange
        var builder = new WebFluxOptionsBuilder();
        builder.ConfigureCrawling(c =>
        {
            c.MaxConcurrentRequests = 0;
            c.DefaultTimeoutSeconds = -1;
        });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        exception.Message.Should().Contain("Crawling.MaxConcurrentRequests must be greater than 0");
        exception.Message.Should().Contain("Crawling.DefaultTimeoutSeconds must be greater than 0");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void WebFluxOptions_CompleteWorkflow_ShouldWork()
    {
        // Arrange & Act
        var options = new WebFluxOptionsBuilder()
            .EnableDevelopmentMode()
            .EnableVerboseLogging()
            .ConfigureCrawling(c =>
            {
                c.MaxConcurrentRequests = 10;
                c.DefaultTimeoutSeconds = 60;
            })
            .ConfigureChunking(c =>
            {
                c.DefaultMaxChunkSize = 2000;
                c.DefaultMinChunkSize = 200;
            })
            .ConfigurePerformance(p =>
            {
                p.MaxDegreeOfParallelism = 4;
            })
            .Build();

        // Assert
        var validation = options.Validate();
        validation.IsValid.Should().BeTrue();
        options.DevelopmentMode.Should().BeTrue();
        options.EnableVerboseLogging.Should().BeTrue();
    }

    [Fact]
    public void WebFluxOptions_FromConfiguration_CompleteWorkflow_ShouldWork()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["WebFlux:DevelopmentMode"] = "true",
            ["WebFlux:EnableVerboseLogging"] = "false",
            ["WebFlux:EnableMetrics"] = "true",
            ["WebFlux:MaxConcurrentRequests"] = "8",
            ["WebFlux:DefaultTimeoutSeconds"] = "45"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var options = new WebFluxOptionsBuilder()
            .LoadFromConfiguration(configuration)
            .Build();

        // Assert
        var validation = options.Validate();
        validation.IsValid.Should().BeTrue();
        options.DevelopmentMode.Should().BeTrue();
        options.EnableVerboseLogging.Should().BeFalse();
        options.MaxConcurrentRequests.Should().Be(8);
        options.DefaultTimeoutSeconds.Should().Be(45);
    }

    #endregion
}
