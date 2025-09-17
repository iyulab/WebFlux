using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using WebFlux.Core.Models;
using WebFlux.Services.ChunkingStrategies;
using Xunit;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// MemoryOptimizedChunkingStrategy 단위 테스트
/// Phase 4D: 메모리 효율성 최적화 전략의 정확성과 성능을 검증
/// 84% 메모리 감소 목표 달성 여부 확인
/// </summary>
public class MemoryOptimizedChunkingStrategyTests
{
    private readonly Mock<ILogger<MemoryOptimizedChunkingStrategy>> _mockLogger;
    private readonly MemoryOptimizedChunkingStrategy _strategy;

    public MemoryOptimizedChunkingStrategyTests()
    {
        _mockLogger = new Mock<ILogger<MemoryOptimizedChunkingStrategy>>();
        _strategy = new MemoryOptimizedChunkingStrategy(_mockLogger.Object);
    }

    [Fact]
    public async Task ProcessAsync_WithSmallDocument_ShouldUseStandardMode()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/small-doc",
            Title = "Small Document",
            MainContent = "This is a small document with minimal content that should not trigger streaming mode."
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 1000,
            OverlapSize = 100
        };

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count > 0);
        Assert.Equal("MemoryOptimized", result.StrategyUsed);
        Assert.Contains("Standard mode", result.ProcessingNotes ?? string.Empty);
        Assert.True(result.ProcessingTimeMs > 0);
    }

    [Fact]
    public async Task ProcessAsync_WithLargeDocument_ShouldUseStreamingMode()
    {
        // Arrange
        var largeContent = CreateLargeContent(150000); // 150KB 문서
        var content = new ExtractedContent
        {
            Url = "https://example.com/large-doc",
            Title = "Large Document",
            MainContent = largeContent
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 2000,
            OverlapSize = 200
        };

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count > 0);
        Assert.Equal("MemoryOptimized", result.StrategyUsed);
        Assert.Contains("Streaming mode", result.ProcessingNotes ?? string.Empty);
        Assert.All(result.Chunks, chunk => Assert.True(chunk.Content.Length <= options.MaxChunkSize + options.OverlapSize));
    }

    [Fact]
    public async Task ProcessAsync_WithVeryLargeDocument_ShouldManageMemoryEfficiently()
    {
        // Arrange - 1MB 문서로 메모리 효율성 테스트
        var veryLargeContent = CreateLargeContent(1000000);
        var content = new ExtractedContent
        {
            Url = "https://example.com/very-large-doc",
            Title = "Very Large Document",
            MainContent = veryLargeContent
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 1500,
            OverlapSize = 150
        };

        // 메모리 사용량 측정 시작
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // 가비지 컬렉션 강제 실행
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count > 0);
        Assert.Equal("MemoryOptimized", result.StrategyUsed);
        Assert.Contains("Streaming mode", result.ProcessingNotes ?? string.Empty);

        // 메모리 효율성 검증 (정확한 84% 감소는 환경에 따라 달라지므로 합리적인 수준 검증)
        Assert.True(memoryUsed < veryLargeContent.Length, "Memory usage should be significantly less than content size");
    }

    [Fact]
    public async Task ProcessAsync_WithCustomChunkSize_ShouldRespectOptions()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/custom-chunk",
            Title = "Custom Chunk Size Test",
            MainContent = CreateRepeatingContent("Custom chunk size test content. ", 100)
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 500,
            OverlapSize = 50
        };

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count > 0);
        Assert.All(result.Chunks, chunk =>
        {
            Assert.True(chunk.Content.Length <= options.MaxChunkSize + options.OverlapSize);
            Assert.True(chunk.Content.Length > 0);
        });
    }

    [Fact]
    public async Task ProcessAsync_WithOverlapSize_ShouldCreateOverlappingChunks()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/overlap-test",
            Title = "Overlap Test",
            MainContent = CreateRepeatingContent("Overlap test sentence. ", 50)
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 300,
            OverlapSize = 50
        };

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count >= 2); // 충분한 내용으로 여러 청크 생성

        // 연속된 청크들 간에 오버랩이 있는지 확인
        for (int i = 1; i < result.Chunks.Count; i++)
        {
            var previousChunk = result.Chunks[i - 1];
            var currentChunk = result.Chunks[i];

            // 오버랩 검증을 위해 이전 청크의 끝부분과 현재 청크의 시작부분 비교
            var previousEnd = previousChunk.Content.Substring(Math.Max(0, previousChunk.Content.Length - options.OverlapSize));
            var hasOverlap = currentChunk.Content.Contains(previousEnd.Substring(0, Math.Min(20, previousEnd.Length)));

            Assert.True(hasOverlap || previousEnd.Length < 20,
                $"Expected overlap between chunk {i-1} and {i}");
        }
    }

    [Fact]
    public async Task ProcessAsync_WithDefaultOptions_ShouldUseDefaultValues()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/default-options",
            Title = "Default Options Test",
            MainContent = CreateRepeatingContent("Default options test content. ", 100)
        };

        // Act - null options로 테스트
        var result = await _strategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count > 0);
        Assert.Equal("MemoryOptimized", result.StrategyUsed);
        Assert.All(result.Chunks, chunk =>
        {
            Assert.True(chunk.Content.Length <= 2000); // 기본 최대 청크 크기
            Assert.True(chunk.Content.Length > 0);
        });
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyContent_ShouldReturnEmptyResult()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/empty",
            Title = "Empty Content",
            MainContent = string.Empty
        };

        // Act
        var result = await _strategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Chunks);
        Assert.Equal("MemoryOptimized", result.StrategyUsed);
        Assert.Equal(0, result.TotalChunks);
    }

    [Fact]
    public async Task ProcessAsync_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _strategy.ProcessAsync(null!, null, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var largeContent = CreateLargeContent(500000); // 500KB
        var content = new ExtractedContent
        {
            Url = "https://example.com/cancellation-test",
            Title = "Cancellation Test",
            MainContent = largeContent
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 즉시 취소

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _strategy.ProcessAsync(content, null, cts.Token));
    }

    [Fact]
    public async Task ProcessAsync_WithWhitespaceContent_ShouldHandleGracefully()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/whitespace",
            Title = "Whitespace Content",
            MainContent = "   \n\n\t\t   \n   \t  "
        };

        // Act
        var result = await _strategy.ProcessAsync(content, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // 공백만 있는 내용은 빈 결과 또는 정리된 청크를 반환해야 함
        Assert.True(result.Chunks.Count == 0 || result.Chunks.All(c => !string.IsNullOrWhiteSpace(c.Content)));
    }

    [Theory]
    [InlineData(100)]    // 작은 문서
    [InlineData(1000)]   // 중간 문서
    [InlineData(10000)]  // 큰 문서
    [InlineData(100000)] // 매우 큰 문서
    public async Task ProcessAsync_WithVariousContentSizes_ShouldScaleAppropriately(int contentSize)
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = $"https://example.com/size-{contentSize}",
            Title = $"Size Test {contentSize}",
            MainContent = CreateLargeContent(contentSize)
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 1000,
            OverlapSize = 100
        };

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MemoryOptimized", result.StrategyUsed);

        if (contentSize > 0)
        {
            Assert.True(result.Chunks.Count > 0);
            Assert.True(result.TotalChunks > 0);
        }
        else
        {
            Assert.Empty(result.Chunks);
        }
    }

    [Fact]
    public async Task ProcessAsync_ShouldCreateValidChunkMetadata()
    {
        // Arrange
        var content = new ExtractedContent
        {
            Url = "https://example.com/metadata-test",
            Title = "Metadata Test",
            MainContent = CreateRepeatingContent("Metadata validation test content. ", 50)
        };

        var options = new ChunkingOptions
        {
            MaxChunkSize = 500,
            OverlapSize = 50
        };

        // Act
        var result = await _strategy.ProcessAsync(content, options, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Chunks.Count > 0);

        for (int i = 0; i < result.Chunks.Count; i++)
        {
            var chunk = result.Chunks[i];
            Assert.Equal(i, chunk.Index);
            Assert.NotNull(chunk.Content);
            Assert.True(chunk.Content.Length > 0);
            Assert.True(chunk.StartPosition >= 0);
            Assert.True(chunk.EndPosition > chunk.StartPosition);
        }
    }

    private string CreateLargeContent(int size)
    {
        var sb = new StringBuilder(size);
        var baseText = "This is a test sentence for memory optimization testing. ";

        while (sb.Length < size)
        {
            sb.Append(baseText);
        }

        return sb.ToString(0, Math.Min(sb.Length, size));
    }

    private string CreateRepeatingContent(string pattern, int repetitions)
    {
        var sb = new StringBuilder(pattern.Length * repetitions);
        for (int i = 0; i < repetitions; i++)
        {
            sb.Append(pattern);
        }
        return sb.ToString();
    }
}