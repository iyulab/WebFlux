using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 고정 크기 청킹 전략
/// 지정된 크기로 텍스트를 균등하게 분할
/// </summary>
public class FixedSizeChunkingStrategy : BaseChunkingStrategy
{
    public FixedSizeChunkingStrategy(IEventPublisher eventPublisher) : base(eventPublisher)
    {
    }

    /// <summary>
    /// 고정 크기 청킹 수행
    /// </summary>
    /// <param name="text">분할할 텍스트</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>청크 목록</returns>
    protected override Task<IEnumerable<string>> CreateChunksAsync(
        string text,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(Enumerable.Empty<string>());

        var chunks = new List<string>();
        var chunkSize = _configuration.DefaultChunkSize;

        // 텍스트가 청크 크기보다 작은 경우 그대로 반환
        if (text.Length <= chunkSize)
        {
            chunks.Add(text);
            return Task.FromResult(chunks.AsEnumerable());
        }

        int currentPosition = 0;

        while (currentPosition < text.Length)
        {
            var remainingLength = text.Length - currentPosition;
            var actualChunkSize = Math.Min(chunkSize, remainingLength);

            var chunk = text.Substring(currentPosition, actualChunkSize);

            // 단어나 문장 경계에서 자르기 시도
            if (currentPosition + actualChunkSize < text.Length) // 마지막 청크가 아닌 경우
            {
                chunk = AdjustChunkBoundary(text, currentPosition, actualChunkSize);
                actualChunkSize = chunk.Length;
            }

            chunks.Add(chunk.Trim());
            currentPosition += actualChunkSize;

            // 무한 루프 방지
            if (actualChunkSize == 0)
            {
                currentPosition++;
            }
        }

        return Task.FromResult(chunks.AsEnumerable());
    }

    /// <summary>
    /// 청크 경계를 자연스러운 위치로 조정
    /// </summary>
    /// <param name="text">전체 텍스트</param>
    /// <param name="startPosition">시작 위치</param>
    /// <param name="desiredSize">원하는 크기</param>
    /// <returns>조정된 청크</returns>
    private string AdjustChunkBoundary(string text, int startPosition, int desiredSize)
    {
        var endPosition = startPosition + desiredSize;
        var searchRadius = (int)(desiredSize * 0.1); // 10% 범위에서 경계 찾기

        // 1. 문단 경계 찾기 (두 개의 연속된 개행)
        var paragraphBreak = FindBoundary(text, endPosition, searchRadius, "\n\n");
        if (paragraphBreak >= 0)
        {
            return text.Substring(startPosition, paragraphBreak - startPosition + 2);
        }

        // 2. 문장 경계 찾기
        var sentenceBreak = FindBoundary(text, endPosition, searchRadius, new[] { ". ", "! ", "? " });
        if (sentenceBreak >= 0)
        {
            return text.Substring(startPosition, sentenceBreak - startPosition + 1);
        }

        // 3. 개행 경계 찾기
        var lineBreak = FindBoundary(text, endPosition, searchRadius, "\n");
        if (lineBreak >= 0)
        {
            return text.Substring(startPosition, lineBreak - startPosition);
        }

        // 4. 단어 경계 찾기
        var wordBreak = FindWordBoundary(text, endPosition, searchRadius);
        if (wordBreak >= 0)
        {
            return text.Substring(startPosition, wordBreak - startPosition);
        }

        // 5. 기본값: 원래 크기 그대로
        return text.Substring(startPosition, desiredSize);
    }

    /// <summary>
    /// 특정 패턴의 경계 찾기
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="centerPosition">중심 위치</param>
    /// <param name="searchRadius">검색 반경</param>
    /// <param name="pattern">찾을 패턴</param>
    /// <returns>경계 위치 (-1: 못 찾음)</returns>
    private int FindBoundary(string text, int centerPosition, int searchRadius, string pattern)
    {
        return FindBoundary(text, centerPosition, searchRadius, new[] { pattern });
    }

    /// <summary>
    /// 여러 패턴 중 가장 가까운 경계 찾기
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="centerPosition">중심 위치</param>
    /// <param name="searchRadius">검색 반경</param>
    /// <param name="patterns">찾을 패턴들</param>
    /// <returns>경계 위치 (-1: 못 찾음)</returns>
    private int FindBoundary(string text, int centerPosition, int searchRadius, string[] patterns)
    {
        var startSearch = Math.Max(0, centerPosition - searchRadius);
        var endSearch = Math.Min(text.Length, centerPosition + searchRadius);

        var bestPosition = -1;
        var bestDistance = int.MaxValue;

        for (int i = startSearch; i < endSearch; i++)
        {
            foreach (var pattern in patterns)
            {
                if (i + pattern.Length <= text.Length &&
                    text.Substring(i, pattern.Length) == pattern)
                {
                    var distance = Math.Abs(i - centerPosition);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPosition = i;
                    }
                }
            }
        }

