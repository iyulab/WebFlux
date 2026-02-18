using FluentAssertions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Models;

public class RobotsTxtParseResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var result = new RobotsTxtParseResult();

        result.IsSuccess.Should().BeFalse();
        result.FileFound.Should().BeFalse();
        result.RobotsUrl.Should().BeEmpty();
        result.Metadata.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.RawContent.Should().BeNull();
    }

    [Fact]
    public void ShouldInitialize_SuccessfulParse()
    {
        var metadata = new RobotsMetadata { BaseUrl = "https://example.com" };
        var result = new RobotsTxtParseResult
        {
            IsSuccess = true,
            FileFound = true,
            RobotsUrl = "https://example.com/robots.txt",
            Metadata = metadata,
            RawContent = "User-agent: *\nDisallow: /admin"
        };

        result.IsSuccess.Should().BeTrue();
        result.FileFound.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.RawContent.Should().Contain("Disallow");
    }
}

public class RobotsMetadataTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var metadata = new RobotsMetadata();

        metadata.BaseUrl.Should().BeEmpty();
        metadata.Rules.Should().BeEmpty();
        metadata.CrawlDelay.Should().BeNull();
        metadata.Sitemaps.Should().BeEmpty();
        metadata.RequestRate.Should().BeNull();
        metadata.VisitTimeWindow.Should().BeNull();
        metadata.PreferredHost.Should().BeNull();
        metadata.AdditionalMetadata.Should().BeEmpty();
    }

    [Fact]
    public void ShouldInitialize_WithRulesAndSitemaps()
    {
        var metadata = new RobotsMetadata
        {
            BaseUrl = "https://example.com",
            Rules = new()
            {
                ["*"] = [new RobotsRule { Type = RobotsRuleType.Disallow, Pattern = "/admin" }]
            },
            CrawlDelay = TimeSpan.FromSeconds(2),
            Sitemaps = ["https://example.com/sitemap.xml"],
            PreferredHost = "www.example.com"
        };

        metadata.Rules.Should().ContainKey("*");
        metadata.Sitemaps.Should().ContainSingle();
        metadata.CrawlDelay!.Value.TotalSeconds.Should().Be(2);
    }
}

public class RobotsRuleTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var rule = new RobotsRule();

        rule.Type.Should().Be(RobotsRuleType.Allow);
        rule.Pattern.Should().BeEmpty();
        rule.UserAgent.Should().Be("*");
        rule.Priority.Should().Be(0);
    }

    [Fact]
    public void ShouldInitialize_WithAllFields()
    {
        var rule = new RobotsRule
        {
            Type = RobotsRuleType.Disallow,
            Pattern = "/private/*",
            UserAgent = "Googlebot",
            Priority = 1
        };

        rule.Type.Should().Be(RobotsRuleType.Disallow);
        rule.Pattern.Should().Be("/private/*");
        rule.UserAgent.Should().Be("Googlebot");
    }
}

public class RobotsRuleTypeEnumTests
{
    [Fact]
    public void ShouldHaveTwoValues()
    {
        Enum.GetValues<RobotsRuleType>().Should().HaveCount(2);
    }

    [Theory]
    [InlineData(RobotsRuleType.Allow, 0)]
    [InlineData(RobotsRuleType.Disallow, 1)]
    public void ShouldHaveExpectedIntValues(RobotsRuleType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

public class RequestRateLimitTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var limit = new RequestRateLimit();

        limit.RequestCount.Should().Be(0);
        limit.TimeWindow.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ShouldInitialize_WithValues()
    {
        var limit = new RequestRateLimit
        {
            RequestCount = 10,
            TimeWindow = TimeSpan.FromMinutes(1)
        };

        limit.RequestCount.Should().Be(10);
        limit.TimeWindow.TotalMinutes.Should().Be(1);
    }
}

public class VisitTimeWindowTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var window = new VisitTimeWindow();

        window.StartTime.Should().Be(TimeSpan.Zero);
        window.EndTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void IsCurrentTimeAllowed_WithUtcTimezone_ShouldWork()
    {
        var window = new VisitTimeWindow
        {
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromHours(24)
        };

        // Full day window should always be allowed
        window.IsCurrentTimeAllowed(TimeZoneInfo.Utc).Should().BeTrue();
    }
}
