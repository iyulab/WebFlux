using WebFlux.Core.Interfaces;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// Phase 5C: 메타데이터 품질 개선 검증 테스트
/// 90% 품질 목표 달성 확인
/// </summary>
public class MetadataQualityValidationTest
{
    private readonly MetadataExtractor _extractor;

    public MetadataQualityValidationTest()
    {
        _extractor = new MetadataExtractor();
    }

    [Fact(Skip = "v1.0: Requires test data files and EvaluateCompleteness() implementation")]
    public async Task ValidateMetadataQuality_ShouldAchieve90PercentTarget()
    {
        // Arrange - Rich metadata HTML from test data
        var htmlContent = await File.ReadAllTextAsync("TestData/sample-rich-metadata.html");
        var sourceUrl = "https://github.com/iyulab/WebFlux";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert - Phase 5C 목표: 90% 메타데이터 품질 달성
        metadata.QualityScore.Should().BeGreaterThan(0.90,
            "Phase 5C 목표는 90% 이상의 메타데이터 품질 달성입니다");

        // 상세 품질 분석
        var completeness = _extractor.EvaluateCompleteness(metadata);

        // 기본 메타데이터는 90% 이상이어야 함
        completeness.BasicMetadataScore.Should().BeGreaterThan(0.90);

        // Open Graph는 80% 이상이어야 함 (소셜 미디어 최적화)
        completeness.OpenGraphScore.Should().BeGreaterThan(0.80);

        // 전체 완성도는 85% 이상이어야 함
        completeness.OverallScore.Should().BeGreaterThan(0.85);

        // 누락된 중요 필드가 최소한이어야 함
        completeness.MissingCriticalFields.Should().HaveCountLessThan(2);

        // 실제 추출된 데이터 검증
        metadata.Basic.Title.Should().NotBeNullOrEmpty();
        metadata.Basic.Description.Should().NotBeNullOrEmpty();
        metadata.Basic.Keywords.Should().NotBeEmpty();
        metadata.OpenGraph.Title.Should().NotBeNullOrEmpty();
        metadata.OpenGraph.Image.Should().NotBeNullOrEmpty();
        metadata.SchemaOrg.MainEntityType.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "v1.0: Requires test data files")]
    public async Task ValidateMarkdownStructureAccuracy_ShouldAchieve95PercentTarget()
    {
        // Arrange - Blog post with complex structure
        var htmlContent = await File.ReadAllTextAsync("TestData/sample-blog-post.html");
        var sourceUrl = "https://techblog.example.com/rag-optimization";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert - Phase 5C 목표: 95% 마크다운 구조 정확도

        // 문서 구조가 정확히 파싱되어야 함
        metadata.Structure.Headings.Should().HaveCountGreaterThan(5);
        metadata.Structure.ParagraphCount.Should().BeGreaterThan(10);

        // 헤딩 구조가 계층적이어야 함
        var headingLevels = metadata.Structure.Headings.Select(h => h.Level).ToList();
        headingLevels.Should().Contain(1); // H1 존재
        headingLevels.Should().Contain(2); // H2 존재

        // 읽기 시간이 합리적이어야 함 (긴 블로그 포스트)
        metadata.Structure.EstimatedReadingTimeMinutes.Should().BeGreaterThan(5);

        // 복잡도 점수가 적절해야 함
        metadata.Structure.ComplexityScore.Should().BeGreaterThan(0.5);

        // 접근성 점수가 높아야 함
        metadata.Accessibility.AccessibilityScore.Should().BeGreaterThan(70);
    }

    [Theory(Skip = "v1.0: Requires test data files")]
    [InlineData("15개 웹 표준 메타데이터")]
    [InlineData("HtmlAgilityPack 파싱")]
    [InlineData("Schema.org JSON-LD")]
    [InlineData("Open Graph 프로토콜")]
    [InlineData("Dublin Core")]
    public async Task ExtractSpecificMetadataStandard_ShouldSupportAllStandards(string description)
    {
        // Arrange
        var htmlContent = await File.ReadAllTextAsync("TestData/sample-rich-metadata.html");
        var sourceUrl = "https://webflux.dev/test";

        // Act
        var metadata = await _extractor.ExtractMetadataAsync(htmlContent, sourceUrl);

        // Assert - 모든 15개 웹 표준이 지원되어야 함
        switch (description)
        {
            case "15개 웹 표준 메타데이터":
                // 모든 메타데이터 섹션이 존재해야 함
                metadata.Basic.Should().NotBeNull();
                metadata.OpenGraph.Should().NotBeNull();
                metadata.TwitterCards.Should().NotBeNull();
                metadata.SchemaOrg.Should().NotBeNull();
                metadata.DublinCore.Should().NotBeNull();
                metadata.Structure.Should().NotBeNull();
                metadata.Navigation.Should().NotBeNull();
                metadata.Technical.Should().NotBeNull();
                metadata.Classification.Should().NotBeNull();
                metadata.Accessibility.Should().NotBeNull();
                break;

            case "HtmlAgilityPack 파싱":
                // HtmlAgilityPack을 통한 정확한 파싱 검증
                metadata.Structure.Headings.Should().NotBeEmpty();
                metadata.Structure.LinkCount.Should().BeGreaterThan(0);
                break;

            case "Schema.org JSON-LD":
                // JSON-LD 구조화 데이터 파싱 검증
                metadata.SchemaOrg.RawJsonLd.Should().NotBeEmpty();
                metadata.SchemaOrg.MainEntityType.Should().NotBeNullOrEmpty();
                break;

            case "Open Graph 프로토콜":
                // Open Graph 메타데이터 검증
                metadata.OpenGraph.Title.Should().NotBeNullOrEmpty();
                metadata.OpenGraph.Type.Should().NotBeNullOrEmpty();
                break;

            case "Dublin Core":
                // Dublin Core 메타데이터 검증
                metadata.DublinCore.Title.Should().NotBeNullOrEmpty();
                break;
        }
    }

    [Fact(Skip = "v1.0: Requires EvaluateCompleteness() implementation")]
    public void CalculateQualityScore_ShouldMeetPhase5CTargets()
    {
        // Arrange - Complete metadata scenario
        var richMetadata = CreateRichMetadataForTesting();

        // Act
        var qualityScore = _extractor.CalculateQualityScore(richMetadata);

        // Assert - Phase 5C 목표 달성 확인
        qualityScore.Should().BeGreaterThan(0.90,
            "풍부한 메타데이터는 90% 이상의 품질 점수를 달성해야 합니다");

        // 개별 구성 요소 점수 검증
        var completeness = _extractor.EvaluateCompleteness(richMetadata);

        completeness.BasicMetadataScore.Should().BeGreaterThan(0.85,
            "기본 메타데이터 점수는 85% 이상이어야 합니다");

        completeness.OpenGraphScore.Should().BeGreaterThan(0.80,
            "Open Graph 점수는 80% 이상이어야 합니다");

        completeness.SchemaOrgScore.Should().BeGreaterThan(0.70,
            "Schema.org 점수는 70% 이상이어야 합니다");
    }

    private static WebFlux.Core.Models.WebMetadata CreateRichMetadataForTesting()
    {
        return new WebFlux.Core.Models.WebMetadata
        {
            SourceUrl = "https://webflux.dev/test",
            Basic = new WebFlux.Core.Models.BasicHtmlMetadata
            {
                Title = "WebFlux SDK - RAG 최적화 라이브러리",
                Description = "고성능 RAG 전처리를 위한 .NET SDK",
                Keywords = new[] { "RAG", "AI", "웹크롤링", ".NET", "SDK" },
                Author = "Iyulab Corporation",
                Language = "ko",
                CanonicalUrl = "https://webflux.dev/sdk"
            },
            OpenGraph = new WebFlux.Core.Models.OpenGraphMetadata
            {
                Title = "WebFlux SDK",
                Description = "RAG 시스템을 위한 고성능 웹 콘텐츠 처리",
                Image = "https://webflux.dev/images/preview.jpg",
                Url = "https://webflux.dev/sdk",
                Type = "website"
            },
            TwitterCards = new WebFlux.Core.Models.TwitterCardsMetadata
            {
                Card = "summary_large_image",
                Title = "WebFlux SDK",
                Description = "RAG 최적화 라이브러리",
                Image = "https://webflux.dev/images/twitter.jpg"
            },
            SchemaOrg = new WebFlux.Core.Models.SchemaOrgMetadata
            {
                MainEntityType = "SoftwareLibrary",
                Software = new WebFlux.Core.Models.SoftwareInfo
                {
                    Name = "WebFlux SDK",
                    Version = "1.0.0",
                    ProgrammingLanguage = "C#"
                },
                RawJsonLd = new[] { "{\"@type\": \"SoftwareLibrary\"}" }
            },
            Structure = new WebFlux.Core.Models.DocumentStructure
            {
                Headings = new[]
                {
                    new WebFlux.Core.Models.HeadingInfo { Level = 1, Text = "Main Title" },
                    new WebFlux.Core.Models.HeadingInfo { Level = 2, Text = "Features" },
                    new WebFlux.Core.Models.HeadingInfo { Level = 2, Text = "Installation" }
                },
                ParagraphCount = 15,
                LinkCount = 10,
                EstimatedReadingTimeMinutes = 8,
                ComplexityScore = 0.7
            },
            Accessibility = new WebFlux.Core.Models.AccessibilityMetadata
            {
                ImageAltTextCoverage = 0.95,
                HasProperHeadingStructure = true,
                AccessibilityScore = 88
            }
        };
    }
}