namespace WebFlux.Core.Models;

/// <summary>
/// AI로 증강된 콘텐츠
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

    /// <summary>AI 추출 메타데이터</summary>
#if NET8_0_OR_GREATER
    public required AiMetadata Metadata { get; init; }
#else
    public AiMetadata Metadata { get; init; } = new();
#endif

    /// <summary>처리 시간</summary>
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>처리에 소요된 시간 (밀리초)</summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>사용된 토큰 수 (옵션)</summary>
    public int? TokensUsed { get; init; }
}

/// <summary>
/// AI가 추출한 의미론적 메타데이터
/// </summary>
public class AiMetadata
{
    /// <summary>AI 추출 제목</summary>
    public string? Title { get; init; }

    /// <summary>AI 생성 설명</summary>
    public string? Description { get; init; }

    /// <summary>추출된 키워드 목록</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>식별된 주제/토픽 목록</summary>
    public IReadOnlyList<string> Topics { get; init; } = Array.Empty<string>();

    /// <summary>주요 주제</summary>
    public string? MainTopic { get; init; }

    /// <summary>감정 분석 결과 (positive, negative, neutral)</summary>
    public string? Sentiment { get; init; }

    /// <summary>대상 독자층</summary>
    public string? TargetAudience { get; init; }

    /// <summary>예상 독서 시간 (분)</summary>
    public int? EstimatedReadingTimeMinutes { get; init; }

    /// <summary>콘텐츠 타입 (article, tutorial, reference, news 등)</summary>
    public string? ContentType { get; init; }

    /// <summary>난이도 (beginner, intermediate, advanced)</summary>
    public string? DifficultyLevel { get; init; }

    /// <summary>언어 코드 (ko, en, ja 등)</summary>
    public string? Language { get; init; }

    /// <summary>커스텀 메타데이터 (사용자 정의)</summary>
    public IReadOnlyDictionary<string, object> CustomMetadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>신뢰도 점수 (0.0 - 1.0)</summary>
    public double? ConfidenceScore { get; init; }
}
