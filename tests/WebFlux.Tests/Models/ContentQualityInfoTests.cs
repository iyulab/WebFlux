using FluentAssertions;
using WebFlux.Core.Models;

namespace WebFlux.Tests.Models;

public class ContentQualityInfoTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var info = new ContentQualityInfo();

        info.OverallScore.Should().Be(0);
        info.Language.Should().Be("en");
        info.EstimatedReadingTimeMinutes.Should().Be(0);
        info.WordCount.Should().Be(0);
        info.HasPaywall.Should().BeFalse();
        info.RequiresLogin.Should().BeFalse();
        info.HasAgeRestriction.Should().BeFalse();
        info.ContentRatio.Should().Be(0);
        info.AdDensity.Should().Be(0);
        info.HasMainContent.Should().BeFalse();
        info.HasStructuredData.Should().BeFalse();
        info.HasAuthor.Should().BeFalse();
        info.HasPublishDate.Should().BeFalse();
        info.PublishDate.Should().BeNull();
        info.LastModifiedDate.Should().BeNull();
        info.HasCitations.Should().BeFalse();
        info.IsMobileFriendly.Should().BeFalse();
        info.IsSecure.Should().BeFalse();
        info.LoadTimeMs.Should().Be(0);
        info.LlmSuitabilityScore.Should().Be(0);
        info.EstimatedTokenCount.Should().Be(0);
        info.NoiseRatio.Should().Be(0);
        info.ContentType.Should().BeNull();
    }

    [Theory]
    [InlineData(0.9, QualityGrade.Excellent)]
    [InlineData(0.8, QualityGrade.Excellent)]
    [InlineData(0.79, QualityGrade.Good)]
    [InlineData(0.6, QualityGrade.Good)]
    [InlineData(0.59, QualityGrade.Fair)]
    [InlineData(0.4, QualityGrade.Fair)]
    [InlineData(0.39, QualityGrade.Poor)]
    [InlineData(0.2, QualityGrade.Poor)]
    [InlineData(0.19, QualityGrade.VeryPoor)]
    [InlineData(0.0, QualityGrade.VeryPoor)]
    public void Grade_ShouldComputeCorrectly(double score, QualityGrade expected)
    {
        var info = new ContentQualityInfo { OverallScore = score };
        info.Grade.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldIncludePercentageAndGrade()
    {
        var info = new ContentQualityInfo
        {
            OverallScore = 0.85,
            WordCount = 1500,
            EstimatedReadingTimeMinutes = 5
        };

        var result = info.ToString();
        result.Should().Contain("85%");
        result.Should().Contain("Excellent");
        result.Should().Contain("1500 words");
        result.Should().Contain("5min read");
    }

    [Fact]
    public void ToString_WithPaywall_ShouldIncludeWarning()
    {
        var info = new ContentQualityInfo
        {
            OverallScore = 0.7,
            HasPaywall = true
        };

        info.ToString().Should().Contain("[Paywall]");
    }

    [Fact]
    public void ToString_WithoutPaywall_ShouldNotIncludeWarning()
    {
        var info = new ContentQualityInfo { OverallScore = 0.7 };

        info.ToString().Should().NotContain("[Paywall]");
    }
}

public class QualityGradeEnumTests
{
    [Fact]
    public void ShouldHaveFiveValues()
    {
        Enum.GetValues<QualityGrade>().Should().HaveCount(5);
    }

    [Theory]
    [InlineData(QualityGrade.Excellent, 0)]
    [InlineData(QualityGrade.Good, 1)]
    [InlineData(QualityGrade.Fair, 2)]
    [InlineData(QualityGrade.Poor, 3)]
    [InlineData(QualityGrade.VeryPoor, 4)]
    public void ShouldHaveExpectedIntValues(QualityGrade grade, int expected)
    {
        ((int)grade).Should().Be(expected);
    }
}
