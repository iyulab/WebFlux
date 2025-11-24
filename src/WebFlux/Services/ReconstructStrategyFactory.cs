using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Strategies.Reconstruct;

namespace WebFlux.Services;

/// <summary>
/// 재구성 전략 팩토리 구현
/// 전략 선택, 생성 및 최적화 자동 선택 기능 제공
/// </summary>
public class ReconstructStrategyFactory : IReconstructStrategyFactory
{
    private readonly ITextCompletionService? _llmService;
    private readonly ILogger<ReconstructStrategyFactory> _logger;
    private readonly Dictionary<string, Func<IReconstructStrategy>> _strategyCreators;

    public ReconstructStrategyFactory(
        ITextCompletionService? llmService = null,
        ILogger<ReconstructStrategyFactory>? logger = null)
    {
        _llmService = llmService;
        _logger = logger ?? NullLogger<ReconstructStrategyFactory>.Instance;

        // 전략 생성자 등록
        _strategyCreators = new Dictionary<string, Func<IReconstructStrategy>>(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = () => new NoneReconstructStrategy(),
            ["Summarize"] = () => new SummarizeReconstructStrategy(_llmService),
            ["Expand"] = () => new ExpandReconstructStrategy(_llmService),
            ["Rewrite"] = () => new RewriteReconstructStrategy(_llmService),
            ["Enrich"] = () => new EnrichReconstructStrategy(_llmService)
        };

        // LLM 서비스 가용성 로깅
        if (_llmService == null)
        {
            _logger.LogInformation(
                "ITextCompletionService not registered. LLM-based reconstruction strategies (Summarize, Expand, Rewrite, Enrich) will not be available. " +
                "Only 'None' strategy will work without quality degradation.");
        }
        else
        {
            _logger.LogDebug("ITextCompletionService registered. All reconstruction strategies are available.");
        }
    }

    public IEnumerable<string> GetAvailableStrategies()
    {
        return _strategyCreators.Keys;
    }

