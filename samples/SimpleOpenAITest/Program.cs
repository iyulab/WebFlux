using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using WebFlux.Services;
using WebFlux.SimpleTest.Services;
using ProcessingResult = WebFlux.SimpleTest.Models.ProcessingResult;

namespace WebFlux.SimpleTest;

/// <summary>
/// WebFlux SDK의 통합 파이프라인을 사용한 테스트 (Phase 1: Dynamic Rendering + AI Enhancement)
/// target-urls.txt 파일에서 URL 목록을 로드하여 순차 처리
/// Playwright 크롤러와 AI 증강 서비스를 활용한 고급 웹 콘텐츠 처리
/// </summary>
public class SimpleOpenAITest
{

    public static async Task Main(string[] args)
    {
        var sessionStart = DateTime.UtcNow;
        Console.WriteLine("WebFlux SDK - Phase 1 Integration Test");
        Console.WriteLine("Dynamic Rendering (Playwright) + AI Enhancement\n");

        try
        {
            // 환경 변수 로드
            LoadEnvironmentVariables();

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-5-nano";

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY not found");
            }

            Console.WriteLine($"→ Model: {model}");

            // 출력 디렉토리 설정
            var outputBaseDir = Path.Combine(AppContext.BaseDirectory, "output");
            var outputManager = new OutputManager(outputBaseDir);

            // DI 컨테이너 설정 - WebFlux SDK 등록
            var services = new ServiceCollection();

            // 콘솔 로깅 추가 (간결한 포맷)
            services.AddLogging(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.IncludeScopes = false;
                    options.TimestampFormat = "";
                });
                builder.SetMinimumLevel(LogLevel.Information);

