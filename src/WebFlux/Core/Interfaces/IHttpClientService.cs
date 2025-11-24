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
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> GetAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET 요청을 수행하고 문자열로 반환합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 내용</returns>
    Task<string> GetStringAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// GET 요청을 수행하고 바이트 배열로 반환합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>응답 바이트</returns>
    Task<byte[]> GetBytesAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// HEAD 요청을 수행합니다.
    /// </summary>
    /// <param name="url">요청 URL</param>
    /// <param name="headers">추가 헤더</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> HeadAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 사용자 에이전트를 설정합니다.
    /// </summary>
    /// <param name="userAgent">사용자 에이전트 문자열</param>
    void SetUserAgent(string userAgent);

    /// <summary>
    /// 타임아웃을 설정합니다.
    /// </summary>
    /// <param name="timeout">타임아웃 시간</param>
    void SetTimeout(TimeSpan timeout);

    /// <summary>
    /// 기본 헤더를 설정합니다.
    /// </summary>
    /// <param name="headers">설정할 헤더들</param>
    void SetDefaultHeaders(IDictionary<string, string> headers);
}

