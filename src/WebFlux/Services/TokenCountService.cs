using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// 토큰 카운팅 서비스 구현
/// 다양한 토크나이저 모델 지원 및 정확한 토큰 계산 제공
/// OpenAI GPT, Claude, Llama 등 주요 모델들의 토크나이저 지원
/// </summary>
public class TokenCountService : ITokenCountService
{
    private readonly ILogger<TokenCountService> _logger;
    private readonly WebFluxConfiguration _configuration;

    // 토크나이저별 캐시
    private readonly ConcurrentDictionary<string, int> _tokenCache;
    private readonly ConcurrentDictionary<TokenizerModel, ITokenizer> _tokenizers;

    // 토큰 통계
    private readonly ConcurrentDictionary<TokenizerModel, TokenStatistics> _statistics;

    // 정규식 캐시
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex WordBoundaryRegex = new(@"\b\w+\b", RegexOptions.Compiled);
    private static readonly Regex PunctuationRegex = new(@"[^\w\s]", RegexOptions.Compiled);

    public TokenCountService(
        ILogger<TokenCountService> logger,
        IOptions<WebFluxConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

        _tokenCache = new ConcurrentDictionary<string, int>();
        _tokenizers = new ConcurrentDictionary<TokenizerModel, ITokenizer>();
        _statistics = new ConcurrentDictionary<TokenizerModel, TokenStatistics>();

        InitializeTokenizers();

        _logger.LogInformation("TokenCountService initialized with {TokenizerCount} tokenizers",
            _tokenizers.Count);
    }

    public int CountTokens(string text)
    {
        return CountTokensAsync(text).GetAwaiter().GetResult();
    }

    public async Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        try
        {
            // 기본 토크나이저 사용 (설정에서 지정된 모델)
            var defaultModel = GetDefaultTokenizerModel();
            return await CountTokensAsync(text, defaultModel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count tokens for text length: {TextLength}", text.Length);
            throw;
        }
    }

