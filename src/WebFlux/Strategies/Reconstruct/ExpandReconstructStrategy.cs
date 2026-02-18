using System.Diagnostics;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Strategies.Reconstruct;

/// <summary>
/// Expand 재구성 전략
/// LLM을 활용한 콘텐츠 확장 및 상세 설명 추가
/// </summary>
public class ExpandReconstructStrategy : IReconstructStrategy
{
    private readonly ITextCompletionService? _llmService;

    public string Name => "Expand";

    public string Description => "LLM을 활용하여 콘텐츠를 더 상세하게 확장합니다";

    public IEnumerable<string> RecommendedUseCases => new[]
    {
        "간략한 문서를 더 자세하게 만들어야 하는 경우",
        "추가 설명과 예시가 필요한 경우",
        "학습 자료로 활용하기 위한 경우"
    };

    public ExpandReconstructStrategy(ITextCompletionService? llmService = null)
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
                "ITextCompletionService is required for Expand strategy. " +
                "Please register ITextCompletionService in your dependency injection container, " +
                "or use ReconstructOptions.Strategy = \"None\" for basic reconstruction without LLM.");
        }

        var stopwatch = Stopwatch.StartNew();

        // 확장 프롬프트 생성
        var prompt = BuildExpansionPrompt(content, options.ExpansionRatio, options.ContextPrompt);

        // LLM 호출
        var expanded = await _llmService.CompleteAsync(
            prompt,
            new TextCompletionOptions
            {
                MaxTokens = options.MaxTokens ?? 4000,
                Temperature = (float)options.Temperature
            },
            cancellationToken);

        stopwatch.Stop();

        // 재구성된 콘텐츠 생성
        var result = new ReconstructedContent
        {
            Url = content.Url,
            OriginalContent = content.CleanedContent,
            ReconstructedText = expanded,
            StrategyUsed = Name,
            Metadata = content.Metadata,
            UsedLLM = true,
            Enhancements = new List<ContentEnhancement>
            {
                new ContentEnhancement("Expansion", expanded)
            },
            Metrics = new ReconstructMetrics
            {
                Quality = EstimateQuality(expanded, content.CleanedContent),
                CompressionRatio = (double)expanded.Length / content.CleanedContent.Length,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMCallCount = 1,
                TokensUsed = (prompt.Length + expanded.Length) / 4
            }
        };

        return result;
    }

    public TimeSpan EstimateProcessingTime(AnalyzedContent content, ReconstructOptions options)
    {
        var estimatedTokens = content.CleanedContent.Length / 4 * (int)options.ExpansionRatio;
        return TimeSpan.FromMilliseconds(estimatedTokens * 10);
    }

    private static string BuildExpansionPrompt(AnalyzedContent content, double expansionRatio, string? additionalContext)
    {
        var targetLength = (int)(content.CleanedContent.Length * expansionRatio);
        var contextSection = !string.IsNullOrEmpty(additionalContext)
            ? $"\n\nAdditional Context: {additionalContext}"
            : "";

        return $@"Please expand the following content to approximately {targetLength} characters by adding:
- More detailed explanations
- Relevant examples
- Additional context and background information
- Clarifications of complex points

Title: {content.Title}

Original Content:
{content.CleanedContent}
{contextSection}

Expanded Content:";
    }

    private static double EstimateQuality(string expanded, string original)
    {
        var ratio = (double)expanded.Length / original.Length;

        if (ratio >= 1.3 && ratio <= 2.0)
            return 0.9; // 적절한 확장 비율
        if (ratio >= 1.1 && ratio <= 2.5)
            return 0.7;
        return 0.5;
    }
}
