namespace WebFlux.Core.Models;

/// <summary>
/// AI로 증강된 콘텐츠
/// EnrichedMetadata를 사용하여 HTML + AI 메타데이터를 통합 관리
/// </summary>
public class EnhancedContent
{
    /// <summary>원본 콘텐츠</summary>
#if NET8_0_OR_GREATER
    public required string OriginalContent { get; init; }
#else
    public string OriginalContent { get; init; } = string.Empty;
#endif

    /// <summary>AI 생성 요약</summary>
    public string? Summary { get; init; }

    /// <summary>AI 재작성된 콘텐츠</summary>
    public string? RewrittenContent { get; init; }

    /// <summary>
    /// 풍부한 메타데이터 (HTML + AI 융합)
    /// 웹 메타데이터, AI 추출 메타데이터, 사용자 검증을 모두 포함
    /// </summary>
#if NET8_0_OR_GREATER
    public required EnrichedMetadata Metadata { get; init; }
#else
    public EnrichedMetadata Metadata { get; init; } = new();
#endif

    /// <summary>처리 시간</summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>처리에 소요된 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>사용된 토큰 수 (옵션)</summary>
    public int? TokensUsed { get; init; }
}
