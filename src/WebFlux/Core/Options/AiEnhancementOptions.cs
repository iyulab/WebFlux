namespace WebFlux.Core.Options;

/// <summary>
/// 요약 옵션
/// </summary>
public class SummaryOptions
{
    /// <summary>최대 요약 길이 (문자 수)</summary>
    public int MaxLength { get; set; } = 200;

    /// <summary>요약 스타일 (concise: 간결, detailed: 상세, bullet: 불릿 포인트)</summary>
    public string Style { get; set; } = "concise";

    /// <summary>요약 언어 (원본 언어 유지 시 null)</summary>
    public string? TargetLanguage { get; set; }

    /// <summary>핵심 정보만 추출 (불필요한 내용 제거)</summary>
    public bool FocusOnKeyPoints { get; set; } = true;
}

/// <summary>
/// 재작성 옵션
/// </summary>
public class RewriteOptions
{
    /// <summary>대상 독자층 (general: 일반, technical: 기술, beginner: 초급, expert: 전문가)</summary>
    public string TargetAudience { get; set; } = "general";

    /// <summary>언어 단순화 (더 쉬운 단어 사용)</summary>
    public bool SimplifyLanguage { get; set; } = true;

    /// <summary>원본 구조 유지 (제목, 단락 구조 보존)</summary>
    public bool PreserveStructure { get; set; } = true;

    /// <summary>톤 조정 (formal: 격식, casual: 캐주얼, professional: 전문적)</summary>
    public string Tone { get; set; } = "professional";

    /// <summary>기술 용어 설명 추가</summary>
    public bool ExplainTechnicalTerms { get; set; }

    /// <summary>예제 추가</summary>
    public bool AddExamples { get; set; }
}

/// <summary>
/// 메타데이터 추출 옵션
/// </summary>
public class MetadataExtractionOptions
{
    /// <summary>키워드 추출 활성화</summary>
    public bool ExtractKeywords { get; set; } = true;

    /// <summary>주제/토픽 추출 활성화</summary>
    public bool ExtractTopics { get; set; } = true;

    /// <summary>감정 분석 활성화</summary>
    public bool AnalyzeSentiment { get; set; }

    /// <summary>최대 키워드 수</summary>
    public int MaxKeywords { get; set; } = 10;

    /// <summary>최대 주제 수</summary>
    public int MaxTopics { get; set; } = 5;

    /// <summary>대상 독자 분석</summary>
    public bool IdentifyTargetAudience { get; set; } = true;

    /// <summary>독서 시간 추정</summary>
    public bool EstimateReadingTime { get; set; } = true;

    /// <summary>콘텐츠 타입 분류</summary>
    public bool ClassifyContentType { get; set; } = true;

    /// <summary>난이도 평가</summary>
    public bool AssessDifficulty { get; set; } = true;
}

/// <summary>
/// 통합 증강 옵션
/// </summary>
public class EnhancementOptions
{
    /// <summary>요약 활성화</summary>
    public bool EnableSummary { get; set; } = true;

    /// <summary>재작성 활성화</summary>
    public bool EnableRewrite { get; set; }

    /// <summary>메타데이터 추출 활성화</summary>
    public bool EnableMetadata { get; set; } = true;

    /// <summary>요약 옵션</summary>
    public SummaryOptions? SummaryOptions { get; set; }

    /// <summary>재작성 옵션</summary>
    public RewriteOptions? RewriteOptions { get; set; }

    /// <summary>메타데이터 추출 옵션</summary>
    public MetadataExtractionOptions? MetadataOptions { get; set; }

    /// <summary>병렬 처리 활성화 (요약, 재작성, 메타데이터 동시 처리)</summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>타임아웃 (밀리초)</summary>
    public int TimeoutMs { get; set; } = 60000;

    /// <summary>모든 증강 기능 활성화</summary>
    public static EnhancementOptions EnableAll()
    {
        return new EnhancementOptions
        {
            EnableSummary = true,
            EnableRewrite = true,
            EnableMetadata = true
        };
    }

    /// <summary>요약만 활성화</summary>
    public static EnhancementOptions SummaryOnly()
    {
        return new EnhancementOptions
        {
            EnableSummary = true,
            EnableRewrite = false,
            EnableMetadata = false
        };
    }
}
