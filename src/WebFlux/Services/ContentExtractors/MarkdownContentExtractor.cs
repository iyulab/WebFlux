using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;
using System.Text;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// Markdown 콘텐츠 추출기
/// Markdown 형식의 텍스트를 처리하여 읽기 좋은 형태로 바꿀
/// </summary>
public class MarkdownContentExtractor : BaseContentExtractor
{
    public MarkdownContentExtractor(IEventPublisher eventPublisher) : base(eventPublisher)
    {
    }

    protected override Task<string> ExtractTextAsync(
        string content,
        WebContent webContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(string.Empty);

        var processedText = ProcessMarkdown(content);
        return Task.FromResult(processedText);
    }

    private string ProcessMarkdown(string markdown)
    {
        var result = new StringBuilder();
        var lines = markdown.Split('\n');
        bool inCodeBlock = false;
        string codeBlockLanguage = string.Empty;

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd();

            // Code block 처리
            if (trimmedLine.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    inCodeBlock = true;
                    codeBlockLanguage = trimmedLine.Substring(3).Trim();
                    result.AppendLine($"[Code Block - {(string.IsNullOrEmpty(codeBlockLanguage) ? "Plain" : codeBlockLanguage)}]");
                }
                else
                {
                    inCodeBlock = false;
                    result.AppendLine("[End Code Block]");
                    result.AppendLine();
                }
                continue;
            }

            if (inCodeBlock)
            {
                result.AppendLine($"  {trimmedLine}");
                continue;
            }

            // 제목 처리 (# ## ### 등)
            if (trimmedLine.StartsWith("#"))
            {
                var headerLevel = 0;
                while (headerLevel < trimmedLine.Length && trimmedLine[headerLevel] == '#')
                {
                    headerLevel++;
                }

                var headerText = trimmedLine.Substring(headerLevel).Trim();
                result.AppendLine();
                result.AppendLine($"{new string('#', headerLevel)} {headerText}");
                result.AppendLine();
                continue;
            }

            // 목록 처리
            if (Regex.IsMatch(trimmedLine, @"^(\s*)([-*+]|\d+\.)\s+"))
            {
                var match = Regex.Match(trimmedLine, @"^(\s*)([-*+]|\d+\.)\s+(.*)");
                if (match.Success)
                {
                    var indent = match.Groups[1].Value;
                    var marker = match.Groups[2].Value;
                    var text = match.Groups[3].Value;
                    
                    result.AppendLine($"{indent}• {ProcessInlineMarkdown(text)}");
                }
                continue;
            }

            // 인용문 처리
            if (trimmedLine.StartsWith(">"))
            {
                var quoteText = trimmedLine.TrimStart('>', ' ');
                result.AppendLine($"> {ProcessInlineMarkdown(quoteText)}");
                continue;
            }

            // 빈 줄
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                result.AppendLine();
                continue;
            }

            // 일반 텍스트
            result.AppendLine(ProcessInlineMarkdown(trimmedLine));
        }

        return result.ToString();
    }

    private string ProcessInlineMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // **강조** 처리
        text = Regex.Replace(text, @"\*\*(.*?)\*\*", "$1");
        
        // *기울임* 처리
        text = Regex.Replace(text, @"\*(.*?)\*", "$1");
        
        // `코드` 처리
        text = Regex.Replace(text, @"`(.*?)`", "[$1]");
        
        // [링크](주소) 처리
        if (_configuration.IncludeLinkUrls)
        {
            text = Regex.Replace(text, @"\[([^\]]+)\]\(([^\)]+)\)", "$1 ($2)");
        }
        else
        {
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");
        }
        
        // !이미지 처리
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^\)]+\)", "[Image: $1]");
        
        return text;
    }

    protected override Task<ExtractedMetadata> ExtractMetadataAsync(
        WebContent webContent,
        string extractedText,
        CancellationToken cancellationToken)
    {
        var metadata = base.ExtractMetadataAsync(webContent, extractedText, cancellationToken).Result;
        
        AnalyzeMarkdownStructure(webContent.Content, metadata);
        
        return Task.FromResult(metadata);
    }

    private void AnalyzeMarkdownStructure(string markdown, ExtractedMetadata metadata)
    {
        // 제목 구조 분석
        var headingMatches = Regex.Matches(markdown, @"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
        var headingStructure = headingMatches
            .Cast<Match>()
            .Select(m => new
            {
                Level = m.Groups[1].Value.Length,
                Text = m.Groups[2].Value.Trim()
            })
            .ToList();
        
        metadata.OriginalMetadata["heading_structure"] = headingStructure;
        metadata.OriginalMetadata["heading_count"] = headingStructure.Count;
        
        // 링크 개수
        var linkCount = Regex.Matches(markdown, @"\[([^\]]+)\]\(([^\)]+)\)").Count;
        metadata.OriginalMetadata["link_count"] = linkCount;
        
        // 이미지 개수
        var imageCount = Regex.Matches(markdown, @"!\[([^\]]*)\]\([^\)]+\)").Count;
        metadata.OriginalMetadata["image_count"] = imageCount;
        
        // 코드 블록 개수
        var codeBlockCount = Regex.Matches(markdown, @"```[\s\S]*?```").Count;
        metadata.OriginalMetadata["code_block_count"] = codeBlockCount;
        
        // 목록 아이템 개수
        var listItemCount = Regex.Matches(markdown, @"^\s*([-*+]|\d+\.)\s+", RegexOptions.Multiline).Count;
        metadata.OriginalMetadata["list_item_count"] = listItemCount;
    }

    protected override string GetExtractionMethod()
    {
        return "Markdown";
    }
}