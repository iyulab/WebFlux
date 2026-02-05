using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using WebFlux.Configuration;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Services;
using WebFlux.Services.AiEnhancement;
using WebFlux.Services.ChunkingStrategies;
using WebFlux.Services.ContentExtractors;
using WebFlux.Services.Crawlers;

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

        // Phase 5A.2: 멀티모달 처리 파이프라인 등록
        services.AddWebFluxMultimodal();

        // Phase 5C.2: 회복탄력성 서비스 등록
        services.AddWebFluxResilience();

        // Phase 5C.2: 처리 최적화 서비스 등록
        services.AddWebFluxProcessingOptimization();

        // 경량 추출 서비스 등록 (Rate Limiter, 품질 평가기)
        services.AddWebFluxLightweightExtraction();

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

        // PlaywrightCrawler 등록 추가 (Phase 1)
        services.TryAddTransient<PlaywrightCrawler>();

        // 키드 서비스로 크롤러 등록
        services.AddKeyedTransient<ICrawler, BreadthFirstCrawler>("BreadthFirst");
        services.AddKeyedTransient<ICrawler, DepthFirstCrawler>("DepthFirst");
        services.AddKeyedTransient<ICrawler, SitemapCrawler>("Sitemap");
        services.AddKeyedTransient<ICrawler, IntelligentCrawler>("Intelligent");
        services.AddKeyedTransient<ICrawler, PlaywrightCrawler>("Dynamic"); // Phase 1

        // 크롤러 팩토리 등록
        services.TryAddSingleton<ICrawlerFactory, CrawlerFactory>();

        // 과잉 기능 제거 - Interface Provider 패턴으로 단순화
        // 소비자가 필요한 서비스만 구현하도록 변경

        return services;
    }

    /// <summary>
    /// 콘텐츠 추출 관련 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxContentExtraction(this IServiceCollection services)
    {
        // 기본 콘텐츠 추출기 등록 (BasicContentExtractor)
        services.TryAddTransient<BasicContentExtractor>();

        // 키드 서비스로 콘텐츠 추출기 등록 (모든 콘텐츠 타입에 BasicContentExtractor 사용)
        services.AddKeyedTransient<IContentExtractor, BasicContentExtractor>("Html");
        services.AddKeyedTransient<IContentExtractor, BasicContentExtractor>("Text");
        services.AddKeyedTransient<IContentExtractor, BasicContentExtractor>("Json");
        services.AddKeyedTransient<IContentExtractor, BasicContentExtractor>("Markdown");
        services.AddKeyedTransient<IContentExtractor, BasicContentExtractor>("Xml");
        services.AddKeyedTransient<IContentExtractor, BasicContentExtractor>("Default");

        // 콘텐츠 추출기 팩토리 등록
        services.TryAddSingleton<IContentExtractorFactory, ContentExtractorFactory>();

        // Phase 5C: 메타데이터 추출기 등록
        services.TryAddScoped<IMetadataExtractor, MetadataExtractor>();

        // Phase 5C Week 2: 마크다운 구조 분석기 등록
        services.TryAddScoped<IMarkdownStructureAnalyzer, MarkdownStructureAnalyzer>();

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

        // Phase 4D: 고급 청킹 전략들
        services.TryAddTransient<AutoChunkingStrategy>();
        services.TryAddTransient<MemoryOptimizedChunkingStrategy>();

        // 키드 서비스로 청킹 전략 등록
        services.AddKeyedTransient<IChunkingStrategy, FixedSizeChunkingStrategy>("FixedSize");
        services.AddKeyedTransient<IChunkingStrategy, ParagraphChunkingStrategy>("Paragraph");
        services.AddKeyedTransient<IChunkingStrategy, SmartChunkingStrategy>("Smart");
        services.AddKeyedTransient<IChunkingStrategy, SemanticChunkingStrategy>("Semantic");
        services.AddKeyedTransient<IChunkingStrategy, AutoChunkingStrategy>("Auto");
        services.AddKeyedTransient<IChunkingStrategy, MemoryOptimizedChunkingStrategy>("MemoryOptimized");

        // 청킹 전략 팩토리 등록
        services.TryAddSingleton<IChunkingStrategyFactory, ChunkingStrategyFactory>();

        // Interface Provider 패턴: 토큰 계산 서비스는 소비자가 구현
        // services.TryAddSingleton<ITokenCountService, 구현체>();

        // 청크 품질 평가 서비스 등록 (Phase 5B.2)
        // 청킹 품질 평가는 Interface Provider 패턴으로 소비자가 구현
        // services.TryAddScoped<IChunkQualityEvaluator, 구현체>();

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

        // Phase 1: AI 증강 서비스 자동 등록
        services.AddWebFluxAIEnhancement();

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

        // Phase 1: AI 증강 서비스 자동 등록
        services.AddWebFluxAIEnhancement();

        return services;
    }

    /// <summary>
    /// Mock AI 서비스를 등록합니다. (테스트 및 데모용)
    /// Interface Provider 패턴 - 소비자가 실제 구현체 제공
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxMockAIServices(this IServiceCollection services)
    {
        // Interface Provider 패턴: 인터페이스만 등록, 구현은 소비자가 제공
        // Mock 서비스는 제거됨 - 테스트/데모가 필요한 소비자가 직접 구현

        return services;
    }

    /// <summary>
    /// OpenAI AI 서비스를 등록합니다. (프로덕션용)
    /// 실제 구현체는 소비자가 제공 - Interface Provider 패턴
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxOpenAIServices(this IServiceCollection services)
    {
        // Interface Provider 패턴: 소비자가 OpenAI 구현체 제공
        // return services.AddWebFluxAIServices<OpenAITextCompletionService, OpenAIImageToTextService, OpenAITextEmbeddingService>();

        // SDK는 인터페이스만 제공, 구현은 소비자 선택
        return services;
    }

    /// <summary>
    /// 캐싱 서비스를 등록합니다. (Interface Provider 패턴)
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxCaching(this IServiceCollection services)
    {
        // Interface Provider 패턴: 인터페이스만 제공, 구현은 소비자가 제공
        // 기본 MemoryCache는 .NET 표준이므로 등록
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// 성능 측정 인터페이스를 등록합니다. (Interface Provider 패턴)
    /// 구체적인 모니터링은 소비 애플리케이션에서 구현
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxPerformanceMonitoring(this IServiceCollection services)
    {
        // Interface Provider 패턴: 인터페이스만 제공, 구현은 소비자가 선택
        // services.TryAddSingleton<IPerformanceMonitor>(구현체는_소비자가_제공);

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
    /// 진행률 리포팅 서비스를 등록합니다. (Interface Provider 패턴)
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxProgressReporting(this IServiceCollection services)
    {
        // Interface Provider 패턴: 인터페이스만 제공, 구현은 소비자가 제공

        return services;
    }

    /// <summary>
    /// 멀티모달 처리 서비스를 등록합니다. (Interface Provider 패턴)
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxMultimodal(this IServiceCollection services)
    {
        // Interface Provider 패턴: 인터페이스만 제공, 구현은 소비자가 제공

        return services;
    }

    /// <summary>
    /// 회복탄력성 서비스를 등록합니다. (Polly 기반)
    /// 재시도, 회로차단기, 시간초과, 벌크헤드 패턴 제공
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxResilience(this IServiceCollection services)
    {
        // 회복탄력성 서비스 등록
        services.TryAddSingleton<IResilienceService, ResilienceService>();

        // HTTP 클라이언트 등록 (기본 정책은 ResilienceService에서 적용)
        services.AddHttpClient("webflux-default");

        return services;
    }

    /// <summary>
    /// 처리 최적화 서비스를 등록합니다. (지능형 성능 최적화)
    /// 콘텐츠 분석, 성능 메트릭, 리소스 모니터링을 통한 동적 최적화
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxProcessingOptimization(this IServiceCollection services)
    {
        // 처리 최적화 서비스 등록
        // services.TryAddSingleton<IProcessingOptimizationService, ProcessingOptimizationService>();

        // Interface Provider 패턴: 실제 구현체는 소비자가 제공
        // services.TryAddSingleton<IPerformanceMonitor, 구현체>();    // 성능 모니터링
        // services.TryAddSingleton<ITokenCountService, 구현체>();     // 토큰 계산
        // services.TryAddSingleton<ICacheService, 구현체>();          // 캐시 서비스

        return services;
    }

    /// <summary>
    /// 경량 추출 서비스를 등록합니다.
    /// 도메인별 Rate Limiter, 콘텐츠 품질 평가기 등
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxLightweightExtraction(this IServiceCollection services)
    {
        // 도메인별 Rate Limiter 등록 (Singleton - 애플리케이션 전체에서 상태 공유)
        services.TryAddSingleton<IDomainRateLimiter, DomainRateLimiter>();

        // 콘텐츠 품질 평가기 등록 (Transient - 매번 새 인스턴스)
        services.TryAddTransient<IContentQualityEvaluator, ContentQualityEvaluator>();

        return services;
    }

    /// <summary>
    /// AI 증강 서비스를 등록합니다. (Phase 1)
    /// ITextCompletionService가 등록된 경우에만 BasicAiEnhancementService를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddWebFluxAIEnhancement(this IServiceCollection services)
    {
        // BasicAiEnhancementService 등록 (Scoped - ITextCompletionService 의존성)
        // ITextCompletionService가 등록되어 있어야 작동
        services.TryAddScoped<IAiEnhancementService, BasicAiEnhancementService>();

        return services;
    }

}