using FluentAssertions;
using WebFlux.Services.ContentExtractors;
using WebFlux.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace WebFlux.Tests.Simulation;

/// <summary>
/// BasicContentExtractor vs HtmlToMarkdownExtractor 비교 테스트
/// 동일 HTML에 대해 두 추출기의 품질을 비교
/// </summary>
public class ExtractorComparisonTests
{
    private readonly HtmlToMarkdownExtractor _markdownExtractor;
    private readonly BasicContentExtractor _basicExtractor;
    private readonly ITestOutputHelper _output;

    public ExtractorComparisonTests(ITestOutputHelper output)
    {
        _output = output;
        _markdownExtractor = new HtmlToMarkdownExtractor(null);
        _basicExtractor = new BasicContentExtractor(null);
    }

    public static IEnumerable<object[]> AllSnapshots =>
        HtmlSnapshotLoader.GetAllSnapshots()
            .Select(s => new object[] { s.Category, s.Name });

    [Theory]
    [MemberData(nameof(AllSnapshots))]
    public async Task CompareExtractors_BothShouldProduceOutput(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var url = $"https://test.example.com/{name}";

        // Act
        var mdResult = await _markdownExtractor.ExtractFromHtmlAsync(html, url);
        var basicResult = await _basicExtractor.ExtractFromHtmlAsync(html, url);

        // Assert - 두 추출기 모두 결과를 생성해야 함
        mdResult.Text.Should().NotBeNullOrEmpty($"HtmlToMarkdown should produce text for {name}");
        basicResult.Text.Should().NotBeNullOrEmpty($"Basic should produce text for {name}");

        _output.WriteLine($"[{category}/{name}]");
        _output.WriteLine($"  HtmlToMarkdown: {mdResult.Text.Length} chars, {mdResult.WordCount} words");
        _output.WriteLine($"  Basic:          {basicResult.Text.Length} chars");
    }

    [Theory]
    [MemberData(nameof(AllSnapshots))]
    public async Task CompareExtractors_HtmlToMarkdown_ShouldHaveBetterOrEqualQuality(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var url = $"https://test.example.com/{name}";

        // Act
        var mdResult = await _markdownExtractor.ExtractFromHtmlAsync(html, url);
        var basicResult = await _basicExtractor.ExtractFromHtmlAsync(html, url);

        var mdMetrics = QualityMeasurer.Measure(mdResult, html);
        var basicMetrics = QualityMeasurer.Measure(basicResult, html);

        // Output
        _output.WriteLine($"[{category}/{name}]");
        _output.WriteLine($"  HtmlToMarkdown: {mdMetrics}");
        _output.WriteLine($"  Basic:          {basicMetrics}");
        _output.WriteLine($"  Delta (MD - Basic): {mdMetrics.OverallScore - basicMetrics.OverallScore:+0.00;-0.00}");

        // Assert - HtmlToMarkdown이 Basic보다 나쁘지 않아야 함
        // 약간의 허용 오차 적용 (일부 단순 페이지에서는 Basic이 더 나을 수 있음)
        mdMetrics.OverallScore.Should().BeGreaterThanOrEqualTo(
            basicMetrics.OverallScore - 0.15,
            $"HtmlToMarkdown should not be significantly worse than Basic for {name}");
    }

    [Theory]
    [InlineData("News", "news-bbc")]
    [InlineData("TechDoc", "techdoc-mdn")]
    [InlineData("Blog", "blog-medium")]
    public async Task CompareExtractors_StructuredContent_MarkdownShouldPreserveMore(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var url = $"https://test.example.com/{name}";

        // Act
        var mdResult = await _markdownExtractor.ExtractFromHtmlAsync(html, url);
        var basicResult = await _basicExtractor.ExtractFromHtmlAsync(html, url);

        var mdMetrics = QualityMeasurer.Measure(mdResult, html);
        var basicMetrics = QualityMeasurer.Measure(basicResult, html);

        _output.WriteLine($"[{category}/{name}] Structure Preservation:");
        _output.WriteLine($"  HtmlToMarkdown: {mdMetrics.StructurePreservation:F2}");
        _output.WriteLine($"  Basic:          {basicMetrics.StructurePreservation:F2}");

        // Assert - 구조화된 콘텐츠에서는 Markdown 추출기가 구조 보존이 더 좋아야 함
        mdMetrics.StructurePreservation.Should().BeGreaterThanOrEqualTo(
            basicMetrics.StructurePreservation,
            $"HtmlToMarkdown should preserve structure better for {name}");
    }
}
