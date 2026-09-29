using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Extensions;
using Xunit;

namespace WebFlux.Tests.Services;

/// <summary>
/// <see cref="IWebContentProcessor.GetAvailableChunkingStrategies"/> used to return a hand-kept list; the processor resolves a
/// strategy by name from the keyed registrations <c>AddWebFlux()</c> makes. Every name the list offers must resolve, and every
/// <see cref="ChunkingStrategyType"/> a caller can set must be on it.
/// </summary>
public class AvailableChunkingStrategiesTests
{
    [Fact]
    public void EveryListedStrategy_ResolvesFromAddWebFlux_AndMatchesTheStrategyEnum()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWebFlux();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var names = scope.ServiceProvider.GetRequiredService<IWebContentProcessor>().GetAvailableChunkingStrategies();

        names.Should().BeEquivalentTo(Enum.GetNames<ChunkingStrategyType>());
        foreach (var name in names)
            scope.ServiceProvider.GetRequiredKeyedService<IChunkingStrategy>(name).Should().NotBeNull(name);
    }
}
