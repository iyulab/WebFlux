using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 정적/동적 처리 최적화 서비스 구현
/// 콘텐츠 분석, 성능 메트릭, 리소스 모니터링을 통한 지능형 최적화
/// </summary>
public class ProcessingOptimizationService : IProcessingOptimizationService
{
    private readonly ILogger<ProcessingOptimizationService> _logger;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ITokenCountService _tokenCountService;
    private readonly ICacheService _cacheService;
    private readonly WebFluxConfiguration _configuration;

    private readonly ConcurrentDictionary<string, StrategyPerformance> _strategyPerformance;
    private readonly ConcurrentDictionary<string, CacheOptimizationMetrics> _cacheMetrics;
    private readonly Timer _optimizationTimer;
    private readonly object _statsLock = new();

    public ProcessingOptimizationService(
        ILogger<ProcessingOptimizationService> logger,
        IPerformanceMonitor performanceMonitor,
        ITokenCountService tokenCountService,
        ICacheService cacheService,
        IOptions<WebFluxConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _tokenCountService = tokenCountService ?? throw new ArgumentNullException(nameof(tokenCountService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

        _strategyPerformance = new ConcurrentDictionary<string, StrategyPerformance>();
        _cacheMetrics = new ConcurrentDictionary<string, CacheOptimizationMetrics>();

        // 주기적 최적화 분석 (5분 간격)
        _optimizationTimer = new Timer(PerformPeriodicOptimization, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        _logger.LogInformation("ProcessingOptimizationService initialized");
    }

    public async Task<ChunkingStrategyRecommendation> RecommendChunkingStrategyAsync(
        string content,
        ContentMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentNullException.ThrowIfNull(metadata);

        using var activity = _performanceMonitor.StartActivity("RecommendChunkingStrategy");

        var startTime = DateTime.UtcNow;
        try
        {
            // 콘텐츠 분석
            var analysis = await AnalyzeContentAsync(content, metadata, cancellationToken);

            // 전략별 점수 계산
            var strategyScores = await CalculateStrategyScoresAsync(analysis, cancellationToken);

            // 성능 기록 기반 조정
            AdjustScoresBasedOnPerformance(strategyScores);

            // 최고 점수 전략 선택
            var bestStrategy = strategyScores
                .OrderByDescending(kvp => kvp.Value.TotalScore)
                .First();

            var recommendation = new ChunkingStrategyRecommendation
            {
                RecommendedStrategy = bestStrategy.Key,
                Confidence = CalculateConfidence(bestStrategy.Value, strategyScores.Values),
                StrategyScores = strategyScores,
                Reasoning = GenerateRecommendationReasoning(bestStrategy.Key, bestStrategy.Value, analysis),
                ExpectedImprovement = CalculateExpectedImprovement(bestStrategy.Key, analysis),
                ConfigurationParameters = GenerateConfigurationParameters(bestStrategy.Key, analysis)
            };

            var processingTime = DateTime.UtcNow - startTime;
            _logger.LogInformation("Strategy recommendation completed: {Strategy} with confidence {Confidence:P2} in {ProcessingTime}ms",
                recommendation.RecommendedStrategy, recommendation.Confidence, processingTime.TotalMilliseconds);

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recommend chunking strategy for content");
            throw;
        }
    }

    public async Task<ProcessingStrategy> OptimizeStrategyAsync(
        string currentStrategy,
        PerformanceStatistics performanceMetrics)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentStrategy);
        ArgumentNullException.ThrowIfNull(performanceMetrics);

        using var activity = _performanceMonitor.StartActivity("OptimizeStrategy");

        try
        {
            // 현재 전략 성능 분석
            var currentPerformance = AnalyzeCurrentPerformance(currentStrategy, performanceMetrics);

            // 최적화 필요 여부 판단
            var optimizationNeeded = DetermineOptimizationNeed(currentPerformance);

            if (!optimizationNeeded)
            {
                _logger.LogDebug("Current strategy {Strategy} is performing optimally", currentStrategy);
                return CreateCurrentStrategy(currentStrategy);
            }

            // 리소스 사용량 기반 최적화
            var optimizedStrategy = await OptimizeBasedOnResourceUsageAsync(currentStrategy, performanceMetrics);

            _logger.LogInformation("Strategy optimized: {CurrentStrategy} → {OptimizedStrategy}",
                currentStrategy, optimizedStrategy.Name);

            return optimizedStrategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize strategy {Strategy}", currentStrategy);
            throw;
        }
    }

    public async Task<CacheOptimizationResult> OptimizeCacheUsageAsync(
        string cacheKey,
        string contentHash,
        TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheKey);
        ArgumentException.ThrowIfNullOrEmpty(contentHash);

        using var activity = _performanceMonitor.StartActivity("OptimizeCacheUsage");

        try
        {
            // 캐시 통계 분석
            var cacheStats = await _cacheService.GetStatisticsAsync();

            // 최적화된 캐시 키 생성
            var optimizedKey = GenerateOptimizedCacheKey(cacheKey, contentHash);

            // 캐시 히트 확인
            var cachedResult = await _cacheService.GetAsync<object>(optimizedKey);
            var cacheHit = cachedResult != null;

            // TTL 최적화
            var recommendedTTL = ttl ?? CalculateOptimalTTL(contentHash, cacheStats);

            // 메트릭 업데이트
            UpdateCacheMetrics(optimizedKey, cacheHit, recommendedTTL);

            var result = new CacheOptimizationResult
            {
                CacheHit = cacheHit,
                OptimizedCacheKey = optimizedKey,
                RecommendedTTL = recommendedTTL,
                SpaceSavedBytes = CalculateSpaceSavings(optimizedKey, contentHash),
                PerformanceGainEstimate = CalculateCachePerformanceGain(cacheHit, cacheStats)
            };

            _logger.LogDebug("Cache optimization completed for {CacheKey}: Hit={CacheHit}, TTL={TTL}",
                cacheKey, cacheHit, recommendedTTL);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize cache usage for key {CacheKey}", cacheKey);
            throw;
        }
    }

