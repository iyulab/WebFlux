namespace WebFlux.Core.Models;

/// <summary>
/// 처리 오류 정보를 나타내는 클래스
/// </summary>
public class ProcessingError
{
    /// <summary>
    /// 오류 코드
    /// </summary>
#if NET8_0_OR_GREATER
    public required string Code { get; init; }
#else
    public string Code { get; init; } = string.Empty;
#endif

    /// <summary>
    /// 오류 메시지
    /// </summary>
#if NET8_0_OR_GREATER
    public required string Message { get; init; }
#else
    public string Message { get; init; } = string.Empty;
#endif

    /// <summary>
    /// 오류 심각도
    /// </summary>
    public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;

    /// <summary>
    /// 오류 카테고리
    /// </summary>
    public ErrorCategory Category { get; init; } = ErrorCategory.General;

    /// <summary>
    /// 오류 발생 시간
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 내부 예외 정보
    /// </summary>
    public Exception? InnerException { get; init; }

    /// <summary>
    /// 오류 발생 위치 (메서드명, 파일명 등)
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// 스택 트레이스
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// 추가 오류 세부 정보
    /// </summary>
    public IReadOnlyDictionary<string, object> Details { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 재시도 가능 여부
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// 사용자에게 표시 가능한 오류 메시지
    /// </summary>
    public string? UserFriendlyMessage { get; init; }

    /// <summary>
    /// 관련 URL 또는 리소스
    /// </summary>
    public string? RelatedResource { get; init; }

    /// <summary>
    /// 해결 방법 제안
    /// </summary>
    public IReadOnlyList<string> SuggestedActions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 예외로부터 ProcessingError를 생성합니다.
    /// </summary>
    /// <param name="exception">예외</param>
    /// <param name="source">오류 발생 위치</param>
    /// <returns>ProcessingError 인스턴스</returns>
    public static ProcessingError FromException(Exception exception, string? source = null)
    {
        var category = DetermineCategory(exception);
        var severity = DetermineSeverity(exception);
        var isRetryable = DetermineRetryability(exception);

        return new ProcessingError
        {
            Code = GetErrorCode(exception),
            Message = exception.Message,
            Severity = severity,
            Category = category,
            Timestamp = DateTimeOffset.UtcNow,
            InnerException = exception,
            Source = source ?? exception.Source,
            StackTrace = exception.StackTrace,
            IsRetryable = isRetryable,
            UserFriendlyMessage = GetUserFriendlyMessage(exception),
            SuggestedActions = GetSuggestedActions(exception)
        };
    }

    /// <summary>
    /// 간단한 오류를 생성합니다.
    /// </summary>
    /// <param name="code">오류 코드</param>
    /// <param name="message">오류 메시지</param>
    /// <param name="severity">오류 심각도</param>
    /// <param name="category">오류 카테고리</param>
    /// <param name="isRetryable">재시도 가능 여부</param>
    /// <returns>ProcessingError 인스턴스</returns>
    public static ProcessingError Create(
        string code,
        string message,
        ErrorSeverity severity = ErrorSeverity.Error,
        ErrorCategory category = ErrorCategory.General,
        bool isRetryable = false)
    {
        return new ProcessingError
        {
            Code = code,
            Message = message,
            Severity = severity,
            Category = category,
            IsRetryable = isRetryable,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 네트워크 오류를 생성합니다.
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="url">관련 URL</param>
    /// <param name="statusCode">HTTP 상태 코드</param>
    /// <returns>ProcessingError 인스턴스</returns>
    public static ProcessingError NetworkError(string message, string? url = null, int? statusCode = null)
    {
        var details = new Dictionary<string, object>();
        if (statusCode.HasValue)
        {
            details["StatusCode"] = statusCode.Value;
        }

        return new ProcessingError
        {
            Code = "NETWORK_ERROR",
            Message = message,
            Severity = ErrorSeverity.Error,
            Category = ErrorCategory.Network,
            IsRetryable = true,
            RelatedResource = url,
            Details = details,
            UserFriendlyMessage = "네트워크 연결에 문제가 있습니다. 잠시 후 다시 시도해주세요.",
            SuggestedActions = new[] { "네트워크 연결 확인", "잠시 후 재시도", "방화벽 설정 확인" }
        };
    }

    /// <summary>
    /// 인증 오류를 생성합니다.
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="service">관련 서비스</param>
    /// <returns>ProcessingError 인스턴스</returns>
    public static ProcessingError AuthenticationError(string message, string? service = null)
    {
        return new ProcessingError
        {
            Code = "AUTH_ERROR",
            Message = message,
            Severity = ErrorSeverity.Error,
            Category = ErrorCategory.Authentication,
            IsRetryable = false,
            RelatedResource = service,
            UserFriendlyMessage = "인증에 실패했습니다. API 키 또는 인증 정보를 확인해주세요.",
            SuggestedActions = new[] { "API 키 확인", "인증 정보 재설정", "서비스 상태 확인" }
        };
    }

    /// <summary>
    /// 검증 오류를 생성합니다.
    /// </summary>
    /// <param name="message">오류 메시지</param>
    /// <param name="fieldName">검증 실패 필드명</param>
    /// <returns>ProcessingError 인스턴스</returns>
    public static ProcessingError ValidationError(string message, string? fieldName = null)
    {
        var details = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(fieldName))
        {
            details["Field"] = fieldName;
        }

        return new ProcessingError
        {
            Code = "VALIDATION_ERROR",
            Message = message,
            Severity = ErrorSeverity.Warning,
            Category = ErrorCategory.Validation,
            IsRetryable = false,
            Details = details,
            UserFriendlyMessage = "입력값에 문제가 있습니다. 입력 내용을 확인해주세요.",
            SuggestedActions = new[] { "입력값 확인", "형식 검토", "필수 항목 확인" }
        };
    }

    private static ErrorCategory DetermineCategory(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => ErrorCategory.Network,
            TimeoutException => ErrorCategory.Timeout,
            UnauthorizedAccessException => ErrorCategory.Authentication,
            ArgumentException => ErrorCategory.Validation,
            InvalidOperationException => ErrorCategory.Configuration,
            NotSupportedException => ErrorCategory.NotSupported,
            OutOfMemoryException => ErrorCategory.Resource,
            _ => ErrorCategory.General
        };
    }

    private static ErrorSeverity DetermineSeverity(Exception exception)
    {
        return exception switch
        {
            ArgumentException => ErrorSeverity.Warning,
            TimeoutException => ErrorSeverity.Warning,
            OutOfMemoryException => ErrorSeverity.Critical,
            _ => ErrorSeverity.Error
        };
    }

    private static bool DetermineRetryability(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TimeoutException => true,
            OutOfMemoryException => false,
            UnauthorizedAccessException => false,
            ArgumentException => false,
            _ => true
        };
    }

