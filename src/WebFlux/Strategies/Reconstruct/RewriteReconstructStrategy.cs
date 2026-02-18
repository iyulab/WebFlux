using System.Diagnostics;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Strategies.Reconstruct;

/// <summary>
/// Rewrite 재구성 전략
/// LLM을 활용한 콘텐츠 재작성으로 명확성 및 일관성 향상
/// </summary>
public class RewriteReconstructStrategy : IReconstructStrategy
{
    private readonly ITextCompletionService? _llmService;

    public string Name => "Rewrite";

    public string Description => "LLM을 활용하여 콘텐츠를 더 명확하고 일관되게 재작성합니다";

    public IEnumerable<string> RecommendedUseCases => new[]
    {
        "RAG 검색 최적화를 위한 재작성",
        "일관성 없는 문서를 정리해야 하는 경우",
        "특정 스타일로 통일이 필요한 경우"
    };

    public RewriteReconstructStrategy(ITextCompletionService? llmService = null)
    {
        _llmService = llmService;
    }

    public bool IsApplicable(AnalyzedContent content, ReconstructOptions options)
    {
        return _llmService != null && options.UseLLM;
    }

    public async Task<ReconstructedContent> ApplyAsync(
        AnalyzedContent content,
        ReconstructOptions options,
        CancellationToken cancellationToken = default)
    {
        if (_llmService == null)
        {
            throw new InvalidOperationException(
                "ITextCompletionService is required for Rewrite strategy. " +
                "Please register ITextCompletionService in your dependency injection container, " +
                "or use ReconstructOptions.Strategy = \"None\" for basic reconstruction without LLM.");
        }

        var stopwatch = Stopwatch.StartNew();

        // 재작성 프롬프트 생성
        var prompt = BuildRewritePrompt(content, options.RewriteStyle, options.ContextPrompt);

        // LLM 호출
        var rewritten = await _llmService.CompleteAsync(
            prompt,
            new TextCompletionOptions
            {
                MaxTokens = options.MaxTokens ?? 3000,
                Temperature = (float)options.Temperature
            },
            cancellationToken);

        stopwatch.Stop();

        // 재구성된 콘텐츠 생성
        var result = new ReconstructedContent
        {
            Url = content.Url,
            OriginalContent = content.CleanedContent,
            ReconstructedText = rewritten,
            StrategyUsed = Name,
            Metadata = content.Metadata,
            UsedLLM = true,
            Enhancements = new List<ContentEnhancement>
            {
                new ContentEnhancement("Rewrite", rewritten, 0)
                {
                    Metadata = new Dictionary<string, object>
                    {
                        ["Style"] = options.RewriteStyle
                    }
                }
            },
            Metrics = new ReconstructMetrics
            {
                Quality = 0.85, // 재작성은 일반적으로 높은 품질
                CompressionRatio = (double)rewritten.Length / content.CleanedContent.Length,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMCallCount = 1,
                TokensUsed = (prompt.Length + rewritten.Length) / 4
            }
        };

        return result;
    }

    public TimeSpan EstimateProcessingTime(AnalyzedContent content, ReconstructOptions options)
    {
        var estimatedTokens = content.CleanedContent.Length / 4 * 1.2; // 약간 증가 예상
        return TimeSpan.FromMilliseconds(estimatedTokens * 10);
    }

    private static string BuildRewritePrompt(AnalyzedContent content, string style, string? additionalContext)
    {
        var styleInstructions = style.ToLowerInvariant() switch
        {
            "formal" => "Use formal, professional language appropriate for business or academic settings.",
            "casual" => "Use conversational, friendly language that's easy to understand.",
            "technical" => "Use precise technical terminology and maintain technical accuracy.",
            "simple" => "Use simple, clear language avoiding jargon and complex terms.",
            _ => "Maintain the original style while improving clarity and consistency."
        };

        var contextSection = !string.IsNullOrEmpty(additionalContext)
            ? $"\n\nAdditional Instructions: {additionalContext}"
            : "";

        return $@"Please rewrite the following content to improve clarity, consistency, and readability while preserving all key information.

Style Requirements: {styleInstructions}

Title: {content.Title}

Original Content:
{content.CleanedContent}
{contextSection}

Rewritten Content:";
    }
}
