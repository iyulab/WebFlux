using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services;

namespace WebFlux.Examples.DynamicCrawling;

/// <summary>
/// 동적 웹 크롤링 예제
/// Microsoft Playwright를 사용하여 JavaScript로 렌더링되는 동적 페이지를 크롤링합니다.
/// React, Vue, Angular 등 SPA 웹사이트에 적합합니다.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WebFlux SDK - 동적 크롤링 예제 (Playwright) ===\n");

        // 1. Playwright 설치 확인
        Console.WriteLine("📦 Playwright 브라우저 설치 확인 중...");
        try
        {
            // Playwright 브라우저 자동 설치 (처음 실행 시)
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
            if (exitCode == 0)
            {
                Console.WriteLine("✅ Playwright 브라우저 준비 완료\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Playwright 설치 오류: {ex.Message}");
            Console.WriteLine("   수동 설치: pwsh bin/Debug/net10.0/playwright.ps1 install chromium\n");
        }

        // 2. 서비스 컬렉션 구성
        var services = new ServiceCollection();

        // WebFlux + Playwright 서비스 등록
        services.AddWebFlux(options =>
        {
            options.MaxConcurrency = 2;  // 동적 크롤링은 리소스 사용량이 많으므로 동시 실행 제한
            options.UserAgent = "WebFlux-Playwright-Example/1.0";
            options.RequestDelay = TimeSpan.FromSeconds(1);  // 동적 페이지는 더 긴 대기 시간 필요
        });

        // Playwright 지원 활성화
        services.AddWebFluxPlaywright();

        var serviceProvider = services.BuildServiceProvider();

        // 3. WebContentProcessor 서비스 가져오기
        var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();

        // 4. 동적 페이지 URL 정의 (JavaScript로 렌더링되는 SPA 웹사이트)
        var urls = new[]
        {
            "https://react.dev/learn",  // React 공식 문서 (React SPA)
            "https://vuejs.org/guide/introduction.html"  // Vue.js 공식 문서 (Vue SPA)
        };

        Console.WriteLine($"동적 크롤링 시작: {urls.Length}개 SPA 페이지\n");

        // 5. 동적 크롤링 옵션 구성
        var crawlOptions = new CrawlOptions
        {
            MaxDepth = 0,
            FollowExternalLinks = false,
            RespectRobotsTxt = true,
            Timeout = TimeSpan.FromSeconds(60),  // 동적 페이지는 더 긴 타임아웃 필요

            // Playwright 전용 옵션
            UseDynamicCrawling = true,  // 동적 크롤링 활성화
            WaitForNetworkIdle = true,  // 네트워크 요청 완료 대기
            WaitForSelector = "main, article, .content",  // 메인 콘텐츠 로딩 대기
            JavaScriptEnabled = true,
            HeadlessMode = true  // 백그라운드 실행 (디버깅 시 false로 설정)
        };

        // 6. 청킹 옵션 구성 (Smart 전략 사용)
        var chunkingOptions = new ChunkingOptions
        {
            MaxChunkSize = 768,  // 동적 페이지는 더 많은 구조가 있을 수 있음
            MinChunkSize = 150,
            ChunkOverlap = 100,
            Strategy = "smart"  // 구조 인식 청킹 전략
        };

        try
        {
            // 7. 동적 크롤링 및 청킹 실행
            Console.WriteLine("🌐 브라우저 자동화 시작...\n");

            var results = await processor.ProcessUrlsAsync(
                urls,
                crawlOptions,
                chunkingOptions
            );

            // 8. 결과 출력 및 분석
            Console.WriteLine($"\n✅ 동적 크롤링 완료!\n");

            int totalChunks = 0;
            int totalImages = 0;

            foreach (var result in results)
            {
                Console.WriteLine($"📄 URL: {result.Url}");
                Console.WriteLine($"   제목: {result.Title}");
                Console.WriteLine($"   프레임워크 감지: {DetectFramework(result.Metadata)}");
                Console.WriteLine($"   청크 수: {result.Chunks.Count}");
                Console.WriteLine($"   이미지 수: {result.ImageUrls?.Count ?? 0}");
                Console.WriteLine($"   원본 크기: {result.OriginalSize:N0} 문자");
                Console.WriteLine($"   처리 시간: {result.ProcessingTime.TotalSeconds:F2}초");

                // 구조 분석
                var headingCount = result.Chunks.Count(c => c.Metadata.ContainsKey("HeadingLevel"));
                var codeBlockCount = result.Chunks.Count(c => c.Metadata.ContainsKey("IsCodeBlock"));

                Console.WriteLine($"   구조:");
                Console.WriteLine($"      - 헤딩 청크: {headingCount}");
                Console.WriteLine($"      - 코드 블록: {codeBlockCount}");

                // 첫 번째 청크 미리보기
                if (result.Chunks.Any())
                {
                    var firstChunk = result.Chunks.First();
                    var preview = firstChunk.Content.Substring(0, Math.Min(120, firstChunk.Content.Length));
                    Console.WriteLine($"   첫 청크 미리보기: {preview}...");
                }

                Console.WriteLine();
                totalChunks += result.Chunks.Count;
                totalImages += result.ImageUrls?.Count ?? 0;
            }

            Console.WriteLine($"📊 전체 통계:");
            Console.WriteLine($"   처리된 SPA 페이지: {results.Count}");
            Console.WriteLine($"   생성된 청크: {totalChunks}");
            Console.WriteLine($"   수집된 이미지: {totalImages}");
            Console.WriteLine($"   평균 청크/페이지: {(double)totalChunks / results.Count:F1}");

            // 정적 vs 동적 비교
            Console.WriteLine($"\n💡 성능 참고:");
            Console.WriteLine($"   - 동적 크롤링은 정적 크롤링보다 느리지만 87% 더 많은 콘텐츠 추출");
            Console.WriteLine($"   - JavaScript 렌더링 완료 후 콘텐츠 수집으로 높은 품질 보장");
            Console.WriteLine($"   - SPA, React, Vue, Angular 웹사이트에 필수");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 오류 발생: {ex.Message}");
            Console.WriteLine($"   상세: {ex.StackTrace}");

            if (ex.Message.Contains("Executable doesn't exist"))
            {
                Console.WriteLine($"\n💡 해결 방법:");
                Console.WriteLine($"   Playwright 브라우저를 수동으로 설치하세요:");
                Console.WriteLine($"   pwsh bin/Debug/net10.0/playwright.ps1 install chromium");
            }
        }

        Console.WriteLine("\n프로그램 종료. 아무 키나 누르세요...");
        Console.ReadKey();
    }

    /// <summary>
    /// 메타데이터에서 JavaScript 프레임워크 감지
    /// </summary>
    private static string DetectFramework(Dictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("Framework", out var framework))
        {
            return framework.ToString() ?? "Unknown";
        }

        if (metadata.TryGetValue("TechnologyStack", out var stack))
        {
            var stackStr = stack.ToString() ?? "";
            if (stackStr.Contains("React")) return "React";
            if (stackStr.Contains("Vue")) return "Vue.js";
            if (stackStr.Contains("Angular")) return "Angular";
        }

        return "Not detected";
    }
}
