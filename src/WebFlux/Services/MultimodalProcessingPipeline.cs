using Microsoft.Extensions.Logging;
using WebFlux.Core.Interfaces;
using WebFlux.Core.Models;
using WebFlux.Core.Options;
using System.Diagnostics;

namespace WebFlux.Services;

/// <summary>
/// 멀티모달 처리 파이프라인 (Phase 5A.2)
/// 텍스트와 이미지를 통합하여 처리하고 고품질 RAG용 콘텐츠로 변환
/// </summary>
public class MultimodalProcessingPipeline : IMultimodalProcessingPipeline
{
    private readonly IImageToTextService _imageToTextService;
    private readonly ILogger<MultimodalProcessingPipeline> _logger;
    private readonly MultimodalStatistics _statistics = new();

    public MultimodalProcessingPipeline(
        IImageToTextService imageToTextService,
        ILogger<MultimodalProcessingPipeline> logger)
    {
        _imageToTextService = imageToTextService ?? throw new ArgumentNullException(nameof(imageToTextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 추출된 콘텐츠를 멀티모달 처리하여 통합 텍스트로 변환
    /// </summary>
    public async Task<MultimodalProcessingResult> ProcessAsync(
        ExtractedContent content,
        MultimodalProcessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MultimodalProcessingOptions();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting multimodal processing for content with {ImageCount} images",
            content.Images?.Count ?? 0);

        var result = new MultimodalProcessingResult
        {
            OriginalText = content.Text
        };

        try
        {
            // 이미지가 없으면 원본 텍스트 그대로 반환
            if (content.Images == null || content.Images.Count == 0)
            {
                result.CombinedText = content.Text;
                result.QualityScore = 1.0;
                return result;
            }

            // 이미지 처리 우선순위 결정
            var prioritizedImages = PrioritizeImages(content.Images, options);
            var maxImages = Math.Min(prioritizedImages.Count, options.MaxImages);

            _logger.LogDebug("Processing {MaxImages} prioritized images out of {TotalImages}",
                maxImages, content.Images.Count);

            // 병렬 이미지 처리
            var imageProcessingTasks = new List<Task<ProcessedImageInfo>>();

            for (int i = 0; i < maxImages; i++)
            {
                var image = prioritizedImages[i];
                var task = ProcessImageAsync(image, options.ImageToTextOptions, cancellationToken);
                imageProcessingTasks.Add(task);
            }

            // 모든 이미지 처리 완료 대기
            var processedImages = await Task.WhenAll(imageProcessingTasks);

            // 성공한 이미지들만 필터링
            var successfulImages = processedImages.Where(p => p.IsSuccess).ToList();
            var failedImages = processedImages.Where(p => !p.IsSuccess).ToList();

            result.ProcessedImages = processedImages.ToList();
            result.SuccessfulImages = successfulImages.Count;
            result.FailedImages = failedImages.Count;

            // 이미지 텍스트 추출 및 통합
            result.ImageTexts = successfulImages
                .Where(p => !string.IsNullOrWhiteSpace(p.ExtractedText))
                .Select(p => FormatImageText(p, options))
                .ToList();

            // 텍스트 통합 전략 적용
            result.CombinedText = CombineTextAndImages(
                content.Text,
                result.ImageTexts,
                options.TextIntegrationStrategy);

            // 품질 점수 계산
            result.QualityScore = CalculateQualityScore(result, options);

            _statistics.TotalProcessed++;
            _statistics.ImagesProcessed += result.ProcessedImages.Count;
            _statistics.SuccessfulConversions += result.SuccessfulImages;

            _logger.LogInformation("Multimodal processing completed: {SuccessCount}/{TotalCount} images successful, quality score: {QualityScore:F2}",
                result.SuccessfulImages, result.ProcessedImages.Count, result.QualityScore);
        }
        catch (Exception ex)
        {
            _statistics.ErrorCount++;
            _logger.LogError(ex, "Failed to process multimodal content");

            // 폴백: 원본 텍스트만 반환
            result.CombinedText = content.Text;
            result.QualityScore = 0.5; // 부분적 성공
        }
        finally
        {
            result.TotalProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 개별 이미지 처리
    /// </summary>
    private async Task<ProcessedImageInfo> ProcessImageAsync(
        ImageInfo image,
        ImageToTextOptions? options,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Processing image: {ImageUrl}", image.Url);

            var result = await _imageToTextService.ExtractTextFromWebImageAsync(
                image.Url,
                options,
                cancellationToken);

            return new ProcessedImageInfo
            {
                ImageUrl = image.Url,
                ExtractedText = result.ExtractedText,
                IsSuccess = result.IsSuccess,
                Confidence = result.Confidence,
                ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorMessage = result.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process image {ImageUrl}", image.Url);

            return new ProcessedImageInfo
            {
                ImageUrl = image.Url,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// 이미지 우선순위 결정 (컨텍스트, 크기, 위치 기반)
    /// </summary>
    private List<ImageInfo> PrioritizeImages(IReadOnlyList<ImageInfo> images, MultimodalProcessingOptions options)
    {
        return images
            .Select(img => new { Image = img, Score = CalculateImagePriority(img, options) })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Image)
            .ToList();
    }

    /// <summary>
    /// 이미지 우선순위 점수 계산
    /// </summary>
    private double CalculateImagePriority(ImageInfo image, MultimodalProcessingOptions options)
    {
        double score = 0.0;

        // 1. 컨텍스트 정보가 있으면 높은 점수
        if (!string.IsNullOrWhiteSpace(image.Context))
            score += 0.3;

        // 2. Alt 텍스트가 있으면 점수 추가
        if (!string.IsNullOrWhiteSpace(image.AltText))
            score += 0.2;

        // 3. 페이지 상단에 위치한 이미지 우선
        if (image.Position < 3)
            score += 0.2;

        // 4. 특정 형식 우선 (PNG, JPEG 선호)
        if (image.Format == "PNG" || image.Format == "JPEG")
            score += 0.1;

        // 5. 적절한 크기 (너무 작지 않은 이미지)
        if (image.Dimensions != null && IsAppropriateSize(image.Dimensions))
            score += 0.2;

        return score;
    }

    /// <summary>
    /// 이미지 크기가 적절한지 확인
    /// </summary>
    private bool IsAppropriateSize(string dimensions)
    {
        try
        {
            var parts = dimensions.Split('x');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var width) &&
                int.TryParse(parts[1], out var height))
            {
                // 최소 크기 50x50, 너무 크지 않은 크기
                return width >= 50 && height >= 50 && width <= 2000 && height <= 2000;
            }
        }
        catch { }

        return true; // 크기를 확인할 수 없으면 포함
    }

    /// <summary>
    /// 이미지 텍스트 포맷팅
    /// </summary>
    private string FormatImageText(ProcessedImageInfo imageInfo, MultimodalProcessingOptions options)
    {
        var confidence = imageInfo.Confidence;
        var confidenceLevel = confidence switch
        {
            >= 0.9 => "high",
            >= 0.7 => "medium",
            _ => "low"
        };

        return options.ImageTextFormat switch
        {
            ImageTextFormat.Inline => $"[Image: {imageInfo.ExtractedText}]",
            ImageTextFormat.Annotated => $"[Image (confidence: {confidenceLevel}): {imageInfo.ExtractedText}]",
            ImageTextFormat.Structured => $"\n## Image Content\n{imageInfo.ExtractedText}\n",
            _ => imageInfo.ExtractedText
        };
    }

    /// <summary>
    /// 텍스트와 이미지 텍스트 통합
    /// </summary>
    private string CombineTextAndImages(
        string originalText,
        List<string> imageTexts,
        TextIntegrationStrategy strategy)
    {
        if (imageTexts.Count == 0)
            return originalText;

        return strategy switch
        {
            TextIntegrationStrategy.Append => $"{originalText}\n\n{string.Join("\n", imageTexts)}",
            TextIntegrationStrategy.Interleave => InterleaveTextAndImages(originalText, imageTexts),
            TextIntegrationStrategy.Structured => CreateStructuredContent(originalText, imageTexts),
            _ => $"{originalText}\n\n{string.Join("\n", imageTexts)}"
        };
    }

    /// <summary>
    /// 텍스트와 이미지를 인터리브 방식으로 통합
    /// </summary>
    private string InterleaveTextAndImages(string originalText, List<string> imageTexts)
    {
        // 텍스트를 문단으로 분할하고 이미지 텍스트를 중간중간 삽입
        var paragraphs = originalText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        int imageIndex = 0;
        int imagesPerSection = Math.Max(1, paragraphs.Length / Math.Max(1, imageTexts.Count));

        for (int i = 0; i < paragraphs.Length; i++)
        {
            result.Add(paragraphs[i]);

            // 적절한 간격으로 이미지 텍스트 삽입
            if ((i + 1) % imagesPerSection == 0 && imageIndex < imageTexts.Count)
            {
                result.Add(imageTexts[imageIndex++]);
            }
        }

        // 남은 이미지 텍스트 추가
        while (imageIndex < imageTexts.Count)
        {
            result.Add(imageTexts[imageIndex++]);
        }

        return string.Join("\n\n", result);
    }

    /// <summary>
    /// 구조화된 콘텐츠 생성
    /// </summary>
    private string CreateStructuredContent(string originalText, List<string> imageTexts)
    {
        var result = new List<string>
        {
            "# Content",
            "",
            originalText,
            "",
            "# Visual Content",
            ""
        };

        result.AddRange(imageTexts);

        return string.Join("\n", result);
    }

    /// <summary>
    /// 처리 품질 점수 계산
    /// </summary>
    private double CalculateQualityScore(MultimodalProcessingResult result, MultimodalProcessingOptions options)
    {
        if (result.ProcessedImages.Count == 0)
            return 1.0; // 이미지가 없으면 완전한 점수

        var successRate = (double)result.SuccessfulImages / result.ProcessedImages.Count;
        var avgConfidence = result.ProcessedImages
            .Where(p => p.IsSuccess)
            .Select(p => p.Confidence)
            .DefaultIfEmpty(0.0)
            .Average();

        // 성공률 (50%) + 평균 신뢰도 (50%)
        return (successRate * 0.5) + (avgConfidence * 0.5);
    }

    /// <summary>
    /// 처리 통계 반환
    /// </summary>
    public MultimodalStatistics GetStatistics()
    {
        return _statistics;
    }
}

/// <summary>
/// 멀티모달 처리 통계
/// </summary>
public class MultimodalStatistics
{
    public int TotalProcessed { get; set; }
    public int ImagesProcessed { get; set; }
    public int SuccessfulConversions { get; set; }
    public int ErrorCount { get; set; }
    public double AverageQualityScore { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
}