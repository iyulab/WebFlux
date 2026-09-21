using WebFlux.Core.Interfaces;
using WebFlux.Core.Utilities;

namespace WebFlux.Services;

/// <summary>
/// HTTP 클라이언트 서비스 구현체
/// WebFlux에 최적화된 HTTP 요청 처리
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _defaultHeaders = new();
    private long _defaultTimeoutTicks = TimeSpan.FromSeconds(30).Ticks;

    public HttpClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // The fallback identity, for requests that do not bring their own User-Agent. Set only when
        // the HttpClient arrives without one: adding a second product token is how a crawler ends up
        // announcing itself as two bots.
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", WebFluxUserAgent.Default);

        // Timeouts are per request (see SendAsync). HttpClient.Timeout is one value for every
        // concurrent caller and cannot change after the first request, so it cannot carry a
        // per-crawl option; left in place it would also silently cap any longer request timeout.
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// GET 요청을 수행합니다.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 추가 헤더 설정
        AddHeaders(request, headers);

        return await SendAsync(request, timeout, cancellationToken);
    }

    /// <summary>
    /// GET 요청을 수행하고 문자열로 반환합니다.
    /// </summary>
    public async Task<string> GetStringAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(url, headers, timeout, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// GET 요청을 수행하고 바이트 배열로 반환합니다.
    /// </summary>
    public async Task<byte[]> GetBytesAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(url, headers, timeout, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// HEAD 요청을 수행합니다.
    /// </summary>
    public async Task<HttpResponseMessage> HeadAsync(
        string url,
        IDictionary<string, string>? headers = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        AddHeaders(request, headers);
        return await SendAsync(request, timeout, cancellationToken);
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
    /// 요청이 자기 타임아웃을 주지 않았을 때 쓰는 기본값을 설정합니다 (상한이 아니다).
    /// </summary>
    public void SetTimeout(TimeSpan timeout)
    {
        ValidateTimeout(timeout, nameof(timeout));
        Interlocked.Exchange(ref _defaultTimeoutTicks, timeout.Ticks);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        var effective = timeout ?? TimeSpan.FromTicks(Interlocked.Read(ref _defaultTimeoutTicks));
        ValidateTimeout(effective, nameof(timeout));

        if (effective == Timeout.InfiniteTimeSpan)
            return await _httpClient.SendAsync(request, cancellationToken);

        // The default completion option buffers the body inside SendAsync, so this bounds the
        // whole response, not just the headers.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(effective);

        try
        {
            return await _httpClient.SendAsync(request, timeoutSource.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The request to {request.RequestUri} did not complete within {effective.TotalMilliseconds:0} ms.", ex);
        }
    }

    private static void ValidateTimeout(TimeSpan timeout, string paramName)
    {
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(paramName, timeout, "Timeout must be positive or Timeout.InfiniteTimeSpan.");
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
                // A per-request value replaces the default of the same name rather than joining it.
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }
}