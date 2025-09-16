using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Text.RegularExpressions;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 청킹 전략 기본 구현체
/// 모든 청킹 전략의 공통 기능 제공
/// </summary>
public abstract class BaseChunkingStrategy : IChunkingStrategy
{
    protected readonly IEventPublisher _eventPublisher;
    protected ChunkingConfiguration _configuration = new();

    protected BaseChunkingStrategy(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <summary>
    /// 추출된 콘텐츠를 청크로 분할
    /// </summary>
    /// <param name="extractedContent">추출된 콘텐츠</param>
    /// <param name="configuration">청킹 구성</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>청크 목록</returns>
    public virtual async Task<IEnumerable<WebContentChunk>> ChunkAsync(
        ExtractedContent extractedContent,
        ChunkingConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (extractedContent == null)
            throw new ArgumentNullException(nameof(extractedContent));

        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            await _eventPublisher.PublishAsync(new ChunkingStartedEvent
            {
                Url = extractedContent.OriginalUrl,
                TextLength = extractedContent.Text.Length,
                Strategy = GetStrategyName(),
                Timestamp = startTime
            }, cancellationToken);

            // 텍스트 전처리
            var preprocessedText = await PreprocessTextAsync(extractedContent.Text, cancellationToken);

            // 실제 청킹 로직 (파생 클래스에서 구현)
            var rawChunks = await CreateChunksAsync(preprocessedText, extractedContent, cancellationToken);

            // 청크 후처리 및 검증
            var processedChunks = await PostprocessChunksAsync(rawChunks, extractedContent, cancellationToken);

            // 청크에 메타데이터 추가
            var finalChunks = await EnrichChunksAsync(processedChunks, extractedContent, cancellationToken);

            var chunkList = finalChunks.ToList();

            await _eventPublisher.PublishAsync(new ChunkingCompletedEvent
            {
                Url = extractedContent.OriginalUrl,
                ChunkCount = chunkList.Count,
                AverageChunkSize = chunkList.Count > 0 ? (int)chunkList.Average(c => c.Content.Length) : 0,
                ProcessingTimeMs = (int)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds,
                Strategy = GetStrategyName(),
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            return chunkList;
        }
        catch (Exception ex)
        {
            await _eventPublisher.PublishAsync(new ChunkingFailedEvent
            {
                Url = extractedContent.OriginalUrl,
                Error = ex.Message,
                Strategy = GetStrategyName(),
                Timestamp = DateTimeOffset.UtcNow
            }, cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// 텍스트 전처리
    /// </summary>
    /// <param name="text">원본 텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>전처리된 텍스트</returns>
    protected virtual Task<string> PreprocessTextAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(string.Empty);

        var processed = text;

        // 과도한 공백 정리
        if (_configuration.NormalizeWhitespace)
        {
            processed = Regex.Replace(processed, @"\s+", " ");
            processed = Regex.Replace(processed, @"\n\s*\n", "\n\n");
        }

        // 최소 길이 확인
        if (processed.Length < _configuration.MinChunkSize)
        {
            return Task.FromResult(processed);
        }

        return Task.FromResult(processed);
    }

    /// <summary>
    /// 실제 청킹 로직 (파생 클래스에서 구현)
    /// </summary>
    /// <param name="text">전처리된 텍스트</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>원시 청크 목록</returns>
    protected abstract Task<IEnumerable<string>> CreateChunksAsync(
        string text,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken);

    /// <summary>
    /// 청크 후처리 및 검증
    /// </summary>
    /// <param name="rawChunks">원시 청크</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 청크</returns>
    protected virtual Task<IEnumerable<string>> PostprocessChunksAsync(
        IEnumerable<string> rawChunks,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        var processedChunks = rawChunks
            .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
            .Select(chunk => chunk.Trim())
            .Where(chunk => chunk.Length >= _configuration.MinChunkSize)
            .Where(chunk => chunk.Length <= _configuration.MaxChunkSize || _configuration.MaxChunkSize <= 0)
            .ToList();

        // 겹침 처리
        if (_configuration.OverlapSize > 0)
        {
            processedChunks = ApplyOverlap(processedChunks).ToList();
        }

        return Task.FromResult(processedChunks.AsEnumerable());
    }

    /// <summary>
    /// 청크에 메타데이터 추가
    /// </summary>
    /// <param name="chunks">처리된 청크</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>최종 청크</returns>
    protected virtual Task<IEnumerable<WebContentChunk>> EnrichChunksAsync(
        IEnumerable<string> chunks,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        var enrichedChunks = chunks.Select((content, index) => new WebContentChunk
        {
            Id = GenerateChunkId(extractedContent.OriginalUrl, index),
            Content = content,
            Metadata = new WebContentMetadata
            {
                Source = extractedContent.OriginalUrl,
                Title = $"{extractedContent.Metadata?.Title ?? "Document"} - Part {index + 1}",
                CrawledAt = extractedContent.ExtractionTimestamp,
                ContentLength = content.Length,
                AdditionalData = new Dictionary<string, object>
                {
                    ["ChunkIndex"] = index,
                    ["ChunkingStrategy"] = GetStrategyName(),
                    ["OriginalContentType"] = extractedContent.OriginalContentType,
                    ["ExtractionMethod"] = extractedContent.ExtractionMethod,
                    ["WordCount"] = CountWords(content),
                    ["CharacterCount"] = content.Length,
                    ["Language"] = extractedContent.Metadata?.Language ?? "unknown"
                }
            },
            ChunkingStrategy = GetStrategyName(),
            ChunkIndex = index,
            TotalChunks = chunks.Count(),
            CreatedAt = DateTimeOffset.UtcNow
        }).ToList();

        return Task.FromResult(enrichedChunks.AsEnumerable());
    }

    /// <summary>
    /// 청크 간 겹침 적용
    /// </summary>
    /// <param name="chunks">원본 청크 목록</param>
    /// <returns>겹침이 적용된 청크 목록</returns>
    protected virtual IEnumerable<string> ApplyOverlap(IList<string> chunks)
    {
        if (chunks.Count <= 1 || _configuration.OverlapSize <= 0)
            return chunks;

        var overlappedChunks = new List<string>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            // 이전 청크의 끝부분을 현재 청크 앞에 추가
            if (i > 0)
            {
                var previousChunk = chunks[i - 1];
                var overlapText = GetOverlapText(previousChunk, _configuration.OverlapSize, false);
                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    chunk = $"{overlapText}\n\n{chunk}";
                }
            }

            // 다음 청크의 시작부분을 현재 청크 뒤에 추가
            if (i < chunks.Count - 1)
            {
                var nextChunk = chunks[i + 1];
                var overlapText = GetOverlapText(nextChunk, _configuration.OverlapSize, true);
                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    chunk = $"{chunk}\n\n{overlapText}";
                }
            }

            overlappedChunks.Add(chunk);
        }

        return overlappedChunks;
    }

    /// <summary>
    /// 겹침 텍스트 추출
    /// </summary>
    /// <param name="text">대상 텍스트</param>
    /// <param name="size">겹침 크기</param>
    /// <param name="fromStart">시작부분에서 추출할지 여부</param>
    /// <returns>겹침 텍스트</returns>
    protected virtual string GetOverlapText(string text, int size, bool fromStart)
    {
        if (string.IsNullOrWhiteSpace(text) || size <= 0)
            return string.Empty;

        if (text.Length <= size)
            return text;

        if (fromStart)
        {
            var endIndex = Math.Min(size, text.Length);
            var overlap = text.Substring(0, endIndex);

            // 단어 경계에서 자르기
            var lastSpace = overlap.LastIndexOf(' ');
            if (lastSpace > endIndex * 0.7) // 70% 이상인 경우만 단어 경계에서 자르기
            {
                overlap = overlap.Substring(0, lastSpace);
            }

            return overlap;
        }
        else
        {
            var startIndex = Math.Max(0, text.Length - size);
            var overlap = text.Substring(startIndex);

            // 단어 경계에서 자르기
            var firstSpace = overlap.IndexOf(' ');
            if (firstSpace >= 0 && firstSpace < size * 0.3) // 30% 이하인 경우만 단어 경계에서 자르기
            {
                overlap = overlap.Substring(firstSpace + 1);
            }

            return overlap;
        }
    }

    /// <summary>
    /// 청크 ID 생성
    /// </summary>
    /// <param name="url">원본 URL</param>
    /// <param name="index">청크 인덱스</param>
    /// <returns>생성된 청크 ID</returns>
    protected virtual string GenerateChunkId(string url, int index)
    {
        var urlHash = url.GetHashCode().ToString("X8");
        return $"{urlHash}_{GetStrategyName()}_{index:D4}";
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

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 전략 이름 반환 (파생 클래스에서 구현)
    /// </summary>
    /// <returns>전략 이름</returns>
    protected abstract string GetStrategyName();

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public virtual void Dispose()
    {
        // 기본 구현에서는 정리할 리소스 없음
    }
}

/// <summary>
/// 청킹 시작 이벤트
/// </summary>
public class ChunkingStartedEvent : ProcessingEvent
{
    public string Url { get; set; } = string.Empty;
    public int TextLength { get; set; }
    public string Strategy { get; set; } = string.Empty;
}

/// <summary>
/// 청킹 완료 이벤트
/// </summary>
public class ChunkingCompletedEvent : ProcessingEvent
{
    public string Url { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public int AverageChunkSize { get; set; }
    public int ProcessingTimeMs { get; set; }
    public string Strategy { get; set; } = string.Empty;
}

/// <summary>
/// 청킹 실패 이벤트
/// </summary>
public class ChunkingFailedEvent : ProcessingEvent
{
    public string Url { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
}