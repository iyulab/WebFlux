using System.Text.RegularExpressions;

namespace WebFlux.Core.Utilities;

/// <summary>
/// URL 정규화 유틸리티
/// 동일한 리소스를 가리키는 URL들을 일관된 형태로 변환
/// </summary>
public static class UrlNormalizer
{
    /// <summary>
    /// URL을 정규화합니다
    /// </summary>
    public static string Normalize(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        // 1. Scheme lowercase
        var scheme = uri.Scheme.ToLowerInvariant();

        // 2. Host lowercase
        var host = uri.Host.ToLowerInvariant();

        // 3. Remove www prefix
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host.Substring(4);

        // 4. Remove default ports (80 for http, 443 for https)
        var port = uri.Port;
        var includePort = !uri.IsDefaultPort;

        // 5. Path - remove trailing slash (except for root "/")
        var path = uri.AbsolutePath;
        if (path.Length > 1 && path.EndsWith('/'))

            path = path.TrimEnd('/');

        // 6. Remove fragment (#)
        // Fragment is already excluded from AbsolutePath

        // 7. Clean double slashes in path
        path = Regex.Replace(path, @"//+", "/");

        // 8. Keep query string as-is
        var query = uri.Query;

        // Build normalized URL
        var portPart = includePort ? $":{port}" : "";
        return $"{scheme}://{host}{portPart}{path}{query}";
    }

    /// <summary>
    /// 두 URL이 동일한 리소스를 가리키는지 확인합니다
    /// </summary>
    public static bool AreEquivalent(string url1, string url2)
    {
        return Normalize(url1) == Normalize(url2);
    }
}
