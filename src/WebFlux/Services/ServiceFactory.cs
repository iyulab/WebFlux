using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;

namespace WebFlux.Services;

/// <summary>
/// 서비스 팩토리 구현
/// Interface Provider 패턴에서 서비스 인스턴스를 생성하는 역할
/// </summary>
public class ServiceFactory : IServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T CreateService<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public object CreateService(Type serviceType)
    {
        return _serviceProvider.GetRequiredService(serviceType);
    }

    public T CreateNamedService<T>(string name) where T : class
    {
        return _serviceProvider.GetRequiredKeyedService<T>(name);
    }

    public ICrawler CreateCrawler(CrawlStrategy strategy)
    {
        return strategy switch
        {
            CrawlStrategy.BreadthFirst => _serviceProvider.GetRequiredKeyedService<ICrawler>("BreadthFirst"),
            CrawlStrategy.DepthFirst => _serviceProvider.GetRequiredKeyedService<ICrawler>("DepthFirst"),
            CrawlStrategy.Intelligent => _serviceProvider.GetRequiredKeyedService<ICrawler>("Intelligent"),
            CrawlStrategy.Sitemap => _serviceProvider.GetRequiredKeyedService<ICrawler>("Sitemap"),
            _ => throw new ArgumentException($"Unknown crawl strategy: {strategy}")
        };
    }

    public IContentExtractor CreateContentExtractor(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "text/html" => _serviceProvider.GetRequiredKeyedService<IContentExtractor>("Html"),
            "text/plain" => _serviceProvider.GetRequiredKeyedService<IContentExtractor>("Text"),
            "application/json" => _serviceProvider.GetRequiredKeyedService<IContentExtractor>("Json"),
            "text/markdown" => _serviceProvider.GetRequiredKeyedService<IContentExtractor>("Markdown"),
            "application/xml" => _serviceProvider.GetRequiredKeyedService<IContentExtractor>("Xml"),
            _ => _serviceProvider.GetRequiredKeyedService<IContentExtractor>("Default")
        };
    }

    public IChunkingStrategy CreateChunkingStrategy(string strategyType)
    {
        return _serviceProvider.GetRequiredKeyedService<IChunkingStrategy>(strategyType);
    }
}