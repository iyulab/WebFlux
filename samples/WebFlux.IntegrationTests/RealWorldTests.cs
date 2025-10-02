using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using WebFlux.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace WebFlux.IntegrationTests;

/// <summary>
/// 실제 웹사이트를 대상으로 한 통합 테스트
/// target-urls.txt의 실제 URL들을 처리하여 성능과 품질 평가
/// </summary>
public class RealWorldTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IWebContentProcessor _processor;

    public RealWorldTests(ITestOutputHelper output)
    {
        _output = output;

        // 서비스 컨테이너 설정
        var services = new ServiceCollection();

        // WebFlux 서비스 등록
        services.AddWebFlux(options =>
        {
            options.EnableDevelopmentMode(true);
            options.EnableVerboseLogging(true);
            options.ConfigureCrawling(crawling =>
            {
                crawling.MaxPages = 5; // 테스트에서는 제한적으로
                crawling.MaxDepth = 2;
                crawling.DelayBetweenRequests = TimeSpan.FromSeconds(1);
            });
            options.ConfigureChunking(chunking =>
            {
                chunking.DefaultChunkSize = 1000;
                chunking.DefaultMaxChunkSize = 2000;
                chunking.DefaultMinChunkSize = 200;
            });
        });

        // 실제 AI 서비스 등록 (OpenAI)
        services.AddSingleton<ITextCompletionService, OpenAITextCompletionService>();
        services.AddHttpClient<OpenAITextCompletionService>();

        // 로깅 설정
        services.AddLogging(builder =>
        {
            builder.AddXUnit(output);
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _serviceProvider = services.BuildServiceProvider();
        _processor = _serviceProvider.GetRequiredService<IWebContentProcessor>();
    }

    [Fact]
    public async Task ProcessRealWebsites_ShouldSucceed()
    {
        // Arrange
        var testUrls = await LoadTestUrls();
        var configuration = new WebFluxConfiguration
        {
            Crawling = new CrawlConfiguration
            {
                StartUrls = testUrls,
                Strategy = CrawlStrategy.BreadthFirst,
                MaxPages = 3, // 각 사이트당 3페이지만
                MaxDepth = 1,
                DelayBetweenRequests = TimeSpan.FromSeconds(2) // 서버 부하 고려
            },
            Extraction = new ExtractionConfiguration
            {
                IncludeLinkUrls = false,
                NormalizeWhitespace = true,
                MinTextLength = 100
            },
            Chunking = new ChunkingConfiguration
            {
                DefaultStrategy = "Paragraph",
                DefaultChunkSize = 800,
                DefaultMaxChunkSize = 1500,
                OverlapSize = 100
            }
        };

        var processedChunks = new List<WebContentChunk>();
        var processingMetrics = new ProcessingMetrics();

        // Act
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            await foreach (var chunk in _processor.ProcessAsync(configuration))
            {
                processedChunks.Add(chunk);
                processingMetrics.TotalChunks++;

                // 진행 상황 로깅
                if (processedChunks.Count % 5 == 0)
                {
                    _output.WriteLine($"Processed {processedChunks.Count} chunks so far...");
                }

                // 안전장치: 너무 많은 청크 생성 방지
                if (processedChunks.Count > 50)
                {
                    _output.WriteLine("Reached chunk limit, stopping test");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Processing failed: {ex.Message}");
            throw;
        }

        var endTime = DateTimeOffset.UtcNow;
        var totalTime = endTime - startTime;

        // Assert & Analyze
        Assert.True(processedChunks.Count > 0, "Should process at least one chunk");

        // 성능 메트릭 계산
        processingMetrics.TotalProcessingTime = totalTime;
        processingMetrics.AverageChunkSize = processedChunks.Average(c => c.Content.Length);
        processingMetrics.ProcessingRate = processedChunks.Count / totalTime.TotalMinutes;

        // 품질 메트릭 계산
        var qualityMetrics = AnalyzeChunkQuality(processedChunks);

        // 결과 리포팅
        await ReportTestResults(processingMetrics, qualityMetrics, processedChunks);

        // 최소 품질 기준 검증
        Assert.True(qualityMetrics.AverageReadability > 0.6,
            $"Average readability ({qualityMetrics.AverageReadability:F2}) should be > 0.6");
        Assert.True(qualityMetrics.MeaningfulChunkRatio > 0.7,
            $"Meaningful chunk ratio ({qualityMetrics.MeaningfulChunkRatio:F2}) should be > 0.7");
    }

    [Fact]
    public async Task ProcessMicrosoftDocs_ShouldExtractTechnicalContent()
    {
        // Arrange - Microsoft 기술 문서 테스트
        var configuration = new WebFluxConfiguration
        {
            Crawling = new CrawlConfiguration
            {
                StartUrls = new List<string> { "https://learn.microsoft.com/ko-kr/windows-server" },
                Strategy = CrawlStrategy.BreadthFirst,
                MaxPages = 2,
                MaxDepth = 1
            },
            Chunking = new ChunkingConfiguration
            {
                DefaultStrategy = "FixedSize",
                DefaultChunkSize = 1200
            }
        };

        var chunks = new List<WebContentChunk>();

        // Act
        await foreach (var chunk in _processor.ProcessAsync(configuration))
        {
            chunks.Add(chunk);
            if (chunks.Count >= 10) break; // 테스트 제한
        }

        // Assert
        Assert.True(chunks.Count > 0);

        // 기술 문서 특성 검증
        var hasCodeBlocks = chunks.Any(c => c.Content.Contains("```") || c.Content.Contains("코드"));
        var hasHeaders = chunks.Any(c => c.Content.Contains("#") || c.Metadata?.Title?.Length > 0);

        _output.WriteLine($"Processed {chunks.Count} chunks from Microsoft Docs");
        _output.WriteLine($"Has code blocks: {hasCodeBlocks}");
        _output.WriteLine($"Has headers: {hasHeaders}");
    }

    [Fact]
    public async Task ChunkingStrategies_ShouldProduceDifferentResults()
    {
        // Arrange
        var testUrl = "https://docs.centos.org/";
        var strategies = new[] { "FixedSize", "Paragraph" };
        var results = new Dictionary<string, List<WebContentChunk>>();

        // Act - 각 전략으로 동일한 콘텐츠 처리
        foreach (var strategy in strategies)
        {
            var configuration = new WebFluxConfiguration
            {
                Crawling = new CrawlConfiguration
                {
                    StartUrls = new List<string> { testUrl },
                    MaxPages = 1
                },
                Chunking = new ChunkingConfiguration
                {
                    DefaultStrategy = strategy,
                    DefaultChunkSize = 800
                }
            };

            var chunks = new List<WebContentChunk>();
            await foreach (var chunk in _processor.ProcessAsync(configuration))
            {
                chunks.Add(chunk);
                if (chunks.Count >= 5) break; // 테스트 제한
            }

            results[strategy] = chunks;
        }

        // Assert - 전략별 차이점 분석
        Assert.True(results.ContainsKey("FixedSize"));
        Assert.True(results.ContainsKey("Paragraph"));

        var fixedSizeResults = results["FixedSize"];
        var paragraphResults = results["Paragraph"];

        // 청킹 전략 비교 리포팅
        _output.WriteLine($"FixedSize Strategy: {fixedSizeResults.Count} chunks");
        _output.WriteLine($"Paragraph Strategy: {paragraphResults.Count} chunks");

        if (fixedSizeResults.Any() && paragraphResults.Any())
        {
            var fixedAvgSize = fixedSizeResults.Average(c => c.Content.Length);
            var paragraphAvgSize = paragraphResults.Average(c => c.Content.Length);

            _output.WriteLine($"FixedSize avg size: {fixedAvgSize:F0} chars");
            _output.WriteLine($"Paragraph avg size: {paragraphAvgSize:F0} chars");

            // 전략에 따른 차이가 있어야 함
            Assert.True(Math.Abs(fixedAvgSize - paragraphAvgSize) > 50,
                "Different strategies should produce different average chunk sizes");
        }
    }

    private async Task<List<string>> LoadTestUrls()
    {
        try
        {
            var filePath = Path.Combine("..", "..", "..", "..", "target-urls.txt");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine("test", "target-urls.txt");
            }

            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                return lines.Where(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("http"))
                           .ToList();
            }

            _output.WriteLine("target-urls.txt not found, using default URLs");
            return new List<string>
            {
                "https://learn.microsoft.com/ko-kr/windows-server"
            };
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to load test URLs: {ex.Message}");
            return new List<string>
            {
                "https://learn.microsoft.com/ko-kr/windows-server"
            };
        }
    }

    private ChunkQualityMetrics AnalyzeChunkQuality(List<WebContentChunk> chunks)
    {
        var metrics = new ChunkQualityMetrics();

        if (!chunks.Any()) return metrics;

        // 평균 가독성 점수 (단순한 휴리스틱)
        var readabilityScores = chunks.Select(CalculateReadabilityScore);
        metrics.AverageReadability = readabilityScores.Average();

        // 의미있는 청크 비율
        var meaningfulChunks = chunks.Count(c =>
            c.Content.Length >= 100 &&
            c.Content.Split(' ').Length >= 10 &&
            !string.IsNullOrWhiteSpace(c.Content.Trim()));
        metrics.MeaningfulChunkRatio = (double)meaningfulChunks / chunks.Count;

        // 청크 크기 분포
        var sizes = chunks.Select(c => c.Content.Length).ToList();
        metrics.AverageChunkSize = sizes.Average();
        metrics.ChunkSizeStandardDeviation = CalculateStandardDeviation(sizes);

        // 중복 컨텐츠 비율
        var uniqueContents = chunks.Select(c => c.Content.GetHashCode()).Distinct().Count();
        metrics.ContentDuplicationRatio = 1.0 - (double)uniqueContents / chunks.Count;

        return metrics;
    }

    private double CalculateReadabilityScore(WebContentChunk chunk)
    {
        // 단순한 가독성 점수 계산
        var words = chunk.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sentences = chunk.Content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        if (sentences.Length == 0) return 0.0;

        var avgWordsPerSentence = (double)words.Length / sentences.Length;
        var avgCharsPerWord = words.Average(w => w.Length);

        // 간단한 가독성 공식 (낮을수록 읽기 쉬움)
        var readabilityScore = Math.Max(0, 1.0 - (avgWordsPerSentence / 20.0) - (avgCharsPerWord / 10.0));
        return Math.Min(1.0, readabilityScore);
    }

    private double CalculateStandardDeviation(IEnumerable<int> values)
    {
        var enumerable = values.ToList();
        var mean = enumerable.Average();
        var squaredDifferences = enumerable.Select(x => Math.Pow(x - mean, 2));
        var variance = squaredDifferences.Average();
        return Math.Sqrt(variance);
    }

    private async Task ReportTestResults(
        ProcessingMetrics processingMetrics,
        ChunkQualityMetrics qualityMetrics,
        List<WebContentChunk> chunks)
    {
        _output.WriteLine("\n=== 🎯 WebFlux SDK 실제 성능 테스트 결과 ===");
        _output.WriteLine($"⏱️  총 처리 시간: {processingMetrics.TotalProcessingTime.TotalSeconds:F1}초");
        _output.WriteLine($"📊 총 청크 수: {processingMetrics.TotalChunks}개");
        _output.WriteLine($"🚀 처리 속도: {processingMetrics.ProcessingRate:F1} chunks/분");
        _output.WriteLine($"📏 평균 청크 크기: {processingMetrics.AverageChunkSize:F0} 문자");

        _output.WriteLine("\n=== 📋 청크 품질 분석 ===");
        _output.WriteLine($"📚 평균 가독성 점수: {qualityMetrics.AverageReadability:F2}/1.0");
        _output.WriteLine($"✨ 의미있는 청크 비율: {qualityMetrics.MeaningfulChunkRatio:F2}");
        _output.WriteLine($"📐 크기 표준편차: {qualityMetrics.ChunkSizeStandardDeviation:F1}");
        _output.WriteLine($"🔄 중복 컨텐츠 비율: {qualityMetrics.ContentDuplicationRatio:F2}");

        _output.WriteLine("\n=== 📝 샘플 청크 미리보기 ===");
        var sampleChunk = chunks.FirstOrDefault();
        if (sampleChunk != null)
        {
            var preview = sampleChunk.Content.Length > 200
                ? sampleChunk.Content.Substring(0, 200) + "..."
                : sampleChunk.Content;
            _output.WriteLine($"🔸 청크 ID: {sampleChunk.Id}");
            _output.WriteLine($"🔸 전략: {sampleChunk.ChunkingStrategy}");
            _output.WriteLine($"🔸 크기: {sampleChunk.Content.Length} 문자");
            _output.WriteLine($"🔸 내용: {preview}");
        }

        // 성능 목표 대비 평가
        _output.WriteLine("\n=== 🎯 목표 대비 성과 ===");
        var targetRate = 100; // chunks per minute (목표)
        var achievementRate = (processingMetrics.ProcessingRate / targetRate) * 100;
        _output.WriteLine($"📈 처리 속도 달성율: {achievementRate:F1}% (목표: {targetRate} chunks/분)");

        var targetQuality = 0.75; // 목표 품질 점수
        var qualityAchievement = (qualityMetrics.MeaningfulChunkRatio / targetQuality) * 100;
        _output.WriteLine($"🏆 품질 달성율: {qualityAchievement:F1}% (목표: {targetQuality:F2})");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

/// <summary>
/// 처리 성능 메트릭
/// </summary>
public class ProcessingMetrics
{
    public TimeSpan TotalProcessingTime { get; set; }
    public int TotalChunks { get; set; }
    public double AverageChunkSize { get; set; }
    public double ProcessingRate { get; set; } // chunks per minute
}

/// <summary>
/// 청크 품질 메트릭
/// </summary>
public class ChunkQualityMetrics
{
    public double AverageReadability { get; set; }
    public double MeaningfulChunkRatio { get; set; }
    public double AverageChunkSize { get; set; }
    public double ChunkSizeStandardDeviation { get; set; }
    public double ContentDuplicationRatio { get; set; }
}