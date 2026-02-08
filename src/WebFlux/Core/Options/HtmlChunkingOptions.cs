using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// HTML DOM 구조 기반 청킹 옵션
/// 시맨틱 태그를 활용하여 의미적으로 완결된 청크 생성
/// </summary>
public class HtmlChunkingOptions : IValidatable
{
    /// <summary>
    /// DOM 구조 기반 청킹 활성화
    /// </summary>
    public bool PreserveDomStructure { get; set; } = true;

    /// <summary>
    /// 주요 콘텐츠 영역 선택자
    /// 이 선택자에 매치되는 요소 내부의 콘텐츠만 추출
    /// </summary>
    public List<string> ContentSelectors { get; set; } = new()
    {
        "article",
        "main",
        "[role='main']",
        ".content",
        "#content",
        ".post-content",
        ".entry-content",
        ".article-content"
    };

    /// <summary>
    /// 제외할 영역 선택자
    /// 이 선택자에 매치되는 요소는 콘텐츠에서 제외
    /// </summary>
    public List<string> ExcludeSelectors { get; set; } = new()
    {
        "nav",
        "footer",
        "header",
        "aside",
        ".sidebar",
        ".advertisement",
        ".ads",
        ".social-share",
        ".related-posts",
        ".comments",
        "#comments",
        ".navigation",
        ".menu",
        "[role='navigation']",
        "[role='complementary']",
        "[aria-hidden='true']"
    };

    /// <summary>
    /// 섹션 분리 기준 태그
    /// 이 태그들을 기준으로 청크를 분리
    /// </summary>
    public List<string> SectionTags { get; set; } = new()
    {
        "section",
        "article",
        "div.section",
        "div[class*='section']"
    };

    /// <summary>
    /// 헤딩 태그 (청크 분리 기준)
    /// </summary>
    public List<string> HeadingTags { get; set; } = new()
    {
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6"
    };

    /// <summary>
    /// 최대 청크 크기 (문자 수)
    /// </summary>
    public int MaxChunkSize { get; set; } = 1500;

    /// <summary>
    /// 최소 청크 크기 (문자 수)
    /// 이보다 작은 청크는 이전 또는 다음 청크와 병합
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// 헤딩 계층 구조 유지
    /// </summary>
    public bool PreserveHeadingHierarchy { get; set; } = true;

    /// <summary>
    /// DOM 경로 포함
    /// </summary>
    public bool IncludeDomPath { get; set; } = true;

    /// <summary>
    /// 코드 블록을 단일 청크로 유지
    /// </summary>
    public bool KeepCodeBlocksTogether { get; set; } = true;

    /// <summary>
    /// 테이블을 단일 청크로 유지
    /// </summary>
    public bool KeepTablesTogether { get; set; } = true;

    /// <summary>
    /// 리스트를 단일 청크로 유지
    /// </summary>
    public bool KeepListsTogether { get; set; } = true;

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxChunkSize <= 0)
            errors.Add("MaxChunkSize must be greater than 0");

        if (MinChunkSize <= 0)
            errors.Add("MinChunkSize must be greater than 0");

        if (MaxChunkSize <= MinChunkSize)
            errors.Add("MaxChunkSize must be greater than MinChunkSize");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
