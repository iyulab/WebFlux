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

            // 기본 메타데이터 추출 (HTML 기반)
            var metadata = await ExtractBasicMetadataAsync(webContent, processedText, cancellationToken);

            // 통계 정보 계산
            var wordCount = CountWords(processedText);
            var charCount = processedText.Length;
            var readingTime = EstimateReadingTime(processedText);

            var result = new ExtractedContent
            {
                Text = processedText,
                Metadata = metadata,
                OriginalUrl = webContent.Url,
                OriginalContentType = webContent.ContentType,
                ExtractionMethod = GetExtractionMethod(),
                ExtractionTimestamp = DateTimeOffset.UtcNow,
                ProcessingTimeMs = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds,
                WordCount = wordCount,
                CharacterCount = charCount,
                ReadingTimeMinutes = readingTime
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
    /// 기본 메타데이터 추출 (HTML 기반)
    /// AI 메타데이터 추출이 활성화된 경우 HtmlMetadataSnapshot만 생성
    /// </summary>
    /// <param name="webContent">원본 웹 콘텐츠</param>
    /// <param name="extractedText">추출된 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>기본 메타데이터</returns>
    protected virtual Task<EnrichedMetadata> ExtractBasicMetadataAsync(
        WebContent webContent,
        string extractedText,
        CancellationToken cancellationToken)
    {
        var metadata = new EnrichedMetadata
        {
            Url = webContent.Url,
            Domain = !string.IsNullOrEmpty(webContent.Url) ? new Uri(webContent.Url).Host : string.Empty,
            Title = webContent.Metadata?.Title ?? ExtractTitleFromContent(extractedText),
            Description = ExtractDescriptionFromContent(extractedText),
            Language = DetectLanguage(extractedText),
            Keywords = ExtractKeywords(extractedText).AsReadOnly(),
            Source = MetadataSource.Html,
            ExtractedAt = DateTimeOffset.UtcNow
        };

        // FieldSources 초기화
        metadata.FieldSources["title"] = MetadataSource.Html;
        metadata.FieldSources["description"] = MetadataSource.Html;
        metadata.FieldSources["language"] = MetadataSource.Html;
        metadata.FieldSources["keywords"] = MetadataSource.Html;

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
    /// HTML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="enableMetadataExtraction">AI 메타데이터 추출 활성화 (기본값: false)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public abstract Task<ExtractedContent> ExtractFromHtmlAsync(
        string htmlContent,
        string sourceUrl,
        bool enableMetadataExtraction = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 마크다운 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="markdownContent">마크다운 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public abstract Task<ExtractedContent> ExtractFromMarkdownAsync(
        string markdownContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// JSON 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="jsonContent">JSON 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public abstract Task<ExtractedContent> ExtractFromJsonAsync(
        string jsonContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// XML 콘텐츠에서 텍스트와 메타데이터를 추출합니다.
    /// </summary>
    /// <param name="xmlContent">XML 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public abstract Task<ExtractedContent> ExtractFromXmlAsync(
        string xmlContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 일반 텍스트 콘텐츠를 처리합니다.
    /// </summary>
    /// <param name="textContent">텍스트 콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public abstract Task<ExtractedContent> ExtractFromTextAsync(
        string textContent,
        string sourceUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 콘텐츠 유형을 자동으로 감지하여 추출합니다.
    /// </summary>
    /// <param name="content">콘텐츠</param>
    /// <param name="sourceUrl">원본 URL</param>
    /// <param name="contentType">콘텐츠 타입 힌트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 콘텐츠</returns>
    public virtual async Task<ExtractedContent> ExtractAutoAsync(
        string content,
        string sourceUrl,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        if (string.IsNullOrWhiteSpace(sourceUrl))
            throw new ArgumentException("Source URL cannot be null or empty", nameof(sourceUrl));

        // 콘텐츠 타입에 따른 자동 추출
        if (string.IsNullOrWhiteSpace(contentType))
            return await ExtractFromTextAsync(content, sourceUrl, cancellationToken);

        var ct = contentType.ToLowerInvariant();

        if (ct.Contains("html"))
            return await ExtractFromHtmlAsync(content, sourceUrl, false, cancellationToken);
        if (ct.Contains("json"))
            return await ExtractFromJsonAsync(content, sourceUrl, cancellationToken);
        if (ct.Contains("xml"))
            return await ExtractFromXmlAsync(content, sourceUrl, cancellationToken);
        if (ct.Contains("markdown") || ct.Contains("md"))
            return await ExtractFromMarkdownAsync(content, sourceUrl, cancellationToken);

        return await ExtractFromTextAsync(content, sourceUrl, cancellationToken);
    }

    /// <summary>
    /// 지원하는 콘텐츠 타입 목록을 반환합니다.
    /// </summary>
    /// <returns>지원하는 MIME 타입 목록</returns>
    public virtual IReadOnlyList<string> GetSupportedContentTypes()
    {
        return new List<string>
        {
            "text/html",
            "text/plain",
            "text/markdown",
            "application/json",
            "text/xml",
            "application/xml"
        }.AsReadOnly();
    }

    /// <summary>
    /// 추출 통계를 반환합니다.
    /// </summary>
    /// <returns>추출 통계 정보</returns>
    public virtual ExtractionStatistics GetStatistics()
    {
        return new ExtractionStatistics
        {
            TotalExtractions = 0,
            SuccessfulExtractions = 0,
            FailedExtractions = 0,
            AverageProcessingTimeMs = 0,
            SupportedContentTypes = GetSupportedContentTypes().Count
        };
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
    public override string EventType => "ContentExtractionStarted";
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int ContentLength { get; set; }
}

/// <summary>
/// 콘텐츠 추출 완료 이벤트
/// </summary>
public class ContentExtractionCompletedEvent : ProcessingEvent
{
    public override string EventType => "ContentExtractionCompleted";
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
    public override string EventType => "ContentExtractionFailed";
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}