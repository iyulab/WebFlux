namespace WebFlux.Core.Models;

/// <summary>
/// 배치 추출 결과
/// 성공/실패 분리, 부분 실패 허용
/// </summary>
public class BatchExtractResult
{
    /// <summary>
    /// 성공한 추출 결과 목록
    /// </summary>
    public IReadOnlyList<ExtractedContent> Succeeded { get; init; } = Array.Empty<ExtractedContent>();

    /// <summary>
    /// 실패한 추출 목록
    /// </summary>
    public IReadOnlyList<FailedExtraction> Failed { get; init; } = Array.Empty<FailedExtraction>();

    /// <summary>
    /// 총 처리 시간 (밀리초)
    /// </summary>
    public long TotalDurationMs { get; init; }

    /// <summary>
    /// 처리된 총 URL 수
    /// </summary>
    public int TotalCount => Succeeded.Count + Failed.Count;

    /// <summary>
    /// 성공률 (0.0 - 1.0)
    /// </summary>
    public double SuccessRate => TotalCount > 0 ? (double)Succeeded.Count / TotalCount : 0;

    /// <summary>
    /// 배치 통계
    /// </summary>
    public BatchStatistics Statistics { get; init; } = new();

    /// <summary>
    /// 처리 시작 시간
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// 처리 완료 시간
    /// </summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// 빈 결과 생성
    /// </summary>
    public static BatchExtractResult Empty => new()
    {
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// 결과 요약 문자열
    /// </summary>
    public override string ToString()
    {
        return $"BatchExtractResult: {Succeeded.Count}/{TotalCount} succeeded ({SuccessRate:P0}), {TotalDurationMs}ms";
    }
}

/// <summary>
/// 실패한 추출 정보
/// </summary>
public class FailedExtraction
{
    /// <summary>
    /// 실패한 URL
    /// </summary>
#if NET8_0_OR_GREATER
    public required string Url { get; init; }
#else
    public string Url { get; init; } = string.Empty;
#endif

    /// <summary>
    /// 오류 코드
    /// </summary>
    public string ErrorCode { get; init; } = "Unknown";

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// HTTP 상태 코드 (해당하는 경우)
    /// </summary>
    public int? HttpStatusCode { get; init; }

    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// 처리 시간 (밀리초)
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// 실패 시점
    /// </summary>
    public DateTimeOffset FailedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 예외 정보 (디버깅용)
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 결과 문자열
    /// </summary>
    public override string ToString()
    {
        var statusPart = HttpStatusCode.HasValue ? $" (HTTP {HttpStatusCode})" : "";
        return $"FailedExtraction: {Url} - {ErrorCode}{statusPart}: {ErrorMessage}";
    }
}

/// <summary>
/// 배치 처리 통계
/// </summary>
public class BatchStatistics
{
    /// <summary>
    /// 평균 처리 시간 (밀리초)
    /// </summary>
    public double AverageProcessingTimeMs { get; init; }

    /// <summary>
    /// 총 추출된 문자 수
    /// </summary>
    public long TotalCharactersExtracted { get; init; }

    /// <summary>
    /// 캐시 히트율 (0.0 - 1.0)
    /// </summary>
    public double CacheHitRate { get; init; }

    /// <summary>
    /// 캐시에서 로드된 URL 수
    /// </summary>
    public int CacheHits { get; init; }

    /// <summary>
    /// 네트워크에서 가져온 URL 수
    /// </summary>
    public int CacheMisses { get; init; }

    /// <summary>
    /// 도메인별 처리 수
    /// </summary>
    public IReadOnlyDictionary<string, int> ProcessedByDomain { get; init; } =
        new Dictionary<string, int>();

    /// <summary>
    /// 오류 코드별 실패 수
    /// </summary>
    public IReadOnlyDictionary<string, int> FailuresByErrorCode { get; init; } =
        new Dictionary<string, int>();

    /// <summary>
    /// 최소 처리 시간 (밀리초)
    /// </summary>
    public long MinProcessingTimeMs { get; init; }

    /// <summary>
    /// 최대 처리 시간 (밀리초)
    /// </summary>
    public long MaxProcessingTimeMs { get; init; }

    /// <summary>
    /// 동적 렌더링 사용 수
    /// </summary>
    public int DynamicRenderingCount { get; init; }

    /// <summary>
    /// 정적 렌더링 사용 수
    /// </summary>
    public int StaticRenderingCount { get; init; }
}

/// <summary>
/// 추출 오류 코드 상수
/// </summary>
public static class ExtractErrorCodes
{
    /// <summary>요청 타임아웃</summary>
    public const string Timeout = "Timeout";

    /// <summary>페이지를 찾을 수 없음 (404)</summary>
    public const string NotFound = "NotFound";

    /// <summary>접근 차단됨 (403, 429 등)</summary>
    public const string Blocked = "Blocked";

    /// <summary>콘텐츠 파싱 오류</summary>
    public const string ParseError = "ParseError";

    /// <summary>네트워크 연결 오류</summary>
    public const string NetworkError = "NetworkError";

    /// <summary>서버 오류 (5xx)</summary>
    public const string ServerError = "ServerError";

    /// <summary>콘텐츠 없음</summary>
    public const string EmptyContent = "EmptyContent";

    /// <summary>잘못된 URL</summary>
    public const string InvalidUrl = "InvalidUrl";

    /// <summary>지원하지 않는 콘텐츠 타입</summary>
    public const string UnsupportedContentType = "UnsupportedContentType";

    /// <summary>SSL/TLS 오류</summary>
    public const string SslError = "SslError";

    /// <summary>너무 많은 리다이렉트</summary>
    public const string TooManyRedirects = "TooManyRedirects";

    /// <summary>알 수 없는 오류</summary>
    public const string Unknown = "Unknown";

    /// <summary>
    /// HTTP 상태 코드로부터 오류 코드 결정
    /// </summary>
    public static string FromHttpStatusCode(int statusCode)
    {
        return statusCode switch
        {
            404 => NotFound,
            403 or 429 or 401 => Blocked,
            >= 500 and < 600 => ServerError,
            >= 400 and < 500 => Unknown,
            _ => Unknown
        };
    }
}
