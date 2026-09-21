using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;
using WebFlux.Services.Crawlers;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// <c>CrawlOptions.TimeoutMs</c> 가 HTTP 크롤러의 <b>모든 요청</b>에 닿는가 — 실제 소켓으로.
/// </summary>
/// <remarks>
/// 목(mock) 으로는 이 결함을 볼 수 없다: 옵션이 무시되는지 여부는 «응답이 늦는 서버» 앞에서만 드러난다.
/// 그래서 지연 응답하는 로컬 listener 를 쓰고, 벽시계 상한 대신 두 가지를 단언한다 —
/// <b>서버가 받은 요청 수</b>(재시도 여부)와 <b>경과 시간 &lt; 서버 지연</b>(페이지를 기다렸다면 성립할 수 없다).
/// 각 거절 fact 에는 같은 서버에 넉넉한 타임아웃을 준 양성 대조군이 붙는다.
/// </remarks>
public sealed class RequestTimeoutTests : IDisposable
{
    private static readonly TimeSpan SlowPage = TimeSpan.FromSeconds(4);

    private readonly DelayedServer _server = new();
    private readonly HttpClient _httpClient = new();
    private readonly HttpClientService _http;
    private readonly BreadthFirstCrawler _crawler;

    public RequestTimeoutTests()
    {
        _http = new HttpClientService(_httpClient);
        _crawler = new BreadthFirstCrawler(_http, Substitute.For<IEventPublisher>());
    }

    [Fact]
    public async Task APageSlowerThanTimeoutMs_Fails_BeforeThePageAnswers_AndIsNotRetried()
    {
        _server.Delay("/slow", SlowPage);
        var sw = Stopwatch.StartNew();

        var result = await _crawler.CrawlAsync(
            _server.Url("/slow"),
            new CrawlOptions { TimeoutMs = 300, RespectRobotsTxt = false },
            TestContext.Current.CancellationToken);

        sw.Stop();
        result.IsSuccess.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
        sw.Elapsed.Should().BeLessThan(SlowPage, "a crawl that waited for the page cannot finish before the page does");
        _server.Hits("/slow").Should().Be(1, "a caller who asked for 300 ms should hear back once, not after four attempts");
    }

