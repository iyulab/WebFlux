namespace WebFlux.Core.Models;

/// <summary>
/// 청킹 전략 점수 및 선택 이유 추적
/// Phase 5B.2: 향상된 자동 전략 선택을 위한 점수 시스템
/// </summary>
public class StrategyScore
{
    private readonly List<ScoreComponent> _components = new();

    /// <summary>
    /// 총 점수
    /// </summary>
    public double TotalScore => _components.Sum(c => c.Score);

    /// <summary>
    /// 점수 구성 요소들
    /// </summary>
    public IReadOnlyList<ScoreComponent> Components => _components.AsReadOnly();

    /// <summary>
    /// 점수 이유들
    /// </summary>
    public IEnumerable<string> Reasons => _components.Select(c => c.Reason);

    /// <summary>
    /// 점수 추가
    /// </summary>
    /// <param name="category">점수 범주</param>
    /// <param name="score">점수 값</param>
    /// <param name="reason">점수 이유</param>
    public void AddScore(string category, double score, string reason)
    {
        _components.Add(new ScoreComponent
        {
            Category = category,
            Score = score,
            Reason = reason
        });
    }

    /// <summary>
    /// 특정 범주의 점수 합계
    /// </summary>
    /// <param name="category">범주</param>
    /// <returns>해당 범주의 총 점수</returns>
    public double GetCategoryScore(string category)
    {
        return _components.Where(c => c.Category == category).Sum(c => c.Score);
    }

    /// <summary>
    /// 점수 정규화 (0.0 ~ 1.0 범위로)
    /// </summary>
    /// <param name="maxScore">최대 점수</param>
    /// <returns>정규화된 점수</returns>
    public double GetNormalizedScore(double maxScore = 1.0)
    {
        if (TotalScore <= 0) return 0.0;
        return Math.Min(TotalScore / maxScore, 1.0);
    }

    /// <summary>
    /// 점수 상세 정보 반환
    /// </summary>
    /// <returns>점수 구성 요소별 상세 정보</returns>
    public string GetDetailedScoreInfo()
    {
        var categoryGroups = _components
            .GroupBy(c => c.Category)
            .Select(g => $"{g.Key}: {g.Sum(c => c.Score):F2}")
            .ToList();

        return $"Total: {TotalScore:F2} ({string.Join(", ", categoryGroups)})";
    }
}

/// <summary>
/// 점수 구성 요소
/// </summary>
public class ScoreComponent
{
    /// <summary>
    /// 점수 범주 (content_type, structure, size, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 점수 값
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 점수 부여 이유
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 자동 전략 선택 구성
/// Phase 5B.2: 향상된 알고리즘 설정값들
/// </summary>
public class AutoChunkingConfiguration
{
    /// <summary>
    /// 높은 이미지 밀도 임계값
    /// </summary>
    public double HighImageDensityThreshold { get; set; } = 0.3;

    /// <summary>
    /// 긴 문서 임계값 (바이트)
    /// </summary>
    public int LongDocumentThreshold { get; set; } = 50000; // 50KB

    /// <summary>
    /// 매우 긴 문서 임계값 (바이트)
    /// </summary>
    public int VeryLongDocumentThreshold { get; set; } = 100000; // 100KB

    /// <summary>
    /// 짧은 문서 임계값 (바이트)
    /// </summary>
    public int ShortDocumentThreshold { get; set; } = 5000; // 5KB

    /// <summary>
    /// 높은 복잡도 임계값
    /// </summary>
    public double HighComplexityThreshold { get; set; } = 0.7;

    /// <summary>
    /// 중간 복잡도 임계값
    /// </summary>
    public double MediumComplexityThreshold { get; set; } = 0.4;

    /// <summary>
    /// 기술 콘텐츠 키워드들
    /// </summary>
    public HashSet<string> TechnicalKeywords { get; set; } = new()
    {
        "class", "function", "method", "api", "endpoint", "parameter",
        "return", "exception", "interface", "implementation", "algorithm",
        "data structure", "performance", "optimization", "configuration",
        "deployment", "installation", "setup", "tutorial", "guide"
    };

    /// <summary>
    /// 학술 콘텐츠 키워드들
    /// </summary>
    public HashSet<string> AcademicKeywords { get; set; } = new()
    {
        "abstract", "introduction", "methodology", "results", "conclusion",
        "references", "bibliography", "research", "study", "analysis",
        "experiment", "hypothesis", "theory", "literature review"
    };

