using FluentAssertions;
using WebFlux.Core.Options;

namespace WebFlux.Tests.Core.Options;

/// <summary>
/// 모든 옵션 클래스의 IValidatable 구현 단위 테스트
/// 기본값 검증, 잘못된 값 검증, 교차 속성 제약 조건 검증
/// </summary>
public class OptionsValidationTests
{
    #region ChunkingOptions

    [Fact]
    public void ChunkingOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new ChunkingOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ChunkingOptions_NegativeMaxChunkSize_ShouldFail()
    {
        var options = new ChunkingOptions { MaxChunkSize = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxChunkSize"));
    }

    [Fact]
    public void ChunkingOptions_ZeroMinChunkSize_ShouldFail()
    {
        var options = new ChunkingOptions { MinChunkSize = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MinChunkSize"));
    }

    [Fact]
    public void ChunkingOptions_MaxChunkSizeLessThanMinChunkSize_ShouldFail()
    {
        var options = new ChunkingOptions { MaxChunkSize = 10, MinChunkSize = 20 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxChunkSize") && e.Contains("MinChunkSize"));
    }

    [Fact]
    public void ChunkingOptions_NegativeChunkOverlap_ShouldFail()
    {
        var options = new ChunkingOptions { ChunkOverlap = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ChunkOverlap"));
    }

    [Fact]
    public void ChunkingOptions_ChunkOverlapGreaterThanMaxChunkSize_ShouldFail()
    {
        var options = new ChunkingOptions { MaxChunkSize = 100, ChunkOverlap = 100 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ChunkOverlap") && e.Contains("MaxChunkSize"));
    }

    [Fact]
    public void ChunkingOptions_SemanticThresholdOutOfRange_ShouldFail()
    {
        var options = new ChunkingOptions { SemanticThreshold = 1.5 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SemanticThreshold"));
    }

    [Fact]
    public void ChunkingOptions_QualityThresholdOutOfRange_ShouldFail()
    {
        var options = new ChunkingOptions { QualityThreshold = -0.1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("QualityThreshold"));
    }

    [Fact]
    public void ChunkingOptions_ZeroMaxParallelism_ShouldFail()
    {
        var options = new ChunkingOptions { MaxParallelism = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxParallelism"));
    }

    #endregion

    #region CrawlOptions

    [Fact]
    public void CrawlOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new CrawlOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CrawlOptions_ZeroMaxPages_ShouldFail()
    {
        var options = new CrawlOptions { MaxPages = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxPages"));
    }

    [Fact]
    public void CrawlOptions_NegativeMaxDepth_ShouldFail()
    {
        var options = new CrawlOptions { MaxDepth = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxDepth"));
    }

    [Fact]
    public void CrawlOptions_ZeroConcurrentRequests_ShouldFail()
    {
        var options = new CrawlOptions { ConcurrentRequests = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ConcurrentRequests"));
    }

    [Fact]
    public void CrawlOptions_ZeroTimeoutSeconds_ShouldFail()
    {
        var options = new CrawlOptions { TimeoutSeconds = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
    }

    [Fact]
    public void CrawlOptions_MinConfidenceOutOfRange_ShouldFail()
    {
        var options = new CrawlOptions { MinConfidence = 1.5f };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MinConfidence"));
    }

    [Fact]
    public void CrawlOptions_NegativeMaxRetries_ShouldFail()
    {
        var options = new CrawlOptions { MaxRetries = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    [Fact]
    public void CrawlOptions_NegativeDelayBetweenRequestsMs_ShouldFail()
    {
        var options = new CrawlOptions { DelayBetweenRequestsMs = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DelayBetweenRequestsMs"));
    }

    #endregion

    #region ExtractOptions

    [Fact]
    public void ExtractOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new ExtractOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ExtractOptions_ZeroTimeoutSeconds_ShouldFail()
    {
        var options = new ExtractOptions { TimeoutSeconds = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
    }

    [Fact]
    public void ExtractOptions_ZeroMaxConcurrency_ShouldFail()
    {
        var options = new ExtractOptions { MaxConcurrency = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxConcurrency"));
    }

    [Fact]
    public void ExtractOptions_NegativeMaxRetries_ShouldFail()
    {
        var options = new ExtractOptions { MaxRetries = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    [Fact]
    public void ExtractOptions_ZeroCacheExpirationMinutes_ShouldFail()
    {
        var options = new ExtractOptions { CacheExpirationMinutes = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("CacheExpirationMinutes"));
    }

    [Fact]
    public void ExtractOptions_NegativeDomainMinIntervalMs_ShouldFail()
    {
        var options = new ExtractOptions { DomainMinIntervalMs = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DomainMinIntervalMs"));
    }

    #endregion

    #region AnalysisOptions

    [Fact]
    public void AnalysisOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new AnalysisOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void AnalysisOptions_MinContentQualityOutOfRange_ShouldFail()
    {
        var options = new AnalysisOptions { MinContentQuality = 1.5 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MinContentQuality"));
    }

    [Fact]
    public void AnalysisOptions_ZeroMinSectionLength_ShouldFail()
    {
        var options = new AnalysisOptions { MinSectionLength = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MinSectionLength"));
    }

    [Fact]
    public void AnalysisOptions_ZeroMaxSectionDepth_ShouldFail()
    {
        var options = new AnalysisOptions { MaxSectionDepth = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxSectionDepth"));
    }

    [Fact]
    public void AnalysisOptions_ZeroTimeoutMs_ShouldFail()
    {
        var options = new AnalysisOptions { TimeoutMs = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutMs"));
    }

    #endregion

    #region ReconstructOptions

    [Fact]
    public void ReconstructOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new ReconstructOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReconstructOptions_QualityTargetOutOfRange_ShouldFail()
    {
        var options = new ReconstructOptions { QualityTarget = -0.1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("QualityTarget"));
    }

    [Fact]
    public void ReconstructOptions_ZeroSummaryRatio_ShouldFail()
    {
        var options = new ReconstructOptions { SummaryRatio = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("SummaryRatio"));
    }

    [Fact]
    public void ReconstructOptions_ExpansionRatioLessThanOne_ShouldFail()
    {
        var options = new ReconstructOptions { ExpansionRatio = 0.5 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ExpansionRatio"));
    }

    [Fact]
    public void ReconstructOptions_TemperatureOutOfRange_ShouldFail()
    {
        var options = new ReconstructOptions { Temperature = 3.0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Temperature"));
    }

    [Fact]
    public void ReconstructOptions_ZeroTimeoutMs_ShouldFail()
    {
        var options = new ReconstructOptions { TimeoutMs = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutMs"));
    }

    #endregion

    #region PipelineOptions

    [Fact]
    public void PipelineOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new PipelineOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PipelineOptions_ZeroMaxConcurrency_ShouldFail()
    {
        var options = new PipelineOptions { MaxConcurrency = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxConcurrency"));
    }

    [Fact]
    public void PipelineOptions_ZeroTotalTimeoutMs_ShouldFail()
    {
        var options = new PipelineOptions { TotalTimeoutMs = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TotalTimeoutMs"));
    }

    [Fact]
    public void PipelineOptions_ZeroProgressReportIntervalMs_ShouldFail()
    {
        var options = new PipelineOptions { ProgressReportIntervalMs = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ProgressReportIntervalMs"));
    }

    #endregion

    #region MultimodalProcessingOptions

    [Fact]
    public void MultimodalProcessingOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new MultimodalProcessingOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void MultimodalProcessingOptions_ZeroMaxImages_ShouldFail()
    {
        var options = new MultimodalProcessingOptions { MaxImages = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxImages"));
    }

    [Fact]
    public void MultimodalProcessingOptions_MinimumConfidenceOutOfRange_ShouldFail()
    {
        var options = new MultimodalProcessingOptions { MinimumConfidence = 1.5 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MinimumConfidence"));
    }

    [Fact]
    public void MultimodalProcessingOptions_ZeroTimeoutSeconds_ShouldFail()
    {
        var options = new MultimodalProcessingOptions { TimeoutSeconds = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
    }

    [Fact]
    public void MultimodalProcessingOptions_ZeroMaxConcurrentImages_ShouldFail()
    {
        var options = new MultimodalProcessingOptions { MaxConcurrentImages = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxConcurrentImages"));
    }

    [Fact]
    public void MultimodalProcessingOptions_NegativeRetryCount_ShouldFail()
    {
        var options = new MultimodalProcessingOptions { RetryCount = -1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("RetryCount"));
    }

    #endregion

    #region TextCompletionOptions

    [Fact]
    public void TextCompletionOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new TextCompletionOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TextCompletionOptions_ZeroMaxTokens_ShouldFail()
    {
        var options = new TextCompletionOptions { MaxTokens = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxTokens"));
    }

    [Fact]
    public void TextCompletionOptions_TemperatureOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { Temperature = 2.5 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Temperature"));
    }

    [Fact]
    public void TextCompletionOptions_TopPOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { TopP = -0.1 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TopP"));
    }

    [Fact]
    public void TextCompletionOptions_FrequencyPenaltyOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { FrequencyPenalty = 3.0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("FrequencyPenalty"));
    }

    [Fact]
    public void TextCompletionOptions_PresencePenaltyOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { PresencePenalty = -3.0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("PresencePenalty"));
    }

    #endregion

    #region ImageToTextOptions

    [Fact]
    public void ImageToTextOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new ImageToTextOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ImageToTextOptions_ZeroMaxDescriptionLength_ShouldFail()
    {
        var options = new ImageToTextOptions { MaxDescriptionLength = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxDescriptionLength"));
    }

    #endregion

    #region HtmlChunkingOptions

    [Fact]
    public void HtmlChunkingOptions_DefaultValues_ShouldPassValidation()
    {
        var options = new HtmlChunkingOptions();
        var result = options.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void HtmlChunkingOptions_ZeroMaxChunkSize_ShouldFail()
    {
        var options = new HtmlChunkingOptions { MaxChunkSize = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxChunkSize"));
    }

    [Fact]
    public void HtmlChunkingOptions_ZeroMinChunkSize_ShouldFail()
    {
        var options = new HtmlChunkingOptions { MinChunkSize = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MinChunkSize"));
    }

    [Fact]
    public void HtmlChunkingOptions_MaxChunkSizeLessThanMinChunkSize_ShouldFail()
    {
        var options = new HtmlChunkingOptions { MaxChunkSize = 50, MinChunkSize = 100 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("MaxChunkSize") && e.Contains("MinChunkSize"));
    }

    #endregion

    #region MultipleErrors

    [Fact]
    public void ChunkingOptions_MultipleInvalidValues_ShouldReturnAllErrors()
    {
        var options = new ChunkingOptions
        {
            MaxChunkSize = -1,
            MinChunkSize = -1,
            ChunkOverlap = -1,
            SemanticThreshold = 2.0,
            QualityThreshold = -1.0,
            MaxParallelism = 0
        };

        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().BeGreaterThanOrEqualTo(5);
    }

    #endregion
}
