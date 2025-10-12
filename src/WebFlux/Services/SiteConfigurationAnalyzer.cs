using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// YamlDotNet 기반 사이트 설정 분석기
/// Jekyll, Hugo, Next.js 등의 정적 사이트 생성기 설정 분석
/// 90% 설정 품질 목표 달성을 위한 고급 분석 기능 제공
/// </summary>
public class SiteConfigurationAnalyzer : ISiteConfigurationAnalyzer
{
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

    private static readonly Dictionary<string, SiteConfigurationType> FileNamePatterns = new()
    {
        { "_config.yml", SiteConfigurationType.Jekyll },
        { "_config.yaml", SiteConfigurationType.Jekyll },
        { "config.yml", SiteConfigurationType.Hugo },
        { "config.yaml", SiteConfigurationType.Hugo },
        { "hugo.yml", SiteConfigurationType.Hugo },
        { "hugo.yaml", SiteConfigurationType.Hugo },
        { ".gitbook.yaml", SiteConfigurationType.GitBook },
        { ".gitbook.yml", SiteConfigurationType.GitBook },
        { "docusaurus.config.js", SiteConfigurationType.Docusaurus },
        { "gatsby-config.js", SiteConfigurationType.Gatsby },
        { "next.config.js", SiteConfigurationType.NextJs }
    };

