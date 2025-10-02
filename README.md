# WebFlux

A .NET SDK for preprocessing web content for RAG (Retrieval-Augmented Generation) systems.

[![NuGet Version](https://img.shields.io/nuget/v/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/WebFlux?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/WebFlux/)
[![.NET Support](https://img.shields.io/badge/.NET-8%20|%209-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/iyulab/WebFlux?style=flat-square&color=green)](https://github.com/iyulab/WebFlux/blob/main/LICENSE)

## Overview

WebFlux is a .NET library that processes web content into chunks suitable for RAG systems. It handles the complete pipeline from web crawling to content chunking, with support for various content formats and processing strategies.

### What is WebFlux?

WebFlux transforms web content into structured, semantic chunks optimized for retrieval systems. The library provides:

- **Content Extraction**: Parse HTML, Markdown, JSON, XML, and other web formats
- **Content Analysis**: Analyze document structure, quality, and metadata
- **Content Reconstruction**: Optionally enhance content with LLM-based strategies
- **Content Chunking**: Split content into semantic chunks with configurable strategies

### Architecture

WebFlux follows an interface-based architecture where the library defines the contracts, and consuming applications provide implementations for AI services:

**What WebFlux Provides:**
- Processing pipeline and orchestration
- Content extraction and parsing
- Chunking strategies and algorithms
- Web crawling and metadata analysis
- Interface definitions for AI services

**What You Provide:**
- LLM service implementation (ITextCompletionService)
- Embedding service implementation (ITextEmbeddingService)
- Image processing implementation (IImageToTextService) - optional
- Vector storage implementation (IVectorStore)

This design allows you to use any LLM provider (OpenAI, Anthropic, Azure, local models) while maintaining a consistent processing pipeline.

## Features

- **4-Stage Processing Pipeline**: Extract → Analyze → Reconstruct → Chunk
- **Multiple Chunking Strategies**: Auto, Smart, Semantic, Intelligent, MemoryOptimized, Paragraph, FixedSize
- **Content Reconstruction**: Optional LLM-based enhancement with None, Summarize, Expand, Rewrite, Enrich strategies
- **Web Metadata Support**: robots.txt, sitemap.xml, ai.txt, llms.txt, manifest.json, and 10+ other standards
- **Multimodal Processing**: Text and image content processing
- **Streaming Support**: Process large websites with AsyncEnumerable
- **Parallel Processing**: Concurrent crawling and processing
- **Extensible Design**: Implement custom extractors, strategies, and processors

## Installation

**NuGet Package Manager:**
```powershell
Install-Package WebFlux
```

**dotnet CLI:**
```bash
dotnet add package WebFlux
```

**.csproj:**
```xml
<PackageReference Include="WebFlux" Version="0.1.0" />
```

## Quick Start

```csharp
using WebFlux;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register your AI service implementations
services.AddScoped<ITextCompletionService, YourLLMService>();
services.AddScoped<ITextEmbeddingService, YourEmbeddingService>();
services.AddScoped<IImageToTextService, YourVisionService>(); // Optional

// Register your vector store implementation
services.AddScoped<IVectorStore, YourVectorStore>();

// Register WebFlux
services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// Process a website
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 100,
    RespectRobotsTxt = true
};

await foreach (var result in processor.ProcessWithProgressAsync("https://example.com", options))
{
    if (result.IsSuccess && result.Result != null)
    {
        foreach (var chunk in result.Result)
        {
            // Store chunks in your vector database
            await StoreChunk(chunk);
        }
    }
}
```

## Core Concepts

### Processing Pipeline

WebFlux processes web content through four stages:

1. **Extract**: Fetch and parse web content (HTML, Markdown, JSON, XML, PDF)
2. **Analyze**: Analyze document structure, quality metrics, and metadata
3. **Reconstruct**: Optionally enhance content using LLM strategies
4. **Chunk**: Split content into semantic chunks for retrieval

### Chunking Strategies

Choose a chunking strategy based on your content and requirements:

| Strategy | Use Case |
|----------|----------|
| Auto | Automatically selects the best strategy |
| Smart | HTML documentation, structured content |
| Semantic | General web pages, articles |
| Intelligent | Blogs, news, knowledge bases |
| MemoryOptimized | Large documents, memory constraints |
| Paragraph | Markdown docs, natural boundaries |
| FixedSize | Uniform chunks, testing |

### Reconstruction Strategies

Optionally enhance content quality before chunking:

| Strategy | Description | Requires LLM |
|----------|-------------|--------------|
| None | Use original content | No |
| Summarize | Create condensed version | Yes |
| Expand | Add explanations and examples | Yes |
| Rewrite | Improve clarity and consistency | Yes |
| Enrich | Add context and metadata | Yes |

Note: LLM-based strategies require ITextCompletionService implementation. If not provided, the system automatically falls back to "None" strategy with appropriate warnings.

### Web Metadata Standards

WebFlux analyzes multiple web standards to optimize crawling and content extraction:

- **robots.txt**: Crawling rules and permissions
- **sitemap.xml**: Site structure and URL discovery
- **ai.txt**: AI usage policies and guidelines
- **llms.txt**: Site structure for AI agents
- **manifest.json**: PWA metadata
- **security.txt**: Security policies
- **.well-known**: Standard metadata
- And more (package.json, ads.txt, humans.txt, etc.)

## Documentation

For detailed guides and advanced usage:

- **[Tutorial](docs/TUTORIAL.md)**: Step-by-step installation and usage guide
- **[Pipeline Design](docs/PIPELINE_DESIGN.md)**: Processing pipeline architecture
- **[Interfaces](docs/INTERFACES.md)**: Interface contracts and implementations
- **[Chunking Strategies](docs/CHUNKING_STRATEGIES.md)**: Detailed strategy guide

## Example: Basic Usage

```csharp
// Simple single-page processing
var processor = provider.GetRequiredService<IWebContentProcessor>();

var chunks = await processor.ChunkAsync(
    "https://example.com/article",
    new ChunkingOptions
    {
        Strategy = "Auto",
        MaxChunkSize = 512,
        OverlapSize = 64
    }
);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content}");
}
```

## Example: Custom Implementation

```csharp
// Implement your LLM service
public class MyLLMService : ITextCompletionService
{
    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Your implementation here
        return await CallYourLLMAPI(prompt, options, cancellationToken);
    }
}

// Register and use
services.AddScoped<ITextCompletionService, MyLLMService>();
```

## Contributing

Contributions are welcome! Please see our contributing guidelines for details.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

- **Issues**: [GitHub Issues](https://github.com/iyulab/WebFlux/issues)
- **Documentation**: [docs/](docs/)
- **NuGet**: [WebFlux Package](https://www.nuget.org/packages/WebFlux/)
