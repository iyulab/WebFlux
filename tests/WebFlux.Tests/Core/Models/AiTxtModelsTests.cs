using WebFlux.Core.Models;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Models;

/// <summary>
/// AI.txt 관련 모델 단위 테스트
/// Core AI models 검증
/// </summary>
public class AiTxtModelsTests
{
    #region AiTxtParseResult Tests

    [Fact]
    public void AiTxtParseResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new AiTxtParseResult();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.FileFound.Should().BeFalse();
        result.AiTxtUrl.Should().Be(string.Empty);
        result.Metadata.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.RawContent.Should().BeNull();
        result.ParsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AiTxtParseResult_SuccessScenario_ShouldHaveCorrectState()
    {
        // Arrange & Act
        var result = new AiTxtParseResult
        {
            IsSuccess = true,
            FileFound = true,
            AiTxtUrl = "https://example.com/ai.txt",
            Metadata = new AiTxtMetadata(),
            RawContent = "# ai.txt content"
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.FileFound.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void AiTxtParseResult_FailureScenario_ShouldHaveErrorMessage()
    {
        // Arrange & Act
        var result = new AiTxtParseResult
        {
            IsSuccess = false,
            FileFound = false,
            ErrorMessage = "File not found"
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("File not found");
        result.Metadata.Should().BeNull();
    }

    #endregion

    #region AiTxtMetadata Tests

    [Fact]
    public void AiTxtMetadata_ShouldInitializeWithDefaults()
    {
        // Act
        var metadata = new AiTxtMetadata();

        // Assert
        metadata.BaseUrl.Should().Be(string.Empty);
        metadata.Version.Should().Be("1.0");
        metadata.Owner.Should().BeNull();
        metadata.AgentPermissions.Should().NotBeNull().And.BeEmpty();
        metadata.DefaultPermissions.Should().BeNull();
        metadata.ContentLicenses.Should().NotBeNull().And.BeEmpty();
        metadata.DataUsagePolicies.Should().NotBeNull().And.BeEmpty();
        metadata.AdditionalMetadata.Should().NotBeNull().And.BeEmpty();
        metadata.ParsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AiTxtMetadata_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var metadata = new AiTxtMetadata
        {
            BaseUrl = "https://example.com",
            Version = "2.0",
            DefaultPermissions = new AiAgentPermissions(),
            CrawlingGuidelines = new AiCrawlingGuidelines(),
            LastUpdated = DateTimeOffset.UtcNow.AddDays(-7)
        };

        // Assert
        metadata.BaseUrl.Should().Be("https://example.com");
        metadata.Version.Should().Be("2.0");
        metadata.DefaultPermissions.Should().NotBeNull();
        metadata.CrawlingGuidelines.Should().NotBeNull();
    }

    #endregion

    #region AiTxtStatistics Tests

    [Fact]
    public void AiTxtStatistics_ShouldInitializeWithDefaults()
    {
        // Act
        var stats = new AiTxtStatistics();

        // Assert
        stats.TotalParseAttempts.Should().Be(0);
        stats.SuccessfulParses.Should().Be(0);
        stats.SitesWithAiTxt.Should().Be(0);
        stats.AverageParseTime.Should().Be(0);
        stats.CommonErrors.Should().NotBeNull().And.BeEmpty();
        stats.VersionStatistics.Should().NotBeNull().And.BeEmpty();
        stats.LastUpdated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AiTxtStatistics_ShouldCalculateSuccessRate()
    {
        // Arrange
        var stats = new AiTxtStatistics
        {
            TotalParseAttempts = 100,
            SuccessfulParses = 85
        };

        // Act
        var successRate = stats.SuccessfulParses / (double)stats.TotalParseAttempts;

        // Assert
        successRate.Should().BeApproximately(0.85, 0.001);
    }

    [Fact]
    public void AiTxtStatistics_WithRealisticData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var stats = new AiTxtStatistics
        {
            TotalParseAttempts = 1000,
            SuccessfulParses = 750,
            SitesWithAiTxt = 600,
            AverageParseTime = 45.5,
            CommonErrors = new Dictionary<string, int>
            {
                { "FileNotFound", 150 },
                { "ParseError", 80 },
                { "NetworkTimeout", 20 }
            },
            VersionStatistics = new Dictionary<string, int>
            {
                { "1.0", 500 },
                { "2.0", 250 }
            }
        };

        // Assert
        stats.TotalParseAttempts.Should().Be(1000);
        stats.SuccessfulParses.Should().Be(750);
        stats.CommonErrors.Should().HaveCount(3);
        stats.VersionStatistics.Should().HaveCount(2);
    }

    #endregion

    #region AiAgentPermissions Tests

    [Fact]
    public void AiAgentPermissions_ShouldInitializeWithDefaults()
    {
        // Act
        var permissions = new AiAgentPermissions();

        // Assert
        permissions.AgentPattern.Should().Be("*");
        permissions.AllowedActions.Should().NotBeNull().And.BeEmpty();
        permissions.DisallowedActions.Should().NotBeNull().And.BeEmpty();
        permissions.AllowedPaths.Should().NotBeNull().And.BeEmpty();
        permissions.DisallowedPaths.Should().NotBeNull().And.BeEmpty();
        permissions.UsageLimits.Should().BeNull();
        permissions.TimeWindows.Should().NotBeNull().And.BeEmpty();
        permissions.AdditionalConditions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AiAgentPermissions_ShouldAllowConfiguration()
    {
        // Arrange & Act
        var permissions = new AiAgentPermissions
        {
            AgentPattern = "GPTBot/*",
            AllowedActions = new List<AiAction> { AiAction.Read, AiAction.Index },
            DisallowedActions = new List<AiAction> { AiAction.Training },
            AllowedPaths = new List<string> { "/public/*", "/api/*" },
            DisallowedPaths = new List<string> { "/private/*" },
            UsageLimits = new AiUsageLimits { MaxRequestsPerHour = 100 }
        };

        // Assert
        permissions.AgentPattern.Should().Be("GPTBot/*");
        permissions.AllowedActions.Should().HaveCount(2);
        permissions.DisallowedActions.Should().HaveCount(1);
        permissions.UsageLimits.Should().NotBeNull();
    }

    #endregion

    #region AiUsageLimits Tests

    [Fact]
    public void AiUsageLimits_ShouldInitializeWithNullDefaults()
    {
        // Act
        var limits = new AiUsageLimits();

        // Assert
        limits.MaxRequestsPerHour.Should().BeNull();
        limits.MaxRequestsPerDay.Should().BeNull();
        limits.MaxConcurrentConnections.Should().BeNull();
        limits.MaxDataTransferPerDay.Should().BeNull();
        limits.MinDelayBetweenRequests.Should().BeNull();
        limits.MaxSessionDurationMinutes.Should().BeNull();
        limits.QuotaResetTimezone.Should().BeNull();
    }

    [Fact]
    public void AiUsageLimits_ShouldAllowLimitConfiguration()
    {
        // Arrange & Act
        var limits = new AiUsageLimits
        {
            MaxRequestsPerHour = 100,
            MaxRequestsPerDay = 2000,
            MaxConcurrentConnections = 5,
            MaxDataTransferPerDay = 1024 * 1024 * 100, // 100MB
            MinDelayBetweenRequests = 100, // 100ms
            MaxSessionDurationMinutes = 60,
            QuotaResetTimezone = "UTC"
        };

        // Assert
        limits.MaxRequestsPerHour.Should().Be(100);
        limits.MaxRequestsPerDay.Should().Be(2000);
        limits.MaxConcurrentConnections.Should().Be(5);
        limits.MinDelayBetweenRequests.Should().Be(100);
    }

    #endregion

    #region AiCrawlingGuidelines Tests

    [Fact]
    public void AiCrawlingGuidelines_ShouldInitializeWithDefaults()
    {
        // Act
        var guidelines = new AiCrawlingGuidelines();

        // Assert
        guidelines.RecommendedRate.Should().BeNull();
        guidelines.MaxConcurrentConnections.Should().BeNull();
        guidelines.AllowedTimeWindows.Should().NotBeNull().And.BeEmpty();
        guidelines.UserAgentRequirements.Should().NotBeNull().And.BeEmpty();
        guidelines.CrawlPriorities.Should().NotBeNull().And.BeEmpty();
        guidelines.ExcludePaths.Should().NotBeNull().And.BeEmpty();
        guidelines.IncludePaths.Should().NotBeNull().And.BeEmpty();
        guidelines.LoggingRequirements.Should().BeNull();
    }

    [Fact]
    public void AiCrawlingGuidelines_ShouldAllowConfiguration()
    {
        // Arrange & Act
        var guidelines = new AiCrawlingGuidelines
        {
            RecommendedRate = 10.0,
            MaxConcurrentConnections = 3,
            UserAgentRequirements = new List<string> { "Must include contact info" },
            ExcludePaths = new List<string> { "/admin/*", "/private/*" },
            IncludePaths = new List<string> { "/public/*", "/api/*" }
        };

        // Assert
        guidelines.RecommendedRate.Should().Be(10.0);
        guidelines.MaxConcurrentConnections.Should().Be(3);
        guidelines.ExcludePaths.Should().HaveCount(2);
        guidelines.IncludePaths.Should().HaveCount(2);
    }

    #endregion

    #region AccessRestriction Tests

    [Fact]
    public void AccessRestriction_ShouldInitializeWithDefaults()
    {
        // Act
        var restriction = new AccessRestriction();

        // Assert
        restriction.RequireAuthentication.Should().BeFalse();
        restriction.AllowedRoles.Should().NotBeNull().And.BeEmpty();
        restriction.RestrictedCountries.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void AccessRestriction_ShouldAllowConfiguration()
    {
        // Arrange & Act
        var restriction = new AccessRestriction
        {
            RequireAuthentication = true,
            AllowedRoles = new List<string> { "admin", "editor" },
            RestrictedCountries = new List<string> { "XX", "YY" }
        };

        // Assert
        restriction.RequireAuthentication.Should().BeTrue();
        restriction.AllowedRoles.Should().HaveCount(2);
        restriction.RestrictedCountries.Should().HaveCount(2);
    }

    #endregion

    #region ApiEndpoint Tests

    [Fact]
    public void ApiEndpoint_ShouldInitializeWithDefaults()
    {
        // Act
        var endpoint = new ApiEndpoint();

        // Assert
        endpoint.Name.Should().Be(string.Empty);
        endpoint.Path.Should().Be(string.Empty);
        endpoint.Methods.Should().NotBeNull().And.BeEmpty();
        endpoint.Description.Should().BeNull();
        endpoint.RequiresAuthentication.Should().BeFalse();
        endpoint.UsageLimits.Should().BeNull();
        endpoint.SupportedContentTypes.Should().NotBeNull().And.BeEmpty();
        endpoint.ExampleRequest.Should().BeNull();
        endpoint.ExampleResponse.Should().BeNull();
    }

    [Fact]
    public void ApiEndpoint_ShouldAllowConfiguration()
    {
        // Arrange & Act
        var endpoint = new ApiEndpoint
        {
            Name = "Get Content",
            Path = "/api/v1/content",
            Methods = new List<string> { "GET", "POST" },
            Description = "Retrieve content data",
            RequiresAuthentication = true,
            UsageLimits = new AiUsageLimits { MaxRequestsPerHour = 50 },
            SupportedContentTypes = new List<string> { "application/json", "text/plain" },
            ExampleRequest = "GET /api/v1/content?id=123",
            ExampleResponse = "{\"id\": 123, \"content\": \"...\"}"
        };

        // Assert
        endpoint.Name.Should().Be("Get Content");
        endpoint.Path.Should().Be("/api/v1/content");
        endpoint.Methods.Should().HaveCount(2);
        endpoint.RequiresAuthentication.Should().BeTrue();
        endpoint.UsageLimits.Should().NotBeNull();
    }

    #endregion

    #region TimeWindow Tests

    [Fact]
    public void TimeWindow_IsCurrentTimeAllowed_WithinRange_ShouldReturnTrue()
    {
        // Arrange
        var window = new TimeWindow
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };
        var testTime = DateTimeOffset.UtcNow.Date.AddHours(12);

        // Act
        var isAllowed = window.IsCurrentTimeAllowed(testTime);

        // Assert
        isAllowed.Should().BeTrue();
    }

    [Fact]
    public void TimeWindow_IsCurrentTimeAllowed_OutsideRange_ShouldReturnFalse()
    {
        // Arrange
        var window = new TimeWindow
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };
        var testTime = DateTimeOffset.UtcNow.Date.AddHours(20);

        // Act
        var isAllowed = window.IsCurrentTimeAllowed(testTime);

        // Assert
        isAllowed.Should().BeFalse();
    }

    [Fact]
    public void TimeWindow_IsCurrentTimeAllowed_CrossMidnight_ShouldWork()
    {
        // Arrange - 22:00 to 06:00 (crosses midnight)
        var window = new TimeWindow
        {
            StartTime = TimeSpan.FromHours(22),
            EndTime = TimeSpan.FromHours(6)
        };

        // Test at 23:00 (should be allowed)
        var testTime1 = DateTimeOffset.UtcNow.Date.AddHours(23);
        var isAllowed1 = window.IsCurrentTimeAllowed(testTime1);

        // Test at 03:00 (should be allowed)
        var testTime2 = DateTimeOffset.UtcNow.Date.AddHours(3);
        var isAllowed2 = window.IsCurrentTimeAllowed(testTime2);

        // Test at 12:00 (should not be allowed)
        var testTime3 = DateTimeOffset.UtcNow.Date.AddHours(12);
        var isAllowed3 = window.IsCurrentTimeAllowed(testTime3);

        // Assert
        isAllowed1.Should().BeTrue();
        isAllowed2.Should().BeTrue();
        isAllowed3.Should().BeFalse();
    }

    [Fact]
    public void TimeWindow_IsCurrentTimeAllowed_WithDaysOfWeek_ShouldFilterByDay()
    {
        // Arrange - Only Monday and Wednesday
        var window = new TimeWindow
        {
            StartTime = TimeSpan.FromHours(0),
            EndTime = new TimeSpan(23, 59, 59),
            DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday }
        };

        // Find a Monday
        var monday = DateTimeOffset.UtcNow.Date;
        while (monday.DayOfWeek != DayOfWeek.Monday)
        {
            monday = monday.AddDays(1);
        }

        var tuesday = monday.AddDays(1);

        // Act
        var mondayAllowed = window.IsCurrentTimeAllowed(monday.AddHours(12));
        var tuesdayAllowed = window.IsCurrentTimeAllowed(tuesday.AddHours(12));

        // Assert
        mondayAllowed.Should().BeTrue();
        tuesdayAllowed.Should().BeFalse();
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void AiAction_ShouldHaveAllValues()
    {
        // Assert
        Enum.IsDefined<AiAction>(AiAction.Read).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Index).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Training).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.FineTuning).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Inference).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Analysis).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Summarization).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Translation).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Search).Should().BeTrue();
        Enum.IsDefined<AiAction>(AiAction.Caching).Should().BeTrue();
    }

    [Fact]
    public void OrganizationType_ShouldHaveAllValues()
    {
        // Assert
        Enum.IsDefined<OrganizationType>(OrganizationType.Individual).Should().BeTrue();
        Enum.IsDefined<OrganizationType>(OrganizationType.Corporation).Should().BeTrue();
        Enum.IsDefined<OrganizationType>(OrganizationType.NonProfit).Should().BeTrue();
        Enum.IsDefined<OrganizationType>(OrganizationType.Government).Should().BeTrue();
        Enum.IsDefined<OrganizationType>(OrganizationType.Educational).Should().BeTrue();
        Enum.IsDefined<OrganizationType>(OrganizationType.Research).Should().BeTrue();
        Enum.IsDefined<OrganizationType>(OrganizationType.Other).Should().BeTrue();
    }

    [Fact]
    public void LicenseType_ShouldHaveAllValues()
    {
        // Assert
        Enum.IsDefined<LicenseType>(LicenseType.Proprietary).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.CreativeCommons).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.MIT).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.GPL).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.Apache).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.BSD).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.PublicDomain).Should().BeTrue();
        Enum.IsDefined<LicenseType>(LicenseType.Custom).Should().BeTrue();
    }

    [Fact]
    public void DataUsageType_ShouldHaveAllValues()
    {
        // Assert
        Enum.IsDefined<DataUsageType>(DataUsageType.Training).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.FineTuning).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.Inference).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.Research).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.Commercial).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.Personal).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.Analytics).Should().BeTrue();
        Enum.IsDefined<DataUsageType>(DataUsageType.Caching).Should().BeTrue();
    }

    #endregion
}