    public SiteConfigurationAnalyzer()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// YAML 설정 파일을 분석합니다
    /// </summary>
    public Task<SiteConfiguration> AnalyzeConfigurationAsync(
        string yamlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        if (yamlContent == null)
            throw new ArgumentNullException(nameof(yamlContent));
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new ArgumentException("YAML content cannot be empty", nameof(yamlContent));

        try
        {
            // YAML 파싱
            var parsedYaml = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

            // 설정 타입 감지
            var configType = DetectConfigurationType(yamlContent, ExtractFileNameFromUrl(sourceUrl));

            // 구조화된 설정 추출
            var siteInfo = ExtractSiteInfo(parsedYaml);
            var buildConfig = ExtractBuildConfiguration(parsedYaml);
            var contentConfig = ExtractContentConfiguration(parsedYaml);
            var pluginConfig = ExtractPluginConfiguration(parsedYaml);
            var deploymentConfig = ExtractDeploymentConfiguration(parsedYaml);
            var seoConfig = ExtractSeoConfiguration(parsedYaml);
            var performanceConfig = ExtractPerformanceConfiguration(parsedYaml);

            var configuration = new SiteConfiguration
            {
                SourceUrl = sourceUrl,
                ConfigType = configType,
                SiteInfo = siteInfo,
                BuildConfig = buildConfig,
                ContentConfig = contentConfig,
                PluginConfig = pluginConfig,
                DeploymentConfig = deploymentConfig,
                SeoConfig = seoConfig,
                PerformanceConfig = performanceConfig,
                RawYaml = yamlContent,
                ParsedYaml = parsedYaml
            };

            // 품질 평가 및 문제점 검출
            var issues = ValidateConfiguration(configuration);
            var qualityAssessment = AssessQuality(configuration);

            return Task.FromResult(new SiteConfiguration
            {
                SourceUrl = configuration.SourceUrl,
                ConfigType = configuration.ConfigType,
                SiteInfo = configuration.SiteInfo,
                BuildConfig = configuration.BuildConfig,
                ContentConfig = configuration.ContentConfig,
                PluginConfig = configuration.PluginConfig,
                DeploymentConfig = configuration.DeploymentConfig,
                SeoConfig = configuration.SeoConfig,
                PerformanceConfig = configuration.PerformanceConfig,
                RawYaml = configuration.RawYaml,
                ParsedYaml = configuration.ParsedYaml,
                QualityScore = qualityAssessment.OverallScore,
                Issues = issues
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to analyze YAML configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 설정 파일 타입을 감지합니다
    /// </summary>
    public SiteConfigurationType DetectConfigurationType(string yamlContent, string? fileName = null)
    {
        // 파일명 기반 감지
        if (!string.IsNullOrEmpty(fileName))
        {
            var lowerFileName = fileName.ToLowerInvariant();
            foreach (var pattern in FileNamePatterns)
            {
                if (lowerFileName.Contains(pattern.Key.ToLowerInvariant()))
                {
                    return pattern.Value;
                }
            }
        }

        // 콘텐츠 기반 감지
        return DetectConfigurationTypeFromContent(yamlContent);
    }

    /// <summary>
    /// 설정 품질을 평가합니다
    /// </summary>
    public ConfigurationQualityAssessment AssessQuality(SiteConfiguration configuration)
    {
        var completenessScore = CalculateCompletenessScore(configuration);
        var securityScore = CalculateSecurityScore(configuration);
        var performanceScore = CalculatePerformanceScore(configuration);
        var seoScore = CalculateSeoScore(configuration);
        var bestPracticesScore = CalculateBestPracticesScore(configuration);

        var overallScore = (completenessScore + securityScore + performanceScore + seoScore + bestPracticesScore) / 5.0;

        var issueSummary = CalculateIssueSummary(configuration.Issues);

        return new ConfigurationQualityAssessment
        {
            OverallScore = overallScore,
            CompletenessScore = completenessScore,
            SecurityScore = securityScore,
            PerformanceScore = performanceScore,
            SeoScore = seoScore,
            BestPracticesScore = bestPracticesScore,
            IssueSummary = issueSummary
        };
    }

    /// <summary>
    /// 설정을 검증하고 문제점을 찾습니다
    /// </summary>
    public IReadOnlyList<ConfigurationIssue> ValidateConfiguration(SiteConfiguration configuration)
    {
        var issues = new List<ConfigurationIssue>();

        // 필수 설정 검증
        ValidateRequiredSettings(configuration, issues);

        // 보안 설정 검증
        ValidateSecuritySettings(configuration, issues);

        // 성능 설정 검증
        ValidatePerformanceSettings(configuration, issues);

        // SEO 설정 검증
        ValidateSeoSettings(configuration, issues);

        // 호환성 검증
        ValidateCompatibility(configuration, issues);

        return issues.AsReadOnly();
    }

    /// <summary>
    /// 설정 최적화 권장사항을 제공합니다
    /// </summary>
    public IReadOnlyList<ConfigurationRecommendation> GetOptimizationRecommendations(SiteConfiguration configuration)
    {
        var recommendations = new List<ConfigurationRecommendation>();

        // 성능 최적화 권장사항
        AddPerformanceRecommendations(configuration, recommendations);

        // 보안 강화 권장사항
        AddSecurityRecommendations(configuration, recommendations);

        // SEO 최적화 권장사항
        AddSeoRecommendations(configuration, recommendations);

        // 모범 사례 권장사항
        AddBestPracticeRecommendations(configuration, recommendations);

        return recommendations.AsReadOnly();
    }

    /// <summary>
    /// YAML을 특정 사이트 생성기 형식으로 변환합니다
    /// </summary>
    public string ConvertToFormat(SiteConfiguration configuration, SiteConfigurationType targetType)
    {
        var convertedConfig = ConvertConfigurationFormat(configuration, targetType);
        return _serializer.Serialize(convertedConfig);
    }

    /// <summary>
    /// 설정 마이그레이션 가이드를 생성합니다
    /// </summary>
    public ConfigurationMigrationGuide GenerateMigrationGuide(
        SiteConfiguration sourceConfig,
        SiteConfigurationType targetType)
    {
        var compatibilityScore = CalculateCompatibilityScore(sourceConfig.ConfigType, targetType);
        var steps = GenerateMigrationSteps(sourceConfig.ConfigType, targetType);
        var convertedConfig = ConvertToFormat(sourceConfig, targetType);
        var incompatibleSettings = FindIncompatibleSettings(sourceConfig, targetType);
        var additionalTasks = GenerateAdditionalTasks(sourceConfig.ConfigType, targetType);

        return new ConfigurationMigrationGuide
        {
            SourceType = sourceConfig.ConfigType,
            TargetType = targetType,
            CompatibilityScore = compatibilityScore,
            Steps = steps,
            ConvertedConfiguration = convertedConfig,
            IncompatibleSettings = incompatibleSettings,
            AdditionalTasks = additionalTasks
        };
    }

    #region Private Helper Methods

    private SiteConfigurationType DetectConfigurationTypeFromContent(string yamlContent)
    {
        var content = yamlContent.ToLowerInvariant();

        // Jekyll 특성 감지
        if (content.Contains("jekyll") || content.Contains("permalink:") || content.Contains("gems:"))
        {
            return SiteConfigurationType.Jekyll;
        }

        // Hugo 특성 감지
        if (content.Contains("hugo") || content.Contains("baseurl:") || content.Contains("languagecode:"))
        {
            return SiteConfigurationType.Hugo;
        }

        // GitBook 특성 감지
        if (content.Contains("gitbook") || content.Contains("structure:"))
        {
            return SiteConfigurationType.GitBook;
        }

        return SiteConfigurationType.Generic;
    }

    private string? ExtractFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }
        catch
        {
            return null;
        }
    }

    private SiteConfigurationInfo ExtractSiteInfo(Dictionary<string, object> parsedYaml)
    {
        return new SiteConfigurationInfo
        {
            Title = GetStringValue(parsedYaml, "title"),
            Description = GetStringValue(parsedYaml, "description"),
            Url = GetStringValue(parsedYaml, "url"),
            BaseUrl = GetStringValue(parsedYaml, "baseurl"),
            Author = ExtractAuthorInfo(parsedYaml),
            Language = GetStringValue(parsedYaml, "lang") ?? GetStringValue(parsedYaml, "language"),
            Timezone = GetStringValue(parsedYaml, "timezone"),
            Encoding = GetStringValue(parsedYaml, "encoding")
        };
    }

    private AuthorInfo? ExtractAuthorInfo(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("author", out var authorObj))
            return null;

        if (authorObj is string authorString)
        {
            return new AuthorInfo { Name = authorString };
        }

        var authorDict = ConvertToStringDictionary(authorObj);
        if (authorDict != null)
        {
            return new AuthorInfo
            {
                Name = GetStringValue(authorDict, "name"),
                Email = GetStringValue(authorDict, "email"),
                Url = GetStringValue(authorDict, "url"),
                Social = ExtractSocialMedia(authorDict)
            };
        }

        return null;
    }

    private Dictionary<string, string> ExtractSocialMedia(Dictionary<string, object> authorDict)
    {
        var social = new Dictionary<string, string>();

        if (authorDict.TryGetValue("social", out var socialObj))
        {
            var socialDict = ConvertToStringDictionary(socialObj);
            if (socialDict != null)
            {
                foreach (var kvp in socialDict)
                {
                    if (kvp.Value?.ToString() is string value)
                    {
                        social[kvp.Key] = value;
                    }
                }
            }
        }

        return social;
    }

    private BuildConfiguration ExtractBuildConfiguration(Dictionary<string, object> parsedYaml)
    {
        return new BuildConfiguration
        {
            SourceDirectory = GetStringValue(parsedYaml, "source"),
            OutputDirectory = GetStringValue(parsedYaml, "destination"),
            ExcludePatterns = GetStringArrayValue(parsedYaml, "exclude"),
            IncludePatterns = GetStringArrayValue(parsedYaml, "include"),
            Environment = GetStringValue(parsedYaml, "environment"),
            IncrementalBuild = GetBoolValue(parsedYaml, "incremental"),
            ShowDrafts = GetBoolValue(parsedYaml, "show_drafts"),
            ShowFuture = GetBoolValue(parsedYaml, "future")
        };
    }

    private ContentConfiguration ExtractContentConfiguration(Dictionary<string, object> parsedYaml)
    {
        return new ContentConfiguration
        {
            MarkdownEngine = GetStringValue(parsedYaml, "markdown"),
            Highlighter = GetStringValue(parsedYaml, "highlighter"),
            PermalinkFormat = GetStringValue(parsedYaml, "permalink"),
            PaginateCount = GetIntValue(parsedYaml, "paginate"),
            PaginatePath = GetStringValue(parsedYaml, "paginate_path"),
            ExcerptSeparator = GetStringValue(parsedYaml, "excerpt_separator"),
            Collections = ExtractCollections(parsedYaml),
            Defaults = ExtractDefaults(parsedYaml)
        };
    }

    private Dictionary<string, CollectionConfig> ExtractCollections(Dictionary<string, object> parsedYaml)
    {
        var collections = new Dictionary<string, CollectionConfig>();

        if (parsedYaml.TryGetValue("collections", out var collectionsObj))
        {
            var collectionsDict = ConvertToStringDictionary(collectionsObj);
            if (collectionsDict != null)
            {
                foreach (var kvp in collectionsDict)
                {
                    var collectionDict = ConvertToStringDictionary(kvp.Value);
                    if (collectionDict != null)
                    {
                        collections[kvp.Key] = new CollectionConfig
                        {
                            Output = GetBoolValue(collectionDict, "output"),
                            Permalink = GetStringValue(collectionDict, "permalink")
                        };
                    }
                }
            }
        }

        return collections;
    }

    private IReadOnlyList<DefaultConfig> ExtractDefaults(Dictionary<string, object> parsedYaml)
    {
        var defaults = new List<DefaultConfig>();

        if (parsedYaml.TryGetValue("defaults", out var defaultsObj) &&
            defaultsObj is IEnumerable<object> defaultsList)
        {
            foreach (var item in defaultsList)
            {
                if (item is Dictionary<string, object> defaultDict)
                {
                    var scope = ExtractDefaultScope(defaultDict);
                    var values = GetDictionaryValue(defaultDict, "values");

                    defaults.Add(new DefaultConfig
                    {
                        Scope = scope,
                        Values = values
                    });
                }
            }
        }

        return defaults.AsReadOnly();
    }

    private DefaultScope? ExtractDefaultScope(Dictionary<string, object> defaultDict)
    {
        if (defaultDict.TryGetValue("scope", out var scopeObj) &&
            scopeObj is Dictionary<string, object> scopeDict)
        {
            return new DefaultScope
            {
                Path = GetStringValue(scopeDict, "path"),
                Type = GetStringValue(scopeDict, "type")
            };
        }

        return null;
    }

    private PluginConfiguration ExtractPluginConfiguration(Dictionary<string, object> parsedYaml)
    {
        return new PluginConfiguration
        {
            Plugins = GetStringArrayValue(parsedYaml, "plugins"),
            Gems = GetStringArrayValue(parsedYaml, "gems"),
            PluginSettings = ExtractPluginSettings(parsedYaml)
        };
    }

    private Dictionary<string, object> ExtractPluginSettings(Dictionary<string, object> parsedYaml)
    {
        var settings = new Dictionary<string, object>();

        // 플러그인별 설정 추출
        var knownPluginKeys = new[] { "sass", "kramdown", "rouge", "jekyll-feed", "jekyll-sitemap" };

        foreach (var key in knownPluginKeys)
        {
            if (parsedYaml.TryGetValue(key, out var value))
            {
                settings[key] = value;
            }
        }

        return settings;
    }

    private DeploymentConfiguration ExtractDeploymentConfiguration(Dictionary<string, object> parsedYaml)
    {
        return new DeploymentConfiguration
        {
            Target = DetectDeploymentTarget(parsedYaml),
            GitHubPages = ExtractGitHubPagesConfig(parsedYaml),
            Netlify = ExtractNetlifyConfig(parsedYaml),
            Vercel = ExtractVercelConfig(parsedYaml)
        };
    }

    private string? DetectDeploymentTarget(Dictionary<string, object> parsedYaml)
    {
        if (parsedYaml.ContainsKey("github") || parsedYaml.ContainsKey("repository"))
            return "GitHub Pages";

        if (parsedYaml.ContainsKey("netlify"))
            return "Netlify";

        if (parsedYaml.ContainsKey("vercel"))
            return "Vercel";

        return null;
    }

    private GitHubPagesConfig? ExtractGitHubPagesConfig(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("github", out var githubObj))
            return null;

        var githubDict = ConvertToStringDictionary(githubObj);
        if (githubDict == null)
            return null;

        return new GitHubPagesConfig
        {
            Branch = GetStringValue(githubDict, "branch"),
            Folder = GetStringValue(githubDict, "folder"),
            CustomDomain = GetStringValue(githubDict, "custom_domain")
        };
    }

