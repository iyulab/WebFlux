using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;

namespace WebFlux.Services.AI;

/// <summary>
/// Mock 텍스트 임베딩 서비스 (테스트 및 데모용)
/// 실제 AI 서비스 없이도 의미론적 청킹 테스트 가능
/// </summary>
public class MockTextEmbeddingService : ITextEmbeddingService
{
    private readonly ILogger<MockTextEmbeddingService> _logger;
    private readonly Random _random = new();

    public int MaxTokens => 8191;
    public int EmbeddingDimension => 1536;

    public MockTextEmbeddingService(ILogger<MockTextEmbeddingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 단일 텍스트를 Mock 임베딩 벡터로 변환
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[EmbeddingDimension];

        var embeddings = await GetEmbeddingsAsync(new[] { text }, cancellationToken);
        return embeddings.FirstOrDefault() ?? new float[EmbeddingDimension];
    }

    /// <summary>
    /// 여러 텍스트를 배치로 Mock 임베딩 벡터로 변환
    /// </summary>
    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
            return Array.Empty<float[]>();

        _logger.LogDebug("Mock 임베딩 생성: {TextCount}개 텍스트", texts.Count);

        // 짧은 지연 시뮬레이션
        await Task.Delay(50, cancellationToken);

        var embeddings = new List<float[]>();

        foreach (var text in texts)
        {
            var embedding = GenerateSemanticEmbedding(text);
            embeddings.Add(embedding);
        }

        return embeddings;
    }

    /// <summary>
    /// 텍스트 내용을 기반으로 의미있는 Mock 임베딩 생성
    /// </summary>
    private float[] GenerateSemanticEmbedding(string text)
    {
        var embedding = new float[EmbeddingDimension];
        var hash = text.GetHashCode();
        var random = new Random(hash); // 동일한 텍스트에 대해 일관된 임베딩 생성

        // 텍스트의 특성을 반영한 임베딩 생성
        var textFeatures = ExtractTextFeatures(text);

        for (int i = 0; i < EmbeddingDimension; i++)
        {
            // 기본 랜덤 값
            float value = (float)(random.NextDouble() * 2 - 1); // -1 ~ 1

            // 텍스트 특성 반영
            if (i < textFeatures.Length)
            {
                value += textFeatures[i] * 0.3f; // 특성 가중치 적용
            }

            // 정규화
            embedding[i] = Math.Max(-1f, Math.Min(1f, value));
        }

        // 벡터 정규화 (단위 벡터로 변환)
        var norm = Math.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= (float)norm;
            }
        }

        return embedding;
    }

    /// <summary>
    /// 텍스트에서 의미적 특성 추출
    /// </summary>
    private float[] ExtractTextFeatures(string text)
    {
        var features = new List<float>();

        // 1. 텍스트 길이 특성 (정규화)
        features.Add(Math.Min(1f, text.Length / 1000f));

        // 2. 단어 수 특성
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        features.Add(Math.Min(1f, wordCount / 100f));

        // 3. 문장 수 특성
        var sentenceCount = text.Split('.', '!', '?').Length - 1;
        features.Add(Math.Min(1f, sentenceCount / 10f));

        // 4. 키워드 기반 의미 특성
        var keywords = new Dictionary<string, float>
        {
            {"기술", 0.8f}, {"개발", 0.8f}, {"프로그래밍", 0.8f},
            {"비즈니스", 0.6f}, {"마케팅", 0.6f}, {"판매", 0.6f},
            {"교육", 0.5f}, {"학습", 0.5f}, {"연구", 0.5f},
            {"정치", 0.4f}, {"사회", 0.4f}, {"문화", 0.4f}
        };

        var textLower = text.ToLower();
        foreach (var keyword in keywords)
        {
            if (textLower.Contains(keyword.Key))
            {
                features.Add(keyword.Value);
            }
        }

        // 5. 특수 문자 비율
        var specialChars = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        features.Add(Math.Min(1f, specialChars / (float)text.Length));

        // 6. 숫자 포함 여부
        features.Add(text.Any(char.IsDigit) ? 1f : 0f);

        // 7. 대문자 비율
        var upperCaseRatio = text.Count(char.IsUpper) / (float)Math.Max(1, text.Length);
        features.Add(Math.Min(1f, upperCaseRatio));

        // 특성 배열 크기를 고정된 크기로 맞춤
        while (features.Count < 50) // 처음 50개 차원에 특성 반영
        {
            features.Add(0f);
        }

        return features.Take(50).ToArray();
    }
}