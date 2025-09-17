using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using System.Text.RegularExpressions;
using System.Text;

namespace WebFlux.Services.ChunkingStrategies;

/// <summary>
/// 멀티모달 청킹 전략 (Phase 5A.3)
/// 텍스트와 이미지 콘텐츠를 통합하여 의미적 연결성을 보존하는 청킹
/// 이미지 컨텍스트와 텍스트를 함께 고려하여 최적의 청킹 포인트 결정
/// </summary>
public class MultimodalChunkingStrategy : BaseChunkingStrategy
{
    private readonly IMultimodalProcessingPipeline _multimodalPipeline;
    private readonly ILogger<MultimodalChunkingStrategy> _logger;
    private readonly List<MultimodalChunkPoint> _chunkPoints = new();

    public override string Name => "Multimodal";
    public override string Description => "텍스트와 이미지를 통합하여 의미적 연결성을 보존하는 청킹 전략";

    public MultimodalChunkingStrategy(
        IEventPublisher eventPublisher,
        IMultimodalProcessingPipeline multimodalPipeline,
        ILogger<MultimodalChunkingStrategy> logger) : base(eventPublisher)
    {
        _multimodalPipeline = multimodalPipeline ?? throw new ArgumentNullException(nameof(multimodalPipeline));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 멀티모달 청킹 실행
    /// </summary>
    protected override async Task<IEnumerable<string>> CreateChunksAsync(
        string text,
        ExtractedContent extractedContent,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting multimodal chunking for content with {ImageCount} images",
            extractedContent?.Images?.Count ?? 0);

        try
        {
            // 1. 멀티모달 처리: 이미지를 텍스트로 변환하고 통합
            var multimodalResult = await _multimodalPipeline.ProcessAsync(
                extractedContent,
                new MultimodalProcessingOptions
                {
                    TextIntegrationStrategy = TextIntegrationStrategy.Interleave,
                    ImageTextFormat = ImageTextFormat.Structured,
                    MaxImages = 15
                },
                cancellationToken);

            // 2. 통합된 텍스트로 멀티모달 청킹 포인트 분석
            AnalyzeMultimodalStructure(multimodalResult, extractedContent);

            // 3. 의미적 연결성을 고려한 청킹
            var chunks = CreateSemanticChunks(multimodalResult.CombinedText, multimodalResult);

            // 4. 청크 품질 최적화
            var optimizedChunks = await OptimizeMultimodalChunks(chunks, multimodalResult, cancellationToken);

            _logger.LogInformation("Multimodal chunking completed: {ChunkCount} chunks created",
                optimizedChunks.Count());

            return optimizedChunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform multimodal chunking, falling back to text-only");

            // 폴백: 텍스트만으로 기본 청킹
            return await CreateTextOnlyChunks(text, cancellationToken);
        }
    }

    /// <summary>
    /// 멀티모달 구조 분석
    /// </summary>
    private void AnalyzeMultimodalStructure(MultimodalProcessingResult result, ExtractedContent extractedContent)
    {
        _chunkPoints.Clear();

        var text = result.CombinedText;
        var lines = text.Split('\n');

        int currentPosition = 0;
        double cumulativeImageDensity = 0.0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineLength = line.Length;

            // 구조적 마커 감지
            var chunkPoint = new MultimodalChunkPoint
            {
                Position = currentPosition,
                LineNumber = i,
                Content = line,
                Type = DetermineContentType(line),
                ImageDensity = CalculateImageDensity(line, result),
                SemanticWeight = CalculateSemanticWeight(line, i, lines),
                ContextualBoundary = IsContextualBoundary(line, i, lines)
            };

            _chunkPoints.Add(chunkPoint);
            currentPosition += lineLength + 1; // +1 for newline
            cumulativeImageDensity += chunkPoint.ImageDensity;
        }

        // 이미지 밀도 정규화
        if (_chunkPoints.Count > 0)
        {
            var avgDensity = cumulativeImageDensity / _chunkPoints.Count;
            foreach (var point in _chunkPoints)
            {
                point.NormalizedImageDensity = point.ImageDensity / Math.Max(avgDensity, 0.1);
            }
        }

