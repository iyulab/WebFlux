using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services.ChunkingStrategies;

namespace WebFlux.Services;

/// <summary>
/// 서비스 팩토리 구현체
/// DI 컨테이너를 사용하여 동적으로 서비스 인스턴스 생성
/// </summary>
public class ServiceFactory : IServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// ServiceFactory 생성자
    /// </summary>
    /// <param name="serviceProvider">서비스 프로바이더</param>
    public ServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>서비스 인스턴스</returns>
    public T CreateService<T>() where T : class
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
        return service;
    }

    /// <summary>
    /// 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="serviceType">서비스 타입</param>
    /// <returns>서비스 인스턴스</returns>
    public object CreateService(Type serviceType)
    {
        var service = _serviceProvider.GetService(serviceType);
        if (service == null)
        {
            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }
        return service;
    }

    /// <summary>
    /// 명명된 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <param name="name">서비스 이름</param>
    /// <returns>서비스 인스턴스</returns>
    public T CreateNamedService<T>(string name) where T : class
    {
        var services = _serviceProvider.GetServices<T>();
        var namedService = services.FirstOrDefault(s => s.GetType().Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (namedService == null)
        {
            throw new InvalidOperationException($"Named service '{name}' of type {typeof(T).Name} is not found.");
        }

        return namedService;
    }

    /// <summary>
    /// 크롤러 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="strategy">크롤링 전략</param>
    /// <returns>크롤러 인스턴스</returns>
    public ICrawler CreateCrawler(CrawlStrategy strategy)
    {
        return strategy switch
        {
            CrawlStrategy.BreadthFirst => CreateService<BreadthFirstCrawler>(),
            CrawlStrategy.DepthFirst => CreateService<DepthFirstCrawler>(),
            CrawlStrategy.Sitemap => CreateService<SitemapCrawler>(),
            CrawlStrategy.Priority => CreateService<BreadthFirstCrawler>(), // 기본값으로 BreadthFirst 사용
            _ => throw new NotSupportedException($"Crawl strategy '{strategy}' is not supported.")
        };
    }

    /// <summary>
    /// 콘텐츠 추출기 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="contentType">콘텐츠 타입</param>
    /// <returns>콘텐츠 추출기 인스턴스</returns>
    public IContentExtractor CreateContentExtractor(string contentType)
    {
        var normalizedContentType = contentType.ToLowerInvariant();

        return normalizedContentType switch
        {
            "text/html" or "application/xhtml+xml" => CreateService<HtmlContentExtractor>(),
            "text/markdown" or "text/x-markdown" => CreateService<MarkdownContentExtractor>(),
            "application/json" => CreateService<JsonContentExtractor>(),
            "application/xml" or "text/xml" => CreateService<XmlContentExtractor>(),
            "text/plain" => CreateService<TextContentExtractor>(),
            _ => CreateService<TextContentExtractor>() // 기본값으로 텍스트 추출기 사용
        };
    }

    /// <summary>
    /// 청킹 전략 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="strategyType">청킹 전략 타입</param>
    /// <returns>청킹 전략 인스턴스</returns>
    public IChunkingStrategy CreateChunkingStrategy(string strategyType)
    {
        var normalizedStrategy = strategyType.ToLowerInvariant();

        return normalizedStrategy switch
        {
            "fixedsize" or "fixed" => CreateService<FixedSizeChunkingStrategy>(),
            "paragraph" or "para" => CreateService<ParagraphChunkingStrategy>(),
            "smart" or "structure" or "structural" => CreateService<SmartChunkingStrategy>(),
            "semantic" or "embedding" or "similarity" => CreateService<SemanticChunkingStrategy>(),
            // 향후 추가될 전략들
            // "intelligent" => CreateService<IntelligentChunkingStrategy>(),
            // "memoryoptimized" => CreateService<MemoryOptimizedChunkingStrategy>(),
            // "auto" => CreateService<AutoChunkingStrategy>(),
            _ => CreateService<FixedSizeChunkingStrategy>() // 기본값
        };
    }
}

/// <summary>
/// 크롤러 팩토리 인터페이스
/// </summary>
public interface ICrawlerFactory
{
    /// <summary>
    /// 크롤러를 생성합니다.
    /// </summary>
    /// <param name="strategy">크롤링 전략</param>
    /// <returns>크롤러 인스턴스</returns>
    ICrawler CreateCrawler(CrawlStrategy strategy);
}

/// <summary>
/// 크롤러 팩토리 구현체
/// </summary>
public class CrawlerFactory : ICrawlerFactory
{
    private readonly IServiceFactory _serviceFactory;

    public CrawlerFactory(IServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    public ICrawler CreateCrawler(CrawlStrategy strategy)
    {
        return _serviceFactory.CreateCrawler(strategy);
    }
}

/// <summary>
/// 콘텐츠 추출기 팩토리 인터페이스
/// </summary>
public interface IContentExtractorFactory
{
    /// <summary>
    /// 콘텐츠 추출기를 생성합니다.
    /// </summary>
    /// <param name="contentType">콘텐츠 타입</param>
    /// <returns>콘텐츠 추출기 인스턴스</returns>
    IContentExtractor CreateExtractor(string contentType);
}

/// <summary>
/// 콘텐츠 추출기 팩토리 구현체
/// </summary>
public class ContentExtractorFactory : IContentExtractorFactory
{
    private readonly IServiceFactory _serviceFactory;

    public ContentExtractorFactory(IServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    public IContentExtractor CreateExtractor(string contentType)
    {
        return _serviceFactory.CreateContentExtractor(contentType);
    }
}

/// <summary>
/// 청킹 전략 팩토리 인터페이스
/// </summary>
public interface IChunkingStrategyFactory
{
    /// <summary>
    /// 청킹 전략을 생성합니다.
    /// </summary>
    /// <param name="strategyType">청킹 전략 타입</param>
    /// <returns>청킹 전략 인스턴스</returns>
    IChunkingStrategy CreateStrategy(string strategyType);

    /// <summary>
    /// 사용 가능한 전략 목록을 반환합니다.
    /// </summary>
    /// <returns>전략 이름 목록</returns>
    IReadOnlyList<string> GetAvailableStrategies();
}

/// <summary>
/// 청킹 전략 팩토리 구현체
/// </summary>
public class ChunkingStrategyFactory : IChunkingStrategyFactory
{
    private readonly IServiceFactory _serviceFactory;
    private static readonly IReadOnlyList<string> AvailableStrategies = new[]
    {
        "FixedSize",
        "Paragraph",
        "Smart",
        "Semantic"
        // 향후 추가: "Intelligent", "MemoryOptimized", "Auto"
    };

    public ChunkingStrategyFactory(IServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    }

    public IChunkingStrategy CreateStrategy(string strategyType)
    {
        return _serviceFactory.CreateChunkingStrategy(strategyType);
    }

    public IReadOnlyList<string> GetAvailableStrategies()
    {
        return AvailableStrategies;
    }
}