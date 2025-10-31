using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Infrastructure.AI;

/// <summary>
/// AI 기반 웹 메타데이터 추출기
/// ITextCompletionService를 사용하여 웹 콘텐츠에서 메타데이터를 추출합니다
/// </summary>
public class AIWebMetadataExtractor : IWebMetadataExtractor
{
    private readonly ITextCompletionService _completionService;
    private readonly ILogger<AIWebMetadataExtractor> _logger;

    public AIWebMetadataExtractor(
        ITextCompletionService completionService,
        ILogger<AIWebMetadataExtractor> logger)
    {
        _completionService = completionService ?? throw new ArgumentNullException(nameof(completionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EnrichedMetadata> ExtractAsync(
        string content,
        string url,
        HtmlMetadataSnapshot? htmlMetadata = null,
        MetadataSchema schema = MetadataSchema.General,
        string? customPrompt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (schema == MetadataSchema.Custom && string.IsNullOrWhiteSpace(customPrompt))
        {
            throw new ArgumentException("Custom schema requires customPrompt parameter", nameof(customPrompt));
        }

        try
        {
            _logger.LogInformation("Extracting metadata for URL: {Url}, Schema: {Schema}", url, schema);

            // 1. 프롬프트 생성
            var prompt = BuildPrompt(content, url, htmlMetadata, schema, customPrompt);

            // 2. AI 추출 실행
            var response = await _completionService.CompleteAsync(prompt, null, cancellationToken);

            // 3. JSON 파싱
            var aiMetadata = ParseAiResponse(response);

            // 4. HTML 메타데이터와 융합
            var enrichedMetadata = MergeWithHtmlMetadata(aiMetadata, htmlMetadata, url);

            _logger.LogInformation(
                "Metadata extraction completed. Confidence: {Confidence}, Topics: {TopicCount}",
                enrichedMetadata.OverallConfidence,
                enrichedMetadata.Topics.Count);

            return enrichedMetadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata for URL: {Url}", url);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EnrichedMetadata>> ExtractBatchAsync(
        IEnumerable<(string content, string url, HtmlMetadataSnapshot? htmlMetadata)> items,
        MetadataSchema schema = MetadataSchema.General,
        string? customPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();

        if (!itemList.Any())
        {
            return Array.Empty<EnrichedMetadata>();
        }

        _logger.LogInformation("Starting batch metadata extraction for {Count} items", itemList.Count);

        // 병렬 처리 (API 호출 최적화)
        var tasks = itemList.Select(item =>
            ExtractAsync(item.content, item.url, item.htmlMetadata, schema, customPrompt, cancellationToken));

        var results = await Task.WhenAll(tasks);

        _logger.LogInformation("Batch extraction completed. Processed {Count} items", results.Length);

        return results;
    }

    /// <inheritdoc />
    public IReadOnlyList<MetadataSchema> GetSupportedSchemas()
    {
        return Enum.GetValues<MetadataSchema>().ToList();
    }

    /// <inheritdoc />
    public string GetSchemaDescription(MetadataSchema schema)
    {
        return schema switch
        {
            MetadataSchema.General => "일반 웹 콘텐츠 (블로그, 랜딩 페이지). 추출: topics, keywords, description, documentType, language, categories",
            MetadataSchema.TechnicalDoc => "기술 문서 (react.dev, MDN, API 문서). 추출: topics, libraries, frameworks, technologies, apiVersion, keywords",
            MetadataSchema.ProductManual => "제품 페이지 (전자상거래, 제품 상세). 추출: productName, company, version, model, price, currency, categories",
            MetadataSchema.Article => "블로그/뉴스 기사. 추출: articleTitle, author, publishedDate, tags, readingTimeMinutes",
            MetadataSchema.Custom => "사용자 정의 스키마 (customPrompt 필수)",
            _ => "Unknown schema"
        };
    }

    // ===================================================================
    // Private Helper Methods
    // ===================================================================

    /// <summary>
    /// 스키마별 AI 프롬프트 생성
    /// </summary>
    private string BuildPrompt(
        string content,
        string url,
        HtmlMetadataSnapshot? htmlMetadata,
        MetadataSchema schema,
        string? customPrompt)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a metadata extraction expert. Analyze the following web content and extract structured metadata in JSON format.");
        sb.AppendLine();
        sb.AppendLine($"**URL**: {url}");
        sb.AppendLine();

        // HTML 메타데이터 힌트 추가
        if (htmlMetadata != null)
        {
            sb.AppendLine("**HTML Metadata Hints**:");
            if (htmlMetadata.OpenGraph != null)
            {
                sb.AppendLine($"- Title: {htmlMetadata.OpenGraph.Title}");
                sb.AppendLine($"- Description: {htmlMetadata.OpenGraph.Description}");
                sb.AppendLine($"- Type: {htmlMetadata.OpenGraph.Type}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("**Content**:");
        sb.AppendLine(content);
        sb.AppendLine();

        // 스키마별 프롬프트
        if (schema == MetadataSchema.Custom && !string.IsNullOrWhiteSpace(customPrompt))
        {
            sb.AppendLine(customPrompt);
        }
        else
        {
            sb.AppendLine(GetSchemaPrompt(schema));
        }

        sb.AppendLine();
        sb.AppendLine("Return ONLY valid JSON with no additional text. Include a 'confidence' field (0.0-1.0) for overall extraction quality.");

        return sb.ToString();
    }

    /// <summary>
    /// 스키마별 추출 프롬프트 반환
    /// </summary>
    private string GetSchemaPrompt(MetadataSchema schema)
    {
        return schema switch
        {
            MetadataSchema.General => @"
Extract the following fields in JSON format:
{
  ""title"": ""Main page title"",
  ""description"": ""Brief description (1-2 sentences)"",
  ""topics"": [""topic1"", ""topic2"", ""topic3""],
  ""keywords"": [""keyword1"", ""keyword2"", ""keyword3""],
  ""contentType"": ""article|documentation|product|tutorial|blog"",
  ""siteStructure"": ""blog|documentation|product|news|forum"",
  ""language"": ""en|ko|ja|zh"",
  ""confidence"": 0.95
}",

            MetadataSchema.TechnicalDoc => @"
Extract the following fields for technical documentation:
{
  ""title"": ""Documentation title"",
  ""description"": ""Brief description"",
  ""topics"": [""React Hooks"", ""useState"", ""useEffect""],
  ""keywords"": [""react"", ""hooks"", ""state management""],
  ""schemaSpecificData"": {
    ""libraries"": [""react@18.2.0"", ""react-dom@18.2.0""],
    ""frameworks"": [""React""],
    ""technologies"": [""JavaScript"", ""TypeScript""],
    ""apiVersion"": ""v18"",
    ""codeLanguages"": [""javascript"", ""typescript""]
  },
  ""confidence"": 0.95
}",

            MetadataSchema.ProductManual => @"
Extract the following fields for product pages:
{
  ""title"": ""Product name"",
  ""description"": ""Product description"",
  ""schemaSpecificData"": {
    ""productName"": ""iPhone 15 Pro"",
    ""company"": ""Apple"",
    ""version"": ""15"",
    ""model"": ""Pro"",
    ""price"": 999.00,
    ""currency"": ""USD"",
    ""categories"": [""smartphones"", ""electronics""]
  },
  ""confidence"": 0.95
}",

            MetadataSchema.Article => @"
Extract the following fields for articles/blog posts:
{
  ""title"": ""Article title"",
  ""description"": ""Brief summary"",
  ""author"": ""John Doe"",
  ""publishedDate"": ""2024-01-10"",
  ""topics"": [""JavaScript"", ""Patterns"", ""Best Practices""],
  ""keywords"": [""javascript"", ""design patterns"", ""clean code""],
  ""schemaSpecificData"": {
    ""tags"": [""javascript"", ""patterns""],
    ""readingTimeMinutes"": 8,
    ""articleType"": ""tutorial|opinion|news|review""
  },
  ""confidence"": 0.95
}",

            _ => throw new ArgumentException($"Unsupported schema: {schema}")
        };
    }

    /// <summary>
    /// AI 응답 JSON 파싱
    /// </summary>
    private Dictionary<string, object> ParseAiResponse(string response)
    {
        try
        {
            // JSON 추출 (마크다운 코드 블록 제거)
            var jsonContent = response.Trim();
            if (jsonContent.StartsWith("```"))
            {
                var lines = jsonContent.Split('\n');
                jsonContent = string.Join('\n', lines.Skip(1).SkipLast(1));
            }

            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
            return result ?? new Dictionary<string, object>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON: {Response}", response);
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// HTML 메타데이터와 AI 메타데이터 융합
    /// </summary>
    private EnrichedMetadata MergeWithHtmlMetadata(
        Dictionary<string, object> aiMetadata,
        HtmlMetadataSnapshot? htmlMetadata,
        string url)
    {
        var enriched = new EnrichedMetadata
        {
            Url = url,
            Domain = new Uri(url).Host,
            ExtractedAt = DateTimeOffset.UtcNow,
            Source = htmlMetadata != null ? MetadataSource.Merged : MetadataSource.AI,
            HtmlMetadata = htmlMetadata
        };

        // HTML 우선, AI로 보완 전략
        if (htmlMetadata?.OpenGraph != null)
        {
            enriched.Title = htmlMetadata.OpenGraph.Title;
            enriched.Description = htmlMetadata.OpenGraph.Description;
            enriched.FieldSources["title"] = MetadataSource.Html;
            enriched.FieldSources["description"] = MetadataSource.Html;
        }

        // AI 메타데이터 추가
        if (aiMetadata.TryGetValue("title", out var aiTitle) && string.IsNullOrWhiteSpace(enriched.Title))
        {
            enriched.Title = aiTitle.ToString();
            enriched.FieldSources["title"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("description", out var aiDesc) && string.IsNullOrWhiteSpace(enriched.Description))
        {
            enriched.Description = aiDesc.ToString();
            enriched.FieldSources["description"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("author", out var author))
        {
            enriched.Author = author.ToString();
            enriched.FieldSources["author"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("language", out var lang))
        {
            enriched.Language = lang.ToString();
            enriched.FieldSources["language"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("topics", out var topics) && topics is JsonElement topicsJson && topicsJson.ValueKind == JsonValueKind.Array)
        {
            enriched.Topics = topicsJson.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            enriched.FieldSources["topics"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("keywords", out var keywords) && keywords is JsonElement keywordsJson && keywordsJson.ValueKind == JsonValueKind.Array)
        {
            enriched.Keywords = keywordsJson.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            enriched.FieldSources["keywords"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("contentType", out var contentType))
        {
            enriched.ContentType = contentType.ToString();
            enriched.FieldSources["contentType"] = MetadataSource.AI;
        }

        if (aiMetadata.TryGetValue("siteStructure", out var siteStructure))
        {
            enriched.SiteStructure = siteStructure.ToString();
            enriched.FieldSources["siteStructure"] = MetadataSource.AI;
        }

        // 스키마별 데이터
        if (aiMetadata.TryGetValue("schemaSpecificData", out var schemaData) && schemaData is JsonElement schemaJson)
        {
            enriched.SchemaSpecificData = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson.GetRawText()) ?? new Dictionary<string, object>();
        }

        // 신뢰도
        if (aiMetadata.TryGetValue("confidence", out var confidence))
        {
            if (confidence is JsonElement confJson && confJson.ValueKind == JsonValueKind.Number)
            {
                enriched.OverallConfidence = (float)confJson.GetDouble();
            }
            else if (float.TryParse(confidence.ToString(), out var confFloat))
            {
                enriched.OverallConfidence = confFloat;
            }
        }

        return enriched;
    }
}