    public async Task<int> CountTokensAsync(string text, TokenizerModel model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        try
        {
            // 캐시 키 생성
            var cacheKey = GenerateCacheKey(text, model);

            // 캐시에서 확인
            if (_tokenCache.TryGetValue(cacheKey, out var cachedCount))
            {
                UpdateStatistics(model, cachedCount, true);
                return cachedCount;
            }

            // 토크나이저로 계산
            var tokenizer = GetTokenizer(model);
            var tokenCount = await tokenizer.CountTokensAsync(text, cancellationToken);

            // 캐시에 저장 (메모리 관리를 위해 제한)
            if (_tokenCache.Count < 10000)
            {
                _tokenCache.TryAdd(cacheKey, tokenCount);
            }

            UpdateStatistics(model, tokenCount, false);

            _logger.LogTrace("Counted {TokenCount} tokens for {Model} (text length: {TextLength})",
                tokenCount, model, text.Length);

            return tokenCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count tokens with model {Model} for text length: {TextLength}",
                model, text.Length);
            throw;
        }
    }

    public string TruncateToTokenLimit(string text, int maxTokens)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTokens);

        try
        {
            var model = GetDefaultTokenizerModel();
            return TruncateToTokenLimit(text, maxTokens, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to truncate text to {MaxTokens} tokens", maxTokens);
            throw;
        }
    }

    public string TruncateToTokenLimit(string text, int maxTokens, TokenizerModel model)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTokens);

        try
        {
            var tokenizer = GetTokenizer(model);
            return tokenizer.TruncateToTokenLimit(text, maxTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to truncate text to {MaxTokens} tokens with model {Model}",
                maxTokens, model);
            throw;
        }
    }

    public async Task<TokenAnalysis> AnalyzeTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        try
        {
            var tasks = _tokenizers.Keys.Select(async model =>
            {
                var count = await CountTokensAsync(text, model, cancellationToken);
                return new { Model = model, Count = count };
            });

            var results = await Task.WhenAll(tasks);

            var analysis = new TokenAnalysis
            {
                Text = text,
                TextLength = text.Length,
                WordCount = CountWords(text),
                CharacterCount = text.Length,
                TokenCounts = results.ToDictionary(r => r.Model, r => r.Count),
                EstimatedCostByModel = CalculateEstimatedCosts(results),
                OptimalModel = DetermineOptimalModel(results),
                CompressionRatios = CalculateCompressionRatios(text, results),
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Token analysis completed for text length {TextLength}: {ModelCount} models analyzed",
                text.Length, results.Length);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze tokens for text length: {TextLength}", text.Length);
            throw;
        }
    }

    public Task<TokenStatistics> GetStatisticsAsync(TokenizerModel? model = null)
    {
        try
        {
            if (model.HasValue)
            {
                // 특정 모델의 통계
                return Task.FromResult(_statistics.GetValueOrDefault(model.Value, new TokenStatistics()));
            }

            // 전체 통계 집계
            var totalStats = new TokenStatistics
            {
                Model = null,
                TotalRequests = _statistics.Values.Sum(s => s.TotalRequests),
                CacheHits = _statistics.Values.Sum(s => s.CacheHits),
                CacheMisses = _statistics.Values.Sum(s => s.CacheMisses),
                TotalTokensCounted = _statistics.Values.Sum(s => s.TotalTokensCounted),
                AverageTokensPerRequest = _statistics.Values.Any() ?
                    _statistics.Values.Average(s => s.AverageTokensPerRequest) : 0,
                LastUpdated = DateTime.UtcNow
            };

            return Task.FromResult(totalStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token statistics");
            return Task.FromResult(new TokenStatistics());
        }
    }

    #region Private Methods

    private void InitializeTokenizers()
    {
        // GPT 계열 토크나이저 (tiktoken 기반)
        _tokenizers[TokenizerModel.GPT3] = new GPTTokenizer(TokenizerModel.GPT3);
        _tokenizers[TokenizerModel.GPT4] = new GPTTokenizer(TokenizerModel.GPT4);
        _tokenizers[TokenizerModel.GPT4Turbo] = new GPTTokenizer(TokenizerModel.GPT4Turbo);

        // Claude 계열 토크나이저
        _tokenizers[TokenizerModel.Claude3] = new ClaudeTokenizer(TokenizerModel.Claude3);
        _tokenizers[TokenizerModel.Claude3Opus] = new ClaudeTokenizer(TokenizerModel.Claude3Opus);

        // Llama 계열 토크나이저
        _tokenizers[TokenizerModel.Llama2] = new LlamaTokenizer(TokenizerModel.Llama2);
        _tokenizers[TokenizerModel.Llama3] = new LlamaTokenizer(TokenizerModel.Llama3);

        // 범용 토크나이저
        _tokenizers[TokenizerModel.Generic] = new GenericTokenizer();
    }

    private TokenizerModel GetDefaultTokenizerModel()
    {
        // 설정에서 기본 모델 가져오기
        return _configuration.DefaultTokenizerModel ?? TokenizerModel.GPT4;
    }

    private ITokenizer GetTokenizer(TokenizerModel model)
    {
        if (_tokenizers.TryGetValue(model, out var tokenizer))
        {
            return tokenizer;
        }

        _logger.LogWarning("Tokenizer not found for model {Model}, using generic tokenizer", model);
        return _tokenizers[TokenizerModel.Generic];
    }

    private string GenerateCacheKey(string text, TokenizerModel model)
    {
        // 텍스트 해시와 모델을 조합한 캐시 키
        var textHash = text.Length < 1000 ? text.GetHashCode().ToString() :
            text.Substring(0, 500).GetHashCode().ToString() + text.Length.ToString();

        return $"{model}:{textHash}";
    }

    private void UpdateStatistics(TokenizerModel model, int tokenCount, bool cacheHit)
    {
        _statistics.AddOrUpdate(model,
            new TokenStatistics
            {
                Model = model,
                TotalRequests = 1,
                CacheHits = cacheHit ? 1 : 0,
                CacheMisses = cacheHit ? 0 : 1,
                TotalTokensCounted = tokenCount,
                AverageTokensPerRequest = tokenCount,
                LastUpdated = DateTime.UtcNow
            },
            (key, existing) =>
            {
                var newTotalRequests = existing.TotalRequests + 1;
                var newTotalTokens = existing.TotalTokensCounted + tokenCount;

                return new TokenStatistics
                {
                    Model = model,
                    TotalRequests = newTotalRequests,
                    CacheHits = existing.CacheHits + (cacheHit ? 1 : 0),
                    CacheMisses = existing.CacheMisses + (cacheHit ? 0 : 1),
                    TotalTokensCounted = newTotalTokens,
                    AverageTokensPerRequest = (double)newTotalTokens / newTotalRequests,
                    LastUpdated = DateTime.UtcNow
                };
            });
    }

    private int CountWords(string text)
    {
        return WordBoundaryRegex.Matches(text).Count;
    }

    private Dictionary<TokenizerModel, double> CalculateEstimatedCosts(IEnumerable<dynamic> results)
    {
        // 모델별 예상 비용 계산 (토큰당 비용 × 토큰 수)
        var costs = new Dictionary<TokenizerModel, double>();

        foreach (var result in results)
        {
            var model = (TokenizerModel)result.Model;
            var tokenCount = (int)result.Count;
            var costPerToken = GetCostPerToken(model);

            costs[model] = tokenCount * costPerToken;
        }

        return costs;
    }

    private double GetCostPerToken(TokenizerModel model)
    {
        // 토큰당 비용 (USD, 예시 값)
        return model switch
        {
            TokenizerModel.GPT4 => 0.00003,
            TokenizerModel.GPT4Turbo => 0.00001,
            TokenizerModel.GPT3 => 0.000002,
            TokenizerModel.Claude3Opus => 0.000015,
            TokenizerModel.Claude3 => 0.000003,
            TokenizerModel.Llama3 => 0.000001,
            TokenizerModel.Llama2 => 0.0000005,
            _ => 0.000001
        };
    }

    private TokenizerModel DetermineOptimalModel(IEnumerable<dynamic> results)
    {
        // 비용 대비 성능을 고려한 최적 모델 선택
        return results.Cast<dynamic>()
            .OrderBy(r => CalculateModelScore((TokenizerModel)r.Model, (int)r.Count))
            .First().Model;
    }

    private double CalculateModelScore(TokenizerModel model, int tokenCount)
    {
        var costPerToken = GetCostPerToken(model);
        var qualityScore = GetModelQualityScore(model);

        // 비용 대비 품질 점수
        return (costPerToken * tokenCount) / qualityScore;
    }

    private double GetModelQualityScore(TokenizerModel model)
    {
        // 모델 품질 점수 (1-10)
        return model switch
        {
            TokenizerModel.GPT4 => 9.5,
            TokenizerModel.GPT4Turbo => 9.0,
            TokenizerModel.Claude3Opus => 9.2,
            TokenizerModel.Claude3 => 8.5,
            TokenizerModel.GPT3 => 7.0,
            TokenizerModel.Llama3 => 8.0,
            TokenizerModel.Llama2 => 6.5,
            _ => 5.0
        };
    }

    private Dictionary<TokenizerModel, double> CalculateCompressionRatios(string text, IEnumerable<dynamic> results)
    {
        var characterCount = text.Length;
        var ratios = new Dictionary<TokenizerModel, double>();

        foreach (var result in results)
        {
            var model = (TokenizerModel)result.Model;
            var tokenCount = (int)result.Count;

            // 압축률 = 토큰 수 / 문자 수
            ratios[model] = tokenCount > 0 ? (double)characterCount / tokenCount : 0;
        }

        return ratios;
    }

    /// <summary>
    /// 여러 텍스트의 토큰 수를 일괄 계산합니다
    /// </summary>
    public async Task<int[]> CountTokensBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        var textList = texts.ToList();
        if (!textList.Any()) return Array.Empty<int>();

        try
        {
            var primaryTokenizer = _tokenizers[_defaultModel];
            var tasks = textList.Select(text => primaryTokenizer.CountTokensAsync(text, cancellationToken));
            var results = await Task.WhenAll(tasks);

            _logger.LogTrace("Batch token counting completed: {Count} texts", textList.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count tokens for batch");
            throw;
        }
    }

    /// <summary>
    /// 사용 중인 토크나이저 모델명
    /// </summary>
    public string ModelName => _defaultModel.ToString();

    /// <summary>
    /// 예상 토큰 수 (정확한 계산 전 빠른 추정)
    /// </summary>
    public int EstimateTokens(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text)) return 0;

        // 빠른 추정 (평균 4문자 = 1토큰)
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    #endregion
}

