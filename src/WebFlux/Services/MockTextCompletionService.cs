using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// Mock 텍스트 완성 서비스 (테스트용)
/// 실제 AI 서비스 대신 가짜 응답 제공
/// DEBUG 빌드에서만 사용되며, Release에서는 더미 데이터 기반으로 동작
/// </summary>
#if DEBUG
public class MockTextCompletionService : ITextCompletionService
#else
internal class MockTextCompletionService : ITextCompletionService
#endif
{
    private readonly Random _random = new();
    private readonly Dictionary<string, string[]> _responseTemplates;
    private int _requestCount = 0;
    private bool _simulateErrors = false;
    private TimeSpan _responseDelay = TimeSpan.FromMilliseconds(100);

    public MockTextCompletionService()
    {
#if DEBUG
        // DEBUG 모드: 다양한 시나리오 테스트를 위한 풍부한 응답 데이터
        _responseTemplates = new Dictionary<string, string[]>
        {
            ["summarize"] = new[]
            {
                "This content discusses key topics including market trends, technology developments, and strategic recommendations for business growth.",
                "The article covers important aspects of digital transformation, customer engagement strategies, and operational efficiency improvements.",
                "Main points include industry analysis, competitive landscape, and future opportunities in the emerging market sectors."
            },
#else
        // RELEASE 모드: 프로덕션에서는 간소화된 더미 데이터만 제공
        _responseTemplates = new Dictionary<string, string[]>
        {
            ["summarize"] = new[]
            {
                "Content summary: Key business insights and strategic recommendations provided.",
                "Analysis of main topics with actionable conclusions for implementation."
            },
#endif
            ["analyze"] = new[]
            {
#if DEBUG
                "Analysis reveals three critical factors: market positioning, resource allocation, and strategic timing for optimal implementation.",
                "Key findings indicate strong correlation between user engagement metrics and conversion rates across multiple touchpoints.",
                "Data suggests significant opportunities for optimization through automation, personalization, and enhanced user experience design."
#else
                "Analysis complete: Key factors and strategic recommendations identified.",
                "Data insights: Performance metrics and optimization opportunities outlined."
#endif
            },
            ["extract"] = new[]
            {
#if DEBUG
                "• Key insight: Customer satisfaction drives retention\n• Important fact: 85% improvement in efficiency\n• Notable trend: Mobile usage increasing 40% yearly",
                "• Primary finding: Cost reduction of 25% achieved\n• Critical factor: Integration challenges identified\n• Success metric: 95% user adoption rate",
                "• Main conclusion: Strategy alignment essential\n• Risk factor: Market volatility concerns\n• Opportunity: New customer segments emerging"
#else
                "• Key insights extracted from content\n• Important metrics and trends identified\n• Strategic recommendations provided"
#endif
            },
            ["default"] = new[]
            {
#if DEBUG
                "This is a comprehensive analysis of the provided content, highlighting key themes and actionable insights for strategic decision-making.",
                "The content presents valuable information that can be leveraged for improving operational efficiency and driving business growth.",
                "Based on the context provided, several important considerations emerge for effective implementation and long-term success."
#else
                "Content analysis completed with key insights and recommendations.",
                "Strategic overview provided based on content evaluation."
#endif
            }
        };
    }

    /// <summary>
    /// 텍스트 완성 요청 처리
    /// </summary>
    /// <param name="prompt">입력 프롬프트</param>
    /// <param name="maxTokens">최대 토큰 수</param>
    /// <param name="temperature">창의성 온도</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>완성된 텍스트</returns>
    public async Task<string> CompleteTextAsync(
        string prompt,
        int maxTokens = 1000,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
    {
        _requestCount++;

        // 응답 지연 시뮬레이션
        await Task.Delay(_responseDelay, cancellationToken);

        // 오류 시뮬레이션
        if (_simulateErrors && _requestCount % 10 == 0)
        {
            throw new InvalidOperationException("Mock API rate limit exceeded (simulated error)");
        }

        if (_requestCount % 50 == 0)
        {
            throw new TimeoutException("Mock API timeout (simulated network error)");
        }

        // 프롬프트 분석하여 적절한 응답 카테고리 선택
        var category = DetermineResponseCategory(prompt.ToLowerInvariant());
        var templates = _responseTemplates[category];
        var selectedTemplate = templates[_random.Next(templates.Length)];

        // 토큰 길이에 따른 응답 조정
        var response = AdjustResponseLength(selectedTemplate, maxTokens);

        return response;
    }

    /// <summary>
    /// 스트리밍 텍스트 완성 (시뮬레이션)
    /// </summary>
    /// <param name="prompt">입력 프롬프트</param>
    /// <param name="maxTokens">최대 토큰 수</param>
    /// <param name="temperature">창의성 온도</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>텍스트 스트림</returns>
    public async IAsyncEnumerable<string> CompleteTextStreamAsync(
        string prompt,
        int maxTokens = 1000,
        float temperature = 0.7f,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fullResponse = await CompleteTextAsync(prompt, maxTokens, temperature, cancellationToken);
        var words = fullResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 단어별로 스트리밍 시뮬레이션
        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken); // 스트리밍 지연
            yield return word + " ";
        }
    }

    /// <summary>
    /// 서비스 상태 확인
    /// </summary>
    /// <returns>서비스 상태 정보</returns>
    public Task<ServiceHealthInfo> GetHealthAsync()
    {
        return Task.FromResult(new ServiceHealthInfo
        {
            ServiceName = "MockTextCompletionService",
            IsHealthy = !_simulateErrors || _requestCount % 5 != 0,
            ResponseTimeMs = (int)_responseDelay.TotalMilliseconds,
            RequestCount = _requestCount,
            LastError = _simulateErrors && _requestCount % 5 == 0 ? "Simulated service degradation" : null,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["SimulateErrors"] = _simulateErrors,
                ["ResponseDelay"] = _responseDelay.TotalMilliseconds,
                ["AvailableCategories"] = string.Join(", ", _responseTemplates.Keys)
            }
        });
    }

    /// <summary>
    /// 오류 시뮬레이션 활성화/비활성화
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetErrorSimulation(bool enabled)
    {
        _simulateErrors = enabled;
    }

    /// <summary>
    /// 응답 지연 시간 설정
    /// </summary>
    /// <param name="delay">지연 시간</param>
    public void SetResponseDelay(TimeSpan delay)
    {
        _responseDelay = delay;
    }

    /// <summary>
    /// 요청 횟수 재설정
    /// </summary>
    public void ResetRequestCount()
    {
        _requestCount = 0;
    }

    /// <summary>
    /// 프롬프트 기반 응답 카테고리 결정
    /// </summary>
    /// <param name="prompt">소문자 프롬프트</param>
    /// <returns>응답 카테고리</returns>
    private string DetermineResponseCategory(string prompt)
    {
        if (prompt.Contains("summarize") || prompt.Contains("summary") || prompt.Contains("요약"))
            return "summarize";

        if (prompt.Contains("analyze") || prompt.Contains("analysis") || prompt.Contains("분석"))
            return "analyze";

        if (prompt.Contains("extract") || prompt.Contains("key points") || prompt.Contains("추출"))
            return "extract";

        return "default";
    }

    /// <summary>
    /// 최대 토큰 수에 따른 응답 길이 조정
    /// </summary>
    /// <param name="template">기본 템플릿</param>
    /// <param name="maxTokens">최대 토큰 수</param>
    /// <returns>조정된 응답</returns>
    private string AdjustResponseLength(string template, int maxTokens)
    {
        // 간단한 토큰 추정 (평균 4글자 = 1토큰)
        var estimatedTokens = template.Length / 4;

        if (estimatedTokens <= maxTokens)
            return template;

        // 토큰 제한에 맞게 잘라내기
        var targetLength = maxTokens * 4;
        var truncated = template.Substring(0, Math.Min(targetLength, template.Length));

        // 단어 단위로 자르기
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > targetLength * 0.8) // 80% 이상이면 단어 단위로 자르기
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + "...";
    }
}