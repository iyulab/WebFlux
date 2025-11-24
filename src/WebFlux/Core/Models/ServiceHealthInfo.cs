namespace WebFlux.Core.Models;

/// <summary>
/// 서비스 상태 정보를 나타내는 클래스
/// </summary>
public class ServiceHealthInfo
{
    /// <summary>
    /// 서비스 이름
    /// </summary>
#if NET8_0_OR_GREATER
    public required string ServiceName { get; init; }
#else
    public string ServiceName { get; init; } = string.Empty;
#endif

    /// <summary>
    /// 서비스 상태
    /// </summary>
    public ServiceStatus Status { get; init; }

    /// <summary>
    /// 응답 시간 (밀리초)
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// 사용 가능한 모델 목록
    /// </summary>
    public IReadOnlyList<string> AvailableModels { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 현재 사용률 (0.0 - 1.0)
    /// </summary>
    public double? UsagePercentage { get; init; }

    /// <summary>
    /// 남은 토큰 할당량
    /// </summary>
    public long? RemainingTokens { get; init; }

    /// <summary>
    /// 마지막 확인 시간
    /// </summary>
    public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 오류 메시지 (상태가 Unhealthy인 경우)
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 서비스 상태 열거형
/// </summary>
public enum ServiceStatus
{
    /// <summary>정상</summary>
    Healthy,
    /// <summary>경고</summary>
    Warning,
    /// <summary>비정상</summary>
    Unhealthy,
    /// <summary>알 수 없음</summary>
    Unknown
}