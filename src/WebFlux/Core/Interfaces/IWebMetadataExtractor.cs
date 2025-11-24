using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 웹 콘텐츠에서 AI 기반 메타데이터를 추출하는 인터페이스
/// HTML 메타데이터와 AI 추출을 결합하여 풍부한 메타데이터를 생성
/// </summary>
public interface IWebMetadataExtractor
{
    /// <summary>
    /// 웹 콘텐츠에서 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="content">웹 콘텐츠 텍스트 (HTML, Markdown 등)</param>
    /// <param name="url">소스 URL</param>
    /// <param name="htmlMetadata">HTML에서 추출한 메타데이터 (선택적, AI 프롬프트 힌트로 사용)</param>
    /// <param name="schema">메타데이터 스키마 (General, TechnicalDoc, ProductManual, Article)</param>
    /// <param name="customPrompt">커스텀 추출 프롬프트 (schema가 Custom인 경우 사용)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 풍부한 메타데이터</returns>
    Task<EnrichedMetadata> ExtractAsync(
        string content,
        string url,
        HtmlMetadataSnapshot? htmlMetadata = null,
        MetadataSchema schema = MetadataSchema.General,
        string? customPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 콘텐츠에서 메타데이터를 배치로 추출합니다.
    /// API 호출 최적화를 위해 병렬 처리를 수행합니다.
    /// </summary>
    /// <param name="items">추출할 (content, url) 쌍 목록</param>
    /// <param name="schema">메타데이터 스키마</param>
    /// <param name="customPrompt">커스텀 추출 프롬프트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 메타데이터 목록</returns>
    Task<IReadOnlyList<EnrichedMetadata>> ExtractBatchAsync(
        IEnumerable<(string content, string url, HtmlMetadataSnapshot? htmlMetadata)> items,
        MetadataSchema schema = MetadataSchema.General,
        string? customPrompt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지원하는 메타데이터 스키마 목록을 반환합니다.
    /// </summary>
    /// <returns>지원하는 스키마 목록</returns>
    IReadOnlyList<MetadataSchema> GetSupportedSchemas();

    /// <summary>
    /// 특정 스키마에 대한 설명을 반환합니다.
    /// </summary>
    /// <param name="schema">스키마</param>
    /// <returns>스키마 설명</returns>
    string GetSchemaDescription(MetadataSchema schema);
}

/// <summary>
/// 메타데이터 추출 스키마 열거형
/// 웹 콘텐츠 타입에 따라 최적화된 추출 전략을 선택
/// </summary>
public enum MetadataSchema
{
    /// <summary>
    /// 일반 웹 콘텐츠 (기본값)
    /// 추출 필드: topics, keywords, description, documentType, language, categories
    /// 적합한 콘텐츠: 블로그, 일반 웹페이지, 랜딩 페이지
    /// </summary>
    General,

    /// <summary>
    /// 기술 문서 (Technical Documentation)
    /// 추출 필드: topics, libraries, frameworks, technologies, apiVersion, keywords
    /// 적합한 콘텐츠: react.dev, MDN, API 문서, 개발자 가이드
    /// 예시 데이터: libraries=["react@18.2.0"], frameworks=["React"], technologies=["JavaScript", "TypeScript"]
    /// </summary>
    TechnicalDoc,

    /// <summary>
    /// 제품 페이지 (Product Manual/Specification)
    /// 추출 필드: productName, company, version, model, price, currency, categories
    /// 적합한 콘텐츠: 제품 상세 페이지, 스펙 페이지, 전자상거래 페이지
    /// 예시 데이터: productName="iPhone 15 Pro", company="Apple", price=999.00
    /// </summary>
    ProductManual,

    /// <summary>
    /// 블로그/뉴스 기사 (Article/Blog Post)
    /// 추출 필드: articleTitle, author, publishedDate, tags, readingTimeMinutes
    /// 적합한 콘텐츠: 블로그 포스트, 뉴스 기사, 튜토리얼
    /// 예시 데이터: author="John Doe", publishedDate="2024-01-10", readingTimeMinutes=8
    /// </summary>
    Article,

    /// <summary>
    /// 사용자 정의 스키마 (Custom Schema)
    /// 커스텀 프롬프트를 사용하여 특정 도메인에 맞는 메타데이터 추출
    /// customPrompt 매개변수 필수
    /// </summary>
    Custom
}
