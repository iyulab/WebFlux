using AwesomeAssertions;
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
    public void CrawlOptions_ZeroTimeoutMs_ShouldFail()
    {
        var options = new CrawlOptions { TimeoutMs = 0 };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TimeoutMs"));
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
        var options = new TextCompletionOptions { Temperature = 2.5f };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Temperature"));
    }

    [Fact]
    public void TextCompletionOptions_TopPOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { TopP = -0.1f };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("TopP"));
    }

    [Fact]
    public void TextCompletionOptions_FrequencyPenaltyOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { FrequencyPenalty = 3.0f };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("FrequencyPenalty"));
    }

    [Fact]
    public void TextCompletionOptions_PresencePenaltyOutOfRange_ShouldFail()
    {
        var options = new TextCompletionOptions { PresencePenalty = -3.0f };
        var result = options.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("PresencePenalty"));
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
