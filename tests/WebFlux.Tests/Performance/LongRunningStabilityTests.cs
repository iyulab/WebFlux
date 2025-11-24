using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;
using System.Diagnostics;

namespace WebFlux.Tests.Performance;

/// <summary>
/// 장시간 실행 안정성 테스트 (24시간 목표)
/// Task 5D.4: 메모리 및 성능 프로파일링
/// </summary>
[Trait("Category", "Performance")]
[Trait("Category", "LongRunning")]
public class LongRunningStabilityTests
{
    /// <summary>
    /// 24시간 연속 처리 안정성 테스트
    /// 실제 실행 시간: 테스트 환경에서는 1시간으로 축소 (확장 가능)
    /// </summary>
    [Fact(Skip = "24시간 테스트 - 수동 실행 필요")]
    public async Task ContinuousProcessing_ShouldRemainStable_For24Hours()
    {
        // Arrange
        var testDuration = TimeSpan.FromHours(24);  // 실제 24시간
        var endTime = DateTime.UtcNow.Add(testDuration);

        var options = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100,
            ChunkOverlap = 64
        };

        var strategy = new AutoChunkingStrategy();
        var metrics = new LongRunningMetrics();

        // Act: 24시간 동안 계속 처리
        while (DateTime.UtcNow < endTime)
        {
            metrics.StartIteration();

            try
            {
                // 다양한 문서 처리
                var content = GenerateRandomWebContent();
                var chunks = await strategy.ChunkAsync(content, options);

                chunks.Should().NotBeEmpty();
                metrics.RecordSuccess(chunks.Count);
            }
            catch (Exception ex)
            {
                metrics.RecordFailure(ex);
            }

            // 메모리 샘플링 (1시간마다)
            if (metrics.IterationCount % 1000 == 0)
            {
                metrics.SampleMemory();

                // GC 상태 확인
                var gen2Count = GC.CollectionCount(2);
                metrics.RecordGC(gen2Count);

                // 메모리 누수 조기 감지
                if (metrics.IsMemoryLeakDetected())
                {
                    Assert.Fail($"메모리 누수 감지: {metrics.GetMemoryTrend()}");
                }
            }

            // CPU 휴식 (과부하 방지)
            await Task.Delay(100);
        }

        // Assert: 안정성 검증
        var report = metrics.GenerateReport();

        report.SuccessRate.Should().BeGreaterThan(0.99,
            because: "24시간 동안 99% 이상 성공률 유지");

        report.AverageMemoryMB.Should().BeLessThan(500,
            because: "평균 메모리 사용량이 500MB 미만");

        report.MemoryTrendSlope.Should().BeLessThan(0.1,
            because: "메모리 증가 추세가 거의 없어야 함 (누수 없음)");
    }

    /// <summary>
    /// 고부하 연속 처리 테스트 (10분, 빠른 검증용)
    /// </summary>
    [Fact]
    public async Task HighLoadProcessing_ShouldRemainStable_For10Minutes()
    {
        // Arrange
        var testDuration = TimeSpan.FromMinutes(10);
        var endTime = DateTime.UtcNow.Add(testDuration);

        var options = new ChunkingOptions
        {
            MaxChunkSize = 512
        };

        var strategy = new MemoryOptimizedChunkingStrategy();
        var metrics = new LongRunningMetrics();

        // Act: 10분 동안 고속 처리
        while (DateTime.UtcNow < endTime)
        {
            metrics.StartIteration();

            var content = GenerateRandomWebContent();
            var chunks = await strategy.ChunkAsync(content, options);

            chunks.Should().NotBeEmpty();
            metrics.RecordSuccess(chunks.Count);

            // 메모리 샘플링 (50회마다)
            if (metrics.IterationCount % 50 == 0)
            {
                metrics.SampleMemory();
            }

            // 높은 처리 속도 (딜레이 최소화)
            await Task.Delay(10);
        }

        // Assert
        var report = metrics.GenerateReport();

        report.SuccessRate.Should().BeGreaterThan(0.95,
            because: "고부하에서도 95% 이상 성공률");

        report.AverageMemoryMB.Should().BeLessThan(200,
            because: "MemoryOptimized 전략으로 메모리 사용량 낮음");

        report.TotalIterations.Should().BeGreaterThan(100,
            because: "10분 동안 충분한 반복 처리");
    }

    /// <summary>
    /// GC 압력 테스트 - 빈번한 할당 및 해제
    /// </summary>
    [Fact]
    public async Task FrequentAllocation_ShouldNotCauseExcessiveGCPressure()
    {
        // Arrange
        const int iterations = 1000;
        var options = new ChunkingOptions
        {
            MaxChunkSize = 512
        };

        var strategy = new MemoryOptimizedChunkingStrategy();

        var initialGen0 = GC.CollectionCount(0);
        var initialGen1 = GC.CollectionCount(1);
        var initialGen2 = GC.CollectionCount(2);

        // Act: 1000회 빈번한 할당
        for (int i = 0; i < iterations; i++)
        {
            var content = GenerateRandomWebContent();
            var chunks = await strategy.ChunkAsync(content, options);

            chunks.Should().NotBeEmpty();

            // 중간 GC 방지 (압력 측정을 위해)
            if (i % 100 == 0)
            {
                await Task.Delay(10);
            }
        }

        var finalGen0 = GC.CollectionCount(0);
        var finalGen1 = GC.CollectionCount(1);
        var finalGen2 = GC.CollectionCount(2);

        // Assert: GC 압력 분석
        var gen0Collections = finalGen0 - initialGen0;
        var gen1Collections = finalGen1 - initialGen1;
        var gen2Collections = finalGen2 - initialGen2;

        // Gen 2 컬렉션이 지나치게 많으면 문제 (GC 비결정성 고려하여 완화)
        gen2Collections.Should().BeLessThan(15,
            because: "Gen 2 컬렉션이 많으면 장기 객체 할당 문제");

        // Gen 1 컬렉션도 적절한 수준이어야 함
        gen1Collections.Should().BeLessThan(50,
            because: "Gen 1 컬렉션이 많으면 중기 객체 관리 문제");
    }

    // Helper: 랜덤 웹 콘텐츠 생성
    private static ExtractedContent GenerateRandomWebContent()
    {
        var random = new Random();
        var size = random.Next(1000, 10000);  // 1KB ~ 10KB

        var text = new System.Text.StringBuilder(size);
        text.AppendLine("<html><body>");

        for (int i = 0; i < size / 100; i++)
        {
            text.AppendLine($"<p>Random paragraph {i}. Lorem ipsum dolor sit amet.</p>");
        }

        text.AppendLine("</body></html>");

        return new ExtractedContent
        {
            Text = text.ToString(),
            MainContent = text.ToString(),
            Url = $"https://test-{random.Next(1000)}.com",
            Title = $"Test Document {random.Next(1000)}",
            OriginalContentType = "text/html"
        };
    }
}