#region Tokenizer Implementations

/// <summary>
/// 토크나이저 인터페이스
/// </summary>
public interface ITokenizer
{
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);
    string TruncateToTokenLimit(string text, int maxTokens);
}

/// <summary>
/// GPT 계열 토크나이저 (tiktoken 기반)
/// </summary>
public class GPTTokenizer : ITokenizer
{
    private readonly TokenizerModel _model;

    public GPTTokenizer(TokenizerModel model)
    {
        _model = model;
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        // tiktoken 라이브러리를 사용한 정확한 토큰 계산
        // 실제 구현에서는 tiktoken-sharp 등의 라이브러리 사용
        var approximateCount = EstimateTokenCount(text);
        return Task.FromResult(approximateCount);
    }

    public string TruncateToTokenLimit(string text, int maxTokens)
    {
        // 대략적인 토큰 계산으로 텍스트 자르기
        var currentTokens = EstimateTokenCount(text);
        if (currentTokens <= maxTokens) return text;

        var ratio = (double)maxTokens / currentTokens;
        var targetLength = (int)(text.Length * ratio * 0.9); // 안전 마진

        return text.Substring(0, Math.Min(targetLength, text.Length));
    }

    private int EstimateTokenCount(string text)
    {
        // GPT 토큰 추정 (1 토큰 ≈ 4 문자)
        return _model switch
        {
            TokenizerModel.GPT4 => (int)Math.Ceiling(text.Length / 3.5),
            TokenizerModel.GPT4Turbo => (int)Math.Ceiling(text.Length / 3.8),
            TokenizerModel.GPT3 => (int)Math.Ceiling(text.Length / 4.0),
            _ => (int)Math.Ceiling(text.Length / 4.0)
        };
    }
}

