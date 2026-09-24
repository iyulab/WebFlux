using WebFlux.Core.Options;

namespace WebFlux.Core.Models;

/// <summary>
/// WebFlux SDK 전체 구성 클래스
/// </summary>
public class WebFluxConfiguration
{
    /// <summary>
    /// 크롤링 기본 설정
    /// </summary>
    public CrawlingConfiguration Crawling { get; set; } = new();

    /// <summary>
    /// 청킹 기본 설정
    /// </summary>
    public ChunkingConfiguration Chunking { get; set; } = new();

    /// <summary>
    /// 성능 및 리소스 설정
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// AI 증강 설정 (Phase 1)
    /// </summary>
    public AiEnhancementConfiguration AiEnhancement { get; set; } = new();
}

/// <summary>
/// 크롤링 구성
/// </summary>
public class CrawlingConfiguration
{
    /// <summary>
    /// 시작 URL 목록
    /// </summary>
    public List<string> StartUrls { get; set; } = new();

    /// <summary>
    /// 크롤링 전략 (기본값: <see cref="CrawlStrategy.BreadthFirst"/>). 설정 파일에서는 이름(<c>"Sitemap"</c> 등)으로 준다 —
    /// 알 수 없는 이름은 바인딩 오류다(0.16.0 전에는 문자열이라 오타가 조용히 BreadthFirst 가 됐다).
    /// </summary>
    public CrawlStrategy Strategy { get; set; } = CrawlStrategy.BreadthFirst;

    /// <summary>
    /// 사이트 크롤의 최대 깊이 (기본값: 3). 크롤러의 <c>CrawlOptions.MaxDepth</c> 가 된다. 단일 URL 진입점
    /// (<c>ProcessUrlAsync</c>/배치)은 이 값과 무관하게 그 페이지만 처리한다.
    /// </summary>
    public int MaxDepth { get; set; } = 3;

    /// <summary>
    /// 사이트 크롤의 최대 페이지 수 (기본값: 100). 크롤러의 <c>CrawlOptions.MaxPages</c> 가 된다. 단일 URL 진입점은 1.
    /// </summary>
    public int MaxPages { get; set; } = 100;

    /// <summary>
    /// 기본 User-Agent
    /// </summary>
    public string DefaultUserAgent { get; set; } = WebFlux.Core.Utilities.WebFluxUserAgent.Default;

    /// <summary>
    /// 기본 요청 타임아웃 (초, 기본값: 15). 크롤러의 요청 타임아웃(<c>CrawlOptions.TimeoutMs</c>)이 된다.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// 기본 요청 간 지연 (밀리초, 기본값: 0). 크롤러의 <c>CrawlOptions.DelayMs</c> 가 된다.
    /// </summary>
    public int DefaultDelayMs { get; set; }

    /// <summary>
    /// 최대 동시 요청 수 (기본값: 3). 크롤러의 <c>CrawlOptions.ConcurrentRequests</c> 가 된다.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 3;

    /// <summary>
    /// 기본 재시도 횟수
    /// </summary>
    public int DefaultRetryCount { get; set; } = 3;

    /// <summary>
    /// robots.txt 준수 여부
    /// </summary>
    public bool RespectRobotsTxt { get; set; } = true;

    /// <summary>
    /// 기본 제외 확장자 (기본값: <c>CrawlOptions.ExcludedExtensions</c> 와 같은 목록). 크롤러의 제외 목록이 된다.
    /// </summary>
    public ISet<string> DefaultExcludedExtensions { get; set; } = new HashSet<string>(new WebFlux.Core.Options.CrawlOptions().ExcludedExtensions);

    /// <summary>
    /// 사용자 정의 헤더
    /// </summary>
    public IDictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// 청킹 구성
/// </summary>
public class ChunkingConfiguration
{
    /// <summary>
    /// 기본 청킹 전략 (기본값: <see cref="ChunkingStrategyType.Auto"/>). 설정 파일에서는 이름(<c>"Paragraph"</c> 등)으로 준다 —
    /// 알 수 없는 이름은 바인딩 오류다(0.16.0 전에는 문자열이라 오타가 문서마다 경고 로그와 청크 0 개가 됐다).
    /// </summary>
    public ChunkingStrategyType DefaultStrategy { get; set; } = ChunkingStrategyType.Auto;

    /// <summary>
    /// 최소 청크 크기
    /// </summary>
    public int MinChunkSize { get; set; } = 100;

    /// <summary>
    /// 최대 청크 크기 (기본값: 1000)
    /// </summary>
    public int MaxChunkSize { get; set; } = 1000;

    /// <summary>
    /// 겹침 크기 (기본값: 50). 청킹 옵션의 <c>ChunkOverlap</c> 이 된다.
    /// </summary>
    public int OverlapSize { get; set; } = 50;
}

/// <summary>
/// 성능 구성
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// 최대 병렬 처리 수
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}

/// <summary>
/// AI 증강 구성 (Phase 1)
/// </summary>
public class AiEnhancementConfiguration
{
    /// <summary>
    /// AI 증강 활성화 여부
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 요약 생성 활성화
    /// </summary>
    public bool EnableSummary { get; set; } = true;

    /// <summary>
    /// 메타데이터 추출 활성화
    /// </summary>
    public bool EnableMetadata { get; set; } = true;

    /// <summary>
    /// 재작성 활성화 (기본값: false). 0.14.0 부터 크롤 경로가 실제로 읽는다 — 그 전에는 프로세서가 <c>false</c> 를
    /// 리터럴로 넘겨 이 값은 아무 일도 하지 않았다.
    /// </summary>
    public bool EnableRewrite { get; set; }

    /// <summary>
    /// 요약 옵션 (<see cref="EnableSummary"/> 일 때 요약 프롬프트에 실리는 스타일·길이·언어·핵심 정보). 0.14.0 신설 —
    /// 그 전에는 크롤 경로에서 <see cref="SummaryOptions"/> 를 넘길 방법이 없어 서비스 기본값만 쓰였다.
    /// </summary>
    public SummaryOptions Summary { get; set; } = new();

    /// <summary>
    /// 재작성 옵션 (<see cref="EnableRewrite"/> 일 때). 0.14.0 신설, <see cref="Summary"/> 와 같은 이유.
    /// </summary>
    public RewriteOptions Rewrite { get; set; } = new();

    /// <summary>
    /// 병렬 처리 활성화
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// AI 처리 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMs { get; set; } = 60000;
}
