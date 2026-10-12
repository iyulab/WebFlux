using System.Reflection;
using System.Text.RegularExpressions;
using Iyu.Conventions.Testing.Snippets;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Extensions;

namespace WebFlux.Tests;

/// <summary>
/// Compiles every C# block in README.md and docs/TUTORIAL.md against the current assemblies, and resolves every service
/// those documents ask the container for. The Quick Start once began with <c>using WebFlux;</c> — a namespace that does not
/// exist — and read <c>chunk.ChunkIndex</c>, which <see cref="WebFlux.Core.Models.WebContentChunk"/> implements only
/// explicitly; the tutorial had drifted further (27 of its 34 blocks did not compile). Nothing compiled either document,
/// so a consumer copying them got errors on the first line.
/// </summary>
/// <remarks>
/// Each block is compiled by <see cref="SnippetCompiler"/>: its own usings, the common usings below, and the stand-ins
/// below when the block uses the name without declaring it — values a reader already has from the surrounding text (a
/// built provider, the options a block passes) and the reader's own types an earlier block implemented, not part of what
/// the block shows. A block that only declares types is compiled as a library.
/// </remarks>
public class DocSnippetCompileTests
{
    // The documents whose blocks are compiled, with the number of C# blocks each is expected to hold at least — a
    // parser that silently stops finding blocks would otherwise turn every theory row into "nothing to check".
    private static readonly (string Path, int MinBlocks)[] Documents =
    [
        ("README.md", 6),
        ("docs/TUTORIAL.md", 28),
    ];

    // A block that is deliberately not a program (a signature sketch, pseudocode), by the heading it sits under, with the
    // reason. Shrink this, never grow it silently.
    private static readonly SnippetRoster Fragments = new(new Dictionary<string, string>(StringComparer.Ordinal));

