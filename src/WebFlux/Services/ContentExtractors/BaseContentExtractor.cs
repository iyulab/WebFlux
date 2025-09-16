using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;

namespace WebFlux.Services.ContentExtractors;

/// <summary>
/// 콘텐츠 추출기 기본 구현체
/// 모든 추출기의 공통 기능 제공
/// </summary>
public abstract class BaseContentExtractor : IContentExtractor
{
    protected readonly IEventPublisher _eventPublisher;
    protected ExtractionConfiguration _configuration = new();

    protected BaseContentExtractor(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// 웹 콘텐츠에서 텍스트 추출
    /// </summary>
    /// <param name="webContent">웹 콘텐츠</param>
    /// <param name="configuration">추출 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public virtual async Task<ExtractedContent> ExtractAsync(
        WebContent webContent,
        ExtractionConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (webContent == null)
            throw new ArgumentNullException(nameof(webContent));

        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            await _eventPublisher.PublishAsync(new ContentExtractionStartedEvent
            {
                Url = webContent.Url,
                ContentType = webContent.ContentType,
                ContentLength = webContent.Content.Length,
                Timestamp = startTime
            }, cancellationToken);

            // 콘텐츠 전처리
            var preprocessedContent = await PreprocessContentAsync(webContent.Content, cancellationToken);

            // 실제 추출 로직 (파생 클래스에서 구현)
            var extractedText = await ExtractTextAsync(preprocessedContent, webContent, cancellationToken);

            // 후처리
            var processedText = await PostprocessTextAsync(extractedText, cancellationToken);

            // 메타데이터 추출
            var metadata = await ExtractMetadataAsync(webContent, processedText, cancellationToken);

            var result = new ExtractedContent
            {
                Text = processedText,
                Metadata = metadata,
                OriginalUrl = webContent.Url,
                OriginalContentType = webContent.ContentType,
                ExtractionMethod = GetExtractionMethod(),
                ExtractionTimestamp = DateTimeOffset.UtcNow,
                ProcessingTimeMs = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };

            await _eventPublisher.PublishAsync(new ContentExtractionCompletedEvent
            {
                Url = webContent.Url,
                ExtractedTextLength = processedText.Length,
                ProcessingTimeMs = result.ProcessingTimeMs,
                ExtractionMethod = result.ExtractionMethod,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            await _eventPublisher.PublishAsync(new ContentExtractionFailedEvent
            {
                Url = webContent.Url,
                Error = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// 콘텐츠 전처리 (파생 클래스에서 재정의 가능)
    /// </summary>
    /// <param name="content">원본 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>전처리된 콘텐츠</returns>
    protected virtual Task<string> PreprocessContentAsync(string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(string.Empty);

        // 기본 전처리: 인코딩 정규화, 제어 문자 제거
        var processed = content
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        // 제어 문자 제거 (탭, 개행, 캐리지 리턴 제외)
        processed = Regex.Replace(processed, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

        return Task.FromResult(processed);
    }

    /// <summary>
    /// 텍스트 추출 (파생 클래스에서 구현)
    /// </summary>
    /// <param name="content">전처리된 콘텐츠</param>
    /// <param name="webContent">원본 웹 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 텍스트</returns>
    protected abstract Task<string> ExtractTextAsync(
        string content,
        WebContent webContent,
        CancellationToken cancellationToken);

    /// <summary>
    /// 텍스트 후처리
    /// </summary>
    /// <param name="extractedText">추출된 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>후처리된 텍스트</returns>
    protected virtual Task<string> PostprocessTextAsync(string extractedText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return Task.FromResult(string.Empty);

        var processed = extractedText;

        // 중복 공백 정리
        if (_configuration.NormalizeWhitespace)
        {
            processed = Regex.Replace(processed, @"\s+", " ");
        }

        // 중복 개행 정리
        if (_configuration.NormalizeLineBreaks)
        {
            processed = Regex.Replace(processed, @"\n\s*\n", "\n\n");
        }

        // 앞뒤 공백 제거
        processed = processed.Trim();

        // 최소 텍스트 길이 확인
        if (processed.Length < _configuration.MinTextLength)
        {
            return Task.FromResult(string.Empty);
        }

        // 최대 텍스트 길이 제한
        if (_configuration.MaxTextLength > 0 && processed.Length > _configuration.MaxTextLength)
        {
            processed = processed.Substring(0, _configuration.MaxTextLength);
        }

        return Task.FromResult(processed);
    }

    /// <summary>
    /// 메타데이터 추출
    /// </summary>
    /// <param name="webContent">원본 웹 콘텐츠</param>
    /// <param name="extractedText">추출된 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 메타데이터</returns>
    protected virtual Task<ExtractedMetadata> ExtractMetadataAsync(
        WebContent webContent,
        string extractedText,
        CancellationToken cancellationToken)
    {
        var metadata = new ExtractedMetadata
        {
            Title = webContent.Metadata?.Title ?? ExtractTitleFromContent(extractedText),
            Description = ExtractDescriptionFromContent(extractedText),
            Language = DetectLanguage(extractedText),
            WordCount = CountWords(extractedText),
            CharacterCount = extractedText.Length,
            ReadingTimeMinutes = EstimateReadingTime(extractedText),
            Keywords = ExtractKeywords(extractedText),
            OriginalMetadata = webContent.Metadata?.AdditionalData ?? new Dictionary<string, object>()
        };

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// 추출 방법 이름 반환
    /// </summary>
    /// <returns>추출 방법</returns>
    protected abstract string GetExtractionMethod();

    /// <summary>
    /// 콘텐츠에서 제목 추출
    /// </summary>
    /// <param name="content">콘텐츠</param>
    /// <returns>제목</returns>
    protected virtual string ExtractTitleFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Untitled";

        // 첫 번째 줄을 제목으로 사용 (최대 100자)
        var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(firstLine))
            return "Untitled";

        return firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine;
    }

    /// <summary>
    /// 콘텐츠에서 설명 추출
    /// </summary>
    /// <param name="content">콘텐츠</param>
    /// <returns>설명</returns>
    protected virtual string ExtractDescriptionFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // 첫 번째 문단을 설명으로 사용 (최대 300자)
        var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var description = string.Join(". ", sentences.Take(2)).Trim();

        return description.Length > 300 ? description.Substring(0, 300) + "..." : description;
    }

    /// <summary>
    /// 언어 감지 (간단한 휴리스틱)
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <returns>감지된 언어</returns>
    protected virtual string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "unknown";

        // 간단한 패턴 매칭으로 언어 감지
        var koreanPattern = @"[\uAC00-\uD7A3]";
        var japanesePattern = @"[\u3040-\u309F\u30A0-\u30FF]";
        var chinesePattern = @"[\u4E00-\u9FFF]";

        if (Regex.IsMatch(text, koreanPattern))
            return "ko";
        if (Regex.IsMatch(text, japanesePattern))
            return "ja";
        if (Regex.IsMatch(text, chinesePattern))
            return "zh";

        return "en"; // 기본값
    }

    /// <summary>
    /// 단어 수 계산
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <returns>단어 수</returns>
    protected virtual int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // 공백 기준으로 단어 분리
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 읽기 시간 추정 (분)
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <returns>읽기 시간 (분)</returns>
    protected virtual double EstimateReadingTime(string text)
    {
        var wordCount = CountWords(text);
        // 평균 읽기 속도: 200단어/분
        return Math.Max(1, Math.Round(wordCount / 200.0, 1));
    }

    /// <summary>
    /// 키워드 추출 (간단한 빈도 분석)
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <returns>키워드 목록</returns>
    protected virtual List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // 불용어 목록
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i", "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
            "는", "은", "이", "가", "을", "를", "에", "의", "와", "과", "으로", "로", "에서", "까지", "부터", "하다", "있다", "없다", "되다", "아니다"
        };

        // 단어 추출 및 빈도 계산
        var words = Regex.Matches(text.ToLower(), @"\b\w+\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(word => word.Length > 2 && !stopWords.Contains(word))
            .GroupBy(word => word)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public virtual void Dispose()
    {
        // 기본 구현에서는 정리할 리소스 없음
    }
}

/// <summary>
/// 콘텐츠 추출 시작 이벤트
/// </summary>
public class ContentExtractionStartedEvent : ProcessingEvent
{
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int ContentLength { get; set; }
}

/// <summary>
/// 콘텐츠 추출 완료 이벤트
/// </summary>
public class ContentExtractionCompletedEvent : ProcessingEvent
{
    public string Url { get; set; } = string.Empty;
    public int ExtractedTextLength { get; set; }
    public int ProcessingTimeMs { get; set; }
    public string ExtractionMethod { get; set; } = string.Empty;
}

/// <summary>
/// 콘텐츠 추출 실패 이벤트
/// </summary>
public class ContentExtractionFailedEvent : ProcessingEvent
{
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}