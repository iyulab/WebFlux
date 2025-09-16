using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace WebFlux.SimpleTest;

/// <summary>
/// OpenAI API와 실제 웹 사이트에 대한 간단한 통합 테스트
/// 복잡한 인터페이스 구현 없이 핵심 기능 검증
/// </summary>
public class SimpleOpenAITest
{
    private static readonly HttpClient httpClient = new();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 WebFlux SDK - OpenAI API 실제 테스트 시작");

        try
        {
            // 환경 변수 로드
            LoadEnvironmentVariables();

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-3.5-turbo";

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY가 설정되지 않았습니다.");
            }

            Console.WriteLine($"✅ 환경 설정 로드 완료 - Model: {model}");

            // HTTP 클라이언트 설정
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "WebFlux-SDK/1.0");
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            // 테스트 URL 로드
            var testUrls = await LoadTestUrls();
            Console.WriteLine($"📋 테스트 URL {testUrls.Count}개 로드됨");

            // 각 URL에 대해 테스트 수행
            for (int i = 0; i < Math.Min(testUrls.Count, 2); i++) // 처음 2개만 테스트
            {
                var url = testUrls[i];
                Console.WriteLine($"\n🔗 테스트 {i + 1}: {url}");
                await TestWebsiteProcessing(url, apiKey, model);
            }

            Console.WriteLine("\n🎉 모든 테스트 완료!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 테스트 실패: {ex.Message}");
            Console.WriteLine($"상세 오류: {ex}");
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
            Console.WriteLine($"✅ 환경 변수 파일 로드: {envPath}");
        }
        else
        {
            Console.WriteLine($"⚠️ 환경 변수 파일을 찾을 수 없음: .env.local");
            Console.WriteLine($"현재 디렉토리: {Directory.GetCurrentDirectory()}");
        }
    }

    private static async Task<List<string>> LoadTestUrls()
    {
        var urlsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "test", "target-urls.txt");

        // 상위 디렉토리에서 target-urls.txt 파일 찾기
        for (int i = 0; i < 5; i++)
        {
            if (File.Exists(urlsPath))
            {
                break;
            }
            urlsPath = Path.Combine(Path.GetDirectoryName(urlsPath)!, "..", "target-urls.txt");
        }

        if (File.Exists(urlsPath))
        {
            var lines = await File.ReadAllLinesAsync(urlsPath);
            return lines.Where(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("http"))
                       .ToList();
        }
        else
        {
            Console.WriteLine($"⚠️ 테스트 URL 파일을 찾을 수 없음, 기본 URL 사용");
            return new List<string>
            {
                "https://learn.microsoft.com/ko-kr/windows-server"
            };
        }
    }

    private static async Task TestWebsiteProcessing(string url, string apiKey, string model)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            Console.WriteLine($"  📥 웹 페이지 가져오는 중...");

            // 1단계: 웹 페이지 크롤링
            var webContent = await httpClient.GetStringAsync(url);
            Console.WriteLine($"  ✅ 페이지 로드 완료 ({webContent.Length:N0} 문자)");

            // 2단계: 콘텐츠 추출 (간단한 HTML 태그 제거)
            var cleanContent = ExtractTextFromHtml(webContent);
            var truncatedContent = cleanContent.Length > 3000
                ? cleanContent.Substring(0, 3000) + "..."
                : cleanContent;

            Console.WriteLine($"  🧹 텍스트 추출 완료 ({cleanContent.Length:N0} 문자 → {truncatedContent.Length:N0} 문자)");

            // 3단계: AI 요약 수행
            var summary = await SummarizeWithOpenAI(truncatedContent, apiKey, model);
            Console.WriteLine($"  🤖 AI 요약 완료 ({summary.Length:N0} 문자)");

            var processingTime = DateTime.UtcNow - startTime;

            // 결과 출력
            Console.WriteLine($"  ⏱️ 처리 시간: {processingTime.TotalSeconds:F1}초");
            Console.WriteLine($"  📝 요약 내용:");
            Console.WriteLine($"     {summary.Replace("\n", " ").Substring(0, Math.Min(200, summary.Length))}...");

            // 성능 평가
            var processingRate = cleanContent.Length / processingTime.TotalMinutes;
            Console.WriteLine($"  📊 처리 속도: {processingRate:F0} 문자/분");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 처리 실패: {ex.Message}");
        }
    }

    private static string ExtractTextFromHtml(string html)
    {
        // 간단한 HTML 태그 제거
        var text = html;

        // Script, style 태그 내용 제거
        text = Regex.Replace(text, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = Regex.Replace(text, @"<style[^>]*?>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // HTML 태그 제거
        text = Regex.Replace(text, @"<[^>]+>", " ");

        // HTML 엔티티 디코딩
        text = System.Net.WebUtility.HtmlDecode(text);

        // 공백 정규화
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    private static async Task<string> SummarizeWithOpenAI(string content, string apiKey, string model)
    {
        var prompt = $"다음 웹 페이지 내용을 한국어로 간결하게 요약해 주세요. 주요 내용과 키워드를 포함하여 200자 내외로 요약하세요:\n\n{content}";

        object requestData;

        if (model.Contains("gpt-5"))
        {
            // gpt-5 모델은 특별한 파라미터 요구사항이 있음
            requestData = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "당신은 웹 콘텐츠 요약 전문가입니다. 주어진 내용을 간결하고 정확하게 요약합니다." },
                    new { role = "user", content = prompt }
                },
                max_completion_tokens = 300
                // temperature는 기본값 1만 지원
            };
        }
        else if (model.Contains("gpt-4o"))
        {
            requestData = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "당신은 웹 콘텐츠 요약 전문가입니다. 주어진 내용을 간결하고 정확하게 요약합니다." },
                    new { role = "user", content = prompt }
                },
                max_completion_tokens = 300,
                temperature = 0.3
            };
        }
        else
        {
            // 기존 모델들 (gpt-3.5-turbo, gpt-4 등)
            requestData = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "당신은 웹 콘텐츠 요약 전문가입니다. 주어진 내용을 간결하고 정확하게 요약합니다." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 300,
                temperature = 0.3
            };
        }

        var json = JsonSerializer.Serialize(requestData);
        var httpContent = new StringContent(json, System.Text.Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"OpenAI API 오류: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

        if (responseData.TryGetProperty("choices", out var choices) &&
            choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var responseContent))
            {
                return responseContent.GetString() ?? "요약을 생성할 수 없습니다.";
            }
        }

        throw new InvalidOperationException("OpenAI API 응답 형식이 올바르지 않습니다.");
    }
}