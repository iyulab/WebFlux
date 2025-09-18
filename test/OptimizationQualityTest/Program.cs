using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Diagnostics;

namespace OptimizationQualityTest;

class Program
{
    private static readonly HttpClient HttpClient = new();
    private static ILogger<Program>? _logger;

    static async Task Main(string[] args)
    {
        // 로깅 설정
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<Program>();

        // 구성 로드
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // .env.local 파일에서 환경 변수 로드
        LoadEnvironmentVariables(".env.local");

        var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(openAiApiKey))
        {
            _logger.LogError("OPENAI_API_KEY가 설정되지 않았습니다. .env.local 파일을 확인하세요.");
            return;
        }

        _logger.LogInformation("=== Phase 5C 최적화 시스템 품질 테스트 시작 ===");
        _logger.LogInformation("사용 모델: {Model}", openAiModel);

        // 테스트 URL 로드
        var testUrls = await LoadTestUrlsAsync();
        if (!testUrls.Any())
        {
            _logger.LogError("테스트 URL을 로드할 수 없습니다.");
            return;
        }

        var overallStopwatch = Stopwatch.StartNew();
        var testResults = new List<TestResult>();

        // 각 URL에 대해 테스트 수행
        foreach (var url in testUrls)
        {
            _logger.LogInformation("테스트 URL: {Url}", url);

            var result = await TestUrlAsync(url, openAiApiKey, openAiModel);
            testResults.Add(result);

            _logger.LogInformation("URL 테스트 완료: {Url} - 성공: {Success}", url, result.Success);

            // API 레이트 리밋을 위한 지연
            await Task.Delay(1000);
        }

        overallStopwatch.Stop();