        return bestPosition;
    }

    /// <summary>
    /// 단어 경계 찾기
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="centerPosition">중심 위치</param>
    /// <param name="searchRadius">검색 반경</param>
    /// <returns>단어 경계 위치 (-1: 못 찾음)</returns>
    private int FindWordBoundary(string text, int centerPosition, int searchRadius)
    {
        var startSearch = Math.Max(0, centerPosition - searchRadius);
        var endSearch = Math.Min(text.Length, centerPosition + searchRadius);

        var bestPosition = -1;
        var bestDistance = int.MaxValue;

        for (int i = startSearch; i < endSearch; i++)
        {
            if (IsWordBoundary(text, i))
            {
                var distance = Math.Abs(i - centerPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPosition = i;
                }
            }
        }

        return bestPosition;
    }

    /// <summary>
    /// 지정된 위치가 단어 경계인지 확인
    /// </summary>
    /// <param name="text">텍스트</param>
    /// <param name="position">위치</param>
    /// <returns>단어 경계 여부</returns>
    private bool IsWordBoundary(string text, int position)
    {
        if (position <= 0 || position >= text.Length)
            return true;

        var currentChar = text[position];
        var prevChar = text[position - 1];

        // 공백 문자인 경우
        if (char.IsWhiteSpace(currentChar))
            return true;

        // 구두점인 경우
        if (char.IsPunctuation(currentChar) || char.IsSymbol(currentChar))
            return true;

        // 이전 문자가 공백이나 구두점인 경우
        if (char.IsWhiteSpace(prevChar) || char.IsPunctuation(prevChar) || char.IsSymbol(prevChar))
            return true;

        return false;
    }

    /// <summary>
    /// 고정 크기 전략 특화 후처리
    /// </summary>
    /// <param name="rawChunks">원시 청크</param>
    /// <param name="extractedContent">원본 추출 콘텐츠</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>처리된 청크</returns>
    protected override Task<IEnumerable<string>> PostprocessChunksAsync(
        IEnumerable<string> rawChunks,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        var chunks = rawChunks.ToList();

        // 기본 후처리
        var processedChunks = base.PostprocessChunksAsync(chunks, extractedContent, cancellationToken).Result.ToList();

        // 고정 크기 전략 특화 처리

        // 1. 너무 작은 마지막 청크를 이전 청크와 병합
        if (processedChunks.Count > 1)
        {
            var lastChunk = processedChunks.Last();
            var minSizeThreshold = _configuration.DefaultChunkSize * 0.1; // 10% 이하면 병합

            if (lastChunk.Length < minSizeThreshold)
            {
                var secondLastIndex = processedChunks.Count - 2;
                var mergedChunk = processedChunks[secondLastIndex] + "\n\n" + lastChunk;

                // 병합 후 크기가 최대 크기를 초과하지 않는지 확인
                if (_configuration.MaxChunkSize <= 0 || mergedChunk.Length <= _configuration.MaxChunkSize)
                {
                    processedChunks[secondLastIndex] = mergedChunk;
                    processedChunks.RemoveAt(processedChunks.Count - 1);
                }
            }
        }

        // 2. 청크 크기 분포 분석 및 로깅
        if (processedChunks.Any())
        {
            var sizes = processedChunks.Select(c => c.Length).ToList();
            var avgSize = sizes.Average();
            var minSize = sizes.Min();
            var maxSize = sizes.Max();

            // 통계 정보 이벤트 발행
            _ = Task.Run(async () =>
            {
                await _eventPublisher.PublishAsync(new ChunkingSizeAnalysisEvent
                {
                    Url = extractedContent.OriginalUrl,
                    ChunkCount = processedChunks.Count,
                    AverageSize = (int)avgSize,
                    MinSize = minSize,
                    MaxSize = maxSize,
                    TargetSize = _configuration.DefaultChunkSize,
                    Strategy = GetStrategyName(),
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);
            }, cancellationToken);
        }

        return Task.FromResult(processedChunks.AsEnumerable());
    }

    /// <summary>
    /// 전략 이름 반환
    /// </summary>
    /// <returns>전략 이름</returns>
    protected override string GetStrategyName()
    {
        return "FixedSize";
    }
}

/// <summary>
/// 청킹 크기 분석 이벤트
/// </summary>
public class ChunkingSizeAnalysisEvent : ProcessingEvent
{
    public override string EventType => "ChunkingSizeAnalysis";
    public string Url { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public int AverageSize { get; set; }
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public int TargetSize { get; set; }
    public string Strategy { get; set; } = string.Empty;
}