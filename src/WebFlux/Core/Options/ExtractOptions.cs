using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Core.Options;

/// <summary>
/// 웹 콘텐츠 추출 옵션
/// 청킹 없는 경량 텍스트 추출 API를 위한 설정
/// </summary>
public class ExtractOptions : IValidatable
{
    #region 크롤링 설정

    /// <summary>
    /// JavaScript 렌더링 사용 여부 (Playwright 기반)
    /// SPA, 동적 콘텐츠 페이지에 사용
    /// </summary>
    public bool UseDynamicRendering { get; set; } = false;

    /// <summary>
    /// 동적 렌더링 시 대기할 CSS 선택자
    /// 예: "article.content", "#main-content"
    /// </summary>
    public string? WaitForSelector { get; set; }

    /// <summary>
    /// 요청 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// User-Agent 헤더
    /// </summary>
    public string UserAgent { get; set; } = "WebFlux/1.0";

    /// <summary>
    /// 추가 HTTP 헤더
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    #endregion

    #region 캐싱 설정

    /// <summary>
    /// 캐시 사용 여부
    /// </summary>
    public bool UseCache { get; set; } = true;

    /// <summary>
    /// 캐시 만료 시간 (분)
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 캐시 무시하고 강제 새로고침
    /// </summary>
    public bool ForceRefresh { get; set; } = false;

    #endregion

    #region 추출 설정

    /// <summary>
    /// 출력 포맷
    /// </summary>
    public OutputFormat Format { get; set; } = OutputFormat.Markdown;

    /// <summary>
    /// Boilerplate(헤더, 푸터, 네비게이션 등) 제거 여부
    /// </summary>
    public bool RemoveBoilerplate { get; set; } = true;

    /// <summary>
    /// 메타데이터 포함 여부
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// 이미지 URL 포함 여부
    /// </summary>
    public bool IncludeImages { get; set; } = false;

    /// <summary>
    /// 링크 URL 포함 여부
    /// </summary>
    public bool IncludeLinks { get; set; } = false;

    /// <summary>
    /// 최대 텍스트 길이 (null이면 제한 없음)
    /// LLM 토큰 최적화를 위해 설정
    /// </summary>
    public int? MaxTextLength { get; set; }

    #endregion

    #region 품질 평가 설정

    /// <summary>
    /// 콘텐츠 품질 평가 수행 여부
    /// </summary>
    public bool EvaluateQuality { get; set; } = true;

    #endregion

    #region 재시도 설정

    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    #endregion

    #region 배치 처리 설정

    /// <summary>
    /// 최대 병렬 처리 수
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// 도메인별 Rate Limiting 적용 여부
    /// </summary>
    public bool EnableDomainRateLimiting { get; set; } = true;

    /// <summary>
    /// 도메인별 최소 요청 간격 (밀리초)
    /// </summary>
    public int DomainMinIntervalMs { get; set; } = 1000;

    #endregion

    /// <summary>
    /// 기본 옵션 생성
    /// </summary>
    public static ExtractOptions Default => new();

    /// <summary>
    /// 빠른 추출을 위한 옵션 (캐시 사용, 정적 렌더링)
    /// </summary>
    public static ExtractOptions Fast => new()
    {
        UseDynamicRendering = false,
        UseCache = true,
        TimeoutSeconds = 10,
        EvaluateQuality = false,
        MaxRetries = 1
    };

    /// <summary>
    /// 고품질 추출을 위한 옵션 (동적 렌더링, 품질 평가)
    /// </summary>
    public static ExtractOptions HighQuality => new()
    {
        UseDynamicRendering = true,
        UseCache = true,
        TimeoutSeconds = 30,
        EvaluateQuality = true,
        RemoveBoilerplate = true,
        IncludeMetadata = true,
        MaxRetries = 3
    };

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (TimeoutSeconds <= 0)
            errors.Add("TimeoutSeconds must be greater than 0");

        if (MaxConcurrency <= 0)
            errors.Add("MaxConcurrency must be greater than 0");

        if (MaxRetries < 0)
            errors.Add("MaxRetries must be greater than or equal to 0");

        if (CacheExpirationMinutes <= 0)
            errors.Add("CacheExpirationMinutes must be greater than 0");

        if (DomainMinIntervalMs < 0)
            errors.Add("DomainMinIntervalMs must be greater than or equal to 0");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// 출력 포맷 열거형
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Markdown 포맷 (LLM 최적화, 토큰 절감)
    /// </summary>
    Markdown,

    /// <summary>
    /// HTML 포맷 (구조 보존)
    /// </summary>
    Html,

    /// <summary>
    /// 순수 텍스트 (최소 토큰)
    /// </summary>
    PlainText
}
