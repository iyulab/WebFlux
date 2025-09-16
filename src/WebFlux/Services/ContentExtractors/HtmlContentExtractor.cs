using HtmlAgilityPack;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;
using System.Text;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// HTML 콘텐츠 추출기
/// HTML에서 구조화된 텍스트와 메타데이터 추출
/// </summary>
public class HtmlContentExtractor : BaseContentExtractor
{
    private readonly HashSet<string> _excludedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "nav", "header", "footer", "aside", "noscript",
        "svg", "canvas", "embed", "object", "applet", "iframe"
    };

    private readonly HashSet<string> _blockElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "div", "p", "h1", "h2", "h3", "h4", "h5", "h6", "article", "section",
        "blockquote", "pre", "ul", "ol", "li", "table", "tr", "td", "th"
    };

    public HtmlContentExtractor(IEventPublisher eventPublisher) : base(eventPublisher)
    {
    }

    /// <summary>
    /// HTML에서 텍스트 추출
    /// </summary>
    /// <param name="content">HTML 콘텐츠</param>
    /// <param name="webContent">원본 웹 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 텍스트</returns>
    protected override Task<string> ExtractTextAsync(
        string content,
        WebContent webContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(string.Empty);

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            var extractedText = new StringBuilder();

            // 메인 콘텐츠 영역 식별 및 추출
            var mainContent = FindMainContentArea(doc);
            if (mainContent != null)
            {
                ExtractTextFromNode(mainContent, extractedText);
            }
            else
            {
                // 메인 콘텐츠를 찾지 못한 경우 body 전체에서 추출
                var body = doc.DocumentNode.SelectSingleNode("//body");
                if (body != null)
                {
                    ExtractTextFromNode(body, extractedText);
                }
                else
                {
                    ExtractTextFromNode(doc.DocumentNode, extractedText);
                }
            }

            return Task.FromResult(extractedText.ToString());
        }
        catch (Exception ex)
        {
            // HTML 파싱 실패 시 일반 텍스트 추출로 폴백
            var fallbackText = ExtractTextFromHtmlString(content);
            return Task.FromResult(fallbackText);
        }
    }

    /// <summary>
    /// 메인 콘텐츠 영역 식별
    /// </summary>
    /// <param name="doc">HTML 문서</param>
    /// <returns>메인 콘텐츠 노드</returns>
    private HtmlNode? FindMainContentArea(HtmlDocument doc)
    {
        // 일반적인 메인 콘텐츠 선택자들을 우선순위 순으로 확인
        var selectors = new[]
        {
            "main",
            "[role='main']",
            "article",
            "#main",
            "#content",
            "#main-content",
            ".main",
            ".content",
            ".main-content",
            ".entry-content",
            ".post-content",
            ".article-content"
        };

        foreach (var selector in selectors)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//{selector}");
            if (node != null && GetTextLength(node) > 200) // 최소 텍스트 길이 확인
            {
                return node;
            }
        }

        // 가장 많은 텍스트를 포함한 div 찾기
        var divs = doc.DocumentNode.SelectNodes("//div");
        if (divs != null)
        {
            var bestDiv = divs
                .Where(div => !HasExcludedClass(div) && GetTextLength(div) > 500)
                .OrderByDescending(div => GetTextLength(div))
                .FirstOrDefault();

            if (bestDiv != null)
                return bestDiv;
        }

        return null;
    }

    /// <summary>
    /// 제외해야 할 클래스나 ID를 가진 노드인지 확인
    /// </summary>
    /// <param name="node">HTML 노드</param>
    /// <returns>제외 여부</returns>
    private bool HasExcludedClass(HtmlNode node)
    {
        var classAttr = node.GetAttributeValue("class", "").ToLowerInvariant();
        var idAttr = node.GetAttributeValue("id", "").ToLowerInvariant();

        var excludedPatterns = new[]
        {
            "nav", "menu", "sidebar", "footer", "header", "advertisement", "ads",
            "social", "share", "comment", "popup", "modal", "overlay"
        };

        return excludedPatterns.Any(pattern =>
            classAttr.Contains(pattern) || idAttr.Contains(pattern));
    }

    /// <summary>
    /// 노드의 텍스트 길이 계산
    /// </summary>
    /// <param name="node">HTML 노드</param>
    /// <returns>텍스트 길이</returns>
    private int GetTextLength(HtmlNode node)
    {
        return node.InnerText?.Trim().Length ?? 0;
    }

    /// <summary>
    /// HTML 노드에서 텍스트 추출
    /// </summary>
    /// <param name="node">HTML 노드</param>
    /// <param name="result">결과 StringBuilder</param>
    private void ExtractTextFromNode(HtmlNode node, StringBuilder result)
    {
        if (node == null) return;

        // 제외된 태그는 건너뛰기
        if (_excludedTags.Contains(node.Name))
            return;

        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = HtmlEntity.DeEntitize(node.InnerText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Append(text);
            }
        }
        else if (node.NodeType == HtmlNodeType.Element)
        {
            // 특별한 처리가 필요한 태그들
            switch (node.Name.ToLowerInvariant())
            {
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    result.AppendLine();
                    result.AppendLine($"# {HtmlEntity.DeEntitize(node.InnerText).Trim()}");
                    result.AppendLine();
                    return;

                case "p":
                    result.AppendLine();
                    foreach (var child in node.ChildNodes)
                    {
                        ExtractTextFromNode(child, result);
                    }
                    result.AppendLine();
                    return;

                case "br":
                    result.AppendLine();
                    return;

                case "li":
                    result.AppendLine($"• {HtmlEntity.DeEntitize(node.InnerText).Trim()}");
                    return;

                case "blockquote":
                    result.AppendLine();
                    result.AppendLine($"> {HtmlEntity.DeEntitize(node.InnerText).Trim()}");
                    result.AppendLine();
                    return;

                case "table":
                    ExtractTableContent(node, result);
                    return;

                case "a":
                    var linkText = HtmlEntity.DeEntitize(node.InnerText).Trim();
                    var href = node.GetAttributeValue("href", "");
                    if (!string.IsNullOrWhiteSpace(linkText))
                    {
                        if (!string.IsNullOrWhiteSpace(href) && _configuration.IncludeLinkUrls)
                        {
                            result.Append($"{linkText} ({href})");
                        }
                        else
                        {
                            result.Append(linkText);
                        }
                    }
                    return;

                case "img":
                    var alt = node.GetAttributeValue("alt", "");
                    var title = node.GetAttributeValue("title", "");
                    var imgDescription = !string.IsNullOrWhiteSpace(alt) ? alt : title;
                    if (!string.IsNullOrWhiteSpace(imgDescription))
                    {
                        result.Append($"[Image: {imgDescription}] ");
                    }
                    return;
            }

            // 블록 요소인 경우 앞뒤로 개행 추가
            if (_blockElements.Contains(node.Name))
            {
                result.AppendLine();
            }

            // 자식 노드들 처리
            foreach (var child in node.ChildNodes)
            {
                ExtractTextFromNode(child, result);
            }

            // 블록 요소인 경우 뒤에 개행 추가
            if (_blockElements.Contains(node.Name))
            {
                result.AppendLine();
            }
        }
    }

    /// <summary>
    /// 테이블 콘텐츠 추출
    /// </summary>
    /// <param name="tableNode">테이블 노드</param>
    /// <param name="result">결과 StringBuilder</param>
    private void ExtractTableContent(HtmlNode tableNode, StringBuilder result)
    {
        result.AppendLine();
        result.AppendLine("[Table Content]");

        var rows = tableNode.SelectNodes(".//tr");
        if (rows != null)
        {
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td|.//th");
                if (cells != null)
                {
                    var cellTexts = cells.Select(cell => HtmlEntity.DeEntitize(cell.InnerText).Trim());
                    result.AppendLine(string.Join(" | ", cellTexts));
                }
            }
        }

        result.AppendLine();
    }

    /// <summary>
    /// HTML 문자열에서 직접 텍스트 추출 (폴백)
    /// </summary>
    /// <param name="html">HTML 문자열</param>
    /// <returns>추출된 텍스트</returns>
    private string ExtractTextFromHtmlString(string html)
    {
        // HTML 태그 제거
        var withoutTags = Regex.Replace(html, @"<[^>]+>", " ");

        // HTML 엔티티 디코딩
        var decoded = HtmlEntity.DeEntitize(withoutTags);

        // 연속된 공백 정리
        var normalized = Regex.Replace(decoded, @"\s+", " ");

        return normalized.Trim();
    }

    /// <summary>
    /// HTML 메타데이터 추출
    /// </summary>
    /// <param name="webContent">원본 웹 콘텐츠</param>
    /// <param name="extractedText">추출된 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 메타데이터</returns>
    protected override Task<ExtractedMetadata> ExtractMetadataAsync(
        WebContent webContent,
        string extractedText,
        CancellationToken cancellationToken)
    {
        var baseMetadata = base.ExtractMetadataAsync(webContent, extractedText, cancellationToken).Result;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(webContent.Content);

            // HTML 특화 메타데이터 추출
            ExtractHtmlMetadata(doc, baseMetadata);
        }
        catch
        {
            // HTML 파싱 실패 시 기본 메타데이터만 사용
        }

        return Task.FromResult(baseMetadata);
    }

    /// <summary>
    /// HTML 문서에서 메타데이터 추출
    /// </summary>
    /// <param name="doc">HTML 문서</param>
    /// <param name="metadata">메타데이터 객체</param>
    private void ExtractHtmlMetadata(HtmlDocument doc, ExtractedMetadata metadata)
    {
        // 제목 추출 (title 태그 우선)
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        if (titleNode != null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
        {
            metadata.Title = HtmlEntity.DeEntitize(titleNode.InnerText.Trim());
        }

        // 메타 태그에서 정보 추출
        var metaTags = doc.DocumentNode.SelectNodes("//meta");
        if (metaTags != null)
        {
            foreach (var meta in metaTags)
            {
                var name = meta.GetAttributeValue("name", "").ToLowerInvariant();
                var property = meta.GetAttributeValue("property", "").ToLowerInvariant();
                var content = meta.GetAttributeValue("content", "");

                if (string.IsNullOrWhiteSpace(content)) continue;

                switch (name)
                {
                    case "description":
                        metadata.Description = content;
                        break;
                    case "keywords":
                        metadata.Keywords.AddRange(content.Split(',').Select(k => k.Trim()));
                        break;
                    case "author":
                        metadata.Author = content;
                        break;
                    case "language":
                    case "content-language":
                        metadata.Language = content;
                        break;
                }

                // Open Graph 메타데이터
                switch (property)
                {
                    case "og:title":
                        if (string.IsNullOrWhiteSpace(metadata.Title))
                            metadata.Title = content;
                        break;
                    case "og:description":
                        if (string.IsNullOrWhiteSpace(metadata.Description))
                            metadata.Description = content;
                        break;
                    case "og:type":
                        metadata.OriginalMetadata["og:type"] = content;
                        break;
                    case "og:image":
                        metadata.OriginalMetadata["og:image"] = content;
                        break;
                }
            }
        }

        // 구조화된 데이터 추출 (JSON-LD)
        ExtractStructuredData(doc, metadata);

        // 헤딩 구조 분석
        ExtractHeadingStructure(doc, metadata);
    }

    /// <summary>
    /// JSON-LD 구조화된 데이터 추출
    /// </summary>
    /// <param name="doc">HTML 문서</param>
    /// <param name="metadata">메타데이터 객체</param>
    private void ExtractStructuredData(HtmlDocument doc, ExtractedMetadata metadata)
    {
        var jsonLdScripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (jsonLdScripts != null)
        {
            var structuredDataCount = 0;
            foreach (var script in jsonLdScripts)
            {
                if (!string.IsNullOrWhiteSpace(script.InnerText))
                {
                    metadata.OriginalMetadata[$"structured_data_{structuredDataCount++}"] = script.InnerText.Trim();
                }
            }
        }
    }

    /// <summary>
    /// 헤딩 구조 추출
    /// </summary>
    /// <param name="doc">HTML 문서</param>
    /// <param name="metadata">메타데이터 객체</param>
    private void ExtractHeadingStructure(HtmlDocument doc, ExtractedMetadata metadata)
    {
        var headings = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6");
        if (headings != null && headings.Count > 0)
        {
            var headingStructure = headings
                .Select(h => new
                {
                    Level = int.Parse(h.Name.Substring(1)),
                    Text = HtmlEntity.DeEntitize(h.InnerText).Trim()
                })
                .Where(h => !string.IsNullOrWhiteSpace(h.Text))
                .Take(20) // 최대 20개까지
                .ToList();

            metadata.OriginalMetadata["heading_structure"] = headingStructure;
            metadata.OriginalMetadata["heading_count"] = headingStructure.Count;
        }
    }

    /// <summary>
    /// 추출 방법 반환
    /// </summary>
    /// <returns>추출 방법</returns>
    protected override string GetExtractionMethod()
    {
        return "HTML";
    }
}