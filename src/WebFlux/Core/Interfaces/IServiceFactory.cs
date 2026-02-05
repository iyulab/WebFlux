using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 서비스 팩토리 인터페이스
/// 런타임에 동적으로 서비스 인스턴스를 생성
/// </summary>
public interface IServiceFactory
{
    /// <summary>
    /// 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <returns>서비스 인스턴스</returns>
    T CreateService<T>() where T : class;

    /// <summary>
    /// 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="serviceType">서비스 타입</param>
    /// <returns>서비스 인스턴스</returns>
    object CreateService(Type serviceType);

    /// <summary>
    /// 명명된 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <typeparam name="T">서비스 타입</typeparam>
    /// <param name="name">서비스 이름</param>
    /// <returns>서비스 인스턴스</returns>
    T CreateNamedService<T>(string name) where T : class;

    /// <summary>
    /// 크롤러 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="strategy">크롤링 전략</param>
    /// <returns>크롤러 인스턴스</returns>
    ICrawler CreateCrawler(CrawlStrategy strategy);

    /// <summary>
    /// 콘텐츠 추출기 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="contentType">콘텐츠 타입</param>
    /// <returns>콘텐츠 추출기 인스턴스</returns>
    IContentExtractor CreateContentExtractor(string contentType);

    /// <summary>
    /// 청킹 전략 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="strategyType">청킹 전략 타입</param>
    /// <returns>청킹 전략 인스턴스</returns>
    IChunkingStrategy CreateChunkingStrategy(string strategyType);

    /// <summary>
    /// AI 증강 서비스 인스턴스를 생성합니다.
    /// </summary>
    /// <returns>AI 증강 서비스 인스턴스 (서비스가 등록되지 않은 경우 null)</returns>
    IAiEnhancementService? CreateAiEnhancementService();

    /// <summary>
    /// 캐시 서비스 인스턴스를 생성합니다 (선택적).
    /// Interface Provider 패턴 - 소비자가 등록하지 않은 경우 null 반환
    /// </summary>
    /// <returns>캐시 서비스 인스턴스 (등록되지 않은 경우 null)</returns>
    ICacheService? TryCreateCacheService();

    /// <summary>
    /// 도메인 Rate Limiter 인스턴스를 생성합니다 (선택적).
    /// </summary>
    /// <returns>Rate Limiter 인스턴스 (등록되지 않은 경우 null)</returns>
    IDomainRateLimiter? TryCreateDomainRateLimiter();

    /// <summary>
    /// 콘텐츠 품질 평가기 인스턴스를 생성합니다 (선택적).
    /// </summary>
    /// <returns>품질 평가기 인스턴스 (등록되지 않은 경우 null)</returns>
    IContentQualityEvaluator? TryCreateContentQualityEvaluator();
}