    private NetlifyConfig? ExtractNetlifyConfig(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("netlify", out var netlifyObj))
            return null;

        var netlifyDict = ConvertToStringDictionary(netlifyObj);
        if (netlifyDict == null)
            return null;

        return new NetlifyConfig
        {
            BuildCommand = GetStringValue(netlifyDict, "build_command"),
            PublishDirectory = GetStringValue(netlifyDict, "publish_directory"),
            Environment = GetStringDictionaryValue(netlifyDict, "environment")
        };
    }

    private VercelConfig? ExtractVercelConfig(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("vercel", out var vercelObj))
            return null;

        var vercelDict = ConvertToStringDictionary(vercelObj);
        if (vercelDict == null)
            return null;

        return new VercelConfig
        {
            BuildCommand = GetStringValue(vercelDict, "build_command"),
            OutputDirectory = GetStringValue(vercelDict, "output_directory"),
            Functions = GetDictionaryValue(vercelDict, "functions")
        };
    }

    private SeoConfiguration ExtractSeoConfiguration(Dictionary<string, object> parsedYaml)
    {
        return new SeoConfiguration
        {
            GoogleAnalytics = GetStringValue(parsedYaml, "google_analytics"),
            GoogleTagManager = GetStringValue(parsedYaml, "google_tag_manager"),
            GenerateSitemap = GetBoolValue(parsedYaml, "sitemap", true),
            RobotsConfig = GetStringValue(parsedYaml, "robots"),
            SocialMedia = ExtractSeoSocialMedia(parsedYaml)
        };
    }

    private SocialMediaConfig? ExtractSeoSocialMedia(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("social", out var socialObj))
            return null;

        var socialDict = ConvertToStringDictionary(socialObj);
        if (socialDict == null)
            return null;

        return new SocialMediaConfig
        {
            Twitter = GetStringValue(socialDict, "twitter"),
            Facebook = GetStringValue(socialDict, "facebook"),
            LinkedIn = GetStringValue(socialDict, "linkedin"),
            GitHub = GetStringValue(socialDict, "github")
        };
    }

    private SitePerformanceConfiguration ExtractPerformanceConfiguration(Dictionary<string, object> parsedYaml)
    {
        return new SitePerformanceConfiguration
        {
            CompressHtml = GetBoolValue(parsedYaml, "compress_html"),
            Sass = ExtractSassConfig(parsedYaml),
            Cache = ExtractCacheConfig(parsedYaml),
            Cdn = ExtractCdnConfig(parsedYaml)
        };
    }

    private SassConfig? ExtractSassConfig(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("sass", out var sassObj))
            return null;

        var sassDict = ConvertToStringDictionary(sassObj);
        if (sassDict == null)
            return null;

        return new SassConfig
        {
            Style = GetStringValue(sassDict, "style"),
            Directory = GetStringValue(sassDict, "sass_dir")
        };
    }

    private CacheConfig? ExtractCacheConfig(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("cache", out var cacheObj))
            return null;

        var cacheDict = ConvertToStringDictionary(cacheObj);
        if (cacheDict == null)
            return null;

        return new CacheConfig
        {
            TtlSeconds = GetIntValue(cacheDict, "ttl_seconds"),
            Type = GetStringValue(cacheDict, "type")
        };
    }

    private CdnConfig? ExtractCdnConfig(Dictionary<string, object> parsedYaml)
    {
        if (!parsedYaml.TryGetValue("cdn", out var cdnObj))
            return null;

        var cdnDict = ConvertToStringDictionary(cdnObj);
        if (cdnDict == null)
            return null;

        return new CdnConfig
        {
            Url = GetStringValue(cdnDict, "url"),
            AssetsUrl = GetStringValue(cdnDict, "assets_url")
        };
    }

    // Validation Methods
    private void ValidateRequiredSettings(SiteConfiguration configuration, List<ConfigurationIssue> issues)
    {
        if (string.IsNullOrEmpty(configuration.SiteInfo.Title))
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.MissingRequired,
                Severity = ConfigurationIssueSeverity.Error,
                Message = "사이트 제목이 설정되지 않았습니다",
                AffectedKey = "title",
                Recommendation = "사이트의 제목을 설정하여 SEO와 사용자 경험을 개선하세요"
            });
        }

        if (string.IsNullOrEmpty(configuration.SiteInfo.Description))
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.MissingRequired,
                Severity = ConfigurationIssueSeverity.Warning,
                Message = "사이트 설명이 설정되지 않았습니다",
                AffectedKey = "description",
                Recommendation = "사이트 설명을 추가하여 검색 엔진 최적화를 개선하세요"
            });
        }
    }

    private void ValidateSecuritySettings(SiteConfiguration configuration, List<ConfigurationIssue> issues)
    {
        // 개발 환경에서 프로덕션 배포 확인
        if (configuration.BuildConfig.Environment == "development" &&
            !string.IsNullOrEmpty(configuration.DeploymentConfig.Target))
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Security,
                Severity = ConfigurationIssueSeverity.Critical,
                Message = "개발 환경 설정으로 프로덕션에 배포하려고 합니다",
                AffectedKey = "environment",
                Recommendation = "프로덕션 배포 시 환경을 'production'으로 설정하세요"
            });
        }

        // 드래프트 노출 확인
        if (configuration.BuildConfig.ShowDrafts && configuration.BuildConfig.Environment == "production")
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Security,
                Severity = ConfigurationIssueSeverity.Warning,
                Message = "프로덕션 환경에서 드래프트가 노출됩니다",
                AffectedKey = "show_drafts",
                Recommendation = "프로덕션에서는 show_drafts를 false로 설정하세요"
            });
        }
    }

    private void ValidatePerformanceSettings(SiteConfiguration configuration, List<ConfigurationIssue> issues)
    {
        // HTML 압축 미사용
        if (!configuration.PerformanceConfig.CompressHtml)
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Performance,
                Severity = ConfigurationIssueSeverity.Info,
                Message = "HTML 압축이 비활성화되어 있습니다",
                AffectedKey = "compress_html",
                Recommendation = "HTML 압축을 활성화하여 로딩 속도를 개선하세요"
            });
        }

        // Sass 최적화 미설정
        if (configuration.PerformanceConfig.Sass?.Style != "compressed")
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Performance,
                Severity = ConfigurationIssueSeverity.Info,
                Message = "CSS가 압축되지 않습니다",
                AffectedKey = "sass.style",
                Recommendation = "Sass 스타일을 'compressed'로 설정하여 CSS 크기를 줄이세요"
            });
        }
    }

    private void ValidateSeoSettings(SiteConfiguration configuration, List<ConfigurationIssue> issues)
    {
        // Google Analytics 미설정
        if (string.IsNullOrEmpty(configuration.SeoConfig.GoogleAnalytics))
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Seo,
                Severity = ConfigurationIssueSeverity.Info,
                Message = "Google Analytics가 설정되지 않았습니다",
                AffectedKey = "google_analytics",
                Recommendation = "웹사이트 분석을 위해 Google Analytics를 설정하세요"
            });
        }

        // 사이트맵 미생성
        if (!configuration.SeoConfig.GenerateSitemap)
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Seo,
                Severity = ConfigurationIssueSeverity.Warning,
                Message = "사이트맵이 생성되지 않습니다",
                AffectedKey = "sitemap",
                Recommendation = "검색 엔진 최적화를 위해 사이트맵 생성을 활성화하세요"
            });
        }
    }

    private void ValidateCompatibility(SiteConfiguration configuration, List<ConfigurationIssue> issues)
    {
        // 구버전 플러그인 확인
        var deprecatedPlugins = new[] { "jekyll-paginate", "redcarpet" };
        var usedDeprecatedPlugins = configuration.PluginConfig.Plugins
            .Intersect(deprecatedPlugins, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var plugin in usedDeprecatedPlugins)
        {
            issues.Add(new ConfigurationIssue
            {
                Type = ConfigurationIssueType.Deprecated,
                Severity = ConfigurationIssueSeverity.Warning,
                Message = $"구버전 플러그인 '{plugin}'을 사용하고 있습니다",
                AffectedKey = "plugins",
                Recommendation = $"'{plugin}' 플러그인을 최신 대안으로 교체하세요"
            });
        }
    }

    // Quality Assessment Methods
    private double CalculateCompletenessScore(SiteConfiguration configuration)
    {
        var score = 0.0;
        var maxScore = 10.0;

        if (!string.IsNullOrEmpty(configuration.SiteInfo.Title)) score += 2.0;
        if (!string.IsNullOrEmpty(configuration.SiteInfo.Description)) score += 2.0;
        if (!string.IsNullOrEmpty(configuration.SiteInfo.Url)) score += 1.0;
        if (configuration.SiteInfo.Author != null) score += 1.0;
        if (!string.IsNullOrEmpty(configuration.ContentConfig.PermalinkFormat)) score += 1.0;
        if (configuration.PluginConfig.Plugins.Any()) score += 1.0;
        if (!string.IsNullOrEmpty(configuration.SeoConfig.GoogleAnalytics)) score += 1.0;
        if (configuration.SeoConfig.GenerateSitemap) score += 1.0;

        return Math.Min(score / maxScore, 1.0);
    }

    private double CalculateSecurityScore(SiteConfiguration configuration)
    {
        var score = 1.0;

        // 보안 문제가 있으면 점수 차감
        if (configuration.BuildConfig.Environment == "development" &&
            !string.IsNullOrEmpty(configuration.DeploymentConfig.Target))
        {
            score -= 0.3;
        }

        if (configuration.BuildConfig.ShowDrafts &&
            configuration.BuildConfig.Environment == "production")
        {
            score -= 0.2;
        }

        return Math.Max(score, 0.0);
    }

    private double CalculatePerformanceScore(SiteConfiguration configuration)
    {
        var score = 0.0;
        var maxScore = 4.0;

        if (configuration.PerformanceConfig.CompressHtml) score += 1.0;
        if (configuration.PerformanceConfig.Sass?.Style == "compressed") score += 1.0;
        if (configuration.PerformanceConfig.Cache != null) score += 1.0;
        if (configuration.PerformanceConfig.Cdn != null) score += 1.0;

        return score / maxScore;
    }

    private double CalculateSeoScore(SiteConfiguration configuration)
    {
        var score = 0.0;
        var maxScore = 6.0;

        if (!string.IsNullOrEmpty(configuration.SiteInfo.Title)) score += 1.0;
        if (!string.IsNullOrEmpty(configuration.SiteInfo.Description)) score += 1.0;
        if (!string.IsNullOrEmpty(configuration.SeoConfig.GoogleAnalytics)) score += 1.0;
        if (configuration.SeoConfig.GenerateSitemap) score += 1.0;
        if (configuration.SeoConfig.SocialMedia != null) score += 1.0;
        if (!string.IsNullOrEmpty(configuration.SiteInfo.Language)) score += 1.0;

        return score / maxScore;
    }

    private double CalculateBestPracticesScore(SiteConfiguration configuration)
    {
        var score = 1.0;

        // 구버전 플러그인 사용 시 점수 차감
        var deprecatedPlugins = new[] { "jekyll-paginate", "redcarpet" };
        var deprecatedCount = configuration.PluginConfig.Plugins
            .Count(p => deprecatedPlugins.Contains(p, StringComparer.OrdinalIgnoreCase));

        score -= deprecatedCount * 0.1;

        return Math.Max(score, 0.0);
    }

    private ConfigurationIssueSummary CalculateIssueSummary(IReadOnlyList<ConfigurationIssue> issues)
    {
        return new ConfigurationIssueSummary
        {
            TotalIssues = issues.Count,
            CriticalIssues = issues.Count(i => i.Severity == ConfigurationIssueSeverity.Critical),
            ErrorIssues = issues.Count(i => i.Severity == ConfigurationIssueSeverity.Error),
            WarningIssues = issues.Count(i => i.Severity == ConfigurationIssueSeverity.Warning),
            InfoIssues = issues.Count(i => i.Severity == ConfigurationIssueSeverity.Info)
        };
    }

    // Recommendation Methods
    private void AddPerformanceRecommendations(SiteConfiguration configuration, List<ConfigurationRecommendation> recommendations)
    {
        if (!configuration.PerformanceConfig.CompressHtml)
        {
            recommendations.Add(new ConfigurationRecommendation
            {
                Type = ConfigurationRecommendationType.Performance,
                Priority = RecommendationPriority.Medium,
                Title = "HTML 압축 활성화",
                Description = "HTML 압축을 활성화하여 페이지 로딩 속도를 개선할 수 있습니다",
                CurrentValue = "false",
                RecommendedValue = "true",
                Implementation = "compress_html: true",
                ExpectedBenefit = "파일 크기 20-30% 감소, 로딩 속도 개선"
            });
        }

        if (configuration.PerformanceConfig.Sass?.Style != "compressed")
        {
            recommendations.Add(new ConfigurationRecommendation
            {
                Type = ConfigurationRecommendationType.Performance,
                Priority = RecommendationPriority.Medium,
                Title = "CSS 압축 설정",
                Description = "Sass 출력을 압축하여 CSS 파일 크기를 줄일 수 있습니다",
                CurrentValue = configuration.PerformanceConfig.Sass?.Style ?? "expanded",
                RecommendedValue = "compressed",
                Implementation = "sass:\n  style: compressed",
                ExpectedBenefit = "CSS 파일 크기 40-60% 감소"
            });
        }
    }

    private void AddSecurityRecommendations(SiteConfiguration configuration, List<ConfigurationRecommendation> recommendations)
    {
        if (configuration.BuildConfig.ShowDrafts && configuration.BuildConfig.Environment == "production")
        {
            recommendations.Add(new ConfigurationRecommendation
            {
                Type = ConfigurationRecommendationType.Security,
                Priority = RecommendationPriority.High,
                Title = "프로덕션에서 드래프트 숨김",
                Description = "프로덕션 환경에서는 드래프트 콘텐츠를 노출하지 않아야 합니다",
                CurrentValue = "true",
                RecommendedValue = "false",
                Implementation = "show_drafts: false",
                ExpectedBenefit = "미완성 콘텐츠 노출 방지, 보안 강화"
            });
        }
    }

    private void AddSeoRecommendations(SiteConfiguration configuration, List<ConfigurationRecommendation> recommendations)
    {
        if (string.IsNullOrEmpty(configuration.SeoConfig.GoogleAnalytics))
        {
            recommendations.Add(new ConfigurationRecommendation
            {
                Type = ConfigurationRecommendationType.Seo,
                Priority = RecommendationPriority.Medium,
                Title = "Google Analytics 설정",
                Description = "웹사이트 트래픽 분석을 위해 Google Analytics를 설정하세요",
                CurrentValue = null,
                RecommendedValue = "GA_TRACKING_ID",
                Implementation = "google_analytics: GA_TRACKING_ID",
                ExpectedBenefit = "방문자 분석, 사이트 개선 인사이트 확보"
            });
        }

        if (!configuration.SeoConfig.GenerateSitemap)
        {
            recommendations.Add(new ConfigurationRecommendation
            {
                Type = ConfigurationRecommendationType.Seo,
                Priority = RecommendationPriority.High,
                Title = "사이트맵 생성 활성화",
                Description = "검색 엔진이 사이트를 더 잘 인덱싱할 수 있도록 사이트맵을 생성하세요",
                CurrentValue = "false",
                RecommendedValue = "true",
                Implementation = "sitemap: true",
                ExpectedBenefit = "검색 엔진 최적화, 검색 결과 노출 개선"
            });
        }
    }

    private void AddBestPracticeRecommendations(SiteConfiguration configuration, List<ConfigurationRecommendation> recommendations)
    {
        var deprecatedPlugins = configuration.PluginConfig.Plugins
            .Where(p => new[] { "jekyll-paginate", "redcarpet" }.Contains(p, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var plugin in deprecatedPlugins)
        {
            var modernAlternative = plugin.ToLowerInvariant() switch
            {
                "jekyll-paginate" => "jekyll-paginate-v2",
                "redcarpet" => "kramdown",
                _ => "최신 버전"
            };

            recommendations.Add(new ConfigurationRecommendation
            {
                Type = ConfigurationRecommendationType.BestPractice,
                Priority = RecommendationPriority.Medium,
                Title = $"구버전 플러그인 교체: {plugin}",
                Description = $"{plugin}은 더 이상 권장되지 않는 플러그인입니다",
                CurrentValue = plugin,
                RecommendedValue = modernAlternative,
                Implementation = $"plugins:\n  - {modernAlternative}",
                ExpectedBenefit = "호환성 개선, 최신 기능 사용, 보안 강화"
            });
        }
    }

    // Conversion Methods
    private Dictionary<string, object> ConvertConfigurationFormat(SiteConfiguration configuration, SiteConfigurationType targetType)
    {
        var converted = new Dictionary<string, object>(configuration.ParsedYaml);

        return targetType switch
        {
            SiteConfigurationType.Jekyll => ConvertToJekyllFormat(converted),
            SiteConfigurationType.Hugo => ConvertToHugoFormat(converted),
            SiteConfigurationType.GitBook => ConvertToGitBookFormat(converted),
            _ => converted
        };
    }

    private Dictionary<string, object> ConvertToJekyllFormat(Dictionary<string, object> config)
    {
        var jekyllConfig = new Dictionary<string, object>(config);

        // Jekyll 특정 필드 추가/수정
        if (!jekyllConfig.ContainsKey("markdown"))
            jekyllConfig["markdown"] = "kramdown";

        if (!jekyllConfig.ContainsKey("highlighter"))
            jekyllConfig["highlighter"] = "rouge";

        return jekyllConfig;
    }

    private Dictionary<string, object> ConvertToHugoFormat(Dictionary<string, object> config)
    {
        var hugoConfig = new Dictionary<string, object>();

        // Jekyll -> Hugo 필드 매핑
        if (config.TryGetValue("title", out var title))
            hugoConfig["title"] = title;

        if (config.TryGetValue("description", out var description))
            hugoConfig["description"] = description;

        if (config.TryGetValue("url", out var url))
            hugoConfig["baseURL"] = url;

        if (config.TryGetValue("lang", out var lang))
            hugoConfig["languageCode"] = lang;

        return hugoConfig;
    }

    private Dictionary<string, object> ConvertToGitBookFormat(Dictionary<string, object> config)
    {
        var gitbookConfig = new Dictionary<string, object>();

        if (config.TryGetValue("title", out var title))
            gitbookConfig["title"] = title;

        if (config.TryGetValue("description", out var description))
            gitbookConfig["description"] = description;

        // GitBook 특정 구조 추가
        gitbookConfig["structure"] = new Dictionary<string, object>
        {
            ["readme"] = "README.md",
            ["summary"] = "SUMMARY.md"
        };

        return gitbookConfig;
    }

    // Migration Methods
    private double CalculateCompatibilityScore(SiteConfigurationType source, SiteConfigurationType target)
    {
        if (source == target) return 1.0;

        return (source, target) switch
        {
            (SiteConfigurationType.Jekyll, SiteConfigurationType.Hugo) => 0.7,
            (SiteConfigurationType.Hugo, SiteConfigurationType.Jekyll) => 0.6,
            (SiteConfigurationType.Jekyll, SiteConfigurationType.GitBook) => 0.5,
            (SiteConfigurationType.Hugo, SiteConfigurationType.GitBook) => 0.5,
            _ => 0.3
        };
    }

    private IReadOnlyList<MigrationStep> GenerateMigrationSteps(SiteConfigurationType source, SiteConfigurationType target)
    {
        var steps = new List<MigrationStep>();

        if (source == SiteConfigurationType.Jekyll && target == SiteConfigurationType.Hugo)
        {
            steps.AddRange(new[]
            {
                new MigrationStep
                {
                    StepNumber = 1,
                    Title = "Hugo 설치",
                    Description = "Hugo 정적 사이트 생성기를 설치합니다",
                    Command = "brew install hugo", // macOS 예시
                    IsRequired = true,
                    EstimatedMinutes = 5
                },
                new MigrationStep
                {
                    StepNumber = 2,
                    Title = "설정 파일 변환",
                    Description = "_config.yml을 config.yaml로 변환합니다",
                    IsRequired = true,
                    EstimatedMinutes = 15
                },
                new MigrationStep
                {
                    StepNumber = 3,
                    Title = "콘텐츠 디렉토리 구조 변경",
                    Description = "_posts를 content/posts로 이동합니다",
                    Command = "mv _posts content/posts",
                    IsRequired = true,
                    EstimatedMinutes = 10
                }
            });
        }

        return steps.AsReadOnly();
    }

    private IReadOnlyList<IncompatibleSetting> FindIncompatibleSettings(SiteConfiguration sourceConfig, SiteConfigurationType targetType)
    {
        var incompatible = new List<IncompatibleSetting>();

        if (sourceConfig.ConfigType == SiteConfigurationType.Jekyll && targetType == SiteConfigurationType.Hugo)
        {
            // Jekyll 특정 설정들
            if (sourceConfig.PluginConfig.Plugins.Contains("jekyll-paginate"))
            {
                incompatible.Add(new IncompatibleSetting
                {
                    Key = "plugins.jekyll-paginate",
                    CurrentValue = "jekyll-paginate",
                    Reason = "Hugo는 내장 페이지네이션을 사용합니다",
                    Alternative = "Hugo의 내장 .Paginator 사용",
                    RequiresManualWork = true
                });
            }

            if (!string.IsNullOrEmpty(sourceConfig.ContentConfig.PermalinkFormat))
            {
                incompatible.Add(new IncompatibleSetting
                {
                    Key = "permalink",
                    CurrentValue = sourceConfig.ContentConfig.PermalinkFormat,
                    Reason = "Hugo는 다른 퍼머링크 형식을 사용합니다",
                    Alternative = "Hugo의 slug 시스템 사용",
                    RequiresManualWork = true
                });
            }
        }

        return incompatible.AsReadOnly();
    }

    private IReadOnlyList<string> GenerateAdditionalTasks(SiteConfigurationType source, SiteConfigurationType target)
    {
        var tasks = new List<string>();

        if (source == SiteConfigurationType.Jekyll && target == SiteConfigurationType.Hugo)
        {
            tasks.AddRange(new[]
            {
                "Liquid 템플릿을 Go 템플릿으로 변환",
                "Front Matter 형식 검토 및 수정",
                "이미지 및 정적 파일 경로 업데이트",
                "테마 설정 및 레이아웃 조정",
                "빌드 및 배포 스크립트 수정"
            });
        }

        return tasks.AsReadOnly();
    }

    // Helper Methods

    /// <summary>
    /// YamlDotNet deserializes nested objects as Dictionary<object, object>, not Dictionary<string, object>.
    /// This helper converts object dictionaries to string dictionaries.
    /// </summary>
    private Dictionary<string, object>? ConvertToStringDictionary(object obj)
    {
        if (obj is Dictionary<string, object> stringDict)
            return stringDict;

        if (obj is Dictionary<object, object> objectDict)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in objectDict)
            {
                if (kvp.Key?.ToString() is string key)
                {
                    result[key] = kvp.Value;
                }
            }
            return result;
        }

        return null;
    }

    private string? GetStringValue(Dictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private bool GetBoolValue(Dictionary<string, object> dict, string key, bool defaultValue = false)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                bool boolValue => boolValue,
                string stringValue => bool.TryParse(stringValue, out var parsed) && parsed,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    private int GetIntValue(Dictionary<string, object> dict, string key, int defaultValue = 0)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value switch
            {
                int intValue => intValue,
                string stringValue => int.TryParse(stringValue, out var parsed) ? parsed : defaultValue,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    private IReadOnlyList<string> GetStringArrayValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is IEnumerable<object> enumerable)
        {
            return enumerable.Select(item => item?.ToString()).Where(s => s != null).Cast<string>().ToArray();
        }
        return Array.Empty<string>();
    }

    private Dictionary<string, object> GetDictionaryValue(Dictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value is Dictionary<string, object> dictValue)
        {
            return dictValue;
        }
        return new Dictionary<string, object>();
    }

    private Dictionary<string, string> GetStringDictionaryValue(Dictionary<string, object> dict, string key)
    {
        var result = new Dictionary<string, string>();
        if (dict.TryGetValue(key, out var value) && value is Dictionary<string, object> dictValue)
        {
            foreach (var kvp in dictValue)
            {
                if (kvp.Value?.ToString() is string stringValue)
                {
                    result[kvp.Key] = stringValue;
                }
            }
        }
        return result;
    }

    #endregion
}