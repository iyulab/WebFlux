using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using NSubstitute;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Options;
using WebFlux.Services.Crawlers;
using Xunit;

namespace WebFlux.Tests.Services.Crawlers;

/// <summary>
/// The dynamic crawler through a real Chromium, on a local page whose content needs a browser: text a script adds
/// after the load, content a scroll handler adds, the custom headers a page request carries, and a selector that never
/// appears. Without a browser nothing here can be observed, so the fixture skips the facts when none is installed -
/// except where <c>WEBFLUX_REQUIRE_BROWSER=1</c> says the lane has one (the browser CI job), where a missing browser
/// fails them: a lane that silently skips proves nothing. Install one with
/// <c>pwsh bin/&lt;Configuration&gt;/&lt;tfm&gt;/playwright.ps1 install chromium</c>.
/// </summary>
[Trait("Category", "Browser")]
public sealed class PlaywrightCrawlerBrowserTests : IClassFixture<PlaywrightCrawlerBrowserTests.LocalPage>
{
    private readonly LocalPage _page;

    public PlaywrightCrawlerBrowserTests(LocalPage page) => _page = page;

    private static CrawlOptions Options(Action<CrawlOptions>? configure = null)
    {
        var options = new CrawlOptions
        {
            UseDynamicRendering = true,
            RespectRobotsTxt = false,
            TimeoutMs = 20000,
            EnableScrolling = false,
        };
        configure?.Invoke(options);
        return options;
    }

    private async Task<PlaywrightCrawler> CrawlerAsync()
    {
        await _page.EnsureBrowserAsync();
        return new PlaywrightCrawler(Substitute.For<IHttpClientService>(), Substitute.For<IEventPublisher>(), NullLogger<PlaywrightCrawler>.Instance);
    }

