using System.Diagnostics;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Strategies.Reconstruct;

/// <summary>
/// Summarize 재구성 전략
/// LLM을 활용한 콘텐츠 요약 생성
/// </summary>
public class SummarizeReconstructStrategy : IReconstructStrategy
{
    private readonly ITextCompletionService? _llmService;

    public string Name => "Summarize";

    public string Description => "LLM을 활용하여 콘텐츠를 간결하게 요약합니다";

    public IEnumerable<string> RecommendedUseCases => new[]
    {
        "긴 문서를 짧게 압축해야 하는 경우",
        "핵심 정보만 추출이 필요한 경우",
        "토큰 사용량을 줄여야 하는 경우"
    };

    public SummarizeReconstructStrategy(ITextCompletionService? llmService = null)
    {
        _llmService = llmService;
    }

    public bool IsApplicable(AnalyzedContent content, ReconstructOptions options)
    {
        // LLM 서비스가 있고, UseLLM이 true일 때만 적용 가능
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
                "ITextCompletionService is required for Summarize strategy. " +
                "Please register ITextCompletionService in your dependency injection container, " +
                "or use ReconstructOptions.Strategy = \"None\" for basic reconstruction without LLM.");
        }

        var stopwatch = Stopwatch.StartNew();

        // 요약 프롬프트 생성
        var targetLength = (int)(content.CleanedContent.Length * options.SummaryRatio);
        var prompt = BuildSummaryPrompt(content, targetLength, options.ContextPrompt);

        // LLM 호출
        var summary = await _llmService.CompleteAsync(
            prompt,
            new TextCompletionOptions
            {
                MaxTokens = options.MaxTokens ?? 2000,
                Temperature = (float)options.Temperature
            },
            cancellationToken);

        stopwatch.Stop();

        // 재구성된 콘텐츠 생성
        var result = new ReconstructedContent
        {
            Url = content.Url,
            OriginalContent = content.CleanedContent,
            ReconstructedText = summary,
            StrategyUsed = Name,
            Metadata = content.Metadata,
            UsedLLM = true,
            Enhancements = new List<ContentEnhancement>
            {
                new ContentEnhancement("Summary", summary)
            },
            Metrics = new ReconstructMetrics
            {
                Quality = EstimateQuality(summary, content.CleanedContent),
                CompressionRatio = (double)summary.Length / content.CleanedContent.Length,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMCallCount = 1,
                TokensUsed = EstimateTokens(prompt, summary)
            }
        };

        return result;
    }

    public TimeSpan EstimateProcessingTime(AnalyzedContent content, ReconstructOptions options)
    {
        // LLM 호출 기준 대략적인 예상 시간
        var estimatedTokens = content.CleanedContent.Length / 4; // 대략 1 토큰 = 4자
        return TimeSpan.FromMilliseconds(estimatedTokens * 10); // 토큰당 10ms 가정
    }

    private static string BuildSummaryPrompt(AnalyzedContent content, int targetLength, string? additionalContext)
    {
        var contextSection = !string.IsNullOrEmpty(additionalContext)
            ? $"\n\nAdditional Context: {additionalContext}"
            : "";

        return $@"Please summarize the following content to approximately {targetLength} characters while preserving the key information and main ideas.

Title: {content.Title}

Content:
{content.CleanedContent}
{contextSection}

Summary:";
    }

    private static double EstimateQuality(string summary, string original)
    {
        // 간단한 품질 추정: 요약 길이가 목표 범위 내에 있는지 확인
        var ratio = (double)summary.Length / original.Length;

        if (ratio >= 0.2 && ratio <= 0.5)
            return 0.9; // 이상적인 요약 비율
        if (ratio >= 0.1 && ratio <= 0.6)
            return 0.7;
        return 0.5; // 비율이 적절하지 않음
    }

    private static int EstimateTokens(string prompt, string response)
    {
        // 대략적인 토큰 수 추정 (1 토큰 ≈ 4자)
        return (prompt.Length + response.Length) / 4;
    }
}