    [Fact]
    public async Task TheSamePage_WithARoomyTimeout_Succeeds()
    {
        _server.Delay("/slowish", TimeSpan.FromMilliseconds(400));

        var result = await _crawler.CrawlAsync(
            _server.Url("/slowish"),
            new CrawlOptions { TimeoutMs = 10_000, RespectRobotsTxt = false },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task AHangingRobotsTxt_IsBoundByTheSameTimeout()
    {
        // The robots fetch precedes every page fetch. A timeout that reaches only the page request
        // still leaves the caller waiting on the slowest robots.txt.
        _server.Delay("/robots.txt", SlowPage);
        _server.Delay("/page", TimeSpan.Zero);
        var sw = Stopwatch.StartNew();

        var result = await _crawler.CrawlAsync(
            _server.Url("/page"),
            new CrawlOptions { TimeoutMs = 300, RespectRobotsTxt = true },
            TestContext.Current.CancellationToken);

        sw.Stop();
        sw.Elapsed.Should().BeLessThan(SlowPage);
        // An unreadable robots.txt is "unavailable" (RFC 9309 2.3.1.3 fallback) — the page is fetched.
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AHangingSitemap_IsBoundByTheSameTimeout()
    {
        _server.Delay("/sitemap.xml", SlowPage);
        var sw = Stopwatch.StartNew();

        var results = new List<CrawlResult>();
        await foreach (var r in _crawler.CrawlSitemapAsync(
            _server.Url("/sitemap.xml"),
            new CrawlOptions { TimeoutMs = 300, RespectRobotsTxt = false },
            TestContext.Current.CancellationToken))
        {
            results.Add(r);
        }

        sw.Stop();
        sw.Elapsed.Should().BeLessThan(SlowPage);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task AnExplicitRequestTimeout_IsNotCappedByTheServiceDefault()
    {
        // The old shape fixed HttpClient.Timeout at 30 s: any TimeoutMs above it was silently capped.
        // Scaled down: a service default of 200 ms must not cap an explicit 10 s request.
        _server.Delay("/slowish", TimeSpan.FromMilliseconds(600));
        _http.SetTimeout(TimeSpan.FromMilliseconds(200));

        using var response = await _http.GetAsync(
            _server.Url("/slowish"),
            timeout: TimeSpan.FromSeconds(10),
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithoutAnExplicitTimeout_TheServiceDefaultApplies_EvenAfterTheFirstRequest()
    {
        // HttpClient.Timeout cannot change once a request has been sent; a default that lives on
        // the HttpClient would throw here instead of taking effect.
        _server.Delay("/fast", TimeSpan.Zero);
        _server.Delay("/slowish", TimeSpan.FromMilliseconds(1500));
        using (await _http.GetAsync(_server.Url("/fast"), cancellationToken: TestContext.Current.CancellationToken)) { }

        _http.SetTimeout(TimeSpan.FromMilliseconds(200));

        var act = async () => await _http.GetAsync(
            _server.Url("/slowish"), cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task CallerCancellation_IsNotReportedAsATimeout()
    {
        _server.Delay("/slow", SlowPage);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(200);

        var act = async () => await _http.GetAsync(
            _server.Url("/slow"), timeout: TimeSpan.FromSeconds(10), cancellationToken: cts.Token);

        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.Should().NotBeOfType<TimeoutException>();
    }

    [Fact]
    public async Task ExtractOptionsTimeoutSeconds_ReachesTheSingleUrlPath_AsATimeoutCode_WithOneRequest()
    {
        _server.Delay("/slow", SlowPage);
        var factory = Substitute.For<IServiceFactory>();
        factory.CreateCrawler(Arg.Any<CrawlStrategy>()).Returns(_crawler);
        factory.TryCreateCacheService().Returns((ICacheService?)null);
        factory.TryCreateDomainRateLimiter().Returns((IDomainRateLimiter?)null);
        factory.TryCreateContentQualityEvaluator().Returns((IContentQualityEvaluator?)null);
        using var processor = new WebContentProcessor(
            factory, Substitute.For<IEventPublisher>(), Substitute.For<ILogger<WebContentProcessor>>());
        var sw = Stopwatch.StartNew();

        var result = await processor.ExtractContentAsync(
            _server.Url("/slow"),
            new ExtractOptions { TimeoutSeconds = 1, MaxRetries = 2, RespectRobotsTxt = false, EvaluateQuality = false },
            TestContext.Current.CancellationToken);

        sw.Stop();
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(ExtractErrorCodes.Timeout);
        sw.Elapsed.Should().BeLessThan(SlowPage);
        _server.Hits("/slow").Should().Be(1, "neither retry layer should repeat a request that timed out");
    }

    public void Dispose()
    {
        _server.Dispose();
        _httpClient.Dispose();
    }

    /// <summary>경로별로 정해진 시간만큼 기다렸다가 200 을 답하는 로컬 listener.</summary>
    private sealed class DelayedServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly Dictionary<string, TimeSpan> _delays = new();
        private readonly Dictionary<string, int> _hits = new();
        private readonly string _base;

        public DelayedServer()
        {
            int port;
            using (var probe = new TcpListener(IPAddress.Loopback, 0))
            {
                probe.Start();
                port = ((IPEndPoint)probe.LocalEndpoint).Port;
            }

            _base = $"http://localhost:{port}";
            _listener.Prefixes.Add(_base + "/");
            _listener.Start();
            _ = Task.Run(AcceptLoopAsync);
        }

        public string Url(string path) => _base + path;

        public void Delay(string path, TimeSpan delay)
        {
            lock (_delays) _delays[path] = delay;
        }

        public int Hits(string path)
        {
            lock (_hits) return _hits.GetValueOrDefault(path);
        }

        private async Task AcceptLoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { return; }

                _ = Task.Run(() => RespondAsync(ctx));
            }
        }

        private async Task RespondAsync(HttpListenerContext ctx)
        {
            var path = ctx.Request.Url!.AbsolutePath;
            lock (_hits) _hits[path] = _hits.GetValueOrDefault(path) + 1;

            TimeSpan delay;
            bool known;
            lock (_delays) known = _delays.TryGetValue(path, out delay);

            try
            {
                if (!known)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                    return;
                }

                await Task.Delay(delay, _stop.Token);
                var body = Encoding.UTF8.GetBytes("<html><head><title>t</title></head><body><p>hello</p></body></html>");
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.OutputStream.WriteAsync(body, _stop.Token);
                ctx.Response.Close();
            }
            catch
            {
                // The client gave up (that is the point of these tests) or the server is stopping.
                try { ctx.Response.Abort(); } catch { /* already gone */ }
            }
        }

        public void Dispose()
        {
            _stop.Cancel();
            try { _listener.Stop(); } catch { /* best effort */ }
            _listener.Close();
            _stop.Dispose();
        }
    }
}
