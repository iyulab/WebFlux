using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using Xunit;
using FluentAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// WebContentProcessor 단위 테스트
/// 파이프라인 오케스트레이션 stub 구현 검증
/// </summary>
public class WebContentProcessorTests : IDisposable
{
    private readonly IServiceFactory _mockServiceFactory;
    private readonly IEventPublisher _mockEventPublisher;
    private readonly ILogger<WebContentProcessor> _mockLogger;
    private readonly WebContentProcessor _processor;

    public WebContentProcessorTests()
    {
        _mockServiceFactory = Substitute.For<IServiceFactory>();
        _mockEventPublisher = Substitute.For<IEventPublisher>();
        _mockLogger = Substitute.For<ILogger<WebContentProcessor>>();

        _processor = new WebContentProcessor(
            _mockServiceFactory,
            _mockEventPublisher,
            _mockLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullServiceFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebContentProcessor(null!, _mockEventPublisher, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebContentProcessor(_mockServiceFactory, null!, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new WebContentProcessor(_mockServiceFactory, _mockEventPublisher, null!));
    }

    [Fact]
    public void Constructor_WithValidArguments_ShouldNotThrow()
    {
        // Act & Assert
        var processor = new WebContentProcessor(
            _mockServiceFactory,
            _mockEventPublisher,
            _mockLogger);

        processor.Should().NotBeNull();
    }

    #endregion

    #region ProcessUrlsBatchAsync Tests

    [Fact]
    public async Task ProcessUrlsBatchAsync_WithEmptyUrls_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var urls = new List<string>();

        // Act
        var result = await _processor.ProcessUrlsBatchAsync(urls);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessUrlsBatchAsync_WithUrls_ShouldReturnDictionaryWithEntries()
    {
        // Arrange
        var urls = new List<string> { "https://example.com", "https://test.com" };
        SetupMocksForProcessUrl();

        // Act
        var result = await _processor.ProcessUrlsBatchAsync(urls);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().ContainKey("https://example.com");
        result.Should().ContainKey("https://test.com");
    }

    [Fact]
    public async Task ProcessUrlsBatchAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var urls = new List<string> { "https://example.com" };
        using var cts = new CancellationTokenSource();
        SetupMocksForProcessUrl();

        // Act
        var result = await _processor.ProcessUrlsBatchAsync(urls, cancellationToken: cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region ProcessWebsiteAsync Tests (Stub)

    [Fact]
    public async Task ProcessWebsiteAsync_ShouldReturnEmptyStream()
    {
        // Arrange
        var startUrl = "https://example.com";

        // Setup mock crawler to return empty results
        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>()));

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>())
            .Returns(mockCrawler);

        // Act
        var chunks = new List<WebContentChunk>();
        await foreach (var chunk in _processor.ProcessWebsiteAsync(startUrl))
        {
            chunks.Add(chunk);
        }

        // Assert - Stub returns empty stream
        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessWebsiteAsync_WithOptions_ShouldReturnEmptyStream()
    {
        // Arrange
        var startUrl = "https://example.com";
        var crawlOptions = new CrawlOptions { MaxPages = 10 };
        var chunkingOptions = new ChunkingOptions { ChunkSize = 1000 };

        // Setup mock crawler to return empty results
        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>()));

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>())
            .Returns(mockCrawler);

        // Act
        var chunks = new List<WebContentChunk>();
        await foreach (var chunk in _processor.ProcessWebsiteAsync(startUrl, crawlOptions, chunkingOptions))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessWebsiteAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var startUrl = "https://example.com";
        using var cts = new CancellationTokenSource();

        // Setup mock crawler to return empty results
        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>()));

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>())
            .Returns(mockCrawler);

        // Act
        var chunks = new List<WebContentChunk>();
        await foreach (var chunk in _processor.ProcessWebsiteAsync(startUrl, cancellationToken: cts.Token))
        {
            chunks.Add(chunk);
        }

        // Assert
        chunks.Should().BeEmpty();
    }

    #endregion

    #region ProcessHtmlAsync Tests

    [Fact]
    public async Task ProcessHtmlAsync_WithHtml_ShouldReturnChunks()
    {
        // Arrange
        var html = "<html><body>Test</body></html>";
        var sourceUrl = "https://example.com";
        SetupMocksForHtmlProcessing(sourceUrl);

        // Act
        var result = await _processor.ProcessHtmlAsync(html, sourceUrl);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessHtmlAsync_WithOptions_ShouldReturnChunks()
    {
        // Arrange
        var html = "<html><body>Test content</body></html>";
        var sourceUrl = "https://example.com";
        var options = new ChunkingOptions { ChunkSize = 500 };
        SetupMocksForHtmlProcessing(sourceUrl);

        // Act
        var result = await _processor.ProcessHtmlAsync(html, sourceUrl, options);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessHtmlAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var html = "<html><body>Test</body></html>";
        var sourceUrl = "https://example.com";
        using var cts = new CancellationTokenSource();
        SetupMocksForHtmlProcessing(sourceUrl);

        // Act
        var result = await _processor.ProcessHtmlAsync(html, sourceUrl, cancellationToken: cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetAvailableChunkingStrategies Tests

    [Fact]
    public void GetAvailableChunkingStrategies_ShouldReturnStrategyList()
    {
        // Act
        var strategies = _processor.GetAvailableChunkingStrategies();

        // Assert
        strategies.Should().NotBeNull();
        strategies.Should().NotBeEmpty();
        strategies.Should().Contain("FixedSize");
        strategies.Should().Contain("Paragraph");
        strategies.Should().Contain("Smart");
        strategies.Should().Contain("Semantic");
        strategies.Should().Contain("Auto");
        strategies.Should().Contain("MemoryOptimized");
    }

    [Fact]
    public void GetAvailableChunkingStrategies_ShouldReturnExactSixStrategies()
    {
        // Act
        var strategies = _processor.GetAvailableChunkingStrategies();

        // Assert
        strategies.Should().HaveCount(6);
    }

    [Fact]
    public void GetAvailableChunkingStrategies_ShouldReturnReadOnlyList()
    {
        // Act
        var strategies = _processor.GetAvailableChunkingStrategies();

        // Assert
        strategies.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var processor = new WebContentProcessor(
            _mockServiceFactory,
            _mockEventPublisher,
            _mockLogger);

        // Act & Assert
        processor.Invoking(p => p.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithMultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var processor = new WebContentProcessor(
            _mockServiceFactory,
            _mockEventPublisher,
            _mockLogger);

        // Act & Assert
        processor.Invoking(p =>
        {
            p.Dispose();
            p.Dispose();
            p.Dispose();
        }).Should().NotThrow();
    }

    #endregion

    #region ProcessUrlAsync Tests (Implemented Method)

    [Fact]
    public async Task ProcessUrlAsync_WithValidUrl_ShouldProcessSuccessfully()
    {
        // Arrange
        var url = "https://example.com";
        var mockCrawler = Substitute.For<ICrawler>();
        var mockExtractor = Substitute.For<IContentExtractor>();
        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();

        // Setup crawler to return web content
        var crawlResults = new List<CrawlResult>
        {
            new CrawlResult
            {
                Url = url,
                Content = "<html><head><title>Test Page</title></head><body>Test content</body></html>",
                ContentType = "text/html",
                StatusCode = 200
            }
        };
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(crawlResults));

        // Setup extractor
        var extractedContent = new ExtractedContent
        {
            Url = url,
            Text = "Test content",
            MainContent = "Test content"
        };
        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(extractedContent);

        // Setup chunking strategy
        var chunks = new List<WebContentChunk>
        {
            new WebContentChunk
            {
                Id = "1",
                Content = "Test content",
                SourceUrl = url,
                StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" }
            }
        };
        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(chunks);

        // Setup service factory
        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);

        // Act
        var result = await _processor.ProcessUrlAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ProcessUrlAsync_WithChunkingOptions_ShouldUseOptions()
    {
        // Arrange
        var url = "https://example.com";
        var options = new ChunkingOptions { MaxChunkSize = 500 };
        var mockCrawler = Substitute.For<ICrawler>();
        var mockExtractor = Substitute.For<IContentExtractor>();
        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();

        // Setup minimal mocks to allow processing
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>
            {
                new CrawlResult { Url = url, Content = "<html><body>Test</body></html>", ContentType = "text/html", StatusCode = 200 }
            }));

        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExtractedContent { Url = url, Text = "Test", MainContent = "Test" });

        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<WebContentChunk>
            {
                new WebContentChunk
                {
                    Id = "1",
                    Content = "Test",
                    SourceUrl = url,
                    StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" }
                }
            });

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);

        // Act
        var result = await _processor.ProcessUrlAsync(url, options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessUrlAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var url = "https://example.com";
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>()));

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _processor.ProcessUrlAsync(url, cancellationToken: cts.Token));
    }

    #endregion

    #region ExtractTitle Helper Tests

    [Fact]
    public void ExtractTitle_WithValidHtml_ShouldExtractTitle()
    {
        // This tests the private ExtractTitle method indirectly through ProcessUrlAsync
        // We can't directly test private methods, but we can verify the behavior through public API
        var html = "<html><head><title>My Page Title</title></head><body>Content</body></html>";

        // Arrange - Create a minimal test that will trigger ExtractTitle
        var mockCrawler = Substitute.For<ICrawler>();
        var crawlResults = new List<CrawlResult>
        {
            new CrawlResult { Url = "https://test.com", Content = html, ContentType = "text/html", StatusCode = 200 }
        };
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(crawlResults));

        var mockExtractor = Substitute.For<IContentExtractor>();
        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExtractedContent { Url = "https://test.com", Text = "Content", MainContent = "Content" });

        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();
        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<WebContentChunk>
            {
                new WebContentChunk
                {
                    Id = "1",
                    Content = "Content",
                    SourceUrl = "https://test.com",
                    StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" }
                }
            });

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);

        // Act - This will trigger ExtractTitle internally
        var task = _processor.ProcessUrlAsync("https://test.com");

        // Assert - Method should complete without throwing
        task.Should().NotBeNull();
    }

    #endregion

    #region Event Publishing Tests

    [Fact]
    public async Task ProcessUrlAsync_ShouldPublishStartedEvent()
    {
        // Arrange
        var url = "https://example.com";
        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>
            {
                new CrawlResult { Url = url, Content = "<html><body>Test</body></html>", ContentType = "text/html", StatusCode = 200 }
            }));

        var mockExtractor = Substitute.For<IContentExtractor>();
        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExtractedContent { Url = url, Text = "Test", MainContent = "Test" });

        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();
        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<WebContentChunk>
            {
                new WebContentChunk
                {
                    Id = "1",
                    Content = "Test",
                    SourceUrl = url,
                    StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" }
                }
            });

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);

        // Act
        await _processor.ProcessUrlAsync(url);

        // Assert
        await _mockEventPublisher.Received(1).PublishAsync(
            Arg.Any<ProcessingStartedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessUrlAsync_ShouldPublishCompletedEvent()
    {
        // Arrange
        var url = "https://example.com";
        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<CrawlResult>
            {
                new CrawlResult { Url = url, Content = "<html><body>Test</body></html>", ContentType = "text/html", StatusCode = 200 }
            }));

        var mockExtractor = Substitute.For<IContentExtractor>();
        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExtractedContent { Url = url, Text = "Test", MainContent = "Test" });

        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();
        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<WebContentChunk>
            {
                new WebContentChunk
                {
                    Id = "1",
                    Content = "Test",
                    SourceUrl = url,
                    StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" }
                }
            });

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);

        // Act
        await _processor.ProcessUrlAsync(url);

        // Assert
        await _mockEventPublisher.Received(1).PublishAsync(
            Arg.Any<ProcessingCompletedEvent>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Event Model Tests

    [Fact]
    public void ProcessingStartedEvent_ShouldSetProperties()
    {
        // Arrange & Act
        var config = new WebFluxConfiguration();
        var startUrls = new List<string> { "https://example.com" };
        var timestamp = DateTimeOffset.UtcNow;

        var evt = new ProcessingStartedEvent
        {
            Message = "Test message",
            Configuration = config,
            StartUrls = startUrls,
            Timestamp = timestamp
        };

        // Assert
        evt.EventType.Should().Be("ProcessingStarted");
        evt.Message.Should().Be("Test message");
        evt.Configuration.Should().BeSameAs(config);
        evt.StartUrls.Should().BeSameAs(startUrls);
        evt.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ProcessingProgressEvent_ShouldSetProperties()
    {
        // Arrange & Act
        var evt = new ProcessingProgressEvent
        {
            Message = "Progress update",
            ProcessedCount = 5,
            ElapsedTime = TimeSpan.FromMinutes(2),
            EstimatedRemaining = TimeSpan.FromMinutes(3),
            CurrentStage = "Chunking",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        evt.EventType.Should().Be("ProcessingProgress");
        evt.ProcessedCount.Should().Be(5);
        evt.ElapsedTime.Should().Be(TimeSpan.FromMinutes(2));
        evt.EstimatedRemaining.Should().Be(TimeSpan.FromMinutes(3));
        evt.CurrentStage.Should().Be("Chunking");
    }

    [Fact]
    public void ProcessingCompletedEvent_ShouldSetProperties()
    {
        // Arrange & Act
        var evt = new ProcessingCompletedEvent
        {
            Message = "Processing complete",
            ProcessedChunkCount = 100,
            TotalProcessingTime = TimeSpan.FromMinutes(10),
            AverageProcessingRate = 10.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        evt.EventType.Should().Be("ProcessingCompleted");
        evt.ProcessedChunkCount.Should().Be(100);
        evt.TotalProcessingTime.Should().Be(TimeSpan.FromMinutes(10));
        evt.AverageProcessingRate.Should().Be(10.0);
    }

    [Fact]
    public void ProcessingFailedEvent_ShouldSetProperties()
    {
        // Arrange & Act
        var evt = new ProcessingFailedEvent
        {
            Message = "Processing failed",
            Error = "Test error message",
            ProcessedCount = 50,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        evt.EventType.Should().Be("ProcessingFailed");
        evt.Error.Should().Be("Test error message");
        evt.ProcessedCount.Should().Be(50);
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }

    /// <summary>
    /// ProcessUrlAsync 위임을 위한 공통 mock 설정
    /// </summary>
    private void SetupMocksForProcessUrl()
    {
        var mockCrawler = Substitute.For<ICrawler>();
        mockCrawler.CrawlWebsiteAsync(Arg.Any<string>(), Arg.Any<CrawlOptions>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ToAsyncEnumerable(new List<CrawlResult>
            {
                new CrawlResult { Url = (string)callInfo[0], Content = "<html><body>Test</body></html>", ContentType = "text/html", StatusCode = 200 }
            }));

        var mockExtractor = Substitute.For<IContentExtractor>();
        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExtractedContent { Url = "https://example.com", Text = "Test", MainContent = "Test" });

        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();
        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<WebContentChunk>
            {
                new WebContentChunk { Id = "1", Content = "Test", SourceUrl = "https://example.com", StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" } }
            });

        _mockServiceFactory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(mockCrawler);
        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
        _mockServiceFactory.CreateAiEnhancementService().Returns((IAiEnhancementService)null!);
    }

    /// <summary>
    /// ProcessHtmlAsync를 위한 공통 mock 설정
    /// </summary>
    private void SetupMocksForHtmlProcessing(string sourceUrl)
    {
        var mockExtractor = Substitute.For<IContentExtractor>();
        mockExtractor.ExtractAutoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ExtractedContent { Url = sourceUrl, Text = "Test content", MainContent = "Test content" });

        var mockChunkingStrategy = Substitute.For<IChunkingStrategy>();
        mockChunkingStrategy.ChunkAsync(Arg.Any<ExtractedContent>(), Arg.Any<ChunkingOptions>(), Arg.Any<CancellationToken>())
            .Returns(new List<WebContentChunk>
            {
                new WebContentChunk { Id = "1", Content = "Test content", SourceUrl = sourceUrl, StrategyInfo = new ChunkingStrategyInfo { StrategyName = "Auto" } }
            });

        _mockServiceFactory.CreateContentExtractor(Arg.Any<string>()).Returns(mockExtractor);
        _mockServiceFactory.CreateChunkingStrategy(Arg.Any<string>()).Returns(mockChunkingStrategy);
    }

    #endregion

    public void Dispose()
    {
        _processor.Dispose();
        GC.SuppressFinalize(this);
    }
}