    // Flux.Abstractions is deliberately not here: it declares TextCompletionOptions, as does WebFlux.Core.Options, so a
    // block that implements ITextCompletionService names the namespace (and the alias) itself, as a reader must.
    private static readonly string[] CommonUsings =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Runtime.CompilerServices",
        "System.Text",
        "System.Threading",
        "System.Threading.Tasks",
        "Microsoft.Extensions.DependencyInjection",
        "FluxCurator.Core.Core",
        "Microsoft.Extensions.Logging",
        "WebFlux.Core.Interfaces",
        "WebFlux.Core.Models",
        "WebFlux.Core.Models.Events",
        "WebFlux.Core.Options",
        "WebFlux.Extensions",
    ];

    private static readonly Dictionary<string, string> StandIns = new(StringComparer.Ordinal)
    {
        ["services"] = "IServiceCollection services = null!;",
        ["provider"] = "IServiceProvider provider = null!;",
        ["serviceProvider"] = "IServiceProvider serviceProvider = null!;",
        ["processor"] = "IWebContentProcessor processor = null!;",
        ["url"] = "string url = \"https://example.com\";",
        ["urls"] = "IEnumerable<string> urls = [];",
        ["crawlOptions"] = "CrawlOptions crawlOptions = new();",
        ["chunkOptions"] = "ChunkingOptions chunkOptions = new();",
        ["logger"] = "ILogger logger = null!;",

        // Types a block names as the reader's own (declared after the program's statements): a one-line registration
        // block names the service an earlier block of the same document implemented.
        ["MyCompletionService"] =
            "sealed class MyCompletionService : Flux.Abstractions.ITextCompletionService { " +
            "public Task<string> CompleteAsync(string prompt, Flux.Abstractions.TextCompletionOptions? options = null, " +
            "CancellationToken cancellationToken = default) => Task.FromResult(\"\"); }",
        ["OpenAIEmbeddingService"] =
            "sealed class OpenAIEmbeddingService(string apiKey) : IEmbedder { " +
            "public int EmbeddingDimension => 0; " +
            "public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<float>()); " +
            "public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default) => " +
            "Task.FromResult<IReadOnlyList<float[]>>([]); " +
            "public float CalculateSimilarity(float[] a, float[] b) => 0; }",
        ["OpenAICompletionService"] =
            "sealed class OpenAICompletionService(string apiKey, string model) : Flux.Abstractions.ITextCompletionService { " +
            "public Task<string> CompleteAsync(string prompt, Flux.Abstractions.TextCompletionOptions? options = null, " +
            "CancellationToken cancellationToken = default) => Task.FromResult(\"\"); }",
    };

    private static readonly string[] AssembliesToLoad =
    [
        "WebFlux", "Flux.Abstractions", "FluxCurator.Core", "OpenAI",
        "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
    ];

    private static readonly SnippetCompiler Compiler = new(SnippetCompiler.ReferencesFor([.. AssembliesToLoad.Select(Assembly.Load)]))
    {
        Usings = CommonUsings,
        StandIns = StandIns,
    };

    private static readonly IReadOnlyList<DocSnippet> Snippets =
        [.. Documents.SelectMany(d => DocSnippets.Read(DocPath(d.Path), d.Path))];

    public static TheoryData<string> Blocks() => new(Snippets.Select(s => s.Key));

    [Theory]
    [MemberData(nameof(Blocks))]
    public void DocBlock_Compiles(string key)
    {
        var snippet = Snippets.Single(s => s.Key == key);
        if (!Fragments.IsFragment(snippet))
            Compiler.ShouldCompile(snippet);
    }

    [Fact]
    public void EveryDocBlock_IsFound()
    {
        foreach (var (path, minBlocks) in Documents)
        {
            var count = Snippets.Count(s => s.File == path);
            Assert.True(count >= minBlocks, $"expected at least {minBlocks} C# blocks in {path}, found {count}");
        }

        Fragments.ShouldMatch([.. Snippets], atLeast: Documents.Sum(d => d.MinBlocks));
    }

    /// <summary>Positive control: the compiler rejects what the old Quick Start did.</summary>
    [Fact]
    public void Compile_RejectsTheOldQuickStart()
    {
        var errors = Compiler.Compile("""
            using WebFlux;
            var chunks = await processor.ProcessUrlAsync("https://example.com");
            foreach (var chunk in chunks) Console.WriteLine(chunk.ChunkIndex);
            """);

        Assert.NotEmpty(errors);
    }

    /// <summary>
    /// A block that compiles can still ask the container for a service <c>AddWebFlux()</c> never registers. Every
    /// <c>GetRequiredService&lt;T&gt;()</c> the documents name must resolve from a bare <c>AddWebFlux()</c>.
    /// </summary>
    [Fact]
    public void EveryServiceTheDocsResolve_IsRegisteredByAddWebFlux()
    {
        var names = Documents
            .SelectMany(d => Regex.Matches(File.ReadAllText(DocPath(d.Path)), @"GetRequiredService<([A-Za-z_][\w\.]*)>\(\)")
                .Select(m => (Doc: d.Path, Name: m.Groups[1].Value)))
            .Distinct()
            .ToList();
        foreach (var (path, _) in Documents)
            Assert.Contains(names, n => n.Doc == path);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebFlux();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        foreach (var (doc, name) in names)
        {
            var type = FindType(name);
            Assert.True(type is not null, $"{doc} resolves {name}, which no loaded WebFlux assembly declares");
            Assert.True(scope.ServiceProvider.GetService(type!) is not null,
                $"{doc} resolves {name} from AddWebFlux(), which does not register it");
        }
    }

    /// <summary>
    /// The Quick Start's container, run: a consumer that copies it gets chunks with no AI service registered. HTML
    /// already in hand stands in for the README's URL so the fact does not depend on the network.
    /// </summary>
    [Fact]
    public async Task QuickStartContainer_ProducesChunks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebFlux();
        await using var provider = services.BuildServiceProvider();
        var processor = provider.GetRequiredService<IWebContentProcessor>();

        var chunks = await processor.ProcessHtmlAsync(
            "<html><head><title>Solar</title></head><body><h1>Solar panels</h1>" +
            "<p>Solar panels convert light into electricity for homes and businesses.</p>" +
            "<p>Batteries store the energy for use at night.</p></body></html>",
            "https://example.com/solar",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Content.Contains("Solar panels", StringComparison.Ordinal));
    }

    private static Type? FindType(string name)
    {
        foreach (var assemblyName in AssembliesToLoad)
            Assembly.Load(assemblyName);
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("WebFlux", StringComparison.Ordinal) == true)
            .SelectMany(a => a.GetExportedTypes())
            .FirstOrDefault(t => t.FullName == name || t.Name == name);
    }

    private static string DocPath(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WebFlux.slnx")))
            dir = dir.Parent;
        return Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("WebFlux.slnx not found above the test output directory"),
            relativePath);
    }
}
