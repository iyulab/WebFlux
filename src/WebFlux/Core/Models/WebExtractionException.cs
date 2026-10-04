namespace WebFlux.Core.Models;

/// <summary>
/// 단일 URL 추출이 실패했을 때 던져지는 예외입니다. 실패 종류는 <see cref="ErrorCode"/>(<see cref="ExtractErrorCodes"/>의
/// 상수)로 구별합니다 — 예: robots.txt 거부는 <see cref="ExtractErrorCodes.DisallowedByRobotsTxt"/>.
/// </summary>
/// <remarks>
/// 호출자가 취소한 경우에는 이 예외가 아니라 <see cref="OperationCanceledException"/>이 전파됩니다.
/// 배치 추출(<c>ExtractBatchAsync</c> · <c>ExtractBatchStreamAsync</c>)은 이 예외를 URL별 <see cref="FailedExtraction"/>으로 담습니다.
/// </remarks>
public sealed class WebExtractionException : Exception
{
    /// <summary>실패 정보로 예외를 만듭니다.</summary>
    /// <param name="url">추출하려던 URL.</param>
    /// <param name="errorCode"><see cref="ExtractErrorCodes"/>의 상수.</param>
    /// <param name="message">실패 설명.</param>
    /// <param name="httpStatusCode">HTTP 응답 상태 코드(있을 때).</param>
    /// <param name="innerException">원인 예외(있을 때).</param>
    public WebExtractionException(string url, string errorCode, string message, int? httpStatusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Url = url;
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>추출하려던 URL.</summary>
    public string Url { get; }

    /// <summary>실패 종류 — <see cref="ExtractErrorCodes"/>의 상수.</summary>
    public string ErrorCode { get; }

    /// <summary>HTTP 응답 상태 코드(해당할 때).</summary>
    public int? HttpStatusCode { get; }
}
