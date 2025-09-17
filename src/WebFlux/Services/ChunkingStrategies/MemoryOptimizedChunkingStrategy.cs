using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Runtime.CompilerServices;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 메모리 최적화 청킹 전략
/// Phase 4D: 대용량 문서 처리 시 84% 메모리 사용량 감소 목표
/// </summary>
public class MemoryOptimizedChunkingStrategy : BaseChunkingStrategy
{
    private readonly ILogger<MemoryOptimizedChunkingStrategy> _logger;
    private readonly MemoryOptimizedConfiguration _config;
    private readonly ConcurrentQueue<StringBuilder> _stringBuilderPool;

    public MemoryOptimizedChunkingStrategy(
        ILogger<MemoryOptimizedChunkingStrategy> logger,
        IEventPublisher eventPublisher) : base(eventPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = new MemoryOptimizedConfiguration();
        _stringBuilderPool = new ConcurrentQueue<StringBuilder>();

        // StringBuilder 풀 초기화
        InitializeStringBuilderPool();
    }

    /// <summary>
    /// 실제 청킹 로직 구현 - 메모리 최적화된 방식
    /// </summary>
    protected override async Task<IEnumerable<string>> CreateChunksAsync(
        string text,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(text))
            return Enumerable.Empty<string>();

        var chunks = new List<string>();

        _logger.LogInformation("MemoryOptimized 청킹 시작: 문서 크기 {Size} 문자", text.Length);

