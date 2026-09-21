using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebFlux.Tests.TestSupport;

/// <summary>
/// 경로별로 정해진 시간만큼 기다렸다가 정해진 본문을 200 으로 답하는 로컬 listener.
/// 받은 요청 수와 마지막 요청의 헤더를 기록한다 — 목(mock)으로는 볼 수 없는 «실제로 무엇이 전송됐는가» 를 단언하기 위해.
/// </summary>
public sealed class LocalHttpServer : IDisposable
{
    private const string DefaultBody = "<html><head><title>t</title></head><body><p>hello</p></body></html>";

    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Dictionary<string, (TimeSpan Delay, string Body, string ContentType)> _routes = new();
    private readonly Dictionary<string, int> _statuses = new();
    private readonly HashSet<string> _aborts = new();
    private readonly Dictionary<string, int> _hits = new();
    private readonly Dictionary<string, Dictionary<string, string>> _lastHeaders = new();
    private readonly string _base;

    public LocalHttpServer()
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

    public void Delay(string path, TimeSpan delay) => Serve(path, DefaultBody, "text/html; charset=utf-8", delay);

    public void Serve(string path, string body, string contentType = "text/html; charset=utf-8", TimeSpan delay = default)
    {
        lock (_routes) _routes[path] = (delay, body, contentType);
    }

    /// <summary>그 경로가 본문 없이 이 상태 코드를 답하게 한다.</summary>
    public void Status(string path, int statusCode)
    {
        lock (_routes) _statuses[path] = statusCode;
    }

    /// <summary>그 경로가 응답 없이 연결을 끊게 한다 (클라이언트에는 전송 오류).</summary>
    public void Abort(string path)
    {
        lock (_routes) _aborts.Add(path);
    }

    public int Hits(string path)
    {
        lock (_hits) return _hits.GetValueOrDefault(path);
    }

    /// <summary>그 경로로 온 마지막 요청의 헤더(이름 대소문자 무시). 요청이 없었으면 빈 사전.</summary>
    public IReadOnlyDictionary<string, string> LastHeaders(string path)
    {
        lock (_hits)
            return _lastHeaders.TryGetValue(path, out var h)
                ? h
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
        lock (_hits)
        {
            _hits[path] = _hits.GetValueOrDefault(path) + 1;
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in ctx.Request.Headers.AllKeys)
                if (key != null) headers[key] = ctx.Request.Headers[key] ?? string.Empty;
            _lastHeaders[path] = headers;
        }

        (TimeSpan Delay, string Body, string ContentType) route;
        bool known, abort;
        int status;
        lock (_routes)
        {
            known = _routes.TryGetValue(path, out route);
            abort = _aborts.Contains(path);
            _statuses.TryGetValue(path, out status);
        }

        try
        {
            if (abort)
            {
                ctx.Response.Abort();
                return;
            }

            if (status != 0)
            {
                ctx.Response.StatusCode = status;
                ctx.Response.Close();
                return;
            }

            if (!known)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            await Task.Delay(route.Delay, _stop.Token);
            var body = Encoding.UTF8.GetBytes(route.Body);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = route.ContentType;
            await ctx.Response.OutputStream.WriteAsync(body, _stop.Token);
            ctx.Response.Close();
        }
        catch
        {
            // The client gave up or the server is stopping.
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
