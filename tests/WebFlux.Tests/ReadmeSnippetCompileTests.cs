using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Extensions;

namespace WebFlux.Tests;

/// <summary>
/// Compiles every <c>```csharp</c> block in README.md against the current assemblies, and resolves every service the
/// README asks the container for. The Quick Start once began with <c>using WebFlux;</c> — a namespace that does not
/// exist — and read <c>chunk.ChunkIndex</c>, which <see cref="WebFlux.Core.Models.WebContentChunk"/> implements only
/// explicitly; nothing compiled it, so a consumer copying the front door got errors on its first line.
/// </summary>
/// <remarks>
/// A block is compiled as a top-level program: its <c>using</c> lines are hoisted, the common usings below are added, and
/// the stand-ins below are declared when the block uses the name without declaring it — values a reader already has
/// from the surrounding text (a built provider, the options a block passes), not part of what the block shows.
/// </remarks>
public class ReadmeSnippetCompileTests
{
    private const string CommonUsings = """
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using WebFlux.Core.Interfaces;
        using WebFlux.Core.Options;
        """;

    private static readonly (string Name, string Declaration)[] StandIns =
    [
        ("services", "IServiceCollection services = null!;"),
        ("provider", "IServiceProvider provider = null!;"),
        ("processor", "IWebContentProcessor processor = null!;"),
        ("url", "string url = \"https://example.com\";"),
        ("urls", "IEnumerable<string> urls = [];"),
        ("crawlOptions", "CrawlOptions crawlOptions = new();"),
        ("chunkOptions", "ChunkingOptions chunkOptions = new();"),
        ("logger", "ILogger logger = null!;"),
    ];

    // Types a block names as the reader's own (declared after the program's statements).
    private static readonly (string Name, string Declaration)[] TypeStandIns =
    [
        ("MyCompletionService",
            "sealed class MyCompletionService : Flux.Abstractions.ITextCompletionService { " +
            "public Task<string> CompleteAsync(string prompt, Flux.Abstractions.TextCompletionOptions? options = null, " +
            "CancellationToken cancellationToken = default) => Task.FromResult(\"\"); }"),
    ];

    private static readonly string[] AssembliesToLoad =
    [
        "WebFlux", "Flux.Abstractions",
        "Microsoft.Extensions.DependencyInjection", "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
    ];

    public static TheoryData<string> Blocks()
    {
        var data = new TheoryData<string>();
        foreach (var block in ReadBlocks())
            data.Add(block.Key);
        return data;
    }

    [Theory]
    [MemberData(nameof(Blocks))]
    public void ReadmeBlock_Compiles(string key)
    {
        var block = ReadBlocks().Single(b => b.Key == key);

        var errors = Compile(block.Code);

        Assert.True(errors.IsEmpty,
            $"README block {key} does not compile against the current API:\n" +
            string.Join("\n", errors.Select(e => e.ToString())) + "\n--- source ---\n" + Program(block.Code));
    }

    [Fact]
    public void EveryReadmeBlock_IsFound()
    {
        var blocks = ReadBlocks();
        Assert.True(blocks.Count >= 6, $"expected the README's C# blocks, found {blocks.Count}");
    }

    /// <summary>Positive control: the compiler rejects what the old Quick Start did.</summary>
    [Fact]
    public void Compile_RejectsTheOldQuickStart()
    {
        var errors = Compile("""
            using WebFlux;
            var chunks = await processor.ProcessUrlAsync("https://example.com");
            foreach (var chunk in chunks) Console.WriteLine(chunk.ChunkIndex);
            """);

        Assert.NotEmpty(errors);
    }

    /// <summary>
    /// A block that compiles can still ask the container for a service <c>AddWebFlux()</c> never registers. Every
    /// <c>GetRequiredService&lt;T&gt;()</c> the README names must resolve from a bare <c>AddWebFlux()</c>.
    /// </summary>
    [Fact]
    public void EveryServiceTheReadmeResolves_IsRegisteredByAddWebFlux()
    {
        var names = Regex.Matches(File.ReadAllText(ReadmePath()), @"GetRequiredService<([A-Za-z_][\w\.]*)>\(\)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(names);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebFlux();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        foreach (var name in names)
        {
            var type = FindType(name);
            Assert.True(type is not null, $"README resolves {name}, which no loaded WebFlux assembly declares");
            Assert.True(scope.ServiceProvider.GetService(type!) is not null,
                $"README resolves {name} from AddWebFlux(), which does not register it");
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

    private sealed record Block(string Key, string Heading, string Code);

    private static List<Block> ReadBlocks()
    {
        var lines = File.ReadAllText(ReadmePath()).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var blocks = new List<Block>();
        var heading = "(top)";
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('#'))
                heading = lines[i].TrimStart('#').Trim();
            if (lines[i].Trim() != "```csharp")
                continue;

            var start = i + 1;
            var code = new StringBuilder();
            for (i++; i < lines.Length && lines[i].Trim() != "```"; i++)
                code.AppendLine(lines[i]);
            blocks.Add(new Block($"line {start}: {heading}", heading, code.ToString()));
        }

        return blocks;
    }

    private static string Program(string code)
    {
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        bool IsUsingDirective(string l) =>
            l.StartsWith("using ", StringComparison.Ordinal) && l.TrimEnd().EndsWith(';')
            && !l.StartsWith("using var ", StringComparison.Ordinal);

        var body = string.Join("\n", lines.Where(l => !IsUsingDirective(l)));
        bool Declares(string name) => Regex.IsMatch(body, $@"\b(var|[A-Z][\w<>?,\s]*)\s+{name}\s*[=;]");
        var standIns = StandIns
            .Where(s => Regex.IsMatch(body, $@"\b{s.Name}\b") && !Declares(s.Name))
            .Select(s => s.Declaration);
        var typeStandIns = TypeStandIns
            .Where(s => Regex.IsMatch(body, $@"\b{s.Name}\b"))
            .Select(s => s.Declaration);

        return string.Join("\n", lines.Where(IsUsingDirective)) + "\n" + CommonUsings + "\n"
               + string.Join("\n", standIns) + "\n" + body + "\n" + string.Join("\n", typeStandIns);
    }

    private static ImmutableArray<Diagnostic> Compile(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(Program(code), new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "ReadmeSnippet", [tree], References(),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
        return compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
    }

    private static List<MetadataReference> References()
    {
        foreach (var name in AssembliesToLoad)
            Assembly.Load(name);

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
            paths.UnionWith(trusted.Split(Path.PathSeparator).Where(p => p.Length > 0));
        paths.UnionWith(AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location.Length > 0)
            .Select(a => a.Location));
        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
    }

    private static string ReadmePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WebFlux.slnx")))
            dir = dir.Parent;
        return Path.Combine(
            dir?.FullName ?? throw new InvalidOperationException("WebFlux.slnx not found above the test output directory"),
            "README.md");
    }
}
