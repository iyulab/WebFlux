using System.Text.Json;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services;

/// <summary>
/// 실제 OpenAI API 기반 텍스트 완성 서비스
/// GPT 모델을 사용한 텍스트 생성 및 요약
/// </summary>
public class OpenAITextCompletionService : ITextCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl = "https://api.openai.com/v1";
    private int _requestCount = 0;
    private readonly SemaphoreSlim _rateLimitSemaphore = new(10, 10); // 동시 요청 제한

    public OpenAITextCompletionService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // 환경 변수에서 설정 로드
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                 ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");

        _model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-3.5-turbo";

        // HTTP 클라이언트 설정
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WebFlux-SDK/1.0");
        _httpClient.Timeout = TimeSpan.FromMinutes(2); // 2분 타임아웃
    }

    /// <summary>
    /// 주어진 프롬프트에 대한 텍스트 완성을 수행합니다.
    /// </summary>
    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await _rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            Interlocked.Increment(ref _requestCount);

            var maxTokens = options?.MaxTokens ?? 1000;
            var temperature = options?.Temperature ?? 0.7f;

            var requestData = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant specialized in content processing and analysis." },
                    new { role = "user", content = prompt }
                },
                max_tokens = Math.Min(maxTokens, 4000), // API 제한 고려
                temperature = Math.Max(0.0f, Math.Min(2.0f, temperature)),
                stream = false
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"OpenAI API request failed: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (responseData.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var responseContent))
                {
                    return responseContent.GetString() ?? string.Empty;
                }
            }

            throw new InvalidOperationException("Invalid response format from OpenAI API");
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException("OpenAI API request timed out");
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"OpenAI API error: {ex.Message}", ex);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// 스트리밍 방식으로 텍스트 완성을 수행합니다.
    /// </summary>
    public async IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 현재는 일반 완성을 단어별로 분할하여 스트리밍 시뮬레이션
        var fullResponse = await CompleteAsync(prompt, options, cancellationToken);
        var words = fullResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken); // 스트리밍 시뮬레이션
            yield return word + " ";
        }
    }

    /// <summary>
    /// 여러 프롬프트에 대한 배치 처리를 수행합니다.
    /// </summary>
    public async Task<IReadOnlyList<string>> CompleteBatchAsync(
        IEnumerable<string> prompts,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = prompts.Select(prompt => CompleteAsync(prompt, options, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList().AsReadOnly();
    }

    /// <summary>
    /// 서비스의 사용 가능 여부를 확인합니다.
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testPrompt = "Test connection. Respond with 'OK'.";
            var response = await CompleteAsync(testPrompt, new TextCompletionOptions { MaxTokens = 10, Temperature = 0.0f }, cancellationToken);
            return !string.IsNullOrEmpty(response);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 서비스의 현재 상태 정보를 반환합니다.
    /// </summary>
    public ServiceHealthInfo GetHealthInfo()
    {
        return new ServiceHealthInfo
        {
            ServiceName = "OpenAITextCompletionService",
            IsHealthy = true, // 실제로는 비동기 상태 확인 필요
            ResponseTimeMs = 0, // 실제 구현에서는 측정 필요
            RequestCount = _requestCount,
            LastError = null,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["Model"] = _model,
                ["RateLimitRemaining"] = _rateLimitSemaphore.CurrentCount,
                ["BaseUrl"] = _baseUrl,
                ["ConnectionStatus"] = "Active"
            }
        };
    }


    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }
}

/// <summary>
/// OpenAI 구성 옵션
/// </summary>
public class OpenAIOptions
{
    /// <summary>
    /// API 키
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 사용할 모델
    /// </summary>
    public string Model { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// API 기본 URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// 최대 동시 요청 수
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// 요청 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// 비용 모니터링 활성화
    /// </summary>
    public bool EnableCostMonitoring { get; set; } = true;

    /// <summary>
    /// 일일 최대 비용 한도 (USD)
    /// </summary>
    public decimal DailyCostLimit { get; set; } = 10.0m;
}