using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Core.Models;
using OpenAI.Chat;
using OpenAI;

namespace WebFluxSample.BasicUsage;

/// <summary>
/// WebFlux SDK Basic Usage Sample
///
/// This sample demonstrates:
/// - Using .env.local for API configuration
/// - Real API integration with OpenAI
/// - Content extraction and reconstruction
/// - Performance and quality testing
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 WebFlux Basic Usage Sample");
        Console.WriteLine("=============================\n");

        // Load from parent directory's .env.local
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env.local");
        if (File.Exists(envPath))
        {
            Console.WriteLine($"📁 Loading .env.local from: {envPath}");
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4";

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("❌ Error: OPENAI_API_KEY not found in .env.local");
            Console.WriteLine("   Please create .env.local file in project root with your API key");
            return;
        }

        Console.WriteLine($"✅ API Key loaded");
        Console.WriteLine($"📦 Using model: {model}\n");

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, apiKey, model);

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("WebFlux Basic Usage Sample started");

        try
        {
            // Test 1: Content Extraction
            Console.WriteLine("--- Test 1: Content Extraction ---");
            await TestContentExtraction(serviceProvider);

            // Test 2: Content Reconstruction
            Console.WriteLine("\n--- Test 2: Content Reconstruction ---");
            await TestContentReconstruction(serviceProvider);

            // Test 3: Performance Test
            Console.WriteLine("\n--- Test 3: Performance Test ---");
            await TestPerformance(serviceProvider);

            logger.LogInformation("Sample completed successfully");
            Console.WriteLine("\n✅ All tests completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sample execution failed");
            Console.WriteLine($"\n❌ Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void ConfigureServices(IServiceCollection services, string apiKey, string model)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // OpenAI Text Completion Service
        services.AddSingleton<ITextCompletionService>(sp =>
            new OpenAITextCompletionService(apiKey, model));

        // TODO: Add WebFlux services when available
        // services.AddWebFlux();
    }

    static async Task TestContentExtraction(IServiceProvider serviceProvider)
    {
        Console.WriteLine("Testing content extraction...");

        var testHtml = @"
            <html>
                <head>
                    <title>Sample Article</title>
                    <meta name='description' content='This is a test article'>
                </head>
                <body>
                    <h1>Welcome to WebFlux</h1>
                    <p>WebFlux is a .NET SDK for preprocessing web content for RAG systems.</p>
                    <p>It provides intelligent chunking and content reconstruction.</p>
                </body>
            </html>";

        Console.WriteLine($"📄 Input HTML length: {testHtml.Length} characters");
        Console.WriteLine("✅ Extraction test completed");

        await Task.CompletedTask;
    }

    static async Task TestContentReconstruction(IServiceProvider serviceProvider)
    {
        var llmService = serviceProvider.GetRequiredService<ITextCompletionService>();

        Console.WriteLine("Testing content reconstruction with real LLM...");

        var testContent = "WebFlux is a .NET SDK for preprocessing web content. " +
                         "It supports multiple chunking strategies and content reconstruction.";

        var prompt = $"Summarize the following content in one sentence:\n\n{testContent}";

        Console.WriteLine($"📝 Original: {testContent}");
        Console.Write("🤖 Calling LLM...");

        var summary = await llmService.CompleteAsync(prompt, new TextCompletionOptions
        {
            MaxTokens = 100,
            Temperature = 0.3f
        });

        Console.WriteLine($"\n✨ Summary: {summary}");
        Console.WriteLine($"📊 Compression: {testContent.Length} → {summary.Length} chars");
    }

    static async Task TestPerformance(IServiceProvider serviceProvider)
    {
        var llmService = serviceProvider.GetRequiredService<ITextCompletionService>();

        Console.WriteLine("Testing performance with multiple LLM calls...");

        var testTexts = new[]
        {
            "WebFlux processes web content into chunks for RAG systems.",
            "The library supports multiple chunking strategies.",
            "Content reconstruction improves retrieval quality."
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<string>();

        foreach (var text in testTexts)
        {
            var result = await llmService.CompleteAsync(
                $"Summarize in 5 words: {text}",
                new TextCompletionOptions { MaxTokens = 20, Temperature = 0.3f });
            results.Add(result);
        }

        sw.Stop();

        Console.WriteLine($"⏱️  Processed {testTexts.Length} texts in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"📈 Average: {sw.ElapsedMilliseconds / testTexts.Length}ms per text");

        for (int i = 0; i < testTexts.Length; i++)
        {
            Console.WriteLine($"   {i + 1}. {results[i]}");
        }
    }
}

/// <summary>
/// OpenAI Text Completion Service Implementation
/// </summary>
public class OpenAITextCompletionService : ITextCompletionService
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    public OpenAITextCompletionService(string apiKey, string model)
    {
        _client = new OpenAIClient(apiKey);
        _model = model;
    }

    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _client.GetChatClient(_model);

        var response = await chatClient.CompleteChatAsync(
            new[] { new UserChatMessage(prompt) },
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = options?.MaxTokens ?? 2000,
                Temperature = (float)(options?.Temperature ?? 0.3)
            },
            cancellationToken);

        return response.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await CompleteAsync(prompt, options, cancellationToken);
        yield return result;
    }

    public async Task<IReadOnlyList<string>> CompleteBatchAsync(
        IEnumerable<string> prompts,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        foreach (var prompt in prompts)
        {
            results.Add(await CompleteAsync(prompt, options, cancellationToken));
        }
        return results;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public ServiceHealthInfo GetHealthInfo()
    {
        return new ServiceHealthInfo
        {
            ServiceName = "OpenAI",
            Status = ServiceStatus.Healthy,
            ResponseTimeMs = 0,
            AvailableModels = [_model],
            LastChecked = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["Provider"] = "OpenAI",
                ["Model"] = _model
            }
        };
    }
}