    /// <summary>
    /// 뉴스 콘텐츠 키워드들
    /// </summary>
    public HashSet<string> NewsKeywords { get; set; } = new()
    {
        "breaking news", "report", "journalist", "correspondent",
        "press release", "statement", "announcement", "update",
        "developing story", "exclusive", "investigation"
    };

    /// <summary>
    /// 점수 가중치 설정
    /// </summary>
    public ScoreWeights Weights { get; set; } = new();
}

/// <summary>
/// 점수 가중치 설정
/// </summary>
public class ScoreWeights
{
    /// <summary>
    /// 콘텐츠 타입 가중치
    /// </summary>
    public double ContentType { get; set; } = 0.3;

    /// <summary>
    /// 구조적 복잡도 가중치
    /// </summary>
    public double StructuralComplexity { get; set; } = 0.25;

    /// <summary>
    /// 문서 크기 가중치
    /// </summary>
    public double DocumentSize { get; set; } = 0.2;

    /// <summary>
    /// 멀티모달 콘텐츠 가중치
    /// </summary>
    public double MultimodalContent { get; set; } = 0.15;

    /// <summary>
    /// 기술적 콘텐츠 가중치
    /// </summary>
    public double TechnicalContent { get; set; } = 0.1;
}

/// <summary>
/// 콘텐츠 분석 메타데이터 확장
/// Phase 5B.2: 더 정교한 콘텐츠 분석을 위한 메타데이터
/// </summary>
public class ContentAnalysisMetadata
{
    /// <summary>
    /// 콘텐츠 타입
    /// </summary>
    public ContentType ContentType { get; set; } = ContentType.Unknown;

    /// <summary>
    /// 구조적 복잡도
    /// </summary>
    public StructuralComplexity StructuralComplexity { get; set; } = StructuralComplexity.Low;

    /// <summary>
    /// 문서 길이 (바이트)
    /// </summary>
    public int DocumentLength { get; set; }

    /// <summary>
    /// 이미지 포함 여부
    /// </summary>
    public bool HasImages { get; set; }

    /// <summary>
    /// 이미지 밀도 (이미지 수 / 텍스트 길이)
    /// </summary>
    public double ImageDensity { get; set; }

    /// <summary>
    /// 기술적 콘텐츠 포함 여부
    /// </summary>
    public bool HasTechnicalContent { get; set; }

    /// <summary>
    /// 다국어 콘텐츠 여부
    /// </summary>
    public bool IsMultiLanguage { get; set; }

    /// <summary>
    /// AI 친화적 사이트 여부
    /// </summary>
    public bool IsAIFriendly { get; set; }

    /// <summary>
    /// PWA 여부
    /// </summary>
    public bool IsPWA { get; set; }

    /// <summary>
    /// 인터랙티브 콘텐츠 여부
    /// </summary>
    public bool IsInteractive { get; set; }

    /// <summary>
    /// 청킹 전략 힌트 (ai.txt에서)
    /// </summary>
    public string? PreferredChunkingHint { get; set; }

    /// <summary>
    /// 콘텐츠 품질 지표
    /// </summary>
    public ContentQualityMetrics QualityMetrics { get; set; } = new();
}

/// <summary>
/// 콘텐츠 품질 지표
/// </summary>
public class ContentQualityMetrics
{
    /// <summary>
    /// 텍스트 밀도 (유의미한 텍스트 비율)
    /// </summary>
    public double TextDensity { get; set; }

    /// <summary>
    /// 구조적 일관성 점수 (0.0 ~ 1.0)
    /// </summary>
    public double StructuralConsistency { get; set; }

    /// <summary>
    /// 의미적 응집성 점수 (0.0 ~ 1.0)
    /// </summary>
    public double SemanticCohesion { get; set; }

    /// <summary>
    /// 읽기 복잡도 점수
    /// </summary>
    public double ReadabilityScore { get; set; }
}

/// <summary>
/// 구조적 복잡도 수준
/// </summary>
public enum StructuralComplexity
{
    /// <summary>단순한 구조</summary>
    Low,
    /// <summary>중간 구조</summary>
    Medium,
    /// <summary>복잡한 구조</summary>
    High
}

/// <summary>
/// 콘텐츠 타입 확장
/// </summary>
public enum ContentType
{
    Unknown,
    Academic,
    News,
    Documentation,
    Blog,
    Forum,
    Product,
    Article,
    Technical,
    ApiReference,
    Tutorial,
    FAQ,
    LegalDocument,
    FinancialReport,
    ResearchPaper
}