using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
/// 콘텐츠 타입 기반으로 최적의 추출기를 선택하고, 실패 시 BasicContentExtractor로 폴백
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
        // 콘텐츠 타입 기반 키드 서비스 선택
        var key = ResolveExtractorKey(contentType);

        try
        {
            var extractor = _serviceProvider.GetKeyedService<IContentExtractor>(key);
            if (extractor != null)
                return extractor;
        }
        catch
        {
            // 키드 서비스 해결 실패 시 폴백
        }

        // 폴백: 기본 추출기
        try
        {
            var defaultExtractor = _serviceProvider.GetKeyedService<IContentExtractor>("Default");
            if (defaultExtractor != null)
                return defaultExtractor;
        }
        catch
        {
            // 기본 추출기도 실패 시 직접 생성
        }

        return new BasicContentExtractor(
            _serviceProvider.GetService(typeof(IEventPublisher)) as IEventPublisher);
    }

    /// <summary>
    /// 콘텐츠 타입을 추출기 키로 변환합니다
    /// </summary>
    private static string ResolveExtractorKey(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return "Default";

        var normalized = contentType.ToLowerInvariant().Trim();

        return normalized switch
        {
            var ct when ct.Contains("html") || ct.Contains("xhtml") => "Html",
            var ct when ct.Contains("json") => "Json",
            var ct when ct.Contains("xml") => "Xml",
            var ct when ct.Contains("markdown") || ct.Contains("md") => "Markdown",
            var ct when ct.Contains("text/plain") => "Text",
            _ => "Default"
        };
    }
}
