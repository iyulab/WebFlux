using FluentAssertions;
using WebFlux.Core.Utilities;

namespace WebFlux.Tests.Core.Utilities;

/// <summary>
/// UrlNormalizer 단위 테스트
/// URL 정규화 및 동등성 비교 검증
/// </summary>
public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://Example.COM/page", "https://example.com/page")]
    [InlineData("https://www.example.com/page", "https://example.com/page")]
    [InlineData("https://example.com/page/", "https://example.com/page")]
    [InlineData("https://example.com/", "https://example.com/")]  // root keeps slash
    [InlineData("https://example.com:443/page", "https://example.com/page")]
    [InlineData("http://example.com:80/page", "http://example.com/page")]
    [InlineData("http://example.com:8080/page", "http://example.com:8080/page")]  // non-default port kept
    [InlineData("https://example.com/page#section", "https://example.com/page")]
    [InlineData("https://example.com//page///sub", "https://example.com/page/sub")]
    public void Normalize_ShouldProduceExpectedResult(string input, string expected)
    {
        UrlNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://example.com/page", "https://Example.COM/page/")]
    [InlineData("https://www.example.com/page", "https://example.com/page")]
    public void AreEquivalent_ShouldReturnTrue(string url1, string url2)
    {
        UrlNormalizer.AreEquivalent(url1, url2).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    public void Normalize_InvalidInput_ShouldReturnAsIs(string? input)
    {
        UrlNormalizer.Normalize(input!).Should().Be(input);
    }
}
