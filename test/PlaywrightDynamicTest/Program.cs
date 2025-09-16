using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace WebFlux.PlaywrightTest;

/// <summary>
/// Playwright를 사용한 동적 웹 콘텐츠 크롤링 및 OpenAI 통합 테스트
/// JavaScript로 렌더링되는 콘텐츠까지 완전하게 수집
/// </summary>
public class PlaywrightDynamicTest
{
    private static IBrowser? browser;
    private static string apiKey = "";
    private static string model = "";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🎭 WebFlux SDK - Playwright 동적 크롤링 테스트 시작");

        try
        {
            // 환경 변수 로드
            LoadEnvironmentVariables();

            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            model = "gpt-5-nano"; // Override to use a known working model

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OPENAI_API_KEY가 설정되지 않았습니다.");
            }

            Console.WriteLine($"✅ 환경 설정 로드 완료 - Model: {model}");

            // Playwright 설정
            var playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, // 백그라운드 실행
                Args = new[] { "--disable-blink-features=AutomationControlled" } // 봇 감지 회피
            });

            Console.WriteLine("🎭 Playwright 브라우저 시작됨");

            // 테스트 URL들
            var testUrls = new[]
            {
                "https://learn.microsoft.com/ko-kr/windows-server",
                "https://docs.centos.org/",
                "https://techdocs.broadcom.com/us/en/vmware-cis/vsphere/vsphere/8-0.html"
            };

            // 각 URL에 대해 동적 크롤링 테스트
            for (int i = 0; i < Math.Min(testUrls.Length, 2); i++)
            {
                var url = testUrls[i];
                Console.WriteLine($"\n🔗 테스트 {i + 1}: {url}");
                await TestDynamicWebsiteProcessing(url);
            }

            Console.WriteLine("\n🎉 모든 테스트 완료!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 테스트 실패: {ex.Message}");
            Console.WriteLine($"상세 오류: {ex}");
            Environment.Exit(1);
        }
        finally
        {
            if (browser != null)
            {
                await browser.CloseAsync();
                Console.WriteLine("🎭 브라우저 종료됨");
            }
        }
    }

    private static async Task TestDynamicWebsiteProcessing(string url)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // 새 페이지 생성
            var page = await browser!.NewPageAsync();

            Console.WriteLine($"  📥 페이지 로딩 시작...");

            // 페이지 로드 최적화 설정
            await page.SetViewportSizeAsync(1920, 1080);
            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            // 페이지 로드 및 동적 콘텐츠 대기
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            // JavaScript 실행 완료까지 추가 대기
            await page.WaitForTimeoutAsync(3000);

            Console.WriteLine($"  ✅ 페이지 로드 완료");

            // 동적으로 렌더링된 전체 콘텐츠 수집
            var pageContent = await page.ContentAsync();
            var visibleText = await page.InnerTextAsync("body");

            // 메타데이터 추출 (안전하게)
            var title = await page.TitleAsync();
            string description = "";
            string keywords = "";

            try
            {
                var descriptionElement = await page.QuerySelectorAsync("meta[name='description']");
                description = descriptionElement != null ? await descriptionElement.GetAttributeAsync("content") ?? "" : "";
            }
            catch { }

            try
            {
                var keywordsElement = await page.QuerySelectorAsync("meta[name='keywords']");
                keywords = keywordsElement != null ? await keywordsElement.GetAttributeAsync("content") ?? "" : "";
            }
            catch { }

            Console.WriteLine($"  🏷️ 제목: {title}");
            Console.WriteLine($"  📄 HTML 크기: {pageContent.Length:N0} 문자");
            Console.WriteLine($"  📝 가시 텍스트: {visibleText.Length:N0} 문자");

            // 추가 동적 요소 감지 및 대기
            var dynamicElements = await DetectDynamicContent(page);
            if (dynamicElements.Count > 0)
            {
                Console.WriteLine($"  ⚡ 동적 요소 {dynamicElements.Count}개 감지됨");

                // 동적 콘텐츠가 완전히 로드될 때까지 추가 대기
                await page.WaitForTimeoutAsync(2000);

                // 업데이트된 콘텐츠 다시 수집
                visibleText = await page.InnerTextAsync("body");
                Console.WriteLine($"  🔄 동적 로딩 후 텍스트: {visibleText.Length:N0} 문자");
            }

            // 텍스트 정제 및 청킹
            var cleanContent = CleanExtractedText(visibleText);
            var chunks = CreateTextChunks(cleanContent, 2000);

            Console.WriteLine($"  🧹 정제된 텍스트: {cleanContent.Length:N0} 문자");
            Console.WriteLine($"  📦 생성된 청크: {chunks.Count}개");

            // 첫 번째 청크로 AI 요약 테스트
            if (chunks.Count > 0)
            {
                var firstChunk = chunks[0];
                var summary = await SummarizeWithOpenAI(firstChunk);

                Console.WriteLine($"  🤖 AI 요약 완료 ({summary.Length:N0} 문자)");
                Console.WriteLine($"  📝 요약: {summary.Substring(0, Math.Min(150, summary.Length))}...");
            }

            var processingTime = DateTime.UtcNow - startTime;
            var processingRate = cleanContent.Length / processingTime.TotalMinutes;

            // 성능 메트릭
            Console.WriteLine($"  ⏱️ 총 처리 시간: {processingTime.TotalSeconds:F1}초");
            Console.WriteLine($"  📊 처리 속도: {processingRate:F0} 문자/분");
            Console.WriteLine($"  🎯 품질 점수: {CalculateContentQuality(cleanContent):F2}/1.0");

            await page.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 처리 실패: {ex.Message}");
        }
    }

    private static async Task<List<string>> DetectDynamicContent(IPage page)
    {
        var dynamicSelectors = new[]
        {
            "[data-loading]",
            "[data-lazy]",
            ".lazy",
            ".loading",
            ".skeleton",
            ".spinner",
            "[aria-busy='true']"
        };

        var foundElements = new List<string>();

        foreach (var selector in dynamicSelectors)
        {
            try
            {
                var elements = await page.QuerySelectorAllAsync(selector);
                if (elements.Count > 0)
                {
                    foundElements.Add($"{selector}: {elements.Count}개");
                }
            }
            catch
            {
                // 선택자가 유효하지 않을 수 있음
            }
        }

        // Intersection Observer로 감지되는 요소들 대기
        await page.EvaluateAsync(@"() => {
            return new Promise(resolve => {
                let loadedElements = 0;
                const observer = new IntersectionObserver((entries) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            loadedElements++;
                        }
                    });
                });

                document.querySelectorAll('img[loading=""lazy""], iframe[loading=""lazy""]').forEach(el => {
                    observer.observe(el);
                });

                setTimeout(() => resolve(loadedElements), 2000);
            });
        }");

        return foundElements;
    }

    private static string CleanExtractedText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        // 불필요한 공백 및 특수문자 정리
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"[\r\n]+", "\n");

        // 네비게이션, 푸터 등 일반적인 노이즈 제거
        var lines = text.Split('\n');
        var cleanLines = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => line.Length > 10) // 너무 짧은 라인 제거
            .Where(line => !IsNavigationNoise(line))
            .ToList();

        return string.Join("\n", cleanLines).Trim();
    }

    private static bool IsNavigationNoise(string line)
    {
        var noisePatterns = new[]
        {
            "cookie", "privacy", "terms", "skip to main",
            "navigation", "menu", "search", "login", "sign in"
        };

        return noisePatterns.Any(pattern =>
            line.ToLower().Contains(pattern) && line.Length < 50);
    }

    private static List<string> CreateTextChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = "";

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > chunkSize && !string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
                currentChunk = paragraph;
            }
            else
            {
                currentChunk += (string.IsNullOrEmpty(currentChunk) ? "" : "\n") + paragraph;
            }
        }

        if (!string.IsNullOrEmpty(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }

        return chunks;
    }

    private static double CalculateContentQuality(string content)
    {
        if (string.IsNullOrEmpty(content)) return 0.0;

        var score = 0.0;
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 길이 점수 (적절한 길이)
        if (content.Length > 500) score += 0.3;
        if (content.Length > 1000) score += 0.2;

        // 단어 다양성 점수
        var uniqueWords = words.Distinct().Count();
        var diversity = (double)uniqueWords / Math.Max(words.Length, 1);
        score += Math.Min(diversity * 0.3, 0.3);

        // 문장 구조 점수
        var sentences = content.Split('.', '!', '?').Length;
        if (sentences > 3) score += 0.2;

        return Math.Min(score, 1.0);
    }

    private static async Task<string> SummarizeWithOpenAI(string content)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var prompt = $"다음 웹 페이지 내용을 한국어로 간결하게 요약해 주세요. 주요 키워드와 핵심 내용을 포함하여 200자 내외로 요약하세요:\n\n{content}";

        object requestData;

        if (model.Contains("gpt-5"))
        {
            requestData = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "당신은 웹 콘텐츠 요약 전문가입니다. 주어진 내용을 간결하고 정확하게 요약합니다." },
                    new { role = "user", content = prompt }
                },
                max_completion_tokens = 300
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
        Console.WriteLine($"  🔍 API Response JSON: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");

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
        }
    }
}