    private static string GetErrorCode(Exception exception)
    {
        return exception.GetType().Name.Replace("Exception", "").ToUpperInvariant();
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "네트워크 연결에 문제가 있습니다.",
            TimeoutException => "요청 시간이 초과되었습니다.",
            UnauthorizedAccessException => "접근 권한이 없습니다.",
            ArgumentException => "입력값이 올바르지 않습니다.",
            OutOfMemoryException => "메모리가 부족합니다.",
            _ => "처리 중 오류가 발생했습니다."
        };
    }

    private static IReadOnlyList<string> GetSuggestedActions(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => new[] { "네트워크 연결 확인", "잠시 후 재시도" },
            TimeoutException => new[] { "타임아웃 시간 증가", "네트워크 상태 확인" },
            UnauthorizedAccessException => new[] { "인증 정보 확인", "권한 설정 검토" },
            ArgumentException => new[] { "입력값 검증", "형식 확인" },
            OutOfMemoryException => new[] { "메모리 사용량 최적화", "처리 단위 축소" },
            _ => new[] { "로그 확인", "잠시 후 재시도" }
        };
    }

    /// <summary>
    /// 오류 정보를 문자열로 변환합니다.
    /// </summary>
    /// <returns>오류 정보 문자열</returns>
    public override string ToString()
    {
        return $"[{Severity}] {Code}: {Message}";
    }
}


/// <summary>
/// 오류 카테고리 열거형
/// </summary>
public enum ErrorCategory
{
    /// <summary>일반</summary>
    General,
    /// <summary>네트워크</summary>
    Network,
    /// <summary>인증</summary>
    Authentication,
    /// <summary>권한</summary>
    Authorization,
    /// <summary>검증</summary>
    Validation,
    /// <summary>구성</summary>
    Configuration,
    /// <summary>타임아웃</summary>
    Timeout,
    /// <summary>리소스</summary>
    Resource,
    /// <summary>지원하지 않음</summary>
    NotSupported,
    /// <summary>비즈니스 로직</summary>
    Business,
    /// <summary>외부 서비스</summary>
    ExternalService
}

/// <summary>
/// 오류 심각도 열거형
/// </summary>
public enum ErrorSeverity
{
    /// <summary>정보</summary>
    Info,
    /// <summary>경고</summary>
    Warning,
    /// <summary>오류</summary>
    Error,
    /// <summary>치명적 오류</summary>
    Critical
}