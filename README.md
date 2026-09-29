# WebFlux

A .NET SDK for preprocessing web content for RAG (Retrieval-Augmented Generation) systems.

[![NuGet Version](https://img.shields.io/nuget/v/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![.NET Support](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/iyulab/WebFlux?style=flat-square&color=green)](https://github.com/iyulab/WebFlux/blob/main/LICENSE)

## Overview

WebFlux processes web content into chunks optimized for RAG systems. It handles web crawling, content extraction and chunking.

## Installation

```bash
dotnet add package WebFlux
```

| Package | What it is |
|---------|------------|
| `WebFlux` | Crawling, extraction and chunking — browser-free. `AddWebFlux()` |
| `WebFlux.Playwright` | Playwright-backed rendering for pages that only render with JavaScript. `AddWebFluxPlaywright()` after `AddWebFlux()`, then `CrawlOptions.UseDynamicRendering = true` |

## Quick Start

`AddWebFlux()` needs no AI service: crawling, extraction and chunking run as they are.

```csharp
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Extensions;

var services = new ServiceCollection();
services.AddWebFlux();

await using var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// One page
var chunks = await processor.ProcessUrlAsync("https://example.com");
foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.SequenceNumber}: {chunk.Content}");
}

// A site: pass CrawlOptions — without them ProcessWebsiteAsync processes the start page only
var crawl = new CrawlOptions { MaxDepth = 2, MaxPages = 20 };
await foreach (var chunk in processor.ProcessWebsiteAsync("https://example.com", crawl))
{
    Console.WriteLine($"{chunk.SourceUrl} #{chunk.SequenceNumber}: {chunk.Content}");
}
```

## Features

- **Crawling** — `ProcessWebsiteAsync(url, CrawlOptions)` streams chunks as pages arrive. `CrawlOptions.Strategy`:
  `BreadthFirst` (default), `DepthFirst`, `Sitemap` (reads sitemap.xml), `Dynamic` (needs `WebFlux.Playwright`);
  `UseDynamicRendering = true` routes to `Dynamic` whatever the strategy. `MaxDepth` / `MaxPages` bound the crawl.
- **Chunking strategies** — Auto, Smart, Semantic, Paragraph, FixedSize, MemoryOptimized (`ChunkingOptions.Strategy`, see
  below); an unknown strategy name throws and lists the available ones.
- **Content formats** — HTML, Markdown, JSON, XML and plain text.
- **Web standards** — robots.txt, matched per [RFC 9309](https://www.rfc-editor.org/rfc/rfc9309.html)
  — groups, longest-match with `Allow` winning ties, `*` and `$`. Honoured on every crawl entry
  point and on the extract API; turn it off per call with `CrawlOptions.RespectRobotsTxt` or
  `ExtractOptions.RespectRobotsTxt` (both default to on). A refusal is reported as
  `CrawlResult.DisallowedByRobotsTxt` / `ExtractErrorCodes.DisallowedByRobotsTxt` rather than as a
  failed request.
- **Identity** — every request carries one User-Agent — `CrawlOptions.UserAgent`, default
  `WebFluxUserAgent.Default` (`WebFlux/{version} (+https://github.com/iyulab/WebFlux)`) — and the
  robots.txt group is chosen by its product token, so `"MyBot/1.0"` follows `User-agent: MyBot`.
  `CrawlOptions.CustomHeaders` are sent per request.
- **Request timeouts** — `CrawlOptions.TimeoutMs` (default 30 000) and `ExtractOptions.TimeoutSeconds`
  (default 15) bound every request a call makes — the page, its robots.txt, a sitemap — on the HTTP
  and the Playwright crawlers alike. A timeout is **not retried**, whatever `MaxRetries` says: it
  arrives as `CrawlResult.TimedOut` / `ExtractErrorCodes.Timeout` after about the time you asked for,
  from `CrawlAsync`, `CrawlWebsiteAsync` and `CrawlSitemapAsync` alike. A non-positive value is not
  "no timeout": the call throws `ArgumentException` naming the option before any request is made.
- **Retries** — `MaxRetries` (on `CrawlOptions` for a crawl, on `ExtractOptions` for a single URL)
  counts attempts once, never nested. Transport errors, 408, 429 and 5xx are retried with backoff;
  a 404/403/410, a robots.txt refusal and a timeout are final.
- **Rich metadata** — SEO, Open Graph, Schema.org and Twitter Cards.
  - On the crawl path (`ProcessWebsiteAsync`): `CrawlOptions.UseHtmlMetadata` (default on, no AI) attaches the HTML
    snapshot to `Metadata.HtmlMetadata`; `CrawlOptions.EnableMetadataExtraction` (opt-in) runs the AI extractor with
    `MetadataSchema`/`CustomMetadataPrompt` when an `ITextCompletionService` (or your own `IWebMetadataExtractor`) is
    registered, sampling long pages to `MetadataExtractionMaxChars` (title + headings + first N characters).
- **Events** — `IEventPublisher` (see below) for processing, per-URL and extraction events.
- **Configuration** — `AddWebFlux(config => …)` or `AddWebFlux(configuration)` (the `"WebFlux"` section) sets the defaults
  `ProcessUrlAsync` starts from: crawling, chunking, AI enhancement.

## Chunking Strategies

| Strategy | Use Case |
|----------|----------|
| Auto | Automatically selects best strategy based on content |
| Smart | Structured HTML documentation |
| Semantic | General web pages and articles — needs a FluxCurator `IEmbedder` registered before `AddWebFlux()` |
| MemoryOptimized | Same token-based splitting as FixedSize; kept as a name for large-document callers |
| Paragraph | Markdown with natural boundaries |
| FixedSize | Uniform chunks for testing |

Chunking runs on [FluxCurator](https://www.nuget.org/packages/FluxCurator/). For `Semantic`, register its embedder
(`FluxCurator.Core.Core.IEmbedder`) in the container before `AddWebFlux()`; the chunker factory picks it up.

## Services You Can Provide

WebFlux uses the **Interface Provider** pattern: nothing below is required for crawling, extraction and chunking.

### ITextCompletionService (optional)

LLM text completion, from the shared [`Flux.Abstractions`](https://www.nuget.org/packages/Flux.Abstractions/)
contract package — only `CompleteAsync` is required, the rest have default implementations. With one registered:

- **AI metadata** on the crawl path — `CrawlOptions.EnableMetadataExtraction = true` (above).
- **AI enhancement** (summaries, rewrites) on `ProcessUrlAsync` — register the enhancement service and turn it on in the
  configuration:

```csharp
using Flux.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Extensions;

services.AddScoped<ITextCompletionService, MyCompletionService>();
services.AddWebFlux(config => config.AiEnhancement.Enabled = true);
services.AddWebFluxAIEnhancement();
```

Implement `WebFlux.Core.Interfaces.IWebLlmService` instead (it extends `ITextCompletionService` with
`IsAvailableAsync` and `GetHealthInfo`) when you also want health checks; `AddWebFluxAIServices<TService>()` registers
it as the completion service and adds the enhancement service.

## Main Processor

`IWebContentProcessor` is the entry point; it is also registered as the two focused interfaces below.

```csharp
using WebFlux.Core.Options;

// Single URL
var chunks = await processor.ProcessUrlAsync("https://example.com");

// Website crawling (streaming)
await foreach (var chunk in processor.ProcessWebsiteAsync(url, crawlOptions, chunkOptions))
{
    // Process chunk
}

// Several URLs: chunks per URL
var results = await processor.ProcessUrlsBatchAsync(urls, chunkOptions);

// HTML you already have
var fromHtml = await processor.ProcessHtmlAsync("<html>…</html>", "https://example.com/page", chunkOptions);
```

For consumers that only need extraction or chunking:

```csharp
// Extraction only (single URL, batch, or streamed batch: ExtractBatchAsync / ExtractBatchStreamAsync)
var extractor = provider.GetRequiredService<IContentExtractService>();
var result = await extractor.ExtractContentAsync("https://example.com");

// Chunking only
var chunker = provider.GetRequiredService<IContentChunkService>();
var chunks = await chunker.ProcessUrlAsync("https://example.com");
```

## Extensibility

### IChunkingStrategy

Implement `WebFlux.Core.Interfaces.IChunkingStrategy` — `Name`, `Description` and
`ChunkAsync(ExtractedContent, ChunkingOptions?, CancellationToken)` returning `IReadOnlyList<WebContentChunk>`.

### IEventPublisher

Subscribe to pipeline events for monitoring and metrics. `IEventPublisher` is registered as a singleton by `AddWebFlux()`.

```csharp
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models.Events;

var publisher = provider.GetRequiredService<IEventPublisher>();

using var started = publisher.Subscribe<UrlProcessingStartedEvent>(e => Console.WriteLine($"Processing {e.Url}"));
using var failed = publisher.Subscribe<ContentExtractionFailedEvent>(e => Console.WriteLine($"{e.Url}: {e.Error}"));
using var done = publisher.Subscribe<ProcessingCompletedEvent>(e =>
    Console.WriteLine($"{e.ProcessedChunkCount} chunks in {e.TotalProcessingTime}"));

// Or every event
using var all = publisher.SubscribeAll(e =>
{
    logger.LogInformation("WebFlux event {EventType}", e.EventType);
    return Task.CompletedTask;
});
```

**Published events** (`WebFlux.Core.Models.Events`):

| When | Events |
|------|--------|
| `ProcessUrlAsync` / the configured pipeline | `ProcessingStartedEvent`, `ProcessingProgressEvent`, `ProcessingCompletedEvent` |
| Every URL a crawler fetches | `UrlProcessingStartedEvent` |
| Every extraction | `ContentExtractionStartedEvent`, `ContentExtractionCompletedEvent`, `ContentExtractionFailedEvent` |
| Retries and circuit breaking | `ResilienceEvent` |

The namespace declares more event types (crawling, chunking and monitoring); they are not published yet.
All events derive from `ProcessingEvent` (`EventId`, `EventType`, `Timestamp`, `Severity`, `CorrelationId`).

## Configuration

```csharp
using WebFlux.Core.Options;

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