    public async Task<ResourceOptimizationSuggestion> AnalyzeResourceUsageAsync()
    {
        using var activity = _performanceMonitor.StartActivity("AnalyzeResourceUsage");

        try
        {
            // 현재 리소스 사용량 수집
            var performanceStats = await _performanceMonitor.GetStatisticsAsync();
            var memoryUsage = GC.GetTotalMemory(false);

            // CPU 및 메모리 사용률 계산
            var cpuUtilization = CalculateCpuUtilization(performanceStats);
            var memoryUtilization = CalculateMemoryUtilization(memoryUsage);

            // 최적화 제안 생성
            var suggestions = GenerateOptimizationSuggestions(cpuUtilization, memoryUtilization, performanceStats);

            // 예상 절약 효과 계산
            var expectedSavings = CalculateExpectedSavings(suggestions);

            var result = new ResourceOptimizationSuggestion
            {
                CpuUtilization = cpuUtilization,
                MemoryUtilization = memoryUtilization,
                Suggestions = suggestions,
                ExpectedSavings = expectedSavings
            };

            _logger.LogInformation("Resource analysis completed: CPU={CpuUtilization:P2}, Memory={MemoryUtilization:P2}, Suggestions={SuggestionCount}",
                cpuUtilization, memoryUtilization, suggestions.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze resource usage");
            throw;
        }
    }

    public async Task<BottleneckOptimizationResult> OptimizeBottlenecksAsync(PipelineMetrics pipelineMetrics)
    {
        ArgumentNullException.ThrowIfNull(pipelineMetrics);

        using var activity = _performanceMonitor.StartActivity("OptimizeBottlenecks");

        try
        {
            // 병목 구간 식별
            var bottlenecks = IdentifyBottlenecks(pipelineMetrics);

            // 최적화 적용
            var optimizations = await ApplyBottleneckOptimizationsAsync(bottlenecks, pipelineMetrics);

            // 성능 향상 추정
            var expectedImprovement = CalculateBottleneckOptimizationImprovement(bottlenecks, optimizations);

            var result = new BottleneckOptimizationResult
            {
                IdentifiedBottlenecks = bottlenecks,
                AppliedOptimizations = optimizations,
                ExpectedPerformanceImprovement = expectedImprovement
            };

            _logger.LogInformation("Bottleneck optimization completed: {BottleneckCount} bottlenecks identified, {OptimizationCount} optimizations applied",
                bottlenecks.Count, optimizations.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize bottlenecks");
            throw;
        }
    }

    public async Task<TokenOptimizationResult> OptimizeTokenUsageAsync(string text, int targetTokenLimit)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetTokenLimit);

        using var activity = _performanceMonitor.StartActivity("OptimizeTokenUsage");

        try
        {
            // 원본 토큰 수 계산
            var originalTokenCount = await _tokenCountService.CountTokensAsync(text);

            if (originalTokenCount <= targetTokenLimit)
            {
                // 이미 제한 내에 있음
                return new TokenOptimizationResult
                {
                    OriginalTokenCount = originalTokenCount,
                    OptimizedTokenCount = originalTokenCount,
                    TokensSaved = 0,
                    SavingsPercent = 0,
                    OptimizedText = text,
                    QualityRetentionScore = 1.0
                };
            }

            // 토큰 최적화 수행
            var optimizedText = await PerformTokenOptimizationAsync(text, targetTokenLimit);
            var optimizedTokenCount = await _tokenCountService.CountTokensAsync(optimizedText);

            var tokensSaved = originalTokenCount - optimizedTokenCount;
            var savingsPercent = (double)tokensSaved / originalTokenCount;
            var qualityScore = CalculateQualityRetentionScore(text, optimizedText);

            var result = new TokenOptimizationResult
            {
                OriginalTokenCount = originalTokenCount,
                OptimizedTokenCount = optimizedTokenCount,
                TokensSaved = tokensSaved,
                SavingsPercent = savingsPercent,
                OptimizedText = optimizedText,
                QualityRetentionScore = qualityScore
            };

            _logger.LogInformation("Token optimization completed: {OriginalTokens} → {OptimizedTokens} tokens ({SavingsPercent:P2} savings)",
                originalTokenCount, optimizedTokenCount, savingsPercent);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize token usage");
            throw;
        }
    }

