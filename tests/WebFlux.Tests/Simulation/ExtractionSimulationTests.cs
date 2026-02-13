using FluentAssertions;
using WebFlux.Services.ContentExtractors;
using WebFlux.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace WebFlux.Tests.Simulation;

/// <summary>
/// 카테고리별 HTML 스냅샷에 대한 추출 시뮬레이션 테스트
/// 전체 파이프라인 품질을 정량적으로 검증
/// </summary>
public class ExtractionSimulationTests
{
    private readonly HtmlToMarkdownExtractor _extractor;
    private readonly ITestOutputHelper _output;

    public ExtractionSimulationTests(ITestOutputHelper output)
    {
        _output = output;
        _extractor = new HtmlToMarkdownExtractor(null);
    }

    [Theory]
    [InlineData("News", "news-bbc")]
    [InlineData("News", "news-naver")]
    [InlineData("TechDoc", "techdoc-mdn")]
    [InlineData("TechDoc", "techdoc-mslearn")]
    [InlineData("Blog", "blog-medium")]
    [InlineData("Blog", "blog-devto")]
    [InlineData("Ecommerce", "ecom-product")]
    [InlineData("Forum", "forum-stackoverflow")]
    [InlineData("Korean", "korean-tistory")]
    [InlineData("Korean", "korean-namu")]
    [InlineData("Edge", "edge-minimal")]
    [InlineData("Edge", "edge-table-heavy")]
    [InlineData("Edge", "edge-image-heavy")]
    public async Task SimulateExtraction_ShouldMeetQualityThreshold(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var url = $"https://test.example.com/{name}";

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, url);
        var metrics = QualityMeasurer.Measure(result, html);

        // Output
        _output.WriteLine($"[{category}/{name}] {metrics}");
        _output.WriteLine($"  Title: {result.Title}");
        _output.WriteLine($"  RawMarkdown length: {result.RawMarkdown?.Length ?? 0}");
        _output.WriteLine($"  FitMarkdown length: {result.FitMarkdown?.Length ?? 0}");
        _output.WriteLine($"  Text length: {result.Text.Length}");
        _output.WriteLine($"  WordCount: {result.WordCount}");
        foreach (var issue in metrics.Issues)
            _output.WriteLine($"  Issue: {issue}");

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().NotBeNullOrEmpty($"extraction should produce text for {category}/{name}");
        result.RawMarkdown.Should().NotBeNullOrEmpty($"RawMarkdown should be populated for {category}/{name}");

        // 종합 품질 점수 0.6 이상
        metrics.OverallScore.Should().BeGreaterThanOrEqualTo(0.6,
            $"quality threshold for {category}/{name}. Issues: {string.Join("; ", metrics.Issues)}");
    }

    [Theory]
    [InlineData("News", "news-bbc")]
    [InlineData("News", "news-naver")]
    [InlineData("Blog", "blog-medium")]
    [InlineData("Blog", "blog-devto")]
    public async Task SimulateExtraction_NewsAndBlog_ShouldHaveHighContentCompleteness(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, $"https://test.example.com/{name}");
        var metrics = QualityMeasurer.Measure(result, html);

        // Assert - 뉴스/블로그는 핵심 콘텐츠 보존율이 높아야 함
        metrics.ContentCompleteness.Should().BeGreaterThanOrEqualTo(0.7,
            $"news/blog content completeness for {name}");
    }

    [Theory]
    [InlineData("TechDoc", "techdoc-mdn")]
    [InlineData("TechDoc", "techdoc-mslearn")]
    [InlineData("Forum", "forum-stackoverflow")]
    public async Task SimulateExtraction_TechContent_ShouldPreserveCodeBlocks(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, $"https://test.example.com/{name}");

        // Assert - 기술 문서/포럼은 코드 블록이 보존되어야 함
        result.Text.Should().MatchRegex(@"```[\s\S]*?```|    .+",
            $"code blocks should be preserved for {name}");
    }

    [Theory]
    [InlineData("Edge", "edge-table-heavy")]
    [InlineData("Ecommerce", "ecom-product")]
    public async Task SimulateExtraction_TableContent_ShouldPreserveTables(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, $"https://test.example.com/{name}");

        // Assert - 테이블이 Markdown 테이블로 보존되어야 함
        result.Text.Should().Contain("|",
            $"tables should be preserved as Markdown tables for {name}");
    }

    [Theory]
    [InlineData("Korean", "korean-tistory")]
    [InlineData("Korean", "korean-namu")]
    [InlineData("News", "news-naver")]
    public async Task SimulateExtraction_Korean_ShouldPreserveKoreanText(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, $"https://test.example.com/{name}");

        // Assert - 한국어 텍스트가 포함되어야 함
        result.Text.Should().MatchRegex(@"[\uAC00-\uD7A3]",
            $"Korean text should be present in output for {name}");
    }

    [Fact]
    public async Task SimulateExtraction_Minimal_ShouldHandleGracefully()
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load("Edge", "edge-minimal");

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, "https://test.example.com/edge-minimal");

        // Assert
        result.Text.Should().Contain("minimal HTML page");
    }

    [Fact]
    public async Task SimulateExtraction_AllSnapshots_ShouldNotThrow()
    {
        // 모든 스냅샷에 대해 예외 없이 추출 완료되어야 함
        foreach (var (category, name, html) in HtmlSnapshotLoader.LoadAll())
        {
            var act = async () => await _extractor.ExtractFromHtmlAsync(
                html, $"https://test.example.com/{name}");

            await act.Should().NotThrowAsync(
                $"extraction should not throw for {category}/{name}");
        }
    }
}
