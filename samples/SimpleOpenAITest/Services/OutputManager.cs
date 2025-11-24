using System.Text.Json;
using WebFlux.SimpleTest.Models;

namespace WebFlux.SimpleTest.Services;

/// <summary>
/// 처리 결과를 구조적으로 파일 시스템에 저장하는 매니저
/// </summary>
public class OutputManager
{
    private readonly string _sessionDir;
    private readonly string _sessionId;
    private readonly JsonSerializerOptions _jsonOptions;

    public OutputManager(string baseOutputDir)
    {
        _sessionId = $"session_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        _sessionDir = Path.Combine(baseOutputDir, _sessionId);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 세션 디렉토리 생성
        Directory.CreateDirectory(_sessionDir);

        Console.WriteLine($"📁 출력 디렉토리 생성: {_sessionDir}\n");
    }

    /// <summary>
    /// URL 처리 결과를 구조적으로 저장
    /// </summary>
    public async Task SaveProcessingResultAsync(ProcessingResult result)
    {
        // URL별 디렉토리 생성 (안전한 디렉토리명)
        var urlDirName = SanitizeDirectoryName($"{result.UrlId}_{GetDomainFromUrl(result.Url)}");
        var urlDir = Path.Combine(_sessionDir, urlDirName);
        Directory.CreateDirectory(urlDir);

        // 1. 메타데이터 저장
        var metadata = new
        {
            url = result.Url,
            urlId = result.UrlId,
            startTime = result.StartTime,
            endTime = result.EndTime,
            processingTimeSeconds = result.ProcessingTime?.TotalSeconds,
            httpStatusCode = result.HttpStatusCode,
            isSuccess = result.IsSuccess,
            errorMessage = result.ErrorMessage,
            statistics = new
            {
                originalLength = result.OriginalLength,
                extractedLength = result.ExtractedLength,
                truncatedLength = result.TruncatedLength,
                summaryLength = result.SummaryLength,
                processingRateCharsPerMin = result.ProcessingRate
            }
        };

        await File.WriteAllTextAsync(
            Path.Combine(urlDir, "metadata.json"),
            JsonSerializer.Serialize(metadata, _jsonOptions));

        // 2. 원본 HTML 저장
        await File.WriteAllTextAsync(
            Path.Combine(urlDir, "01-original.html"),
            result.OriginalHtml);

        // 3. 추출된 텍스트 저장
        await File.WriteAllTextAsync(
            Path.Combine(urlDir, "02-extracted.txt"),
            result.ExtractedText);

        // 4. 잘린 텍스트 저장 (AI 입력)
        await File.WriteAllTextAsync(
            Path.Combine(urlDir, "03-truncated.txt"),
            result.TruncatedText);

        // 5. AI 요약 저장
        if (!string.IsNullOrEmpty(result.Summary))
        {
            await File.WriteAllTextAsync(
                Path.Combine(urlDir, "04-summary.txt"),
                result.Summary);
        }

        // 6. 에러 정보 저장 (있는 경우)
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            await File.WriteAllTextAsync(
                Path.Combine(urlDir, "error.txt"),
                $"Error occurred at: {result.EndTime}\n\n{result.ErrorMessage}");
        }

