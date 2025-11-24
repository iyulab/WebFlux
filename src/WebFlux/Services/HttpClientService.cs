using WebFlux.Core.Interfaces;

namespace WebFlux.Services;

/// <summary>
/// HTTP 클라이언트 서비스 구현체
/// WebFlux에 최적화된 HTTP 요청 처리
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _defaultHeaders = new();

    public HttpClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // 기본 설정
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "WebFlux-SDK/1.0 (+https://github.com/webflux/webflux)");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// GET 요청을 수행합니다.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 추가 헤더 설정
        AddHeaders(request, headers);

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// GET 요청을 수행하고 문자열로 반환합니다.
    /// </summary>
    public async Task<string> GetStringAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(url, headers, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// GET 요청을 수행하고 바이트 배열로 반환합니다.
    /// </summary>
    public async Task<byte[]> GetBytesAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(url, headers, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// HEAD 요청을 수행합니다.
    /// </summary>
    public async Task<HttpResponseMessage> HeadAsync(
        string url,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        AddHeaders(request, headers);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// 사용자 에이전트를 설정합니다.
    /// </summary>
    public void SetUserAgent(string userAgent)
    {
        _httpClient.DefaultRequestHeaders.Remove("User-Agent");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
    }

    /// <summary>
    /// 타임아웃을 설정합니다.
    /// </summary>
    public void SetTimeout(TimeSpan timeout)
    {
        _httpClient.Timeout = timeout;
    }

    /// <summary>
    /// 기본 헤더를 설정합니다.
    /// </summary>
    public void SetDefaultHeaders(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            _defaultHeaders[header.Key] = header.Value;
            _httpClient.DefaultRequestHeaders.Remove(header.Key);
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// 요청에 헤더 추가
    /// </summary>
    private void AddHeaders(HttpRequestMessage request, IDictionary<string, string>? headers)
    {
        // 기본 헤더 추가
        foreach (var header in _defaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 추가 헤더
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}