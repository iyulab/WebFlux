using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Performance;

/// <summary>
/// 대용량 문서 처리 안정성 테스트
/// Task 5D.4: 메모리 및 성능 프로파일링
/// </summary>
[Trait("Category", "Performance")]
[Trait("Category", "LargeDocument")]
public class LargeDocumentStabilityTests
{
    [Fact]
    public async Task MemoryOptimizedStrategy_ShouldHandleLargeDocument_WithoutMemoryLeak()
    {
        // Arrange: 1MB 이상의 대용량 HTML 문서 생성
        var largeHtml = GenerateLargeHtmlDocument(sizeInMb: 1);
        var content = new ExtractedContent
        {
            Text = largeHtml,
            MainContent = largeHtml,
            Url = "https://large-document-test.com",
            Title = "Large Document Test",
            OriginalContentType = "text/html"
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100,
            ChunkOverlap = 64
        };

        var strategy = new MemoryOptimizedChunkingStrategy();

        // Measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);

        // Act: 대용량 문서 청킹
        var chunks = await strategy.ChunkAsync(content, options);

        // Measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert: 메모리 증가가 합리적인 범위 내
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        chunks.Should().NotBeEmpty();
        chunks.Count.Should().BeGreaterThan(0);

        // 메모리 증가가 문서 크기의 5배를 초과하지 않아야 함 (GC 비결정성 고려)
        memoryIncreaseMB.Should().BeLessThan(5.0,
            because: "메모리 증가가 문서 크기의 5배를 초과하면 메모리 누수 가능성");

        // 청크 내용 검증
        foreach (var chunk in chunks)
        {
            chunk.Content.Should().NotBeNullOrEmpty();
            chunk.SourceUrl.Should().Be(content.Url);
        }
    }

    [Fact]
    public async Task MemoryOptimizedStrategy_ShouldProcessMultipleLargeDocuments_Sequentially()
    {
        // Arrange: 10개의 대용량 문서
        const int documentCount = 10;
        var options = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100,
            ChunkOverlap = 64
        };

        var strategy = new MemoryOptimizedChunkingStrategy();

        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        // Act: 순차적으로 처리
        var totalChunks = 0;

        for (int i = 0; i < documentCount; i++)
        {
            var largeHtml = GenerateLargeHtmlDocument(sizeInMb: 0.5);
            var content = new ExtractedContent
            {
                Text = largeHtml,
                MainContent = largeHtml,
                Url = $"https://test-{i}.com",
                Title = $"Document {i}",
                OriginalContentType = "text/html"
            };

            var chunks = await strategy.ChunkAsync(content, options);
            totalChunks += chunks.Count;

            // 각 문서 처리 후 메모리 정리
            if (i % 5 == 0)
            {
                GC.Collect();
            }
        }

        // Measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert: 전체 메모리 증가가 합리적
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        totalChunks.Should().BeGreaterThan(0);

        // 10개 문서 처리 후 메모리 증가가 5MB 이하여야 함 (누수 없음)
        memoryIncreaseMB.Should().BeLessThan(5.0,
            because: "순차 처리 후 메모리가 누적되면 메모리 누수");
    }

    [Fact]
    public async Task MemoryOptimizedStrategy_ShouldStreamLargeDocument_WithLowMemoryFootprint()
    {
        // Arrange: 5MB 초대용량 문서
        var veryLargeHtml = GenerateLargeHtmlDocument(sizeInMb: 5);
        var content = new ExtractedContent
        {
            Text = veryLargeHtml,
            MainContent = veryLargeHtml,
            Url = "https://very-large-document-test.com",
            Title = "Very Large Document Test",
            OriginalContentType = "text/html"
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100,
            ChunkOverlap = 64,
            UseStreaming = true  // 스트리밍 모드 활성화
        };

        var strategy = new MemoryOptimizedChunkingStrategy();

        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        // Act: 스트리밍 모드로 청킹
        var chunks = await strategy.ChunkAsync(content, options);

        // 청크를 즉시 소비 (실제 시나리오 시뮬레이션)
        var processedCount = 0;
        foreach (var chunk in chunks)
        {
            // 청크 처리 시뮬레이션
            _ = chunk.Content.Length;
            processedCount++;
        }

        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert: 스트리밍 모드에서 메모리 사용량이 매우 낮아야 함
        var memoryIncreaseMB = (finalMemory - initialMemory) / (1024.0 * 1024.0);

        processedCount.Should().BeGreaterThan(0);

        // 5MB 문서 처리에도 메모리 증가가 20MB 이하여야 함 (GC 비결정성 고려)
        memoryIncreaseMB.Should().BeLessThan(20.0,
            because: "스트리밍 모드는 전체 문서를 메모리에 로드하지 않음");
    }

    [Fact]
    public async Task AllStrategies_ShouldHandleLargeDocument_WithoutCrashing()
    {
        // Arrange: 모든 청킹 전략 테스트
        var largeHtml = GenerateLargeHtmlDocument(sizeInMb: 1);
        var content = new ExtractedContent
        {
            Text = largeHtml,
            MainContent = largeHtml,
            Url = "https://all-strategies-test.com",
            Title = "All Strategies Test",
            OriginalContentType = "text/html"
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 512,
            MinChunkSize = 100
        };

        var strategies = new IChunkingStrategy[]
        {
            new FixedSizeChunkingStrategy(),
            new ParagraphChunkingStrategy(),
            new SmartChunkingStrategy(),
            new MemoryOptimizedChunkingStrategy()
        };

        // Act & Assert: 모든 전략이 대용량 문서를 처리할 수 있어야 함
        foreach (var strategy in strategies)
        {
            var strategyName = strategy.GetType().Name;

            var act = async () => await strategy.ChunkAsync(content, options);

            await act.Should().NotThrowAsync(
                because: $"{strategyName}는 대용량 문서를 처리할 수 있어야 함");

            var chunks = await act();
            chunks.Should().NotBeEmpty(
                because: $"{strategyName}는 청크를 생성해야 함");
        }
    }

    // Helper: 대용량 HTML 문서 생성
    private static string GenerateLargeHtmlDocument(double sizeInMb)
    {
        var targetSize = (int)(sizeInMb * 1024 * 1024);
        var paragraphSize = 500;  // 평균 문단 크기
        var paragraphCount = targetSize / paragraphSize;

        var html = new System.Text.StringBuilder(targetSize);
        html.AppendLine("<html><head><title>Large Document</title></head><body>");

        for (int i = 0; i < paragraphCount; i++)
        {
            if (i % 50 == 0)
            {
                html.AppendLine($"<h2>Section {i / 50 + 1}</h2>");
            }

            html.AppendLine($"<p>This is paragraph {i}. Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, " +
                "quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
                "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
                "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</p>");
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }
}
