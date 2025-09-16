using System.Xml;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// XML 콘텐츠 추출기
/// XML 문서에서 구조화된 텍스트 정보 추출
/// </summary>
public class XmlContentExtractor : BaseContentExtractor
{
    private readonly HashSet<string> _excludedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "meta", "link", "comment"
    };

    public XmlContentExtractor(IEventPublisher eventPublisher) : base(eventPublisher)
    {
    }

    protected override Task<string> ExtractTextAsync(
        string content,
        WebContent webContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(string.Empty);

        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(content);

            var result = new StringBuilder();
            ExtractTextFromXmlNode(doc.DocumentElement, result, 0);

            return Task.FromResult(result.ToString());
        }
        catch
        {
            // XML 파싱 실패 시 원본 텍스트 반환
            return Task.FromResult(content);
        }
    }

    private void ExtractTextFromXmlNode(XmlNode? node, StringBuilder result, int depth)
    {
        if (node == null) return;

        // 제외된 요소는 건너뛰기
        if (_excludedElements.Contains(node.Name))
            return;

        switch (node.NodeType)
        {
            case XmlNodeType.Element:
                // 요소 시작
                var indent = new string(' ', depth * 2);
                result.AppendLine($"{indent}**{node.Name}:**");

                // 속성 정보 추가
                if (node.Attributes?.Count > 0)
                {
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (!string.IsNullOrWhiteSpace(attr.Value))
                        {
                            result.AppendLine($"{indent}  {attr.Name}: {attr.Value}");
                        }
                    }
                }

                // 자식 노드들 처리
                foreach (XmlNode child in node.ChildNodes)
                {
                    ExtractTextFromXmlNode(child, result, depth + 1);
                }

                result.AppendLine();
                break;

            case XmlNodeType.Text:
            case XmlNodeType.CDATA:
                var text = node.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var textIndent = new string(' ', depth * 2);
                    result.AppendLine($"{textIndent}{text}");
                }
                break;
        }
    }

    protected override Task<ExtractedMetadata> ExtractMetadataAsync(
        WebContent webContent,
        string extractedText,
        CancellationToken cancellationToken)
    {
        var metadata = base.ExtractMetadataAsync(webContent, extractedText, cancellationToken).Result;

        try
        {
            AnalyzeXmlStructure(webContent.Content, metadata);
        }
        catch
        {
            // XML 분석 실패 시 기본 메타데이터만 사용
        }

        return Task.FromResult(metadata);
    }

    private void AnalyzeXmlStructure(string xml, ExtractedMetadata metadata)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        // 루트 요소 정보
        if (doc.DocumentElement != null)
        {
            metadata.OriginalMetadata["root_element"] = doc.DocumentElement.Name;

            // 네임스페이스 정보
            if (!string.IsNullOrEmpty(doc.DocumentElement.NamespaceURI))
            {
                metadata.OriginalMetadata["namespace"] = doc.DocumentElement.NamespaceURI;
            }
        }

        // 요소 통계
        var elementCounts = new Dictionary<string, int>();
        CountElements(doc.DocumentElement, elementCounts);

        metadata.OriginalMetadata["element_counts"] = elementCounts;
        metadata.OriginalMetadata["total_elements"] = elementCounts.Values.Sum();
        metadata.OriginalMetadata["unique_elements"] = elementCounts.Count;
    }

    private void CountElements(XmlNode? node, Dictionary<string, int> counts)
    {
        if (node?.NodeType == XmlNodeType.Element)
        {
            counts[node.Name] = counts.GetValueOrDefault(node.Name, 0) + 1;

            foreach (XmlNode child in node.ChildNodes)
            {
                CountElements(child, counts);
            }
        }
    }

    protected override string GetExtractionMethod()
    {
        return "XML";
    }
}