using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 스마트 청킹 전략 (단순화됨)
/// HTML/Markdown 헤더 구조를 고려한 청킹
/// </summary>
public class SmartChunkingStrategy : BaseChunkingStrategy
{
    public override string Name => "Smart";
    public override string Description => "구조 인식 청킹 - HTML/Markdown 헤더 기반 맥락 보존";

    public SmartChunkingStrategy(IEventPublisher? eventPublisher = null)
        : base(eventPublisher)
    {
    }

    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // 동기 작업을 비동기로 래핑

        var text = content.MainContent ?? content.Text ?? string.Empty;
        var sourceUrl = content.Url ?? content.OriginalUrl ?? string.Empty;
        var maxChunkSize = options?.ChunkSize ?? 1500; // 기본 1500자

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<WebContentChunk>();
        }

        // 헤딩 구조가 있으면 활용, 없으면 문단 기반으로 분할
        if (content.Headings?.Any() == true)
        {
            return SplitByHeadings(text, content.Headings, maxChunkSize, sourceUrl);
        }
        else
        {
            return SplitByStructuralElements(text, maxChunkSize, sourceUrl);
        }
    }

    /// <summary>
    /// 헤딩 구조를 기반으로 분할
    /// </summary>
    private IReadOnlyList<WebContentChunk> SplitByHeadings(string text, List<string> headings, int maxChunkSize, string sourceUrl)
    {
        var chunks = new List<WebContentChunk>();
        var sections = new List<string>();
        var currentSection = string.Empty;
        var sequenceNumber = 0;

        // 간단한 구현: 헤딩을 찾아서 섹션으로 분할
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (IsHeading(line) && currentSection.Length > maxChunkSize)
            {
                if (!string.IsNullOrWhiteSpace(currentSection))
                {
                    chunks.Add(CreateChunk(currentSection.Trim(), sequenceNumber++, sourceUrl));
                    currentSection = string.Empty;
                }
            }
            currentSection += line + "\n";
        }

        // 마지막 섹션 추가
        if (!string.IsNullOrWhiteSpace(currentSection))
        {
            chunks.Add(CreateChunk(currentSection.Trim(), sequenceNumber, sourceUrl));
        }

        return chunks.AsReadOnly();
    }

    /// <summary>
    /// 구조적 요소를 기반으로 분할 (헤딩이 없는 경우)
    /// </summary>
    private IReadOnlyList<WebContentChunk> SplitByStructuralElements(string text, int maxChunkSize, string sourceUrl)
    {
        // 헤딩이 없으면 문단 기반으로 분할
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<WebContentChunk>();
        var currentChunk = new List<string>();
        var currentLength = 0;
        var sequenceNumber = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentLength + paragraph.Length > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(CreateChunk(string.Join("\n\n", currentChunk), sequenceNumber++, sourceUrl));
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(paragraph.Trim());
            currentLength += paragraph.Length + 2;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(CreateChunk(string.Join("\n\n", currentChunk), sequenceNumber, sourceUrl));
        }

        return chunks.AsReadOnly();
    }

    /// <summary>
    /// 라인이 헤딩인지 판단
    /// </summary>
    private bool IsHeading(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("#") || // Markdown 헤딩
               trimmed.StartsWith("<h") || // HTML 헤딩
               (trimmed.Length > 0 && trimmed.Length < 100 && !trimmed.Contains(".")); // 짧은 제목 라인
    }
}