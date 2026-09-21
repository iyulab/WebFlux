using System.Net;
using WebFlux.Services;
using Xunit;
using AwesomeAssertions;

namespace WebFlux.Tests.Services;

/// <summary>
/// HttpClientService 단위 테스트
/// HTTP 요청/응답 처리 및 설정 관리 검증
/// </summary>
public class HttpClientServiceTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly MockHttpMessageHandler _mockHandler;
    private HttpClientService _service;

    public HttpClientServiceTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        _service = new HttpClientService(_httpClient);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidHttpClient_ShouldNotThrow()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act & Assert
        var service = new HttpClientService(httpClient);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HttpClientService(null!));
    }

    [Fact]
    public void Constructor_ShouldSetDefaultUserAgent()
    {
        // Arrange & Act
        using var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);

        // Assert
        httpClient.DefaultRequestHeaders.UserAgent.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("WebFlux-SDK");
    }

    [Fact]
    public void Constructor_LeavesTimeoutsToEachRequest_NotToTheSharedHttpClient()
    {
        // Arrange & Act
        using var httpClient = new HttpClient();
        var service = new HttpClientService(httpClient);

        // Assert: HttpClient.Timeout is one value for every concurrent caller and would cap any
        // longer per-request timeout. The per-request behaviour itself is pinned against a real
        // socket in RequestTimeoutTests.
        httpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithValidUrl_ShouldReturnResponse()
    {
        // Arrange
        var url = "https://example.com";
        _mockHandler.SetupResponse(HttpStatusCode.OK, "Test content");

        // Act
        var response = await _service.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Be("Test content");
    }

    [Fact]
    public async Task GetAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var url = "https://example.com";
        var headers = new Dictionary<string, string>
        {
            { "X-Custom-Header", "CustomValue" },
            { "Authorization", "Bearer token123" }
        };
        _mockHandler.SetupResponse(HttpStatusCode.OK, "Success");

        // Act
        var response = await _service.GetAsync(url, headers, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        _mockHandler.LastRequest.Should().NotBeNull();
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "X-Custom-Header");
        _mockHandler.LastRequest.Headers.Should().Contain(h => h.Key == "Authorization");
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var url = "https://example.com";
        var cts = new CancellationTokenSource();
        _mockHandler.SetupDelayedResponse(HttpStatusCode.OK, "Content", TimeSpan.FromSeconds(5));

        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _service.GetAsync(url, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GetAsync_WithNotFoundUrl_ShouldReturnNotFoundResponse()
    {
        // Arrange
        var url = "https://example.com/notfound";
        _mockHandler.SetupResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var response = await _service.GetAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetStringAsync Tests

    [Fact]
    public async Task GetStringAsync_WithValidUrl_ShouldReturnContent()
    {
        // Arrange
        var url = "https://example.com";
        var expectedContent = "Test HTML content";
        _mockHandler.SetupResponse(HttpStatusCode.OK, expectedContent);

        // Act
        var content = await _service.GetStringAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        content.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetStringAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var url = "https://example.com";
        var headers = new Dictionary<string, string> { { "Accept", "text/html" } };
        _mockHandler.SetupResponse(HttpStatusCode.OK, "HTML");

        // Act
        var content = await _service.GetStringAsync(url, headers, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        content.Should().Be("HTML");
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "Accept");
    }

    [Fact]
    public async Task GetStringAsync_WithErrorResponse_ShouldThrowHttpRequestException()
    {
        // Arrange
        var url = "https://example.com";
        _mockHandler.SetupResponse(HttpStatusCode.InternalServerError, "Error");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _service.GetStringAsync(url, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetStringAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var url = "https://example.com";
        var cts = new CancellationTokenSource();
        _mockHandler.SetupDelayedResponse(HttpStatusCode.OK, "Content", TimeSpan.FromSeconds(5));

        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _service.GetStringAsync(url, cancellationToken: cts.Token));
    }

    #endregion

    #region GetBytesAsync Tests

    [Fact]
    public async Task GetBytesAsync_WithValidUrl_ShouldReturnBytes()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        var expectedBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        _mockHandler.SetupBytesResponse(HttpStatusCode.OK, expectedBytes);

        // Act
        var bytes = await _service.GetBytesAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        bytes.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async Task GetBytesAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var url = "https://example.com/file.pdf";
        var headers = new Dictionary<string, string> { { "Accept", "application/pdf" } };
        _mockHandler.SetupBytesResponse(HttpStatusCode.OK, new byte[] { 0x25, 0x50, 0x44, 0x46 });

        // Act
        var bytes = await _service.GetBytesAsync(url, headers, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        bytes.Should().NotBeNull();
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "Accept");
    }

    [Fact]
    public async Task GetBytesAsync_WithErrorResponse_ShouldThrowHttpRequestException()
    {
        // Arrange
        var url = "https://example.com/image.jpg";
        _mockHandler.SetupResponse(HttpStatusCode.Forbidden, "Access Denied");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await _service.GetBytesAsync(url, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetBytesAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var url = "https://example.com/large-file.zip";
        var cts = new CancellationTokenSource();
        _mockHandler.SetupDelayedResponse(HttpStatusCode.OK, "Content", TimeSpan.FromSeconds(5));

        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _service.GetBytesAsync(url, cancellationToken: cts.Token));
    }

    #endregion

    #region HeadAsync Tests

    [Fact]
    public async Task HeadAsync_WithValidUrl_ShouldReturnResponse()
    {
        // Arrange
        var url = "https://example.com";
        _mockHandler.SetupResponse(HttpStatusCode.OK, "");

        // Act
        var response = await _service.HeadAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Head);
    }

    [Fact]
    public async Task HeadAsync_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var url = "https://example.com";
        var headers = new Dictionary<string, string> { { "X-Check-Exists", "true" } };
        _mockHandler.SetupResponse(HttpStatusCode.OK, "");

        // Act
        var response = await _service.HeadAsync(url, headers, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "X-Check-Exists");
    }

    [Fact]
    public async Task HeadAsync_WithNotFoundUrl_ShouldReturnNotFoundResponse()
    {
        // Arrange
        var url = "https://example.com/missing";
        _mockHandler.SetupResponse(HttpStatusCode.NotFound, "");

        // Act
        var response = await _service.HeadAsync(url, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region SetUserAgent Tests

    [Fact]
    public void SetUserAgent_WithCustomAgent_ShouldUpdateUserAgent()
    {
        // Arrange
        var customAgent = "CustomBot/2.0";

        // Act
        _service.SetUserAgent(customAgent);

        // Assert
        _httpClient.DefaultRequestHeaders.UserAgent.ToString().Should().Contain("CustomBot/2.0");
    }

    [Fact]
    public async Task SetUserAgent_ShouldReflectInRequests()
    {
        // Arrange
        var customAgent = "TestAgent/1.0";
        _mockHandler.SetupResponse(HttpStatusCode.OK, "Success");
        _service.SetUserAgent(customAgent);

        // Act
        await _service.GetAsync("https://example.com", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockHandler.LastRequest!.Headers.UserAgent.ToString().Should().Contain("TestAgent/1.0");
    }

    #endregion

    #region SetTimeout Tests

    [Fact]
    public void SetTimeout_ChangesTheDefault_WithoutTouchingTheSharedHttpClient()
    {
        // Act
        _service.SetTimeout(TimeSpan.FromSeconds(60));

        // Assert: the default lives in the service (applied per request), so it can change after
        // the first request has been sent; HttpClient.Timeout cannot.
        _httpClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public void SetTimeout_AcceptsInfinite_AndRejectsZeroOrNegative()
    {
        var infinite = () => _service.SetTimeout(Timeout.InfiniteTimeSpan);
        var zero = () => _service.SetTimeout(TimeSpan.Zero);
        var negative = () => _service.SetTimeout(TimeSpan.FromSeconds(-5));

        infinite.Should().NotThrow();
        zero.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region SetDefaultHeaders Tests

    [Fact]
    public void SetDefaultHeaders_WithSingleHeader_ShouldAddHeader()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "X-API-Key", "secret123" } };

        // Act
        _service.SetDefaultHeaders(headers);

        // Assert
        _httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-API-Key");
    }

    [Fact]
    public void SetDefaultHeaders_WithMultipleHeaders_ShouldAddAllHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "X-API-Key", "secret123" },
            { "X-Client-Id", "client456" },
            { "Accept-Language", "ko-KR" }
        };

        // Act
        _service.SetDefaultHeaders(headers);

        // Assert
        _httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-API-Key");
        _httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-Client-Id");
        _httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "Accept-Language");
    }

    [Fact]
    public async Task SetDefaultHeaders_ShouldIncludeInAllRequests()
    {
        // Arrange
        var headers = new Dictionary<string, string> { { "X-Default-Header", "DefaultValue" } };
        _mockHandler.SetupResponse(HttpStatusCode.OK, "Success");
        _service.SetDefaultHeaders(headers);

        // Act
        await _service.GetAsync("https://example.com", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "X-Default-Header");
    }

    [Fact]
    public async Task SetDefaultHeaders_WithRequestHeaders_ShouldCombineBoth()
    {
        // Arrange
        var defaultHeaders = new Dictionary<string, string> { { "X-Default", "default" } };
        var requestHeaders = new Dictionary<string, string> { { "X-Request", "request" } };
        _mockHandler.SetupResponse(HttpStatusCode.OK, "Success");
        _service.SetDefaultHeaders(defaultHeaders);

        // Act
        await _service.GetAsync("https://example.com", requestHeaders, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockHandler.LastRequest!.Headers.Should().Contain(h => h.Key == "X-Default");
        _mockHandler.LastRequest.Headers.Should().Contain(h => h.Key == "X-Request");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task HttpClientService_CompleteWorkflow_ShouldWork()
    {
        // Arrange
        using var httpClient = new HttpClient(new MockHttpMessageHandler());
        var service = new HttpClientService(httpClient);
        var mockHandler = httpClient.GetType()
            .GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(httpClient) as MockHttpMessageHandler;

        if (mockHandler != null)
        {
            mockHandler.SetupResponse(HttpStatusCode.OK, "Integration test content");
        }

        // Act & Assert
        service.SetUserAgent("IntegrationBot/1.0");
        service.SetTimeout(TimeSpan.FromSeconds(45));
        service.SetDefaultHeaders(new Dictionary<string, string> { { "X-Test", "integration" } });

        // GetAsync should work
        var response = await service.GetAsync("https://example.com", cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}

/// <summary>
/// Mock HttpMessageHandler for testing
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _content = string.Empty;
    private byte[]? _bytesContent;
    private TimeSpan _delay = TimeSpan.Zero;
    public HttpRequestMessage? LastRequest { get; private set; }

    public void SetupResponse(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
        _bytesContent = null;
        _delay = TimeSpan.Zero;
    }

    public void SetupBytesResponse(HttpStatusCode statusCode, byte[] content)
    {
        _statusCode = statusCode;
        _bytesContent = content;
        _content = string.Empty;
        _delay = TimeSpan.Zero;
    }

    public void SetupDelayedResponse(HttpStatusCode statusCode, string content, TimeSpan delay)
    {
        _statusCode = statusCode;
        _content = content;
        _bytesContent = null;
        _delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (_delay > TimeSpan.Zero)
        {
            await Task.Delay(_delay, cancellationToken);
        }

        var response = new HttpResponseMessage(_statusCode);

        if (_bytesContent != null)
        {
            response.Content = new ByteArrayContent(_bytesContent);
        }
        else
        {
            response.Content = new StringContent(_content);
        }

        return response;
    }
}
