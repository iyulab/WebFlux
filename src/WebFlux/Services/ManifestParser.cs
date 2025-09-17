using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// Web App Manifest 파일 파싱 서비스
/// PWA 및 웹앱 메타데이터 처리 (W3C Web App Manifest 표준)
/// </summary>
public class ManifestParser : IManifestParser
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ManifestParser> _logger;
    private readonly Dictionary<string, ManifestParseResult> _cache;
    private readonly ManifestStatistics _statistics;
    private readonly object _cacheLock = new();
    private readonly object _statsLock = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(4);

    // manifest.json 자동 감지를 위한 일반적인 경로들
    private static readonly string[] CommonManifestPaths =
    {
        "/manifest.json",
        "/manifest.webmanifest",
        "/app.webmanifest",
        "/site.webmanifest"
    };

    public ManifestParser(HttpClient httpClient, ILogger<ManifestParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = new Dictionary<string, ManifestParseResult>();
        _statistics = new ManifestStatistics();
    }

    public async Task<ManifestParseResult> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
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
            _logger.LogInformation("Starting manifest parsing for {BaseUrl}", baseUrl);

            // 1. 메인 HTML 페이지에서 manifest 링크 찾기
            var htmlContent = await DownloadHtmlAsync(baseUrl, cancellationToken);
            if (!string.IsNullOrEmpty(htmlContent))
            {
                var htmlResult = await ParseFromHtmlAsync(htmlContent, baseUrl, cancellationToken);
                if (htmlResult.FileFound)
                {
                    return htmlResult;
                }
            }

            // 2. 일반적인 경로에서 manifest 찾기
            foreach (var path in CommonManifestPaths)
            {
                var manifestUrl = new Uri(new Uri(baseUrl), path).ToString();

                try
                {
                    var content = await DownloadManifestAsync(manifestUrl, cancellationToken);
                    if (content != null)
                    {
                        var metadata = await ParseContentAsync(content, baseUrl);
                        var result = await CreateParseResultAsync(manifestUrl, metadata, content);

                        await UpdateStatisticsAsync(result, DateTimeOffset.UtcNow - startTime);
                        await CacheResultAsync(baseUrl, result);

                        _logger.LogInformation("Successfully parsed manifest for {BaseUrl} from {ManifestUrl}", baseUrl, manifestUrl);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse manifest at {ManifestUrl}", manifestUrl);
                }
            }

            // manifest 파일을 찾지 못함
            var notFoundResult = new ManifestParseResult
            {
                IsSuccess = true,
                FileFound = false,
                ManifestUrl = string.Empty,
                ParsedAt = DateTimeOffset.UtcNow
            };

            await CacheResultAsync(baseUrl, notFoundResult);
            _logger.LogDebug("No manifest found for {BaseUrl}", baseUrl);
            return notFoundResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manifest parsing failed for {BaseUrl}", baseUrl);

            lock (_statsLock)
            {
                var errorType = ex.GetType().Name;
                if (_statistics.CommonErrors.ContainsKey(errorType))
                    _statistics.CommonErrors[errorType]++;
                else
                    _statistics.CommonErrors[errorType] = 1;
            }

            return new ManifestParseResult
            {
                IsSuccess = false,
                FileFound = false,
                ErrorMessage = ex.Message,
                ParsedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public async Task<ManifestParseResult> ParseFromHtmlAsync(string htmlContent, string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            // HTML에서 manifest 링크 추출
            var manifestUrls = ExtractManifestUrls(htmlContent, baseUrl);

            foreach (var manifestUrl in manifestUrls)
            {
                try
                {
                    var content = await DownloadManifestAsync(manifestUrl, cancellationToken);
                    if (content != null)
                    {
                        var metadata = await ParseContentAsync(content, baseUrl);
                        return await CreateParseResultAsync(manifestUrl, metadata, content);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse manifest from HTML link: {ManifestUrl}", manifestUrl);
                }
            }

            return new ManifestParseResult
            {
                IsSuccess = true,
                FileFound = false,
                ParsedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse manifest from HTML");
            return new ManifestParseResult
            {
                IsSuccess = false,
                FileFound = false,
                ErrorMessage = ex.Message,
                ParsedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public async Task<ManifestMetadata> ParseContentAsync(string content, string baseUrl)
    {
        var metadata = new ManifestMetadata
        {
            BaseUrl = baseUrl,
            ParsedAt = DateTimeOffset.UtcNow
        };

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            // 기본 속성 파싱
            if (root.TryGetProperty("name", out var nameElement))
                metadata.Name = nameElement.GetString();

            if (root.TryGetProperty("short_name", out var shortNameElement))
                metadata.ShortName = shortNameElement.GetString();

            if (root.TryGetProperty("description", out var descElement))
                metadata.Description = descElement.GetString();

            if (root.TryGetProperty("start_url", out var startUrlElement))
                metadata.StartUrl = ResolveUrl(startUrlElement.GetString(), baseUrl);

            if (root.TryGetProperty("scope", out var scopeElement))
                metadata.Scope = ResolveUrl(scopeElement.GetString(), baseUrl);

            if (root.TryGetProperty("display", out var displayElement) &&
                Enum.TryParse<DisplayMode>(displayElement.GetString(), true, out var display))
                metadata.Display = display;

            if (root.TryGetProperty("orientation", out var orientationElement) &&
                Enum.TryParse<ScreenOrientation>(orientationElement.GetString()?.Replace("-", ""), true, out var orientation))
                metadata.Orientation = orientation;

            if (root.TryGetProperty("theme_color", out var themeColorElement))
                metadata.ThemeColor = themeColorElement.GetString();

            if (root.TryGetProperty("background_color", out var bgColorElement))
                metadata.BackgroundColor = bgColorElement.GetString();

            if (root.TryGetProperty("lang", out var langElement))
                metadata.Lang = langElement.GetString();

            if (root.TryGetProperty("dir", out var dirElement) &&
                Enum.TryParse<TextDirection>(dirElement.GetString(), true, out var textDir))
                metadata.Dir = textDir;

            // 아이콘 파싱
            if (root.TryGetProperty("icons", out var iconsElement) && iconsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var iconElement in iconsElement.EnumerateArray())
                {
                    var icon = ParseIcon(iconElement, baseUrl);
                    if (icon != null)
                        metadata.Icons.Add(icon);
                }
            }

            // 스크린샷 파싱
            if (root.TryGetProperty("screenshots", out var screenshotsElement) && screenshotsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var screenshotElement in screenshotsElement.EnumerateArray())
                {
                    var screenshot = ParseScreenshot(screenshotElement, baseUrl);
                    if (screenshot != null)
                        metadata.Screenshots.Add(screenshot);
                }
            }

            // 카테고리 파싱
            if (root.TryGetProperty("categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var categoryElement in categoriesElement.EnumerateArray())
                {
                    var category = categoryElement.GetString();
                    if (!string.IsNullOrEmpty(category))
                        metadata.Categories.Add(category);
                }
            }

            // 관련 애플리케이션 파싱
            if (root.TryGetProperty("related_applications", out var relatedAppsElement) && relatedAppsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var appElement in relatedAppsElement.EnumerateArray())
                {
                    var app = ParseRelatedApplication(appElement);
                    if (app != null)
                        metadata.RelatedApplications.Add(app);
                }
            }

            if (root.TryGetProperty("prefer_related_applications", out var preferElement))
                metadata.PreferRelatedApplications = preferElement.GetBoolean();

            // 바로가기 파싱
            if (root.TryGetProperty("shortcuts", out var shortcutsElement) && shortcutsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var shortcutElement in shortcutsElement.EnumerateArray())
                {
                    var shortcut = ParseShortcut(shortcutElement, baseUrl);
                    if (shortcut != null)
                        metadata.Shortcuts.Add(shortcut);
                }
            }

            // 공유 대상 파싱
            if (root.TryGetProperty("share_target", out var shareTargetElement))
            {
                metadata.ShareTarget = ParseShareTarget(shareTargetElement, baseUrl);
            }

            return metadata;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in manifest content");
            throw new ArgumentException("Invalid JSON format in manifest", ex);
        }
    }

    public async Task<PwaCompatibilityResult> EvaluatePwaCompatibilityAsync(ManifestMetadata metadata)
    {
        var result = new PwaCompatibilityResult
        {
            EvaluatedAt = DateTimeOffset.UtcNow
        };

        var requirements = new List<PwaRequirement>();

        // 필수 요구사항 검증
        requirements.Add(new PwaRequirement
        {
            Name = "Name",
            IsMet = !string.IsNullOrEmpty(metadata.Name),
            Score = !string.IsNullOrEmpty(metadata.Name) ? 100 : 0,
            Importance = RequirementImportance.Critical,
            Description = "앱 이름이 정의되어야 합니다",
            FailureReason = string.IsNullOrEmpty(metadata.Name) ? "name 속성이 없습니다" : null
        });

        requirements.Add(new PwaRequirement
        {
            Name = "StartUrl",
            IsMet = !string.IsNullOrEmpty(metadata.StartUrl),
            Score = !string.IsNullOrEmpty(metadata.StartUrl) ? 100 : 0,
            Importance = RequirementImportance.Critical,
            Description = "시작 URL이 정의되어야 합니다",
            FailureReason = string.IsNullOrEmpty(metadata.StartUrl) ? "start_url 속성이 없습니다" : null
        });

        var hasAppropriateIcons = metadata.Icons.Any(i =>
            i.Sizes.Any(s => s.Contains("192") || s.Contains("512")));
        requirements.Add(new PwaRequirement
        {
            Name = "Icons",
            IsMet = hasAppropriateIcons,
            Score = hasAppropriateIcons ? 100 : 0,
            Importance = RequirementImportance.Critical,
            Description = "적절한 크기의 아이콘(192x192, 512x512)이 필요합니다",
            FailureReason = !hasAppropriateIcons ? "192x192 또는 512x512 크기의 아이콘이 없습니다" : null
        });

        var hasStandaloneDisplay = metadata.Display == DisplayMode.Standalone ||
                                 metadata.Display == DisplayMode.Fullscreen ||
                                 metadata.Display == DisplayMode.MinimalUi;
        requirements.Add(new PwaRequirement
        {
            Name = "Display",
            IsMet = hasStandaloneDisplay,
            Score = hasStandaloneDisplay ? 100 : 0,
            Importance = RequirementImportance.High,
            Description = "독립실행형 표시 모드가 권장됩니다",
            FailureReason = !hasStandaloneDisplay ? "display 모드가 standalone, fullscreen, minimal-ui가 아닙니다" : null
        });

        result.Requirements = requirements;
        result.MeetsMinimumRequirements = requirements.Where(r => r.Importance == RequirementImportance.Critical).All(r => r.IsMet);
        result.IsInstallable = result.MeetsMinimumRequirements && hasStandaloneDisplay;
        result.OverallScore = requirements.Average(r => r.Score);

        // PWA 성숙도 레벨 결정
        if (result.OverallScore >= 90) result.MaturityLevel = PwaMaturityLevel.Expert;
        else if (result.OverallScore >= 75) result.MaturityLevel = PwaMaturityLevel.Advanced;
        else if (result.OverallScore >= 60) result.MaturityLevel = PwaMaturityLevel.Intermediate;
        else if (result.OverallScore >= 40) result.MaturityLevel = PwaMaturityLevel.Beginner;
        else result.MaturityLevel = PwaMaturityLevel.Basic;

        return result;
    }

    public async Task<IconAnalysisResult> AnalyzeIconsAsync(ManifestMetadata metadata)
    {
        var result = new IconAnalysisResult
        {
            TotalIcons = metadata.Icons.Count
        };

        // 사용 가능한 크기 분석
        foreach (var icon in metadata.Icons)
        {
            result.AvailableSizes.AddRange(icon.Sizes);
            if (!string.IsNullOrEmpty(icon.Type) && !result.SupportedFormats.Contains(icon.Type))
                result.SupportedFormats.Add(icon.Type);

            result.SupportsMaskable = result.SupportsMaskable || icon.Purpose.Contains(IconPurpose.Maskable);
            result.SupportsMonochrome = result.SupportsMonochrome || icon.Purpose.Contains(IconPurpose.Monochrome);
        }

        result.AvailableSizes = result.AvailableSizes.Distinct().ToList();

        // 권장 크기 확인
        var recommendedSizes = new[] { "192x192", "512x512", "144x144", "96x96", "72x72", "48x48" };
        result.MissingRecommendedSizes = recommendedSizes.Where(size => !result.AvailableSizes.Contains(size)).ToList();

        // 품질 점수 계산
        var baseScore = metadata.Icons.Any() ? 50 : 0;
        var sizeScore = Math.Min(30, result.AvailableSizes.Count * 5);
        var purposeScore = (result.SupportsMaskable ? 10 : 0) + (result.SupportsMonochrome ? 5 : 0);
        var formatScore = result.SupportedFormats.Count > 1 ? 5 : 0;

        result.QualityScore = baseScore + sizeScore + purposeScore + formatScore;

        return result;
    }

    public async Task<AppCategoryPrediction> PredictAppCategoryAsync(ManifestMetadata metadata)
    {
        var categoryScores = new Dictionary<string, double>();

        // 명시된 카테고리가 있으면 높은 신뢰도로 사용
        if (metadata.Categories.Any())
        {
            var primaryCategory = metadata.Categories.First();
            return new AppCategoryPrediction
            {
                PrimaryCategory = primaryCategory,
                Confidence = 95,
                CategoryScores = metadata.Categories.ToDictionary(c => c, _ => 95.0),
                PredictionReasons = new List<string> { "매니페스트에 명시된 카테고리" },
                StandardCategories = metadata.Categories
            };
        }

        // 이름과 설명 기반 예측
        var text = $"{metadata.Name} {metadata.Description}".ToLowerInvariant();

        // 간단한 키워드 기반 분류
        var categoryKeywords = new Dictionary<string, string[]>
        {
            ["business"] = new[] { "business", "office", "work", "enterprise", "corporate" },
            ["education"] = new[] { "education", "learn", "study", "school", "course", "tutorial" },
            ["entertainment"] = new[] { "game", "entertainment", "fun", "music", "video", "movie" },
            ["finance"] = new[] { "finance", "money", "bank", "payment", "wallet", "crypto" },
            ["health"] = new[] { "health", "medical", "fitness", "exercise", "diet" },
            ["lifestyle"] = new[] { "lifestyle", "food", "recipe", "fashion", "travel" },
            ["news"] = new[] { "news", "media", "journal", "blog", "article" },
            ["productivity"] = new[] { "productivity", "todo", "task", "organize", "calendar", "note" },
            ["shopping"] = new[] { "shop", "store", "buy", "purchase", "ecommerce", "retail" },
            ["social"] = new[] { "social", "chat", "message", "community", "network", "friend" },
            ["utilities"] = new[] { "utility", "tool", "converter", "calculator", "weather" }
        };

        foreach (var (category, keywords) in categoryKeywords)
        {
            var score = keywords.Sum(keyword => text.Contains(keyword) ? 20.0 : 0.0);
            if (score > 0)
                categoryScores[category] = score;
        }

        if (!categoryScores.Any())
        {
            categoryScores["utilities"] = 10.0; // 기본값
        }

        var topCategory = categoryScores.OrderByDescending(kvp => kvp.Value).First();

        return new AppCategoryPrediction
        {
            PrimaryCategory = topCategory.Key,
            Confidence = Math.Min(80, topCategory.Value),
            CategoryScores = categoryScores,
            PredictionReasons = new List<string> { "이름과 설명 기반 키워드 분석" },
            StandardCategories = new List<string> { topCategory.Key }
        };
    }

    public async Task<ManifestValidationResult> ValidateManifestAsync(ManifestMetadata metadata)
    {
        var result = new ManifestValidationResult
        {
            RuleResults = new List<ValidationRule>()
        };

        // 기본 검증 규칙들
        var rules = new[]
        {
            new ValidationRule
            {
                Name = "name",
                Passed = !string.IsNullOrEmpty(metadata.Name),
                Severity = ValidationSeverity.Error,
                Message = "name 속성은 필수입니다",
                Recommendation = "앱의 이름을 name 속성에 지정하세요"
            },
            new ValidationRule
            {
                Name = "start_url",
                Passed = !string.IsNullOrEmpty(metadata.StartUrl),
                Severity = ValidationSeverity.Error,
                Message = "start_url 속성은 필수입니다",
                Recommendation = "앱의 시작 URL을 지정하세요"
            },
            new ValidationRule
            {
                Name = "icons",
                Passed = metadata.Icons.Any(),
                Severity = ValidationSeverity.Warning,
                Message = "아이콘이 정의되지 않았습니다",
                Recommendation = "최소한 192x192와 512x512 크기의 아이콘을 추가하세요"
            }
        };

        result.RuleResults.AddRange(rules);

        var errorCount = rules.Count(r => !r.Passed && r.Severity == ValidationSeverity.Error);
        var warningCount = rules.Count(r => !r.Passed && r.Severity == ValidationSeverity.Warning);

        result.IsValid = errorCount == 0;
        result.ValidationScore = ((double)(rules.Length - errorCount - warningCount * 0.5) / rules.Length) * 100;

        if (result.ValidationScore >= 90) result.ComplianceLevel = StandardComplianceLevel.FullCompliance;
        else if (result.ValidationScore >= 75) result.ComplianceLevel = StandardComplianceLevel.HighCompliance;
        else if (result.ValidationScore >= 50) result.ComplianceLevel = StandardComplianceLevel.MediumCompliance;
        else if (result.ValidationScore >= 25) result.ComplianceLevel = StandardComplianceLevel.LowCompliance;
        else result.ComplianceLevel = StandardComplianceLevel.NonCompliant;

        result.Errors = rules.Where(r => !r.Passed && r.Severity == ValidationSeverity.Error)
                            .Select(r => r.Message).ToList();
        result.Warnings = rules.Where(r => !r.Passed && r.Severity == ValidationSeverity.Warning)
                             .Select(r => r.Message).ToList();

        return result;
    }

    public IReadOnlyList<string> GetSupportedSpecVersions()
    {
        return new List<string> { "1.0" }.AsReadOnly();
    }

    public ManifestStatistics GetStatistics()
    {
        lock (_statsLock)
        {
            _statistics.LastUpdated = DateTimeOffset.UtcNow;
            return _statistics;
        }
    }

    // Private helper methods
    private async Task<string?> DownloadHtmlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to download HTML from {Url}", url);
        }
        return null;
    }

    private async Task<string?> DownloadManifestAsync(string manifestUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
            request.Headers.Add("User-Agent", "WebFlux-ManifestParser/1.0");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Manifest download exception: {Url}", manifestUrl);
            return null;
        }
    }

    private List<string> ExtractManifestUrls(string htmlContent, string baseUrl)
    {
        var urls = new List<string>();

        // <link rel="manifest" href="..."> 패턴 찾기
        var linkPattern = @"<link[^>]+rel=[""']manifest[""'][^>]*href=[""']([^""']+)[""'][^>]*>";
        var matches = Regex.Matches(htmlContent, linkPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var href = match.Groups[1].Value;
            var resolvedUrl = ResolveUrl(href, baseUrl);
            if (!string.IsNullOrEmpty(resolvedUrl))
                urls.Add(resolvedUrl);
        }

        return urls;
    }

    private string ResolveUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;

        try
        {
            var baseUri = new Uri(baseUrl);
            var resolvedUri = new Uri(baseUri, url);
            return resolvedUri.ToString();
        }
        catch
        {
            return url;
        }
    }

    private ManifestIcon? ParseIcon(JsonElement iconElement, string baseUrl)
    {
        try
        {
            if (!iconElement.TryGetProperty("src", out var srcElement))
                return null;

            var icon = new ManifestIcon
            {
                Src = ResolveUrl(srcElement.GetString(), baseUrl)
            };

            if (iconElement.TryGetProperty("sizes", out var sizesElement))
            {
                if (sizesElement.ValueKind == JsonValueKind.String)
                {
                    icon.Sizes.Add(sizesElement.GetString()!);
                }
                else if (sizesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sizeElement in sizesElement.EnumerateArray())
                    {
                        var size = sizeElement.GetString();
                        if (!string.IsNullOrEmpty(size))
                            icon.Sizes.Add(size);
                    }
                }
            }

            if (iconElement.TryGetProperty("type", out var typeElement))
                icon.Type = typeElement.GetString();

            if (iconElement.TryGetProperty("purpose", out var purposeElement))
            {
                var purposeString = purposeElement.GetString() ?? "any";
                var purposes = purposeString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var purpose in purposes)
                {
                    if (Enum.TryParse<IconPurpose>(purpose, true, out var iconPurpose))
                        icon.Purpose.Add(iconPurpose);
                }
            }

            if (icon.Purpose.Count == 0)
                icon.Purpose.Add(IconPurpose.Any);

            return icon;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse icon element");
            return null;
        }
    }

    private ManifestScreenshot? ParseScreenshot(JsonElement screenshotElement, string baseUrl)
    {
        try
        {
            if (!screenshotElement.TryGetProperty("src", out var srcElement))
                return null;

            var screenshot = new ManifestScreenshot
            {
                Src = ResolveUrl(srcElement.GetString(), baseUrl)
            };

            if (screenshotElement.TryGetProperty("sizes", out var sizesElement) && sizesElement.ValueKind == JsonValueKind.String)
                screenshot.Sizes.Add(sizesElement.GetString()!);

            if (screenshotElement.TryGetProperty("type", out var typeElement))
                screenshot.Type = typeElement.GetString();

            if (screenshotElement.TryGetProperty("form_factor", out var formFactorElement) &&
                Enum.TryParse<ScreenshotFormFactor>(formFactorElement.GetString(), true, out var formFactor))
                screenshot.FormFactor = formFactor;

            if (screenshotElement.TryGetProperty("label", out var labelElement))
                screenshot.Label = labelElement.GetString();

            return screenshot;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse screenshot element");
            return null;
        }
    }

    private RelatedApplication? ParseRelatedApplication(JsonElement appElement)
    {
        try
        {
            if (!appElement.TryGetProperty("platform", out var platformElement))
                return null;

            var app = new RelatedApplication
            {
                Platform = platformElement.GetString() ?? string.Empty
            };

            if (appElement.TryGetProperty("url", out var urlElement))
                app.Url = urlElement.GetString();

            if (appElement.TryGetProperty("id", out var idElement))
                app.Id = idElement.GetString();

            if (appElement.TryGetProperty("min_version", out var minVersionElement))
                app.MinVersion = minVersionElement.GetString();

            return app;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse related application element");
            return null;
        }
    }

    private ManifestShortcut? ParseShortcut(JsonElement shortcutElement, string baseUrl)
    {
        try
        {
            if (!shortcutElement.TryGetProperty("name", out var nameElement) ||
                !shortcutElement.TryGetProperty("url", out var urlElement))
                return null;

            var shortcut = new ManifestShortcut
            {
                Name = nameElement.GetString() ?? string.Empty,
                Url = ResolveUrl(urlElement.GetString(), baseUrl)
            };

            if (shortcutElement.TryGetProperty("short_name", out var shortNameElement))
                shortcut.ShortName = shortNameElement.GetString();

            if (shortcutElement.TryGetProperty("description", out var descElement))
                shortcut.Description = descElement.GetString();

            if (shortcutElement.TryGetProperty("icons", out var iconsElement) && iconsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var iconElement in iconsElement.EnumerateArray())
                {
                    var icon = ParseIcon(iconElement, baseUrl);
                    if (icon != null)
                        shortcut.Icons.Add(icon);
                }
            }

            return shortcut;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse shortcut element");
            return null;
        }
    }

    private ShareTarget? ParseShareTarget(JsonElement shareTargetElement, string baseUrl)
    {
        try
        {
            if (!shareTargetElement.TryGetProperty("action", out var actionElement))
                return null;

            var shareTarget = new ShareTarget
            {
                Action = ResolveUrl(actionElement.GetString(), baseUrl)
            };

            if (shareTargetElement.TryGetProperty("method", out var methodElement))
                shareTarget.Method = methodElement.GetString() ?? "GET";

            if (shareTargetElement.TryGetProperty("enctype", out var enctypeElement))
                shareTarget.Enctype = enctypeElement.GetString() ?? "application/x-www-form-urlencoded";

            if (shareTargetElement.TryGetProperty("params", out var paramsElement))
            {
                shareTarget.Params = new ShareTargetParams();

                if (paramsElement.TryGetProperty("title", out var titleElement))
                    shareTarget.Params.Title = titleElement.GetString();

                if (paramsElement.TryGetProperty("text", out var textElement))
                    shareTarget.Params.Text = textElement.GetString();

                if (paramsElement.TryGetProperty("url", out var urlParamElement))
                    shareTarget.Params.Url = urlParamElement.GetString();
            }

            return shareTarget;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse share target element");
            return null;
        }
    }

    private async Task<ManifestParseResult> CreateParseResultAsync(string manifestUrl, ManifestMetadata metadata, string rawContent)
    {
        var result = new ManifestParseResult
        {
            IsSuccess = true,
            FileFound = true,
            ManifestUrl = manifestUrl,
            Metadata = metadata,
            RawContent = rawContent,
            ParsedAt = DateTimeOffset.UtcNow
        };

        // PWA 호환성 평가 수행
        result.PwaCompatibility = await EvaluatePwaCompatibilityAsync(metadata);
        result.IconAnalysis = await AnalyzeIconsAsync(metadata);

        return result;
    }

    private async Task UpdateStatisticsAsync(ManifestParseResult result, TimeSpan parseTime)
    {
        lock (_statsLock)
        {
            if (result.IsSuccess)
            {
                _statistics.SuccessfulParses++;
                if (result.FileFound)
                    _statistics.SitesWithManifest++;

                if (result.PwaCompatibility?.MeetsMinimumRequirements == true)
                    _statistics.PwaCompatibleSites++;

                if (result.Metadata?.Display != null)
                {
                    if (_statistics.DisplayModeDistribution.ContainsKey(result.Metadata.Display.Value))
                        _statistics.DisplayModeDistribution[result.Metadata.Display.Value]++;
                    else
                        _statistics.DisplayModeDistribution[result.Metadata.Display.Value] = 1;
                }

                if (result.Metadata?.Categories != null)
                {
                    foreach (var category in result.Metadata.Categories)
                    {
                        if (_statistics.CategoryDistribution.ContainsKey(category))
                            _statistics.CategoryDistribution[category]++;
                        else
                            _statistics.CategoryDistribution[category] = 1;
                    }
                }

                var parseTimeMs = parseTime.TotalMilliseconds;
                _statistics.AverageParseTime = (_statistics.AverageParseTime * (_statistics.TotalParseAttempts - 1) + parseTimeMs) / _statistics.TotalParseAttempts;

                if (result.PwaCompatibility != null)
                {
                    var currentAvg = _statistics.AveragePwaScore;
                    var count = _statistics.PwaCompatibleSites + (_statistics.SitesWithManifest - _statistics.PwaCompatibleSites);
                    _statistics.AveragePwaScore = (currentAvg * (count - 1) + result.PwaCompatibility.OverallScore) / count;
                }
            }
        }
    }

    private async Task CacheResultAsync(string baseUrl, ManifestParseResult result)
    {
        lock (_cacheLock)
        {
            _cache[baseUrl] = result;
        }
    }
}