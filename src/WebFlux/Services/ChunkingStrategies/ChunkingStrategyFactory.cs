using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using ChunkingOptions = WebFlux.Core.Options.ChunkingOptions;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 청킹 전략 팩토리 구현
/// Phase 4D: Auto 전략과 MemoryOptimized 전략을 포함한 7가지 전략 지원
/// </summary>
public class ChunkingStrategyFactory : IChunkingStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChunkingStrategyFactory> _logger;
    private readonly Dictionary<string, Func<IChunkingStrategy>> _strategyCreators;
    private readonly Dictionary<string, StrategyInfo> _strategyInfos;

    public ChunkingStrategyFactory(
        IServiceProvider serviceProvider,
        ILogger<ChunkingStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategyCreators = new Dictionary<string, Func<IChunkingStrategy>>(StringComparer.OrdinalIgnoreCase);
        _strategyInfos = new Dictionary<string, StrategyInfo>(StringComparer.OrdinalIgnoreCase);

        InitializeStrategies();
    }

    public async Task<IChunkingStrategy> CreateStrategyAsync(string strategyName)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(strategyName))
        {
            throw new ArgumentException("전략 이름이 필요합니다.", nameof(strategyName));
        }

        if (!_strategyCreators.TryGetValue(strategyName, out var creator))
        {
            _logger.LogWarning("알 수 없는 청킹 전략: {StrategyName}, Paragraph 전략으로 대체", strategyName);
            creator = _strategyCreators["Paragraph"];
        }

        try
        {
            var strategy = creator();
            _logger.LogDebug("청킹 전략 생성 완료: {StrategyName}", strategy.Name);
            return strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "청킹 전략 생성 실패: {StrategyName}", strategyName);
            throw new InvalidOperationException($"청킹 전략 '{strategyName}' 생성에 실패했습니다.", ex);
        }
    }

    public IEnumerable<string> GetAvailableStrategies()
    {
        return _strategyCreators.Keys;
    }


    public async Task<StrategyInfo> GetStrategyInfoAsync(string strategyName)
    {
        await Task.CompletedTask;

        if (_strategyInfos.TryGetValue(strategyName, out var info))
        {
            return info;
        }

        throw new ArgumentException($"알 수 없는 전략: {strategyName}");
    }

    public async Task<string> RecommendStrategyAsync(ExtractedContent content, ChunkingOptions? options = null)
    {
        await Task.CompletedTask;

        try
        {
            var contentLength = content.MainContent?.Length ?? 0;
            var hasImages = content.ImageUrls?.Any() ?? false;
            var hasHeadings = content.Headings?.Any() ?? false;
            var isTechnical = AnalyzeTechnicalContent(content);

            // 메모리 제약이 있는 경우
            if (options?.MinimizeMemoryUsage == true || contentLength > 100000)
            {
                return "MemoryOptimized";
            }

            // Auto 전략이 사용 가능한 경우 (메타데이터 기반 선택)
            if (HasMetadataContext(content))
            {
                return "Auto";
            }

            // 멀티미디어 콘텐츠
            if (hasImages && contentLength > 5000)
            {
                return "Smart";
            }

            // 기술 문서
            if (isTechnical || hasHeadings)
            {
                return "Smart";
            }

            // 긴 텍스트 문서
            if (contentLength > 10000)
            {
                return "Semantic";
            }

            // 일반 문서
            return "Paragraph";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "전략 추천 중 오류 발생, 기본 전략 반환");
            return "Paragraph";
        }
    }

    /// <summary>
    /// 전략 초기화
    /// </summary>
    private void InitializeStrategies()
    {
        // 1. FixedSize - 고정 크기 분할
        _strategyCreators["FixedSize"] = () =>
            _serviceProvider.GetRequiredService<FixedSizeChunkingStrategy>();

        _strategyInfos["FixedSize"] = new StrategyInfo
        {
            Name = "FixedSize",
            Description = "고정 크기 기반 청킹 - 단순하고 예측 가능한 분할",
            PerformanceInfo = new PerformanceInfo
            {
                AverageProcessingTime = TimeSpan.FromMilliseconds(50),
                MemoryUsage = "Low",
                CPUIntensity = "Low",
                Scalability = "Excellent"
            },
            UseCases = new List<string> { "단순 텍스트", "성능 우선", "일관된 크기 필요" },
            SuitableContentTypes = new List<string> { "일반 텍스트", "로그 파일", "데이터 덤프" }
        };

        // 2. Paragraph - 문단 기반 분할
        _strategyCreators["Paragraph"] = () =>
            _serviceProvider.GetRequiredService<ParagraphChunkingStrategy>();

        _strategyInfos["Paragraph"] = new StrategyInfo
        {
            Name = "Paragraph",
            Description = "문단 기반 청킹 - 자연스러운 텍스트 경계 보존",
            PerformanceInfo = new PerformanceInfo
            {
                AverageProcessingTime = TimeSpan.FromMilliseconds(80),
                MemoryUsage = "Low",
                CPUIntensity = "Low",
                Scalability = "High"
            },
            UseCases = new List<string> { "일반 기사", "블로그 포스트", "안정적인 기본 전략" },
            SuitableContentTypes = new List<string> { "뉴스 기사", "블로그", "에세이", "소설" }
        };

        // 3. Smart - 구조 인식 분할
        _strategyCreators["Smart"] = () =>
            _serviceProvider.GetRequiredService<SmartChunkingStrategy>();

        _strategyInfos["Smart"] = new StrategyInfo
        {
            Name = "Smart",
            Description = "구조 인식 청킹 - HTML/Markdown 헤더 기반 맥락 보존",
            PerformanceInfo = new PerformanceInfo
            {
                AverageProcessingTime = TimeSpan.FromMilliseconds(120),
                MemoryUsage = "Medium",
                CPUIntensity = "Medium",
                Scalability = "High"
            },
            UseCases = new List<string> { "기술 문서", "구조화된 콘텐츠", "헤딩이 있는 문서" },
            SuitableContentTypes = new List<string> { "기술 문서", "가이드", "매뉴얼", "위키" }
        };

        // 4. Semantic - 의미론적 분할
        _strategyCreators["Semantic"] = () =>
            _serviceProvider.GetRequiredService<SemanticChunkingStrategy>();

        _strategyInfos["Semantic"] = new StrategyInfo
        {
            Name = "Semantic",
            Description = "의미론적 청킹 - 임베딩 기반 의미적 일관성 최적화",
            PerformanceInfo = new PerformanceInfo
            {
                AverageProcessingTime = TimeSpan.FromMilliseconds(500),
                MemoryUsage = "High",
                CPUIntensity = "High",
                Scalability = "Medium"
            },
            UseCases = new List<string> { "고품질 RAG", "의미적 일관성 중요", "학술 문서" },
            SuitableContentTypes = new List<string> { "연구 논문", "학술 기사", "복잡한 텍스트" }
        };

        // 5. Auto - 지능형 자동 선택
        _strategyCreators["Auto"] = () =>
            _serviceProvider.GetRequiredService<AutoChunkingStrategy>();

        _strategyInfos["Auto"] = new StrategyInfo
        {
            Name = "Auto",
            Description = "지능형 자동 전략 선택 - 메타데이터 기반 최적화",
            PerformanceInfo = new PerformanceInfo
            {
                AverageProcessingTime = TimeSpan.FromMilliseconds(200),
                MemoryUsage = "Variable",
                CPUIntensity = "Medium",
                Scalability = "High"
            },
            UseCases = new List<string> { "다양한 콘텐츠 타입", "최적화 자동화", "메타데이터 활용" },
            SuitableContentTypes = new List<string> { "모든 웹 콘텐츠", "ai.txt 지원 사이트", "메타데이터 풍부한 사이트" }
        };

        // 6. MemoryOptimized - 메모리 최적화
        _strategyCreators["MemoryOptimized"] = () =>
            _serviceProvider.GetRequiredService<MemoryOptimizedChunkingStrategy>();

        _strategyInfos["MemoryOptimized"] = new StrategyInfo
        {
            Name = "MemoryOptimized",
            Description = "메모리 효율성 최적화 - 대용량 문서 처리 전용",
            PerformanceInfo = new PerformanceInfo
            {
                AverageProcessingTime = TimeSpan.FromMilliseconds(80),
                MemoryUsage = "Optimized (84% reduction)",
                CPUIntensity = "Low",
                Scalability = "Excellent"
            },
            UseCases = new List<string> { "대용량 문서", "메모리 제약 환경", "스트리밍 처리" },
            SuitableContentTypes = new List<string> { "대용량 텍스트", "로그 파일", "데이터 덤프" }
        };

        _logger.LogInformation("청킹 전략 팩토리 초기화 완료: {StrategyCount}개 전략", _strategyCreators.Count);
    }

    /// <summary>
    /// 기술적 콘텐츠 분석
    /// </summary>
    private bool AnalyzeTechnicalContent(ExtractedContent content)
    {
        var text = content.MainContent?.ToLowerInvariant() ?? "";
        var technicalKeywords = new[]
        {
            "class ", "function ", "method ", "api ", "```", "code", "example",
            "parameter", "return", "import", "export", "interface", "type"
        };

        var techCount = technicalKeywords.Count(keyword => text.Contains(keyword));
        return techCount >= 3;
    }

    /// <summary>
    /// 메타데이터 컨텍스트 존재 여부 확인
    /// </summary>
    private bool HasMetadataContext(ExtractedContent content)
    {
        // URL에서 메타데이터 가능성 추정
        var url = content.Url?.ToLowerInvariant() ?? "";

        // 일반적으로 메타데이터가 풍부한 사이트들
        var metadataRichSites = new[]
        {
            "github.com", "stackoverflow.com", "medium.com", "dev.to",
            "docs.", "api.", "learn.", "guide.", "manual."
        };

        return metadataRichSites.Any(site => url.Contains(site)) ||
               !string.IsNullOrEmpty(content.Title) ||
               content.Headings?.Any() == true;
    }
}