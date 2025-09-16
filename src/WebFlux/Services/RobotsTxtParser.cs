using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// robots.txt 파일 파싱 및 규칙 적용 서비스
/// RFC 9309 표준을 따르며 크롤링 최적화를 제공
/// </summary>
public class RobotsTxtParser : IRobotsTxtParser
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RobotsTxtParser> _logger;
    private readonly Dictionary<string, RobotsTxtParseResult> _cache;
    private readonly object _cacheLock = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(4);

    public RobotsTxtParser(HttpClient httpClient, ILogger<RobotsTxtParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = new Dictionary<string, RobotsTxtParseResult>();
    }

    public async Task<RobotsTxtParseResult> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var robotsUrl = GetRobotsUrl(baseUrl);

            // 캐시 확인
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(robotsUrl, out var cachedResult) &&
                    DateTimeOffset.UtcNow - cachedResult.ParsedAt < CacheExpiry)
                {
                    return cachedResult;
                }
            }

            // robots.txt 다운로드
            var content = await DownloadRobotsTxtAsync(robotsUrl, cancellationToken);

            var parseResult = content != null
                ? await ParseContentAsync(content, baseUrl)
                : CreateDefaultResult(baseUrl);

            // 캐시 저장
            lock (_cacheLock)
            {
                _cache[robotsUrl] = parseResult;
            }

            return parseResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "robots.txt 파싱 실패: {BaseUrl}", baseUrl);
            return CreateErrorResult(baseUrl, ex.Message);
        }
    }

    public async Task<RobotsMetadata> ParseContentAsync(string content, string baseUrl)
    {
        var metadata = new RobotsMetadata { BaseUrl = baseUrl };
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var currentUserAgent = "*";
        var rules = new Dictionary<string, List<RobotsRule>>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 주석 제거
            var commentIndex = trimmedLine.IndexOf('#');
            if (commentIndex >= 0)
                trimmedLine = trimmedLine.Substring(0, commentIndex).Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var field = trimmedLine.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = trimmedLine.Substring(colonIndex + 1).Trim();

            switch (field)
            {
                case "user-agent":
                    currentUserAgent = value;
                    if (!rules.ContainsKey(currentUserAgent))
                        rules[currentUserAgent] = new List<RobotsRule>();
                    break;

                case "disallow":
                    if (!rules.ContainsKey(currentUserAgent))
                        rules[currentUserAgent] = new List<RobotsRule>();
                    rules[currentUserAgent].Add(new RobotsRule
                    {
                        Type = RobotsRuleType.Disallow,
                        Pattern = value,
                        UserAgent = currentUserAgent
                    });
                    break;

                case "allow":
                    if (!rules.ContainsKey(currentUserAgent))
                        rules[currentUserAgent] = new List<RobotsRule>();
                    rules[currentUserAgent].Add(new RobotsRule
                    {
                        Type = RobotsRuleType.Allow,
                        Pattern = value,
                        UserAgent = currentUserAgent
                    });
                    break;

                case "crawl-delay":
                    if (double.TryParse(value, out var delay))
                    {
                        metadata.CrawlDelay = TimeSpan.FromSeconds(delay);
                    }
                    break;

                case "sitemap":
                    metadata.Sitemaps.Add(value);
                    break;

                case "request-rate":
                    ParseRequestRate(value, metadata);
                    break;

                case "visit-time":
                    ParseVisitTime(value, metadata);
                    break;

                case "host":
                    metadata.PreferredHost = value;
                    break;
            }
        }

        // 규칙을 우선순위별로 정렬
        foreach (var userAgent in rules.Keys)
        {
            metadata.Rules[userAgent] = rules[userAgent]
                .OrderBy(r => r.Type == RobotsRuleType.Allow ? 0 : 1)
                .ThenByDescending(r => r.Pattern.Length)
                .ToList();
        }

        return metadata;
    }

    public bool IsUrlAllowed(RobotsMetadata metadata, string url, string userAgent = "*")
    {
        var path = GetPathFromUrl(url);

        // User-Agent 매칭 우선순위: 정확한 매치 -> 와일드카드
        var applicableRules = new List<RobotsRule>();

        if (metadata.Rules.TryGetValue(userAgent, out var exactRules))
            applicableRules.AddRange(exactRules);

        if (metadata.Rules.TryGetValue("*", out var wildcardRules))
            applicableRules.AddRange(wildcardRules);

        // 규칙 적용 (Allow가 Disallow보다 우선, 더 구체적인 패턴이 우선)
        foreach (var rule in applicableRules.OrderBy(r => r.Type).ThenByDescending(r => r.Pattern.Length))
        {
            if (MatchesPattern(path, rule.Pattern))
            {
                return rule.Type == RobotsRuleType.Allow;
            }
        }

        // 기본값: 허용
        return true;
    }

    public TimeSpan? GetCrawlDelay(RobotsMetadata metadata, string userAgent = "*")
    {
        return metadata.CrawlDelay;
    }

    public IReadOnlyList<string> GetSitemaps(RobotsMetadata metadata)
    {
        return metadata.Sitemaps.AsReadOnly();
    }

    private async Task<string?> DownloadRobotsTxtAsync(string robotsUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, robotsUrl);
            request.Headers.Add("User-Agent", "WebFluxBot/1.0");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("robots.txt 다운로드 실패: {StatusCode} - {Url}", response.StatusCode, robotsUrl);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "robots.txt 다운로드 예외: {Url}", robotsUrl);
            return null;
        }
    }

    private string GetRobotsUrl(string baseUrl)
    {
        var uri = new Uri(baseUrl.TrimEnd('/'));
        return $"{uri.Scheme}://{uri.Host}{(uri.Port != 80 && uri.Port != 443 ? $":{uri.Port}" : "")}/robots.txt";
    }

    private string GetPathFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.PathAndQuery;
        }
        catch
        {
            return url;
        }
    }

    private bool MatchesPattern(string path, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        // 빈 패턴은 모든 것을 허용
        if (pattern == "/")
            return true;

        // 와일드카드를 정규식으로 변환
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\$", "$") + ".*";

        try
        {
            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            // 정규식 오류 시 단순 문자열 매칭
            return path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ParseRequestRate(string value, RobotsMetadata metadata)
    {
        // 형식: "1/10s" (10초 내 1개 요청)
        var match = Regex.Match(value, @"(\d+)/(\d+)([smh])?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var requests) &&
                int.TryParse(match.Groups[2].Value, out var timeValue))
            {
                var timeUnit = match.Groups[3].Value.ToLowerInvariant();
                var multiplier = timeUnit switch
                {
                    "h" => 3600,
                    "m" => 60,
                    _ => 1
                };

                metadata.RequestRate = new RequestRateLimit
                {
                    RequestCount = requests,
                    TimeWindow = TimeSpan.FromSeconds(timeValue * multiplier)
                };
            }
        }
    }

    private void ParseVisitTime(string value, RobotsMetadata metadata)
    {
        // 형식: "0800-1400" (08:00부터 14:00까지)
        var match = Regex.Match(value, @"(\d{4})-(\d{4})");
        if (match.Success)
        {
            if (TimeSpan.TryParseExact(match.Groups[1].Value, @"hhmm", null, out var start) &&
                TimeSpan.TryParseExact(match.Groups[2].Value, @"hhmm", null, out var end))
            {
                metadata.VisitTimeWindow = new VisitTimeWindow
                {
                    StartTime = start,
                    EndTime = end
                };
            }
        }
    }

    private RobotsTxtParseResult CreateDefaultResult(string baseUrl)
    {
        return new RobotsTxtParseResult
        {
            IsSuccess = true,
            FileFound = false,
            RobotsUrl = GetRobotsUrl(baseUrl),
            Metadata = new RobotsMetadata
            {
                BaseUrl = baseUrl,
                Rules = new Dictionary<string, List<RobotsRule>>
                {
                    ["*"] = new List<RobotsRule>()
                }
            },
            ParsedAt = DateTimeOffset.UtcNow
        };
    }

    private RobotsTxtParseResult CreateErrorResult(string baseUrl, string errorMessage)
    {
        return new RobotsTxtParseResult
        {
            IsSuccess = false,
            FileFound = false,
            RobotsUrl = GetRobotsUrl(baseUrl),
            ErrorMessage = errorMessage,
            ParsedAt = DateTimeOffset.UtcNow
        };
    }
}