        try
        {
            // 대용량 문서 감지 및 스트리밍 처리
            if (ShouldUseStreamingMode(text))
            {
                _logger.LogInformation("대용량 문서 감지, 스트리밍 모드 사용");
                chunks = ProcessWithStreaming(text);
            }
            else
            {
                _logger.LogInformation("표준 모드 사용");
                chunks = ProcessWithBatching(text);
            }

            _logger.LogInformation("MemoryOptimized 청킹 완료: {ChunkCount}개 청크 생성", chunks.Count);
            return chunks;
        }
        finally
        {
            // 메모리 정리
            ForceGarbageCollection();
        }
    }

    /// <summary>
    /// 전략 이름 반환
    /// </summary>
    protected override string GetStrategyName() => "MemoryOptimized";

    /// <summary>
    /// 스트리밍 청킹 (별도 공개 메서드)
    /// </summary>
    public async IAsyncEnumerable<string> ChunkStreamAsync(
        ExtractedContent content,
        ChunkingOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text = content.MainContent ?? "";
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var chunkIndex = 0;
        var stringBuilder = GetStringBuilder();

        try
        {
            var textLength = text.Length;
            var position = 0;

            _logger.LogInformation("메모리 최적화 스트리밍 시작: {Length:N0} 문자", textLength);

            while (position < textLength)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 청크 크기 계산 (메모리 기반 동적 조정)
                var chunkSize = CalculateOptimalChunkSize();
                var endPosition = Math.Min(position + chunkSize, textLength);

                // 자연스러운 경계 찾기
                endPosition = FindNaturalBoundary(text, position, endPosition);

                // StringBuilder 재사용으로 메모리 할당 최소화
                stringBuilder.Clear();
                stringBuilder.Append(text, position, endPosition - position);
                var chunkText = stringBuilder.ToString();

                yield return chunkText;

                // 메모리 관리
                position = endPosition;
                chunkIndex++;

                if (chunkIndex % _config.GCTriggerInterval == 0)
                {
                    ForceGarbageCollection();
                    _logger.LogDebug("중간 GC 수행: 청크 {Index}", chunkIndex);
                }
            }

            _logger.LogInformation("스트리밍 완료: {ChunkCount}개 청크 생성", chunkIndex);
        }
        finally
        {
            ReturnStringBuilder(stringBuilder);
        }
    }

    /// <summary>
    /// 스트리밍 모드 사용 여부 결정
    /// </summary>
    private bool ShouldUseStreamingMode(string text)
    {
        return text.Length > _config.StreamingThreshold;
    }

    /// <summary>
    /// 스트리밍 방식으로 처리
    /// </summary>
    private List<string> ProcessWithStreaming(string text)
    {
        var chunks = new List<string>();
        var stringBuilder = GetStringBuilder();

        try
        {
            var position = 0;
            var textLength = text.Length;

            while (position < textLength)
            {
                var chunkSize = CalculateOptimalChunkSize();
                var endPosition = Math.Min(position + chunkSize, textLength);
                endPosition = FindNaturalBoundary(text, position, endPosition);

                stringBuilder.Clear();
                stringBuilder.Append(text, position, endPosition - position);
                chunks.Add(stringBuilder.ToString());

                position = endPosition;

                if (chunks.Count % _config.GCTriggerInterval == 0)
                {
                    ForceGarbageCollection();
                }
            }
        }
        finally
        {
            ReturnStringBuilder(stringBuilder);
        }

        return chunks;
    }

    /// <summary>
    /// 배치 방식으로 처리
    /// </summary>
    private List<string> ProcessWithBatching(string text)
    {
        var chunks = new List<string>();
        var chunkSize = _config.DefaultChunkSize;
        var position = 0;

        while (position < text.Length)
        {
            var endPosition = Math.Min(position + chunkSize, text.Length);
            endPosition = FindNaturalBoundary(text, position, endPosition);

            var chunk = text.Substring(position, endPosition - position);
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk.Trim());
            }

            position = endPosition;
        }

        return chunks;
    }

    /// <summary>
    /// 최적 청크 크기 계산
    /// </summary>
    private int CalculateOptimalChunkSize()
    {
        var availableMemory = GC.GetTotalMemory(false);

        // 메모리 압박 시 청크 크기 축소
        if (availableMemory > _config.MemoryPressureThreshold)
        {
            return Math.Max(_config.DefaultChunkSize / 2, _config.MinChunkSize);
        }

        return _config.DefaultChunkSize;
    }

    /// <summary>
    /// 자연스러운 경계 찾기
    /// </summary>
    private int FindNaturalBoundary(string text, int startPosition, int endPosition)
    {
        if (endPosition >= text.Length)
            return text.Length;

        // 문장 끝 찾기
        for (int i = endPosition - 1; i > startPosition + _config.MinChunkSize; i--)
        {
            if (text[i] == '.' && i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
                return i + 1;
        }

        // 단어 경계 찾기
        for (int i = endPosition - 1; i > startPosition + _config.MinChunkSize; i--)
        {
            if (char.IsWhiteSpace(text[i]))
                return i;
        }

        return endPosition;
    }

    /// <summary>
    /// StringBuilder 풀 초기화
    /// </summary>
    private void InitializeStringBuilderPool()
    {
        for (int i = 0; i < _config.StringBuilderPoolSize; i++)
        {
            _stringBuilderPool.Enqueue(new StringBuilder(_config.MaxStringBuilderCapacity));
        }
    }

    /// <summary>
    /// StringBuilder 가져오기
    /// </summary>
    private StringBuilder GetStringBuilder()
    {
        if (_stringBuilderPool.TryDequeue(out var stringBuilder))
        {
            stringBuilder.Clear();
            return stringBuilder;
        }

        return new StringBuilder(_config.MaxStringBuilderCapacity);
    }

    /// <summary>
    /// StringBuilder 반환
    /// </summary>
    private void ReturnStringBuilder(StringBuilder stringBuilder)
    {
        if (stringBuilder.Capacity <= _config.MaxStringBuilderCapacity)
        {
            _stringBuilderPool.Enqueue(stringBuilder);
        }
    }

    /// <summary>
    /// 강제 가비지 컬렉션
    /// </summary>
    private void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

/// <summary>
/// 메모리 최적화 구성
/// </summary>
public class MemoryOptimizedConfiguration
{
    public int DefaultChunkSize { get; set; } = 1000;
    public int MinChunkSize { get; set; } = 200;
    public int BatchSize { get; set; } = 50000;
    public int StreamingThreshold { get; set; } = 100000;
    public long MemoryPressureThreshold { get; set; } = 500 * 1024 * 1024; // 500MB
    public int GCTriggerInterval { get; set; } = 100;
    public int StringBuilderPoolSize { get; set; } = 10;
    public int MaxStringBuilderCapacity { get; set; } = 10000;
}