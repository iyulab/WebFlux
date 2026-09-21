using System.Reflection;

namespace WebFlux.Core.Utilities;

/// <summary>
/// The one place that says who this crawler is.
/// </summary>
/// <remarks>
/// A crawler's User-Agent is how a site operator identifies it, writes a robots.txt group for it
/// and finds someone to complain to. It therefore carries exactly one product token, the library's
/// real version, and a contact URL that belongs to this project.
/// </remarks>
public static class WebFluxUserAgent
{
    /// <summary>The product token robots.txt groups are matched against.</summary>
    public const string ProductToken = "WebFlux";

    /// <summary>The project URL sent as the contact comment.</summary>
    public const string ContactUrl = "https://github.com/iyulab/WebFlux";

    /// <summary><c>WebFlux/{major}.{minor} (+{ContactUrl})</c>.</summary>
    public static string Default { get; } = Build();

    private static string Build()
    {
        var version = typeof(WebFluxUserAgent).Assembly.GetName().Version;
        var label = version is null ? "0" : $"{version.Major}.{version.Minor}";
        return $"{ProductToken}/{label} (+{ContactUrl})";
    }
}
