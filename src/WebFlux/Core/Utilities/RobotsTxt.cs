using System.Text;
using WebFlux.Core.Interfaces;

namespace WebFlux.Core.Utilities;

/// <summary>
/// robots.txt parsing and rule matching, in one place, following
/// <see href="https://www.rfc-editor.org/rfc/rfc9309.html">RFC 9309</see>.
/// </summary>
/// <remarks>
/// <para>
/// Split out of the crawler so that what the rules <em>mean</em> is a directly testable unit. While
/// this logic lived as a private static helper on the crawler base class it could only be reached
/// through an HTTP fetch, so nothing exercised it — the rules were parsed and applied for the first
/// time when the gate that calls them was connected.
/// </para>
/// <para>
/// Implemented from RFC 9309 sections 2.2.1 (groups), 2.2.2 (most-specific match) and 2.2.3
/// (<c>*</c> and <c>$</c>). Percent-encoding normalisation of the comparison operands (section
/// 2.2.2's table) is <em>not</em> implemented: a pattern written <c>/foo/%62%61%7A</c> does not
/// match the path <c>/foo/baz</c>.
/// </para>
/// <para>
/// A group is selected by an exact, case-insensitive match on the product token, falling back to
/// the <c>*</c> group. A caller that passes a full browser-style User-Agent header rather than a
/// product token therefore lands on <c>*</c>, which is the conservative direction.
/// </para>
/// </remarks>
public static class RobotsTxt
{
    /// <summary>
    /// Parses a robots.txt document into per-user-agent rules.
    /// </summary>
    /// <remarks>
    /// Consecutive <c>User-agent</c> lines form one group and share the rules that follow them
    /// (RFC 9309 section 2.2.1), and a product token that appears in more than one group has those
    /// groups' rules combined rather than replaced. A rule line with an empty value is not a rule
    /// and is dropped — an empty <c>Disallow:</c> is the canonical "everything is allowed" file.
    /// </remarks>
    /// <param name="content">The robots.txt body.</param>
    public static RobotsTxtInfo Parse(string content)
    {
        var allowed = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var disallowed = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var delays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sitemaps = new List<string>();

        // The group being accumulated: the product tokens declared for it, and its rules so far.
        var groupAgents = new List<string>();
        var groupAllow = new List<string>();
        var groupDisallow = new List<string>();
        int? groupDelay = null;
        var groupHasRules = false;

        void FlushGroup()
        {
            foreach (var agent in groupAgents)
            {
                if (groupAllow.Count > 0)
                {
                    if (!allowed.TryGetValue(agent, out var list))
                        allowed[agent] = list = new List<string>();
                    list.AddRange(groupAllow);
                }

                if (groupDisallow.Count > 0)
                {
                    if (!disallowed.TryGetValue(agent, out var list))
                        disallowed[agent] = list = new List<string>();
                    list.AddRange(groupDisallow);
                }

                // First declaration wins, so a merge is deterministic regardless of group order.
                if (groupDelay.HasValue && !delays.ContainsKey(agent))
                    delays[agent] = groupDelay.Value;
            }

            groupAgents.Clear();
            groupAllow.Clear();
            groupDisallow.Clear();
            groupDelay = null;
            groupHasRules = false;
        }

        foreach (var rawLine in content.Split('\n'))
        {
            // A '#' starts a comment anywhere on the line (RFC 9309 section 2.2.3).
            var line = rawLine;
            var hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash];

            line = line.Trim();
            if (line.Length == 0) continue;

            var colon = line.IndexOf(':');
            if (colon <= 0) continue;

            var directive = line[..colon].Trim().ToLowerInvariant();
            var value = line[(colon + 1)..].Trim();

            switch (directive)
            {
                case "user-agent":
                    // A user-agent line after this group's rules starts a new group; one that
                    // follows another user-agent line joins the same group.
                    if (groupHasRules) FlushGroup();
                    if (value.Length > 0) groupAgents.Add(value);
                    break;

                case "allow":
                    groupHasRules = true;
                    if (value.Length > 0) groupAllow.Add(value);
                    break;

                case "disallow":
                    groupHasRules = true;
                    if (value.Length > 0) groupDisallow.Add(value);
                    break;

                case "crawl-delay":
                    groupHasRules = true;
                    if (int.TryParse(value, out var delay)) groupDelay = delay;
                    break;

                case "sitemap":
                    // Not tied to a group.
                    if (value.Length > 0) sitemaps.Add(value);
                    break;
            }
        }

        FlushGroup();

