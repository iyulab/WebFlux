using System.Runtime.CompilerServices;
using OpenAI.Chat;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.SimpleTest.Services;

/// <summary>
/// OpenAI 공식 SDK를 사용한 ITextCompletionService 구현
/// </summary>
public class OpenAiTextCompletionService : ITextCompletionService
{
    private readonly ChatClient _chatClient;
    private readonly string _model;

    public OpenAiTextCompletionService(string model, string apiKey)
    {
        _model = model;
        _chatClient = new ChatClient(model, apiKey);
    }

    public async Task<string> CompleteAsync(
        string prompt,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new TextCompletionOptions();

        var messages = new List<ChatMessage>();

        // 시스템 프롬프트가 있으면 추가
        if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
        {
            messages.Add(new SystemChatMessage(options.SystemPrompt));
        }

        messages.Add(new UserChatMessage(prompt));

        var chatOptions = CreateChatCompletionOptions(options);

        var completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

        if (completion.Value.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Value.Content[0].Text))
        {
            throw new InvalidOperationException(
                $"OpenAI API returned empty content (Finish Reason: {completion.Value.FinishReason})");
        }

        return completion.Value.Content[0].Text;
    }

    public async IAsyncEnumerable<string> CompleteStreamAsync(
        string prompt,
        TextCompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new TextCompletionOptions();

        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(options.SystemPrompt))
        {
            messages.Add(new SystemChatMessage(options.SystemPrompt));
        }

        messages.Add(new UserChatMessage(prompt));

        var chatOptions = CreateChatCompletionOptions(options);

        await foreach (var update in _chatClient.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken))
        {
            if (update.ContentUpdate.Count > 0)
            {
                var text = update.ContentUpdate[0].Text;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
    }

    public async Task<IReadOnlyList<string>> CompleteBatchAsync(
        IEnumerable<string> prompts,
        TextCompletionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        foreach (var prompt in prompts)
        {
            var result = await CompleteAsync(prompt, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 간단한 테스트 메시지로 확인
            var testCompletion = await _chatClient.CompleteChatAsync(
                [new UserChatMessage("test")],
                new ChatCompletionOptions { MaxOutputTokenCount = 10 },
                cancellationToken);

            return testCompletion.Value.Content.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public ServiceHealthInfo GetHealthInfo()
    {
        return new ServiceHealthInfo
        {
            ServiceName = "OpenAI",
            Status = ServiceStatus.Healthy,
            ResponseTimeMs = 0,
            AvailableModels = [_model],
            LastChecked = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["Provider"] = "OpenAI",
                ["Model"] = _model,
                ["SDK"] = "OpenAI .NET v2.4.0"
            }
        };
    }

    private ChatCompletionOptions CreateChatCompletionOptions(TextCompletionOptions options)
    {
        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = options.MaxTokens
        };

        // gpt-5 모델은 temperature 기본값만 지원
        if (!_model.Contains("gpt-5"))
        {
            chatOptions.Temperature = (float)options.Temperature;
            chatOptions.TopP = (float)options.TopP;
            chatOptions.FrequencyPenalty = (float)options.FrequencyPenalty;
            chatOptions.PresencePenalty = (float)options.PresencePenalty;
        }

        return chatOptions;
    }
}
