using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// 일반 텍스트 콘텐츠 추출기
/// 플레인 텍스트에서 구조화된 정보 추출
/// </summary>
public class TextContentExtractor : BaseContentExtractor
{
    public TextContentExtractor(IEventPublisher eventPublisher) : base(eventPublisher)
    {
    }

    /// <summary>
    /// 텍스트에서 콘텐츠 추출
    /// </summary>
    /// <param name="content">텍스트 콘텐츠</param>
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

        var processedText = ProcessPlainText(content);
        return Task.FromResult(processedText);
    }

    /// <summary>
    /// 일반 텍스트 처리
    /// </summary>
    /// <param name="text">원본 텍스트</param>
    /// <returns>처리된 텍스트</returns>
    private string ProcessPlainText(string text)
    {
        // 1. 인코딩 정규화
        text = NormalizeEncoding(text);

        // 2. 구조 인식 및 마크업 추가
        text = AddStructuralMarkup(text);

        // 3. 불필요한 내용 제거
        text = RemoveUnwantedContent(text);

        // 4. 포맷팅 정리
        text = NormalizeFormatting(text);

        return text;
    }

    /// <summary>
    /// 인코딩 정규화
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>정규화된 텍스트</returns>
    private string NormalizeEncoding(string text)
    {
        // 다양한 공백 문자를 일반 공백으로 변환
        text = Regex.Replace(text, @"[\u00A0\u1680\u2000-\u200A\u202F\u205F\u3000]", " ");

        // 다양한 대시 문자를 하이픈으로 변환
        text = Regex.Replace(text, @"[\u2010-\u2015\u2212]", "-");

        // 다양한 따옴표를 일반 따옴표로 변환
        text = Regex.Replace(text, @"[\u2018\u2019]", "'");
        text = Regex.Replace(text, @"[\u201C\u201D]", "\"");

        return text;
    }

    /// <summary>
    /// 구조적 마크업 추가
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>구조화된 텍스트</returns>
    private string AddStructuralMarkup(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.None);
        var result = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
            {
                result.Add(string.Empty);
                continue;
            }

            // 제목 패턴 인식 및 마크업
            var markedLine = IdentifyAndMarkTitle(line, i, lines);
            if (markedLine != line)
            {
                result.Add(markedLine);
                continue;
            }

            // 목록 항목 인식
            markedLine = IdentifyAndMarkListItem(line);
            if (markedLine != line)
            {
                result.Add(markedLine);
                continue;
            }

            // 인용문 인식
            markedLine = IdentifyAndMarkQuote(line);
            if (markedLine != line)
            {
                result.Add(markedLine);
                continue;
            }

            // 일반 텍스트
            result.Add(line);
        }

        return string.Join("\n", result);
    }

    /// <summary>
    /// 제목 식별 및 마크업
    /// </summary>
    /// <param name="line">현재 줄</param>
    /// <param name="lineIndex">줄 인덱스</param>
    /// <param name="allLines">전체 줄 배열</param>
    /// <returns>마크업된 줄</returns>
    private string IdentifyAndMarkTitle(string line, int lineIndex, string[] allLines)
    {
        // 1. 대문자로만 구성된 짧은 줄 (제목 가능성)
        if (line.Length > 5 && line.Length < 100 && line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c)))
        {
            return $"# {line}";
        }

        // 2. 줄 끝에 콜론이 있고 다음 줄이 들여쓰기된 경우
        if (line.EndsWith(":") && lineIndex + 1 < allLines.Length)
        {
            var nextLine = allLines[lineIndex + 1];
            if (!string.IsNullOrWhiteSpace(nextLine) && (nextLine.StartsWith("  ") || nextLine.StartsWith("\t")))
            {
                return $"## {line.TrimEnd(':')}";
            }
        }

        // 3. 숫자나 문자로 시작하는 섹션 번호가 있는 경우
        if (Regex.IsMatch(line, @"^(\d+\.|\d+\)\s+|[A-Z]\.\s+|[IVX]+\.\s+)"))
        {
            return $"### {line}";
        }

        // 4. 다음 줄이 구분선인 경우 (===, ---, ___)
        if (lineIndex + 1 < allLines.Length)
        {
            var nextLine = allLines[lineIndex + 1].Trim();
            if (IsUnderline(nextLine) && nextLine.Length >= line.Length * 0.5)
            {
                return $"# {line}";
            }
        }

        return line;
    }

    /// <summary>
    /// 구분선인지 확인
    /// </summary>
    /// <param name="line">확인할 줄</param>
    /// <returns>구분선 여부</returns>
    private bool IsUnderline(string line)
    {
        if (line.Length < 3) return false;

        var firstChar = line[0];
        return (firstChar == '=' || firstChar == '-' || firstChar == '_') &&
               line.All(c => c == firstChar);
    }

    /// <summary>
    /// 목록 항목 식별 및 마크업
    /// </summary>
    /// <param name="line">현재 줄</param>
    /// <returns>마크업된 줄</returns>
    private string IdentifyAndMarkListItem(string line)
    {
        // 이미 마크다운 목록 형태인 경우
        if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
        {
            return line;
        }

        // 숫자 목록
        if (Regex.IsMatch(line, @"^\d+\.\s+"))
        {
            return line;
        }

        // 기타 목록 패턴들
        if (Regex.IsMatch(line, @"^[\u2022\u2023\u25E6\u2043\u2219]\s+")) // 다양한 불릿 문자
        {
            return $"• {line.Substring(1).TrimStart()}";
        }

        // 괄호 목록 (1), (2), (a), (b)
        if (Regex.IsMatch(line, @"^\([0-9a-zA-Z]+\)\s+"))
        {
            return $"• {Regex.Replace(line, @"^\([0-9a-zA-Z]+\)\s+", "")}";
        }

        return line;
    }

    /// <summary>
    /// 인용문 식별 및 마크업
    /// </summary>
    /// <param name="line">현재 줄</param>
    /// <returns>마크업된 줄</returns>
    private string IdentifyAndMarkQuote(string line)
    {
        // 이미 마크다운 인용 형태인 경우
        if (line.StartsWith("> "))
        {
            return line;
        }

        // 따옴표로 둘러싸인 내용
        if ((line.StartsWith("\"") && line.EndsWith("\"")) ||
            (line.StartsWith("'") && line.EndsWith("'")) ||
            (line.StartsWith(""") && line.EndsWith(""")))
        {
            return $"> {line}";
        }

        // 들여쓰기된 긴 줄 (인용문 가능성)
        if ((line.StartsWith("    ") || line.StartsWith("\t")) && line.Length > 50)
        {
            return $"> {line.TrimStart()}";
        }

        return line;
    }

    /// <summary>
    /// 불필요한 내용 제거
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>정리된 텍스트</returns>
    private string RemoveUnwantedContent(string text)
    {
        // URL만 있는 줄 제거 (구성에 따라)
        if (!_configuration.IncludeLinkUrls)
        {
            text = Regex.Replace(text, @"^https?://\S+$", "", RegexOptions.Multiline);
        }

        // 이메일만 있는 줄 제거
        text = Regex.Replace(text, @"^\S+@\S+\.\S+$", "", RegexOptions.Multiline);

        // 반복되는 특수문자 제거 (구분선이 아닌 경우)
        text = Regex.Replace(text, @"^[^\w\s]{10,}$", "", RegexOptions.Multiline);

        // 과도한 공백 줄 제거
        text = Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");

        return text;
    }

    /// <summary>
    /// 포맷팅 정리
    /// </summary>
    /// <param name="text">입력 텍스트</param>
    /// <returns>정리된 텍스트</returns>
    private string NormalizeFormatting(string text)
    {
        var lines = text.Split('\n');
        var result = new List<string>();
        var inCodeBlock = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 코드 블록 감지
            if (trimmedLine.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                result.Add(line);
                continue;
            }

            // 코드 블록 내부는 건드리지 않음
            if (inCodeBlock)
            {
                result.Add(line);
                continue;
            }

            // 빈 줄
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                result.Add(string.Empty);
                continue;
            }

            // 일반 텍스트 정리
            var cleaned = CleanTextLine(trimmedLine);
            result.Add(cleaned);
        }

        return string.Join("\n", result);
    }

    /// <summary>
    /// 개별 텍스트 줄 정리
    /// </summary>
    /// <param name="line">텍스트 줄</param>
    /// <returns>정리된 줄</returns>
    private string CleanTextLine(string line)
    {
        // 연속된 공백을 하나로
        line = Regex.Replace(line, @"\s+", " ");

        // 문장 부호 주변 공백 정리
        line = Regex.Replace(line, @"\s+([,.!?;:])", "$1");
        line = Regex.Replace(line, @"([.!?])\s*", "$1 ");

        // 괄호 주변 공백 정리
        line = Regex.Replace(line, @"\s+([)])", "$1");
        line = Regex.Replace(line, @"([(])\s+", "$1");

        return line.Trim();
    }

    /// <summary>
    /// 텍스트 메타데이터 추출
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
        var metadata = base.ExtractMetadataAsync(webContent, extractedText, cancellationToken).Result;

        // 텍스트 특화 분석
        AnalyzeTextStructure(extractedText, metadata);
        AnalyzeContentComplexity(extractedText, metadata);

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// 텍스트 구조 분석
    /// </summary>
    /// <param name="text">분석할 텍스트</param>
    /// <param name="metadata">메타데이터 객체</param>
    private void AnalyzeTextStructure(string text, ExtractedMetadata metadata)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 제목 개수 분석
        var headingCount = lines.Count(line => line.TrimStart().StartsWith("#"));
        metadata.OriginalMetadata["heading_count"] = headingCount;

        // 목록 개수 분석
        var listItemCount = lines.Count(line =>
            line.TrimStart().StartsWith("• ") ||
            line.TrimStart().StartsWith("- ") ||
            line.TrimStart().StartsWith("* ") ||
            Regex.IsMatch(line.TrimStart(), @"^\d+\.\s+"));
        metadata.OriginalMetadata["list_item_count"] = listItemCount;

        // 문단 개수 추정
        var paragraphCount = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
        metadata.OriginalMetadata["paragraph_count"] = paragraphCount;

        // 문장 개수 분석
        var sentenceCount = Regex.Matches(text, @"[.!?]+").Count;
        metadata.OriginalMetadata["sentence_count"] = sentenceCount;
    }

    /// <summary>
    /// 콘텐츠 복잡도 분석
    /// </summary>
    /// <param name="text">분석할 텍스트</param>
    /// <param name="metadata">메타데이터 객체</param>
    private void AnalyzeContentComplexity(string text, ExtractedMetadata metadata)
    {
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return;

        // 평균 단어 길이
        var avgWordLength = words.Average(w => w.Length);
        metadata.OriginalMetadata["avg_word_length"] = Math.Round(avgWordLength, 2);

        // 복잡한 단어 비율 (7글자 이상)
        var complexWords = words.Count(w => w.Length >= 7);
        var complexWordRatio = (double)complexWords / words.Length * 100;
        metadata.OriginalMetadata["complex_word_ratio"] = Math.Round(complexWordRatio, 2);

        // 수치 데이터 포함 여부
        var hasNumbers = Regex.IsMatch(text, @"\d+");
        metadata.OriginalMetadata["contains_numbers"] = hasNumbers;

        // URL 포함 여부
        var hasUrls = Regex.IsMatch(text, @"https?://\S+");
        metadata.OriginalMetadata["contains_urls"] = hasUrls;

        // 이메일 포함 여부
        var hasEmails = Regex.IsMatch(text, @"\S+@\S+\.\S+");
        metadata.OriginalMetadata["contains_emails"] = hasEmails;
    }

    /// <summary>
    /// 추출 방법 반환
    /// </summary>
    /// <returns>추출 방법</returns>
    protected override string GetExtractionMethod()
    {
        return "PlainText";
    }
}