using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Extensions;
using WebFlux.Services;
using WebFlux.Services.Crawlers;
using Xunit;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// Dynamic rendering is declared by WebFlux and implemented by a separate package. Three call paths
/// can ask for it, and one of them — the UseDynamicRendering option — never names the strategy at
/// all. Left to the container each fails differently, and none of those failures mentions a package,
/// so what a consumer sees would depend on which factory their code happened to reach. These tests
/// fix a single answer across all three.
/// </summary>
public class DynamicCrawlerResolutionTests
{
    private static ServiceProvider WithoutDynamicRenderer()
        => new ServiceCollection().AddWebFluxCrawlingStub().BuildServiceProvider();

    private static ServiceProvider WithDynamicRenderer(ICrawler crawler)
        => new ServiceCollection()
            .AddWebFluxCrawlingStub()
            .AddKeyedTransient(CrawlerKeys.Dynamic, (_, _) => crawler)
            .BuildServiceProvider();

    [Fact]
    public void CrawlerFactory_DynamicWithoutThePackage_NamesTheMissingPackage()
    {
        var factory = new CrawlerFactory(WithoutDynamicRenderer());

        var act = () => factory.CreateCrawler(CrawlStrategy.Dynamic);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WebFlux.Playwright*AddWebFluxPlaywright*");
    }

    [Fact]
    public void ServiceFactory_DynamicWithoutThePackage_NamesTheSameMissingPackage()
    {
        var factory = new ServiceFactory(WithoutDynamicRenderer());

        var act = () => factory.CreateCrawler(CrawlStrategy.Dynamic);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WebFlux.Playwright*AddWebFluxPlaywright*");
    }

    [Fact]
    public void BothFactories_ProduceTheIdenticalMessage()
    {
        // A consumer's diagnosis must not depend on which factory their code path used.
        var provider = WithoutDynamicRenderer();

        var fromCrawlerFactory = Record.Exception(
            () => new CrawlerFactory(provider).CreateCrawler(CrawlStrategy.Dynamic));
        var fromServiceFactory = Record.Exception(
            () => new ServiceFactory(provider).CreateCrawler(CrawlStrategy.Dynamic));

        fromCrawlerFactory!.Message.Should().Be(fromServiceFactory!.Message);
    }

    [Fact]
    public void TheMessageTellsTheConsumerHowToProceedWithoutThePackage()
    {
        // Installing a browser runtime is not the only remedy, and a consumer who does not want one
        // has to be able to read the other way out of the same sentence.
        var act = () => DynamicCrawlerResolver.Resolve(WithoutDynamicRenderer());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UseDynamicRendering*");
    }

    [Fact]
    public void WithADynamicCrawlerRegistered_ResolutionSucceedsThroughBothFactories()
    {
        var registered = Substitute.For<ICrawler>();
        var provider = WithDynamicRenderer(registered);

        new CrawlerFactory(provider).CreateCrawler(CrawlStrategy.Dynamic).Should().BeSameAs(registered);
        new ServiceFactory(provider).CreateCrawler(CrawlStrategy.Dynamic).Should().BeSameAs(registered);
    }

    [Fact]
    public void AddWebFluxPlaywright_FillsExactlyTheKeyThatAddWebFluxLeavesEmpty()
    {
        // The two halves of the contract have to be asserted against each other, and against the real
        // registration rather than a crawler put under the key by hand — otherwise the extension could
        // register under a different key and every other test here would still pass.
        var services = new ServiceCollection();
        services.AddWebFluxPlaywright();

        // Descriptors only: TryAddSingleton<IPlaywright>(factory) is lazy, so nothing here launches or
        // requires a browser.
        services.Any(d => Equals(d.ServiceKey, CrawlerKeys.Dynamic) && d.ServiceType == typeof(ICrawler))
            .Should().BeTrue();
    }

    [Fact]
    public void AddWebFluxPlaywright_MakesTheOtherwiseFailingRequestSucceed()
    {
        var services = new ServiceCollection().AddWebFluxCrawlingStub();
        var before = () => new CrawlerFactory(services.BuildServiceProvider()).CreateCrawler(CrawlStrategy.Dynamic);
        before.Should().Throw<InvalidOperationException>();

        services.AddWebFluxPlaywright();

        // The crawler's own dependencies, which the base package would normally have supplied. It
        // opens a browser on first crawl, not on construction, so resolving it here starts nothing.
        services.AddLogging();
        services.AddSingleton(Substitute.For<IHttpClientService>());
        services.AddSingleton(Substitute.For<IEventPublisher>());

        var resolved = services.BuildServiceProvider().GetKeyedService<ICrawler>(CrawlerKeys.Dynamic);

        resolved.Should().NotBeNull("adding the package must make the previously failing request work");
    }

    [Fact]
    public void AddWebFlux_DoesNotRegisterADynamicCrawler()
    {
        // The whole point of the split: the base registration must leave this key empty, or consumers
        // keep inheriting a browser runtime they never asked for.
        var services = new ServiceCollection();
        services.AddWebFluxCrawling();

        services.Any(d => Equals(d.ServiceKey, CrawlerKeys.Dynamic)).Should().BeFalse();
    }
}

internal static class CrawlerStubRegistration
{
    /// <summary>
    /// Registers the crawlers the base package owns, without the container-wide wiring AddWebFlux
    /// performs — these tests are about one key being absent, not about the rest of the graph.
    /// </summary>
    public static IServiceCollection AddWebFluxCrawlingStub(this IServiceCollection services)
    {
        services.AddKeyedTransient(CrawlerKeys.BreadthFirst, (_, _) => Substitute.For<ICrawler>());
        return services;
    }
}
