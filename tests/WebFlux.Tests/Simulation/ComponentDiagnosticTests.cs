using FluentAssertions;
using ReverseMarkdown;
using WebFlux.Core.Options;
using WebFlux.Services.ContentExtractors;
using WebFlux.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace WebFlux.Tests.Simulation;

/// <summary>
/// 파이프라인 각 컴포넌트별 진단 테스트
/// HtmlContentCleaner, TextDensityFilter, ReverseMarkdown 각 단계별 품질 분석
/// </summary>
public class ComponentDiagnosticTests
{
    private readonly HtmlContentCleaner _cleaner;
    private readonly TextDensityFilter _densityFilter;
    private readonly Converter _markdownConverter;
    private readonly ITestOutputHelper _output;

    public ComponentDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
        _cleaner = new HtmlContentCleaner();
        _densityFilter = new TextDensityFilter();
        _markdownConverter = new Converter(new ReverseMarkdown.Config
        {
            GithubFlavored = true,
            SmartHrefHandling = true,
            RemoveComments = true,
            UnknownTags = Config.UnknownTagsOption.Bypass
        });
    }

    public static IEnumerable<object[]> AllSnapshots =>
        HtmlSnapshotLoader.GetAllSnapshots()
            .Select(s => new object[] { s.Category, s.Name });

    [Theory]
    [MemberData(nameof(AllSnapshots))]
    public async Task Diagnostic_CleanerOnly_ShouldReduceSize(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act - HtmlContentCleaner만 적용
        var cleanedHtml = await _cleaner.CleanAsync(html, $"https://test.example.com/{name}");

        // Output
        var reductionPct = html.Length > 0
            ? (1.0 - (double)cleanedHtml.Length / html.Length) * 100
            : 0;
        _output.WriteLine($"[{category}/{name}] Cleaner: {html.Length} -> {cleanedHtml.Length} chars ({reductionPct:F1}% reduction)");

        // Assert - 정리 후 크기가 줄어야 함 (최소 페이지 제외)
        if (html.Length > 200)
        {
            cleanedHtml.Length.Should().BeLessThan(html.Length,
                $"cleaner should reduce size for {name}");
        }
    }

    [Theory]
    [MemberData(nameof(AllSnapshots))]
    public async Task Diagnostic_DensityFilterOnly_ShouldNotRemoveTooMuch(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var cleanedHtml = await _cleaner.CleanAsync(html, $"https://test.example.com/{name}");

        if (string.IsNullOrWhiteSpace(cleanedHtml))
            return;

        // Act - TextDensityFilter만 적용
        var filteredHtml = await _densityFilter.FilterAsync(cleanedHtml);

        // Output
        var retainedPct = cleanedHtml.Length > 0
            ? (double)filteredHtml.Length / cleanedHtml.Length * 100
            : 0;
        _output.WriteLine($"[{category}/{name}] DensityFilter: {cleanedHtml.Length} -> {filteredHtml.Length} chars ({retainedPct:F1}% retained)");

        // Assert - 필터가 모든 콘텐츠를 제거하면 안 됨
        if (cleanedHtml.Length > 100)
        {
            filteredHtml.Should().NotBeNullOrEmpty(
                $"density filter should not remove all content for {name}");

            // 최소 10% 이상 보존
            filteredHtml.Length.Should().BeGreaterThan(cleanedHtml.Length / 10,
                $"density filter should retain at least 10% for {name}");
        }
    }

    [Theory]
    [MemberData(nameof(AllSnapshots))]
    public async Task Diagnostic_MarkdownConversionOnly_ShouldProduceValidMarkdown(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var cleanedHtml = await _cleaner.CleanAsync(html, $"https://test.example.com/{name}");

        if (string.IsNullOrWhiteSpace(cleanedHtml))
            return;

        // Act - ReverseMarkdown만 적용
        var markdown = _markdownConverter.Convert(cleanedHtml);

        // Output
        _output.WriteLine($"[{category}/{name}] Markdown conversion: {cleanedHtml.Length} HTML -> {markdown.Length} MD chars");

        // Assert
        markdown.Should().NotBeNullOrEmpty(
            $"markdown conversion should produce output for {name}");
    }

    [Theory]
    [InlineData("News", "news-bbc")]
    [InlineData("News", "news-naver")]
    [InlineData("Blog", "blog-medium")]
    public async Task Diagnostic_CleanerShouldRemoveNoiseElements(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act
        var cleanedHtml = await _cleaner.CleanAsync(html, $"https://test.example.com/{name}");

        // Assert - 클리너가 노이즈 요소를 제거했는지 확인
        cleanedHtml.Should().NotContain("cookie", "cookie banners should be removed");
        cleanedHtml.Should().NotContainEquivalentOf("<nav",
            "navigation should be removed in OnlyMainContent mode");
    }

    [Theory]
    [InlineData("Edge", "edge-table-heavy")]
    [InlineData("Ecommerce", "ecom-product")]
    public async Task Diagnostic_TablePreservation_ThroughPipeline(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act - 각 단계별 테이블 보존 확인
        var cleanedHtml = await _cleaner.CleanAsync(html, $"https://test.example.com/{name}");
        var markdown = _markdownConverter.Convert(cleanedHtml);

        // Output
        var htmlTableCount = System.Text.RegularExpressions.Regex
            .Matches(html, @"<table[\s>]", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        var cleanedTableCount = System.Text.RegularExpressions.Regex
            .Matches(cleanedHtml, @"<table[\s>]", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        var mdTableCount = System.Text.RegularExpressions.Regex
            .Matches(markdown, @"\|.*\|.*\|").Count;

        _output.WriteLine($"[{category}/{name}] Tables: HTML={htmlTableCount}, Cleaned={cleanedTableCount}, Markdown={mdTableCount}");

        // Assert - 메인 콘텐츠의 테이블은 보존되어야 함
        mdTableCount.Should().BeGreaterThan(0,
            $"tables should be preserved in markdown for {name}");
    }

    [Theory]
    [InlineData("Edge", "edge-image-heavy")]
    public async Task Diagnostic_ImagePreservation_ThroughPipeline(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act
        var cleanedHtml = await _cleaner.CleanAsync(html, $"https://test.example.com/{name}");
        var markdown = _markdownConverter.Convert(cleanedHtml);

        // Output
        var htmlImageCount = System.Text.RegularExpressions.Regex
            .Matches(html, @"<img\s", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        var mdImageCount = System.Text.RegularExpressions.Regex
            .Matches(markdown, @"!\[.*?\]\(.+?\)").Count;

        _output.WriteLine($"[{category}/{name}] Images: HTML={htmlImageCount}, Markdown={mdImageCount}");

        // Assert - 메인 콘텐츠 이미지가 보존되어야 함
        mdImageCount.Should().BeGreaterThan(0,
            $"main content images should be preserved for {name}");
    }
}
