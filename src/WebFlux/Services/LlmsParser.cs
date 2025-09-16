using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services;

/// <summary>
/// llms.txt 파일 파싱 서비스 구현체
/// AI 친화적 웹 표준을 통한 사이트 구조 및 메타데이터 추출
/// </summary>
public class LlmsParser : ILlmsParser
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<LlmsParser> _logger;
    private readonly LlmsParsingStatistics _statistics;
    private readonly Dictionary<string, LlmsParseResult> _cache;
    private readonly SemaphoreSlim _semaphore;

    private static readonly string[] CommonLlmsPaths =
    {
        "/llms.txt",
        "/.well-known/llms.txt",
        "/ai/llms.txt",
        "/docs/llms.txt"
    };

    public LlmsParser(IHttpClientService httpClient, ILogger<LlmsParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _statistics = new LlmsParsingStatistics();
        _cache = new Dictionary<string, LlmsParseResult>();
        _semaphore = new SemaphoreSlim(5, 5); // 최대 5개 동시 파싱
    }

    public async Task<LlmsParseResult?> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

        var normalizedUrl = NormalizeUrl(baseUrl);

        // 캐시 확인
        if (_cache.TryGetValue(normalizedUrl, out var cachedResult) &&
            cachedResult.ParsedAt > DateTimeOffset.UtcNow.AddHours(-1))
        {
            _logger.LogDebug("Returning cached llms.txt result for {Url}", normalizedUrl);
            return cachedResult;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            _statistics.TotalParseAttempts++;
            var startTime = DateTimeOffset.UtcNow;

            var result = await TryFindAndParseLlmsTxtAsync(normalizedUrl, cancellationToken);

            var parseTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _statistics.AverageParseTime = (_statistics.AverageParseTime * _statistics.SuccessfulParses + parseTime) / (_statistics.SuccessfulParses + 1);

            if (result.IsSuccess)
            {
                _statistics.SuccessfulParses++;
                if (result.FileFound)
                    _statistics.SitesWithLlmsFile++;

                if (result.Metadata?.Version != null)
                {
                    _statistics.VersionStatistics.TryGetValue(result.Metadata.Version, out var count);
                    _statistics.VersionStatistics[result.Metadata.Version] = count + 1;
                }
            }
            else if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                _statistics.CommonErrors.TryGetValue(result.ErrorMessage, out var errorCount);
                _statistics.CommonErrors[result.ErrorMessage] = errorCount + 1;
            }

            _statistics.LastUpdated = DateTimeOffset.UtcNow;

            // 캐시 저장
            _cache[normalizedUrl] = result;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing llms.txt from {Url}", normalizedUrl);
            var errorResult = new LlmsParseResult
            {
                IsSuccess = false,
                FileFound = false,
                ErrorMessage = ex.Message,
                ParsedAt = DateTimeOffset.UtcNow
            };

            _statistics.CommonErrors.TryGetValue(ex.Message, out var errorCount);
            _statistics.CommonErrors[ex.Message] = errorCount + 1;

            return errorResult;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<LlmsParseResult> TryFindAndParseLlmsTxtAsync(string baseUrl, CancellationToken cancellationToken)
    {
        foreach (var path in CommonLlmsPaths)
        {
            var llmsUrl = $"{baseUrl.TrimEnd('/')}{path}";

            try
            {
                var response = await _httpClient.GetAsync(llmsUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var metadata = await ParseContentAsync(content, baseUrl);

                    return new LlmsParseResult
                    {
                        IsSuccess = true,
                        FileFound = true,
                        LlmsUrl = llmsUrl,
                        Metadata = metadata,
                        RawContent = content,
                        ParsedAt = DateTimeOffset.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Failed to fetch llms.txt from {Url}: {Error}", llmsUrl, ex.Message);
            }
        }

        // llms.txt 파일을 찾지 못한 경우
        return new LlmsParseResult
        {
            IsSuccess = true,
            FileFound = false,
            ParsedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<LlmsMetadata> ParseContentAsync(string content, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        await Task.Delay(1); // async method로 만들기 위한 최소 지연

        var metadata = new LlmsMetadata
        {
            BaseUrl = baseUrl,
            LastUpdated = DateTimeOffset.UtcNow
        };

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                          .Select(line => line.Trim())
                          .Where(line => !string.IsNullOrEmpty(line))
                          .ToArray();

        string currentSection = "";
        var currentSectionObject = new LlmsSection();
        var isInMetadataSection = true;

        foreach (var line in lines)
        {
            // 주석 건너뛰기
            if (line.StartsWith("#") || line.StartsWith("//"))
                continue;

            // 메타데이터 파싱
            if (isInMetadataSection && line.Contains(":"))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim().ToLowerInvariant();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "site_name":
                        case "sitename":
                            metadata.SiteName = value;
                            break;
                        case "description":
                            metadata.Description = value;
                            break;
                        case "version":
                            metadata.Version = value;
                            break;
                        case "contact_email":
                        case "email":
                            metadata.Contact ??= new LlmsContact();
                            metadata.Contact.Email = value;
                            break;
                        case "website":
                            metadata.Contact ??= new LlmsContact();
                            metadata.Contact.Website = value;
                            break;
                        case "languages":
                            metadata.Languages = value.Split(',')
                                                     .Select(lang => lang.Trim())
                                                     .ToList();
                            break;
                        case "tags":
                            metadata.Tags = value.Split(',')
                                                .Select(tag => tag.Trim())
                                                .ToList();
                            break;
                        case "crawl_rate_limit":
                        case "rate_limit":
                            if (double.TryParse(value, out var rateLimit))
                            {
                                metadata.CrawlingGuidelines ??= new LlmsCrawlingGuidelines();
                                metadata.CrawlingGuidelines.RecommendedRateLimit = rateLimit;
                            }
                            break;
                        case "max_connections":
                            if (int.TryParse(value, out var maxConnections))
                            {
                                metadata.CrawlingGuidelines ??= new LlmsCrawlingGuidelines();
                                metadata.CrawlingGuidelines.MaxConcurrentConnections = maxConnections;
                            }
                            break;
                        case "user_agent":
                            metadata.CrawlingGuidelines ??= new LlmsCrawlingGuidelines();
                            metadata.CrawlingGuidelines.PreferredUserAgent = value;
                            break;
                    }
                }
            }

            // 섹션 헤더 파싱 ([Section Name])
            var sectionMatch = Regex.Match(line, @"^\[(.+)\]$");
            if (sectionMatch.Success)
            {
                // 이전 섹션 저장
                if (!string.IsNullOrEmpty(currentSection) && !string.IsNullOrEmpty(currentSectionObject.Name))
                {
                    metadata.Sections.Add(currentSectionObject);
                }

                currentSection = sectionMatch.Groups[1].Value;
                currentSectionObject = new LlmsSection { Name = currentSection };
                isInMetadataSection = false;
                continue;
            }

            // 섹션 내용 파싱
            if (!isInMetadataSection && !string.IsNullOrEmpty(currentSection))
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim().ToLowerInvariant();
                        var value = parts[1].Trim();

                        switch (key)
                        {
                            case "path":
                                currentSectionObject.Path = value;
                                break;
                            case "description":
                                currentSectionObject.Description = value;
                                break;
                            case "priority":
                                if (int.TryParse(value, out var priority))
                                    currentSectionObject.Priority = priority;
                                break;
                            case "content_type":
                                currentSectionObject.ContentType = value;
                                break;
                            case "estimated_pages":
                                if (int.TryParse(value, out var pageCount))
                                    currentSectionObject.EstimatedPageCount = pageCount;
                                break;
                            case "tags":
                                currentSectionObject.Tags = value.Split(',')
                                                                 .Select(tag => tag.Trim())
                                                                 .ToList();
                                break;
                        }
                    }
                }
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    // 중요한 페이지 목록 파싱
                    var pageInfo = line.Substring(2).Trim();
                    var page = ParsePageInfo(pageInfo, baseUrl);
                    if (page != null)
                    {
                        metadata.ImportantPages.Add(page);
                    }
                }
            }
        }

        // 마지막 섹션 저장
        if (!string.IsNullOrEmpty(currentSection) && !string.IsNullOrEmpty(currentSectionObject.Name))
        {
            metadata.Sections.Add(currentSectionObject);
        }

        return metadata;
    }

    private LlmsPage? ParsePageInfo(string pageInfo, string baseUrl)
    {
        // 간단한 페이지 정보 파싱: "경로 - 설명 (우선순위: N)"
        var match = Regex.Match(pageInfo, @"^([^\s]+)\s*-?\s*([^(]*?)(?:\s*\(priority:\s*(\d+)\))?$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var path = match.Groups[1].Value.Trim();
            var description = match.Groups[2].Value.Trim();
            var priorityStr = match.Groups[3].Value;

            return new LlmsPage
            {
                Path = path,
                Description = description,
                Priority = int.TryParse(priorityStr, out var priority) ? priority : 5,
                Title = ExtractTitleFromPath(path)
            };
        }

        // 단순 경로만 있는 경우
        return new LlmsPage
        {
            Path = pageInfo,
            Title = ExtractTitleFromPath(pageInfo)
        };
    }

    private string ExtractTitleFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0)
        {
            var lastSegment = segments[^1];
            // 확장자 제거
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(lastSegment);
            // 하이픈을 공백으로 변환하고 첫 글자 대문자로
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                nameWithoutExtension.Replace('-', ' ').Replace('_', ' '));
        }
        return path;
    }

    public async Task<CrawlOptions> OptimizeCrawlOptionsAsync(LlmsMetadata metadata, CrawlOptions crawlOptions)
    {
        await Task.Delay(1); // async method

        var optimizedOptions = new CrawlOptions
        {
            // 기존 옵션 복사
            MaxDepth = crawlOptions.MaxDepth,
            MaxPages = crawlOptions.MaxPages,
            Strategy = crawlOptions.Strategy,
            FollowExternalLinks = crawlOptions.FollowExternalLinks,
            RespectRobotsTxt = crawlOptions.RespectRobotsTxt,
            Timeout = crawlOptions.Timeout,
            UserAgent = crawlOptions.UserAgent,
            Headers = new Dictionary<string, string>(crawlOptions.Headers),
            ExcludePatterns = new List<string>(crawlOptions.ExcludePatterns),
            IncludePatterns = new List<string>(crawlOptions.IncludePatterns)
        };

        // llms.txt 메타데이터 기반 최적화
        if (metadata.CrawlingGuidelines != null)
        {
            var guidelines = metadata.CrawlingGuidelines;

            // 크롤링 속도 제한 적용
            if (guidelines.RecommendedRateLimit.HasValue)
            {
                var delayMs = (int)(1000.0 / guidelines.RecommendedRateLimit.Value);
                optimizedOptions.DelayBetweenRequests = TimeSpan.FromMilliseconds(Math.Max(delayMs, 100));
            }

            // 최대 동시 연결 수 적용
            if (guidelines.MaxConcurrentConnections.HasValue)
            {
                optimizedOptions.MaxConcurrency = Math.Min(guidelines.MaxConcurrentConnections.Value, 10);
            }

            // User-Agent 적용
            if (!string.IsNullOrEmpty(guidelines.PreferredUserAgent))
            {
                optimizedOptions.UserAgent = guidelines.PreferredUserAgent;
            }

            // 제외 패턴 추가
            if (guidelines.ExcludePatterns.Any())
            {
                optimizedOptions.ExcludePatterns.AddRange(guidelines.ExcludePatterns);
            }

            // 포함 패턴 추가
            if (guidelines.IncludePatterns.Any())
            {
                optimizedOptions.IncludePatterns.AddRange(guidelines.IncludePatterns);
            }
        }

        // 중요 페이지 우선순위 적용
        if (metadata.ImportantPages.Any())
        {
            var highPriorityPages = metadata.ImportantPages
                .Where(p => p.Priority >= 8)
                .Select(p => p.Path)
                .ToList();

            if (highPriorityPages.Any())
            {
                optimizedOptions.PriorityUrls = highPriorityPages;
            }
        }

        // 섹션 기반 깊이 조정
        if (metadata.Sections.Any())
        {
            var maxSectionDepth = metadata.Sections
                .Where(s => !string.IsNullOrEmpty(s.Path))
                .Max(s => s.Path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length);

            optimizedOptions.MaxDepth = Math.Max(optimizedOptions.MaxDepth, maxSectionDepth + 1);
        }

        _logger.LogInformation("Optimized crawl options using llms.txt metadata for {SiteName}", metadata.SiteName);

        return optimizedOptions;
    }

    public async Task<ChunkingOptions> EnhanceChunkingOptionsAsync(
        LlmsMetadata metadata,
        ExtractedContent content,
        ChunkingOptions chunkingOptions)
    {
        await Task.Delay(1); // async method

        var enhancedOptions = new ChunkingOptions
        {
            Strategy = chunkingOptions.Strategy,
            ChunkSize = chunkingOptions.ChunkSize,
            ChunkOverlap = chunkingOptions.ChunkOverlap,
            PreserveFormatting = chunkingOptions.PreserveFormatting,
            SplitOnSentences = chunkingOptions.SplitOnSentences,
            CustomSeparators = new List<string>(chunkingOptions.CustomSeparators),
            Metadata = new Dictionary<string, object>(chunkingOptions.Metadata)
        };

        // llms.txt 메타데이터를 청킹 메타데이터에 추가
        enhancedOptions.Metadata["llms_site_name"] = metadata.SiteName;
        enhancedOptions.Metadata["llms_description"] = metadata.Description;

        if (metadata.Tags.Any())
        {
            enhancedOptions.Metadata["llms_tags"] = string.Join(", ", metadata.Tags);
        }

        // 콘텐츠 타입별 청킹 전략 조정
        var relatedSection = FindRelatedSection(metadata, content);
        if (relatedSection != null)
        {
            enhancedOptions.Metadata["llms_section"] = relatedSection.Name;
            enhancedOptions.Metadata["llms_content_type"] = relatedSection.ContentType;
            enhancedOptions.Metadata["llms_priority"] = relatedSection.Priority;

            // 콘텐츠 타입에 따른 청킹 크기 조정
            switch (relatedSection.ContentType?.ToLowerInvariant())
            {
                case "reference":
                case "api":
                    enhancedOptions.ChunkSize = Math.Max(enhancedOptions.ChunkSize, 2000); // 참조 문서는 더 큰 청크
                    break;
                case "tutorial":
                case "guide":
                    enhancedOptions.SplitOnSentences = true; // 가이드는 문장 단위 분할
                    break;
                case "code":
                    enhancedOptions.CustomSeparators.AddRange(new[] { "```", "function ", "class ", "def " });
                    break;
            }
        }

        _logger.LogDebug("Enhanced chunking options with llms.txt metadata");

        return enhancedOptions;
    }

    private LlmsSection? FindRelatedSection(LlmsMetadata metadata, ExtractedContent content)
    {
        if (string.IsNullOrEmpty(content.Url) || !metadata.Sections.Any())
            return null;

        var urlPath = new Uri(content.Url).AbsolutePath;

        // 정확한 경로 매칭 시도
        var exactMatch = metadata.Sections.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.Path) && urlPath.StartsWith(s.Path, StringComparison.OrdinalIgnoreCase));

        if (exactMatch != null)
            return exactMatch;

        // 부분 매칭 시도
        var partialMatch = metadata.Sections
            .Where(s => !string.IsNullOrEmpty(s.Path))
            .OrderByDescending(s => GetPathMatchScore(urlPath, s.Path))
            .FirstOrDefault();

        return partialMatch;
    }

    private int GetPathMatchScore(string urlPath, string sectionPath)
    {
        var urlSegments = urlPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var sectionSegments = sectionPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var matchingSegments = urlSegments
            .Zip(sectionSegments, (u, s) => string.Equals(u, s, StringComparison.OrdinalIgnoreCase))
            .TakeWhile(match => match)
            .Count();

        return matchingSegments;
    }

    private string NormalizeUrl(string url)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
    }

    public IReadOnlyList<string> GetSupportedVersions()
    {
        return new[] { "1.0", "1.1", "2.0" };
    }

    public LlmsParsingStatistics GetStatistics()
    {
        return _statistics;
    }
}