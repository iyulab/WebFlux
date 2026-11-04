using AwesomeAssertions;
using FluxCurator;
using FluxCurator.Core.Core;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Services;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using CuratorStrategy = FluxCurator.Core.Domain.ChunkingStrategy;
using WebFluxOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Tests.Services;

/// <summary>
/// A language nobody declared is not Korean. Three places used to say "ko" when the page or the caller had said nothing:
/// the chunking default (which picks the sentence-boundary profile), the metadata of every crawled page, and the Markdown
/// metadata fallback. Each fact asserts what a reader of the result can observe, with a control that proves the input is
/// one the wrong language would have handled differently.
/// </summary>
public class LanguageDefaultsTests
{
    // Abbreviation-heavy English: with the Korean boundary profile the sentence chunker cuts after "D.C." and "no."
    private const string English =
        "Dr. Smith met Mr. Jones at 5 p.m. on Jan. 5 in Washington, D.C. to discuss the U.S. budget. Prof. Lee, e.g. the chair, agreed with Mrs. Brown. " +
        "They left at approx. 6 p.m. and met St. John near the Ave. entrance. Mt. Rainier was visible from the 3rd fl. window. " +
        "Ms. Park noted vs. the old plan that costs rose by 4.5 percent. Sgt. Kim confirmed no. 7 was ready.";

    private const string Korean =
        "안녕하세요. 오늘은 날씨가 아주 좋습니다. 내일은 비가 올 것이라고 합니다. 우산을 챙기는 것이 좋겠습니다. " +
        "회의는 오후 세 시에 시작합니다. 자료는 미리 공유해 두었습니다. 질문이 있으면 편하게 말씀해 주세요.";

    private static async Task<string[]> SentenceChunks(string text, WebFluxOptions options)
    {
        var provider = new ServiceCollection().AddFluxCurator().BuildServiceProvider();
        var factory = provider.GetRequiredService<IChunkerFactory>();
        factory.TryCreateChunker(CuratorStrategy.Sentence, out var chunker).Should().BeTrue();
        var curatorOptions = FluxCuratorChunkAdapter.ToCuratorOptions(options, CuratorStrategy.Sentence);
        var chunks = await chunker!.ChunkAsync(text, curatorOptions, TestContext.Current.CancellationToken);
        return chunks.Select(c => c.Content.Replace('\n', ' ')).ToArray();
    }

    private static WebFluxOptions Tiny(string? language = null)
    {
        var options = new WebFluxOptions { MaxChunkSize = 25, MinChunkSize = 1, ChunkOverlap = 0 };
        if (language is not null) options.Language = language;
        return options;
    }

    [Fact]
    public void DefaultChunkingOptions_DeclareNoLanguage()
    {
        new WebFluxOptions().Language.Should().BeNullOrWhiteSpace("a default nobody chose must not reach the chunker as the caller's choice");
    }

    [Fact]
    public void DefaultChunkingOptions_ReachFluxCuratorAsDetect()
    {
        FluxCuratorChunkAdapter.ToCuratorOptions(new WebFluxOptions(), CuratorStrategy.Sentence).LanguageCode.Should().BeNull();
    }

    [Fact]
    public async Task EnglishPage_WithDefaultOptions_IsCutLikeEnglish()
    {
        var withDefault = await SentenceChunks(English, Tiny());
        var asEnglish = await SentenceChunks(English, Tiny("en"));
        var asKorean = await SentenceChunks(English, Tiny("ko"));

        asKorean.Should().NotEqual(asEnglish, "control: this text is cut differently by the Korean profile, so the equality below proves something");
        withDefault.Should().Equal(asEnglish);
    }

    [Fact]
    public async Task KoreanPage_WithDefaultOptions_IsStillCutLikeKorean()
    {
        // control for the other direction: detection must not cost Korean pages anything
        var withDefault = await SentenceChunks(Korean, Tiny());
        var asKorean = await SentenceChunks(Korean, Tiny("ko"));

        withDefault.Should().Equal(asKorean);
    }

    [Theory]
    [InlineData("<html lang=\"en\"><head><title>t</title></head><body>x</body></html>", "en")]
    [InlineData("<html lang=\"ko-KR\"><body>x</body></html>", "ko-KR")]
    [InlineData("<html xml:lang=\"fr\"><body>x</body></html>", "fr")]
    public void CrawledPage_ReportsTheLanguageItDeclares(string html, string expected)
    {
        WebContentProcessor.BuildCrawledMetadata(html, "text/html").Language.Should().Be(expected);
    }

    [Fact]
    public void CrawledPage_WithoutADeclaredLanguage_ReportsNone()
    {
        WebContentProcessor.BuildCrawledMetadata("<html><head><title>No language</title></head><body>Plain English text.</body></html>", "text/html")
            .Language.Should().BeNull("an unknown language is unknown, not Korean");
    }

    [Fact]
    public void CrawledPage_MetadataKeepsTitleTypeAndLength()
    {
        var html = "<html lang=\"en\"><head><title>Hello</title></head><body>x</body></html>";
        var metadata = WebContentProcessor.BuildCrawledMetadata(html, null);

        metadata.Title.Should().Be("Hello");
        metadata.ContentType.Should().Be("text/html");
        metadata.ContentLength.Should().Be(html.Length);
    }

    [Fact]
    public async Task MarkdownWithoutFrontMatterLanguage_ReportsNone()
    {
        var result = await new MarkdownStructureAnalyzer().AnalyzeStructureAsync("# Heading\n\nSome English text.\n", "doc.md", TestContext.Current.CancellationToken);

        result.Metadata.Language.Should().BeNull();
    }

    [Fact]
    public async Task MarkdownFrontMatterLanguage_IsStillRead()
    {
        var result = await new MarkdownStructureAnalyzer().AnalyzeStructureAsync("---\nlanguage: de\n---\n\n# Titel\n\nText.\n", "doc.md", TestContext.Current.CancellationToken);

        result.Metadata.Language.Should().Be("de");
    }
}
