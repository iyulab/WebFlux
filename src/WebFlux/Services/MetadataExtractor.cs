using HtmlAgilityPack;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// HtmlAgilityPack을 사용한 고급 메타데이터 추출기
/// 15개 웹 표준 메타데이터를 포괄적으로 지원합니다
/// </summary>
public class MetadataExtractor : IMetadataExtractor
{
    private static readonly Regex WordCountRegex = new(@"\b\w+\b", RegexOptions.Compiled);
    private static readonly TimeSpan ReadingSpeed = TimeSpan.FromMinutes(1.0 / 250); // 250 words per minute

    /// <summary>
    /// HTML 콘텐츠에서 포괄적인 메타데이터를 추출합니다
    /// </summary>
    public async Task<WebMetadata> ExtractMetadataAsync(
        string htmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var metadata = new WebMetadata
        {
            SourceUrl = sourceUrl,
            ExtractedAt = DateTimeOffset.UtcNow,
            Basic = ExtractBasicMetadata(doc),
            OpenGraph = ExtractOpenGraphMetadata(doc),
            TwitterCards = ExtractTwitterCardsMetadata(doc),
            SchemaOrg = await ExtractSchemaOrgMetadataAsync(doc, cancellationToken),
            DublinCore = ExtractDublinCoreMetadata(doc),
            Structure = ExtractDocumentStructure(doc),
            Navigation = ExtractSiteNavigation(doc),
            Technical = ExtractTechnicalMetadata(doc, sourceUrl),
            Classification = ExtractContentClassification(doc),
            Accessibility = ExtractAccessibilityMetadata(doc)
        };

        // Calculate quality score
        var qualityScore = CalculateQualityScore(metadata);

        // Create new metadata with calculated quality score
        return new WebMetadata
        {
            SourceUrl = metadata.SourceUrl,
            ExtractedAt = metadata.ExtractedAt,
            Basic = metadata.Basic,
            OpenGraph = metadata.OpenGraph,
            TwitterCards = metadata.TwitterCards,
            SchemaOrg = metadata.SchemaOrg,
            DublinCore = metadata.DublinCore,
            Structure = metadata.Structure,
            Navigation = metadata.Navigation,
            Technical = metadata.Technical,
            Classification = metadata.Classification,
            Accessibility = metadata.Accessibility,
            QualityScore = qualityScore,
            CustomMetadata = metadata.CustomMetadata
        };
    }

    /// <summary>
    /// 기본 HTML 메타데이터 추출
    /// </summary>
    private static BasicHtmlMetadata ExtractBasicMetadata(HtmlDocument doc)
    {
        var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
        var description = GetMetaContent(doc, "description");
        var keywords = GetMetaContent(doc, "keywords")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim()).ToArray() ?? Array.Empty<string>();
        var author = GetMetaContent(doc, "author");
        var robots = GetMetaContent(doc, "robots");
        var language = doc.DocumentNode.GetAttributeValue("lang", string.Empty) is { Length: > 0 } langVal ? langVal : GetMetaContent(doc, "language");
        var charset = GetMetaContent(doc, "charset") ?? (doc.DocumentNode.SelectSingleNode("//meta[@charset]")?.GetAttributeValue("charset", string.Empty) is { Length: > 0 } charsetVal ? charsetVal : null);
        var viewport = GetMetaContent(doc, "viewport");
        var themeColor = GetMetaContent(doc, "theme-color");
        var canonical = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']")?.GetAttributeValue("href", string.Empty) is { Length: > 0 } canonicalVal ? canonicalVal : null;

        // Extract alternate languages
        var alternateLanguages = doc.DocumentNode.SelectNodes("//link[@rel='alternate' and @hreflang]")
            ?.Select(node => new AlternateLanguage
            {
                Language = node.GetAttributeValue("hreflang", string.Empty) is { Length: > 0 } hreflangVal ? hreflangVal : null,
                Url = node.GetAttributeValue("href", string.Empty) is { Length: > 0 } hrefVal ? hrefVal : null
            }).ToArray() ?? Array.Empty<AlternateLanguage>();

        // Parse dates
        var lastModified = ParseDate(GetMetaContent(doc, "last-modified"));
        var published = ParseDate(GetMetaContent(doc, "publication-date"));

