using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebFlux.Core.Options;
using WebFlux.Core.Interfaces;

namespace WebFlux.Services.AI;

/// <summary>
/// OpenAI 기반 텍스트 임베딩 서비스
/// text-embedding-3-small 또는 text-embedding-3-large 모델 사용
/// </summary>
public class OpenAITextEmbeddingService : ITextEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAITextEmbeddingService> _logger;
    private readonly AIConfiguration _configuration;
    private const string EMBEDDINGS_ENDPOINT = "https://api.openai.com/v1/embeddings";

    public int MaxTokens => 8191; // text-embedding-3 모델의 최대 토큰 수
    public int EmbeddingDimension => 1536; // text-embedding-3-small 기본 차원

    public OpenAITextEmbeddingService(
        HttpClient httpClient,
        IOptions<AIConfiguration> configuration,
        ILogger<OpenAITextEmbeddingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));

        // OpenAI API 헤더 설정
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);

        if (!string.IsNullOrEmpty(_configuration.Organization))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _configuration.Organization);
        }
    }

    /// <summary>
    /// 단일 텍스트를 임베딩 벡터로 변환
    /// </summary>
    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[EmbeddingDimension];

        var embeddings = await GetEmbeddingsAsync(new[] { text }, cancellationToken);
        return embeddings.FirstOrDefault() ?? new float[EmbeddingDimension];
    }

    /// <summary>
    /// 여러 텍스트를 배치로 임베딩 벡터로 변환
    /// </summary>
    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
            return Array.Empty<float[]>();

        try
        {
            var request = new EmbeddingRequest
            {
                Input = texts.ToArray(),
                Model = _configuration.EmbeddingModel ?? "text-embedding-3-small",
                EncodingFormat = "float"
            };

            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _logger.LogDebug("임베딩 API 요청: {TextCount}개 텍스트, 모델: {Model}",
                texts.Count, request.Model);

            var response = await _httpClient.PostAsync(EMBEDDINGS_ENDPOINT, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("임베딩 API 요청 실패: {StatusCode}, {Error}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"OpenAI 임베딩 API 요청 실패: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (embeddingResponse?.Data == null || embeddingResponse.Data.Length == 0)
            {
                _logger.LogWarning("임베딩 응답이 비어있음");
                return texts.Select(_ => new float[EmbeddingDimension]).ToArray();
            }

            _logger.LogDebug("임베딩 생성 완료: {EmbeddingCount}개, 토큰 사용량: {TokenUsage}",
                embeddingResponse.Data.Length, embeddingResponse.Usage?.TotalTokens ?? 0);

            return embeddingResponse.Data
                .OrderBy(d => d.Index)
                .Select(d => d.Embedding)
                .ToArray();
        }
        catch (Exception ex) when (!(ex is HttpRequestException))
        {
            _logger.LogError(ex, "임베딩 생성 중 오류 발생");
            throw new InvalidOperationException("임베딩 생성 실패", ex);
        }
    }
}

#region DTOs

/// <summary>
/// 임베딩 요청 DTO
/// </summary>
internal class EmbeddingRequest
{
    public string[] Input { get; set; } = Array.Empty<string>();
    public string Model { get; set; } = string.Empty;
    public string? EncodingFormat { get; set; }
    public int? Dimensions { get; set; }
    public string? User { get; set; }
}

/// <summary>
/// 임베딩 응답 DTO
/// </summary>
internal class EmbeddingResponse
{
    public string Object { get; set; } = string.Empty;
    public EmbeddingData[] Data { get; set; } = Array.Empty<EmbeddingData>();
    public string Model { get; set; } = string.Empty;
    public EmbeddingUsage? Usage { get; set; }
}

/// <summary>
/// 임베딩 데이터 DTO
/// </summary>
internal class EmbeddingData
{
    public string Object { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public int Index { get; set; }
}

/// <summary>
/// 임베딩 사용량 DTO
/// </summary>
internal class EmbeddingUsage
{
    public int PromptTokens { get; set; }
    public int TotalTokens { get; set; }
}

#endregion