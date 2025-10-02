using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.AutoLinks;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Extensions.Citations;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Figures;
using Markdig.Extensions.MediaLinks;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text.RegularExpressions;
using System.Diagnostics;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services;

/// <summary>
/// Markdig 기반 마크다운 구조 분석기
/// 95% 구조 정확도 달성을 목표로 합니다
/// </summary>
public class MarkdownStructureAnalyzer : IMarkdownStructureAnalyzer
{
    private readonly MarkdownPipeline _pipeline;
    private static readonly Regex WordCountRegex = new(@"\b\w+\b", RegexOptions.Compiled);
    private static readonly Regex YamlFrontMatterRegex = new(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Compiled | RegexOptions.Singleline);

    public MarkdownStructureAnalyzer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // 대부분의 확장 기능 활성화
            .Build();
    }

    /// <summary>
    /// 마크다운 콘텐츠에서 구조 정보를 추출합니다
    /// </summary>
    public async Task<MarkdownStructureInfo> AnalyzeStructureAsync(
        string markdownContent,
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Front Matter 추출
            var (content, frontMatter) = ExtractFrontMatter(markdownContent);

            // Markdig로 문서 파싱
            var document = Markdown.Parse(content, _pipeline);

            // 구조 정보 추출
            var structureInfo = await ExtractStructureInfoAsync(document, content, sourceUrl, frontMatter, cancellationToken);

            // 정확도 및 품질 점수 계산
            var accuracy = ValidateStructureAccuracy(structureInfo);
            var quality = AssessQuality(structureInfo);

            // 정확도 및 품질 점수 설정
            return new MarkdownStructureInfo
            {
                SourceUrl = structureInfo.SourceUrl,
                Metadata = structureInfo.Metadata,
                Headings = structureInfo.Headings,
                CodeBlocks = structureInfo.CodeBlocks,
                Links = structureInfo.Links,
                Images = structureInfo.Images,
                Tables = structureInfo.Tables,
                TableOfContents = structureInfo.TableOfContents,
                Statistics = structureInfo.Statistics,
                StructureAccuracy = accuracy,
                QualityScore = quality.OverallScore
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// 마크다운을 HTML로 변환하면서 구조 정보를 유지합니다
    /// </summary>
    public async Task<MarkdownConversionResult> ConvertToHtmlWithStructureAsync(
        string markdownContent,
        MarkdownConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MarkdownConversionOptions();
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);

        try
        {
            // 구조 분석
            var structureInfo = await AnalyzeStructureAsync(markdownContent, "conversion", cancellationToken);

            var parseStart = stopwatch.ElapsedMilliseconds;

            // HTML 변환
            var html = Markdown.ToHtml(markdownContent, _pipeline);

            var conversionTime = stopwatch.ElapsedMilliseconds - parseStart;
            var memoryAfter = GC.GetTotalMemory(false);

            var performance = new ConversionPerformanceInfo
            {
                ParsingTimeMs = parseStart,
                ConversionTimeMs = conversionTime,
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = Math.Max(0, memoryAfter - memoryBefore),
                ProcessedElementCount = CountElements(structureInfo)
            };

            return new MarkdownConversionResult
            {
                Html = html,
                StructureInfo = structureInfo,
                Options = options,
                Performance = performance
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// 마크다운 구조의 정확도를 검증합니다
    /// </summary>
    public double ValidateStructureAccuracy(MarkdownStructureInfo structureInfo)
    {
        var scores = new List<double>();

        // 헤딩 구조 정확도 (30%)
        var headingScore = ValidateHeadingStructure(structureInfo.Headings);
        scores.Add(headingScore * 0.30);

        // 링크 유효성 (20%)
        var linkScore = ValidateLinks(structureInfo.Links);
        scores.Add(linkScore * 0.20);

        // 코드 블록 정확도 (15%)
        var codeScore = ValidateCodeBlocks(structureInfo.CodeBlocks);
        scores.Add(codeScore * 0.15);

        // 테이블 구조 (15%)
        var tableScore = ValidateTables(structureInfo.Tables);
        scores.Add(tableScore * 0.15);

        // 목록 구조 (10%)
        var listScore = ValidateLists(structureInfo.Lists);
        scores.Add(listScore * 0.10);

        // 이미지 정확도 (10%)
        var imageScore = ValidateImages(structureInfo.Images);
        scores.Add(imageScore * 0.10);

        return scores.Sum();
    }

    /// <summary>
    /// 마크다운 콘텐츠의 품질을 평가합니다
    /// </summary>
    public MarkdownQualityAssessment AssessQuality(MarkdownStructureInfo structureInfo)
    {
        var issues = new List<QualityIssue>();
        var recommendations = new List<string>();
        var strengths = new List<string>();

        // 구조 품질 평가
        var structuralQuality = AssessStructuralQuality(structureInfo, issues, recommendations, strengths);

        // 콘텐츠 품질 평가
        var contentQuality = AssessContentQuality(structureInfo, issues, recommendations, strengths);

        // 가독성 평가
        var readability = structureInfo.Statistics.ReadabilityScore;

        // 접근성 평가
        var accessibility = AssessAccessibility(structureInfo, issues, recommendations);

        // SEO 친화성 평가
        var seoFriendliness = AssessSeoFriendliness(structureInfo, issues, recommendations);

        var overallScore = (structuralQuality * 0.3 + contentQuality * 0.25 + readability * 0.2 +
                           accessibility * 0.15 + seoFriendliness * 0.1);

        return new MarkdownQualityAssessment
        {
            OverallScore = overallScore,
            StructuralQuality = structuralQuality,
            ContentQuality = contentQuality,
            ReadabilityScore = readability,
            AccessibilityScore = accessibility,
            SeoFriendliness = seoFriendliness,
            Issues = issues,
            Recommendations = recommendations,
            Strengths = strengths
        };
    }

    // Private helper methods

    private (string content, Dictionary<string, object> frontMatter) ExtractFrontMatter(string markdownContent)
    {
        var frontMatter = new Dictionary<string, object>();
        var content = markdownContent;

        var match = YamlFrontMatterRegex.Match(markdownContent);
        if (match.Success)
        {
            var yamlContent = match.Groups[1].Value;
            content = markdownContent.Substring(match.Length);

            // YAML 파싱은 YamlDotNet 구현에서 처리
            // 여기서는 기본적인 key-value 추출만 수행
            foreach (var line in yamlContent.Split('\n'))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    frontMatter[key] = value;
                }
            }
        }

        return (content, frontMatter);
    }

    private Task<MarkdownStructureInfo> ExtractStructureInfoAsync(
        MarkdownDocument document,
        string content,
        string sourceUrl,
        Dictionary<string, object> frontMatter,
        CancellationToken cancellationToken)
    {
        var lines = content.Split('\n');

        // 각 구조 요소 추출
        var headings = ExtractHeadings(document, lines);
        var toc = GenerateTableOfContents(headings);
        var codeBlocks = ExtractCodeBlocks(document, lines);
        var links = ExtractLinks(document);
        var images = ExtractImages(document);
        var tables = ExtractTables(document, lines);
        var lists = ExtractLists(document, lines);
        var quotes = ExtractQuotes(document, lines);
        var math = ExtractMathExpressions(document);
        var footnotes = ExtractFootnotes(document);
        var embeds = ExtractEmbeds(links);
        var statistics = CalculateStatistics(content, document);
        var metadata = ExtractMetadata(frontMatter, headings, content);

        return Task.FromResult(new MarkdownStructureInfo
        {
            SourceUrl = sourceUrl,
            Metadata = metadata,
            Headings = headings,
            TableOfContents = toc,
            CodeBlocks = codeBlocks,
            Links = links,
            Images = images,
            Tables = tables,
            Lists = lists,
            Quotes = quotes,
            MathExpressions = math,
            Footnotes = footnotes,
            Embeds = embeds,
            Statistics = statistics
        });
    }

    private IReadOnlyList<MarkdownHeading> ExtractHeadings(MarkdownDocument document, string[] lines)
    {
        var headings = new List<MarkdownHeading>();
        var headingStack = new Stack<MarkdownHeading>();

        foreach (var block in document.Descendants<HeadingBlock>())
        {
            var text = block.Inline?.ToString() ?? "";
            var level = block.Level;
            var lineNumber = block.Line + 1;
            var id = GenerateHeadingId(text);

            var heading = new MarkdownHeading
            {
                Level = level,
                Text = text,
                Id = id,
                LineNumber = lineNumber,
                Position = block.Span.Start
            };

            // 헤딩 계층 구조 구성
            while (headingStack.Count > 0 && headingStack.Peek().Level >= level)
            {
                headingStack.Pop();
            }

            if (headingStack.Count > 0)
            {
                var parent = headingStack.Peek();
                var children = parent.Children.ToList();
                children.Add(heading);
                headingStack.Pop();
                headingStack.Push(new MarkdownHeading
                {
                    Level = parent.Level,
                    Text = parent.Text,
                    Id = parent.Id,
                    LineNumber = parent.LineNumber,
                    Children = children
                });
            }

            headingStack.Push(heading);
            headings.Add(heading);
        }

        return headings;
    }

    private TableOfContents GenerateTableOfContents(IReadOnlyList<MarkdownHeading> headings)
    {
        var tocItems = new List<TocItem>();
        var maxDepth = 0;

        foreach (var heading in headings)
        {
            if (heading.Level > maxDepth) maxDepth = heading.Level;

            var tocItem = new TocItem
            {
                Title = heading.Text,
                Link = $"#{heading.Id}",
                Level = heading.Level
            };

            tocItems.Add(tocItem);
        }

        var markdownToc = GenerateTocMarkdown(tocItems);

        return new TableOfContents
        {
            Items = tocItems,
            MaxDepth = maxDepth,
            TotalItems = tocItems.Count,
            MarkdownContent = markdownToc
        };
    }

    private string GenerateTocMarkdown(List<TocItem> items)
    {
        var toc = new System.Text.StringBuilder();
        toc.AppendLine("## Table of Contents");
        toc.AppendLine();

        foreach (var item in items)
        {
            var indent = new string(' ', (item.Level - 1) * 2);
            toc.AppendLine($"{indent}- [{item.Title}]({item.Link})");
        }

        return toc.ToString();
    }

    private IReadOnlyList<MarkdownCodeBlock> ExtractCodeBlocks(MarkdownDocument document, string[] lines)
    {
        var codeBlocks = new List<MarkdownCodeBlock>();

        foreach (var block in document.Descendants<CodeBlock>())
        {
            var isInline = block is not FencedCodeBlock;
            var language = block is FencedCodeBlock fenced ? fenced.Info : null;
            var code = string.Join(Environment.NewLine, block.Lines.Lines.Select(l => l.Slice.ToString()));
            var lineNumber = block.Line + 1;
            var lineCount = block.Lines.Count;

            var codeBlock = new MarkdownCodeBlock
            {
                Code = code,
                Language = language,
                LineNumber = lineNumber,
                LineCount = lineCount,
                IsInline = isInline
            };

            codeBlocks.Add(codeBlock);
        }

        return codeBlocks;
    }

    private IReadOnlyList<MarkdownLink> ExtractLinks(MarkdownDocument document)
    {
        var links = new List<MarkdownLink>();

        foreach (var link in document.Descendants<LinkInline>())
        {
            var linkType = DetermineLinkType(link.Url);

            var markdownLink = new MarkdownLink
            {
                Text = link.ToString() ?? "",
                Url = link.Url ?? "",
                Title = link.Title,
                Type = linkType,
                LineNumber = link.Line + 1
            };

            links.Add(markdownLink);
        }

        return links;
    }

    private IReadOnlyList<MarkdownImage> ExtractImages(MarkdownDocument document)
    {
        var images = new List<MarkdownImage>();

        foreach (var image in document.Descendants<LinkInline>().Where(l => l.IsImage))
        {
            var markdownImage = new MarkdownImage
            {
                AltText = image.ToString() ?? "",
                Url = image.Url ?? "",
                Title = image.Title,
                LineNumber = image.Line + 1
            };

            images.Add(markdownImage);
        }

        return images;
    }

    private IReadOnlyList<MarkdownTable> ExtractTables(MarkdownDocument document, string[] lines)
    {
        var tables = new List<MarkdownTable>();

        foreach (var table in document.Descendants<Table>())
        {
            var headers = new List<string>();
            var rows = new List<List<string>>();
            var alignments = new List<TableColumnAlignment>();

            // 헤더 추출
            if (table.FirstOrDefault() is TableRow headerRow)
            {
                foreach (var cell in headerRow.Descendants<TableCell>())
                {
                    headers.Add(cell.Descendants<LiteralInline>().FirstOrDefault()?.Content.ToString() ?? "");
                }
            }

            // 데이터 행 추출
            foreach (var row in table.Descendants<TableRow>().Skip(1))
            {
                var rowData = new List<string>();
                foreach (var cell in row.Descendants<TableCell>())
                {
                    rowData.Add(cell.Descendants<LiteralInline>().FirstOrDefault()?.Content.ToString() ?? "");
                }
                rows.Add(rowData);
            }

            var markdownTable = new MarkdownTable
            {
                Headers = headers,
                Rows = rows.Select(r => (IReadOnlyList<string>)r).ToArray(),
                ColumnAlignments = alignments,
                LineNumber = table.Line + 1,
                ColumnCount = headers.Count,
                RowCount = rows.Count
            };

            tables.Add(markdownTable);
        }

        return tables;
    }

    private IReadOnlyList<MarkdownList> ExtractLists(MarkdownDocument document, string[] lines)
    {
        var lists = new List<MarkdownList>();

        foreach (var list in document.Descendants<ListBlock>())
        {
            var items = new List<MarkdownListItem>();

            foreach (var listItem in list.Descendants<ListItemBlock>())
            {
                var text = listItem.Descendants<LiteralInline>().FirstOrDefault()?.Content.ToString() ?? "";
                bool? isChecked = null;

                // 작업 목록 확인
                if (listItem.Descendants<TaskList>().Any())
                {
                    isChecked = listItem.Descendants<TaskList>().First().Checked;
                }

                var item = new MarkdownListItem
                {
                    Text = text,
                    IsChecked = isChecked,
                    LineNumber = listItem.Line + 1
                };

                items.Add(item);
            }

            var markdownList = new MarkdownList
            {
                Items = items,
                IsOrdered = list.IsOrdered,
                LineNumber = list.Line + 1,
                MarkerType = list.IsOrdered ? "1." : "*"
            };

            lists.Add(markdownList);
        }

        return lists;
    }

    private IReadOnlyList<MarkdownQuote> ExtractQuotes(MarkdownDocument document, string[] lines)
    {
        var quotes = new List<MarkdownQuote>();

        foreach (var quote in document.Descendants<QuoteBlock>())
        {
            var content = string.Join("\n", quote.Descendants<LiteralInline>().Select(l => l.Content.ToString() ?? ""));

            var markdownQuote = new MarkdownQuote
            {
                Content = content,
                Level = 1, // TODO: 중첩 레벨 계산
                LineNumber = quote.Line + 1,
                LineCount = quote.Count
            };

            quotes.Add(markdownQuote);
        }

        return quotes;
    }

    private IReadOnlyList<MarkdownMath> ExtractMathExpressions(MarkdownDocument document)
    {
        var mathExpressions = new List<MarkdownMath>();

        // Math extension이 활성화된 경우 수식 추출
        foreach (var math in document.Descendants().Where(d => d.GetType().Name.Contains("Math")))
        {
            var expression = math.ToString();
            var isInline = math.GetType().Name.Contains("Inline");

            var markdownMath = new MarkdownMath
            {
                Expression = expression,
                IsInline = isInline,
                LineNumber = math.Line + 1,
                MathType = "LaTeX"
            };

            mathExpressions.Add(markdownMath);
        }

        return mathExpressions;
    }

    private IReadOnlyList<MarkdownFootnote> ExtractFootnotes(MarkdownDocument document)
    {
        var footnotes = new List<MarkdownFootnote>();

        // Footnote extension이 활성화된 경우 각주 추출
        foreach (var footnote in document.Descendants().Where(d => d.GetType().Name.Contains("Footnote")))
        {
            var id = footnote.GetType().GetProperty("Label")?.GetValue(footnote)?.ToString() ?? "";
            var content = footnote.ToString();

            var markdownFootnote = new MarkdownFootnote
            {
                Id = id,
                Content = content,
                ReferenceLineNumber = footnote.Line + 1,
                DefinitionLineNumber = footnote.Line + 1
            };

            footnotes.Add(markdownFootnote);
        }

        return footnotes;
    }

    private IReadOnlyList<MarkdownEmbed> ExtractEmbeds(IReadOnlyList<MarkdownLink> links)
    {
        var embeds = new List<MarkdownEmbed>();

        foreach (var link in links)
        {
            var embedType = DetermineEmbedType(link.Url);
            if (embedType != MarkdownEmbedType.Generic) // Generic이 아닌 경우만 임베드로 처리
            {
                var embed = new MarkdownEmbed
                {
                    Type = embedType,
                    Url = link.Url,
                    Title = link.Text,
                    LineNumber = link.LineNumber
                };

                embeds.Add(embed);
            }
        }

        return embeds;
    }

    private MarkdownStatistics CalculateStatistics(string content, MarkdownDocument document)
    {
        var lines = content.Split('\n');
        var contentLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Count();
        var words = WordCountRegex.Matches(content).Count;
        var characters = content.Length;
        var charactersNoSpaces = content.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "").Length;

        // 문단 수 계산 (빈 줄로 구분)
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

        // 읽기 시간 계산 (평균 250단어/분)
        var readingTime = Math.Max(1, (int)Math.Ceiling(words / 250.0));

        // 복잡도 점수 계산
        var complexity = CalculateComplexityScore(document);

        // 가독성 점수 계산 (간단한 알고리즘)
        var readability = CalculateReadabilityScore(words, characters, paragraphs);

        return new MarkdownStatistics
        {
            TotalLines = lines.Length,
            ContentLines = contentLines,
            WordCount = words,
            CharacterCount = characters,
            CharacterCountNoSpaces = charactersNoSpaces,
            ParagraphCount = paragraphs,
            EstimatedReadingTimeMinutes = readingTime,
            ComplexityScore = complexity,
            ReadabilityScore = readability
        };
    }

    private MarkdownDocumentMetadata ExtractMetadata(Dictionary<string, object> frontMatter, IReadOnlyList<MarkdownHeading> headings, string content)
    {
        var title = frontMatter.GetValueOrDefault("title")?.ToString() ??
                   headings.FirstOrDefault(h => h.Level == 1)?.Text ??
                   "Untitled";

        var author = frontMatter.GetValueOrDefault("author")?.ToString();
        var description = frontMatter.GetValueOrDefault("description")?.ToString();
        var language = frontMatter.GetValueOrDefault("language")?.ToString() ?? "ko";

        // 태그와 카테고리 추출
        var tags = ExtractListFromFrontMatter(frontMatter, "tags");
        var categories = ExtractListFromFrontMatter(frontMatter, "categories");

        // 날짜 파싱
        DateTimeOffset? createdAt = null;
        DateTimeOffset? modifiedAt = null;

        if (frontMatter.GetValueOrDefault("date")?.ToString() is string dateStr)
        {
            DateTimeOffset.TryParse(dateStr, out var date);
            createdAt = date;
        }

        return new MarkdownDocumentMetadata
        {
            Title = title,
            Author = author,
            Description = description,
            Language = language,
            Tags = tags,
            Categories = categories,
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt,
            FrontMatter = frontMatter
        };
    }

    // Validation and Assessment methods

    private double ValidateHeadingStructure(IReadOnlyList<MarkdownHeading> headings)
    {
        if (!headings.Any()) return 0.0;

        var score = 1.0;
        var levels = headings.Select(h => h.Level).ToArray();

        // H1으로 시작하는지 확인
        if (levels[0] != 1)
            score -= 0.2;

        // 레벨 건너뛰기 확인
        for (int i = 1; i < levels.Length; i++)
        {
            if (levels[i] - levels[i - 1] > 1)
                score -= 0.1;
        }

        return Math.Max(0.0, score);
    }

    private double ValidateLinks(IReadOnlyList<MarkdownLink> links)
    {
        if (!links.Any()) return 1.0;

        var validLinks = links.Count(l => !string.IsNullOrEmpty(l.Url) && Uri.IsWellFormedUriString(l.Url, UriKind.RelativeOrAbsolute));
        return (double)validLinks / links.Count;
    }

    private double ValidateCodeBlocks(IReadOnlyList<MarkdownCodeBlock> codeBlocks)
    {
        if (!codeBlocks.Any()) return 1.0;

        var validBlocks = codeBlocks.Count(c => !string.IsNullOrEmpty(c.Code));
        return (double)validBlocks / codeBlocks.Count;
    }

    private double ValidateTables(IReadOnlyList<MarkdownTable> tables)
    {
        if (!tables.Any()) return 1.0;

        var validTables = tables.Count(t => t.Headers.Any() && t.Rows.Any());
        return (double)validTables / tables.Count;
    }

    private double ValidateLists(IReadOnlyList<MarkdownList> lists)
    {
        if (!lists.Any()) return 1.0;

        var validLists = lists.Count(l => l.Items.Any());
        return (double)validLists / lists.Count;
    }

    private double ValidateImages(IReadOnlyList<MarkdownImage> images)
    {
        if (!images.Any()) return 1.0;

        var validImages = images.Count(i => !string.IsNullOrEmpty(i.Url) && !string.IsNullOrEmpty(i.AltText));
        return (double)validImages / images.Count;
    }

    // Helper methods

    private string GenerateHeadingId(string text)
    {
        return text.ToLowerInvariant()
                  .Replace(" ", "-")
                  .Replace("?", "")
                  .Replace("!", "")
                  .Replace(".", "")
                  .Replace(",", "");
    }

    private MarkdownLinkType DetermineLinkType(string? url)
    {
        if (string.IsNullOrEmpty(url)) return MarkdownLinkType.Inline;

        if (url.StartsWith("#")) return MarkdownLinkType.Internal;
        if (url.StartsWith("mailto:")) return MarkdownLinkType.Email;
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) return MarkdownLinkType.AutoLink;

        return MarkdownLinkType.Inline;
    }

    private MarkdownEmbedType DetermineEmbedType(string url)
    {
        if (url.Contains("youtube.com") || url.Contains("youtu.be")) return MarkdownEmbedType.YouTube;
        if (url.Contains("vimeo.com")) return MarkdownEmbedType.Vimeo;
        if (url.Contains("twitter.com") || url.Contains("x.com")) return MarkdownEmbedType.Twitter;
        if (url.Contains("gist.github.com")) return MarkdownEmbedType.GitHubGist;
        if (url.Contains("codepen.io")) return MarkdownEmbedType.CodePen;

        return MarkdownEmbedType.Generic;
    }

    private double CalculateComplexityScore(MarkdownDocument document)
    {
        var score = 0.0;
        var totalElements = document.Descendants().Count();

        if (totalElements == 0) return 0.0;

        // 요소별 가중치
        var headings = document.Descendants<HeadingBlock>().Count();
        var codeBlocks = document.Descendants<CodeBlock>().Count();
        var tables = document.Descendants<Table>().Count();
        var lists = document.Descendants<ListBlock>().Count();
        var links = document.Descendants<LinkInline>().Count();

        score += headings * 0.1;
        score += codeBlocks * 0.2;
        score += tables * 0.3;
        score += lists * 0.1;
        score += links * 0.05;

        return Math.Min(1.0, score / 10.0); // 최대 1.0으로 정규화
    }

    private double CalculateReadabilityScore(int words, int characters, int paragraphs)
    {
        if (words == 0 || paragraphs == 0) return 0.0;

        // 간단한 가독성 공식 (Flesch Reading Ease 기반)
        var avgWordsPerSentence = (double)words / paragraphs;
        var avgSyllablesPerWord = (double)characters / words; // 근사치

        var score = 206.835 - 1.015 * avgWordsPerSentence - 84.6 * avgSyllablesPerWord;

        // 0-1 범위로 정규화
        return Math.Max(0.0, Math.Min(1.0, score / 100.0));
    }

    private int CountElements(MarkdownStructureInfo structureInfo)
    {
        return structureInfo.Headings.Count +
               structureInfo.CodeBlocks.Count +
               structureInfo.Links.Count +
               structureInfo.Images.Count +
               structureInfo.Tables.Count +
               structureInfo.Lists.Count +
               structureInfo.Quotes.Count;
    }

    private IReadOnlyList<string> ExtractListFromFrontMatter(Dictionary<string, object> frontMatter, string key)
    {
        if (frontMatter.GetValueOrDefault(key)?.ToString() is string value)
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim())
                       .ToArray();
        }
        return Array.Empty<string>();
    }

    private double AssessStructuralQuality(MarkdownStructureInfo structureInfo, List<QualityIssue> issues, List<string> recommendations, List<string> strengths)
    {
        var score = 1.0;

        // 헤딩 구조 검사
        if (!structureInfo.Headings.Any())
        {
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.HeadingStructure,
                Severity = QualityIssueSeverity.Warning,
                Description = "문서에 헤딩이 없습니다",
                Solution = "문서 구조를 위해 헤딩을 추가하세요"
            });
            score -= 0.3;
        }
        else
        {
            strengths.Add("명확한 헤딩 구조");
        }

        // 목차 평가
        if (structureInfo.TableOfContents.TotalItems > 3)
        {
            strengths.Add("상세한 목차 구조");
        }

        return Math.Max(0.0, score);
    }

    private double AssessContentQuality(MarkdownStructureInfo structureInfo, List<QualityIssue> issues, List<string> recommendations, List<string> strengths)
    {
        var score = 1.0;

        // 콘텐츠 길이 평가
        if (structureInfo.Statistics.WordCount < 100)
        {
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.Readability,
                Severity = QualityIssueSeverity.Info,
                Description = "콘텐츠가 짧습니다",
                Solution = "더 상세한 내용을 추가하는 것을 고려하세요"
            });
            score -= 0.2;
        }

        // 이미지 alt 텍스트 평가
        var imagesWithoutAlt = structureInfo.Images.Count(i => string.IsNullOrEmpty(i.AltText));
        if (imagesWithoutAlt > 0)
        {
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.Accessibility,
                Severity = QualityIssueSeverity.Warning,
                Description = $"{imagesWithoutAlt}개의 이미지에 alt 텍스트가 없습니다",
                Solution = "모든 이미지에 적절한 alt 텍스트를 추가하세요"
            });
            score -= 0.1 * imagesWithoutAlt / Math.Max(1, structureInfo.Images.Count);
        }

        return Math.Max(0.0, score);
    }

    private double AssessAccessibility(MarkdownStructureInfo structureInfo, List<QualityIssue> issues, List<string> recommendations)
    {
        var score = 1.0;

        // 이미지 접근성
        var totalImages = structureInfo.Images.Count;
        if (totalImages > 0)
        {
            var imagesWithAlt = structureInfo.Images.Count(i => !string.IsNullOrEmpty(i.AltText));
            score *= (double)imagesWithAlt / totalImages;
        }

        // 헤딩 순서 검사
        if (structureInfo.Headings.Any() && structureInfo.Headings.First().Level != 1)
        {
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.Accessibility,
                Severity = QualityIssueSeverity.Warning,
                Description = "첫 번째 헤딩이 H1이 아닙니다",
                Solution = "문서는 H1 헤딩으로 시작해야 합니다"
            });
            score -= 0.2;
        }

        return Math.Max(0.0, score);
    }

    private double AssessSeoFriendliness(MarkdownStructureInfo structureInfo, List<QualityIssue> issues, List<string> recommendations)
    {
        var score = 1.0;

        // 제목 평가
        if (string.IsNullOrEmpty(structureInfo.Metadata.Title))
        {
            issues.Add(new QualityIssue
            {
                Type = QualityIssueType.Seo,
                Severity = QualityIssueSeverity.Warning,
                Description = "문서 제목이 없습니다",
                Solution = "SEO를 위해 명확한 제목을 추가하세요"
            });
            score -= 0.3;
        }

        // 설명 평가
        if (string.IsNullOrEmpty(structureInfo.Metadata.Description))
        {
            recommendations.Add("SEO 개선을 위해 문서 설명을 추가하세요");
            score -= 0.2;
        }

        return Math.Max(0.0, score);
    }
}