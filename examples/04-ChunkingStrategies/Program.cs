using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.ChunkingStrategies;

namespace WebFlux.Examples.ChunkingStrategies;

/// <summary>
/// 청킹 전략 비교 예제
/// 6가지 청킹 전략의 성능과 품질을 비교 분석합니다.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WebFlux SDK - 청킹 전략 비교 예제 ===\n");

        // 1. 서비스 설정
        var services = new ServiceCollection();
        services.AddWebFlux();
        var serviceProvider = services.BuildServiceProvider();

        // 2. 테스트 콘텐츠 생성
        var testContent = GenerateTestContent();
        Console.WriteLine($"테스트 문서 크기: {testContent.Text.Length:N0} 문자\n");

        // 3. 테스트할 청킹 전략 목록
        var strategies = new Dictionary<string, IChunkingStrategy>
        {
            ["FixedSize"] = new FixedSizeChunkingStrategy(),
            ["Paragraph"] = new ParagraphChunkingStrategy(),
            ["Smart"] = new SmartChunkingStrategy(),
            ["Semantic"] = new SemanticChunkingStrategy(),
            ["MemoryOptimized"] = new MemoryOptimizedChunkingStrategy(),
            ["Auto"] = new AutoChunkingStrategy()
        };

        // 4. 비교 결과 저장
        var results = new List<ChunkingComparisonResult>();

        Console.WriteLine("┌" + new string('─', 88) + "┐");
        Console.WriteLine("│  전략명           │  청크 수  │  처리 시간  │  메모리 사용  │  품질 점수  │");
        Console.WriteLine("├" + new string('─', 88) + "┤");

        // 5. 각 전략 테스트
        foreach (var (strategyName, strategy) in strategies)
        {
            var result = await TestStrategy(strategyName, strategy, testContent);
            results.Add(result);

            // 결과 출력
            Console.WriteLine($"│  {strategyName,-16} │  {result.ChunkCount,7}  │  {result.ProcessingTime,9:F2}ms  │  {result.MemoryUsed,11:F2}MB  │  {result.QualityScore,10:F2}  │");
        }

        Console.WriteLine("└" + new string('─', 88) + "┘\n");

        // 6. 상세 분석
        Console.WriteLine("📊 상세 분석:\n");

        // 가장 빠른 전략
        var fastest = results.OrderBy(r => r.ProcessingTime).First();
        Console.WriteLine($"⚡ 가장 빠른 전략: {fastest.StrategyName} ({fastest.ProcessingTime:F2}ms)");

        // 메모리 효율적인 전략
        var mostEfficient = results.OrderBy(r => r.MemoryUsed).First();
        Console.WriteLine($"💾 가장 메모리 효율적: {mostEfficient.StrategyName} ({mostEfficient.MemoryUsed:F2}MB)");

        // 가장 높은 품질
        var highestQuality = results.OrderByDescending(r => r.QualityScore).First();
        Console.WriteLine($"✨ 가장 높은 품질: {highestQuality.StrategyName} (점수: {highestQuality.QualityScore:F2})");

        // 7. 전략별 특성 분석
        Console.WriteLine($"\n📋 전략별 특성:\n");

        foreach (var result in results)
        {
            Console.WriteLine($"▶ {result.StrategyName}");
            Console.WriteLine($"   청크 크기 범위: {result.MinChunkSize} ~ {result.MaxChunkSize} 문자");
            Console.WriteLine($"   평균 청크 크기: {result.AverageChunkSize:F0} 문자");
            Console.WriteLine($"   표준 편차: {result.StandardDeviation:F0} (일관성: {GetConsistencyRating(result.StandardDeviation)})");
            Console.WriteLine($"   권장 사용: {GetRecommendation(result.StrategyName)}");
            Console.WriteLine();
        }

        // 8. 사용 시나리오별 추천
        Console.WriteLine("💡 시나리오별 추천 전략:\n");
        Console.WriteLine("📚 일반 텍스트 (뉴스, 블로그):");
        Console.WriteLine("   1순위: Paragraph - 자연스러운 문단 보존");
        Console.WriteLine("   2순위: FixedSize - 빠른 처리 속도\n");

        Console.WriteLine("📖 기술 문서 (API, 가이드):");
        Console.WriteLine("   1순위: Smart - 헤딩 구조 인식");
        Console.WriteLine("   2순위: Auto - 자동 최적 전략 선택\n");

        Console.WriteLine("🎓 학술 논문:");
        Console.WriteLine("   1순위: Semantic - 의미적 일관성");
        Console.WriteLine("   2순위: Smart - 구조 보존\n");

        Console.WriteLine("💾 대용량 문서 (>1MB):");
        Console.WriteLine("   1순위: MemoryOptimized - 84% 메모리 절약");
        Console.WriteLine("   2순위: Auto - 자동 메모리 최적화\n");

        Console.WriteLine("🚀 성능 우선 (실시간 처리):");
        Console.WriteLine("   1순위: FixedSize - 최고 속도");
        Console.WriteLine("   2순위: Paragraph - 빠르고 자연스러움\n");

        Console.WriteLine("🎯 품질 우선 (RAG 정확도):");
        Console.WriteLine("   1순위: Semantic - 최고 의미적 일관성");
        Console.WriteLine("   2순위: Smart - 구조 보존 + 높은 품질\n");

        // 9. 성능 비교 차트
        Console.WriteLine("📈 성능 비교 차트:\n");
        PrintPerformanceChart(results);

        Console.WriteLine("\n프로그램 종료. 아무 키나 누르세요...");
        Console.ReadKey();
    }

    private static async Task<ChunkingComparisonResult> TestStrategy(
        string strategyName,
        IChunkingStrategy strategy,
        ExtractedContent content)
    {
        var options = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100,
            ChunkOverlap = 64
        };

        // 메모리 측정 시작
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        // 처리 시간 측정
        var stopwatch = Stopwatch.StartNew();
        var chunks = await strategy.ChunkAsync(content, options);
        stopwatch.Stop();

        // 메모리 측정 종료
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        // 품질 점수 계산
        var qualityScore = CalculateQualityScore(chunks);

        // 청크 크기 통계
        var chunkSizes = chunks.Select(c => c.Content.Length).ToList();
        var avgSize = chunkSizes.Average();
        var stdDev = Math.Sqrt(chunkSizes.Select(s => Math.Pow(s - avgSize, 2)).Average());

        return new ChunkingComparisonResult
        {
            StrategyName = strategyName,
            ChunkCount = chunks.Count,
            ProcessingTime = stopwatch.Elapsed.TotalMilliseconds,
            MemoryUsed = Math.Max(0, memoryUsed),  // 음수 방지
            QualityScore = qualityScore,
            MinChunkSize = chunkSizes.Min(),
            MaxChunkSize = chunkSizes.Max(),
            AverageChunkSize = avgSize,
            StandardDeviation = stdDev
        };
    }

    private static double CalculateQualityScore(List<WebContentChunk> chunks)
    {
        double score = 0;

        // 1. 크기 일관성 (30%)
        var chunkSizes = chunks.Select(c => c.Content.Length).ToList();
        var avgSize = chunkSizes.Average();
        var sizeVariance = chunkSizes.Select(s => Math.Abs(s - avgSize) / avgSize).Average();
        score += Math.Max(0, (1 - sizeVariance)) * 30;

        // 2. 의미적 완결성 (40%)
        var completeChunks = chunks.Count(c =>
            c.Content.Trim().EndsWith(".") ||
            c.Content.Trim().EndsWith("!") ||
            c.Content.Trim().EndsWith("?"));
        score += (completeChunks / (double)chunks.Count) * 40;

        // 3. 구조 보존 (30%)
        var structuredChunks = chunks.Count(c =>
            c.Metadata.ContainsKey("HeadingLevel") ||
            c.Metadata.ContainsKey("ParentHeading"));
        score += (structuredChunks / (double)chunks.Count) * 30;

        return Math.Min(100, score);
    }

    private static string GetConsistencyRating(double stdDev)
    {
        if (stdDev < 50) return "매우 높음";
        if (stdDev < 100) return "높음";
        if (stdDev < 150) return "중간";
        if (stdDev < 200) return "낮음";
        return "매우 낮음";
    }

    private static string GetRecommendation(string strategyName)
    {
        return strategyName switch
        {
            "FixedSize" => "빠른 처리가 필요한 실시간 시스템",
            "Paragraph" => "일반 텍스트 문서 (뉴스, 블로그)",
            "Smart" => "기술 문서, API 가이드 (구조 인식 필요)",
            "Semantic" => "학술 논문, 복잡한 텍스트 (의미 보존 중요)",
            "MemoryOptimized" => "대용량 문서, 메모리 제약 환경",
            "Auto" => "다양한 문서 타입 자동 처리",
            _ => "범용 사용"
        };
    }

    private static void PrintPerformanceChart(List<ChunkingComparisonResult> results)
    {
        var maxTime = results.Max(r => r.ProcessingTime);
        var maxMemory = results.Max(r => r.MemoryUsed);

        Console.WriteLine("처리 시간 (상대적):");
        foreach (var result in results.OrderBy(r => r.ProcessingTime))
        {
            var barLength = (int)((result.ProcessingTime / maxTime) * 50);
            var bar = new string('█', barLength);
            Console.WriteLine($"  {result.StrategyName,-16} {bar} {result.ProcessingTime:F2}ms");
        }

        Console.WriteLine($"\n메모리 사용 (상대적):");
        foreach (var result in results.OrderBy(r => r.MemoryUsed))
        {
            var barLength = (int)((result.MemoryUsed / maxMemory) * 50);
            var bar = new string('█', barLength);
            Console.WriteLine($"  {result.StrategyName,-16} {bar} {result.MemoryUsed:F2}MB");
        }
    }

    private static ExtractedContent GenerateTestContent()
    {
        var text = @"
# Introduction to C# 12

C# 12 introduces several new features that enhance developer productivity and code quality.

## Primary Constructors

Primary constructors provide a concise syntax for declaring constructor parameters directly in the class declaration.
This feature reduces boilerplate code and makes the intent clearer.

Example code:
```csharp
public class Person(string name, int age)
{
    public string Name => name;
    public int Age => age;
}
```

## Collection Expressions

Collection expressions offer a new syntax for creating and initializing collections.
This makes code more readable and consistent across different collection types.

### Benefits
- Improved readability
- Type inference support
- Consistent syntax

## Lambda Improvements

C# 12 brings several improvements to lambda expressions, including natural type inference
and better performance optimizations.

### Performance
Lambda expressions are now optimized at compile-time for better runtime performance.

## Conclusion

C# 12 represents a significant step forward in language evolution, focusing on developer
experience and code quality improvements.
";

        return new ExtractedContent
        {
            Text = text,
            MainContent = text,
            Url = "https://example.com/csharp-12",
            Title = "Introduction to C# 12",
            OriginalContentType = "text/markdown"
        };
    }
}

class ChunkingComparisonResult
{
    public string StrategyName { get; set; } = "";
    public int ChunkCount { get; set; }
    public double ProcessingTime { get; set; }
    public double MemoryUsed { get; set; }
    public double QualityScore { get; set; }
    public int MinChunkSize { get; set; }
    public int MaxChunkSize { get; set; }
    public double AverageChunkSize { get; set; }
    public double StandardDeviation { get; set; }
}