        _logger.LogDebug("Analyzed {PointCount} multimodal chunk points", _chunkPoints.Count);
    }

    /// <summary>
    /// 의미적 연결성을 고려한 청킹
    /// </summary>
    private List<string> CreateSemanticChunks(string combinedText, MultimodalProcessingResult result)
    {
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        var currentChunkScore = 0.0;
        var targetChunkSize = GetTargetChunkSize();

        foreach (var point in _chunkPoints)
        {
            var shouldBreak = ShouldBreakChunk(point, currentChunk, currentChunkScore, targetChunkSize);

            if (shouldBreak && currentChunk.Length > 50) // 최소 길이 확보
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
                currentChunkScore = 0.0;
            }

            currentChunk.AppendLine(point.Content);
            currentChunkScore += point.SemanticWeight + point.NormalizedImageDensity;
        }

        // 마지막 청크 추가
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }

    /// <summary>
    /// 청킹 포인트 결정
    /// </summary>
    private bool ShouldBreakChunk(
        MultimodalChunkPoint point,
        StringBuilder currentChunk,
        double currentScore,
        int targetSize)
    {
        // 1. 크기 기반 조건
        if (currentChunk.Length >= targetSize * 1.5) return true;

        // 2. 컨텍스트 경계
        if (point.ContextualBoundary && currentChunk.Length >= targetSize * 0.5) return true;

        // 3. 이미지 밀도가 급격히 변하는 지점
        if (point.NormalizedImageDensity > 2.0 && currentChunk.Length >= targetSize * 0.3) return true;

        // 4. 구조적 마커 (헤더, 섹션 등)
        if (point.Type == ContentType.Header && currentChunk.Length >= targetSize * 0.4) return true;

        // 5. 의미적 가중치가 높은 지점
        if (point.SemanticWeight > 0.8 && currentChunk.Length >= targetSize * 0.6) return true;

        return false;
    }

    /// <summary>
    /// 멀티모달 청크 최적화
    /// </summary>
    private async Task<IEnumerable<string>> OptimizeMultimodalChunks(
        List<string> chunks,
        MultimodalProcessingResult result,
        CancellationToken cancellationToken)
    {
        var optimizedChunks = new List<string>();

        foreach (var chunk in chunks)
        {
            var optimizedChunk = await OptimizeIndividualChunk(chunk, result, cancellationToken);
            optimizedChunks.Add(optimizedChunk);
        }

        return optimizedChunks;
    }

    /// <summary>
    /// 개별 청크 최적화
    /// </summary>
    private async Task<string> OptimizeIndividualChunk(
        string chunk,
        MultimodalProcessingResult result,
        CancellationToken cancellationToken)
    {
        // 1. 이미지 컨텍스트 보강
        var enhancedChunk = EnhanceImageContext(chunk, result);

        // 2. 텍스트 정제
        var cleanedChunk = CleanAndNormalizeText(enhancedChunk);

        // 3. 중복 제거
        var deduplicatedChunk = RemoveDuplicateImageReferences(cleanedChunk);

        return deduplicatedChunk;
    }

    /// <summary>
    /// 이미지 컨텍스트 보강
    /// </summary>
    private string EnhanceImageContext(string chunk, MultimodalProcessingResult result)
    {
        // 이미지 참조를 찾아서 더 풍부한 컨텍스트로 교체
        var imagePattern = @"\[Image:([^\]]+)\]";
        var matches = Regex.Matches(chunk, imagePattern);

        var enhancedChunk = chunk;

        foreach (Match match in matches)
        {
            var imageText = match.Groups[1].Value.Trim();
            var enhancedText = $"[Visual Content: {imageText}]";

            // 관련 이미지 정보가 있으면 추가 컨텍스트 제공
            var relatedImage = result.ProcessedImages
                .FirstOrDefault(img => img.ExtractedText.Contains(imageText) ||
                                      imageText.Contains(img.ExtractedText.Substring(0, Math.Min(20, img.ExtractedText.Length))));

            if (relatedImage != null && relatedImage.Confidence > 0.7)
            {
                enhancedText = $"[High-Quality Visual Content: {imageText}]";
            }

            enhancedChunk = enhancedChunk.Replace(match.Value, enhancedText);
        }

        return enhancedChunk;
    }

    /// <summary>
    /// 텍스트 정제 및 정규화
    /// </summary>
    private string CleanAndNormalizeText(string text)
    {
        // 과도한 공백 제거
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");

        // 이미지 참조 정리
        text = Regex.Replace(text, @"\[Image:\s*\]", "[Empty Image]");

        return text.Trim();
    }

    /// <summary>
    /// 중복 이미지 참조 제거
    /// </summary>
    private string RemoveDuplicateImageReferences(string text)
    {
        var imageRefs = new HashSet<string>();
        var pattern = @"\[(?:Image|Visual Content):[^\]]+\]";

        return Regex.Replace(text, pattern, match =>
        {
            var imageRef = match.Value;
            if (imageRefs.Contains(imageRef))
            {
                return ""; // 중복 제거
            }
            imageRefs.Add(imageRef);
            return imageRef;
        });
    }

    /// <summary>
    /// 콘텐츠 타입 결정
    /// </summary>
    private ContentType DetermineContentType(string line)
    {
        if (Regex.IsMatch(line, @"^#{1,6}\s+")) return ContentType.Header;
        if (Regex.IsMatch(line, @"^\[(?:Image|Visual Content):")) return ContentType.Image;
        if (Regex.IsMatch(line, @"^##\s*Image Content")) return ContentType.ImageSection;
        if (string.IsNullOrWhiteSpace(line)) return ContentType.Separator;
        if (line.Length < 20) return ContentType.ShortText;

        return ContentType.Paragraph;
    }

    /// <summary>
    /// 이미지 밀도 계산
    /// </summary>
    private double CalculateImageDensity(string line, MultimodalProcessingResult result)
    {
        var imageRefs = Regex.Matches(line, @"\[(?:Image|Visual Content):[^\]]+\]").Count;
        if (imageRefs == 0) return 0.0;

        var lineLength = Math.Max(line.Length, 1);
        return (double)imageRefs / lineLength * 100; // 100자당 이미지 참조 수
    }

    /// <summary>
    /// 의미적 가중치 계산
    /// </summary>
    private double CalculateSemanticWeight(string line, int lineIndex, string[] allLines)
    {
        double weight = 0.0;

        // 헤더는 높은 가중치
        if (Regex.IsMatch(line, @"^#{1,6}\s+")) weight += 0.8;

        // 이미지 섹션은 중간 가중치
        if (Regex.IsMatch(line, @"^\[(?:Image|Visual Content):")) weight += 0.6;

        // 첫 번째와 마지막 라인은 높은 가중치
        if (lineIndex == 0 || lineIndex == allLines.Length - 1) weight += 0.4;

        // 긴 문장은 높은 가중치
        if (line.Length > 100) weight += 0.3;

        return Math.Min(weight, 1.0);
    }

    /// <summary>
    /// 컨텍스트 경계 판별
    /// </summary>
    private bool IsContextualBoundary(string line, int lineIndex, string[] allLines)
    {
        // 헤더 다음 라인
        if (lineIndex > 0 && Regex.IsMatch(allLines[lineIndex - 1], @"^#{1,6}\s+")) return true;

        // 이미지 섹션 시작
        if (Regex.IsMatch(line, @"^##\s*Image Content")) return true;

        // 빈 라인 다음의 새로운 콘텐츠
        if (lineIndex > 0 && string.IsNullOrWhiteSpace(allLines[lineIndex - 1]) && !string.IsNullOrWhiteSpace(line))
            return true;

        return false;
    }

    /// <summary>
    /// 대상 청크 크기 결정
    /// </summary>
    private int GetTargetChunkSize()
    {
        // 기본 설정에서 청크 크기 가져오기 (기본값: 512자)
        return CurrentOptions?.MaxChunkSize ?? 512;
    }

    /// <summary>
    /// 텍스트 전용 폴백 청킹
    /// </summary>
    private async Task<IEnumerable<string>> CreateTextOnlyChunks(string text, CancellationToken cancellationToken)
    {
        // 간단한 문단 기반 청킹으로 폴백
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        var targetSize = GetTargetChunkSize();

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > targetSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            currentChunk.AppendLine(paragraph);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}

/// <summary>
/// 멀티모달 청킹 포인트 정보
/// </summary>
public class MultimodalChunkPoint
{
    public int Position { get; set; }
    public int LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public double ImageDensity { get; set; }
    public double NormalizedImageDensity { get; set; }
    public double SemanticWeight { get; set; }
    public bool ContextualBoundary { get; set; }
}

/// <summary>
/// 콘텐츠 타입 열거형
/// </summary>
public enum ContentType
{
    Header,
    Paragraph,
    Image,
    ImageSection,
    Separator,
    ShortText
}