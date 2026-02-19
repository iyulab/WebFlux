using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
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
            CrawlStrategy.Dynamic => _serviceProvider.GetRequiredKeyedService<ICrawler>("Dynamic"),
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

    public IAiEnhancementService? CreateAiEnhancementService()
    {
        // AI 증강 서비스는 선택적이므로 GetService 사용 (등록되지 않은 경우 null 반환)
        return _serviceProvider.GetService<IAiEnhancementService>();
    }

    public ICacheService? TryCreateCacheService()
    {
        // 캐시 서비스는 선택적 (Interface Provider 패턴)
        return _serviceProvider.GetService<ICacheService>();
    }

    public IDomainRateLimiter? TryCreateDomainRateLimiter()
    {
        // Rate Limiter는 선택적
        return _serviceProvider.GetService<IDomainRateLimiter>();
    }

    public IContentQualityEvaluator? TryCreateContentQualityEvaluator()
    {
        // 품질 평가기는 선택적
        return _serviceProvider.GetService<IContentQualityEvaluator>();
    }
}