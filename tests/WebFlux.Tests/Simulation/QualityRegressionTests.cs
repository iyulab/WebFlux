using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using WebFlux.Services.ContentExtractors;
using WebFlux.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace WebFlux.Tests.Simulation;

/// <summary>
/// 골든 파일 기반 회귀 테스트
/// 첫 실행 시 골든 파일 생성, 이후 실행 시 유사도 비교
/// </summary>
public class QualityRegressionTests
{
    private readonly HtmlToMarkdownExtractor _extractor;
    private readonly ITestOutputHelper _output;
    private static readonly string GoldenFilesDirectory;

    /// <summary>
    /// 골든 파일과의 최소 유사도 임계값 (0.0~1.0)
    /// </summary>
    private const double MinSimilarity = 0.7;

    static QualityRegressionTests()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        GoldenFilesDirectory = Path.Combine(assemblyDir, "Fixtures", "GoldenFiles");
    }

    public QualityRegressionTests(ITestOutputHelper output)
    {
        _output = output;
        _extractor = new HtmlToMarkdownExtractor(null);
    }

    public static IEnumerable<object[]> AllSnapshots =>
        HtmlSnapshotLoader.GetAllSnapshots()
            .Select(s => new object[] { s.Category, s.Name });

    [Theory]
    [MemberData(nameof(AllSnapshots))]
    public async Task RegressionCheck_ShouldMatchGoldenFileOrCreate(string category, string name)
    {
        // Arrange
        var html = HtmlSnapshotLoader.Load(category, name);
        var goldenFileName = $"{category}-{name}.md";
        var goldenFilePath = Path.Combine(GoldenFilesDirectory, goldenFileName);

        // Act
        var result = await _extractor.ExtractFromHtmlAsync(html, $"https://test.example.com/{name}");
        var currentMarkdown = result.Text;

        // 골든 파일이 없으면 생성
        if (!File.Exists(goldenFilePath))
        {
            Directory.CreateDirectory(GoldenFilesDirectory);
            await File.WriteAllTextAsync(goldenFilePath, currentMarkdown);
            _output.WriteLine($"[{category}/{name}] Golden file created: {goldenFileName} ({currentMarkdown.Length} chars)");
            return;
        }

        // 골든 파일과 비교
        var goldenMarkdown = await File.ReadAllTextAsync(goldenFilePath);
        var similarity = CalculateSimilarity(goldenMarkdown, currentMarkdown);

        _output.WriteLine($"[{category}/{name}] Similarity with golden file: {similarity:F3}");
        _output.WriteLine($"  Golden: {goldenMarkdown.Length} chars");
        _output.WriteLine($"  Current: {currentMarkdown.Length} chars");

        if (similarity < MinSimilarity)
        {
            // 차이점 요약 출력
            var diffs = GetDiffSummary(goldenMarkdown, currentMarkdown);
            foreach (var diff in diffs)
                _output.WriteLine($"  Diff: {diff}");
        }

        // Assert
        similarity.Should().BeGreaterThanOrEqualTo(MinSimilarity,
            $"output should be similar to golden file for {category}/{name}. " +
            $"If this is an intentional change, delete {goldenFileName} to regenerate.");
    }

    [Fact]
    public async Task RegressionCheck_AllSnapshots_ShouldHaveConsistentTitle()
    {
        foreach (var (category, name, html) in HtmlSnapshotLoader.LoadAll())
        {
            var result = await _extractor.ExtractFromHtmlAsync(
                html, $"https://test.example.com/{name}");

            _output.WriteLine($"[{category}/{name}] Title: \"{result.Title}\"");

            // 제목이 비어있거나 Untitled가 아니어야 함 (edge-minimal 제외)
            if (name != "edge-minimal")
            {
                result.Title.Should().NotBe("Untitled",
                    $"title should be extracted for {name}");
            }
        }
    }

    /// <summary>
    /// 두 텍스트의 유사도 계산 (단어 기반 Jaccard 유사도)
    /// </summary>
    private static double CalculateSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) && string.IsNullOrEmpty(text2))
            return 1.0;
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        var words1 = ExtractWords(text1);
        var words2 = ExtractWords(text2);

        if (words1.Count == 0 && words2.Count == 0)
            return 1.0;

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    /// <summary>
    /// 텍스트에서 의미있는 단어 추출
    /// </summary>
    private static HashSet<string> ExtractWords(string text)
    {
        return Regex.Matches(text, @"\b\w{3,}\b")
            .Select(m => m.Value.ToLowerInvariant())
            .ToHashSet();
    }

    /// <summary>
    /// 두 텍스트의 차이점 요약
    /// </summary>
    private static List<string> GetDiffSummary(string golden, string current)
    {
        var diffs = new List<string>();

        var goldenWords = ExtractWords(golden);
        var currentWords = ExtractWords(current);

        var missing = goldenWords.Except(currentWords).Take(10).ToList();
        var added = currentWords.Except(goldenWords).Take(10).ToList();

        if (missing.Count > 0)
            diffs.Add($"Missing words: {string.Join(", ", missing)}");
        if (added.Count > 0)
            diffs.Add($"Added words: {string.Join(", ", added)}");

        var lengthDiff = Math.Abs(golden.Length - current.Length);
        if (lengthDiff > 100)
            diffs.Add($"Length difference: {lengthDiff} chars");

        return diffs;
    }
}
