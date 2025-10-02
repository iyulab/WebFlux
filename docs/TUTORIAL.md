# WebFlux Tutorial

This tutorial guides you through installing and using WebFlux for RAG content preprocessing.

## Table of Contents

- [Installation](#installation)
- [Basic Usage](#basic-usage)
- [Processing Pipeline](#processing-pipeline)
- [Chunking Strategies](#chunking-strategies)
- [Reconstruction Strategies](#reconstruction-strategies)
- [Web Crawling](#web-crawling)
- [Service Implementation](#service-implementation)
- [Advanced Scenarios](#advanced-scenarios)
- [Performance Optimization](#performance-optimization)
- [Troubleshooting](#troubleshooting)

## Installation

### Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- An LLM service (OpenAI, Anthropic, Azure OpenAI, or local model)
- A vector database for storing chunks

### Install WebFlux

**Using NuGet Package Manager Console:**
```powershell
Install-Package WebFlux
```

**Using dotnet CLI:**
```bash
dotnet add package WebFlux
```

**Using .csproj file:**
```xml
<ItemGroup>
  <PackageReference Include="WebFlux" Version="0.1.0" />
</ItemGroup>
```

### Verify Installation

Create a simple console application to verify installation:

```csharp
using WebFlux;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddWebFlux();
var provider = services.BuildServiceProvider();

Console.WriteLine("WebFlux installed successfully!");
```

## Basic Usage

### Minimal Setup

```csharp
using WebFlux;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register your AI service implementations
services.AddScoped<ITextCompletionService, YourLLMService>();
services.AddScoped<ITextEmbeddingService, YourEmbeddingService>();

// Register WebFlux
services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();
```

### Process a Single Page

```csharp
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
    Console.WriteLine($"Chunk {chunk.ChunkIndex}:");
    Console.WriteLine($"  Content: {chunk.Content.Substring(0, 100)}...");
    Console.WriteLine($"  Metadata: {chunk.Metadata.Title}");
}
```

### Process Multiple Pages

```csharp
var options = new CrawlOptions
{
    MaxDepth = 2,
    MaxPages = 50,
    RespectRobotsTxt = true
};

await foreach (var result in processor.ProcessWithProgressAsync(
    "https://docs.example.com",
    options))
{
    if (result.IsSuccess && result.Result != null)
    {
        foreach (var chunk in result.Result)
        {
            await ProcessChunk(chunk);
        }
    }

    if (result.Progress != null)
    {
        Console.WriteLine(
            $"Progress: {result.Progress.PercentComplete:F1}% " +
            $"({result.Progress.PagesProcessed}/{result.Progress.TotalPages})");
    }
}
```

## Processing Pipeline

WebFlux processes content through four stages:

### Stage 1: Extract

Extract raw content from a URL:

```csharp
var extractor = provider.GetRequiredService<IContentExtractor>();
var rawContent = await extractor.ExtractAsync("https://example.com/page");

Console.WriteLine($"Title: {rawContent.Metadata.Title}");
Console.WriteLine($"Content Length: {rawContent.Content.Length}");
Console.WriteLine($"Content Type: {rawContent.ContentType}");
```

### Stage 2: Analyze

Analyze content structure and quality:

```csharp
var analyzer = provider.GetRequiredService<IContentAnalyzer>();
var analyzedContent = await analyzer.AnalyzeAsync(rawContent);

Console.WriteLine($"Quality Score: {analyzedContent.Metrics?.ContentQuality:F2}");
Console.WriteLine($"Sections: {analyzedContent.Sections?.Count}");
Console.WriteLine($"Images: {analyzedContent.Images?.Count}");
```

### Stage 3: Reconstruct

Optionally enhance content with LLM:

```csharp
var reconstructor = provider.GetRequiredService<IContentReconstructor>();

var reconstructOptions = new ReconstructOptions
{
    Strategy = "Auto",
    UseLLM = true,
    Temperature = 0.3,
    MaxTokens = 2000
};

var reconstructedContent = await reconstructor.ReconstructAsync(
    analyzedContent,
    reconstructOptions);

Console.WriteLine($"Strategy Used: {reconstructedContent.StrategyUsed}");
Console.WriteLine($"Used LLM: {reconstructedContent.UsedLLM}");
```

### Stage 4: Chunk

Split content into semantic chunks:

```csharp
var chunker = provider.GetRequiredService<IContentChunker>();

var chunkingOptions = new ChunkingOptions
{
    Strategy = "Auto",
    MaxChunkSize = 512,
    OverlapSize = 64
};

var chunks = await chunker.ChunkAsync(
    reconstructedContent,
    chunkingOptions);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.ChunkIndex}: {chunk.Content.Length} chars");
}
```

## Chunking Strategies

### Auto Strategy (Recommended)

Automatically selects the best strategy based on content analysis:

```csharp
var options = new ChunkingOptions
{
    Strategy = "Auto",
    MaxChunkSize = 512,
    OverlapSize = 64
};

var chunks = await processor.ChunkAsync(url, options);
```

### Smart Strategy

Best for HTML documentation and structured content:

```csharp
var options = new ChunkingOptions
{
    Strategy = "Smart",
    MaxChunkSize = 512,
    OverlapSize = 64,
    PreserveStructure = true
};
```

### Semantic Strategy

Preserves semantic meaning across chunks:

```csharp
var options = new ChunkingOptions
{
    Strategy = "Semantic",
    MaxChunkSize = 512,
    OverlapSize = 64,
    UseEmbeddings = true  // Requires IEmbeddingService
};
```

### Intelligent Strategy

Uses LLM for intelligent chunk boundaries:

```csharp
var options = new ChunkingOptions
{
    Strategy = "Intelligent",
    MaxChunkSize = 512,
    OverlapSize = 64,
    UseLLM = true  // Requires ITextCompletionService
};
```

### MemoryOptimized Strategy

Minimizes memory usage for large documents:

```csharp
var options = new ChunkingOptions
{
    Strategy = "MemoryOptimized",
    MaxChunkSize = 512,
    OverlapSize = 64,
    StreamingMode = true
};
```

### Paragraph Strategy

Chunks at natural paragraph boundaries:

```csharp
var options = new ChunkingOptions
{
    Strategy = "Paragraph",
    MinChunkSize = 100,
    MaxChunkSize = 1000
};
```

### FixedSize Strategy

Simple fixed-size chunking:

```csharp
var options = new ChunkingOptions
{
    Strategy = "FixedSize",
    MaxChunkSize = 512,
    OverlapSize = 64
};
```

## Reconstruction Strategies

### None Strategy

Use original content without modification:

```csharp
var options = new ReconstructOptions
{
    Strategy = "None"
};

var result = await processor.ReconstructAsync(analyzedContent, options);
// No LLM calls, preserves original content
```

### Summarize Strategy

Create condensed version of content:

```csharp
var options = new ReconstructOptions
{
    Strategy = "Summarize",
    UseLLM = true,
    Temperature = 0.3,
    MaxTokens = 1000
};

var result = await processor.ReconstructAsync(analyzedContent, options);
```

### Expand Strategy

Add detailed explanations and examples:

```csharp
var options = new ReconstructOptions
{
    Strategy = "Expand",
    UseLLM = true,
    Temperature = 0.5,
    MaxTokens = 3000,
    ContextPrompt = "Focus on technical details"
};

var result = await processor.ReconstructAsync(analyzedContent, options);
```

### Rewrite Strategy

Improve clarity and consistency:

```csharp
var options = new ReconstructOptions
{
    Strategy = "Rewrite",
    UseLLM = true,
    Temperature = 0.3,
    RewriteStyle = "Technical",  // "Technical", "Formal", "Casual", "Simple"
    MaxTokens = 2000
};

var result = await processor.ReconstructAsync(analyzedContent, options);
```

### Enrich Strategy

Add context and metadata:

```csharp
var options = new ReconstructOptions
{
    Strategy = "Enrich",
    UseLLM = true,
    Temperature = 0.3,
    EnrichmentTypes = new[] { "Context", "Definitions", "Examples", "RelatedInfo" },
    MaxTokens = 2000
};

var result = await processor.ReconstructAsync(analyzedContent, options);
```

### Auto Strategy

Automatically selects the best reconstruction strategy:

```csharp
var options = new ReconstructOptions
{
    Strategy = "Auto",
    UseLLM = true,
    Temperature = 0.3
};

var result = await processor.ReconstructAsync(analyzedContent, options);
// Logs which strategy was selected and why
```

## Web Crawling

### Basic Crawling

```csharp
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 100,
    RespectRobotsTxt = true,
    DelayBetweenRequests = TimeSpan.FromMilliseconds(500)
};

var pages = await processor.CrawlAsync("https://example.com", options);
```

### Filtered Crawling

```csharp
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 100,
    AllowedDomains = new[] { "docs.example.com", "help.example.com" },
    IncludePatterns = new[] { "/docs/", "/api/", "/guide/" },
    ExcludePatterns = new[] { "/admin/", "*.pdf", "/archive/" }
};
```

### Concurrent Crawling

```csharp
var options = new CrawlOptions
{
    MaxDepth = 3,
    MaxPages = 500,
    MaxConcurrentRequests = 5,  // Parallel requests
    Timeout = TimeSpan.FromSeconds(30),
    RetryCount = 3
};
```

### Crawling Strategies

```csharp
// Breadth-first (default)
var options = new CrawlOptions
{
    Strategy = CrawlStrategy.BreadthFirst
};

// Depth-first
var options = new CrawlOptions
{
    Strategy = CrawlStrategy.DepthFirst
};

// Intelligent (LLM-based prioritization)
var options = new CrawlOptions
{
    Strategy = CrawlStrategy.Intelligent,
    UseLLM = true  // Requires ITextCompletionService
};

// Sitemap-based
var options = new CrawlOptions
{
    Strategy = CrawlStrategy.Sitemap,
    PreferSitemap = true
};
```

## Service Implementation

### Implementing ITextCompletionService

```csharp
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;

public class OpenAICompletionService : ITextCompletionService
{
    private readonly OpenAIClient _client;

    public OpenAICompletionService(string apiKey)
    {
        _client = new OpenAIClient(apiKey);
    }

    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient("gpt-4");

        var response = await chatClient.CompleteChatAsync(
            new[] { new UserChatMessage(prompt) },
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = options?.MaxTokens ?? 2000,
                Temperature = options?.Temperature ?? 0.3f
            },
            cancellationToken);

        return response.Value.Content[0].Text;
    }
}

// Registration
services.AddScoped<ITextCompletionService>(sp =>
    new OpenAICompletionService(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!));
```

### Implementing ITextEmbeddingService

```csharp
public class OpenAIEmbeddingService : ITextEmbeddingService
{
    private readonly OpenAIClient _client;

    public OpenAIEmbeddingService(string apiKey)
    {
        _client = new OpenAIClient(apiKey);
    }

    public async Task<float[]> GenerateAsync(
        string text,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddingClient = _client.GetEmbeddingClient("text-embedding-3-small");

        var response = await embeddingClient.GenerateEmbeddingAsync(
            text,
            cancellationToken);

        return response.Value.Vector.ToArray();
    }

    public async Task<IEnumerable<float[]>> GenerateBatchAsync(
        IEnumerable<string> texts,
        EmbeddingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddingClient = _client.GetEmbeddingClient("text-embedding-3-small");

        var response = await embeddingClient.GenerateEmbeddingsAsync(
            texts.ToList(),
            cancellationToken);

        return response.Value.Select(e => e.Vector.ToArray());
    }
}

// Registration
services.AddScoped<ITextEmbeddingService>(sp =>
    new OpenAIEmbeddingService(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!));
```

### Implementing IImageToTextService

```csharp
public class OpenAIImageToTextService : IImageToTextService
{
    private readonly OpenAIClient _client;
    private readonly HttpClient _httpClient;

    public OpenAIImageToTextService(string apiKey, HttpClient httpClient)
    {
        _client = new OpenAIClient(apiKey);
        _httpClient = httpClient;
    }

    public async Task<ImageToTextResult> ExtractTextFromWebImageAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var imageData = await _httpClient.GetByteArrayAsync(imageUrl, cancellationToken);
        var chatClient = _client.GetChatClient("gpt-4o");

        var messages = new[]
        {
            new SystemChatMessage("Extract all text from this image accurately."),
            new UserChatMessage(
                ChatMessageContentPart.CreateImagePart(
                    BinaryData.FromBytes(imageData),
                    "image/jpeg"))
        };

        var response = await chatClient.CompleteChatAsync(
            messages,
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1000,
                Temperature = 0.1f
            },
            cancellationToken);

        return new ImageToTextResult
        {
            ExtractedText = response.Value.Content[0].Text,
            Confidence = 0.95,
            IsSuccess = true,
            SourceUrl = imageUrl
        };
    }
}

// Registration
services.AddScoped<IImageToTextService>(sp =>
    new OpenAIImageToTextService(
        Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
        sp.GetRequiredService<HttpClient>()));
```

### Using Without Optional Services

WebFlux handles missing optional services gracefully:

```csharp
var services = new ServiceCollection();

// Only register required services
services.AddScoped<ITextEmbeddingService, YourEmbeddingService>();

// DO NOT register ITextCompletionService or IImageToTextService

services.AddWebFlux();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<IWebContentProcessor>();

// This works - Auto strategy falls back to "None"
var options = new ReconstructOptions { Strategy = "Auto" };
var result = await processor.ReconstructAsync(content, options);
// Logs: "ITextCompletionService not available. Using 'None' strategy."

// This will throw InvalidOperationException with helpful message
var options2 = new ReconstructOptions { Strategy = "Rewrite", UseLLM = true };
var result2 = await processor.ReconstructAsync(content, options2);
// Exception: "ITextCompletionService is required for Rewrite strategy.
//            Please register ITextCompletionService in your DI container,
//            or use ReconstructOptions.Strategy = 'None' for basic reconstruction."
```

## Advanced Scenarios

### RAG Pipeline Integration

```csharp
public class RagIndexingService
{
    private readonly IWebContentProcessor _processor;
    private readonly ITextEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public async Task IndexWebsiteAsync(string baseUrl)
    {
        var crawlOptions = new CrawlOptions
        {
            MaxDepth = 3,
            MaxPages = 100,
            RespectRobotsTxt = true
        };

        var chunkingOptions = new ChunkingOptions
        {
            Strategy = "Auto",
            MaxChunkSize = 512,
            OverlapSize = 64
        };

        await foreach (var result in _processor.ProcessWithProgressAsync(
            baseUrl,
            crawlOptions,
            chunkingOptions))
        {
            if (result.IsSuccess && result.Result != null)
            {
                foreach (var chunk in result.Result)
                {
                    // Generate embedding
                    var embedding = await _embeddingService.GenerateAsync(chunk.Content);

                    // Store in vector database
                    await _vectorStore.UpsertAsync(new VectorDocument
                    {
                        Id = chunk.Id,
                        Content = chunk.Content,
                        Embedding = embedding,
                        Metadata = new Dictionary<string, object>
                        {
                            ["source_url"] = chunk.SourceUrl,
                            ["chunk_index"] = chunk.ChunkIndex,
                            ["title"] = chunk.Metadata.Title ?? "",
                            ["indexed_at"] = DateTime.UtcNow
                        }
                    });
                }
            }
        }
    }
}
```

### Custom Content Extractor

```csharp
public class CustomApiExtractor : IContentExtractor
{
    public string ExtractorType => "CustomApi";
    public IEnumerable<string> SupportedContentTypes => new[] { "application/json" };

    public bool CanExtract(string contentType, string url)
    {
        return url.Contains("/api/") && contentType == "application/json";
    }

    public async Task<RawWebContent> ExtractAsync(
        string url,
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<CustomApiResponse>(json);

        return new RawWebContent
        {
            Url = url,
            Content = data.MainContent,
            ContentType = "application/json",
            Metadata = new WebContentMetadata
            {
                Title = data.Title,
                Description = data.Description,
                Properties = new Dictionary<string, object>
                {
                    ["api_version"] = data.Version,
                    ["last_updated"] = data.UpdatedAt
                }
            }
        };
    }
}

// Registration
services.AddTransient<IContentExtractor, CustomApiExtractor>();
```

### Batch Processing with Progress Tracking

```csharp
public class BatchProcessor
{
    public async Task ProcessUrlsAsync(IEnumerable<string> urls, IProgress<ProgressInfo> progress)
    {
        int total = urls.Count();
        int processed = 0;

        foreach (var url in urls)
        {
            try
            {
                var chunks = await _processor.ChunkAsync(url, _defaultOptions);
                await StoreChunks(chunks);

                processed++;
                progress.Report(new ProgressInfo
                {
                    Current = processed,
                    Total = total,
                    PercentComplete = (double)processed / total * 100,
                    CurrentUrl = url
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {url}: {ex.Message}");
            }
        }
    }
}

// Usage
var progress = new Progress<ProgressInfo>(info =>
{
    Console.WriteLine($"Progress: {info.PercentComplete:F1}% ({info.Current}/{info.Total})");
    Console.WriteLine($"Current: {info.CurrentUrl}");
});

await batchProcessor.ProcessUrlsAsync(urls, progress);
```

### Incremental Updates

```csharp
public class IncrementalIndexer
{
    public async Task UpdateIndexAsync(string baseUrl)
    {
        // Get last crawl time from vector store
        var lastCrawl = await _vectorStore.GetMetadataAsync(baseUrl, "last_crawl_time");
        var lastCrawlTime = lastCrawl != null
            ? DateTime.Parse(lastCrawl.ToString()!)
            : DateTime.MinValue;

        var options = new CrawlOptions
        {
            MaxDepth = 3,
            IfModifiedSince = lastCrawlTime,  // Only process updated pages
            RespectRobotsTxt = true
        };

        await foreach (var result in _processor.ProcessWithProgressAsync(baseUrl, options))
        {
            if (result.IsSuccess && result.Result != null)
            {
                foreach (var chunk in result.Result)
                {
                    // Update or insert chunk
                    await _vectorStore.UpsertAsync(CreateVectorDocument(chunk));
                }
            }
        }

        // Update last crawl time
        await _vectorStore.SetMetadataAsync(baseUrl, "last_crawl_time", DateTime.UtcNow);
    }
}
```

## Performance Optimization

### Parallel Processing

```csharp
var options = new CrawlOptions
{
    MaxConcurrentRequests = Environment.ProcessorCount,  // Use all CPU cores
    MaxPages = 1000,
    Timeout = TimeSpan.FromSeconds(30)
};
```

### Memory-Efficient Streaming

```csharp
var options = new ChunkingOptions
{
    Strategy = "MemoryOptimized",
    StreamingMode = true,
    MaxChunkSize = 512
};

await foreach (var chunk in _processor.ChunkStreamAsync(url, options))
{
    await ProcessChunkImmediately(chunk);
    // Chunk is processed and can be garbage collected
}
```

### Caching

```csharp
services.AddWebFlux(options =>
{
    options.EnableCaching = true;
    options.CacheDuration = TimeSpan.FromHours(4);
    options.MaxCacheSize = 1000;  // Max cached items
});
```

### Batch Embedding Generation

```csharp
// Process chunks in batches for efficient embedding generation
var chunkBatch = new List<WebContentChunk>();
const int batchSize = 100;

await foreach (var result in _processor.ProcessWithProgressAsync(url, options))
{
    if (result.Result != null)
    {
        chunkBatch.AddRange(result.Result);

        if (chunkBatch.Count >= batchSize)
        {
            await ProcessBatch(chunkBatch);
            chunkBatch.Clear();
        }
    }
}

// Process remaining chunks
if (chunkBatch.Any())
{
    await ProcessBatch(chunkBatch);
}

async Task ProcessBatch(List<WebContentChunk> chunks)
{
    var texts = chunks.Select(c => c.Content);
    var embeddings = await _embeddingService.GenerateBatchAsync(texts);

    await _vectorStore.UpsertBatchAsync(
        chunks.Zip(embeddings, CreateVectorDocument));
}
```

## Troubleshooting

### Missing Service Errors

**Problem**: `InvalidOperationException: ITextCompletionService is required for Rewrite strategy`

**Solution**: Either register the required service or use a strategy that doesn't require it:

```csharp
// Option 1: Register the service
services.AddScoped<ITextCompletionService, YourLLMService>();

// Option 2: Use a non-LLM strategy
var options = new ReconstructOptions { Strategy = "None" };

// Option 3: Let Auto strategy handle it
var options = new ReconstructOptions { Strategy = "Auto" };  // Falls back to "None"
```

### Memory Issues

**Problem**: High memory usage when processing large websites

**Solution**: Use MemoryOptimized strategy and streaming:

```csharp
var chunkingOptions = new ChunkingOptions
{
    Strategy = "MemoryOptimized",
    StreamingMode = true
};

var crawlOptions = new CrawlOptions
{
    MaxConcurrentRequests = 2,  // Reduce concurrency
    MaxPages = 100  // Process in smaller batches
};
```

### Rate Limiting

**Problem**: Getting rate-limited by target website

**Solution**: Adjust crawling delays and concurrency:

```csharp
var options = new CrawlOptions
{
    DelayBetweenRequests = TimeSpan.FromSeconds(2),
    MaxConcurrentRequests = 1,
    RespectRobotsTxt = true,
    UserAgent = "YourBot/1.0 (+https://yoursite.com/bot)"
};
```

### Poor Chunk Quality

**Problem**: Chunks don't preserve semantic meaning

**Solution**: Use Semantic or Intelligent chunking strategy:

```csharp
var options = new ChunkingOptions
{
    Strategy = "Semantic",
    UseEmbeddings = true,
    OverlapSize = 100  // Increase overlap for better context
};
```

### Slow Processing

**Problem**: Processing is slower than expected

**Solution**: Enable parallel processing and caching:

```csharp
// Enable caching
services.AddWebFlux(config =>
{
    config.EnableCaching = true;
    config.CacheDuration = TimeSpan.FromHours(4);
});

// Increase concurrency
var options = new CrawlOptions
{
    MaxConcurrentRequests = 5,
    MaxPages = 500
};

// Use faster chunking strategy
var chunkingOptions = new ChunkingOptions
{
    Strategy = "FixedSize",  // Fastest but lowest quality
    MaxChunkSize = 512
};
```

### robots.txt Violations

**Problem**: Blocked by robots.txt rules

**Solution**: Ensure compliance and check permissions:

```csharp
var options = new CrawlOptions
{
    RespectRobotsTxt = true,  // Always enable
    UserAgent = "YourBot/1.0",  // Set identifiable User-Agent
    DelayBetweenRequests = TimeSpan.FromSeconds(1)  // Be polite
};

// Check robots.txt before crawling
var robotsParser = provider.GetRequiredService<IRobotsParser>();
var isAllowed = await robotsParser.IsAllowedAsync(url, options.UserAgent);
```

## Next Steps

- Explore [Pipeline Design](PIPELINE_DESIGN.md) for architecture details
- Review [Interfaces](INTERFACES.md) for implementation reference
- Read [Chunking Strategies](CHUNKING_STRATEGIES.md) for strategy details
- Check the [samples](../samples/) directory for complete examples
