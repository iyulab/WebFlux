using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// ai.txt 파일 파싱 서비스
/// AI 에이전트를 위한 웹사이트 가이드라인 및 메타데이터 처리
/// </summary>
public class AiTxtParser : IAiTxtParser
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiTxtParser> _logger;
    private readonly Dictionary<string, AiTxtParseResult> _cache;
    private readonly AiTxtStatistics _statistics;
    private readonly object _cacheLock = new();
    private readonly object _statsLock = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(6);

    // ai.txt 자동 감지를 위한 일반적인 경로들
    private static readonly string[] CommonAiTxtPaths =
    {
        "/ai.txt",
        "/.well-known/ai.txt",
        "/ai-policy.txt",
        "/ai-guidelines.txt"
    };

    public AiTxtParser(HttpClient httpClient, ILogger<AiTxtParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = new Dictionary<string, AiTxtParseResult>();
        _statistics = new AiTxtStatistics();
    }

    public async Task<AiTxtParseResult> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        lock (_statsLock)
        {
            _statistics.TotalParseAttempts++;
        }

        // 캐시 확인
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(baseUrl, out var cachedResult) &&
                DateTimeOffset.UtcNow - cachedResult.ParsedAt < CacheExpiry)
            {
                return cachedResult;
            }
        }

        try
        {
            _logger.LogInformation("Starting ai.txt parsing for {BaseUrl}", baseUrl);

            // 일반적인 경로에서 ai.txt 찾기
            foreach (var path in CommonAiTxtPaths)
            {
                var aiTxtUrl = new Uri(new Uri(baseUrl), path).ToString();

                try
                {
                    var content = await DownloadAiTxtAsync(aiTxtUrl, cancellationToken);
                    if (content != null)
                    {
                        var metadata = await ParseContentAsync(content, baseUrl);
                        var parseTime = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

                        var result = new AiTxtParseResult
                        {
                            IsSuccess = true,
                            FileFound = true,
                            AiTxtUrl = aiTxtUrl,
                            Metadata = metadata,
                            RawContent = content,
                            ParsedAt = DateTimeOffset.UtcNow
                        };

                        // 통계 업데이트
                        lock (_statsLock)
                        {
                            _statistics.SuccessfulParses++;
                            _statistics.SitesWithAiTxt++;
                            _statistics.AverageParseTime = (_statistics.AverageParseTime * (_statistics.TotalParseAttempts - 1) + parseTime) / _statistics.TotalParseAttempts;

                            if (_statistics.VersionStatistics.ContainsKey(metadata.Version))
                                _statistics.VersionStatistics[metadata.Version]++;
                            else
                                _statistics.VersionStatistics[metadata.Version] = 1;
                        }

                        // 캐시 저장
                        lock (_cacheLock)
                        {
                            _cache[baseUrl] = result;
                        }

                        _logger.LogInformation("Successfully parsed ai.txt for {BaseUrl} from {AiTxtUrl}", baseUrl, aiTxtUrl);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse ai.txt at {AiTxtUrl}", aiTxtUrl);
                }
            }

            // ai.txt 파일을 찾지 못함
            var notFoundResult = new AiTxtParseResult
            {
                IsSuccess = true,
                FileFound = false,
                AiTxtUrl = string.Empty,
                ParsedAt = DateTimeOffset.UtcNow
            };

            // 캐시 저장
            lock (_cacheLock)
            {
                _cache[baseUrl] = notFoundResult;
            }

            _logger.LogDebug("No ai.txt found for {BaseUrl}", baseUrl);
            return notFoundResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ai.txt parsing failed for {BaseUrl}", baseUrl);

            lock (_statsLock)
            {
                var errorType = ex.GetType().Name;
                if (_statistics.CommonErrors.ContainsKey(errorType))
                    _statistics.CommonErrors[errorType]++;
                else
                    _statistics.CommonErrors[errorType] = 1;
            }

            return new AiTxtParseResult
            {
                IsSuccess = false,
                FileFound = false,
                ErrorMessage = ex.Message,
                ParsedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public async Task<AiTxtMetadata> ParseContentAsync(string content, string baseUrl)
    {
        var metadata = new AiTxtMetadata
        {
            BaseUrl = baseUrl,
            ParsedAt = DateTimeOffset.UtcNow
        };

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var currentSection = string.Empty;
        var currentAgent = string.Empty;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 주석 및 빈 줄 건너뛰기
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            // 섹션 헤더 처리
            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = trimmedLine[1..^1].ToLowerInvariant();
                continue;
            }

            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex <= 0) continue;

            var key = trimmedLine.Substring(0, colonIndex).Trim().ToLowerInvariant();
            var value = trimmedLine.Substring(colonIndex + 1).Trim();

            try
            {
                switch (currentSection)
                {
                    case "":
                    case "general":
                        ParseGeneralSection(metadata, key, value);
                        break;

                    case "agent":
                        ParseAgentSection(metadata, key, value, ref currentAgent);
                        break;

                    case "license":
                        ParseLicenseSection(metadata, key, value);
                        break;

                    case "policy":
                        ParsePolicySection(metadata, key, value);
                        break;

                    case "contact":
                        ParseContactSection(metadata, key, value);
                        break;

                    case "api":
                        ParseApiSection(metadata, key, value);
                        break;

                    case "crawling":
                        ParseCrawlingSection(metadata, key, value);
                        break;

                    case "ethics":
                        ParseEthicsSection(metadata, key, value);
                        break;

                    case "privacy":
                        ParsePrivacySection(metadata, key, value);
                        break;

                    case "security":
                        ParseSecuritySection(metadata, key, value);
                        break;

                    default:
                        // 알 수 없는 섹션은 추가 메타데이터에 저장
                        if (!metadata.AdditionalMetadata.ContainsKey(currentSection))
                            metadata.AdditionalMetadata[currentSection] = new Dictionary<string, string>();
                        ((Dictionary<string, string>)metadata.AdditionalMetadata[currentSection])[key] = value;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ai.txt line: {Line}", trimmedLine);
            }
        }

        return metadata;
    }

    public bool IsActionAllowed(AiTxtMetadata metadata, string agentName, AiAction action)
    {
        // 특정 에이전트에 대한 권한 확인
        var permissions = GetAgentPermissions(metadata, agentName);
        if (permissions == null)
            return true; // 기본적으로 허용

        // 명시적으로 금지된 작업인지 확인
        if (permissions.DisallowedActions.Contains(action))
            return false;

        // 허용된 작업 목록이 있으면 그 안에 있는지 확인
        if (permissions.AllowedActions.Any())
            return permissions.AllowedActions.Contains(action);

        // 시간 제한 확인
        if (permissions.TimeWindows.Any())
        {
            return permissions.TimeWindows.Any(tw => tw.IsCurrentTimeAllowed());
        }

        return true; // 기본적으로 허용
    }

    public AiUsageLimits? GetUsageLimits(AiTxtMetadata metadata, string agentName)
    {
        var permissions = GetAgentPermissions(metadata, agentName);
        return permissions?.UsageLimits;
    }

    public ContentLicense? GetContentLicense(AiTxtMetadata metadata, string contentPath)
    {
        // 경로에 가장 구체적으로 매칭되는 라이센스 찾기
        return metadata.ContentLicenses
            .Where(license => license.ContentPatterns.Any(pattern =>
                Regex.IsMatch(contentPath, WildcardToRegex(pattern), RegexOptions.IgnoreCase)))
            .OrderByDescending(license => license.ContentPatterns
                .Where(pattern => Regex.IsMatch(contentPath, WildcardToRegex(pattern), RegexOptions.IgnoreCase))
                .Max(pattern => pattern.Length))
            .FirstOrDefault();
    }

    public DataUsagePolicy? GetDataUsagePolicy(AiTxtMetadata metadata, DataUsageType usageType)
    {
        return metadata.DataUsagePolicies
            .FirstOrDefault(policy => policy.UsageType == usageType);
    }

    public ContactInfo? GetContactInfo(AiTxtMetadata metadata)
    {
        return metadata.Contact;
    }

    public IReadOnlyList<string> GetSupportedVersions()
    {
        return new List<string> { "1.0", "1.1" }.AsReadOnly();
    }

    public AiTxtStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            _statistics.LastUpdated = DateTimeOffset.UtcNow;
            return _statistics;
        }
    }

    private async Task<string?> DownloadAiTxtAsync(string aiTxtUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, aiTxtUrl);
            request.Headers.Add("User-Agent", "WebFlux-AiTxtParser/1.0");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("ai.txt download failed: {StatusCode} - {Url}", response.StatusCode, aiTxtUrl);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ai.txt download exception: {Url}", aiTxtUrl);
            return null;
        }
    }

    private void ParseGeneralSection(AiTxtMetadata metadata, string key, string value)
    {
        switch (key)
        {
            case "version":
                metadata.Version = value;
                break;

            case "last-updated":
                if (DateTimeOffset.TryParse(value, out var lastUpdated))
                    metadata.LastUpdated = lastUpdated;
                break;

            case "owner":
                metadata.Owner ??= new SiteOwnerInfo();
                metadata.Owner.Name = value;
                break;

            case "owner-email":
                metadata.Owner ??= new SiteOwnerInfo();
                metadata.Owner.Email = value;
                break;

            case "owner-website":
                metadata.Owner ??= new SiteOwnerInfo();
                metadata.Owner.Website = value;
                break;

            case "organization-type":
                metadata.Owner ??= new SiteOwnerInfo();
                if (Enum.TryParse<OrganizationType>(value, true, out var orgType))
                    metadata.Owner.OrganizationType = orgType;
                break;

            case "country":
                metadata.Owner ??= new SiteOwnerInfo();
                metadata.Owner.Country = value;
                break;

            default:
                metadata.AdditionalMetadata[key] = value;
                break;
        }
    }

    private void ParseAgentSection(AiTxtMetadata metadata, string key, string value, ref string currentAgent)
    {
        switch (key)
        {
            case "name":
            case "agent":
                currentAgent = value;
                if (!metadata.AgentPermissions.ContainsKey(currentAgent))
                    metadata.AgentPermissions[currentAgent] = new AiAgentPermissions { AgentPattern = currentAgent };
                break;

            case "allow":
                if (!string.IsNullOrEmpty(currentAgent))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    var actions = ParseActionList(value);
                    permissions.AllowedActions.AddRange(actions);
                }
                break;

            case "disallow":
                if (!string.IsNullOrEmpty(currentAgent))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    var actions = ParseActionList(value);
                    permissions.DisallowedActions.AddRange(actions);
                }
                break;

            case "allow-paths":
                if (!string.IsNullOrEmpty(currentAgent))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    permissions.AllowedPaths.AddRange(value.Split(',').Select(p => p.Trim()));
                }
                break;

            case "disallow-paths":
                if (!string.IsNullOrEmpty(currentAgent))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    permissions.DisallowedPaths.AddRange(value.Split(',').Select(p => p.Trim()));
                }
                break;

            case "max-requests-per-hour":
                if (!string.IsNullOrEmpty(currentAgent) && int.TryParse(value, out var maxRequestsPerHour))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    permissions.UsageLimits ??= new AiUsageLimits();
                    permissions.UsageLimits.MaxRequestsPerHour = maxRequestsPerHour;
                }
                break;

            case "max-requests-per-day":
                if (!string.IsNullOrEmpty(currentAgent) && int.TryParse(value, out var maxRequestsPerDay))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    permissions.UsageLimits ??= new AiUsageLimits();
                    permissions.UsageLimits.MaxRequestsPerDay = maxRequestsPerDay;
                }
                break;

            case "time-window":
                if (!string.IsNullOrEmpty(currentAgent))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    var timeWindow = ParseTimeWindow(value);
                    if (timeWindow != null)
                        permissions.TimeWindows.Add(timeWindow);
                }
                break;

            default:
                if (!string.IsNullOrEmpty(currentAgent))
                {
                    var permissions = metadata.AgentPermissions[currentAgent];
                    permissions.AdditionalConditions[key] = value;
                }
                break;
        }
    }

    private void ParseLicenseSection(AiTxtMetadata metadata, string key, string value)
    {
        var currentLicense = metadata.ContentLicenses.LastOrDefault();

        switch (key)
        {
            case "name":
                currentLicense = new ContentLicense { Name = value };
                metadata.ContentLicenses.Add(currentLicense);
                break;

            case "url":
                if (currentLicense != null)
                    currentLicense.Url = value;
                break;

            case "type":
                if (currentLicense != null && Enum.TryParse<LicenseType>(value, true, out var licenseType))
                    currentLicense.Type = licenseType;
                break;

            case "content-patterns":
                if (currentLicense != null)
                    currentLicense.ContentPatterns.AddRange(value.Split(',').Select(p => p.Trim()));
                break;

            case "commercial-use":
                if (currentLicense != null && bool.TryParse(value, out var commercialUse))
                    currentLicense.AllowCommercialUse = commercialUse;
                break;

            case "modification":
                if (currentLicense != null && bool.TryParse(value, out var modification))
                    currentLicense.AllowModification = modification;
                break;

            case "redistribution":
                if (currentLicense != null && bool.TryParse(value, out var redistribution))
                    currentLicense.AllowRedistribution = redistribution;
                break;

            case "attribution":
                if (currentLicense != null && bool.TryParse(value, out var attribution))
                    currentLicense.RequireAttribution = attribution;
                break;

            case "conditions":
                if (currentLicense != null)
                    currentLicense.Conditions.AddRange(value.Split(',').Select(c => c.Trim()));
                break;

            case "limitations":
                if (currentLicense != null)
                    currentLicense.Limitations.AddRange(value.Split(',').Select(l => l.Trim()));
                break;
        }
    }

    private void ParsePolicySection(AiTxtMetadata metadata, string key, string value)
    {
        var currentPolicy = metadata.DataUsagePolicies.LastOrDefault();

        switch (key)
        {
            case "name":
                currentPolicy = new DataUsagePolicy { Name = value };
                metadata.DataUsagePolicies.Add(currentPolicy);
                break;

            case "usage-type":
                if (currentPolicy != null && Enum.TryParse<DataUsageType>(value, true, out var usageType))
                    currentPolicy.UsageType = usageType;
                break;

            case "allowed":
                if (currentPolicy != null && bool.TryParse(value, out var allowed))
                    currentPolicy.IsAllowed = allowed;
                break;

            case "content-patterns":
                if (currentPolicy != null)
                    currentPolicy.ContentPatterns.AddRange(value.Split(',').Select(p => p.Trim()));
                break;

            case "conditions":
                if (currentPolicy != null)
                    currentPolicy.Conditions.AddRange(value.Split(',').Select(c => c.Trim()));
                break;

            case "retention-period":
                if (currentPolicy != null && TimeSpan.TryParse(value, out var retention))
                    currentPolicy.DataRetentionPeriod = retention;
                break;

            case "anonymization":
                if (currentPolicy != null && bool.TryParse(value, out var anonymization))
                    currentPolicy.RequireAnonymization = anonymization;
                break;

            case "consent":
                if (currentPolicy != null && bool.TryParse(value, out var consent))
                    currentPolicy.RequireConsent = consent;
                break;

            case "description":
                if (currentPolicy != null)
                    currentPolicy.Description = value;
                break;
        }
    }

    private void ParseContactSection(AiTxtMetadata metadata, string key, string value)
    {
        metadata.Contact ??= new ContactInfo();

        switch (key)
        {
            case "email":
                metadata.Contact.Email = value;
                break;

            case "phone":
                metadata.Contact.Phone = value;
                break;

            case "website":
                metadata.Contact.Website = value;
                break;

            case "preferred-method":
                metadata.Contact.PreferredContactMethod = value;
                break;

            case "response-time":
                if (int.TryParse(value, out var responseTime))
                    metadata.Contact.ResponseTimeHours = responseTime;
                break;

            case "languages":
                metadata.Contact.Languages.AddRange(value.Split(',').Select(l => l.Trim()));
                break;

            default:
                if (key.StartsWith("social-"))
                {
                    var platform = key.Substring(7);
                    metadata.Contact.SocialMedia[platform] = value;
                }
                else
                {
                    metadata.Contact.OtherContacts[key] = value;
                }
                break;
        }
    }

    private void ParseApiSection(AiTxtMetadata metadata, string key, string value)
    {
        var currentEndpoint = metadata.ApiEndpoints.LastOrDefault();

        switch (key)
        {
            case "name":
                currentEndpoint = new ApiEndpoint { Name = value };
                metadata.ApiEndpoints.Add(currentEndpoint);
                break;

            case "path":
                if (currentEndpoint != null)
                    currentEndpoint.Path = value;
                break;

            case "methods":
                if (currentEndpoint != null)
                    currentEndpoint.Methods.AddRange(value.Split(',').Select(m => m.Trim().ToUpperInvariant()));
                break;

            case "description":
                if (currentEndpoint != null)
                    currentEndpoint.Description = value;
                break;

            case "auth-required":
                if (currentEndpoint != null && bool.TryParse(value, out var authRequired))
                    currentEndpoint.RequiresAuthentication = authRequired;
                break;

            case "content-types":
                if (currentEndpoint != null)
                    currentEndpoint.SupportedContentTypes.AddRange(value.Split(',').Select(ct => ct.Trim()));
                break;
        }
    }

    private void ParseCrawlingSection(AiTxtMetadata metadata, string key, string value)
    {
        metadata.CrawlingGuidelines ??= new AiCrawlingGuidelines();

        switch (key)
        {
            case "recommended-rate":
                if (double.TryParse(value, out var rate))
                    metadata.CrawlingGuidelines.RecommendedRate = rate;
                break;

            case "max-concurrent":
                if (int.TryParse(value, out var maxConcurrent))
                    metadata.CrawlingGuidelines.MaxConcurrentConnections = maxConcurrent;
                break;

            case "user-agent-requirements":
                metadata.CrawlingGuidelines.UserAgentRequirements.AddRange(value.Split(',').Select(ua => ua.Trim()));
                break;

            case "exclude-paths":
                metadata.CrawlingGuidelines.ExcludePaths.AddRange(value.Split(',').Select(p => p.Trim()));
                break;

            case "include-paths":
                metadata.CrawlingGuidelines.IncludePaths.AddRange(value.Split(',').Select(p => p.Trim()));
                break;

            case "time-window":
                var timeWindow = ParseTimeWindow(value);
                if (timeWindow != null)
                    metadata.CrawlingGuidelines.AllowedTimeWindows.Add(timeWindow);
                break;
        }
    }

    private void ParseEthicsSection(AiTxtMetadata metadata, string key, string value)
    {
        metadata.EthicsGuidelines ??= new EthicsGuidelines();

        switch (key)
        {
            case "version":
                metadata.EthicsGuidelines.Version = value;
                break;

            case "prohibited-use-cases":
                metadata.EthicsGuidelines.ProhibitedUseCases.AddRange(value.Split(',').Select(uc => uc.Trim()));
                break;

            case "required-considerations":
                metadata.EthicsGuidelines.RequiredConsiderations.AddRange(value.Split(',').Select(c => c.Trim()));
                break;

            case "bias-mitigation":
                metadata.EthicsGuidelines.BiasMitigationRequirements.AddRange(value.Split(',').Select(b => b.Trim()));
                break;

            case "transparency":
                metadata.EthicsGuidelines.TransparencyRequirements.AddRange(value.Split(',').Select(t => t.Trim()));
                break;
        }
    }

    private void ParsePrivacySection(AiTxtMetadata metadata, string key, string value)
    {
        metadata.PrivacyPolicy ??= new PrivacyPolicy();

        switch (key)
        {
            case "version":
                metadata.PrivacyPolicy.Version = value;
                break;

            case "processing-purposes":
                metadata.PrivacyPolicy.ProcessingPurposes.AddRange(value.Split(',').Select(p => p.Trim()));
                break;

            case "user-rights":
                metadata.PrivacyPolicy.UserRights.AddRange(value.Split(',').Select(r => r.Trim()));
                break;

            case "contact-email":
                metadata.PrivacyPolicy.PrivacyContact ??= new ContactInfo();
                metadata.PrivacyPolicy.PrivacyContact.Email = value;
                break;
        }
    }

    private void ParseSecuritySection(AiTxtMetadata metadata, string key, string value)
    {
        metadata.SecurityRequirements ??= new SecurityRequirements();

        switch (key)
        {
            case "encryption":
                metadata.SecurityRequirements.RequiredEncryption.AddRange(value.Split(',').Select(e => e.Trim()));
                break;

            case "authentication":
                metadata.SecurityRequirements.AuthenticationRequirements.AddRange(value.Split(',').Select(a => a.Trim()));
                break;

            case "access-control":
                metadata.SecurityRequirements.AccessControlRequirements.AddRange(value.Split(',').Select(ac => ac.Trim()));
                break;

            case "contact-email":
                metadata.SecurityRequirements.SecurityContact ??= new ContactInfo();
                metadata.SecurityRequirements.SecurityContact.Email = value;
                break;
        }
    }

    private AiAgentPermissions? GetAgentPermissions(AiTxtMetadata metadata, string agentName)
    {
        // 정확한 매치 먼저 확인
        if (metadata.AgentPermissions.TryGetValue(agentName, out var exactMatch))
            return exactMatch;

        // 패턴 매치 확인
        foreach (var kvp in metadata.AgentPermissions)
        {
            if (IsAgentMatch(agentName, kvp.Key))
                return kvp.Value;
        }

        // 와일드카드 매치 확인
        if (metadata.AgentPermissions.TryGetValue("*", out var wildcardMatch))
            return wildcardMatch;

        // 기본 권한 반환
        return metadata.DefaultPermissions;
    }

    private bool IsAgentMatch(string agentName, string pattern)
    {
        if (pattern == "*") return true;
        if (pattern == agentName) return true;

        try
        {
            var regex = WildcardToRegex(pattern);
            return Regex.IsMatch(agentName, regex, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
    }

    private List<AiAction> ParseActionList(string value)
    {
        var actions = new List<AiAction>();
        var actionStrings = value.Split(',').Select(a => a.Trim());

        foreach (var actionString in actionStrings)
        {
            if (Enum.TryParse<AiAction>(actionString, true, out var action))
                actions.Add(action);
        }

        return actions;
    }

    private TimeWindow? ParseTimeWindow(string value)
    {
        try
        {
            // 형식: "09:00-17:00" 또는 "09:00-17:00;Mon,Tue,Wed,Thu,Fri"
            var parts = value.Split(';');
            var timeRange = parts[0];
            var daysPart = parts.Length > 1 ? parts[1] : null;

            var timeParts = timeRange.Split('-');
            if (timeParts.Length != 2) return null;

            if (!TimeSpan.TryParse(timeParts[0], out var startTime) ||
                !TimeSpan.TryParse(timeParts[1], out var endTime))
                return null;

            var timeWindow = new TimeWindow
            {
                StartTime = startTime,
                EndTime = endTime
            };

            if (!string.IsNullOrEmpty(daysPart))
            {
                timeWindow.DaysOfWeek = new List<DayOfWeek>();
                var dayStrings = daysPart.Split(',').Select(d => d.Trim());

                foreach (var dayString in dayStrings)
                {
                    if (Enum.TryParse<DayOfWeek>(dayString, true, out var dayOfWeek))
                        timeWindow.DaysOfWeek.Add(dayOfWeek);
                }
            }

            return timeWindow;
        }
        catch
        {
            return null;
        }
    }
}