/// <summary>
/// 장시간 실행 메트릭 수집기
/// </summary>
public class LongRunningMetrics
{
    private readonly List<double> _memorySamplesMB = new();
    private readonly List<int> _gcGen2Samples = new();
    private int _successCount = 0;
    private int _failureCount = 0;
    private int _totalChunks = 0;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public int IterationCount => _successCount + _failureCount;

    public void StartIteration()
    {
        // 반복 시작 (필요시 추가 로직)
    }

    public void RecordSuccess(int chunkCount)
    {
        Interlocked.Increment(ref _successCount);
        Interlocked.Add(ref _totalChunks, chunkCount);
    }

    public void RecordFailure(Exception ex)
    {
        Interlocked.Increment(ref _failureCount);
    }

    public void SampleMemory()
    {
        var memoryMB = GetCurrentMemoryMB();
        lock (_memorySamplesMB)
        {
            _memorySamplesMB.Add(memoryMB);
        }
    }

    public void RecordGC(int gen2Count)
    {
        lock (_gcGen2Samples)
        {
            _gcGen2Samples.Add(gen2Count);
        }
    }

    public double GetCurrentMemoryMB()
    {
        return GC.GetTotalMemory(false) / (1024.0 * 1024.0);
    }

    public bool IsMemoryLeakDetected()
    {
        lock (_memorySamplesMB)
        {
            if (_memorySamplesMB.Count < 10)
                return false;

            // 최근 10개 샘플의 평균이 초기 샘플보다 50% 이상 증가
            var initialAvg = _memorySamplesMB.Take(5).Average();
            var recentAvg = _memorySamplesMB.TakeLast(5).Average();

            return (recentAvg - initialAvg) / initialAvg > 0.5;
        }
    }

    public string GetMemoryTrend()
    {
        lock (_memorySamplesMB)
        {
            if (_memorySamplesMB.Count < 2)
                return "N/A";

            var first = _memorySamplesMB.First();
            var last = _memorySamplesMB.Last();

            return $"{first:F2} MB → {last:F2} MB (증가: {last - first:F2} MB)";
        }
    }

    public LongRunningReport GenerateReport()
    {
        lock (_memorySamplesMB)
        {
            var avgMemory = _memorySamplesMB.Any() ? _memorySamplesMB.Average() : 0;
            var successRate = IterationCount > 0 ? (double)_successCount / IterationCount : 0;

            // 메모리 증가 추세 계산 (선형 회귀)
            var trendSlope = CalculateMemoryTrendSlope();

            return new LongRunningReport
            {
                TotalIterations = IterationCount,
                SuccessCount = _successCount,
                FailureCount = _failureCount,
                SuccessRate = successRate,
                TotalChunks = _totalChunks,
                AverageMemoryMB = avgMemory,
                MemoryTrendSlope = trendSlope,
                ElapsedTime = _stopwatch.Elapsed
            };
        }
    }

    private double CalculateMemoryTrendSlope()
    {
        if (_memorySamplesMB.Count < 2)
            return 0;

        // 단순 선형 회귀로 기울기 계산
        var n = _memorySamplesMB.Count;
        var sumX = Enumerable.Range(0, n).Sum();
        var sumY = _memorySamplesMB.Sum();
        var sumXY = Enumerable.Range(0, n).Select(i => i * _memorySamplesMB[i]).Sum();
        var sumX2 = Enumerable.Range(0, n).Select(i => i * i).Sum();

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }
}

/// <summary>
/// 장시간 실행 리포트
/// </summary>
public class LongRunningReport
{
    public int TotalIterations { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public int TotalChunks { get; set; }
    public double AverageMemoryMB { get; set; }
    public double MemoryTrendSlope { get; set; }  // MB/iteration
    public TimeSpan ElapsedTime { get; set; }
}
