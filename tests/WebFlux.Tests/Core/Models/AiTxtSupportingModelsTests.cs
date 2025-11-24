using WebFlux.Core.Models;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Core.Models;

/// <summary>
/// AI.txt 지원 모델 단위 테스트
/// Supporting AI models 검증
/// </summary>
public class AiTxtSupportingModelsTests
{
    #region SiteOwnerInfo Tests

    [Fact]
    public void SiteOwnerInfo_ShouldInitializeWithDefaults()
    {
        // Act
        var owner = new SiteOwnerInfo();

        // Assert
        owner.Name.Should().Be(string.Empty);
        owner.Email.Should().BeNull();
        owner.Website.Should().BeNull();
        owner.OrganizationType.Should().BeNull();
        owner.Country.Should().BeNull();
        owner.LegalEntity.Should().BeNull();
        owner.SocialMedia.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SiteOwnerInfo_ShouldAllowPropertyAssignment()
    {
        // Arrange & Act
        var owner = new SiteOwnerInfo
        {
            Name = "Example Corp",
            Email = "contact@example.com",
            Website = "https://example.com",
            OrganizationType = OrganizationType.Corporation,
            Country = "South Korea",
            LegalEntity = "Example Corp. Ltd."
        };

        // Assert
        owner.Name.Should().Be("Example Corp");
        owner.Email.Should().Be("contact@example.com");
        owner.Website.Should().Be("https://example.com");
        owner.OrganizationType.Should().Be(OrganizationType.Corporation);
        owner.Country.Should().Be("South Korea");
        owner.LegalEntity.Should().Be("Example Corp. Ltd.");
    }

    [Fact]
    public void SiteOwnerInfo_SocialMedia_ShouldAllowMultipleEntries()
    {
        // Arrange
        var owner = new SiteOwnerInfo();

        // Act
        owner.SocialMedia["twitter"] = "https://twitter.com/example";
        owner.SocialMedia["linkedin"] = "https://linkedin.com/company/example";
        owner.SocialMedia["github"] = "https://github.com/example";

        // Assert
        owner.SocialMedia.Should().HaveCount(3);
        owner.SocialMedia["twitter"].Should().Be("https://twitter.com/example");
    }

    #endregion

    #region ContentLicense Tests

    [Fact]
    public void ContentLicense_ShouldInitializeWithDefaults()
    {
        // Act
        var license = new ContentLicense();

        // Assert
        license.Name.Should().Be(string.Empty);
        license.Url.Should().BeNull();
        license.ContentPatterns.Should().NotBeNull().And.BeEmpty();
        license.Type.Should().Be(default(LicenseType));
        license.AllowCommercialUse.Should().BeFalse();
        license.AllowModification.Should().BeFalse();
        license.AllowRedistribution.Should().BeFalse();
        license.RequireAttribution.Should().BeFalse();
        license.Conditions.Should().NotBeNull().And.BeEmpty();
        license.Limitations.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ContentLicense_MIT_ShouldAllowAllPermissions()
    {
        // Arrange & Act
        var license = new ContentLicense
        {
            Name = "MIT License",
            Url = "https://opensource.org/licenses/MIT",
            Type = LicenseType.MIT,
            AllowCommercialUse = true,
            AllowModification = true,
            AllowRedistribution = true,
            RequireAttribution = true
        };

        // Assert
        license.AllowCommercialUse.Should().BeTrue();
        license.AllowModification.Should().BeTrue();
        license.AllowRedistribution.Should().BeTrue();
        license.RequireAttribution.Should().BeTrue();
    }

    [Fact]
    public void ContentLicense_Proprietary_ShouldRestrictAllPermissions()
    {
        // Arrange & Act
        var license = new ContentLicense
        {
            Name = "Proprietary License",
            Type = LicenseType.Proprietary,
            AllowCommercialUse = false,
            AllowModification = false,
            AllowRedistribution = false,
            RequireAttribution = true,
            Limitations = new List<string> { "No commercial use", "No modifications" }
        };

        // Assert
        license.AllowCommercialUse.Should().BeFalse();
        license.AllowModification.Should().BeFalse();
        license.AllowRedistribution.Should().BeFalse();
        license.Limitations.Should().HaveCount(2);
    }

    #endregion

    #region DataUsagePolicy Tests

    [Fact]
    public void DataUsagePolicy_ShouldInitializeWithDefaults()
    {
        // Act
        var policy = new DataUsagePolicy();

        // Assert
        policy.Name.Should().Be(string.Empty);
        policy.UsageType.Should().Be(default(DataUsageType));
        policy.IsAllowed.Should().BeFalse();
        policy.ContentPatterns.Should().NotBeNull().And.BeEmpty();
        policy.Conditions.Should().NotBeNull().And.BeEmpty();
        policy.DataRetentionPeriod.Should().BeNull();
        policy.RequireAnonymization.Should().BeFalse();
        policy.RequireConsent.Should().BeFalse();
        policy.Description.Should().BeNull();
    }

    [Fact]
    public void DataUsagePolicy_Training_ShouldRequireStrictConditions()
    {
        // Arrange & Act
        var policy = new DataUsagePolicy
        {
            Name = "AI Training Policy",
            UsageType = DataUsageType.Training,
            IsAllowed = true,
            ContentPatterns = new List<string> { "/public/*" },
            Conditions = new List<string> { "Attribution required", "No PII" },
            DataRetentionPeriod = TimeSpan.FromDays(90),
            RequireAnonymization = true,
            RequireConsent = true,
            Description = "Data may be used for AI training with strict privacy controls"
        };

        // Assert
        policy.UsageType.Should().Be(DataUsageType.Training);
        policy.IsAllowed.Should().BeTrue();
        policy.RequireAnonymization.Should().BeTrue();
        policy.RequireConsent.Should().BeTrue();
        policy.DataRetentionPeriod.Should().Be(TimeSpan.FromDays(90));
        policy.Conditions.Should().HaveCount(2);
    }

    [Fact]
    public void DataUsagePolicy_Commercial_ShouldHaveAppropriateRestrictions()
    {
        // Arrange & Act
        var policy = new DataUsagePolicy
        {
            Name = "Commercial Use Policy",
            UsageType = DataUsageType.Commercial,
            IsAllowed = false,
            Conditions = new List<string> { "Contact for licensing" },
            Description = "Commercial use requires separate agreement"
        };

        // Assert
        policy.UsageType.Should().Be(DataUsageType.Commercial);
        policy.IsAllowed.Should().BeFalse();
        policy.Description.Should().Contain("agreement");
    }

    #endregion

    #region ContactInfo Tests

    [Fact]
    public void ContactInfo_ShouldInitializeWithDefaults()
    {
        // Act
        var contact = new ContactInfo();

        // Assert
        contact.Email.Should().BeNull();
        contact.Phone.Should().BeNull();
        contact.Website.Should().BeNull();
        contact.SocialMedia.Should().NotBeNull().And.BeEmpty();
        contact.OtherContacts.Should().NotBeNull().And.BeEmpty();
        contact.PreferredContactMethod.Should().BeNull();
        contact.ResponseTimeHours.Should().BeNull();
        contact.Languages.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ContactInfo_ShouldAllowMultipleContactMethods()
    {
        // Arrange & Act
        var contact = new ContactInfo
        {
            Email = "support@example.com",
            Phone = "+82-2-1234-5678",
            Website = "https://example.com/contact",
            PreferredContactMethod = "email",
            ResponseTimeHours = 24,
            Languages = new List<string> { "ko", "en" }
        };

        // Assert
        contact.Email.Should().Be("support@example.com");
        contact.Phone.Should().Be("+82-2-1234-5678");
        contact.PreferredContactMethod.Should().Be("email");
        contact.ResponseTimeHours.Should().Be(24);
        contact.Languages.Should().HaveCount(2);
    }

    [Fact]
    public void ContactInfo_SocialMedia_ShouldSupportMultiplePlatforms()
    {
        // Arrange
        var contact = new ContactInfo();

        // Act
        contact.SocialMedia["twitter"] = "@example";
        contact.SocialMedia["slack"] = "example.slack.com";
        contact.OtherContacts["discord"] = "example#1234";

        // Assert
        contact.SocialMedia.Should().HaveCount(2);
        contact.OtherContacts.Should().HaveCount(1);
    }

    #endregion

    #region SupportedAiModel Tests

    [Fact]
    public void SupportedAiModel_ShouldInitializeWithDefaults()
    {
        // Act
        var model = new SupportedAiModel();

        // Assert
        model.Name.Should().Be(string.Empty);
        model.Version.Should().BeNull();
        model.Provider.Should().BeNull();
        model.SupportedTasks.Should().NotBeNull().And.BeEmpty();
        model.Description.Should().BeNull();
        model.ApiEndpoint.Should().BeNull();
        model.AuthenticationMethod.Should().BeNull();
        model.UsageLimits.Should().BeNull();
    }

    [Fact]
    public void SupportedAiModel_GPT_ShouldHaveCompleteConfiguration()
    {
        // Arrange & Act
        var model = new SupportedAiModel
        {
            Name = "GPT-4",
            Version = "4.0",
            Provider = "OpenAI",
            SupportedTasks = new List<string> { "chat", "completion", "analysis" },
            Description = "Large multimodal model",
            ApiEndpoint = "https://api.openai.com/v1/chat/completions",
            AuthenticationMethod = "Bearer Token",
            UsageLimits = new AiUsageLimits { MaxRequestsPerHour = 100 }
        };

        // Assert
        model.Name.Should().Be("GPT-4");
        model.Provider.Should().Be("OpenAI");
        model.SupportedTasks.Should().HaveCount(3);
        model.SupportedTasks.Should().Contain("chat");
        model.UsageLimits.Should().NotBeNull();
    }

    [Fact]
    public void SupportedAiModel_CustomModel_ShouldAllowFlexibleConfiguration()
    {
        // Arrange & Act
        var model = new SupportedAiModel
        {
            Name = "Custom Model",
            SupportedTasks = new List<string> { "embedding", "classification" },
            ApiEndpoint = "https://custom.example.com/api"
        };

        // Assert
        model.Name.Should().Be("Custom Model");
        model.SupportedTasks.Should().HaveCount(2);
        model.Provider.Should().BeNull();
    }

    #endregion

    #region ContentCategory Tests

    [Fact]
    public void ContentCategory_ShouldInitializeWithDefaults()
    {
        // Act
        var category = new ContentCategory();

        // Assert
        category.Name.Should().Be(string.Empty);
        category.PathPatterns.Should().NotBeNull().And.BeEmpty();
        category.Description.Should().BeNull();
        category.ContentTypes.Should().NotBeNull().And.BeEmpty();
        category.Tags.Should().NotBeNull().And.BeEmpty();
        category.Priority.Should().Be(5);
        category.AccessRestriction.Should().BeNull();
    }

    [Fact]
    public void ContentCategory_Blog_ShouldHaveAppropriateConfiguration()
    {
        // Arrange & Act
        var category = new ContentCategory
        {
            Name = "Blog Posts",
            PathPatterns = new List<string> { "/blog/*", "/posts/*" },
            Description = "Public blog articles",
            ContentTypes = new List<string> { "text/html", "application/json" },
            Tags = new List<string> { "public", "blog", "articles" },
            Priority = 8
        };

        // Assert
        category.Name.Should().Be("Blog Posts");
        category.PathPatterns.Should().HaveCount(2);
        category.ContentTypes.Should().Contain("text/html");
        category.Tags.Should().HaveCount(3);
        category.Priority.Should().Be(8);
    }

    [Fact]
    public void ContentCategory_WithAccessRestriction_ShouldEnforceRestrictions()
    {
        // Arrange & Act
        var category = new ContentCategory
        {
            Name = "Premium Content",
            PathPatterns = new List<string> { "/premium/*" },
            Priority = 10,
            AccessRestriction = new AccessRestriction
            {
                RequireAuthentication = true,
                AllowedRoles = new List<string> { "premium", "admin" }
            }
        };

        // Assert
        category.AccessRestriction.Should().NotBeNull();
        category.AccessRestriction!.RequireAuthentication.Should().BeTrue();
        category.AccessRestriction.AllowedRoles.Should().HaveCount(2);
    }

    #endregion

    #region EthicsGuidelines Tests

    [Fact]
    public void EthicsGuidelines_ShouldInitializeWithDefaults()
    {
        // Act
        var guidelines = new EthicsGuidelines();

        // Assert
        guidelines.Version.Should().Be("1.0");
        guidelines.Principles.Should().NotBeNull().And.BeEmpty();
        guidelines.ProhibitedUseCases.Should().NotBeNull().And.BeEmpty();
        guidelines.RequiredConsiderations.Should().NotBeNull().And.BeEmpty();
        guidelines.BiasMitigationRequirements.Should().NotBeNull().And.BeEmpty();
        guidelines.TransparencyRequirements.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EthicsGuidelines_ShouldEnforceEthicalStandards()
    {
        // Arrange & Act
        var guidelines = new EthicsGuidelines
        {
            Version = "2.0",
            ProhibitedUseCases = new List<string>
            {
                "Surveillance without consent",
                "Discriminatory profiling",
                "Manipulation"
            },
            RequiredConsiderations = new List<string>
            {
                "Privacy impact assessment",
                "Bias evaluation"
            },
            BiasMitigationRequirements = new List<string>
            {
                "Regular bias audits",
                "Diverse training data"
            },
            TransparencyRequirements = new List<string>
            {
                "Disclosure of AI usage",
                "Explainable outputs"
            }
        };

        // Assert
        guidelines.ProhibitedUseCases.Should().HaveCount(3);
        guidelines.RequiredConsiderations.Should().HaveCount(2);
        guidelines.BiasMitigationRequirements.Should().HaveCount(2);
        guidelines.TransparencyRequirements.Should().HaveCount(2);
    }

    #endregion

    #region PrivacyPolicy Tests

    [Fact]
    public void PrivacyPolicy_ShouldInitializeWithDefaults()
    {
        // Act
        var policy = new PrivacyPolicy();

        // Assert
        policy.Version.Should().Be("1.0");
        policy.CollectedDataTypes.Should().NotBeNull().And.BeEmpty();
        policy.ProcessingPurposes.Should().NotBeNull().And.BeEmpty();
        policy.RetentionPeriods.Should().NotBeNull().And.BeEmpty();
        policy.ThirdPartySharing.Should().NotBeNull().And.BeEmpty();
        policy.UserRights.Should().NotBeNull().And.BeEmpty();
        policy.PrivacyContact.Should().BeNull();
    }

    [Fact]
    public void PrivacyPolicy_ShouldDefineDataHandlingPractices()
    {
        // Arrange & Act
        var policy = new PrivacyPolicy
        {
            Version = "2.0",
            ProcessingPurposes = new List<string>
            {
                "Service improvement",
                "Analytics",
                "Personalization"
            },
            UserRights = new List<string>
            {
                "Right to access",
                "Right to deletion",
                "Right to portability"
            },
            PrivacyContact = new ContactInfo
            {
                Email = "privacy@example.com"
            }
        };

        policy.RetentionPeriods["user_data"] = TimeSpan.FromDays(365);
        policy.RetentionPeriods["analytics"] = TimeSpan.FromDays(90);

        // Assert
        policy.ProcessingPurposes.Should().HaveCount(3);
        policy.UserRights.Should().HaveCount(3);
        policy.RetentionPeriods.Should().HaveCount(2);
        policy.RetentionPeriods["user_data"].Should().Be(TimeSpan.FromDays(365));
        policy.PrivacyContact.Should().NotBeNull();
    }

    #endregion

    #region SecurityRequirements Tests

    [Fact]
    public void SecurityRequirements_ShouldInitializeWithDefaults()
    {
        // Act
        var requirements = new SecurityRequirements();

        // Assert
        requirements.RequiredEncryption.Should().NotBeNull().And.BeEmpty();
        requirements.AuthenticationRequirements.Should().NotBeNull().And.BeEmpty();
        requirements.AccessControlRequirements.Should().NotBeNull().And.BeEmpty();
        requirements.AuditLogRequirements.Should().BeNull();
        requirements.SecurityContact.Should().BeNull();
    }

    [Fact]
    public void SecurityRequirements_ShouldEnforceStrictSecurity()
    {
        // Arrange & Act
        var requirements = new SecurityRequirements
        {
            RequiredEncryption = new List<string>
            {
                "TLS 1.3",
                "AES-256"
            },
            AuthenticationRequirements = new List<string>
            {
                "OAuth 2.0",
                "Multi-factor authentication"
            },
            AccessControlRequirements = new List<string>
            {
                "Role-based access control",
                "IP whitelisting"
            },
            SecurityContact = new ContactInfo
            {
                Email = "security@example.com",
                ResponseTimeHours = 4
            }
        };

        // Assert
        requirements.RequiredEncryption.Should().HaveCount(2);
        requirements.AuthenticationRequirements.Should().HaveCount(2);
        requirements.AccessControlRequirements.Should().HaveCount(2);
        requirements.SecurityContact.Should().NotBeNull();
        requirements.SecurityContact!.ResponseTimeHours.Should().Be(4);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AllSupportingModels_ShouldBeIndependentlyInstantiable()
    {
        // Act
        var owner = new SiteOwnerInfo();
        var license = new ContentLicense();
        var policy = new DataUsagePolicy();
        var contact = new ContactInfo();
        var model = new SupportedAiModel();
        var category = new ContentCategory();
        var ethics = new EthicsGuidelines();
        var privacy = new PrivacyPolicy();
        var security = new SecurityRequirements();

        // Assert
        owner.Should().NotBeNull();
        license.Should().NotBeNull();
        policy.Should().NotBeNull();
        contact.Should().NotBeNull();
        model.Should().NotBeNull();
        category.Should().NotBeNull();
        ethics.Should().NotBeNull();
        privacy.Should().NotBeNull();
        security.Should().NotBeNull();
    }

    #endregion
}
