namespace WebFlux.Core.Models;

/// <summary>
/// 콘텐츠 품질 정보
/// AI 리서치용 콘텐츠 필터링 및 우선순위 결정에 사용
/// </summary>
public class ContentQualityInfo
{
    #region 기본 품질 점수

    /// <summary>
    /// 전체 품질 점수 (0.0 - 1.0)
    /// 여러 품질 지표의 가중 평균
    /// </summary>
    public double OverallScore { get; init; }

    /// <summary>
    /// 품질 등급
    /// </summary>
    public QualityGrade Grade => OverallScore switch
    {
        >= 0.8 => QualityGrade.Excellent,
        >= 0.6 => QualityGrade.Good,
        >= 0.4 => QualityGrade.Fair,
        >= 0.2 => QualityGrade.Poor,
        _ => QualityGrade.VeryPoor
    };

    #endregion

    #region 콘텐츠 특성

    /// <summary>
    /// 콘텐츠 분류 (기사, 블로그, 문서, 상품 페이지 등)
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// 감지된 언어 (ISO 639-1 코드)
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// 예상 읽기 시간 (분)
    /// </summary>
    public int EstimatedReadingTimeMinutes { get; init; }

    /// <summary>
    /// 단어 수
    /// </summary>
    public int WordCount { get; init; }

    #endregion

    #region 접근성 지표

    /// <summary>
    /// 페이월 감지 여부
    /// </summary>
    public bool HasPaywall { get; init; }

    /// <summary>
    /// 로그인 필요 여부
    /// </summary>
    public bool RequiresLogin { get; init; }

    /// <summary>
    /// 연령 제한 콘텐츠 여부
    /// </summary>
    public bool HasAgeRestriction { get; init; }

    #endregion

    #region 콘텐츠 구조 지표

    /// <summary>
    /// 콘텐츠 비율 (텍스트 / 전체 HTML)
    /// 높을수록 콘텐츠 밀도가 높음
    /// </summary>
    public double ContentRatio { get; init; }

    /// <summary>
    /// 광고 밀도 (0.0 - 1.0)
    /// 낮을수록 좋음
    /// </summary>
    public double AdDensity { get; init; }

    /// <summary>
    /// 본문 콘텐츠 존재 여부
    /// </summary>
    public bool HasMainContent { get; init; }

    /// <summary>
    /// 구조화된 데이터 존재 여부 (Schema.org 등)
    /// </summary>
    public bool HasStructuredData { get; init; }

    #endregion

    #region 신뢰성 지표

    /// <summary>
    /// 작성자 정보 존재 여부
    /// </summary>
    public bool HasAuthor { get; init; }

    /// <summary>
    /// 게시 날짜 존재 여부
    /// </summary>
    public bool HasPublishDate { get; init; }

    /// <summary>
    /// 게시 날짜
    /// </summary>
    public DateTimeOffset? PublishDate { get; init; }

    /// <summary>
    /// 마지막 수정 날짜
    /// </summary>
    public DateTimeOffset? LastModifiedDate { get; init; }

    /// <summary>
    /// 출처/인용 존재 여부
    /// </summary>
    public bool HasCitations { get; init; }

    #endregion

    #region 기술적 지표

    /// <summary>
    /// 모바일 친화적 여부
    /// </summary>
    public bool IsMobileFriendly { get; init; }

    /// <summary>
    /// HTTPS 사용 여부
    /// </summary>
    public bool IsSecure { get; init; }

    /// <summary>
    /// 페이지 로드 시간 (밀리초)
    /// </summary>
    public long LoadTimeMs { get; init; }

    #endregion

    #region LLM 최적화 지표

    /// <summary>
    /// LLM 처리 적합성 점수 (0.0 - 1.0)
    /// 토큰 효율, 구조화 수준, 노이즈 비율 고려
    /// </summary>
    public double LlmSuitabilityScore { get; init; }

    /// <summary>
    /// 예상 토큰 수 (GPT-4 기준)
    /// </summary>
    public int EstimatedTokenCount { get; init; }

    /// <summary>
    /// 노이즈 비율 (0.0 - 1.0)
    /// 광고, 네비게이션, 중복 콘텐츠 등
    /// </summary>
    public double NoiseRatio { get; init; }

    #endregion

    /// <summary>
    /// 품질 요약 문자열
    /// </summary>
    public override string ToString()
    {
        var paywallWarning = HasPaywall ? " [Paywall]" : "";
        return $"Quality: {OverallScore:P0} ({Grade}){paywallWarning}, {WordCount} words, ~{EstimatedReadingTimeMinutes}min read";
    }
}

/// <summary>
/// 품질 등급 열거형
/// </summary>
public enum QualityGrade
{
    /// <summary>매우 우수 (80%+)</summary>
    Excellent,

    /// <summary>우수 (60-80%)</summary>
    Good,

    /// <summary>보통 (40-60%)</summary>
    Fair,

    /// <summary>미흡 (20-40%)</summary>
    Poor,

    /// <summary>매우 미흡 (20% 미만)</summary>
    VeryPoor
}
