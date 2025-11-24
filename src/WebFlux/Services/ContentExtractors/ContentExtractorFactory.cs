using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// 콘텐츠 추출기 팩토리 인터페이스
/// Interface Provider 패턴에서 콘텐츠 추출기 인스턴스 생성을 담당
/// </summary>
public interface IContentExtractorFactory
{
    /// <summary>
    /// 콘텐츠 타입에 따른 추출기 인스턴스를 생성합니다
    /// </summary>
    /// <param name="contentType">콘텐츠 타입</param>
    /// <returns>콘텐츠 추출기 인스턴스</returns>
    IContentExtractor CreateExtractor(string contentType);
}

/// <summary>
/// 콘텐츠 추출기 팩토리 구현
/// </summary>
public class ContentExtractorFactory : IContentExtractorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ContentExtractorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IContentExtractor CreateExtractor(string contentType)
    {
        // Interface Provider 패턴에 따라 기본 추출기 반환
        // 실제 추출기는 구현자가 제공
        var extractor = _serviceProvider.GetService(typeof(IContentExtractor)) as IContentExtractor;
        return extractor ?? new BasicContentExtractor(_serviceProvider.GetService(typeof(IEventPublisher)) as IEventPublisher);
    }
}