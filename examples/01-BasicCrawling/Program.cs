using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services;

namespace WebFlux.Examples.BasicCrawling;

/// <summary>
/// 기본 웹 크롤링 예제
/// 정적 HTML 페이지를 크롤링하고 청킹하는 가장 간단한 사용 예제
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WebFlux SDK - 기본 크롤링 예제 ===\n");

        // 1. 서비스 컬렉션 구성
        var services = new ServiceCollection();

        // WebFlux 핵심 서비스 등록
        services.AddWebFlux(options =>
        {
            // 크롤링 옵션 설정
            options.MaxConcurrency = 3;
            options.UserAgent = "WebFlux-Example/1.0";
            options.RequestDelay = TimeSpan.FromMilliseconds(500);

            // 청킹 옵션 설정
            options.DefaultChunkSize = 512;
            options.ChunkOverlap = 50;
        });

        var serviceProvider = services.BuildServiceProvider();

        // 2. WebContentProcessor 서비스 가져오기
        var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();

        // 3. 크롤링할 URL 정의
        var urls = new[]
        {
            "https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12",
            "https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9"
        };

        Console.WriteLine($"크롤링 시작: {urls.Length}개 페이지\n");

        // 4. 크롤링 옵션 구성
        var crawlOptions = new CrawlOptions
        {
            MaxDepth = 0,  // 주어진 URL만 크롤링 (링크 탐색 안함)
            FollowExternalLinks = false,
            RespectRobotsTxt = true,
            Timeout = TimeSpan.FromSeconds(30)
        };

        // 5. 청킹 옵션 구성
        var chunkingOptions = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100,
            ChunkOverlap = 64,
            Strategy = "paragraph"  // 문단 기반 청킹
        };

        try
        {
            // 6. 크롤링 및 청킹 실행
            var results = await processor.ProcessUrlsAsync(
                urls,
                crawlOptions,
                chunkingOptions
            );

            // 7. 결과 출력
            Console.WriteLine($"\n✅ 크롤링 완료!\n");

            int totalChunks = 0;
            foreach (var result in results)
            {
                Console.WriteLine($"📄 URL: {result.Url}");
                Console.WriteLine($"   제목: {result.Title}");
                Console.WriteLine($"   청크 수: {result.Chunks.Count}");
                Console.WriteLine($"   원본 크기: {result.OriginalSize:N0} 문자");
                Console.WriteLine($"   처리 시간: {result.ProcessingTime.TotalSeconds:F2}초");

                // 첫 번째 청크 미리보기
                if (result.Chunks.Any())
                {
                    var firstChunk = result.Chunks.First();
                    Console.WriteLine($"   첫 청크 미리보기: {firstChunk.Content.Substring(0, Math.Min(100, firstChunk.Content.Length))}...");
                }

                Console.WriteLine();
                totalChunks += result.Chunks.Count;
            }

            Console.WriteLine($"📊 전체 통계:");
            Console.WriteLine($"   처리된 페이지: {results.Count}");
            Console.WriteLine($"   생성된 청크: {totalChunks}");
            Console.WriteLine($"   평균 청크/페이지: {(double)totalChunks / results.Count:F1}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 오류 발생: {ex.Message}");
            Console.WriteLine($"   상세: {ex.StackTrace}");
        }

        Console.WriteLine("\n프로그램 종료. 아무 키나 누르세요...");
        Console.ReadKey();
    }
}
