using Microsoft.Extensions.Configuration;
using WebFlux.Core.Models;

namespace WebFlux.Configuration;

/// <summary>
/// WebFlux 옵션 설정 클래스
/// appsettings.json이나 다른 구성 소스와 바인딩
/// </summary>
public class WebFluxOptions
{
    /// <summary>
    /// 구성 섹션 이름
    /// </summary>
    public const string SectionName = "WebFlux";

    /// <summary>
    /// WebFlux 구성
    /// </summary>
    public WebFluxConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// 개발 모드 여부
    /// </summary>
    public bool DevelopmentMode { get; set; } = false;

    /// <summary>
    /// 상세 로깅 활성화 여부
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// 메트릭 수집 활성화 여부
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// 성능 프로파일링 활성화 여부
    /// </summary>
    public bool EnableProfiling { get; set; } = false;

    /// <summary>
    /// 기본 사용자 에이전트
    /// </summary>
    public string DefaultUserAgent { get; set; } = "WebFlux/1.0 (+https://github.com/webflux/webflux)";

    /// <summary>
    /// 최대 동시 요청 수
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// 기본 타임아웃 (초)
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 구성 검증
    /// </summary>
    /// <returns>검증 결과</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (MaxConcurrentRequests <= 0)
        {
            errors.Add("MaxConcurrentRequests must be greater than 0");
        }

        if (DefaultTimeoutSeconds <= 0)
        {
            errors.Add("DefaultTimeoutSeconds must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(DefaultUserAgent))
        {
            errors.Add("DefaultUserAgent cannot be empty");
        }

        // WebFluxConfiguration 검증
        var configValidation = ValidateWebFluxConfiguration();
        errors.AddRange(configValidation);

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private List<string> ValidateWebFluxConfiguration()
    {
        var errors = new List<string>();

        // 크롤링 구성 검증
        if (Configuration.Crawling.MaxConcurrentRequests <= 0)
        {
            errors.Add("Crawling.MaxConcurrentRequests must be greater than 0");
        }

        if (Configuration.Crawling.DefaultTimeoutSeconds <= 0)
        {
            errors.Add("Crawling.DefaultTimeoutSeconds must be greater than 0");
        }

        // 청킹 구성 검증
        if (Configuration.Chunking.DefaultMaxChunkSize <= 0)
        {
            errors.Add("Chunking.DefaultMaxChunkSize must be greater than 0");
        }

        if (Configuration.Chunking.DefaultMinChunkSize <= 0)
        {
            errors.Add("Chunking.DefaultMinChunkSize must be greater than 0");
        }

        if (Configuration.Chunking.DefaultMaxChunkSize <= Configuration.Chunking.DefaultMinChunkSize)
        {
            errors.Add("Chunking.DefaultMaxChunkSize must be greater than DefaultMinChunkSize");
        }

        // 성능 구성 검증
        if (Configuration.Performance.MaxDegreeOfParallelism <= 0)
        {
            errors.Add("Performance.MaxDegreeOfParallelism must be greater than 0");
        }

        return errors;
    }
}

/// <summary>
/// 검증 결과
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 오류 목록
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 경고 목록
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// WebFlux 옵션 빌더
/// </summary>
public class WebFluxOptionsBuilder
{
    private readonly WebFluxOptions _options = new();

    /// <summary>
    /// 개발 모드 설정
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder EnableDevelopmentMode(bool enabled = true)
    {
        _options.DevelopmentMode = enabled;
        return this;
    }

    /// <summary>
    /// 상세 로깅 설정
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder EnableVerboseLogging(bool enabled = true)
    {
        _options.EnableVerboseLogging = enabled;
        return this;
    }

    /// <summary>
    /// 메트릭 수집 설정
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder EnableMetrics(bool enabled = true)
    {
        _options.EnableMetrics = enabled;
        return this;
    }

    /// <summary>
    /// 크롤링 설정
    /// </summary>
    /// <param name="configure">크롤링 구성 액션</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder ConfigureCrawling(Action<CrawlingConfiguration> configure)
    {
        configure(_options.Configuration.Crawling);
        return this;
    }

    /// <summary>
    /// 청킹 설정
    /// </summary>
    /// <param name="configure">청킹 구성 액션</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder ConfigureChunking(Action<ChunkingConfiguration> configure)
    {
        configure(_options.Configuration.Chunking);
        return this;
    }

    /// <summary>
    /// 성능 설정
    /// </summary>
    /// <param name="configure">성능 구성 액션</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder ConfigurePerformance(Action<PerformanceConfiguration> configure)
    {
        configure(_options.Configuration.Performance);
        return this;
    }

    /// <summary>
    /// 구성에서 설정 로드
    /// </summary>
    /// <param name="configuration">구성 객체</param>
    /// <param name="sectionName">섹션 이름</param>
    /// <returns>빌더 인스턴스</returns>
    public WebFluxOptionsBuilder LoadFromConfiguration(IConfiguration configuration, string sectionName = WebFluxOptions.SectionName)
    {
        var section = configuration.GetSection(sectionName);
        section.Bind(_options);
        return this;
    }

    /// <summary>
    /// 옵션 빌드
    /// </summary>
    /// <returns>WebFlux 옵션</returns>
    public WebFluxOptions Build()
    {
        var validation = _options.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Invalid WebFlux configuration: {string.Join(", ", validation.Errors)}");
        }

        return _options;
    }
}