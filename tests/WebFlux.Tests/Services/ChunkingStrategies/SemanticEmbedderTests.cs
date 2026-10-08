using AwesomeAssertions;
using FluxCurator.Core.Core;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Extensions;
using Xunit;

namespace WebFlux.Tests.Services.ChunkingStrategies;

/// <summary>
/// Semantic chunking runs on FluxCurator, which reads <see cref="IEmbedder"/> from the container. The embedder a consumer
/// registers is the one that embeds — WebFlux used to declare a second embedding contract that nothing read.
/// </summary>
public class SemanticEmbedderTests
{
    private const string Text =
        "Cats are small domesticated carnivores. They purr and sleep most of the day. " +
        "Kittens are young cats.\n\n" +
        "Quarterly revenue grew by twelve percent. The board approved the dividend. " +
        "Operating margins improved in every region.";

    private sealed class CountingEmbedder : IEmbedder
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public int EmbeddingDimension => 3;

        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            // Two clusters, so a semantic splitter has a boundary to find.
            float[] vector = text.Contains("revenue", StringComparison.OrdinalIgnoreCase)
                || text.Contains("board", StringComparison.OrdinalIgnoreCase)
                || text.Contains("margin", StringComparison.OrdinalIgnoreCase)
                ? [0f, 1f, 0f]
                : [1f, 0f, 0f];
            return Task.FromResult(vector);
        }

        public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        {
            var result = new List<float[]>();
            foreach (var text in texts)
                result.Add(await GenerateEmbeddingAsync(text, cancellationToken));
            return result;
        }

        public float CalculateSimilarity(float[] embedding1, float[] embedding2)
        {
            float dot = 0, n1 = 0, n2 = 0;
            for (var i = 0; i < embedding1.Length; i++)
            {
                dot += embedding1[i] * embedding2[i];
                n1 += embedding1[i] * embedding1[i];
                n2 += embedding2[i] * embedding2[i];
            }

            return dot / MathF.Sqrt(n1 * n2);
        }
    }

    [Fact]
    public async Task The_registered_embedder_drives_semantic_chunking()
    {
        var embedder = new CountingEmbedder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEmbedder>(embedder);
        services.AddWebFlux();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var semantic = scope.ServiceProvider.GetRequiredKeyedService<IChunkingStrategy>("Semantic");
        var chunks = await semantic.ChunkAsync(
            new ExtractedContent { Url = "https://example.com", MainContent = Text },
            cancellationToken: TestContext.Current.CancellationToken);

        chunks.Should().NotBeEmpty();
        embedder.Calls.Should().BeGreaterThan(0, "the consumer's embedder is the one semantic chunking uses");
    }

    [Fact]
    public async Task Without_an_embedder_semantic_chunking_says_it_needs_one()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebFlux();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var semantic = scope.ServiceProvider.GetRequiredKeyedService<IChunkingStrategy>("Semantic");
        var act = () => semantic.ChunkAsync(
            new ExtractedContent { Url = "https://example.com", MainContent = Text },
            cancellationToken: TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("embedder");
    }
}
