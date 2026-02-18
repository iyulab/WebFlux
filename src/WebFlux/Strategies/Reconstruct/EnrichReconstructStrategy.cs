using System.Diagnostics;
using System.Globalization;
using System.Text;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Strategies.Reconstruct;

/// <summary>
/// Enrich 재구성 전략
/// LLM을 활용한 추가 컨텍스트 및 메타데이터 보강
/// </summary>
public class EnrichReconstructStrategy : IReconstructStrategy
{
    private readonly ITextCompletionService? _llmService;

    public string Name => "Enrich";

    public string Description => "LLM을 활용하여 추가 컨텍스트와 메타데이터로 콘텐츠를 보강합니다";

    public IEnumerable<string> RecommendedUseCases => new[]
    {
        "RAG 검색 정확도 향상이 필요한 경우",
        "추가 배경지식과 정의가 필요한 경우",
        "관련 정보 연결이 필요한 경우"
    };

    public EnrichReconstructStrategy(ITextCompletionService? llmService = null)
    {
        _llmService = llmService;
    }

    public bool IsApplicable(AnalyzedContent content, ReconstructOptions options)
    {
        return _llmService != null && options.UseLLM && options.EnrichmentTypes.Count != 0;
    }

    public async Task<ReconstructedContent> ApplyAsync(
        AnalyzedContent content,
        ReconstructOptions options,
        CancellationToken cancellationToken = default)
    {
        if (_llmService == null)
        {
            throw new InvalidOperationException(
                "ITextCompletionService is required for Enrich strategy. " +
                "Please register ITextCompletionService in your dependency injection container, " +
                "or use ReconstructOptions.Strategy = \"None\" for basic reconstruction without LLM.");
        }

        var stopwatch = Stopwatch.StartNew();
        var enhancements = new List<ContentEnhancement>();
        var llmCallCount = 0;

        // 각 증강 타입별로 처리
        foreach (var enrichmentType in options.EnrichmentTypes)
        {
            var prompt = BuildEnrichmentPrompt(content, enrichmentType, options.ContextPrompt);

            var enrichedContent = await _llmService.CompleteAsync(
                prompt,
                new TextCompletionOptions
                {
                    MaxTokens = 1000,
                    Temperature = (float)options.Temperature
                },
                cancellationToken);

            enhancements.Add(new ContentEnhancement(enrichmentType, enrichedContent)
            {
                Confidence = 0.8
            });

            llmCallCount++;
        }

        stopwatch.Stop();

        // 원본 + 증강 통합
        var enrichedText = CombineEnhancements(content.CleanedContent, enhancements);

        // 재구성된 콘텐츠 생성
        var result = new ReconstructedContent
        {
            Url = content.Url,
            OriginalContent = content.CleanedContent,
            ReconstructedText = enrichedText,
            StrategyUsed = Name,
            Metadata = content.Metadata,
            UsedLLM = true,
            Enhancements = enhancements,
            Metrics = new ReconstructMetrics
            {
                Quality = 0.85,
                CompressionRatio = (double)enrichedText.Length / content.CleanedContent.Length,
                EnhancementBytes = enrichedText.Length - content.CleanedContent.Length,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMCallCount = llmCallCount,
                TokensUsed = EstimateTotalTokens(enhancements)
            }
        };

        return result;
    }

    public TimeSpan EstimateProcessingTime(AnalyzedContent content, ReconstructOptions options)
    {
        // 각 증강 타입마다 LLM 호출
        var estimatedTokensPerCall = 1000;
        var totalCalls = options.EnrichmentTypes.Count;
        return TimeSpan.FromMilliseconds(estimatedTokensPerCall * totalCalls * 10);
    }

    private static string BuildEnrichmentPrompt(AnalyzedContent content, string enrichmentType, string? additionalContext)
    {
        var instructions = enrichmentType.ToLowerInvariant() switch
        {
            "context" => "Provide relevant background context and historical information that would help understand this content better.",
            "definitions" => "Identify and define key technical terms, concepts, and acronyms mentioned in the content.",
            "examples" => "Provide concrete examples and use cases that illustrate the main concepts.",
            "relatedinfo" => "Identify related topics, concepts, and resources that complement this content.",
            _ => $"Provide {enrichmentType} enrichment for this content."
        };

        var contextSection = !string.IsNullOrEmpty(additionalContext)
            ? $"\n\nAdditional Context: {additionalContext}"
            : "";

        return $@"{instructions}

Title: {content.Title}

Content:
{content.CleanedContent}
{contextSection}

Enrichment:";
    }

    private static string CombineEnhancements(string originalContent, List<ContentEnhancement> enhancements)
    {
        if (enhancements.Count == 0)
            return originalContent;

        var sb = new StringBuilder();
        sb.AppendLine(originalContent);
        sb.AppendLine();
        sb.AppendLine("## Additional Context");

        foreach (var enhancement in enhancements)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"### {enhancement.Type}");
            sb.AppendLine(enhancement.Content);
        }

        return sb.ToString();
    }

    private static int EstimateTotalTokens(List<ContentEnhancement> enhancements)
    {
        return enhancements.Sum(e => e.Content.Length / 4);
    }
}
