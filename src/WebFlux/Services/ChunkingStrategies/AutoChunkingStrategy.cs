using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 자동 청킹 전략 - Phase 5B 고도화
/// 품질 평가 시스템과 실시간 최적화를 통한 지능형 전략 선택
/// </summary>
public partial class AutoChunkingStrategy : BaseChunkingStrategy
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly IPerformanceMonitor? _performanceMonitor;
    private readonly ICacheService? _cacheService;
    private readonly ITokenCountService? _tokenCountService;
    private readonly ILogger<AutoChunkingStrategy>? _logger;
    private readonly AutoChunkingConfiguration _config;

    public override string Name => "Auto";
    public override string Description => "AI 기반 지능형 자동 전략 선택 - 품질 평가 및 실시간 최적화";

    public AutoChunkingStrategy(
        IEventPublisher? eventPublisher = null,
        IServiceProvider? serviceProvider = null,
        IPerformanceMonitor? performanceMonitor = null,
        ICacheService? cacheService = null,
        ITokenCountService? tokenCountService = null,
        ILogger<AutoChunkingStrategy>? logger = null)
        : base(eventPublisher)
    {
        _serviceProvider = serviceProvider;
        _performanceMonitor = performanceMonitor;
        _cacheService = cacheService;
        _tokenCountService = tokenCountService;
        _logger = logger;
        _config = new AutoChunkingConfiguration();
    }

    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var text = content.MainContent ?? content.Text ?? string.Empty;
        var sourceUrl = content.Url ?? content.OriginalUrl ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        using var operationScope = _performanceMonitor?.MeasureOperation("auto_chunking_analysis") as IOperationScope;
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Phase 5B.1: 캐시에서 전략 결과 확인
            var cacheKey = GenerateCacheKey(content, options);
            var cachedResult = await GetCachedResultAsync(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                if (_logger != null) LogUsingCachedResult(_logger, sourceUrl);
                return cachedResult;
            }

            // Phase 5B.2: 향상된 콘텐츠 분석 및 전략 선택
            var analysisMetadata = await AnalyzeContentAsync(content, cancellationToken);
            var strategyScores = await CalculateStrategyScoresAsync(analysisMetadata, options, cancellationToken);

            // Phase 5B.3: 최적 전략 선택 (성능 히스토리 고려)
            var selectedStrategy = SelectOptimalStrategy(strategyScores, analysisMetadata);

            if (_logger != null) LogSelectedStrategy(_logger, selectedStrategy.Name, strategyScores[selectedStrategy.Name].TotalScore, sourceUrl);

            // Phase 5B.4: 실시간 성능 모니터링과 함께 청킹 수행
            var chunks = await ExecuteChunkingWithMonitoring(selectedStrategy, content, options, cancellationToken);

            // Phase 5B.5: 품질 평가 및 결과 캐싱
            var qualityScore = await EvaluateChunkQuality(chunks, analysisMetadata, cancellationToken);
            await CacheResultAsync(cacheKey, chunks, qualityScore, cancellationToken);

            // Phase 5B.6: 성능 메트릭 기록
            var processingTime = DateTimeOffset.UtcNow - startTime;
            _performanceMonitor?.RecordChunkingMetrics(selectedStrategy.Name, qualityScore, chunks.Count, processingTime);

            operationScope?.Complete();

            return chunks;
        }
        catch (Exception ex)
        {
            if (_logger != null) LogAutoChunkingError(_logger, ex, sourceUrl);
            operationScope?.RecordError(ex);

            // Fallback to simple strategy
            var fallbackStrategy = new ParagraphChunkingStrategy(EventPublisher);
            return await fallbackStrategy.ChunkAsync(content, options, cancellationToken);
        }
    }

    /// <summary>
    /// Phase 5B.1: 캐시 키 생성
    /// </summary>
    private static string GenerateCacheKey(ExtractedContent content, ChunkingOptions? options)
    {
        var contentHash = content.Url?.GetHashCode() ?? content.Text?.GetHashCode() ?? 0;
        var optionsHash = options?.GetHashCode() ?? 0;
        return $"auto_chunk:{contentHash}:{optionsHash}";
    }

    /// <summary>
    /// Phase 5B.1: 캐시된 결과 조회
    /// </summary>
    private async Task<IReadOnlyList<WebContentChunk>?> GetCachedResultAsync(string cacheKey, CancellationToken cancellationToken)
    {
        if (_cacheService == null) return null;

        try
        {
            return await _cacheService.GetAsync<List<WebContentChunk>>(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            if (_logger != null) LogCacheGetFailed(_logger, ex, cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Phase 5B.2: 향상된 콘텐츠 분석
    /// </summary>
    private async Task<ContentAnalysisMetadata> AnalyzeContentAsync(ExtractedContent content, CancellationToken cancellationToken)
    {
        var text = content.MainContent ?? content.Text ?? string.Empty;
        var textLength = text.Length;

        var analysis = new ContentAnalysisMetadata
        {
            DocumentLength = textLength,
            HasImages = content.ImageUrls?.Count > 0,
            ImageDensity = CalculateImageDensity(content),
            HasTechnicalContent = await DetectTechnicalContentAsync(text, cancellationToken),
            ContentType = ClassifyContentType(content),
            StructuralComplexity = AnalyzeStructuralComplexity(content),
            QualityMetrics = await CalculateQualityMetricsAsync(content, cancellationToken)
        };

        return analysis;
    }

    /// <summary>
    /// Phase 5B.2: 전략별 점수 계산
    /// </summary>
    private async Task<Dictionary<string, StrategyScore>> CalculateStrategyScoresAsync(
        ContentAnalysisMetadata analysis, ChunkingOptions? options, CancellationToken cancellationToken)
    {
        var strategies = new[] { "FixedSize", "Paragraph", "Smart", "Semantic", "MemoryOptimized" };
        var scores = new Dictionary<string, StrategyScore>();

        foreach (var strategy in strategies)
        {
            var score = new StrategyScore();

            // 문서 크기 점수
            AddDocumentSizeScore(score, strategy, analysis.DocumentLength);

            // 구조적 복잡도 점수
            AddStructuralComplexityScore(score, strategy, analysis.StructuralComplexity);

            // 콘텐츠 타입 점수
            AddContentTypeScore(score, strategy, analysis.ContentType);

            // 멀티모달 점수
            AddMultimodalScore(score, strategy, analysis.HasImages, analysis.ImageDensity);

            // 기술 콘텐츠 점수
            AddTechnicalContentScore(score, strategy, analysis.HasTechnicalContent);

            // 성능 히스토리 점수 추가
            await AddPerformanceHistoryScoreAsync(score, strategy, cancellationToken);

            scores[strategy] = score;
        }

        return scores;
    }

    /// <summary>
    /// Phase 5B.3: 최적 전략 선택
    /// </summary>
    private IChunkingStrategy SelectOptimalStrategy(Dictionary<string, StrategyScore> scores, ContentAnalysisMetadata analysis)
    {
        var bestStrategy = scores.OrderByDescending(kvp => kvp.Value.TotalScore).First();

        if (_logger != null)
        {
            var scoresText = string.Join(", ", scores.Select(kvp => $"{kvp.Key}:{kvp.Value.TotalScore:F2}"));
            LogStrategyScores(_logger, scoresText);
        }

        return bestStrategy.Key switch
        {
            "Smart" => new SmartChunkingStrategy(EventPublisher),
            "Semantic" => new SemanticChunkingStrategy(EventPublisher),
            "MemoryOptimized" => new MemoryOptimizedChunkingStrategy(EventPublisher),
            "Paragraph" => new ParagraphChunkingStrategy(EventPublisher),
            _ => new FixedSizeChunkingStrategy(EventPublisher)
        };
    }

    /// <summary>
    /// Phase 5B.4: 모니터링과 함께 청킹 수행
    /// </summary>
    private async Task<IReadOnlyList<WebContentChunk>> ExecuteChunkingWithMonitoring(
        IChunkingStrategy strategy, ExtractedContent content, ChunkingOptions? options, CancellationToken cancellationToken)
    {
        using var scope = _performanceMonitor?.MeasureOperation($"chunking_{strategy.Name.ToLowerInvariant()}") as IOperationScope;

        try
        {
            var chunks = await strategy.ChunkAsync(content, options, cancellationToken);
            scope?.Complete();
            return chunks;
        }
        catch (Exception ex)
        {
            scope?.RecordError(ex);
            throw;
        }
    }

    /// <summary>
    /// Phase 5B.5: 청킹 품질 평가
    /// </summary>
    private async Task<double> EvaluateChunkQuality(IReadOnlyList<WebContentChunk> chunks,
        ContentAnalysisMetadata analysis, CancellationToken cancellationToken)
    {
        if (!chunks.Any()) return 0.0;

        var qualityScore = 0.0;
        var totalWeight = 0.0;

        // 청크 크기 일관성 (가중치: 0.3)
        var sizeConsistencyScore = CalculateSizeConsistency(chunks);
        qualityScore += sizeConsistencyScore * 0.3;
        totalWeight += 0.3;

        // 의미적 응집성 (가중치: 0.4)
        var semanticCohesionScore = await CalculateSemanticCohesion(chunks, cancellationToken);
        qualityScore += semanticCohesionScore * 0.4;
        totalWeight += 0.4;

        // 구조적 일관성 (가중치: 0.2)
        var structuralConsistencyScore = CalculateStructuralConsistency(chunks, analysis);
        qualityScore += structuralConsistencyScore * 0.2;
        totalWeight += 0.2;

        // 토큰 효율성 (가중치: 0.1)
        var tokenEfficiencyScore = await CalculateTokenEfficiency(chunks, cancellationToken);
        qualityScore += tokenEfficiencyScore * 0.1;
        totalWeight += 0.1;

        return totalWeight > 0 ? qualityScore / totalWeight : 0.0;
    }

    /// <summary>
    /// Phase 5B.5: 결과 캐싱
    /// </summary>
    private async Task CacheResultAsync(string cacheKey, IReadOnlyList<WebContentChunk> chunks,
        double qualityScore, CancellationToken cancellationToken)
    {
        if (_cacheService == null || qualityScore < 0.5) return;

        try
        {
            var expiration = qualityScore > 0.8 ? TimeSpan.FromHours(4) : TimeSpan.FromHours(1);
            await _cacheService.SetAsync(cacheKey, chunks.ToList(), expiration, cancellationToken);

            if (_logger != null) LogCachedChunkingResult(_logger, qualityScore);
        }
        catch (Exception ex)
        {
            if (_logger != null) LogCacheSetFailed(_logger, ex);
        }
    }

    #region Helper Methods

    private static double CalculateImageDensity(ExtractedContent content)
    {
        var imageCount = content.ImageUrls?.Count ?? 0;
        var textLength = content.MainContent?.Length ?? content.Text?.Length ?? 1;
        return (double)imageCount / Math.Max(textLength / 1000, 1); // 이미지 수 / (텍스트 길이/1000)
    }

    private Task<bool> DetectTechnicalContentAsync(string text, CancellationToken cancellationToken)
    {
        var lowerText = text.ToLowerInvariant();
        var technicalMatches = _config.TechnicalKeywords.Count(keyword => lowerText.Contains(keyword));
        return Task.FromResult(technicalMatches >= 3);
    }

    private ContentType ClassifyContentType(ExtractedContent content)
    {
        var text = (content.MainContent ?? content.Text ?? "").ToLowerInvariant();
        var url = content.Url?.ToLowerInvariant() ?? "";

        if (_config.AcademicKeywords.Any(k => text.Contains(k))) return ContentType.Academic;
        if (_config.NewsKeywords.Any(k => text.Contains(k))) return ContentType.News;
        if (_config.TechnicalKeywords.Any(k => text.Contains(k))) return ContentType.Technical;
        if (url.Contains("doc") || url.Contains("api")) return ContentType.Documentation;
        if (url.Contains("blog")) return ContentType.Blog;

        return ContentType.Article;
    }

    private static StructuralComplexity AnalyzeStructuralComplexity(ExtractedContent content)
    {
        var headingCount = content.Headings?.Count ?? 0;
        var imageCount = content.ImageUrls?.Count ?? 0;
        var textLength = content.MainContent?.Length ?? content.Text?.Length ?? 0;

        var complexityScore = (headingCount * 2 + imageCount) / Math.Max(textLength / 1000.0, 1);

        return complexityScore switch
        {
            >= 2.0 => StructuralComplexity.High,
            >= 0.5 => StructuralComplexity.Medium,
            _ => StructuralComplexity.Low
        };
    }

    private static async Task<ContentQualityMetrics> CalculateQualityMetricsAsync(ExtractedContent content, CancellationToken cancellationToken)
    {
        var text = content.MainContent ?? content.Text ?? "";

        return new ContentQualityMetrics
        {
            TextDensity = CalculateTextDensity(text),
            StructuralConsistency = CalculateBaseStructuralConsistency(content),
            SemanticCohesion = await EstimateSemanticCohesion(text, cancellationToken),
            ReadabilityScore = CalculateReadabilityScore(text)
        };
    }

    private void AddDocumentSizeScore(StrategyScore score, string strategy, int documentLength)
    {
        var sizeScore = strategy switch
        {
            "MemoryOptimized" when documentLength > _config.VeryLongDocumentThreshold => 0.9,
            "Smart" when documentLength > _config.LongDocumentThreshold => 0.8,
            "Paragraph" when documentLength > _config.ShortDocumentThreshold => 0.7,
            "FixedSize" when documentLength <= _config.ShortDocumentThreshold => 0.6,
            _ => 0.3
        };

        score.AddScore("document_size", sizeScore * _config.Weights.DocumentSize,
            $"Document length: {documentLength} bytes");
    }

    private void AddStructuralComplexityScore(StrategyScore score, string strategy, StructuralComplexity complexity)
    {
        var complexityScore = (strategy, complexity) switch
        {
            ("Smart", StructuralComplexity.High) => 0.9,
            ("Smart", StructuralComplexity.Medium) => 0.8,
            ("Semantic", StructuralComplexity.High) => 0.7,
            ("Paragraph", StructuralComplexity.Medium) => 0.6,
            ("FixedSize", StructuralComplexity.Low) => 0.5,
            _ => 0.3
        };

        score.AddScore("structural_complexity", complexityScore * _config.Weights.StructuralComplexity,
            $"Complexity level: {complexity}");
    }

    private void AddContentTypeScore(StrategyScore score, string strategy, ContentType contentType)
    {
        var typeScore = (strategy, contentType) switch
        {
            ("Semantic", ContentType.Academic) => 0.9,
            ("Semantic", ContentType.Technical) => 0.8,
            ("Smart", ContentType.Documentation) => 0.9,
            ("Smart", ContentType.Technical) => 0.8,
            ("Paragraph", ContentType.Blog) => 0.7,
            ("Paragraph", ContentType.Article) => 0.6,
            _ => 0.4
        };

        score.AddScore("content_type", typeScore * _config.Weights.ContentType,
            $"Content type: {contentType}");
    }

    private void AddMultimodalScore(StrategyScore score, string strategy, bool hasImages, double imageDensity)
    {
        if (!hasImages)
        {
            score.AddScore("multimodal", 0.5 * _config.Weights.MultimodalContent, "No images");
            return;
        }

        var multimodalScore = strategy switch
        {
            "Smart" when imageDensity > _config.HighImageDensityThreshold => 0.9,
            "Smart" => 0.7,
            "Paragraph" when imageDensity <= _config.HighImageDensityThreshold => 0.6,
            _ => 0.4
        };

        score.AddScore("multimodal", multimodalScore * _config.Weights.MultimodalContent,
            $"Images present, density: {imageDensity:F3}");
    }

    private void AddTechnicalContentScore(StrategyScore score, string strategy, bool hasTechnicalContent)
    {
        var techScore = (strategy, hasTechnicalContent) switch
        {
            ("Semantic", true) => 0.9,
            ("Smart", true) => 0.8,
            ("Paragraph", false) => 0.6,
            ("FixedSize", false) => 0.5,
            _ => 0.4
        };

        score.AddScore("technical_content", techScore * _config.Weights.TechnicalContent,
            $"Technical content: {hasTechnicalContent}");
    }

    private async Task AddPerformanceHistoryScoreAsync(StrategyScore score, string strategy, CancellationToken cancellationToken)
    {
        if (_performanceMonitor == null)
        {
            score.AddScore("performance_history", 0.5, "No performance monitor available");
            return;
        }

        try
        {
            var stats = await _performanceMonitor.GetStatisticsAsync();

            // 성능 통계가 있다면 해당 전략의 과거 성과를 고려
            var performanceScore = stats.AverageChunkQuality * 0.7 + (1.0 - stats.ErrorRate) * 0.3;
            score.AddScore("performance_history", performanceScore * 0.1,
                $"Historical performance: {performanceScore:F3}");
        }
        catch (Exception ex)
        {
            if (_logger != null) LogPerformanceHistoryFailed(_logger, ex, strategy);
            score.AddScore("performance_history", 0.5, "Performance history unavailable");
        }
    }

    private static double CalculateSizeConsistency(IReadOnlyList<WebContentChunk> chunks)
    {
        if (chunks.Count < 2) return 1.0;

        var lengths = chunks.Select(c => c.Content.Length).ToArray();
        var average = lengths.Average();
        var variance = lengths.Select(l => Math.Pow(l - average, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);
        var coefficientOfVariation = standardDeviation / average;

        return Math.Max(0.0, 1.0 - coefficientOfVariation);
    }

    private static Task<double> CalculateSemanticCohesion(IReadOnlyList<WebContentChunk> chunks, CancellationToken cancellationToken)
    {
        // 의미적 응집성 평가 (간단한 구현)
        // 실제로는 임베딩 서비스를 사용해야 하지만 여기서는 키워드 기반 평가
        if (chunks.Count < 2) return Task.FromResult(1.0);

        var cohesionScores = new List<double>();
        for (int i = 0; i < chunks.Count - 1; i++)
        {
            var similarity = CalculateTextSimilarity(chunks[i].Content, chunks[i + 1].Content);
            cohesionScores.Add(similarity);
        }

        return Task.FromResult(cohesionScores.Average());
    }

    private static double CalculateStructuralConsistency(IReadOnlyList<WebContentChunk> chunks, ContentAnalysisMetadata analysis)
    {
        // 구조적 일관성: 청크들이 논리적 구조를 유지하는지 평가
        var consistencyScore = 1.0;

        // 청크 크기의 적절성
        var avgChunkSize = chunks.Average(c => c.Content.Length);
        var targetSize = analysis.DocumentLength / Math.Max(chunks.Count, 1);
        var sizeRatio = Math.Min(avgChunkSize, targetSize) / Math.Max(avgChunkSize, targetSize);
        consistencyScore *= sizeRatio;

        return Math.Max(0.0, consistencyScore);
    }

    private async Task<double> CalculateTokenEfficiency(IReadOnlyList<WebContentChunk> chunks, CancellationToken cancellationToken)
    {
        if (_tokenCountService == null) return 0.8; // 기본점수

        try
        {
            var tokenCounts = await _tokenCountService.CountTokensBatchAsync(
                chunks.Select(c => c.Content), cancellationToken);

            var avgTokensPerChunk = tokenCounts.Average();
            var idealTokenRange = (500, 1500); // 이상적인 토큰 범위

            if (avgTokensPerChunk >= idealTokenRange.Item1 && avgTokensPerChunk <= idealTokenRange.Item2)
                return 1.0;

            if (avgTokensPerChunk < idealTokenRange.Item1)
                return avgTokensPerChunk / idealTokenRange.Item1;

            return idealTokenRange.Item2 / avgTokensPerChunk;
        }
        catch (Exception ex)
        {
            if (_logger != null) LogTokenEfficiencyFailed(_logger, ex);
            return 0.8;
        }
    }

    private static double CalculateTextDensity(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0.0;

        var totalChars = text.Length;
        var meaningfulChars = text.Count(c => !char.IsWhiteSpace(c) && c != '\n' && c != '\r');
        return (double)meaningfulChars / totalChars;
    }

    private static double CalculateBaseStructuralConsistency(ExtractedContent content)
    {
        var headingCount = content.Headings?.Count ?? 0;
        var textLength = content.MainContent?.Length ?? content.Text?.Length ?? 0;

        if (textLength == 0) return 0.0;
        if (headingCount == 0) return 0.5; // 구조가 없으면 중간 점수

        var headingDensity = (double)headingCount / (textLength / 1000.0);
        return Math.Min(1.0, headingDensity * 0.1); // 적절한 헤딩 밀도 점수
    }

    private static Task<double> EstimateSemanticCohesion(string text, CancellationToken cancellationToken)
    {
        // 간단한 의미적 응집성 추정 (실제로는 NLP 모델 사용)
        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length < 2) return Task.FromResult(1.0);

        var cohesionSum = 0.0;
        for (int i = 0; i < sentences.Length - 1; i++)
        {
            cohesionSum += CalculateTextSimilarity(sentences[i], sentences[i + 1]);
        }

        return Task.FromResult(cohesionSum / (sentences.Length - 1));
    }

    private static double CalculateReadabilityScore(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0.0;

        var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        var words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        if (sentences.Length == 0 || words.Length == 0) return 0.0;

        var avgWordsPerSentence = (double)words.Length / sentences.Length;
        var avgCharsPerWord = words.Average(w => w.Length);

        // 간단한 가독성 점수 (낮을수록 읽기 쉬움)
        var complexityScore = avgWordsPerSentence * 0.1 + avgCharsPerWord * 0.2;
        return Math.Max(0.0, 1.0 - complexityScore / 20.0);
    }

    private static double CalculateTextSimilarity(string text1, string text2)
    {
        // 간단한 텍스트 유사성 계산 (Jaccard 유사성)
        var words1 = text1.ToLowerInvariant().Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.ToLowerInvariant().Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (words1.Count == 0 && words2.Count == 0) return 1.0;
        if (words1.Count == 0 || words2.Count == 0) return 0.0;

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return (double)intersection / union;
    }

    #endregion

    // ===================================================================
    // LoggerMessage Definitions
    // ===================================================================

    [LoggerMessage(Level = LogLevel.Debug, Message = "Using cached chunking result for {Url}")]
    private static partial void LogUsingCachedResult(ILogger logger, string Url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Selected strategy: {Strategy} with score: {Score} for content: {Url}")]
    private static partial void LogSelectedStrategy(ILogger logger, string Strategy, double Score, string Url);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in auto chunking strategy for {Url}")]
    private static partial void LogAutoChunkingError(ILogger logger, Exception ex, string Url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get cached result for key: {CacheKey}")]
    private static partial void LogCacheGetFailed(ILogger logger, Exception ex, string CacheKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Strategy scores: {Scores}")]
    private static partial void LogStrategyScores(ILogger logger, string Scores);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached chunking result with quality score: {QualityScore}")]
    private static partial void LogCachedChunkingResult(ILogger logger, double QualityScore);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to cache chunking result")]
    private static partial void LogCacheSetFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get performance history for strategy: {Strategy}")]
    private static partial void LogPerformanceHistoryFailed(ILogger logger, Exception ex, string Strategy);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to calculate token efficiency")]
    private static partial void LogTokenEfficiencyFailed(ILogger logger, Exception ex);
}
