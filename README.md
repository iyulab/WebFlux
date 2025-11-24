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

// Process a website
await foreach (var result in processor.ProcessWithProgressAsync("https://example.com"))
{
    if (result.IsSuccess && result.Result != null)
    {
        foreach (var chunk in result.Result)
        {
            Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content}");
        }
    }
}
```

## Features

- **Interface-Based Design**: Bring your own AI services (OpenAI, Anthropic, Azure, local models)
- **Multiple Chunking Strategies**: Auto, Smart, Semantic, Intelligent, MemoryOptimized, Paragraph, FixedSize, DomStructure
- **Content Formats**: HTML, Markdown, JSON, XML, PDF
- **Web Standards**: robots.txt, sitemap.xml, ai.txt, llms.txt, manifest.json
- **Streaming**: Process large websites with AsyncEnumerable
- **Parallel Processing**: Concurrent crawling and processing
- **Rich Metadata**: Web document metadata extraction (SEO, Open Graph, Schema.org, Twitter Cards)
- **Progress Tracking**: Real-time batch crawling progress with detailed statistics

## Chunking Strategies

| Strategy | Use Case |
|----------|----------|
| Auto | Automatically selects best strategy based on content |
| Smart | Structured HTML documentation |
| Semantic | General web pages and articles |
| Intelligent | Blogs and knowledge bases |
| MemoryOptimized | Large documents with memory constraints |
| Paragraph | Markdown with natural boundaries |
| FixedSize | Uniform chunks for testing |
| DomStructure | HTML DOM structure-based chunking preserving semantic boundaries |

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
LLM text completion for multimodal processing and content reconstruction:

```csharp
public interface ITextCompletionService
{
    Task<string> CompleteAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> CompleteStreamAsync(string prompt, TextCompletionOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
```

#### IImageToTextService (Optional)
Image-to-text conversion for multimodal content:

```csharp
public interface IImageToTextService
{
    Task<string> ConvertImageToTextAsync(string imageUrl, ImageToTextOptions? options = null, CancellationToken cancellationToken = default);
    Task<string> ExtractTextFromImageAsync(string imageUrl, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
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

#### IProgressReporter & IEventPublisher
Monitor processing progress and subscribe to system events:

```csharp
// Progress monitoring
await foreach (var progress in progressReporter.MonitorProgressAsync(jobId))
{
    Console.WriteLine($"Progress: {progress.Progress:P0}");
}

// Event subscription
eventPublisher.Subscribe<PageProcessedEvent>(async evt => await LogEvent(evt));
```

For detailed implementation examples, see the [Tutorial](docs/TUTORIAL.md#핵심-인터페이스).

## Configuration

```csharp
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 100,
    RespectRobotsTxt = true,
    UserAgent = "MyBot/1.0"
};

var chunkOptions = new ChunkingOptions
{
    Strategy = "Auto",
    MaxChunkSize = 512,
    OverlapSize = 64
};

await foreach (var result in processor.ProcessWithProgressAsync(url, options, chunkOptions))
{
    // Handle results
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
