# WebFlux

A .NET SDK for preprocessing web content for RAG (Retrieval-Augmented Generation) systems.

[![NuGet Version](https://img.shields.io/nuget/v/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![.NET Support](https://img.shields.io/badge/.NET-8%20|%209-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
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
- **Multiple Chunking Strategies**: Auto, Smart, Semantic, Intelligent, MemoryOptimized, Paragraph, FixedSize
- **Content Formats**: HTML, Markdown, JSON, XML, PDF
- **Web Standards**: robots.txt, sitemap.xml, ai.txt, llms.txt, manifest.json
- **Streaming**: Process large websites with AsyncEnumerable
- **Parallel Processing**: Concurrent crawling and processing

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

## Core Interfaces

You provide implementations for AI services:

```csharp
public interface ITextEmbeddingService
{
    Task<double[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public interface ITextCompletionService // Optional, for content reconstruction
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
```

WebFlux handles the rest: crawling, extraction, analysis, and chunking.

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

## License

MIT License - see LICENSE file for details.

## Support

- Issues: [GitHub Issues](https://github.com/iyulab/WebFlux/issues)
- Package: [NuGet](https://www.nuget.org/packages/WebFlux/)