    public IReconstructStrategy CreateStrategy(string strategyName)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            _logger.LogWarning("Strategy name is null or empty, defaulting to 'None' strategy");
            return new NoneReconstructStrategy();
        }

        if (!_strategyCreators.TryGetValue(strategyName, out var creator))
        {
            _logger.LogWarning(
                "Unknown strategy '{StrategyName}' requested. Available strategies: {AvailableStrategies}. Defaulting to 'None'",
                strategyName,
                string.Join(", ", GetAvailableStrategies()));
            return new NoneReconstructStrategy();
        }

        var strategy = creator();

        // LLM 전략인데 서비스가 없으면 경고
        if (strategyName != "None" && _llmService == null)
        {
            _logger.LogWarning(
                "Strategy '{StrategyName}' requires ITextCompletionService, but it is not registered. " +
                "This strategy will fail if used. Consider registering ITextCompletionService or using 'None' strategy.",
                strategyName);
        }

        _logger.LogDebug("Created reconstruction strategy: {StrategyName}", strategyName);
        return strategy;
    }

    public IReconstructStrategy CreateOptimalStrategy(AnalyzedContent content, ReconstructOptions options)
    {
        // 사용자가 명시적으로 전략을 지정했으면 그것을 사용
        if (!string.IsNullOrEmpty(options.Strategy) && options.Strategy != "Auto")
        {
            _logger.LogInformation("Using explicitly specified strategy: {Strategy}", options.Strategy);
            return CreateStrategy(options.Strategy);
        }

        // Auto 전략 선택 로직
        _logger.LogDebug("Auto-selecting optimal reconstruction strategy based on content analysis");

        // LLM 서비스가 없거나 UseLLM이 false면 무조건 None
        if (_llmService == null || !options.UseLLM)
        {
            if (_llmService == null)
            {
                _logger.LogInformation(
                    "ITextCompletionService not available. Using 'None' strategy. " +
                    "Output quality: Original content preserved without enhancement.");
            }
            else
            {
                _logger.LogInformation(
                    "UseLLM is false. Using 'None' strategy. " +
                    "Set ReconstructOptions.UseLLM = true to enable LLM-based strategies.");
            }
            return new NoneReconstructStrategy();
        }

        // 콘텐츠 분석 기반 전략 선택
        var selectedStrategy = AnalyzeAndSelectStrategy(content, options);

        _logger.LogInformation(
            "Auto-selected strategy: {Strategy} (Content length: {Length}, Quality: {Quality:F2})",
            selectedStrategy,
            content.CleanedContent.Length,
            content.Metrics?.ContentQuality ?? 0);

        return CreateStrategy(selectedStrategy);
    }

    public Dictionary<string, ReconstructStrategyCharacteristics> GetStrategyCharacteristics()
    {
        return new Dictionary<string, ReconstructStrategyCharacteristics>
        {
            ["None"] = new ReconstructStrategyCharacteristics
            {
                Name = "None",
                Description = "재구성 없이 원본 콘텐츠를 그대로 유지",
                QualityLevel = QualityLevel.High,
                MemoryUsage = MemoryUsage.Low,
                ComputationCost = ComputationCost.Low,
                RequiresLLM = false,
                RecommendedUseCases = new[]
                {
                    "빠른 처리가 필요한 경우",
                    "원본 콘텐츠 품질이 충분히 높은 경우",
                    "LLM 비용을 절감해야 하는 경우"
                }
            },
            ["Summarize"] = new ReconstructStrategyCharacteristics
            {
                Name = "Summarize",
                Description = "LLM을 활용한 콘텐츠 요약 생성",
                QualityLevel = QualityLevel.High,
                MemoryUsage = MemoryUsage.Low,
                ComputationCost = ComputationCost.Medium,
                RequiresLLM = true,
                RecommendedUseCases = new[]
                {
                    "긴 문서를 짧게 압축해야 하는 경우",
                    "핵심 정보만 추출이 필요한 경우",
                    "토큰 사용량을 줄여야 하는 경우"
                }
            },
            ["Expand"] = new ReconstructStrategyCharacteristics
            {
                Name = "Expand",
                Description = "LLM을 활용한 콘텐츠 확장 및 상세 설명 추가",
                QualityLevel = QualityLevel.VeryHigh,
                MemoryUsage = MemoryUsage.Medium,
                ComputationCost = ComputationCost.High,
                RequiresLLM = true,
                RecommendedUseCases = new[]
                {
                    "간략한 문서를 더 자세하게 만들어야 하는 경우",
                    "추가 설명과 예시가 필요한 경우",
                    "학습 자료로 활용하기 위한 경우"
                }
            },
            ["Rewrite"] = new ReconstructStrategyCharacteristics
            {
                Name = "Rewrite",
                Description = "LLM을 활용한 콘텐츠 재작성으로 명확성 및 일관성 향상",
                QualityLevel = QualityLevel.VeryHigh,
                MemoryUsage = MemoryUsage.Medium,
                ComputationCost = ComputationCost.High,
                RequiresLLM = true,
                RecommendedUseCases = new[]
                {
                    "RAG 검색 최적화를 위한 재작성",
                    "일관성 없는 문서를 정리해야 하는 경우",
                    "특정 스타일로 통일이 필요한 경우"
                }
            },
            ["Enrich"] = new ReconstructStrategyCharacteristics
            {
                Name = "Enrich",
                Description = "LLM을 활용한 추가 컨텍스트 및 메타데이터 보강",
                QualityLevel = QualityLevel.VeryHigh,
                MemoryUsage = MemoryUsage.Medium,
                ComputationCost = ComputationCost.VeryHigh,
                RequiresLLM = true,
                RecommendedUseCases = new[]
                {
                    "RAG 검색 정확도 향상이 필요한 경우",
                    "추가 배경지식과 정의가 필요한 경우",
                    "관련 정보 연결이 필요한 경우"
                }
            }
        };
    }

    private string AnalyzeAndSelectStrategy(AnalyzedContent content, ReconstructOptions options)
    {
        var contentLength = content.CleanedContent.Length;
        var quality = content.Metrics?.ContentQuality ?? 0;
        var hasImages = content.Images?.Any() ?? false;

        // 매우 긴 콘텐츠 → 요약
        if (contentLength > 10000)
        {
            return "Summarize";
        }

        // 품질이 낮은 콘텐츠 → 재작성
        if (quality < 0.6)
        {
            return "Rewrite";
        }

        // 짧은 콘텐츠 → 확장
        if (contentLength < 500)
        {
            return "Expand";
        }

        // 이미지가 많거나 기술 문서 → 보강
        if (hasImages || content.Sections?.Count > 5)
        {
            return "Enrich";
        }

        // 기본값: 재작성 (가장 범용적)
        return "Rewrite";
    }
}
