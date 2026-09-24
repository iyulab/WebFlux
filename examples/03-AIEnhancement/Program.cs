using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.AI;

namespace WebFlux.Examples.AIEnhancement;

/// <summary>
/// AI 향상 예제
/// OpenAI API를 사용하여 크롤링된 콘텐츠를 요약하고 품질을 향상시킵니다.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WebFlux SDK - AI Enhancement 예제 ===\n");

        // 1. 환경 변수에서 API 키 로드
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("❌ 오류: OPENAI_API_KEY 환경 변수가 설정되지 않았습니다.");
            Console.WriteLine("   설정 방법:");
            Console.WriteLine("   - Windows: setx OPENAI_API_KEY \"your-api-key\"");
            Console.WriteLine("   - Linux/Mac: export OPENAI_API_KEY=\"your-api-key\"");
            Console.WriteLine("\n프로그램 종료. 아무 키나 누르세요...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("✅ OpenAI API 키 확인 완료\n");

        // 2. 서비스 컬렉션 구성
        var services = new ServiceCollection();

        // WebFlux 핵심 서비스 등록
        services.AddWebFlux(options =>
        {
            options.Crawling.MaxConcurrentRequests = 2;
            options.Crawling.DefaultUserAgent = "WebFlux-AI-Example/1.0";
        });

        // OpenAI 서비스 등록
        services.AddSingleton<ITextCompletionService>(sp =>
            new OpenAITextCompletionService(apiKey, "gpt-4o-mini"));  // 비용 효율적인 모델

        services.AddSingleton<IAiEnhancementService, BasicAiEnhancementService>();

        var serviceProvider = services.BuildServiceProvider();

        // 3. 서비스 가져오기
        var processor = serviceProvider.GetRequiredService<IWebContentProcessor>();
        var aiEnhancement = serviceProvider.GetRequiredService<IAiEnhancementService>();

        // 4. 크롤링할 URL 정의
        var urls = new[]
        {
            "https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12"
        };

        Console.WriteLine($"AI 향상 크롤링 시작: {urls.Length}개 페이지\n");

        // 5. 크롤링 옵션
        var crawlOptions = new CrawlOptions
        {
            MaxDepth = 0,
            RespectRobotsTxt = true,
            Timeout = TimeSpan.FromSeconds(30)
        };

        // 6. 청킹 옵션 (Semantic 전략 사용)
        var chunkingOptions = new ChunkingOptions
        {
            MaxChunkSize = 1024,  // AI 처리는 더 큰 청크 권장
            MinChunkSize = 200,
            ChunkOverlap = 128,
            Strategy = "semantic"  // 의미론적 청킹
        };

        try
        {
            // 7. 크롤링 및 청킹
            Console.WriteLine("📡 웹 페이지 크롤링 중...\n");
            var results = await processor.ProcessUrlsAsync(
                urls,
                crawlOptions,
                chunkingOptions
            );

            foreach (var result in results)
            {
                Console.WriteLine($"📄 URL: {result.Url}");
                Console.WriteLine($"   제목: {result.Title}");
                Console.WriteLine($"   청크 수: {result.Chunks.Count}\n");

                // 8. AI 향상 옵션 구성
                var enhancementOptions = new AiEnhancementOptions
                {
                    GenerateSummary = true,      // 요약 생성
                    ExtractKeywords = true,      // 키워드 추출
                    GenerateQuestions = true,    // 관련 질문 생성
                    TranslateToLanguage = "ko",  // 한국어 번역
                    MaxSummaryLength = 200
                };

                // 9. AI로 콘텐츠 향상 (처음 3개 청크만)
                Console.WriteLine("🤖 AI 콘텐츠 향상 중...\n");
                var chunksToEnhance = result.Chunks.Take(3).ToList();

                int chunkIndex = 1;
                foreach (var chunk in chunksToEnhance)
                {
                    Console.WriteLine($"청크 {chunkIndex}/{chunksToEnhance.Count}:");
                    Console.WriteLine($"원본 (영문, {chunk.Content.Length}자):");
                    Console.WriteLine($"{chunk.Content.Substring(0, Math.Min(150, chunk.Content.Length))}...\n");

                    // AI 향상 수행
                    var enhanced = await aiEnhancement.EnhanceContentAsync(
                        chunk.Content,
                        enhancementOptions
                    );

                    // 향상된 결과 출력
                    Console.WriteLine($"✨ AI 향상 결과:");
                    Console.WriteLine($"📝 요약 (한국어):");
                    Console.WriteLine($"   {enhanced.Summary}\n");

                    Console.WriteLine($"🔑 키워드:");
                    Console.WriteLine($"   {string.Join(", ", enhanced.Keywords)}\n");

                    Console.WriteLine($"❓ 관련 질문:");
                    foreach (var question in enhanced.SuggestedQuestions.Take(3))
                    {
                        Console.WriteLine($"   - {question}");
                    }

                    Console.WriteLine($"\n처리 시간: {enhanced.ProcessingTime.TotalSeconds:F2}초");
                    Console.WriteLine($"토큰 사용: {enhanced.TokensUsed} 토큰\n");
                    Console.WriteLine(new string('-', 80) + "\n");

                    chunkIndex++;

                    // API 속도 제한 방지
                    await Task.Delay(1000);
                }

                // 10. 전체 문서 요약 생성
                Console.WriteLine("📊 전체 문서 요약 생성 중...\n");
                var allContent = string.Join("\n\n", result.Chunks.Select(c => c.Content));
                var documentSummary = await aiEnhancement.EnhanceContentAsync(
                    allContent,
                    new AiEnhancementOptions
                    {
                        GenerateSummary = true,
                        MaxSummaryLength = 500,
                        TranslateToLanguage = "ko"
                    }
                );

                Console.WriteLine($"📄 전체 문서 요약 (한국어):");
                Console.WriteLine($"{documentSummary.Summary}\n");

                // 11. 비용 분석
                var totalTokens = chunksToEnhance.Sum(c =>
                    c.Metadata.TryGetValue("AI_TokensUsed", out var tokens) ?
                    (int)tokens : 0);

                Console.WriteLine($"💰 AI 처리 비용 분석:");
                Console.WriteLine($"   총 토큰 사용: {totalTokens:N0} 토큰");
                Console.WriteLine($"   예상 비용 (gpt-4o-mini): ${totalTokens * 0.00015 / 1000:F4}");
                Console.WriteLine($"   청크당 평균: {totalTokens / chunksToEnhance.Count:F0} 토큰");
            }

            Console.WriteLine($"\n✅ AI 향상 완료!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 오류 발생: {ex.Message}");
            Console.WriteLine($"   상세: {ex.StackTrace}");

            if (ex.Message.Contains("Incorrect API key") || ex.Message.Contains("401"))
            {
                Console.WriteLine($"\n💡 해결 방법:");
                Console.WriteLine($"   OpenAI API 키가 올바른지 확인하세요.");
                Console.WriteLine($"   https://platform.openai.com/api-keys");
            }
            else if (ex.Message.Contains("Rate limit") || ex.Message.Contains("429"))
            {
                Console.WriteLine($"\n💡 해결 방법:");
                Console.WriteLine($"   API 속도 제한에 도달했습니다. 잠시 후 다시 시도하세요.");
            }
        }

        Console.WriteLine("\n프로그램 종료. 아무 키나 누르세요...");
        Console.ReadKey();
    }
}
