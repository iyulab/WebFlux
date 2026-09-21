namespace WebFlux.Core.Interfaces;

/// <summary>
/// HTTP 클라이언트 서비스 인터페이스
/// WebFlux에 최적화된 HTTP 요청 처리
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// GET 요청을 수행합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="timeout">이 요청 하나의 타임아웃(응답 본문 수신까지). <c>null</c> 이면 서비스 기본값(<see cref="SetTimeout"/>, 처음엔 30초)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> GetAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET 요청을 수행하고 문자열로 반환합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="timeout">이 요청 하나의 타임아웃(응답 본문 수신까지). <c>null</c> 이면 서비스 기본값(<see cref="SetTimeout"/>, 처음엔 30초)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 내용</returns>
    Task<string> GetStringAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET 요청을 수행하고 바이트 배열로 반환합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="timeout">이 요청 하나의 타임아웃(응답 본문 수신까지). <c>null</c> 이면 서비스 기본값(<see cref="SetTimeout"/>, 처음엔 30초)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 바이트</returns>
    Task<byte[]> GetBytesAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HEAD 요청을 수행합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="timeout">이 요청 하나의 타임아웃(응답 본문 수신까지). <c>null</c> 이면 서비스 기본값(<see cref="SetTimeout"/>, 처음엔 30초)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> HeadAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 사용자 에이전트를 설정합니다.
    /// </summary>
    /// <param name="userAgent">사용자 에이전트 문자열</param>
    void SetUserAgent(string userAgent);

    /// <summary>
    /// 요청이 자기 <c>timeout</c> 을 주지 않았을 때 쓰는 기본 타임아웃을 설정합니다.
    /// </summary>
    /// <remarks>
    /// 상한이 아니라 <b>기본값</b>이다 — 요청이 명시한 타임아웃은 이 값보다 길어도 그대로 지켜진다.
    /// 타임아웃은 요청마다 적용되므로 첫 요청을 보낸 뒤에도 바꿀 수 있고, 동시에 도는 다른 요청에
    /// 영향을 주지 않는다. 시간이 다 되면 <see cref="TimeoutException"/> 을 던진다(호출자의 취소는
    /// <see cref="OperationCanceledException"/> 그대로).
    /// </remarks>
    /// <param name="timeout">타임아웃 시간</param>
    void SetTimeout(TimeSpan timeout);

    /// <summary>
    /// 기본 헤더를 설정합니다.
    /// </summary>
    /// <param name="headers">설정할 헤더들</param>
    void SetDefaultHeaders(IDictionary<string, string> headers);
}