                // 디버깅을 위해 모든 로그 출력
                // builder.AddFilter("WebFlux.Services.Crawlers.PlaywrightCrawler", LogLevel.Warning);
                // builder.AddFilter("WebFlux.Services.AiEnhancement", LogLevel.Warning);
            });

            // WebFlux SDK 등록 (Phase 1: Dynamic Rendering + AI Enhancement)
            services.AddWebFlux(config =>
            {
                config.Crawling.Strategy = "Dynamic";
                config.Crawling.DefaultTimeoutSeconds = 30;
                config.Crawling.DefaultDelayMs = 500;

                config.AiEnhancement.Enabled = true;
                config.AiEnhancement.EnableSummary = true;
                config.AiEnhancement.EnableMetadata = true;
                config.AiEnhancement.EnableRewrite = false;
                config.AiEnhancement.EnableParallelProcessing = true;

                config.Chunking.DefaultStrategy = "Paragraph";
                config.Chunking.MaxChunkSize = 1000;
                config.Chunking.MinChunkSize = 100;
            });

            // AI 서비스 구현체 등록 (OpenAI)
            services.AddSingleton<ITextCompletionService>(sp => new OpenAiTextCompletionService(model, apiKey));

            // AI 증강 서비스 등록
            services.AddWebFluxAIEnhancement();

            // 서비스 프로바이더 빌드
            var serviceProvider = services.BuildServiceProvider();

            // WebFlux 처리기 가져오기
            var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();
            var llmService = serviceProvider.GetRequiredService<ITextCompletionService>();
            var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();

            // 실시간 진행 상황 이벤트 구독
            SubscribeToProgressEvents(eventPublisher);

            // 서비스 상태 확인
            var healthInfo = llmService.GetHealthInfo();
            Console.WriteLine($"→ AI Service: {healthInfo.Status} ({healthInfo.Metadata["Provider"]})\n");

            // 테스트 URL 로드
            var testUrls = await LoadTestUrls();
            Console.WriteLine($"Processing {testUrls.Count} URL(s)\n");

            // 모든 URL을 순회하면서 테스트하고 결과 수집
            var results = new List<ProcessingResult>();
            int urlIndex = 1;

            foreach (var url in testUrls)
            {
                var urlId = $"url-{urlIndex:D3}";
                Console.WriteLine($"[{urlId}] {url}");

                try
                {
                    var result = await TestWebsiteProcessing(url, urlId, processor, outputManager);
                    results.Add(result);
                    await outputManager.SaveProcessingResultAsync(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed: {ex.Message}");

                    var errorResult = new ProcessingResult
                    {
                        Url = url,
                        UrlId = urlId,
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow,
                        OriginalHtml = "",
                        ExtractedText = "",
                        TruncatedText = "",
                        ErrorMessage = ex.ToString()
                    };
                    results.Add(errorResult);
                    await outputManager.SaveProcessingResultAsync(errorResult);
                }

                Console.WriteLine();
                urlIndex++;
            }

            var sessionEnd = DateTime.UtcNow;

            // 세션 전체 요약 저장
            await outputManager.SaveSessionSummaryAsync(results, model, sessionStart, sessionEnd);

            // 콘솔 결과 요약
            var successCount = results.Count(r => r.IsSuccess);
            var failureCount = results.Count - successCount;

            Console.WriteLine("Summary");
            Console.WriteLine($"  Success: {successCount}/{testUrls.Count} ({(testUrls.Count > 0 ? (double)successCount / testUrls.Count * 100 : 0):F0}%)");
            Console.WriteLine($"  Duration: {(sessionEnd - sessionStart).TotalSeconds:F1}s");
            Console.WriteLine($"  Output: {outputManager.GetSessionDirectory()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Test failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void LoadEnvironmentVariables()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env.local"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", ".env.local"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env.local"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env.local"),
            "D:\\data\\WebFlux\\.env.local"
        };

        string envPath = "";
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                envPath = path;
                break;
            }
        }

        if (!string.IsNullOrEmpty(envPath))
        {
            var lines = File.ReadAllLines(envPath);
            foreach (var line in lines)
            {
                if (line.Contains("=") && !line.StartsWith("#"))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                    }
                }
            }
        }
    }

    private static async Task<List<string>> LoadTestUrls()
    {
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "target-urls.txt"),
            Path.Combine(AppContext.BaseDirectory, "target-urls.txt"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "target-urls.txt"),
        };

        string? urlsPath = null;
        foreach (var path in possiblePaths)
        {
            var normalizedPath = Path.GetFullPath(path);
            if (File.Exists(normalizedPath))
            {
                urlsPath = normalizedPath;
                break;
            }
        }

        if (urlsPath != null)
        {
            var lines = await File.ReadAllLinesAsync(urlsPath);
            var urls = lines
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#") && line.StartsWith("http"))
                .ToList();

            if (urls.Count > 0)
            {
                return urls;
            }
        }

        return new List<string> { "https://learn.microsoft.com/ko-kr/windows-server" };
    }

    private static void SubscribeToProgressEvents(IEventPublisher eventPublisher)
    {
        eventPublisher.Subscribe<ProcessingEvent>(evt =>
        {
            if (evt.EventType == "ProcessingProgress")
            {
                var progressEvt = evt as ProcessingProgressEvent;
                if (progressEvt != null)
                {
                    Console.WriteLine($"  → {progressEvt.CurrentStage}: {progressEvt.ProcessedCount} processed");
                }
            }

            return Task.CompletedTask;
        });
    }

    private static async Task<ProcessingResult> TestWebsiteProcessing(string url, string urlId, IWebContentProcessor processor, OutputManager outputManager)
    {
        var startTime = DateTime.UtcNow;

        // 청킹 옵션 설정
        var chunkingOptions = new ChunkingOptions
        {
            Strategy = ChunkingStrategyType.Paragraph,
            MaxChunkSize = 1000,
            MinChunkSize = 100,
            ChunkOverlap = 50
        };

        // WebFlux 통합 파이프라인 실행: ProcessUrlAsync
        // Crawling → Extraction → AI Enhancement → Chunking
        var chunks = await processor.ProcessUrlAsync(url, chunkingOptions);

        var endTime = DateTime.UtcNow;
        var processingTime = endTime - startTime;

        // 청크 파일 저장 (세션 디렉토리에)
        if (chunks.Count > 0)
        {
            await SaveChunkFiles(chunks, urlId, url, outputManager);
        }

        // 메타데이터에서 정보 추출
        string? extractedText = null;
        string? aiSummary = null;
        string? originalHtml = null;

        if (chunks.Count > 0)
        {
            var firstChunk = chunks[0];

            extractedText = firstChunk.AdditionalMetadata.ContainsKey("extracted_text")
                ? firstChunk.AdditionalMetadata["extracted_text"]?.ToString()
                : string.Join("\n\n", chunks.Select(c => c.Content));

            aiSummary = firstChunk.AdditionalMetadata.ContainsKey("ai_summary")
                ? firstChunk.AdditionalMetadata["ai_summary"]?.ToString()
                : null;

            originalHtml = firstChunk.AdditionalMetadata.ContainsKey("original_html")
                ? firstChunk.AdditionalMetadata["original_html"]?.ToString()
                : null;
        }

        // 간결한 결과 출력
        Console.WriteLine($"  ✓ Completed: {chunks.Count} chunks, {extractedText?.Length ?? 0:N0} chars in {processingTime.TotalSeconds:F1}s");

        if (!string.IsNullOrEmpty(aiSummary))
        {
            var summaryPreview = aiSummary.Replace("\n", " ");
            if (summaryPreview.Length > 120)
            {
                summaryPreview = summaryPreview.Substring(0, 120) + "...";
            }
            Console.WriteLine($"  → Summary: {summaryPreview}");
        }

        // ProcessingResult 생성 및 반환
        var truncatedText = extractedText?.Length > 3000
            ? extractedText.Substring(0, 3000) + "..."
            : extractedText ?? "";

        return new ProcessingResult
        {
            Url = url,
            UrlId = urlId,
            StartTime = startTime,
            EndTime = endTime,
            HttpStatusCode = 200,
            OriginalHtml = originalHtml ?? "",
            ExtractedText = extractedText ?? "",
            TruncatedText = truncatedText,
            Summary = aiSummary ?? ""
        };
    }

    private static async Task SaveChunkFiles(IReadOnlyList<WebContentChunk> chunks, string urlId, string url, OutputManager outputManager)
    {
        // URL별 디렉토리 경로 생성 (OutputManager와 동일한 규칙 사용)
        var urlDirName = SanitizeDirectoryName($"{urlId}_{GetDomainFromUrl(url)}");
        var urlDir = Path.Combine(outputManager.GetSessionDirectory(), urlDirName);

        // chunks 하위 디렉토리 생성
        var chunksDir = Path.Combine(urlDir, "chunks");
        Directory.CreateDirectory(chunksDir);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 각 청크를 개별 JSON 파일로 저장
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var chunkFileName = $"chunk-{i + 1:D4}.json";
            var chunkFilePath = Path.Combine(chunksDir, chunkFileName);

            var chunkJson = JsonSerializer.Serialize(chunk, jsonOptions);
            await File.WriteAllTextAsync(chunkFilePath, chunkJson);
        }

        Console.WriteLine($"  📦 {chunks.Count}개 청크 파일 저장 완료");
    }

    private static string GetDomainFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return "unknown";
        }
    }

    private static string SanitizeDirectoryName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
    }

}