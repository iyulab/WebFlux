using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// DOM 구조 기반 청킹 전략
/// HTML 시맨틱 태그를 활용하여 의미적으로 완결된 청크 생성
/// </summary>
public class DomStructureChunkingStrategy : BaseChunkingStrategy
{
    public DomStructureChunkingStrategy(IEventPublisher? eventPublisher = null)
        : base(eventPublisher)
    {
    }

    /// <inheritdoc />
    public override string Name => "DomStructure";

    /// <inheritdoc />
    public override string Description => "HTML DOM 구조를 보존하여 시맨틱 경계에서 청킹";

    /// <inheritdoc />
    public override async Task<IReadOnlyList<WebContentChunk>> ChunkAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var htmlOptions = GetHtmlChunkingOptions(options);

        // HTML 콘텐츠가 없으면 텍스트 기반 청킹으로 폴백
        if (string.IsNullOrEmpty(content.OriginalHtml))
        {
            return SplitBySize(content.Text, htmlOptions.MaxChunkSize, content.SourceUrl);
        }

        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(content.OriginalHtml), cancellationToken);

        // 주요 콘텐츠 영역 찾기
        var mainContent = FindMainContent(document, htmlOptions);
        if (mainContent == null)
        {
            return SplitBySize(content.Text, htmlOptions.MaxChunkSize, content.SourceUrl);
        }

        // 제외 영역 제거
        RemoveExcludedElements(mainContent, htmlOptions);

        // DOM 구조 기반 청킹
        var chunks = new List<WebContentChunk>();
        var sequenceCounter = new SequenceCounter();
        var currentHeadingPath = new List<string>();

        ProcessNode(
            mainContent,
            chunks,
            htmlOptions,
            content.SourceUrl,
            currentHeadingPath,
            string.Empty,
            sequenceCounter,
            cancellationToken);

        // 작은 청크 병합
        return MergeSmallChunks(chunks, htmlOptions);
    }

    private static HtmlChunkingOptions GetHtmlChunkingOptions(ChunkingOptions? options)
    {
        if (options?.StrategySpecificOptions?.TryGetValue("HtmlChunkingOptions", out var htmlOpts) == true
            && htmlOpts is HtmlChunkingOptions hco)
        {
            return hco;
        }

        return new HtmlChunkingOptions
        {
            MaxChunkSize = options?.MaxChunkSize ?? 1500,
            MinChunkSize = options?.MinChunkSize ?? 100
        };
    }

    private static IElement? FindMainContent(IDocument document, HtmlChunkingOptions options)
    {
        foreach (var selector in options.ContentSelectors)
        {
            var element = document.QuerySelector(selector);
            if (element != null)
                return element;
        }

        // 폴백: body
        return document.Body;
    }

    private static void RemoveExcludedElements(IElement element, HtmlChunkingOptions options)
    {
        foreach (var selector in options.ExcludeSelectors)
        {
            var toRemove = element.QuerySelectorAll(selector).ToList();
            foreach (var el in toRemove)
            {
                el.Remove();
            }
        }
    }

    /// <summary>
    /// 시퀀스 번호 카운터 (ref 대신 사용)
    /// </summary>
    private class SequenceCounter
    {
        public int Value { get; set; }
        public int GetNext() => Value++;
    }

    private void ProcessNode(
        INode node,
        List<WebContentChunk> chunks,
        HtmlChunkingOptions options,
        string sourceUrl,
        List<string> headingPath,
        string domPath,
        SequenceCounter sequenceCounter,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (node is IElement element)
        {
            var tagName = element.TagName.ToLowerInvariant();
            var newDomPath = string.IsNullOrEmpty(domPath)
                ? GetElementPath(element)
                : $"{domPath} > {GetElementPath(element)}";

            // 헤딩 처리
            if (options.HeadingTags.Contains(tagName))
            {
                var headingText = element.TextContent.Trim();
                if (!string.IsNullOrEmpty(headingText))
                {
                    var level = int.Parse(tagName[1].ToString());
                    UpdateHeadingPath(headingPath, headingText, level);
                }
            }

            // 섹션 분리 요소
            if (IsSectionElement(element, options))
            {
                var sectionContent = new StringBuilder();
                CollectTextContent(element, sectionContent, options);

                var text = sectionContent.ToString().Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    var chunk = CreateDomChunk(
                        text,
                        sequenceCounter.GetNext(),
                        sourceUrl,
                        newDomPath,
                        headingPath.ToList());

                    // 큰 섹션은 분할
                    if (text.Length > options.MaxChunkSize)
                    {
                        var subChunks = SplitLargeSection(text, options, sourceUrl, newDomPath, headingPath, sequenceCounter);
                        chunks.AddRange(subChunks);
                    }
                    else
                    {
                        chunks.Add(chunk);
                    }
                }
                return;
            }

            // 특별 처리 요소 (코드, 테이블, 리스트)
            if (options.KeepCodeBlocksTogether && (tagName == "pre" || tagName == "code"))
            {
                var text = element.TextContent.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    var chunk = CreateDomChunk(text, sequenceCounter.GetNext(), sourceUrl, newDomPath, headingPath.ToList(), ChunkType.Code);
                    chunks.Add(chunk);
                }
                return;
            }

            if (options.KeepTablesTogether && tagName == "table")
            {
                var text = ExtractTableText(element);
                if (!string.IsNullOrEmpty(text))
                {
                    var chunk = CreateDomChunk(text, sequenceCounter.GetNext(), sourceUrl, newDomPath, headingPath.ToList(), ChunkType.Table);
                    chunks.Add(chunk);
                }
                return;
            }

            if (options.KeepListsTogether && (tagName == "ul" || tagName == "ol"))
            {
                var text = ExtractListText(element);
                if (!string.IsNullOrEmpty(text))
                {
                    var chunk = CreateDomChunk(text, sequenceCounter.GetNext(), sourceUrl, newDomPath, headingPath.ToList(), ChunkType.List);
                    chunks.Add(chunk);
                }
                return;
            }

            // 자식 노드 재귀 처리
            foreach (var child in element.ChildNodes)
            {
                ProcessNode(child, chunks, options, sourceUrl, headingPath, newDomPath, sequenceCounter, cancellationToken);
            }
        }
        else if (node is IText textNode)
        {
            var text = textNode.TextContent.Trim();
            if (!string.IsNullOrEmpty(text) && text.Length >= options.MinChunkSize)
            {
                var chunk = CreateDomChunk(text, sequenceCounter.GetNext(), sourceUrl, domPath, headingPath.ToList());
                chunks.Add(chunk);
            }
        }
    }

    private WebContentChunk CreateDomChunk(
        string text,
        int sequenceNumber,
        string sourceUrl,
        string domPath,
        List<string> headingPath,
        ChunkType chunkType = ChunkType.Text)
    {
        return new WebContentChunk
        {
            Id = Guid.NewGuid().ToString(),
            Content = text,
            SequenceNumber = sequenceNumber,
            SourceUrl = sourceUrl,
            HeadingPath = headingPath,
            SectionTitle = headingPath.Count > 0 ? headingPath[^1] : null,
            StrategyInfo = new ChunkingStrategyInfo
            {
                StrategyName = Name,
                ProcessingTimeMs = 0,
                Parameters = new Dictionary<string, object>
                {
                    ["DomPath"] = domPath
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            Type = chunkType
        };
    }

    private static string GetElementPath(IElement element)
    {
        var tag = element.TagName.ToLowerInvariant();

        if (!string.IsNullOrEmpty(element.Id))
            return $"{tag}#{element.Id}";

        var classes = element.ClassList.FirstOrDefault();
        if (!string.IsNullOrEmpty(classes))
            return $"{tag}.{classes}";

        return tag;
    }

    private static void UpdateHeadingPath(List<string> headingPath, string headingText, int level)
    {
        // 현재 레벨보다 높거나 같은 헤딩 제거
        while (headingPath.Count >= level)
        {
            headingPath.RemoveAt(headingPath.Count - 1);
        }

        headingPath.Add(headingText);
    }

    private static bool IsSectionElement(IElement element, HtmlChunkingOptions options)
    {
        var tagName = element.TagName.ToLowerInvariant();

        foreach (var sectionTag in options.SectionTags)
        {
            if (sectionTag.Contains('.'))
            {
                // div.section 형태
                var parts = sectionTag.Split('.');
                if (tagName == parts[0] && element.ClassList.Contains(parts[1]))
                    return true;
            }
            else if (sectionTag.Contains('['))
            {
                // div[class*='section'] 형태
                if (element.Matches(sectionTag))
                    return true;
            }
            else if (tagName == sectionTag)
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectTextContent(IElement element, StringBuilder builder, HtmlChunkingOptions options)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child is IText textNode)
            {
                var text = textNode.TextContent.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    if (builder.Length > 0)
                        builder.Append(' ');
                    builder.Append(text);
                }
            }
            else if (child is IElement childElement)
            {
                CollectTextContent(childElement, builder, options);
            }
        }
    }

    private static string ExtractTableText(IElement table)
    {
        var rows = table.QuerySelectorAll("tr");
        var lines = new List<string>();

        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("th, td")
                .Select(c => c.TextContent.Trim())
                .ToList();
            if (cells.Count > 0)
                lines.Add(string.Join(" | ", cells));
        }

        return string.Join("\n", lines);
    }

    private static string ExtractListText(IElement list)
    {
        var items = list.QuerySelectorAll("li")
            .Select(li => $"• {li.TextContent.Trim()}")
            .ToList();

        return string.Join("\n", items);
    }

    private List<WebContentChunk> SplitLargeSection(
        string text,
        HtmlChunkingOptions options,
        string sourceUrl,
        string domPath,
        List<string> headingPath,
        SequenceCounter sequenceCounter)
    {
        var chunks = new List<WebContentChunk>();
        var sentences = text.Split(new[] { ". ", ".\n", ".\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length > options.MaxChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(CreateDomChunk(
                    currentChunk.ToString().Trim(),
                    sequenceCounter.GetNext(),
                    sourceUrl,
                    domPath,
                    headingPath.ToList()));
                currentChunk.Clear();
            }

            if (currentChunk.Length > 0)
                currentChunk.Append(". ");
            currentChunk.Append(sentence);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateDomChunk(
                currentChunk.ToString().Trim(),
                sequenceCounter.GetNext(),
                sourceUrl,
                domPath,
                headingPath.ToList()));
        }

        return chunks;
    }

    private static IReadOnlyList<WebContentChunk> MergeSmallChunks(
        List<WebContentChunk> chunks,
        HtmlChunkingOptions options)
    {
        if (chunks.Count <= 1)
            return chunks;

        var merged = new List<WebContentChunk>();
        WebContentChunk? pending = null;

        foreach (var chunk in chunks)
        {
            if (pending == null)
            {
                if (chunk.Content.Length < options.MinChunkSize)
                    pending = chunk;
                else
                    merged.Add(chunk);
            }
            else
            {
                // 이전 작은 청크와 현재 청크 병합
                var mergedContent = $"{pending.Content}\n\n{chunk.Content}";
                var mergedChunk = CloneChunkWithContent(
                    pending,
                    mergedContent,
                    pending.HeadingPath.Count > 0 ? pending.HeadingPath : chunk.HeadingPath);

                if (mergedChunk.Content.Length < options.MinChunkSize && chunk != chunks[^1])
                {
                    pending = mergedChunk;
                }
                else
                {
                    merged.Add(mergedChunk);
                    pending = null;
                }
            }
        }

        if (pending != null)
        {
            if (merged.Count > 0)
            {
                // 마지막 청크와 병합
                var last = merged[^1];
                merged[^1] = CloneChunkWithContent(
                    last,
                    $"{last.Content}\n\n{pending.Content}",
                    last.HeadingPath);
            }
            else
            {
                merged.Add(pending);
            }
        }

        // 시퀀스 번호 재할당
        var result = new List<WebContentChunk>();
        for (int i = 0; i < merged.Count; i++)
        {
            result.Add(CloneChunkWithSequence(merged[i], i));
        }

        return result;
    }

    private static WebContentChunk CloneChunkWithContent(
        WebContentChunk original,
        string newContent,
        IReadOnlyList<string> headingPath)
    {
        return new WebContentChunk
        {
            Id = original.Id,
            Content = newContent,
            Title = original.Title,
            SourceUrl = original.SourceUrl,
            CreatedAt = original.CreatedAt,
            Metadata = original.Metadata,
            AdditionalMetadata = original.AdditionalMetadata,
            SequenceNumber = original.SequenceNumber,
            StrategyInfo = original.StrategyInfo,
            QualityScore = original.QualityScore,
            Type = original.Type,
            ParentChunkId = original.ParentChunkId,
            ChildChunkIds = original.ChildChunkIds,
            RelatedImageUrls = original.RelatedImageUrls,
            Tags = original.Tags,
            HeadingPath = headingPath,
            SectionTitle = original.SectionTitle,
            ContextDependency = original.ContextDependency,
            Source = original.Source
        };
    }

    private static WebContentChunk CloneChunkWithSequence(
        WebContentChunk original,
        int newSequenceNumber)
    {
        return new WebContentChunk
        {
            Id = original.Id,
            Content = original.Content,
            Title = original.Title,
            SourceUrl = original.SourceUrl,
            CreatedAt = original.CreatedAt,
            Metadata = original.Metadata,
            AdditionalMetadata = original.AdditionalMetadata,
            SequenceNumber = newSequenceNumber,
            StrategyInfo = original.StrategyInfo,
            QualityScore = original.QualityScore,
            Type = original.Type,
            ParentChunkId = original.ParentChunkId,
            ChildChunkIds = original.ChildChunkIds,
            RelatedImageUrls = original.RelatedImageUrls,
            Tags = original.Tags,
            HeadingPath = original.HeadingPath,
            SectionTitle = original.SectionTitle,
            ContextDependency = original.ContextDependency,
            Source = original.Source
        };
    }
}