        Console.WriteLine($"  💾 결과 저장 완료: {urlDirName}");
    }

    /// <summary>
    /// 세션 전체 요약 저장
    /// </summary>
    public async Task SaveSessionSummaryAsync(
        IReadOnlyList<ProcessingResult> results,
        string model,
        DateTime sessionStart,
        DateTime sessionEnd)
    {
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count - successCount;
        var totalProcessingTime = sessionEnd - sessionStart;

        // 세션 정보 JSON
        var sessionInfo = new
        {
            sessionId = _sessionId,
            sessionStart,
            sessionEnd,
            totalProcessingTimeSeconds = totalProcessingTime.TotalSeconds,
            model,
            totalUrls = results.Count,
            successCount,
            failureCount,
            successRate = results.Count > 0 ? (double)successCount / results.Count * 100 : 0,
            results = results.Select(r => new
            {
                urlId = r.UrlId,
                url = r.Url,
                isSuccess = r.IsSuccess,
                processingTimeSeconds = r.ProcessingTime?.TotalSeconds,
                processingRate = r.ProcessingRate,
                summaryLength = r.SummaryLength
            }).ToList()
        };

        await File.WriteAllTextAsync(
            Path.Combine(_sessionDir, "session-info.json"),
            JsonSerializer.Serialize(sessionInfo, _jsonOptions));

        // 마크다운 요약 리포트
        var reportLines = new List<string>
        {
            $"# WebFlux SDK 테스트 세션 리포트",
            "",
            $"**세션 ID**: `{_sessionId}`  ",
            $"**시작 시간**: {sessionStart:yyyy-MM-dd HH:mm:ss}  ",
            $"**종료 시간**: {sessionEnd:yyyy-MM-dd HH:mm:ss}  ",
            $"**총 처리 시간**: {totalProcessingTime.TotalSeconds:F1}초  ",
            $"**모델**: {model}  ",
            "",
            "## 📊 처리 결과 요약",
            "",
            $"- **총 URL 수**: {results.Count}개",
            $"- **성공**: {successCount}개",
            $"- **실패**: {failureCount}개",
            $"- **성공률**: {(results.Count > 0 ? (double)successCount / results.Count * 100 : 0):F1}%",
            "",
            "## 📋 URL별 상세 결과",
            ""
        };

        foreach (var result in results)
        {
            var status = result.IsSuccess ? "✅" : "❌";
            reportLines.Add($"### {status} {result.UrlId} - {GetDomainFromUrl(result.Url)}");
            reportLines.Add("");
            reportLines.Add($"**URL**: {result.Url}  ");
            reportLines.Add($"**처리 시간**: {result.ProcessingTime?.TotalSeconds:F1}초  ");

            if (result.IsSuccess)
            {
                reportLines.Add($"**처리 속도**: {result.ProcessingRate:F0} 문자/분  ");
                reportLines.Add($"**원본 크기**: {result.OriginalLength:N0} 문자  ");
                reportLines.Add($"**추출 크기**: {result.ExtractedLength:N0} 문자  ");
                reportLines.Add($"**요약 크기**: {result.SummaryLength:N0} 문자  ");
                reportLines.Add("");
                reportLines.Add("**요약 내용**:");
                reportLines.Add("```");
                reportLines.Add(result.Summary ?? "");
                reportLines.Add("```");
            }
            else
            {
                reportLines.Add($"**오류**: {result.ErrorMessage}");
            }

            reportLines.Add("");
        }

        // 통계 섹션
        if (successCount > 0)
        {
            var avgProcessingTime = results.Where(r => r.IsSuccess)
                .Average(r => r.ProcessingTime?.TotalSeconds ?? 0);
            var avgProcessingRate = results.Where(r => r.IsSuccess)
                .Average(r => r.ProcessingRate);
            var totalCharsProcessed = results.Where(r => r.IsSuccess)
                .Sum(r => r.ExtractedLength);

            reportLines.Add("## 📈 성능 통계");
            reportLines.Add("");
            reportLines.Add($"- **평균 처리 시간**: {avgProcessingTime:F1}초");
            reportLines.Add($"- **평균 처리 속도**: {avgProcessingRate:F0} 문자/분");
            reportLines.Add($"- **총 처리 문자 수**: {totalCharsProcessed:N0} 문자");
            reportLines.Add("");
        }

        // 파일 저장
        await File.WriteAllLinesAsync(
            Path.Combine(_sessionDir, "summary.md"),
            reportLines);

        Console.WriteLine($"\n📝 세션 요약 저장 완료: {Path.Combine(_sessionDir, "summary.md")}");
        Console.WriteLine($"📊 세션 정보 저장 완료: {Path.Combine(_sessionDir, "session-info.json")}");
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

    public string GetSessionDirectory() => _sessionDir;
}
