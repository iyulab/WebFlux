using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.AiEnhancement;

/// <summary>
/// 기본 AI 증강 서비스 구현
/// ITextCompletionService를 활용하여 콘텐츠 요약, 재작성, 메타데이터 추출 수행
/// </summary>
public class BasicAiEnhancementService : IAiEnhancementService
{
    private readonly ITextCompletionService _llm;
    private readonly ILogger<BasicAiEnhancementService> _logger;

    public BasicAiEnhancementService(
        ITextCompletionService llm,
        ILogger<BasicAiEnhancementService> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 콘텐츠 요약
    /// </summary>
    public async Task<string> SummarizeAsync(
        string content,
        SummaryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new SummaryOptions();

        _logger.LogInformation("    ⏳ AI: Generating summary ({Length} chars, style: {Style})...",
            content.Length, options.Style);

        var prompt = BuildSummaryPrompt(content, options);

        var completionOptions = new TextCompletionOptions
        {
            MaxTokens = options.MaxLength * 2, // 안전 마진
            Temperature = 0.3, // 일관성을 위해 낮은 temperature
            SystemPrompt = "You are an expert at creating concise, informative summaries."
        };

        var summary = await _llm.CompleteAsync(prompt, completionOptions, cancellationToken);

        _logger.LogInformation("    ✅ AI: Summary generated ({Length} chars)", summary.Length);

        return summary.Trim();
    }

    /// <summary>
    /// 콘텐츠 재작성
    /// </summary>
    public async Task<string> RewriteAsync(
        string content,
        RewriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new RewriteOptions();

        _logger.LogDebug("Rewriting content ({Length} chars) for audience: {Audience}",
            content.Length, options.TargetAudience);

        var prompt = BuildRewritePrompt(content, options);

        var completionOptions = new TextCompletionOptions
        {
            MaxTokens = Math.Max(content.Length * 2, 4000), // 충분한 토큰
            Temperature = 0.5, // 창의성과 일관성의 균형
            SystemPrompt = "You improve text clarity and readability while preserving meaning and factual accuracy."
        };

        var rewritten = await _llm.CompleteAsync(prompt, completionOptions, cancellationToken);

        _logger.LogDebug("Content rewritten: {Length} chars", rewritten.Length);

        return rewritten.Trim();
    }

    /// <summary>
    /// 메타데이터 추출
    /// </summary>
    public async Task<EnrichedMetadata> ExtractMetadataAsync(
        string content,
        MetadataExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MetadataExtractionOptions();

        _logger.LogInformation("    ⏳ AI: Extracting metadata ({Length} chars)...", content.Length);

        var prompt = BuildMetadataPrompt(content, options);

        var completionOptions = new TextCompletionOptions
        {
            MaxTokens = 1500,
            Temperature = 0.2, // 일관성 중요
            SystemPrompt = "You extract structured metadata from content. Output valid JSON only."
        };

        var response = await _llm.CompleteAsync(prompt, completionOptions, cancellationToken);

        var metadata = ParseMetadataResponse(response, options);

        _logger.LogInformation("    ✅ AI: Metadata extracted ({KeywordCount} keywords, {TopicCount} topics)",
            metadata.Keywords.Count, metadata.Topics.Count);

        return metadata;
    }

    /// <summary>
    /// 통합 증강 (병렬 처리 지원)
    /// </summary>
    public async Task<EnhancedContent> EnhanceAsync(
        string content,
        EnhancementOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new EnhancementOptions();

        var startTime = DateTimeOffset.UtcNow;

        _logger.LogInformation("Enhancing content ({Length} chars) - Summary: {EnableSummary}, Rewrite: {EnableRewrite}, Metadata: {EnableMetadata}",
            content.Length, options.EnableSummary, options.EnableRewrite, options.EnableMetadata);

        string? summary = null;
        string? rewritten = null;
        EnrichedMetadata? metadata = null;

        if (options.EnableParallelProcessing)
        {
            // 병렬 처리
            var tasks = new List<Task>();

            if (options.EnableSummary)
                tasks.Add(Task.Run(async () => summary = await SummarizeAsync(content, options.SummaryOptions, cancellationToken), cancellationToken));

            if (options.EnableRewrite)
                tasks.Add(Task.Run(async () => rewritten = await RewriteAsync(content, options.RewriteOptions, cancellationToken), cancellationToken));

            if (options.EnableMetadata)
                tasks.Add(Task.Run(async () => metadata = await ExtractMetadataAsync(content, options.MetadataOptions, cancellationToken), cancellationToken));

            await Task.WhenAll(tasks);
        }
        else
        {
            // 순차 처리
            if (options.EnableSummary)
                summary = await SummarizeAsync(content, options.SummaryOptions, cancellationToken);

            if (options.EnableRewrite)
                rewritten = await RewriteAsync(content, options.RewriteOptions, cancellationToken);

            if (options.EnableMetadata)
                metadata = await ExtractMetadataAsync(content, options.MetadataOptions, cancellationToken);
        }

        var processingTime = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation("Content enhancement completed in {Duration}ms", processingTime);

        return new EnhancedContent
        {
            OriginalContent = content,
            Summary = summary,
            RewrittenContent = rewritten,
            Metadata = metadata ?? new EnrichedMetadata(),
            ProcessedAt = DateTimeOffset.UtcNow,
            ProcessingTimeMs = processingTime
        };
    }

    /// <summary>
    /// 배치 증강
    /// </summary>
    public async Task<IReadOnlyList<EnhancedContent>> EnhanceBatchAsync(
        IEnumerable<string> contents,
        EnhancementOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EnhancedContent>();

        foreach (var content in contents)
        {
            var enhanced = await EnhanceAsync(content, options, cancellationToken);
            results.Add(enhanced);
        }

        return results;
    }

    /// <summary>
    /// 서비스 가용성 확인
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testContent = "This is a test content for checking AI service availability. It needs to be reasonably long to avoid token limit issues.";
            var summary = await SummarizeAsync(testContent, new SummaryOptions { MaxLength = 200 }, cancellationToken);
            return !string.IsNullOrWhiteSpace(summary);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "AI service availability check failed (this is expected during initialization)");
            return false;
        }
    }

    #region Prompt Builders

    private string BuildSummaryPrompt(string content, SummaryOptions options)
    {
        var styleInstructions = options.Style.ToLower() switch
        {
            "concise" => "Create a very concise summary in 2-3 sentences.",
            "detailed" => "Create a comprehensive summary covering all key points.",
            "bullet" => "Create a bullet-point summary with key takeaways.",
            _ => "Summarize the content."
        };

        var lengthInstruction = $"Keep the summary under {options.MaxLength} characters.";

        return $@"{styleInstructions} {lengthInstruction}

Content:
{TruncateContent(content, 6000)}

Summary:";
    }

    private string BuildRewritePrompt(string content, RewriteOptions options)
    {
        var audienceInstruction = options.TargetAudience.ToLower() switch
        {
            "beginner" => "Rewrite for absolute beginners with no prior knowledge.",
            "technical" => "Rewrite for technical professionals with domain expertise.",
            "expert" => "Rewrite for expert practitioners.",
            _ => "Rewrite for a general audience."
        };

        var instructions = new List<string> { audienceInstruction };

        if (options.SimplifyLanguage)
            instructions.Add("Use simple, clear language.");

        if (options.PreserveStructure)
            instructions.Add("Preserve the original structure and organization.");

        if (options.ExplainTechnicalTerms)
            instructions.Add("Explain technical terms in parentheses.");

        var toneInstruction = $"Use a {options.Tone} tone.";
        instructions.Add(toneInstruction);

        return $@"{string.Join(" ", instructions)}

Original Content:
{TruncateContent(content, 6000)}

Rewritten Content:";
    }

    private string BuildMetadataPrompt(string content, MetadataExtractionOptions options)
    {
        var fields = new List<string>();

        if (options.ExtractKeywords)
            fields.Add($"\"keywords\": [list of up to {options.MaxKeywords} relevant keywords]");

        if (options.ExtractTopics)
            fields.Add($"\"topics\": [list of up to {options.MaxTopics} main topics]");

        if (options.IdentifyTargetAudience)
            fields.Add("\"targetAudience\": \"description of target audience\"");

        if (options.EstimateReadingTime)
            fields.Add("\"estimatedReadingTimeMinutes\": number");

        if (options.ClassifyContentType)
            fields.Add("\"contentType\": \"type (article/tutorial/reference/news/blog)\"");

        if (options.AssessDifficulty)
            fields.Add("\"difficultyLevel\": \"level (beginner/intermediate/advanced)\"");

        if (options.AnalyzeSentiment)
            fields.Add("\"sentiment\": \"sentiment (positive/negative/neutral)\"");

        fields.Add("\"title\": \"descriptive title\"");
        fields.Add("\"description\": \"brief description\"");
        fields.Add("\"mainTopic\": \"primary topic\"");
        fields.Add("\"language\": \"language code (ko/en/ja)\"");

        return $@"Extract metadata from the following content and output as JSON:

{{
  {string.Join(",\n  ", fields)}
}}

Content:
{TruncateContent(content, 6000)}

Metadata JSON:";
    }

    #endregion

    #region Helpers

    private string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;

        return content.Substring(0, maxLength) + "\n\n[Content truncated...]";
    }

    private EnrichedMetadata ParseMetadataResponse(string response, MetadataExtractionOptions options)
    {
        try
        {
            // JSON 추출 (코드 블록 제거)
            var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}", RegexOptions.Multiline);
            var json = jsonMatch.Success ? jsonMatch.Value : response;

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var metadata = new EnrichedMetadata
            {
                Title = GetJsonString(root, "title"),
                Description = GetJsonString(root, "description"),
                Keywords = GetJsonStringArray(root, "keywords"),
                Language = GetJsonString(root, "language") ?? "en",
                Source = MetadataSource.AI,
                ExtractedAt = DateTimeOffset.UtcNow,
                OverallConfidence = 0.8f // 기본 신뢰도
            };

            // AI 전용 필드 설정
            var topics = GetJsonStringArray(root, "topics");
            if (topics.Any())
            {
                metadata.SchemaSpecificData["topics"] = topics;
                metadata.FieldSources["topics"] = MetadataSource.AI;
            }

            var mainTopic = GetJsonString(root, "mainTopic");
            if (!string.IsNullOrEmpty(mainTopic))
            {
                metadata.SchemaSpecificData["mainTopic"] = mainTopic;
                metadata.FieldSources["mainTopic"] = MetadataSource.AI;
            }

            var sentiment = GetJsonString(root, "sentiment");
            if (!string.IsNullOrEmpty(sentiment))
            {
                metadata.SchemaSpecificData["sentiment"] = sentiment;
                metadata.FieldSources["sentiment"] = MetadataSource.AI;
            }

            var targetAudience = GetJsonString(root, "targetAudience");
            if (!string.IsNullOrEmpty(targetAudience))
            {
                metadata.SchemaSpecificData["targetAudience"] = targetAudience;
                metadata.FieldSources["targetAudience"] = MetadataSource.AI;
            }

            var contentType = GetJsonString(root, "contentType");
            if (!string.IsNullOrEmpty(contentType))
            {
                metadata.SchemaSpecificData["contentType"] = contentType;
                metadata.FieldSources["contentType"] = MetadataSource.AI;
            }

            var difficultyLevel = GetJsonString(root, "difficultyLevel");
            if (!string.IsNullOrEmpty(difficultyLevel))
            {
                metadata.SchemaSpecificData["difficultyLevel"] = difficultyLevel;
                metadata.FieldSources["difficultyLevel"] = MetadataSource.AI;
            }

            // 필드별 신뢰도 설정
            metadata.FieldConfidence["title"] = 0.9f;
            metadata.FieldConfidence["description"] = 0.85f;
            metadata.FieldConfidence["keywords"] = 0.8f;
            metadata.FieldConfidence["language"] = 0.95f;

            // 필드 소스 설정
            metadata.FieldSources["title"] = MetadataSource.AI;
            metadata.FieldSources["description"] = MetadataSource.AI;
            metadata.FieldSources["keywords"] = MetadataSource.AI;
            metadata.FieldSources["language"] = MetadataSource.AI;

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse metadata JSON, returning empty metadata");
            return new EnrichedMetadata
            {
                Source = MetadataSource.AI,
                ExtractedAt = DateTimeOffset.UtcNow,
                OverallConfidence = 0.0f
            };
        }
    }

    private string? GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private int? GetJsonInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : null;
    }

    private IReadOnlyList<string> GetJsonStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (var item in prop.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
                list.Add(item.GetString() ?? string.Empty);
        }

        return list;
    }

    #endregion
}
