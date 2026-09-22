# WebFlux

A .NET SDK for preprocessing web content for RAG (Retrieval-Augmented Generation) systems.

[![NuGet Version](https://img.shields.io/nuget/v/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![.NET Support](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/iyulab/WebFlux?style=flat-square&color=green)](https://github.com/iyulab/WebFlux/blob/main/LICENSE)

## Overview

WebFlux processes web content into chunks optimized for RAG systems. It handles web crawling, content extraction, and intelligent chunking with support for multiple content formats.

## Installation

```bash
dotnet add package WebFlux
```

`WebFlux` itself is browser-free. For pages that only render with JavaScript, add **WebFlux.Playwright** as well
(`dotnet add package WebFlux.Playwright`). It provides Playwright-backed dynamic rendering.

## Quick Start

```csharp
using WebFlux;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register your AI service implementations
services.AddScoped<ITextEmbeddingService, YourEmbeddingService>();
services.AddScoped<ITextCompletionService, YourLLMService>(); // Optional

// Add WebFlux
services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// Process a single URL
var chunks = await processor.ProcessUrlAsync("https://example.com");
foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content}");
}

// Or stream a whole website
await foreach (var chunk in processor.ProcessWebsiteAsync("https://example.com"))
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content}");
}
```

## Features

- **Interface-Based Design**: Bring your own AI services (OpenAI, Anthropic, Azure, local models)
- **Multiple Chunking Strategies**: Auto, Smart, Semantic, Paragraph, FixedSize, MemoryOptimized — an unknown strategy name throws and lists the available ones
- **Content Formats**: HTML, Markdown, JSON, XML, PDF
- **Web Standards**: robots.txt, matched per [RFC 9309](https://www.rfc-editor.org/rfc/rfc9309.html)
  — groups, longest-match with `Allow` winning ties, `*` and `$`. Honoured on every crawl entry
  point and on the extract API; turn it off per call with `CrawlOptions.RespectRobotsTxt` or
  `ExtractOptions.RespectRobotsTxt` (both default to on). A refusal is reported as
  `CrawlResult.DisallowedByRobotsTxt` / `ExtractErrorCodes.DisallowedByRobotsTxt` rather than as a
  failed request. Also sitemap.xml (`CrawlStrategy.Sitemap`).
- **Identity**: every request carries one User-Agent — `CrawlOptions.UserAgent`, default
  `WebFluxUserAgent.Default` (`WebFlux/{version} (+https://github.com/iyulab/WebFlux)`) — and the
  robots.txt group is chosen by its product token, so `"MyBot/1.0"` follows `User-agent: MyBot`.
  `CrawlOptions.CustomHeaders` are sent per request.
- **Request timeouts**: `CrawlOptions.TimeoutMs` (default 30 000) and `ExtractOptions.TimeoutSeconds`
  (default 15) bound every request a call makes — the page, its robots.txt, a sitemap — on the HTTP
  and the Playwright crawlers alike. A timeout is **not retried**, whatever `MaxRetries` says: it
  arrives as `CrawlResult.TimedOut` / `ExtractErrorCodes.Timeout` after about the time you asked for,
  from `CrawlAsync`, `CrawlWebsiteAsync` and `CrawlSitemapAsync` alike. A non-positive value is not
  "no timeout": the call throws `ArgumentException` naming the option before any request is made.
- **Retries**: `MaxRetries` (on `CrawlOptions` for a crawl, on `ExtractOptions` for a single URL)
  counts attempts once, never nested. Transport errors, 408, 429 and 5xx are retried with backoff;
  a 404/403/410, a robots.txt refusal and a timeout are final.
- **Streaming**: Process large websites with AsyncEnumerable
- **Parallel Processing**: Concurrent crawling and processing
- **Rich Metadata**: Web document metadata extraction (SEO, Open Graph, Schema.org, Twitter Cards)
  - On the crawl path (`ProcessWebsiteAsync`): `CrawlOptions.UseHtmlMetadata` (default on, no AI) attaches the HTML
    snapshot to `Metadata.HtmlMetadata`; `CrawlOptions.EnableMetadataExtraction` (opt-in) runs the AI extractor with
    `MetadataSchema`/`CustomMetadataPrompt` when an `ITextCompletionService` (or your own `IWebMetadataExtractor`) is
    registered, sampling long pages to `MetadataExtractionMaxChars` (title + headings + first N characters).
- **Progress Tracking**: Real-time batch crawling progress with detailed statistics

## Chunking Strategies

| Strategy | Use Case |
|----------|----------|
| Auto | Automatically selects best strategy based on content |
| Smart | Structured HTML documentation |
| Semantic | General web pages and articles (requires an embedder registered with FluxCurator) |
| MemoryOptimized | Same token-based splitting as FixedSize; kept as a name for large-document callers |
| Paragraph | Markdown with natural boundaries |
| FixedSize | Uniform chunks for testing |

## Core Interfaces

WebFlux uses the **Interface Provider** pattern. You provide AI service implementations, and WebFlux handles crawling, extraction, and chunking.

### Required AI Services

#### ITextEmbeddingService (Required)
Vector embedding generation for semantic chunking:

```csharp
public interface ITextEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
    int MaxTokens { get; }
    int EmbeddingDimension { get; }
}
```

### Optional AI Services

#### ITextCompletionService (Optional)
LLM text completion for AI enhancement (summaries, rewrites, metadata). Defined in the shared
[`Flux.Abstractions`](https://www.nuget.org/packages/Flux.Abstractions/) contract package — only
`CompleteAsync` is required, the rest have default implementations:

```csharp
public interface ITextCompletionService
{
    Task<string> CompleteAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    Task<string> CompleteJsonAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> CompleteBatchAsync(IEnumerable<string> prompts, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> CompleteStreamAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
}
```

Implement `WebFlux.Core.Interfaces.IWebLlmService` instead (it extends `ITextCompletionService`)
when you also want health-check support:

```csharp
public interface IWebLlmService : ITextCompletionService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    ServiceHealthInfo GetHealthInfo();
}
```

### Main Processor

#### IWebContentProcessor
The main entry point for all web content processing:

```csharp
// Single URL processing
var chunks = await processor.ProcessUrlAsync("https://example.com");

// Website crawling (streaming)
await foreach (var chunk in processor.ProcessWebsiteAsync(url, crawlOptions, chunkOptions))
{
    // Process chunk
}

// Batch processing
var results = await processor.ProcessUrlsBatchAsync(urls, chunkOptions);
```

#### Focused Interfaces (ISP)
For consumers that only need extraction or chunking:

```csharp
// Extraction only
var extractor = provider.GetRequiredService<IContentExtractService>();
var result = await extractor.ExtractContentAsync("https://example.com");

// Chunking only
var chunker = provider.GetRequiredService<IContentChunkService>();
var chunks = await chunker.ProcessUrlAsync("https://example.com");
```

### Extensibility

#### IChunkingStrategy
Implement custom chunking strategies:

```csharp
public interface IChunkingStrategy
{
    string Name { get; }
    string Description { get; }
    Task<IReadOnlyList<WebContentChunk>> ChunkAsync(ExtractedContent content, ChunkingOptions? options = null, CancellationToken cancellationToken = default);
}
```

#### IEventPublisher
Subscribe to pipeline events for monitoring, metrics collection, and observability.
`IEventPublisher` is automatically registered as a singleton when you call `AddWebFlux()`.

```csharp
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models.Events;

var publisher = provider.GetRequiredService<IEventPublisher>();

// Subscribe to specific event types
using var s1 = publisher.Subscribe<PageCrawledEvent>(async e =>
{
    Console.WriteLine($"Crawled {e.Url} [{e.StatusCode}] in {e.ProcessingTimeMs}ms");
    await metrics.RecordPageCrawl(e);
});

using var s2 = publisher.Subscribe<ChunkGeneratedEvent>(e =>
{
    Console.WriteLine($"Chunk #{e.SequenceNumber} ({e.ChunkSize} tokens) from {e.SourceUrl}");
});

using var s3 = publisher.Subscribe<ErrorOccurredEvent>(e =>
{
    Console.WriteLine($"[{e.ErrorCategory}] {e.ErrorCode}: {e.Message}");
});

// Or subscribe to ALL events
using var sAll = publisher.SubscribeAll(async e =>
{
    await logger.LogEventAsync(e.EventType, e);
});
```

**Available event types** (`WebFlux.Core.Models.Events` namespace):

| Category | Events |
|----------|--------|
| Pipeline | `ProcessingStartedEvent`, `ProcessingProgressEvent`, `ProcessingCompletedEvent`, `ProcessingFailedEvent` |
| Crawling | `CrawlingStartedEvent`, `CrawlingCompletedEvent`, `PageCrawledEvent`, `UrlProcessingStartedEvent`, `UrlProcessedEvent`, `UrlProcessingFailedEvent` |
| Extraction | `ContentExtractionStartedEvent`, `ContentExtractionCompletedEvent`, `ContentExtractionFailedEvent`, `ImageProcessedEvent` |
| Chunking | `ChunkingStartedEvent`, `ChunkingCompletedEvent`, `ChunkGeneratedEvent` |
| Monitoring | `ErrorOccurredEvent`, `PerformanceMetricsEvent` |

All events derive from `ProcessingEvent` (base class with `EventId`, `EventType`, `Timestamp`, `Severity`, `CorrelationId`).

For detailed implementation examples, see the [Tutorial](docs/TUTORIAL.md#핵심-인터페이스).

## Configuration

```csharp
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 100,
    RespectRobotsTxt = true,
    UserAgent = "MyBot/1.0",
    TimeoutMs = 10_000          // per request; a timeout is not retried
};

var chunkOptions = new ChunkingOptions
{
    Strategy = ChunkingStrategyType.Auto,
    MaxChunkSize = 512,
    ChunkOverlap = 64
};

await foreach (var chunk in processor.ProcessWebsiteAsync(url, options, chunkOptions))
{
    // Handle chunk
}
```

## Documentation

- [Tutorial](docs/TUTORIAL.md) - Step-by-step guide with practical examples
- [Architecture](docs/ARCHITECTURE.md) - System design and pipeline
- [Interfaces](docs/INTERFACES.md) - API contracts and implementation guide
- [Chunking Strategies](docs/CHUNKING_STRATEGIES.md) - Detailed strategy guide
- [Changelog](CHANGELOG.md) - Version history and release notes

## License

MIT License - see LICENSE file for details.

## Support

- Issues: [GitHub Issues](https://github.com/iyulab/WebFlux/issues)
- Package: [NuGet](https://www.nuget.org/packages/WebFlux/)