        return new BasicHtmlMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            Author = author,
            Robots = robots,
            Language = language,
            Charset = charset,
            Viewport = viewport,
            ThemeColor = themeColor,
            CanonicalUrl = canonical,
            AlternateLanguages = alternateLanguages,
            LastModified = lastModified,
            Published = published
        };
    }

    /// <summary>
    /// Open Graph 메타데이터 추출
    /// </summary>
    private static OpenGraphMetadata ExtractOpenGraphMetadata(HtmlDocument doc)
    {
        return new OpenGraphMetadata
        {
            Title = GetMetaProperty(doc, "og:title"),
            Description = GetMetaProperty(doc, "og:description"),
            Image = GetMetaProperty(doc, "og:image"),
            Url = GetMetaProperty(doc, "og:url"),
            Type = GetMetaProperty(doc, "og:type"),
            SiteName = GetMetaProperty(doc, "og:site_name"),
            Locale = GetMetaProperty(doc, "og:locale"),
            ImageAlt = GetMetaProperty(doc, "og:image:alt"),
            ImageDimensions = ExtractImageDimensions(doc, "og:image:width", "og:image:height"),
            Video = GetMetaProperty(doc, "og:video"),
            Audio = GetMetaProperty(doc, "og:audio")
        };
    }

    /// <summary>
    /// Twitter Cards 메타데이터 추출
    /// </summary>
    private static TwitterCardsMetadata ExtractTwitterCardsMetadata(HtmlDocument doc)
    {
        return new TwitterCardsMetadata
        {
            Card = GetMetaName(doc, "twitter:card"),
            Title = GetMetaName(doc, "twitter:title"),
            Description = GetMetaName(doc, "twitter:description"),
            Image = GetMetaName(doc, "twitter:image"),
            ImageAlt = GetMetaName(doc, "twitter:image:alt"),
            Site = GetMetaName(doc, "twitter:site"),
            Creator = GetMetaName(doc, "twitter:creator"),
            Player = GetMetaName(doc, "twitter:player"),
            PlayerDimensions = ExtractImageDimensions(doc, "twitter:player:width", "twitter:player:height")
        };
    }

    /// <summary>
    /// Schema.org 구조화 데이터 추출
    /// </summary>
    private static Task<SchemaOrgMetadata> ExtractSchemaOrgMetadataAsync(HtmlDocument doc, CancellationToken cancellationToken)
    {
        var jsonLdScripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']")
            ?.Select(script => script.InnerText?.Trim())
            .Where(text => !string.IsNullOrEmpty(text))
            .Cast<string>()
            .ToArray() ?? Array.Empty<string>();

        var metadata = new SchemaOrgMetadata
        {
            RawJsonLd = jsonLdScripts
        };

        // Parse JSON-LD data
        foreach (var jsonLd in jsonLdScripts)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(jsonLd);
                var root = jsonDoc.RootElement;

                // Extract main entity type
                if (root.TryGetProperty("@type", out var typeElement))
                {
                    metadata = new SchemaOrgMetadata
                    {
                        MainEntityType = typeElement.GetString(),
                        Organization = metadata.Organization,
                        Person = metadata.Person,
                        Article = metadata.Article,
                        Software = metadata.Software,
                        Product = metadata.Product,
                        WebSite = metadata.WebSite,
                        Breadcrumbs = metadata.Breadcrumbs,
                        FaqItems = metadata.FaqItems,
                        RawJsonLd = metadata.RawJsonLd
                    };
                }

                // Extract specific entity information based on type
                var entityType = metadata.MainEntityType?.ToLowerInvariant();
                switch (entityType)
                {
                    case "organization":
                        metadata = new SchemaOrgMetadata
                        {
                            MainEntityType = metadata.MainEntityType,
                            Organization = ExtractOrganizationInfo(root),
                            Person = metadata.Person,
                            Article = metadata.Article,
                            Software = metadata.Software,
                            Product = metadata.Product,
                            WebSite = metadata.WebSite,
                            Breadcrumbs = metadata.Breadcrumbs,
                            FaqItems = metadata.FaqItems,
                            RawJsonLd = metadata.RawJsonLd
                        };
                        break;
                    case "person":
                        metadata = new SchemaOrgMetadata
                        {
                            MainEntityType = metadata.MainEntityType,
                            Organization = metadata.Organization,
                            Person = ExtractPersonInfo(root),
                            Article = metadata.Article,
                            Software = metadata.Software,
                            Product = metadata.Product,
                            WebSite = metadata.WebSite,
                            Breadcrumbs = metadata.Breadcrumbs,
                            FaqItems = metadata.FaqItems,
                            RawJsonLd = metadata.RawJsonLd
                        };
                        break;
                    case "article":
                    case "blogposting":
                    case "newsarticle":
                        metadata = new SchemaOrgMetadata
                        {
                            MainEntityType = metadata.MainEntityType,
                            Organization = metadata.Organization,
                            Person = metadata.Person,
                            Article = ExtractArticleInfo(root),
                            Software = metadata.Software,
                            Product = metadata.Product,
                            WebSite = metadata.WebSite,
                            Breadcrumbs = metadata.Breadcrumbs,
                            FaqItems = metadata.FaqItems,
                            RawJsonLd = metadata.RawJsonLd
                        };
                        break;
                    case "softwareapplication":
                    case "softwarelibrary":
                        metadata = new SchemaOrgMetadata
                        {
                            MainEntityType = metadata.MainEntityType,
                            Organization = metadata.Organization,
                            Person = metadata.Person,
                            Article = metadata.Article,
                            Software = ExtractSoftwareInfo(root),
                            Product = metadata.Product,
                            WebSite = metadata.WebSite,
                            Breadcrumbs = metadata.Breadcrumbs,
                            FaqItems = metadata.FaqItems,
                            RawJsonLd = metadata.RawJsonLd
                        };
                        break;
                    case "product":
                        metadata = new SchemaOrgMetadata
                        {
                            MainEntityType = metadata.MainEntityType,
                            Organization = metadata.Organization,
                            Person = metadata.Person,
                            Article = metadata.Article,
                            Software = metadata.Software,
                            Product = ExtractProductInfo(root),
                            WebSite = metadata.WebSite,
                            Breadcrumbs = metadata.Breadcrumbs,
                            FaqItems = metadata.FaqItems,
                            RawJsonLd = metadata.RawJsonLd
                        };
                        break;
                    case "website":
                        metadata = new SchemaOrgMetadata
                        {
                            MainEntityType = metadata.MainEntityType,
                            Organization = metadata.Organization,
                            Person = metadata.Person,
                            Article = metadata.Article,
                            Software = metadata.Software,
                            Product = metadata.Product,
                            WebSite = ExtractWebSiteInfo(root),
                            Breadcrumbs = metadata.Breadcrumbs,
                            FaqItems = metadata.FaqItems,
                            RawJsonLd = metadata.RawJsonLd
                        };
                        break;
                }
            }
            catch (JsonException)
            {
                // Skip invalid JSON-LD
                continue;
            }
        }

        // Extract breadcrumbs from microdata or JSON-LD
        var breadcrumbs = ExtractBreadcrumbs(doc);
        metadata = new SchemaOrgMetadata
        {
            MainEntityType = metadata.MainEntityType,
            Organization = metadata.Organization,
            Person = metadata.Person,
            Article = metadata.Article,
            Software = metadata.Software,
            Product = metadata.Product,
            WebSite = metadata.WebSite,
            Breadcrumbs = breadcrumbs,
            FaqItems = metadata.FaqItems,
            RawJsonLd = metadata.RawJsonLd
        };

        // Extract FAQ items
        var faqItems = ExtractFaqItems(doc);
        metadata = new SchemaOrgMetadata
        {
            MainEntityType = metadata.MainEntityType,
            Organization = metadata.Organization,
            Person = metadata.Person,
            Article = metadata.Article,
            Software = metadata.Software,
            Product = metadata.Product,
            WebSite = metadata.WebSite,
            Breadcrumbs = metadata.Breadcrumbs,
            FaqItems = faqItems,
            RawJsonLd = metadata.RawJsonLd
        };

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// Dublin Core 메타데이터 추출
    /// </summary>
    private static DublinCoreMetadata ExtractDublinCoreMetadata(HtmlDocument doc)
    {
        return new DublinCoreMetadata
        {
            Title = GetMetaName(doc, "DC.title"),
            Creator = GetMetaName(doc, "DC.creator"),
            Subject = GetMetaName(doc, "DC.subject"),
            Description = GetMetaName(doc, "DC.description"),
            Publisher = GetMetaName(doc, "DC.publisher"),
            Language = GetMetaName(doc, "DC.language"),
            Format = GetMetaName(doc, "DC.format"),
            Type = GetMetaName(doc, "DC.type"),
            Date = GetMetaName(doc, "DC.date"),
            Coverage = GetMetaName(doc, "DC.coverage"),
            Rights = GetMetaName(doc, "DC.rights")
        };
    }

    /// <summary>
    /// 문서 구조 정보 추출
    /// </summary>
    private static DocumentStructure ExtractDocumentStructure(HtmlDocument doc)
    {
        var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6")
            ?.Select(node => new HeadingInfo
            {
                Level = int.Parse(node.Name.AsSpan(1), System.Globalization.CultureInfo.InvariantCulture),
                Text = node.InnerText?.Trim(),
                Id = node.GetAttributeValue("id", string.Empty) is { Length: > 0 } idVal ? idVal : null
            }).ToArray() ?? Array.Empty<HeadingInfo>();

        var sectionCount = doc.DocumentNode.SelectNodes("//section")?.Count ?? 0;
        var paragraphCount = doc.DocumentNode.SelectNodes("//p")?.Count ?? 0;
        var linkCount = doc.DocumentNode.SelectNodes("//a[@href]")?.Count ?? 0;
        var imageCount = doc.DocumentNode.SelectNodes("//img")?.Count ?? 0;
        var tableCount = doc.DocumentNode.SelectNodes("//table")?.Count ?? 0;
        var listCount = doc.DocumentNode.SelectNodes("//ul | //ol")?.Count ?? 0;
        var codeBlockCount = doc.DocumentNode.SelectNodes("//pre | //code")?.Count ?? 0;

        // Calculate reading time
        var textContent = doc.DocumentNode.InnerText;
        var wordCount = WordCountRegex.Count(textContent);
        var readingTime = (int)Math.Ceiling(wordCount * ReadingSpeed.TotalMinutes);

        // Calculate complexity score
        var complexityScore = CalculateComplexityScore(headings.Length, paragraphCount, linkCount, imageCount, tableCount, codeBlockCount);

        return new DocumentStructure
        {
            Headings = headings,
            SectionCount = sectionCount,
            ParagraphCount = paragraphCount,
            LinkCount = linkCount,
            ImageCount = imageCount,
            TableCount = tableCount,
            ListCount = listCount,
            CodeBlockCount = codeBlockCount,
            EstimatedReadingTimeMinutes = readingTime,
            ComplexityScore = complexityScore
        };
    }

    /// <summary>
    /// 사이트 네비게이션 정보 추출
    /// </summary>
    private static SiteNavigation ExtractSiteNavigation(HtmlDocument doc)
    {
        var mainNav = ExtractNavigationLinks(doc, "//nav", "//header//a");
        var footerNav = ExtractNavigationLinks(doc, "//footer//a");
        var sidebarNav = ExtractNavigationLinks(doc, "//aside//a", "//sidebar//a");
        var relatedLinks = ExtractNavigationLinks(doc, "//a[contains(@class, 'related')]");

        var homeUrl = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'home') or contains(@href, '/') and not(contains(@href, '/'))]")?.GetAttributeValue("href", string.Empty) is { Length: > 0 } homeVal ? homeVal : null;
        var sitemapUrl = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'sitemap')]")?.GetAttributeValue("href", string.Empty) is { Length: > 0 } sitemapVal ? sitemapVal : null;
        var rssUrl = doc.DocumentNode.SelectSingleNode("//link[@type='application/rss+xml']")?.GetAttributeValue("href", string.Empty) is { Length: > 0 } rssVal ? rssVal : null;

        return new SiteNavigation
        {
            MainNavigation = mainNav,
            FooterLinks = footerNav,
            SidebarLinks = sidebarNav,
            RelatedLinks = relatedLinks,
            HomeUrl = homeUrl,
            SitemapUrl = sitemapUrl,
            RssFeedUrl = rssUrl
        };
    }

    /// <summary>
    /// 기술적 메타데이터 추출
    /// </summary>
    private static TechnicalMetadata ExtractTechnicalMetadata(HtmlDocument doc, string sourceUrl)
    {
        var isHttps = sourceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var requiresJs = doc.DocumentNode.SelectNodes("//script[not(@type) or @type='text/javascript' or @type='application/javascript']")?.Count > 0;
        var isMobileFriendly = !string.IsNullOrEmpty(GetMetaContent(doc, "viewport"));
        var isPwa = doc.DocumentNode.SelectSingleNode("//link[@rel='manifest']") != null;
        // Check for AMP (amp attribute or link to amphtml)
        var isAmp = doc.DocumentNode.SelectSingleNode("//html[@amp]") != null ||
                    doc.DocumentNode.SelectSingleNode("//link[@rel='amphtml']") != null;

        return new TechnicalMetadata
        {
            RequiresJavaScript = requiresJs,
            IsMobileFriendly = isMobileFriendly,
            IsPwa = isPwa,
            IsAmpPage = isAmp,
            Security = new SecurityInfo
            {
                IsHttps = isHttps
            }
        };
    }

    /// <summary>
    /// 콘텐츠 분류 정보 추출
    /// </summary>
    private static ContentClassification ExtractContentClassification(HtmlDocument doc)
    {
        var categories = ExtractCategories(doc);
        var tags = ExtractTags(doc);
        var contentType = DetermineContentType(doc);

        return new ContentClassification
        {
            Categories = categories,
            Tags = tags,
            ContentType = contentType
        };
    }

    /// <summary>
    /// 접근성 메타데이터 추출
    /// </summary>
    private static AccessibilityMetadata ExtractAccessibilityMetadata(HtmlDocument doc)
    {
        var images = doc.DocumentNode.SelectNodes("//img");
        var imagesWithAlt = doc.DocumentNode.SelectNodes("//img[@alt]");
        var altTextCoverage = images?.Count > 0 ? (double)(imagesWithAlt?.Count ?? 0) / images.Count : 1.0;

        var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");
        var hasProperStructure = ValidateHeadingStructure(headings);

        var hasSkipNav = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'skip') or contains(@href, '#main') or contains(@href, '#content')]") != null;
        var usesAria = doc.DocumentNode.SelectNodes("//*[@aria-label or @aria-labelledby or @aria-describedby]")?.Count > 0;

        var accessibilityScore = CalculateAccessibilityScore(altTextCoverage, hasProperStructure, hasSkipNav, usesAria);

        return new AccessibilityMetadata
        {
            ImageAltTextCoverage = altTextCoverage,
            HasProperHeadingStructure = hasProperStructure,
            HasSkipNavigation = hasSkipNav,
            UsesAriaLabels = usesAria,
            AccessibilityScore = accessibilityScore
        };
    }

    /// <summary>
    /// 메타데이터 품질 점수 계산
    /// </summary>
    public static double CalculateQualityScore(WebMetadata metadata)
    {
        var scores = new List<double>();

        // Basic metadata quality (25%)
        var basicScore = CalculateBasicMetadataScore(metadata.Basic);
        scores.Add(basicScore * 0.25);

        // Open Graph quality (20%)
        var ogScore = CalculateOpenGraphScore(metadata.OpenGraph);
        scores.Add(ogScore * 0.20);

        // Schema.org quality (20%)
        var schemaScore = CalculateSchemaOrgScore(metadata.SchemaOrg);
        scores.Add(schemaScore * 0.20);

        // Document structure quality (15%)
        var structureScore = CalculateStructureScore(metadata.Structure);
        scores.Add(structureScore * 0.15);

        // Technical quality (10%)
        var technicalScore = CalculateTechnicalScore(metadata.Technical);
        scores.Add(technicalScore * 0.10);

        // Accessibility quality (10%)
        var accessibilityScore = metadata.Accessibility.AccessibilityScore / 100.0;
        scores.Add(accessibilityScore * 0.10);

        return scores.Sum();
    }

    /// <summary>
    /// 메타데이터 완성도 평가
    /// </summary>
    public MetadataCompleteness EvaluateCompleteness(WebMetadata metadata)
    {
        var basicScore = CalculateBasicMetadataScore(metadata.Basic);
        var ogScore = CalculateOpenGraphScore(metadata.OpenGraph);
        var twitterScore = CalculateTwitterCardsScore(metadata.TwitterCards);
        var schemaScore = CalculateSchemaOrgScore(metadata.SchemaOrg);

        var overallScore = (basicScore + ogScore + twitterScore + schemaScore) / 4.0;

        var missingFields = new List<string>();
        var recommendations = new List<string>();

        // Check for missing critical fields
        if (string.IsNullOrEmpty(metadata.Basic.Title))
            missingFields.Add("title");
        if (string.IsNullOrEmpty(metadata.Basic.Description))
            missingFields.Add("description");
        if (string.IsNullOrEmpty(metadata.OpenGraph.Image))
            missingFields.Add("og:image");

        // Generate recommendations
        if (basicScore < 0.8)
            recommendations.Add("기본 메타데이터 (title, description, keywords) 개선 필요");
        if (ogScore < 0.6)
            recommendations.Add("Open Graph 메타데이터 추가로 소셜 미디어 최적화 개선");
        if (schemaScore < 0.5)
            recommendations.Add("Schema.org 구조화 데이터 추가로 검색 엔진 최적화 개선");

        return new MetadataCompleteness
        {
            OverallScore = overallScore,
            BasicMetadataScore = basicScore,
            OpenGraphScore = ogScore,
            TwitterCardsScore = twitterScore,
            SchemaOrgScore = schemaScore,
            MissingCriticalFields = missingFields,
            Recommendations = recommendations
        };
    }

    // Helper methods
    private static string? GetMetaContent(HtmlDocument doc, string name)
    {
        return doc.DocumentNode.SelectSingleNode($"//meta[@name='{name}']")?.GetAttributeValue("content", string.Empty) is { Length: > 0 } val ? val : null;
    }

    private static string? GetMetaProperty(HtmlDocument doc, string property)
    {
        return doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")?.GetAttributeValue("content", string.Empty) is { Length: > 0 } val ? val : null;
    }

    private static string? GetMetaName(HtmlDocument doc, string name)
    {
        return doc.DocumentNode.SelectSingleNode($"//meta[@name='{name}']")?.GetAttributeValue("content", string.Empty) is { Length: > 0 } val ? val : null;
    }

    private static ImageDimensions? ExtractImageDimensions(HtmlDocument doc, string widthProperty, string heightProperty)
    {
        var widthStr = GetMetaProperty(doc, widthProperty);
        var heightStr = GetMetaProperty(doc, heightProperty);

        if (int.TryParse(widthStr, out var width) && int.TryParse(heightStr, out var height))
        {
            return new ImageDimensions { Width = width, Height = height };
        }

        return null;
    }

    private static DateTimeOffset? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        if (DateTimeOffset.TryParse(dateString, out var date))
            return date;

        return null;
    }

    private static OrganizationInfo? ExtractOrganizationInfo(JsonElement element)
    {
        return new OrganizationInfo
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Url = element.TryGetProperty("url", out var url) ? url.GetString() : null,
            Logo = element.TryGetProperty("logo", out var logo) ? logo.GetString() : null,
            Description = element.TryGetProperty("description", out var desc) ? desc.GetString() : null
        };
    }

    private static PersonInfo? ExtractPersonInfo(JsonElement element)
    {
        return new PersonInfo
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Url = element.TryGetProperty("url", out var url) ? url.GetString() : null,
            Image = element.TryGetProperty("image", out var image) ? image.GetString() : null,
            JobTitle = element.TryGetProperty("jobTitle", out var jobTitle) ? jobTitle.GetString() : null
        };
    }

    private static ArticleInfo? ExtractArticleInfo(JsonElement element)
    {
        return new ArticleInfo
        {
            Headline = element.TryGetProperty("headline", out var headline) ? headline.GetString() : null,
            DatePublished = element.TryGetProperty("datePublished", out var pubDate) && DateTimeOffset.TryParse(pubDate.GetString(), out var pub) ? pub : null,
            DateModified = element.TryGetProperty("dateModified", out var modDate) && DateTimeOffset.TryParse(modDate.GetString(), out var mod) ? mod : null,
            Author = ExtractAuthorName(element),
            Publisher = ExtractPublisherName(element),
            Section = element.TryGetProperty("articleSection", out var section) ? section.GetString() : null,
            Keywords = ExtractKeywordsFromJsonElement(element)
        };
    }

    private static SoftwareInfo? ExtractSoftwareInfo(JsonElement element)
    {
        return new SoftwareInfo
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Version = element.TryGetProperty("version", out var version) ? version.GetString() : null,
            ProgrammingLanguage = element.TryGetProperty("programmingLanguage", out var lang) ? lang.GetString() : null,
            RuntimePlatform = element.TryGetProperty("runtimePlatform", out var platform) ? platform.GetString() : null,
            License = element.TryGetProperty("license", out var license) ? license.GetString() : null,
            CodeRepository = element.TryGetProperty("codeRepository", out var repo) ? repo.GetString() : null
        };
    }

    private static ProductInfo? ExtractProductInfo(JsonElement element)
    {
        return new ProductInfo
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Brand = ExtractBrandName(element),
            Category = element.TryGetProperty("category", out var category) ? category.GetString() : null
        };
    }

    private static WebSiteInfo? ExtractWebSiteInfo(JsonElement element)
    {
        return new WebSiteInfo
        {
            Name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            Url = element.TryGetProperty("url", out var url) ? url.GetString() : null
        };
    }

    private static string? ExtractAuthorName(JsonElement element)
    {
        if (element.TryGetProperty("author", out var author))
        {
            if (author.ValueKind == JsonValueKind.String)
                return author.GetString();
            if (author.ValueKind == JsonValueKind.Object && author.TryGetProperty("name", out var authorName))
                return authorName.GetString();
        }
        return null;
    }

    private static string? ExtractPublisherName(JsonElement element)
    {
        if (element.TryGetProperty("publisher", out var publisher))
        {
            if (publisher.ValueKind == JsonValueKind.String)
                return publisher.GetString();
            if (publisher.ValueKind == JsonValueKind.Object && publisher.TryGetProperty("name", out var publisherName))
                return publisherName.GetString();
        }
        return null;
    }

    private static string? ExtractBrandName(JsonElement element)
    {
        if (element.TryGetProperty("brand", out var brand))
        {
            if (brand.ValueKind == JsonValueKind.String)
                return brand.GetString();
            if (brand.ValueKind == JsonValueKind.Object && brand.TryGetProperty("name", out var brandName))
                return brandName.GetString();
        }
        return null;
    }

    private static List<string> ExtractKeywordsFromJsonElement(JsonElement element)
    {
        if (element.TryGetProperty("keywords", out var keywords))
        {
            if (keywords.ValueKind == JsonValueKind.String)
            {
                return keywords.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim()).ToList() ?? [];
            }
            if (keywords.ValueKind == JsonValueKind.Array)
            {
                return keywords.EnumerateArray()
                    .Select(k => k.GetString())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList()!;
            }
        }
        return [];
    }

    private static List<BreadcrumbItem> ExtractBreadcrumbs(HtmlDocument doc)
    {
        var breadcrumbs = new List<BreadcrumbItem>();

        // Try to extract from common breadcrumb patterns
        var breadcrumbNodes = doc.DocumentNode.SelectNodes("//nav[contains(@class, 'breadcrumb')]//a | //ol[contains(@class, 'breadcrumb')]//a");
        if (breadcrumbNodes != null)
        {
            foreach (var node in breadcrumbNodes)
            {
                breadcrumbs.Add(new BreadcrumbItem
                {
                    Text = node.InnerText?.Trim() ?? string.Empty,
                    Url = node.GetAttributeValue("href", string.Empty)
                });
            }
        }

        return breadcrumbs;
    }

    private static List<FaqItem> ExtractFaqItems(HtmlDocument doc)
    {
        var faqItems = new List<FaqItem>();

        // Try to extract FAQ items from common patterns
        var faqNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'faq') or contains(@class, 'question')]");
        if (faqNodes != null)
        {
            foreach (var node in faqNodes)
            {
                var question = node.SelectSingleNode(".//h3 | .//h4 | .//*[contains(@class, 'question')]")?.InnerText?.Trim();
                var answer = node.SelectSingleNode(".//*[contains(@class, 'answer') or contains(@class, 'response')]")?.InnerText?.Trim();

                if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                {
                    faqItems.Add(new FaqItem
                    {
                        Question = question,
                        Answer = answer
                    });
                }
            }
        }

        return faqItems;
    }

    private static List<NavigationLink> ExtractNavigationLinks(HtmlDocument doc, params string[] xpaths)
    {
        var links = new List<NavigationLink>();

        foreach (var xpath in xpaths)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var href = node.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(href))
                    {
                        links.Add(new NavigationLink
                        {
                            Text = node.InnerText?.Trim(),
                            Url = href,
                            Title = node.GetAttributeValue("title", string.Empty) is { Length: > 0 } titleVal ? titleVal : null
                        });
                    }
                }
            }
        }

        return links.Distinct().ToList();
    }

    private static List<string> ExtractCategories(HtmlDocument doc)
    {
        var categories = new List<string>();

        // Extract from meta tags
        var categoryMeta = GetMetaContent(doc, "category");
        if (!string.IsNullOrEmpty(categoryMeta))
        {
            categories.AddRange(categoryMeta.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()));
        }

        // Extract from article meta
        var articleCategory = GetMetaProperty(doc, "article:section");
        if (!string.IsNullOrEmpty(articleCategory))
        {
            categories.Add(articleCategory);
        }

        return categories.Distinct().ToList();
    }

    private static List<string> ExtractTags(HtmlDocument doc)
    {
        var tags = new List<string>();

        // Extract from meta tags
        var tagMeta = GetMetaProperty(doc, "article:tag");
        if (!string.IsNullOrEmpty(tagMeta))
        {
            tags.AddRange(tagMeta.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()));
        }

        // Extract from tag elements
        var tagElements = doc.DocumentNode.SelectNodes("//a[contains(@class, 'tag') or contains(@rel, 'tag')]");
        if (tagElements != null)
        {
            tags.AddRange(tagElements.Select(t => t.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t))!);
        }

        return tags.Distinct().ToList();
    }

    private static string? DetermineContentType(HtmlDocument doc)
    {
        // Check for article indicators
        if (doc.DocumentNode.SelectSingleNode("//article") != null ||
            !string.IsNullOrEmpty(GetMetaProperty(doc, "og:type")) && GetMetaProperty(doc, "og:type")!.Contains("article"))
        {
            return "Article";
        }

        // Check for product indicators
        if (doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'product') or contains(@class, 'item')]") != null)
        {
            return "Product";
        }

        // Check for documentation indicators
        if (doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'documentation') or contains(@class, 'docs')]") != null ||
            doc.DocumentNode.SelectNodes("//pre | //code")?.Count > 5)
        {
            return "Documentation";
        }

        return "Webpage";
    }

    private static double CalculateComplexityScore(int headings, int paragraphs, int links, int images, int tables, int codeBlocks)
    {
        var score = 0.0;

        // Heading structure contributes to complexity
        score += Math.Min(headings * 0.1, 1.0);

        // Content richness
        score += Math.Min(paragraphs * 0.01, 0.5);
        score += Math.Min(links * 0.005, 0.3);
        score += Math.Min(images * 0.02, 0.4);
        score += Math.Min(tables * 0.05, 0.3);
        score += Math.Min(codeBlocks * 0.1, 0.5);

        return Math.Min(score, 1.0);
    }

    private static bool ValidateHeadingStructure(HtmlNodeCollection? headings)
    {
        if (headings == null || headings.Count == 0)
            return false;

        var levels = headings.Select(h => int.Parse(h.Name.AsSpan(1), System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        // Check if starts with H1
        if (levels[0] != 1)
            return false;

        // Check for proper hierarchy (no skipping levels)
        for (int i = 1; i < levels.Length; i++)
        {
            if (levels[i] - levels[i - 1] > 1)
                return false;
        }

        return true;
    }

    private static int CalculateAccessibilityScore(double altTextCoverage, bool hasProperStructure, bool hasSkipNav, bool usesAria)
    {
        var score = 0;

        score += (int)(altTextCoverage * 40); // Alt text coverage (40 points)
        score += hasProperStructure ? 25 : 0; // Proper heading structure (25 points)
        score += hasSkipNav ? 15 : 0; // Skip navigation (15 points)
        score += usesAria ? 20 : 0; // ARIA labels (20 points)

        return Math.Min(score, 100);
    }

    private static double CalculateBasicMetadataScore(BasicHtmlMetadata basic)
    {
        var score = 0.0;

        if (!string.IsNullOrEmpty(basic.Title)) score += 0.3;
        if (!string.IsNullOrEmpty(basic.Description)) score += 0.3;
        if (basic.Keywords.Any()) score += 0.1;
        if (!string.IsNullOrEmpty(basic.Author)) score += 0.1;
        if (!string.IsNullOrEmpty(basic.Language)) score += 0.1;
        if (!string.IsNullOrEmpty(basic.CanonicalUrl)) score += 0.1;

        return Math.Min(score, 1.0);
    }

    private static double CalculateOpenGraphScore(OpenGraphMetadata og)
    {
        var score = 0.0;

        if (!string.IsNullOrEmpty(og.Title)) score += 0.25;
        if (!string.IsNullOrEmpty(og.Description)) score += 0.25;
        if (!string.IsNullOrEmpty(og.Image)) score += 0.25;
        if (!string.IsNullOrEmpty(og.Url)) score += 0.15;
        if (!string.IsNullOrEmpty(og.Type)) score += 0.1;

        return Math.Min(score, 1.0);
    }

    private static double CalculateTwitterCardsScore(TwitterCardsMetadata twitter)
    {
        var score = 0.0;

        if (!string.IsNullOrEmpty(twitter.Card)) score += 0.25;
        if (!string.IsNullOrEmpty(twitter.Title)) score += 0.25;
        if (!string.IsNullOrEmpty(twitter.Description)) score += 0.25;
        if (!string.IsNullOrEmpty(twitter.Image)) score += 0.25;

        return Math.Min(score, 1.0);
    }

    private static double CalculateSchemaOrgScore(SchemaOrgMetadata schema)
    {
        var score = 0.0;

        if (!string.IsNullOrEmpty(schema.MainEntityType)) score += 0.3;
        if (schema.Organization != null) score += 0.2;
        if (schema.Article != null) score += 0.2;
        if (schema.Person != null) score += 0.1;
        if (schema.Breadcrumbs.Any()) score += 0.1;
        if (schema.RawJsonLd.Any()) score += 0.1;

        return Math.Min(score, 1.0);
    }

    private static double CalculateStructureScore(DocumentStructure structure)
    {
        var score = 0.0;

        if (structure.Headings.Any()) score += 0.3;
        if (structure.ComplexityScore > 0.3) score += 0.3;
        if (structure.EstimatedReadingTimeMinutes > 0) score += 0.2;
        if (structure.LinkCount > 0) score += 0.2;

        return Math.Min(score, 1.0);
    }

    private static double CalculateTechnicalScore(TechnicalMetadata technical)
    {
        var score = 0.0;

        if (technical.Security.IsHttps) score += 0.3;
        if (technical.IsMobileFriendly) score += 0.3;
        if (!technical.RequiresJavaScript) score += 0.2; // Static content scores higher
        if (technical.IsPwa) score += 0.2;

        return Math.Min(score, 1.0);
    }
}