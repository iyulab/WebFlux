namespace WebFlux.Core.Models;

/// <summary>
/// 처리 결과를 래핑하는 제네릭 클래스
/// 성공/실패 상태와 오류 정보를 포함
/// </summary>
/// <typeparam name="T">결과 데이터 타입</typeparam>
public class ProcessingResult<T>
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 결과 데이터 (성공한 경우)
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// 오류 정보 (실패한 경우)
    /// </summary>
    public ProcessingError? Error { get; init; }

    /// <summary>
    /// 경고 메시지 목록
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 처리 시간 (밀리초)
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// 처리 시작 시간
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// 처리 완료 시간
    /// </summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// 추가 메타데이터
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <param name="data">결과 데이터</param>
    /// <param name="warnings">경고 메시지 목록</param>
    /// <param name="processingTimeMs">처리 시간</param>
    /// <param name="metadata">추가 메타데이터</param>
    /// <returns>성공 결과</returns>
    public static ProcessingResult<T> Success(
        T data,
        IReadOnlyList<string>? warnings = null,
        long processingTimeMs = 0,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProcessingResult<T>
        {
            IsSuccess = true,
            Data = data,
            Warnings = warnings ?? Array.Empty<string>(),
            ProcessingTimeMs = processingTimeMs,
            StartTime = now.AddMilliseconds(-processingTimeMs),
            EndTime = now,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="error">오류 정보</param>
    /// <param name="processingTimeMs">처리 시간</param>
    /// <param name="metadata">추가 메타데이터</param>
    /// <returns>실패 결과</returns>
    public static ProcessingResult<T> Failure(
        ProcessingError error,
        long processingTimeMs = 0,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProcessingResult<T>
        {
            IsSuccess = false,
            Error = error,
            ProcessingTimeMs = processingTimeMs,
            StartTime = now.AddMilliseconds(-processingTimeMs),
            EndTime = now,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// 예외로부터 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="exception">예외</param>
    /// <param name="processingTimeMs">처리 시간</param>
    /// <param name="metadata">추가 메타데이터</param>
    /// <returns>실패 결과</returns>
    public static ProcessingResult<T> FromException(
        Exception exception,
        long processingTimeMs = 0,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var error = ProcessingError.FromException(exception);
        return Failure(error, processingTimeMs, metadata);
    }

    /// <summary>
    /// 오류 메시지로부터 실패 결과를 생성합니다.
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <param name="processingTimeMs">처리 시간</param>
    /// <param name="metadata">추가 메타데이터</param>
    /// <returns>실패 결과</returns>
    public static ProcessingResult<T> FromError(
        string errorMessage,
        string? errorCode = null,
        long processingTimeMs = 0,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        var error = new ProcessingError
        {
            Message = errorMessage,
            Code = errorCode ?? "UNKNOWN_ERROR",
            Severity = ErrorSeverity.Error,
            Timestamp = DateTimeOffset.UtcNow
        };
        return Failure(error, processingTimeMs, metadata);
    }

    /// <summary>
    /// 다른 타입의 ProcessingResult로 변환합니다.
    /// </summary>
    /// <typeparam name="TNew">새로운 결과 타입</typeparam>
    /// <param name="mapper">데이터 변환 함수</param>
    /// <returns>변환된 결과</returns>
    public ProcessingResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!IsSuccess || Data == null)
        {
            return new ProcessingResult<TNew>
            {
                IsSuccess = false,
                Error = Error,
                Warnings = Warnings,
                ProcessingTimeMs = ProcessingTimeMs,
                StartTime = StartTime,
                EndTime = EndTime,
                Metadata = Metadata
            };
        }

        try
        {
            var mappedData = mapper(Data);
            return new ProcessingResult<TNew>
            {
                IsSuccess = true,
                Data = mappedData,
                Warnings = Warnings,
                ProcessingTimeMs = ProcessingTimeMs,
                StartTime = StartTime,
                EndTime = EndTime,
                Metadata = Metadata
            };
        }
        catch (Exception ex)
        {
            return ProcessingResult<TNew>.FromException(ex, ProcessingTimeMs, Metadata);
        }
    }

    /// <summary>
    /// 결과가 성공인지 확인하고 데이터를 반환합니다.
    /// </summary>
    /// <param name="data">결과 데이터</param>
    /// <returns>성공 여부</returns>
    public bool TryGetData(out T? data)
    {
        data = IsSuccess ? Data : default;
        return IsSuccess;
    }

    /// <summary>
    /// 결과 요약 문자열을 반환합니다.
    /// </summary>
    /// <returns>결과 요약</returns>
    public override string ToString()
    {
        if (IsSuccess)
        {
            var warningCount = Warnings.Count;
            var warningText = warningCount > 0 ? $" ({warningCount} warnings)" : "";
            return $"Success: {typeof(T).Name} in {ProcessingTimeMs}ms{warningText}";
        }
        else
        {
            return $"Failed: {Error?.Code ?? "UNKNOWN"} - {Error?.Message ?? "Unknown error"}";
        }
    }
}

/// <summary>
/// 비제네릭 ProcessingResult 유틸리티 클래스
/// </summary>
public static class ProcessingResult
{
    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <returns>성공 결과</returns>
    public static ProcessingResult<object> Success()
    {
        return ProcessingResult<object>.Success(new object());
    }

    /// <summary>
    /// 성공 결과를 생성합니다.
    /// </summary>
    /// <typeparam name="T">결과 타입</typeparam>
    /// <param name="data">결과 데이터</param>
    /// <param name="warnings">경고 메시지</param>
    /// <returns>성공 결과</returns>
    public static ProcessingResult<T> Success<T>(T data, params string[] warnings)
    {
        return ProcessingResult<T>.Success(data, warnings, 0);
    }

    /// <summary>
    /// 실패 결과를 생성합니다.
    /// </summary>
    /// <typeparam name="T">결과 타입</typeparam>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <returns>실패 결과</returns>
    public static ProcessingResult<T> Failure<T>(string errorMessage, string? errorCode = null)
    {
        return ProcessingResult<T>.FromError(errorMessage, errorCode);
    }

    /// <summary>
    /// 예외로부터 실패 결과를 생성합니다.
    /// </summary>
    /// <typeparam name="T">결과 타입</typeparam>
    /// <param name="exception">예외</param>
    /// <returns>실패 결과</returns>
    public static ProcessingResult<T> FromException<T>(Exception exception)
    {
        return ProcessingResult<T>.FromException(exception);
    }
}