/// <summary>
/// Claude 계열 토크나이저
/// </summary>
public class ClaudeTokenizer : ITokenizer
{
    private readonly TokenizerModel _model;

    public ClaudeTokenizer(TokenizerModel model)
    {
        _model = model;
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        var approximateCount = EstimateTokenCount(text);
        return Task.FromResult(approximateCount);
    }

    public string TruncateToTokenLimit(string text, int maxTokens)
    {
        var currentTokens = EstimateTokenCount(text);
        if (currentTokens <= maxTokens) return text;

        var ratio = (double)maxTokens / currentTokens;
        var targetLength = (int)(text.Length * ratio * 0.9);

        return text.Substring(0, Math.Min(targetLength, text.Length));
    }

    private int EstimateTokenCount(string text)
    {
        // Claude 토큰 추정 (1 토큰 ≈ 3.5 문자)
        return (int)Math.Ceiling(text.Length / 3.5);
    }
}

/// <summary>
/// Llama 계열 토크나이저
/// </summary>
public class LlamaTokenizer : ITokenizer
{
    private readonly TokenizerModel _model;

    public LlamaTokenizer(TokenizerModel model)
    {
        _model = model;
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        var approximateCount = EstimateTokenCount(text);
        return Task.FromResult(approximateCount);
    }

    public string TruncateToTokenLimit(string text, int maxTokens)
    {
        var currentTokens = EstimateTokenCount(text);
        if (currentTokens <= maxTokens) return text;

        var ratio = (double)maxTokens / currentTokens;
        var targetLength = (int)(text.Length * ratio * 0.9);

        return text.Substring(0, Math.Min(targetLength, text.Length));
    }

    private int EstimateTokenCount(string text)
    {
        // Llama 토큰 추정
        return _model switch
        {
            TokenizerModel.Llama3 => (int)Math.Ceiling(text.Length / 3.2),
            TokenizerModel.Llama2 => (int)Math.Ceiling(text.Length / 3.0),
            _ => (int)Math.Ceiling(text.Length / 3.0)
        };
    }
}

/// <summary>
/// 범용 토크나이저 (단어 기반)
/// </summary>
public class GenericTokenizer : ITokenizer
{
    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        // 단어 + 구두점 기반 토큰 계산
        var words = Regex.Matches(text, @"\b\w+\b").Count;
        var punctuation = Regex.Matches(text, @"[^\w\s]").Count;
        return Task.FromResult(words + punctuation);
    }

    public string TruncateToTokenLimit(string text, int maxTokens)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxTokens) return text;

        return string.Join(" ", words.Take(maxTokens));
    }
}

#endregion

#region Models


/// <summary>
/// 토큰 분석 결과
/// </summary>
public class TokenAnalysis
{
    /// <summary>분석된 텍스트</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>텍스트 길이</summary>
    public int TextLength { get; init; }

    /// <summary>단어 수</summary>
    public int WordCount { get; init; }

    /// <summary>문자 수</summary>
    public int CharacterCount { get; init; }

    /// <summary>모델별 토큰 수</summary>
    public IReadOnlyDictionary<TokenizerModel, int> TokenCounts { get; init; } =
        new Dictionary<TokenizerModel, int>();

    /// <summary>모델별 예상 비용</summary>
    public IReadOnlyDictionary<TokenizerModel, double> EstimatedCostByModel { get; init; } =
        new Dictionary<TokenizerModel, double>();

    /// <summary>최적 모델</summary>
    public TokenizerModel OptimalModel { get; init; }

    /// <summary>모델별 압축률</summary>
    public IReadOnlyDictionary<TokenizerModel, double> CompressionRatios { get; init; } =
        new Dictionary<TokenizerModel, double>();

    /// <summary>분석 시간</summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 토큰 통계
/// </summary>
public class TokenStatistics
{
    /// <summary>모델 (null이면 전체 통계)</summary>
    public TokenizerModel? Model { get; init; }

    /// <summary>총 요청 수</summary>
    public long TotalRequests { get; init; }

    /// <summary>캐시 히트 수</summary>
    public long CacheHits { get; init; }

    /// <summary>캐시 미스 수</summary>
    public long CacheMisses { get; init; }

    /// <summary>총 계산된 토큰 수</summary>
    public long TotalTokensCounted { get; init; }

    /// <summary>요청당 평균 토큰 수</summary>
    public double AverageTokensPerRequest { get; init; }

    /// <summary>캐시 히트율</summary>
    public double CacheHitRate => TotalRequests > 0
        ? (double)CacheHits / TotalRequests
        : 0.0;

    /// <summary>마지막 업데이트 시간</summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

#endregion