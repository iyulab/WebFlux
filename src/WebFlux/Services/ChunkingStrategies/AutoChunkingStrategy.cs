using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.Json;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// Auto 청킹 전략 - 메타데이터 컨텍스트를 활용한 지능형 전략 자동 선택
/// Phase 4D: 웹 메타데이터를 기반으로 콘텐츠 특성에 최적화된 청킹 전략 선택
/// </summary>
public class AutoChunkingStrategy : BaseChunkingStrategy
{
    private readonly IChunkingStrategyFactory _strategyFactory;
    private readonly IMetadataDiscoveryService _metadataService;
    private readonly ILogger<AutoChunkingStrategy> _logger;
    private readonly Dictionary<string, IChunkingStrategy> _cachedStrategies;
    private readonly AutoChunkingConfiguration _config;

    public override string Name => "Auto";
    public override string Description => "지능형 자동 전략 선택 - 메타데이터 기반 최적화";

    public AutoChunkingStrategy(
        IChunkingStrategyFactory strategyFactory,
        IMetadataDiscoveryService metadataService,
        ILogger<AutoChunkingStrategy> logger)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cachedStrategies = new Dictionary<string, IChunkingStrategy>();
        _config = new AutoChunkingConfiguration();
    }

    public override async Task<ChunkResult> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 메타데이터 기반 전략 선택
            var selectedStrategy = await SelectOptimalStrategyAsync(content, options, cancellationToken);

            _logger.LogInformation("Auto 전략이 '{StrategyName}' 선택: {Url}",
                selectedStrategy.Name, content.Url);

            // 2. 선택된 전략으로 청킹 수행
            var result = await selectedStrategy.ChunkAsync(content, options, cancellationToken);

            // 3. Auto 전략 메타데이터 추가
            result.Metadata["AutoSelectedStrategy"] = selectedStrategy.Name;
            result.Metadata["AutoSelectionReason"] = await GetSelectionReasonAsync(content, selectedStrategy.Name);
            result.Metadata["AutoSelectionConfidence"] = await CalculateConfidenceScoreAsync(content, selectedStrategy.Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto 전략 실행 중 오류 발생: {Url}", content.Url);

            // 오류 시 폴백 전략 사용 (ParagraphChunkingStrategy)
            var fallbackStrategy = await GetCachedStrategyAsync("Paragraph");
            return await fallbackStrategy.ChunkAsync(content, options, cancellationToken);
        }
    }

    public override async IAsyncEnumerable<WebContentChunk> ChunkStreamAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var selectedStrategy = await SelectOptimalStrategyAsync(content, options, cancellationToken);

        _logger.LogInformation("Auto 스트림 전략이 '{StrategyName}' 선택: {Url}",
            selectedStrategy.Name, content.Url);

        await foreach (var chunk in selectedStrategy.ChunkStreamAsync(content, options, cancellationToken))
        {
            // 각 청크에 Auto 전략 메타데이터 추가
            chunk.Metadata["AutoSelectedStrategy"] = selectedStrategy.Name;
            yield return chunk;
        }
    }

    /// <summary>
    /// 메타데이터 기반 최적 전략 선택
    /// </summary>
    private async Task<IChunkingStrategy> SelectOptimalStrategyAsync(
        ExtractedContent content,
        ChunkingOptions? options,
        CancellationToken cancellationToken)
    {
        // 1. 메타데이터 분석
        var metadata = await AnalyzeContentMetadataAsync(content, cancellationToken);

        // 2. 전략 선택 로직
        var strategyName = await DetermineOptimalStrategyAsync(content, metadata, options);

        // 3. 전략 인스턴스 반환
        return await GetCachedStrategyAsync(strategyName);
    }

    /// <summary>
    /// 콘텐츠 메타데이터 분석
    /// </summary>
    private async Task<ContentAnalysisMetadata> AnalyzeContentMetadataAsync(
        ExtractedContent content,
        CancellationToken cancellationToken)
    {
        var analysis = new ContentAnalysisMetadata();

        try
        {
            // WebFlux 메타데이터 발견 서비스 활용
            var discoveryResult = await _metadataService.DiscoverMetadataAsync(content.Url, cancellationToken);

            // 콘텐츠 타입 분석
            analysis.ContentType = AnalyzeContentType(content, discoveryResult);
            analysis.StructuralComplexity = AnalyzeStructuralComplexity(content);
            analysis.HasImages = content.ImageUrls?.Any() ?? false;
            analysis.HasTechnicalContent = AnalyzeTechnicalContent(content);
            analysis.DocumentLength = content.MainContent?.Length ?? 0;
            analysis.IsMultiLanguage = AnalyzeLanguage(content);

            // 메타데이터 컨텍스트 활용
            if (discoveryResult.AiTxtMetadata != null)
            {
                analysis.IsAIFriendly = true;
                analysis.PreferredChunkingHint = discoveryResult.AiTxtMetadata.PreferredChunkingStrategy;
            }

            if (discoveryResult.ManifestMetadata != null)
            {
                analysis.IsPWA = true;
                analysis.IsInteractive = discoveryResult.ManifestMetadata.DisplayMode != "browser";
            }

            _logger.LogDebug("메타데이터 분석 완료: {Analysis}", JsonSerializer.Serialize(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "메타데이터 분석 중 오류 발생, 기본 분석 사용: {Url}", content.Url);
            analysis = CreateFallbackAnalysis(content);
        }

        return analysis;
    }

    /// <summary>
    /// 최적 전략 결정 알고리즘
    /// </summary>
    private async Task<string> DetermineOptimalStrategyAsync(
        ExtractedContent content,
        ContentAnalysisMetadata metadata,
        ChunkingOptions? options)
    {
        await Task.CompletedTask; // 비동기 호환성

        // 1. AI 친화적 사이트의 힌트 우선 적용
        if (metadata.IsAIFriendly && !string.IsNullOrEmpty(metadata.PreferredChunkingHint))
        {
            var hintStrategy = MapChunkingHint(metadata.PreferredChunkingHint);
            if (!string.IsNullOrEmpty(hintStrategy))
            {
                _logger.LogInformation("AI 힌트 기반 전략 선택: {Strategy}", hintStrategy);
                return hintStrategy;
            }
        }

        // 2. 콘텐츠 특성 기반 자동 선택
        var score = new Dictionary<string, double>();

        // 멀티미디어 콘텐츠
        if (metadata.HasImages && metadata.DocumentLength > _config.LargeDocumentThreshold)
        {
            score["Smart"] = 0.9; // 구조 인식으로 이미지 컨텍스트 보존
            score["Semantic"] = 0.6;
            score["Paragraph"] = 0.4;
        }
        // 기술 문서
        else if (metadata.HasTechnicalContent)
        {
            score["Smart"] = 0.8; // 코드 블록과 헤더 구조 보존
            score["Semantic"] = 0.7; // 기술적 의미 연관성
            score["Paragraph"] = 0.5;
        }
        // 대용량 문서
        else if (metadata.DocumentLength > _config.LargeDocumentThreshold)
        {
            score["MemoryOptimized"] = 0.9; // 메모리 효율성 우선
            score["Semantic"] = 0.6;
            score["Smart"] = 0.5;
        }
        // 구조화된 콘텐츠
        else if (metadata.StructuralComplexity > _config.HighComplexityThreshold)
        {
            score["Smart"] = 0.9; // 구조 인식 최적
            score["Semantic"] = 0.7;
            score["Paragraph"] = 0.3;
        }
        // 의미론적 처리가 필요한 콘텐츠
        else if (metadata.ContentType == ContentType.Article || metadata.ContentType == ContentType.Blog)
        {
            score["Semantic"] = 0.9; // 의미적 일관성 최적
            score["Smart"] = 0.7;
            score["Paragraph"] = 0.6;
        }
        // 기본 텍스트 문서
        else
        {
            score["Paragraph"] = 0.8; // 안정적인 기본 전략
            score["FixedSize"] = 0.6;
            score["Smart"] = 0.5;
        }

        // 3. 사용자 옵션 고려
        if (options != null)
        {
            ApplyUserPreferences(score, options);
        }

        // 4. 최고 점수 전략 선택
        var selectedStrategy = score.OrderByDescending(s => s.Value).First().Key;

        _logger.LogInformation("전략 점수: {Scores}, 선택된 전략: {Selected}",
            string.Join(", ", score.Select(s => $"{s.Key}={s.Value:F2}")), selectedStrategy);

        return selectedStrategy;
    }

    /// <summary>
    /// 콘텐츠 타입 분석
    /// </summary>
    private ContentType AnalyzeContentType(ExtractedContent content, MetadataDiscoveryResult discoveryResult)
    {
        var text = content.MainContent?.ToLowerInvariant() ?? "";
        var url = content.Url?.ToLowerInvariant() ?? "";

        // URL 기반 분석
        if (url.Contains("/blog/") || url.Contains("/post/")) return ContentType.Blog;
        if (url.Contains("/news/") || url.Contains("/article/")) return ContentType.Article;
        if (url.Contains("/doc/") || url.Contains("/guide/")) return ContentType.Documentation;
        if (url.Contains("/api/") || url.Contains("/reference/")) return ContentType.ApiReference;

        // 콘텐츠 기반 분석
        if (text.Contains("class ") || text.Contains("function ") || text.Contains("```")) return ContentType.Technical;
        if (content.Headings?.Count > 5) return ContentType.Documentation;

        return ContentType.General;
    }

    /// <summary>
    /// 구조적 복잡도 분석
    /// </summary>
    private double AnalyzeStructuralComplexity(ExtractedContent content)
    {
        double complexity = 0.0;

        // 헤딩 구조 복잡도
        if (content.Headings != null)
        {
            complexity += Math.Min(content.Headings.Count * 0.1, 0.5);
        }

        // 목록과 표 복잡도
        var text = content.MainContent ?? "";
        var listCount = CountOccurrences(text, new[] { "•", "-", "*", "1.", "2.", "3." });
        var tableCount = CountOccurrences(text, new[] { "|", "┌", "├", "└" });

        complexity += Math.Min(listCount * 0.02, 0.3);
        complexity += Math.Min(tableCount * 0.05, 0.2);

        return Math.Min(complexity, 1.0);
    }

    /// <summary>
    /// 기술적 콘텐츠 감지
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
    /// 언어 분석
    /// </summary>
    private bool AnalyzeLanguage(ExtractedContent content)
    {
        // 간단한 다국어 감지 (실제로는 더 정교한 분석 필요)
        var text = content.MainContent ?? "";
        var hasKorean = text.Any(c => c >= '가' && c <= '힣');
        var hasEnglish = text.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));

        return hasKorean && hasEnglish;
    }

    /// <summary>
    /// AI 청킹 힌트 매핑
    /// </summary>
    private string MapChunkingHint(string hint)
    {
        return hint.ToLowerInvariant() switch
        {
            "semantic" => "Semantic",
            "structure" or "structural" => "Smart",
            "paragraph" => "Paragraph",
            "fixed" or "fixedsize" => "FixedSize",
            "memory" or "optimized" => "MemoryOptimized",
            _ => ""
        };
    }

    /// <summary>
    /// 사용자 선호도 적용
    /// </summary>
    private void ApplyUserPreferences(Dictionary<string, double> scores, ChunkingOptions options)
    {
        // 성능 우선
        if (options.PreferPerformance)
        {
            scores["FixedSize"] = scores.GetValueOrDefault("FixedSize", 0) + 0.2;
            scores["Paragraph"] = scores.GetValueOrDefault("Paragraph", 0) + 0.1;
        }

        // 품질 우선
        if (options.PreferQuality)
        {
            scores["Semantic"] = scores.GetValueOrDefault("Semantic", 0) + 0.2;
            scores["Smart"] = scores.GetValueOrDefault("Smart", 0) + 0.1;
        }

        // 메모리 효율성 우선
        if (options.MinimizeMemoryUsage)
        {
            scores["MemoryOptimized"] = scores.GetValueOrDefault("MemoryOptimized", 0) + 0.3;
        }
    }

    /// <summary>
    /// 전략 캐싱 및 반환
    /// </summary>
    private async Task<IChunkingStrategy> GetCachedStrategyAsync(string strategyName)
    {
        if (!_cachedStrategies.TryGetValue(strategyName, out var strategy))
        {
            strategy = await _strategyFactory.CreateStrategyAsync(strategyName);
            _cachedStrategies[strategyName] = strategy;
        }
        return strategy;
    }

    /// <summary>
    /// 선택 이유 생성
    /// </summary>
    private async Task<string> GetSelectionReasonAsync(ExtractedContent content, string strategyName)
    {
        await Task.CompletedTask;

        return strategyName switch
        {
            "Smart" => "구조화된 콘텐츠로 헤더 기반 분할이 효과적",
            "Semantic" => "의미론적 일관성이 중요한 텍스트 콘텐츠",
            "MemoryOptimized" => "대용량 문서로 메모리 효율성 우선",
            "Paragraph" => "일반적인 문서로 안정적인 문단 분할 적용",
            "FixedSize" => "단순 구조로 고정 크기 분할이 적합",
            _ => "기본 전략 적용"
        };
    }

    /// <summary>
    /// 신뢰도 점수 계산
    /// </summary>
    private async Task<double> CalculateConfidenceScoreAsync(ExtractedContent content, string strategyName)
    {
        await Task.CompletedTask;

        // 간단한 신뢰도 계산 (실제로는 더 정교한 알고리즘 필요)
        var baseConfidence = 0.7;

        if (!string.IsNullOrEmpty(content.MainContent))
            baseConfidence += 0.1;

        if (content.Headings?.Any() == true)
            baseConfidence += 0.1;

        if (!string.IsNullOrEmpty(content.Title))
            baseConfidence += 0.1;

        return Math.Min(baseConfidence, 1.0);
    }

    /// <summary>
    /// 폴백 분석 생성
    /// </summary>
    private ContentAnalysisMetadata CreateFallbackAnalysis(ExtractedContent content)
    {
        return new ContentAnalysisMetadata
        {
            ContentType = ContentType.General,
            StructuralComplexity = 0.5,
            HasImages = content.ImageUrls?.Any() ?? false,
            HasTechnicalContent = false,
            DocumentLength = content.MainContent?.Length ?? 0,
            IsMultiLanguage = false,
            IsAIFriendly = false,
            IsPWA = false,
            IsInteractive = false
        };
    }

    /// <summary>
    /// 문자열 패턴 카운트
    /// </summary>
    private int CountOccurrences(string text, string[] patterns)
    {
        return patterns.Sum(pattern =>
            (text.Length - text.Replace(pattern, "").Length) / pattern.Length);
    }

    public override async Task<double> EvaluateSuitabilityAsync(
        ExtractedContent content,
        ChunkingOptions? options = null)
    {
        await Task.CompletedTask;
        return 1.0; // Auto 전략은 항상 최고 적합도
    }

    public override PerformanceInfo GetPerformanceInfo()
    {
        return new PerformanceInfo
        {
            AverageProcessingTime = TimeSpan.FromMilliseconds(200), // 전략 선택 오버헤드 포함
            MemoryUsage = "Variable",
            CPUIntensity = "Medium",
            Scalability = "High"
        };
    }

    public override List<ConfigurationOption> GetConfigurationOptions()
    {
        return new List<ConfigurationOption>
        {
            new() { Key = "LargeDocumentThreshold", DefaultValue = "50000", Description = "대용량 문서 임계값 (문자 수)" },
            new() { Key = "HighComplexityThreshold", DefaultValue = "0.7", Description = "고복잡도 임계값" },
            new() { Key = "EnableMetadataHints", DefaultValue = "true", Description = "메타데이터 힌트 사용 여부" },
            new() { Key = "CacheStrategies", DefaultValue = "true", Description = "전략 인스턴스 캐싱 여부" }
        };
    }

    public override async Task<double> EvaluateChunkQualityAsync(
        WebContentChunk chunk,
        ChunkEvaluationContext? context = null)
    {
        // 선택된 전략의 품질 평가 위임
        if (chunk.Metadata.TryGetValue("AutoSelectedStrategy", out var strategyName))
        {
            var strategy = await GetCachedStrategyAsync(strategyName.ToString() ?? "Paragraph");
            return await strategy.EvaluateChunkQualityAsync(chunk, context);
        }

        return 0.8; // 기본 품질 점수
    }

    public override ChunkingStatistics GetStatistics()
    {
        return new ChunkingStatistics
        {
            TotalChunksProcessed = 0, // 실제 처리는 선택된 전략이 수행
            AverageChunkSize = 0,
            AverageProcessingTime = TimeSpan.FromMilliseconds(200),
            SuccessRate = 0.95,
            QualityScore = 0.85
        };
    }
}

/// <summary>
/// 콘텐츠 분석 메타데이터
/// </summary>
public class ContentAnalysisMetadata
{
    public ContentType ContentType { get; set; } = ContentType.General;
    public double StructuralComplexity { get; set; }
    public bool HasImages { get; set; }
    public bool HasTechnicalContent { get; set; }
    public int DocumentLength { get; set; }
    public bool IsMultiLanguage { get; set; }
    public bool IsAIFriendly { get; set; }
    public bool IsPWA { get; set; }
    public bool IsInteractive { get; set; }
    public string? PreferredChunkingHint { get; set; }
}

/// <summary>
/// 콘텐츠 타입 열거형
/// </summary>
public enum ContentType
{
    General,
    Article,
    Blog,
    Documentation,
    Technical,
    ApiReference,
    News,
    Product,
    Legal
}

/// <summary>
/// Auto 청킹 구성
/// </summary>
public class AutoChunkingConfiguration
{
    public int LargeDocumentThreshold { get; set; } = 50000;
    public double HighComplexityThreshold { get; set; } = 0.7;
    public bool EnableMetadataHints { get; set; } = true;
    public bool CacheStrategies { get; set; } = true;
}