    public async Task<OptimizationStatistics> GetOptimizationStatisticsAsync()
    {
        using var activity = _performanceMonitor.StartActivity("GetOptimizationStatistics");

        try
        {
            lock (_statsLock)
            {
                var totalRequests = _strategyPerformance.Values.Sum(sp => sp.RequestCount);
                var successfulOptimizations = _strategyPerformance.Values.Sum(sp => sp.SuccessCount);
                var averageImprovement = _strategyPerformance.Values
                    .Where(sp => sp.SuccessCount > 0)
                    .Average(sp => sp.AverageImprovement);

                var totalSavings = CalculateTotalResourceSavings();

                return new OptimizationStatistics
                {
                    TotalOptimizationRequests = totalRequests,
                    SuccessfulOptimizations = successfulOptimizations,
                    AveragePerformanceImprovement = averageImprovement,
                    TotalResourceSavings = totalSavings,
                    CollectionPeriod = TimeSpan.FromHours(24), // 기본 24시간
                    LastUpdated = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization statistics");
            throw;
        }
    }

    #region Private Methods

    private async Task<ContentAnalysis> AnalyzeContentAsync(string content, ContentMetadata metadata, CancellationToken cancellationToken)
    {
        var tokenCount = await _tokenCountService.CountTokensAsync(content, cancellationToken);
        var lines = content.Split('\n');
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new ContentAnalysis
        {
            TokenCount = tokenCount,
            LineCount = lines.Length,
            WordCount = words.Length,
            AverageLineLength = lines.Length > 0 ? words.Length / lines.Length : 0,
            HasImages = metadata.HasImages,
            ContentType = metadata.ContentType ?? "text/plain",
            Language = DetectLanguage(content),
            ComplexityScore = CalculateComplexityScore(content),
            StructureScore = CalculateStructureScore(content)
        };
    }

    private async Task<Dictionary<string, StrategyScore>> CalculateStrategyScoresAsync(ContentAnalysis analysis, CancellationToken cancellationToken)
    {
        var strategies = new[] { "Auto", "Smart", "Semantic", "Paragraph", "FixedSize", "MemoryOptimized" };
        var scores = new Dictionary<string, StrategyScore>();

        foreach (var strategy in strategies)
        {
            var score = new StrategyScore();

            // 콘텐츠 기반 점수 계산
            CalculateContentBasedScore(score, strategy, analysis);

            // 성능 기반 점수 계산
            CalculatePerformanceBasedScore(score, strategy);

            scores[strategy] = score;
        }

        return scores;
    }

    private void CalculateContentBasedScore(StrategyScore score, string strategy, ContentAnalysis analysis)
    {
        switch (strategy)
        {
            case "Auto":
                score.AddScore("Content", 0.8, "Good general-purpose strategy");
                if (analysis.HasImages) score.AddScore("Multimodal", 0.9, "Excellent for multimodal content");
                break;

            case "Smart":
                score.AddScore("Content", analysis.ComplexityScore, "Adapts to content complexity");
                if (analysis.StructureScore > 0.7) score.AddScore("Structure", 0.9, "Handles structured content well");
                break;

            case "Semantic":
                if (analysis.TokenCount > 1000) score.AddScore("Size", 0.9, "Excellent for large content");
                if (analysis.ComplexityScore > 0.6) score.AddScore("Complexity", 0.8, "Handles complex content");
                break;

            case "Paragraph":
                if (analysis.StructureScore > 0.8) score.AddScore("Structure", 0.9, "Perfect for well-structured text");
                score.AddScore("Simplicity", 0.7, "Simple and reliable");
                break;

            case "FixedSize":
                score.AddScore("Predictability", 0.8, "Predictable chunk sizes");
                if (analysis.TokenCount < 500) score.AddScore("SmallContent", 0.6, "Reasonable for small content");
                break;

            case "MemoryOptimized":
                if (analysis.TokenCount > 5000) score.AddScore("LargeContent", 0.9, "Excellent for large content");
                score.AddScore("MemoryEfficiency", 0.8, "Memory efficient");
                break;
        }
    }

    private void CalculatePerformanceBasedScore(StrategyScore score, string strategy)
    {
        if (_strategyPerformance.TryGetValue(strategy, out var performance))
        {
            var performanceScore = Math.Min(performance.AverageImprovement / 100.0, 1.0);
            score.AddScore("Performance", performanceScore, $"Historical performance: {performance.AverageImprovement:P2}");
        }
    }

    private void AdjustScoresBasedOnPerformance(Dictionary<string, StrategyScore> strategyScores)
    {
        foreach (var kvp in strategyScores)
        {
            if (_strategyPerformance.TryGetValue(kvp.Key, out var performance))
            {
                var adjustmentFactor = 1.0 + (performance.AverageImprovement / 100.0);
                // 성능 기록에 따른 점수 조정 로직은 StrategyScore 내부에서 처리됨
            }
        }
    }

    private double CalculateConfidence(StrategyScore bestScore, IEnumerable<StrategyScore> allScores)
    {
        var scores = allScores.Select(s => s.TotalScore).OrderByDescending(s => s).ToList();
        if (scores.Count < 2) return 1.0;

        var best = scores[0];
        var secondBest = scores[1];
        var gap = best - secondBest;

        // 점수 차이가 클수록 신뢰도 높음
        return Math.Min(gap / best + 0.5, 1.0);
    }

    private string GenerateRecommendationReasoning(string strategy, StrategyScore score, ContentAnalysis analysis)
    {
        var reasons = score.Components.Select(c => c.Reason).ToList();
        var contentInfo = $"Content has {analysis.TokenCount} tokens, complexity {analysis.ComplexityScore:P1}";

        return $"{strategy} recommended because: {string.Join(", ", reasons)}. {contentInfo}";
    }

    private double CalculateExpectedImprovement(string strategy, ContentAnalysis analysis)
    {
        if (_strategyPerformance.TryGetValue(strategy, out var performance))
        {
            return performance.AverageImprovement;
        }

        // 기본 예상 향상도
        return strategy switch
        {
            "Auto" => 0.15,
            "Smart" => 0.12,
            "Semantic" => 0.20,
            "MemoryOptimized" => 0.25,
            _ => 0.10
        };
    }

    private Dictionary<string, object> GenerateConfigurationParameters(string strategy, ContentAnalysis analysis)
    {
        var parameters = new Dictionary<string, object>();

        switch (strategy)
        {
            case "FixedSize":
                parameters["ChunkSize"] = Math.Min(analysis.TokenCount / 10, 500);
                parameters["Overlap"] = 50;
                break;

            case "Semantic":
                parameters["SimilarityThreshold"] = 0.8;
                parameters["MinChunkSize"] = 100;
                parameters["MaxChunkSize"] = 1000;
                break;

            case "MemoryOptimized":
                parameters["BatchSize"] = 50;
                parameters["StreamingEnabled"] = true;
                break;
        }

        return parameters;
    }

    private async Task<string> PerformTokenOptimizationAsync(string text, int targetTokenLimit)
    {
        // 단순한 토큰 최적화 구현
        // 실제로는 더 정교한 알고리즘 필요
        return _tokenCountService.TruncateToTokenLimit(text, targetTokenLimit);
    }

    private double CalculateQualityRetentionScore(string original, string optimized)
    {
        // 단순한 품질 점수 계산
        // 실제로는 의미 유사도 등을 고려해야 함
        var lengthRatio = (double)optimized.Length / original.Length;
        return Math.Max(lengthRatio, 0.5); // 최소 50% 품질 보장
    }

    private void PerformPeriodicOptimization(object? state)
    {
        try
        {
            _logger.LogDebug("Performing periodic optimization analysis");

            // 주기적 최적화 작업
            CleanupOldMetrics();
            OptimizeStrategyPerformanceTracking();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic optimization");
        }
    }

    private void CleanupOldMetrics()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        // 오래된 메트릭 정리 로직
    }

    private void OptimizeStrategyPerformanceTracking()
    {
        // 전략 성능 추적 최적화 로직
    }

    private string DetectLanguage(string content)
    {
        // 간단한 언어 감지 (실제로는 더 정교한 라이브러리 사용)
        return "en"; // 기본값
    }

    private double CalculateComplexityScore(string content)
    {
        // 문장 길이, 어휘 다양성 등을 고려한 복잡도 계산
        var sentences = content.Split('.', '!', '?');
        var avgSentenceLength = sentences.Average(s => s.Split(' ').Length);
        return Math.Min(avgSentenceLength / 20.0, 1.0);
    }

    private double CalculateStructureScore(string content)
    {
        // 구조화 정도 계산 (헤더, 목록, 단락 등)
        var headers = content.Split('\n').Count(line => line.StartsWith('#'));
        var lists = content.Split('\n').Count(line => line.Trim().StartsWith('-') || line.Trim().StartsWith('*'));
        var paragraphs = content.Split("\n\n").Length;

        var structureElements = headers + lists + paragraphs;
        var lines = content.Split('\n').Length;

        return Math.Min((double)structureElements / lines, 1.0);
    }

    // 추가 private 메서드들은 간소화를 위해 기본 구현만 제공
    private StrategyPerformance AnalyzeCurrentPerformance(string strategy, PerformanceStatistics metrics) => new();
    private bool DetermineOptimizationNeed(StrategyPerformance performance) => false;
    private ProcessingStrategy CreateCurrentStrategy(string strategy) => new() { Name = strategy };
    private async Task<ProcessingStrategy> OptimizeBasedOnResourceUsageAsync(string strategy, PerformanceStatistics metrics) => new() { Name = strategy };
    private string GenerateOptimizedCacheKey(string key, string hash) => $"{key}:{hash}";
    private TimeSpan CalculateOptimalTTL(string hash, CacheStatistics stats) => TimeSpan.FromHours(1);
    private void UpdateCacheMetrics(string key, bool hit, TimeSpan ttl) { }
    private long CalculateSpaceSavings(string key, string hash) => 0;
    private double CalculateCachePerformanceGain(bool hit, CacheStatistics stats) => hit ? 0.5 : 0.0;
    private double CalculateCpuUtilization(PerformanceStatistics stats) => 0.5;
    private double CalculateMemoryUtilization(long memory) => Math.Min(memory / (1024 * 1024 * 1024.0), 1.0);
    private List<OptimizationSuggestion> GenerateOptimizationSuggestions(double cpu, double memory, PerformanceStatistics stats) => new();
    private ResourceSavings CalculateExpectedSavings(List<OptimizationSuggestion> suggestions) => new();
    private List<BottleneckInfo> IdentifyBottlenecks(PipelineMetrics metrics) => new();
    private async Task<List<AppliedOptimization>> ApplyBottleneckOptimizationsAsync(List<BottleneckInfo> bottlenecks, PipelineMetrics metrics) => new();
    private double CalculateBottleneckOptimizationImprovement(List<BottleneckInfo> bottlenecks, List<AppliedOptimization> optimizations) => 0.0;
    private ResourceSavings CalculateTotalResourceSavings() => new();

    #endregion

    public void Dispose()
    {
        _optimizationTimer?.Dispose();
        _performanceMonitor?.Dispose();
    }
}

/// <summary>
/// 전략 성능 추적 정보
/// </summary>
internal class StrategyPerformance
{
    public long RequestCount { get; set; }
    public long SuccessCount { get; set; }
    public double AverageImprovement { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 캐시 최적화 메트릭
/// </summary>
internal class CacheOptimizationMetrics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public TimeSpan AverageTTL { get; set; }
    public long TotalSpaceSaved { get; set; }
}

/// <summary>
/// 콘텐츠 분석 결과
/// </summary>
internal class ContentAnalysis
{
    public int TokenCount { get; set; }
    public int LineCount { get; set; }
    public int WordCount { get; set; }
    public double AverageLineLength { get; set; }
    public bool HasImages { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double ComplexityScore { get; set; }
    public double StructureScore { get; set; }
}