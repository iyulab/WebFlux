using System.Text.Json;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// JSON 콘텐츠 추출기
/// JSON 데이터에서 텍스트 정보 추출
/// </summary>
public class JsonContentExtractor : BaseContentExtractor
{
    public JsonContentExtractor(IEventPublisher eventPublisher) : base(eventPublisher)
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
            var jsonDoc = JsonDocument.Parse(content);
            var extractedText = ExtractTextFromJsonElement(jsonDoc.RootElement);
            return Task.FromResult(extractedText);
        }
        catch
        {
            // JSON 파싱 실패 시 원본 텍스트 반환
            return Task.FromResult(content);
        }
    }

    private string ExtractTextFromJsonElement(JsonElement element)
    {
        var result = new StringBuilder();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    result.AppendLine($"**{property.Name}:**");
                    var valueText = ExtractTextFromJsonElement(property.Value);
                    if (!string.IsNullOrWhiteSpace(valueText))
                    {
                        result.AppendLine(valueText);
                        result.AppendLine();
                    }
                }
                break;

            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    result.AppendLine($"Item {index + 1}:");
                    var itemText = ExtractTextFromJsonElement(item);
                    if (!string.IsNullOrWhiteSpace(itemText))
                    {
                        result.AppendLine(itemText);
                    }
                    index++;
                }
                break;

            case JsonValueKind.String:
                result.Append(element.GetString());
                break;

            case JsonValueKind.Number:
                result.Append(element.GetRawText());
                break;

            case JsonValueKind.True:
                result.Append("true");
                break;

            case JsonValueKind.False:
                result.Append("false");
                break;

            case JsonValueKind.Null:
                result.Append("null");
                break;
        }

        return result.ToString();
    }

    protected override string GetExtractionMethod()
    {
        return "JSON";
    }
}