    [Fact]
    public async Task TextAScriptAddsAfterTheLoad_IsInTheResult_WhenTheCallerNamesTheSelector()
    {
        await using var crawler = await CrawlerAsync();

        var result = await crawler.CrawlAsync(_page.Url, Options(o => o.WaitForSelector = "#late"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        result.StatusCode.Should().Be(200);
        // The element the script creates - the words alone are also in the serialized <script> text before it ever runs.
        result.Content.Should().Contain("<div id=\"late\">rendered by script</div>").And.Contain("static heading");
        result.Metadata["Title"].Should().Be("Browser fixture");
        result.FinalUrl.Should().Be(_page.Url);
    }

    [Fact]
    public async Task WithoutTheSelector_TheCrawlerDoesNotWaitForTheScript()
    {
        // The positive control for the fact above: the script needs more time than the crawler's own short settle, so without a
        // named selector its text is not in the result - which is what makes the test above prove the wait.
        await using var crawler = await CrawlerAsync();

        var result = await crawler.CrawlAsync(_page.Url, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue(result.ErrorMessage);
        result.Content.Should().Contain("static heading").And.NotContain("<div id=\"late\">");
    }

    [Fact]
    public async Task ContentAScrollHandlerAdds_IsInTheResult_OnlyWhenScrollingIsEnabled()
    {
        await using var crawler = await CrawlerAsync();

        var still = await crawler.CrawlAsync(_page.Url, Options(), TestContext.Current.CancellationToken);
        var scrolled = await crawler.CrawlAsync(_page.Url, Options(o => o.EnableScrolling = true), TestContext.Current.CancellationToken);

        still.Content.Should().NotContain("<div id=\"lazy\">");
        scrolled.Content.Should().Contain("<div id=\"lazy\">lazy section</div>");
    }

    [Fact]
    public async Task ImagesAndLinks_AreResolvedAgainstThePage()
    {
        await using var crawler = await CrawlerAsync();

        var result = await crawler.CrawlAsync(_page.Url, Options(), TestContext.Current.CancellationToken);

        result.ImageUrls.Should().Contain(_page.Url + "pic.png");
        result.DiscoveredLinks.Should().Contain(_page.Url + "two");
    }

    [Fact]
    public async Task TheCustomHeadersAndTheUserAgent_ReachThePageRequest()
    {
        await using var crawler = await CrawlerAsync();

        await crawler.CrawlAsync(_page.Url, Options(o =>
        {
            o.CustomHeaders["X-Browser-Fixture"] = "from-the-options";
            o.UserAgent = "BrowserFixtureAgent/1.0";
        }), TestContext.Current.CancellationToken);

        var request = _page.PageRequests.ToArray().Last(r => r.Path == "/");
        request.Headers["X-Browser-Fixture"].Should().Be("from-the-options");
        request.Headers["User-Agent"].Should().Be("BrowserFixtureAgent/1.0");
    }

    [Fact]
    public async Task ASelectorThatNeverAppears_IsAFailedResultThatTimedOut_NotAHang()
    {
        await using var crawler = await CrawlerAsync();

        var result = await crawler.CrawlAsync(_page.Url, Options(o =>
        {
            o.WaitForSelector = "#never-rendered";
            o.TimeoutMs = 1500;
        }), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>One local page for the facts, and the browser check they share.</summary>
    public sealed class LocalPage : IAsyncLifetime
    {
        public sealed record PageRequest(string Path, IReadOnlyDictionary<string, string> Headers);

        private const string Html = """
            <!doctype html><html><head><title>Browser fixture</title></head><body style="margin:0">
            <h1>static heading</h1>
            <a href="/two">two</a>
            <img src="/pic.png">
            <div style="height:3000px">tall spacer</div>
            <script>
              setTimeout(function () {
                var d = document.createElement('div'); d.id = 'late'; d.textContent = 'rendered by script';
                document.body.insertBefore(d, document.body.firstChild);
              }, 1500);
              var added = false;
              window.addEventListener('scroll', function () {
                if (!added && window.scrollY > 300) {
                  added = true;
                  var l = document.createElement('div'); l.id = 'lazy'; l.textContent = 'lazy section';
                  document.body.appendChild(l);
                }
              });
            </script>
            </body></html>
            """;

        private HttpListener? _listener;
        private Task? _serving;
        private bool _browserChecked;
        private string? _browserProblem;

        public string Url { get; private set; } = string.Empty;

        public ConcurrentQueue<PageRequest> PageRequests { get; } = new();

        public ValueTask InitializeAsync()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Url = string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}/");
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();
            _serving = Task.Run(ServeAsync);
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            _listener?.Close();
            if (_serving is not null)
            {
                try { await _serving; } catch (HttpListenerException) { } catch (ObjectDisposedException) { }
            }
        }

        /// <summary>Skips the calling fact when no Chromium is installed, or fails it where <c>WEBFLUX_REQUIRE_BROWSER=1</c>.</summary>
        public async Task EnsureBrowserAsync()
        {
            if (!_browserChecked)
            {
                try
                {
                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Args = ["--no-sandbox"] });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Any failure to get a browser is «no browser here»: a missing executable (PlaywrightException), and the driver
                    // itself not starting (e.g. Node.js 26 without libatomic1 on Linux) - whichever, the facts cannot observe anything.
                    _browserProblem = ex.Message.Split('\n')[0];
                }
                _browserChecked = true;
            }

            if (_browserProblem is null)
                return;

            if (Environment.GetEnvironmentVariable("WEBFLUX_REQUIRE_BROWSER") == "1")
                Assert.Fail($"WEBFLUX_REQUIRE_BROWSER=1 but Chromium cannot be launched: {_browserProblem}");
            Assert.Skip($"No Chromium for Playwright here ({_browserProblem}); install one with playwright.ps1 install chromium.");
        }

        private async Task ServeAsync()
        {
            while (_listener is { IsListening: true })
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    return;
                }

                var path = context.Request.Url?.AbsolutePath ?? "/";
                var headers = context.Request.Headers.AllKeys
                    .Where(k => k is not null)
                    .ToDictionary(k => k!, k => context.Request.Headers[k!] ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                PageRequests.Enqueue(new PageRequest(path, headers));

                var isPage = path == "/";
                var body = isPage ? Encoding.UTF8.GetBytes(Html) : [];
                context.Response.StatusCode = isPage ? 200 : 404;
                context.Response.ContentType = "text/html; charset=utf-8";
                try
                {
                    await context.Response.OutputStream.WriteAsync(body);
                    context.Response.Close();
                }
                catch (HttpListenerException)
                {
                    // The browser closed the connection first (a request it no longer needs).
                }
            }
        }
    }
}