        // 결과 요약
        await GenerateTestReportAsync(testResults, overallStopwatch.Elapsed);
    }

    private static void LoadEnvironmentVariables(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger?.LogWarning(".env.local 파일을 찾을 수 없습니다: {Path}", filePath);
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }
    }

    private static async Task<List<string>> LoadTestUrlsAsync()
    {
        try
        {
            var urls = await File.ReadAllLinesAsync("target-urls.txt");
            return urls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "target-urls.txt 파일을 읽는 중 오류 발생");
            return new List<string>();
        }
    }

    private static async Task<TestResult> TestUrlAsync(string url, string apiKey, string model)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestResult { Url = url };

        try
        {
            // 1. URL에서 콘텐츠 다운로드
            _logger?.LogInformation("콘텐츠 다운로드 중: {Url}", url);
            var content = await DownloadContentAsync(url);

            if (string.IsNullOrEmpty(content))
            {
                result.ErrorMessage = "콘텐츠를 다운로드할 수 없습니다";
                return result;
            }

            result.ContentLength = content.Length;
            _logger?.LogInformation("콘텐츠 다운로드 완료: {Length} 문자", content.Length);

            // 2. OpenAI API로 콘텐츠 분석 및 청킹 전략 추천
            _logger?.LogInformation("AI 분석 시작...");
            var analysis = await AnalyzeContentWithOpenAIAsync(content, apiKey, model);

            result.AiAnalysis = analysis;
            result.Success = !string.IsNullOrEmpty(analysis);

            // 3. 토큰 수 추정
            result.EstimatedTokens = EstimateTokenCount(content);

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            _logger?.LogInformation("분석 완료: {Tokens} 토큰, {Time}ms",
                result.EstimatedTokens, result.ProcessingTime.TotalMilliseconds);

        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "URL 테스트 중 오류 발생: {Url}", url);
            result.ErrorMessage = ex.Message;
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    private static async Task<string> DownloadContentAsync(string url)
    {
        try
        {
            using var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // HTML 태그 제거 (간단한 구현)
                return System.Text.RegularExpressions.Regex.Replace(content, "<[^>]*>", " ")
                    .Replace("&nbsp;", " ")
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("&amp;", "&");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "콘텐츠 다운로드 오류: {Url}", url);
        }

        return string.Empty;
    }

    private static async Task<string> AnalyzeContentWithOpenAIAsync(string content, string apiKey, string model)
    {
        try
        {
            // 콘텐츠가 너무 길면 자르기 (OpenAI 토큰 제한 고려)
            var maxLength = 8000; // 약 2000 토큰
            if (content.Length > maxLength)
            {
                content = content.Substring(0, maxLength) + "...";
            }

            var requestData = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "당신은 웹 콘텐츠 분석 전문가입니다. 주어진 콘텐츠를 분석하여 최적의 청킹 전략을 추천하세요."
                    },
                    new
                    {
                        role = "user",
                        content = $"다음 웹 콘텐츠를 분석하고 최적의 청킹 전략을 추천해 주세요:\n\n{content}\n\n" +
                                "분석 결과를 다음 형식으로 제공해 주세요:\n" +
                                "1. 콘텐츠 유형: (기술문서/뉴스/학술논문/일반웹페이지 등)\n" +
                                "2. 추천 청킹 전략: (FixedSize/Paragraph/Semantic/Smart 등)\n" +
                                "3. 추천 이유: (왜 이 전략이 적합한지)\n" +
                                "4. 예상 청킹 크기: (문자 또는 토큰 수)\n" +
                                "5. 품질 점수: (1-10점)"
                    }
                },
                max_completion_tokens = 500
            };

            var json = JsonSerializer.Serialize(requestData);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await HttpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        return messageContent.GetString() ?? "";
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger?.LogError("OpenAI API 오류: {StatusCode} - {Content}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "OpenAI API 호출 오류");
        }

        return string.Empty;
    }

    private static int EstimateTokenCount(string text)
    {
        // 간단한 토큰 수 추정 (1 토큰 ≈ 4 문자)
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    private static async Task GenerateTestReportAsync(List<TestResult> results, TimeSpan totalTime)
    {
        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count - successCount;
        var avgProcessingTime = results.Where(r => r.Success).Any()
            ? results.Where(r => r.Success).Average(r => r.ProcessingTime.TotalMilliseconds)
            : 0.0;
        var totalTokens = results.Sum(r => r.EstimatedTokens);

        var report = new StringBuilder();
        report.AppendLine("=== Phase 5C 최적화 시스템 품질 테스트 결과 ===");
        report.AppendLine($"테스트 일시: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"총 테스트 URL 수: {results.Count}");
        report.AppendLine($"성공: {successCount}, 실패: {failureCount}");
        report.AppendLine($"성공률: {(double)successCount / results.Count:P2}");
        report.AppendLine($"총 소요 시간: {totalTime.TotalSeconds:F2}초");
        report.AppendLine($"평균 처리 시간: {avgProcessingTime:F2}ms");
        report.AppendLine($"총 예상 토큰 수: {totalTokens:N0}");
        report.AppendLine();

        report.AppendLine("=== 개별 테스트 결과 ===");
        foreach (var result in results)
        {
            report.AppendLine($"URL: {result.Url}");
            report.AppendLine($"  성공: {result.Success}");
            report.AppendLine($"  콘텐츠 길이: {result.ContentLength:N0} 문자");
            report.AppendLine($"  예상 토큰: {result.EstimatedTokens:N0}");
            report.AppendLine($"  처리 시간: {result.ProcessingTime.TotalMilliseconds:F2}ms");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                report.AppendLine($"  오류: {result.ErrorMessage}");
            }

            if (!string.IsNullOrEmpty(result.AiAnalysis))
            {
                report.AppendLine($"  AI 분석 결과:");
                var analysisLines = result.AiAnalysis.Split('\n');
                foreach (var line in analysisLines.Take(5)) // 처음 5줄만 표시
                {
                    report.AppendLine($"    {line}");
                }
            }
            report.AppendLine();
        }

        var reportContent = report.ToString();
        _logger?.LogInformation(reportContent);

        // 파일로 저장
        var reportFileName = $"optimization_quality_test_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        await File.WriteAllTextAsync(reportFileName, reportContent);
        _logger?.LogInformation("테스트 보고서가 저장되었습니다: {FileName}", reportFileName);
    }
}

public class TestResult
{
    public string Url { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ContentLength { get; set; }
    public int EstimatedTokens { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string AiAnalysis { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}