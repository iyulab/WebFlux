using Microsoft.Extensions.DependencyInjection;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using WebFlux.Services;

namespace WebFlux.Examples.CustomServices;

/// <summary>
/// 커스텀 서비스 구현 예제
/// 자체 청킹 전략과 AI 서비스를 구현하는 방법을 보여줍니다.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== WebFlux SDK - 커스텀 서비스 구현 예제 ===\n");

        // 1. 서비스 컬렉션 구성
        var services = new ServiceCollection();

        // WebFlux 핵심 서비스 등록
        services.AddWebFlux();

        // 커스텀 청킹 전략 등록
        services.AddSingleton<IChunkingStrategy, SentenceBasedChunkingStrategy>();

        // 커스텀 AI 서비스 등록
        services.AddSingleton<ITextCompletionService, SimpleTextCompletionService>();

        var serviceProvider = services.BuildServiceProvider();

        // 2. 서비스 가져오기
        var chunkingStrategy = serviceProvider.GetRequiredService<IChunkingStrategy>();
        var aiService = serviceProvider.GetRequiredService<ITextCompletionService>();

        // 3. 테스트 콘텐츠
        var testContent = new ExtractedContent
        {
            Text = @"
WebFlux is a powerful SDK for web content processing. It provides multiple chunking strategies.
The SDK supports RAG preprocessing. You can implement custom services easily.
Integration with AI providers is straightforward. The architecture is clean and extensible.
",
            MainContent = "WebFlux SDK example content",
            Url = "https://example.com",
            Title = "Custom Services Example",
            OriginalContentType = "text/plain"
        };

        Console.WriteLine("📄 테스트 콘텐츠:");
        Console.WriteLine(testContent.Text);
        Console.WriteLine();

        // 4. 커스텀 청킹 전략 테스트
        Console.WriteLine("🔧 커스텀 청킹 전략 (문장 기반):\n");

        var chunkingOptions = new ChunkingOptions
        {
            MaxChunkSize = 200,
            MinChunkSize = 50
        };

        var chunks = await chunkingStrategy.ChunkAsync(testContent, chunkingOptions);

        Console.WriteLine($"생성된 청크 수: {chunks.Count}\n");

        int i = 1;
        foreach (var chunk in chunks)
        {
            Console.WriteLine($"청크 {i}:");
            Console.WriteLine($"  내용: {chunk.Content.Trim()}");
            Console.WriteLine($"  크기: {chunk.Content.Length} 문자");
            Console.WriteLine($"  문장 수: {chunk.Metadata.GetValueOrDefault("SentenceCount", 0)}\n");
            i++;
        }

        // 5. 커스텀 AI 서비스 테스트
        Console.WriteLine("🤖 커스텀 AI 서비스 (간단한 요약):\n");

        foreach (var chunk in chunks.Take(2))  // 처음 2개 청크만
        {
            var prompt = $"다음 텍스트를 한 문장으로 요약하세요: {chunk.Content}";
            var summary = await aiService.CompleteAsync(prompt);

            Console.WriteLine($"원본: {chunk.Content.Trim()}");
            Console.WriteLine($"요약: {summary.Trim()}\n");
        }

        // 6. 커스텀 서비스 활용 사례
        Console.WriteLine("💡 커스텀 서비스 활용 사례:\n");
        Console.WriteLine("✅ 문장 기반 청킹: 문장 경계를 엄격히 준수");
        Console.WriteLine("✅ 간단한 AI 서비스: Mock 대신 실제 로직 구현");
        Console.WriteLine("✅ 도메인 특화: 업계/프로젝트 특성에 맞춤");
        Console.WriteLine("✅ 확장성: WebFlux 인터페이스 기반 자유로운 확장\n");

        Console.WriteLine("프로그램 종료. 아무 키나 누르세요...");
        Console.ReadKey();
    }
}

/// <summary>
/// 커스텀 청킹 전략: 문장 기반 청킹
/// 문장 경계를 엄격히 준수하며, MaxChunkSize 내에서 최대한 많은 문장을 포함합니다.
/// </summary>
public class SentenceBasedChunkingStrategy : IChunkingStrategy
{
    public Task<List<WebContentChunk>> ChunkAsync(ExtractedContent content, ChunkingOptions options)
    {
        var chunks = new List<WebContentChunk>();
        var text = content.Text ?? content.MainContent;

        // 문장 분리 (간단한 구현)
        var sentences = SplitIntoSentences(text);

        var currentChunk = new List<string>();
        int currentSize = 0;

        foreach (var sentence in sentences)
        {
            var sentenceLength = sentence.Length;

            // 현재 청크에 추가 가능한지 확인
            if (currentSize + sentenceLength <= options.MaxChunkSize)
            {
                currentChunk.Add(sentence);
                currentSize += sentenceLength;
            }
            else
            {
                // 현재 청크 완성
                if (currentChunk.Any())
                {
                    chunks.Add(CreateChunk(currentChunk, content, chunks.Count));
                }

                // 새 청크 시작
                currentChunk = new List<string> { sentence };
                currentSize = sentenceLength;
            }
        }

        // 마지막 청크 추가
        if (currentChunk.Any())
        {
            chunks.Add(CreateChunk(currentChunk, content, chunks.Count));
        }

        return Task.FromResult(chunks);
    }

    private List<string> SplitIntoSentences(string text)
    {
        // 간단한 문장 분리 (., !, ? 기준)
        var sentences = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            current.Append(ch);

            if (ch == '.' || ch == '!' || ch == '?')
            {
                var sentence = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    sentences.Add(sentence);
                }
                current.Clear();
            }
        }

        // 남은 텍스트 처리
        if (current.Length > 0)
        {
            var sentence = current.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                sentences.Add(sentence);
            }
        }

        return sentences;
    }

    private WebContentChunk CreateChunk(List<string> sentences, ExtractedContent source, int index)
    {
        var content = string.Join(" ", sentences);

        return new WebContentChunk
        {
            Content = content,
            ChunkIndex = index,
            SourceUrl = source.Url,
            Metadata = new Dictionary<string, object>
            {
                ["SentenceCount"] = sentences.Count,
                ["Strategy"] = "SentenceBased",
                ["Title"] = source.Title ?? ""
            }
        };
    }
}

/// <summary>
/// 커스텀 AI 서비스: 간단한 텍스트 완성
/// 실제 AI 모델 대신 규칙 기반 요약을 수행합니다.
/// </summary>
public class SimpleTextCompletionService : ITextCompletionService
{
    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // 간단한 규칙 기반 요약
        var text = ExtractTextFromPrompt(prompt);

        // 첫 번째 문장 추출
        var firstSentence = text.Split('.', '!', '?').FirstOrDefault()?.Trim() ?? text;

        // 키워드 추출 (간단한 구현)
        var keywords = ExtractKeywords(text);

        var summary = $"{firstSentence}. 주요 키워드: {string.Join(", ", keywords.Take(3))}.";

        return Task.FromResult(summary);
    }

    private string ExtractTextFromPrompt(string prompt)
    {
        // "다음 텍스트를 요약하세요: {text}" 형식에서 텍스트 추출
        var parts = prompt.Split(':');
        return parts.Length > 1 ? parts[1].Trim() : prompt;
    }

    private List<string> ExtractKeywords(string text)
    {
        // 간단한 키워드 추출 (불용어 제거 + 빈도 기반)
        var stopWords = new HashSet<string> { "the", "is", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };

        var words = text.ToLower()
            .Split(new[] { ' ', '.', ',', '!', '?', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        return words;
    }
}
