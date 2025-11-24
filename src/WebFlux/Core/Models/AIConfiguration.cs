namespace WebFlux.Core.Models;

/// <summary>
/// AI 서비스 구성
/// </summary>
public class AIConfiguration
{
    /// <summary>
    /// OpenAI API 키
    /// </summary>
    public string OpenAIApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI 조직 ID
    /// </summary>
    public string? OpenAIOrganizationId { get; set; }

    /// <summary>
    /// OpenAI 기본 URL (사용자 정의 엔드포인트)
    /// </summary>
    public string OpenAIBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// 텍스트 임베딩 모델
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// 텍스트 완성 모델
    /// </summary>
    public string CompletionModel { get; set; } = "gpt-5-nano";

    /// <summary>
    /// 멀티모달 모델 (이미지 처리용)
    /// </summary>
    public string MultimodalModel { get; set; } = "gpt-4-vision-preview";

    /// <summary>
    /// 최대 토큰 수
    /// </summary>
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// 온도 설정 (0-2, 창의성 조절)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top-p 설정 (0-1, 다양성 조절)
    /// </summary>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// 요청 타임아웃 (초)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 재시도 지연 시간 (밀리초)
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// 속도 제한 (분당 요청 수)
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// 배치 처리 크기
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// 캐싱 활성화
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// 캐시 TTL (분)
    /// </summary>
    public int CacheTTLMinutes { get; set; } = 60;

    /// <summary>
    /// 로깅 레벨
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// 요청/응답 로깅 활성화
    /// </summary>
    public bool EnableRequestResponseLogging { get; set; } = false;

    /// <summary>
    /// 비용 추적 활성화
    /// </summary>
    public bool EnableCostTracking { get; set; } = true;

    /// <summary>
    /// 일일 비용 한도 (USD)
    /// </summary>
    public decimal DailyCostLimit { get; set; } = 100m;

    /// <summary>
    /// 월간 비용 한도 (USD)
    /// </summary>
    public decimal MonthlyCostLimit { get; set; } = 1000m;

    /// <summary>
    /// 프록시 설정
    /// </summary>
    public ProxyConfiguration? Proxy { get; set; }

    /// <summary>
    /// 사용자 정의 헤더
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// 환경 설정 (개발/운영)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// 구성 유효성 검사
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(OpenAIApiKey) &&
               !string.IsNullOrWhiteSpace(EmbeddingModel) &&
               !string.IsNullOrWhiteSpace(CompletionModel) &&
               MaxTokens > 0 &&
               Temperature >= 0 && Temperature <= 2 &&
               TopP >= 0 && TopP <= 1 &&
               RequestTimeoutSeconds > 0 &&
               MaxRetries >= 0 &&
               RateLimitPerMinute > 0;
    }
}

/// <summary>
/// 프록시 구성
/// </summary>
public class ProxyConfiguration
{
    /// <summary>
    /// 프록시 서버 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 사용자명
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 비밀번호
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 프록시 무시할 도메인 목록
    /// </summary>
    public List<string> BypassList { get; set; } = new();
}