        var rules = new Dictionary<string, RobotRules>(StringComparer.OrdinalIgnoreCase);
        foreach (var agent in allowed.Keys.Concat(disallowed.Keys).Concat(delays.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            rules[agent] = new RobotRules
            {
                AllowedPaths = (allowed.TryGetValue(agent, out var a) ? a : new List<string>()).AsReadOnly(),
                DisallowedPaths = (disallowed.TryGetValue(agent, out var d) ? d : new List<string>()).AsReadOnly(),
                CrawlDelay = delays.TryGetValue(agent, out var cd) ? cd : null
            };
        }

        return new RobotsTxtInfo
        {
            Access = RobotsTxtAccess.Parsed,
            Content = content,
            Rules = rules,
            Sitemaps = sitemaps.AsReadOnly(),
            CrawlDelay = delays.TryGetValue("*", out var starDelay) ? starDelay : null
        };
    }

    /// <summary>
    /// The product token of a User-Agent string: <c>"MyBot/1.0 (+https://…)"</c> is <c>"MyBot"</c>.
    /// </summary>
    /// <remarks>
    /// RFC 9309 section 2.2.1 defines the token as letters, <c>_</c> and <c>-</c>, and has crawlers
    /// match groups on it. A value with no such prefix (including <c>*</c>) selects the <c>*</c> group.
    /// </remarks>
    public static string ProductToken(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "*";

        var text = userAgent.AsSpan().TrimStart();
        var length = 0;
        while (length < text.Length && (char.IsAsciiLetter(text[length]) || text[length] is '_' or '-'))
            length++;

        return length == 0 ? "*" : text[..length].ToString();
    }

    /// <summary>
    /// Applies a parsed robots.txt to one URL.
    /// </summary>
    /// <remarks>
    /// The most specific matching rule decides, measured in octets of the pattern; an allow and a
    /// disallow of equal specificity resolve to allow (RFC 9309 section 2.2.2). A path no rule
    /// matches is allowed, as is any path when no group matches and the file declares no
    /// <c>*</c> group.
    /// </remarks>
    /// <param name="robotsInfo">The parsed rules.</param>
    /// <param name="url">The absolute URL to test.</param>
    /// <param name="userAgent">The crawler's User-Agent string, or just its product token.</param>
    public static bool IsAllowed(RobotsTxtInfo robotsInfo, string url, string userAgent)
    {
        // RFC 9309 section 2.3.1.4: a server that answered 5xx leaves the rules undefined, and an
        // undefined robots.txt is a complete disallow.
        if (robotsInfo.Access == RobotsTxtAccess.Unreachable) return false;

        if (!robotsInfo.Rules.TryGetValue(ProductToken(userAgent), out var rules) &&
            !robotsInfo.Rules.TryGetValue("*", out rules))
        {
            return true;
        }

        var path = new Uri(url).PathAndQuery;

        var bestAllow = -1;
        foreach (var pattern in rules.AllowedPaths)
        {
            if (Matches(pattern, path))
                bestAllow = Math.Max(bestAllow, Specificity(pattern));
        }

        var bestDisallow = -1;
        foreach (var pattern in rules.DisallowedPaths)
        {
            if (Matches(pattern, path))
                bestDisallow = Math.Max(bestDisallow, Specificity(pattern));
        }

        if (bestDisallow < 0) return true;
        if (bestAllow < 0) return false;

        // "If an allow rule and a disallow rule are equivalent, then the allow rule SHOULD be used."
        return bestAllow >= bestDisallow;
    }

    /// <summary>Octet length of the pattern — RFC 9309's measure of "most specific".</summary>
    private static int Specificity(string pattern) => Encoding.UTF8.GetByteCount(pattern);

    /// <summary>
    /// Matches one rule pattern against a path, interpreting <c>*</c> (zero or more of any
    /// character) and a trailing <c>$</c> (end of the path).
    /// </summary>
    /// <remarks>
    /// Deliberately not a regular expression: robots.txt is attacker-controlled input from the
    /// crawler's point of view, and a pattern of many <c>*</c> segments compiled to a regex is a
    /// catastrophic-backtracking hazard. This scan is linear in the path per segment.
    /// </remarks>
    private static bool Matches(string pattern, string path)
    {
        var anchored = pattern.EndsWith('$');
        if (anchored) pattern = pattern[..^1];

        if (pattern.Length == 0)
            return !anchored || path.Length == 0;

        var segments = pattern.Split('*');

        // The first segment is anchored at the start of the path.
        var first = segments[0];
        if (!path.StartsWith(first, StringComparison.Ordinal)) return false;
        var pos = first.Length;

        for (var i = 1; i < segments.Length; i++)
        {
            var segment = segments[i];
            var isLast = i == segments.Length - 1;

            if (segment.Length == 0)
            {
                // A '*' with nothing after it consumes the remainder, which also satisfies '$'.
                if (isLast) return true;
                continue;
            }

            if (isLast && anchored)
            {
                // The final segment must sit at the very end, and after everything matched so far.
                return path.Length - segment.Length >= pos &&
                       path.EndsWith(segment, StringComparison.Ordinal);
            }

            var index = path.IndexOf(segment, pos, StringComparison.Ordinal);
            if (index < 0) return false;
            pos = index + segment.Length;
        }

        return !anchored || pos == path.Length;
    }
}
