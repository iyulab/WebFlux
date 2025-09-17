using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using WebFlux.Configuration;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using WebFlux.Services.ChunkingStrategies;
using WebFlux.Services.Crawlers;
using WebFlux.Services.AI;
using WebFlux.Services.Progress;

namespace WebFlux.Extensions;

/// <summary>
/// IServiceCollection 확장 메서드
/// WebFlux SDK의 모든 서비스를 DI 컨테이너에 등록
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// WebFlux SDK의 모든 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">구성 객체</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFlux(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddWebFlux(config =>
        {
            configuration.GetSection("WebFlux").Bind(config);
        });
    }

    /// <summary>
    /// WebFlux SDK의 모든 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configureOptions">구성 옵션 설정 액션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFlux(
        this IServiceCollection services,
        Action<WebFluxConfiguration>? configureOptions = null)
    {
        // 기본 구성 등록
        services.Configure<WebFluxConfiguration>(config =>
        {
            // 기본값 설정
            configureOptions?.Invoke(config);
        });

        // 핵심 서비스 등록
        services.AddWebFluxCore();

        // HTTP 클라이언트 등록
        services.AddWebFluxHttp();

        // 크롤링 서비스 등록
        services.AddWebFluxCrawling();

        // 콘텐츠 추출 서비스 등록
        services.AddWebFluxContentExtraction();

        // 청킹 서비스 등록
        services.AddWebFluxChunking();

        // Playwright 서비스 등록
        services.AddWebFluxPlaywright();

        // 진행률 리포팅 서비스 등록
        services.AddWebFluxProgressReporting();

        // 로깅 구성
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// WebFlux 핵심 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxCore(this IServiceCollection services)
    {
        // 메인 처리기 등록 (Scoped - 요청당 하나의 인스턴스)
        services.TryAddScoped<IWebContentProcessor, WebContentProcessor>();

        // 서비스 팩토리 등록
        services.TryAddSingleton<IServiceFactory, ServiceFactory>();

        // 이벤트 발행자 등록 (Singleton - 애플리케이션 전체에서 하나)
        services.TryAddSingleton<IEventPublisher, EventPublisher>();

        return services;
    }

    /// <summary>
    /// HTTP 클라이언트 관련 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxHttp(this IServiceCollection services)
    {
        // HTTP 클라이언트 팩토리 등록
        services.AddHttpClient();

        // WebFlux 전용 HTTP 클라이언트 등록
        services.AddHttpClient<IHttpClientService, HttpClientService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent",
                "WebFlux/1.0 (+https://github.com/webflux/webflux)");
        });

        return services;
    }

    /// <summary>
    /// 크롤링 관련 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxCrawling(this IServiceCollection services)
    {
        // 크롤러 구현체들 등록 (Transient - 매번 새 인스턴스)
        services.TryAddTransient<BreadthFirstCrawler>();
        services.TryAddTransient<DepthFirstCrawler>();
        services.TryAddTransient<SitemapCrawler>();
        services.TryAddTransient<IntelligentCrawler>();

        // 크롤러 팩토리 등록
        services.TryAddSingleton<ICrawlerFactory, CrawlerFactory>();

        // Robots.txt 서비스 등록
        services.TryAddScoped<IRobotsTxtService, RobotsTxtService>();
        services.TryAddScoped<IRobotsTxtParser, RobotsTxtParser>();

        // llms.txt 파서 서비스 등록
        services.TryAddScoped<ILlmsParser, LlmsParser>();

        // 사이트맵 분석 서비스 등록
        services.TryAddScoped<ISitemapAnalyzer, SitemapAnalyzer>();

        // ai.txt 파서 서비스 등록
        services.TryAddScoped<IAiTxtParser, AiTxtParser>();

        // manifest.json 파서 서비스 등록
        services.TryAddScoped<IManifestParser, ManifestParser>();

        return services;
    }

    /// <summary>
    /// 콘텐츠 추출 관련 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxContentExtraction(this IServiceCollection services)
    {
        // 콘텐츠 추출기들 등록 (Transient)
        services.TryAddTransient<PlaywrightContentExtractor>(); // Playwright 기반 동적 크롤링
        services.TryAddTransient<MarkdownContentExtractor>();
        services.TryAddTransient<JsonContentExtractor>();
        services.TryAddTransient<XmlContentExtractor>();
        services.TryAddTransient<TextContentExtractor>();

        // 콘텐츠 추출기 팩토리 등록
        services.TryAddSingleton<IContentExtractorFactory, ContentExtractorFactory>();

        // 메타데이터 추출 서비스 등록
        services.TryAddScoped<IMetadataExtractorService, MetadataExtractorService>();

        return services;
    }

    /// <summary>
    /// 청킹 관련 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxChunking(this IServiceCollection services)
    {
        // 청킹 전략들 등록 (Transient)
        services.TryAddTransient<FixedSizeChunkingStrategy>();
        services.TryAddTransient<ParagraphChunkingStrategy>();
        services.TryAddTransient<SmartChunkingStrategy>();
        services.TryAddTransient<SemanticChunkingStrategy>();

        // 청킹 전략 팩토리 등록
        services.TryAddSingleton<IChunkingStrategyFactory, ChunkingStrategyFactory>();

        // 토큰 계산 서비스 등록
        services.TryAddSingleton<ITokenCountService, TokenCountService>();

        // 청크 품질 평가 서비스 등록
        services.TryAddScoped<IChunkQualityEvaluator, ChunkQualityEvaluator>();

        return services;
    }

    /// <summary>
    /// AI 서비스 구현체를 등록합니다. (소비자가 호출)
    /// </summary>
    /// <typeparam name="TTextCompletion">텍스트 완성 서비스 구현체</typeparam>
    /// <typeparam name="TImageToText">이미지-텍스트 변환 서비스 구현체</typeparam>
    /// <typeparam name="TTextEmbedding">텍스트 임베딩 서비스 구현체</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxAIServices<TTextCompletion, TImageToText, TTextEmbedding>(
        this IServiceCollection services)
        where TTextCompletion : class, ITextCompletionService
        where TImageToText : class, IImageToTextService
        where TTextEmbedding : class, ITextEmbeddingService
    {
        services.TryAddScoped<ITextCompletionService, TTextCompletion>();
        services.TryAddScoped<IImageToTextService, TImageToText>();
        services.TryAddScoped<ITextEmbeddingService, TTextEmbedding>();

        return services;
    }

    /// <summary>
    /// AI 서비스 구현체를 등록합니다. (임베딩 없는 버전, 하위 호환성)
    /// </summary>
    /// <typeparam name="TTextCompletion">텍스트 완성 서비스 구현체</typeparam>
    /// <typeparam name="TImageToText">이미지-텍스트 변환 서비스 구현체</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxAIServices<TTextCompletion, TImageToText>(
        this IServiceCollection services)
        where TTextCompletion : class, ITextCompletionService
        where TImageToText : class, IImageToTextService
    {
        services.TryAddScoped<ITextCompletionService, TTextCompletion>();
        services.TryAddScoped<IImageToTextService, TImageToText>();

        return services;
    }

    /// <summary>
    /// Mock AI 서비스를 등록합니다. (테스트 및 데모용)
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxMockAIServices(this IServiceCollection services)
    {
        return services.AddWebFluxAIServices<MockTextCompletionService, MockImageToTextService, MockTextEmbeddingService>();
    }

    /// <summary>
    /// OpenAI AI 서비스를 등록합니다. (프로덕션용)
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxOpenAIServices(this IServiceCollection services)
    {
        return services.AddWebFluxAIServices<OpenAITextCompletionService, OpenAIImageToTextService, OpenAITextEmbeddingService>();
    }

    /// <summary>
    /// 캐싱 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.TryAddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// 성능 모니터링 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxPerformanceMonitoring(this IServiceCollection services)
    {
        services.TryAddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.TryAddScoped<IMetricsCollector, MetricsCollector>();

        return services;
    }

    /// <summary>
    /// Playwright 관련 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxPlaywright(this IServiceCollection services)
    {
        // Playwright 인스턴스를 Singleton으로 등록 (애플리케이션당 하나)
        services.TryAddSingleton<IPlaywright>(serviceProvider =>
        {
            // Playwright.CreateAsync()를 동기적으로 호출하기 위한 헬퍼
            return Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
        });

        return services;
    }

    /// <summary>
    /// 진행률 리포팅 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxProgressReporting(this IServiceCollection services)
    {
        services.TryAddSingleton<IProgressReporter, InMemoryProgressReporter>();

        return services;
    }
}