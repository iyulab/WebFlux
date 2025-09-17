namespace WebFlux.Core.Models;

/// <summary>
/// API 문서 분석 결과
/// </summary>
public class APIDocumentationAnalysisResult
{
    /// <summary>
    /// 발견된 API 문서들
    /// </summary>
    public List<APIDocumentInfo> DiscoveredDocuments { get; set; } = new();

    /// <summary>
    /// 주요 API 메타데이터
    /// </summary>
    public APIMetadata? PrimaryAPI { get; set; }

    /// <summary>
    /// 엔드포인트 분석 결과
    /// </summary>
    public EndpointAnalysisResult? EndpointAnalysis { get; set; }

    /// <summary>
    /// 스키마 분석 결과
    /// </summary>
    public SchemaAnalysisResult? SchemaAnalysis { get; set; }

    /// <summary>
    /// API 품질 평가
    /// </summary>
    public APIQualityResult? QualityEvaluation { get; set; }

    /// <summary>
    /// 사용 예제
    /// </summary>
    public APIUsageExamplesResult? UsageExamples { get; set; }

    /// <summary>
    /// 분석 품질 점수 (0.0-1.0)
    /// </summary>
    public double QualityScore { get; set; }

    /// <summary>
    /// 분석 수행 시간
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API 문서 정보
/// </summary>
public class APIDocumentInfo
{
    /// <summary>
    /// 문서 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 문서 타입
    /// </summary>
    public APIDocumentationType DocumentationType { get; set; }

    /// <summary>
    /// 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 파일 크기 (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 발견 방법
    /// </summary>
    public string DiscoveryMethod { get; set; } = string.Empty;

    /// <summary>
    /// 컨텐츠 타입
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// API 메타데이터
/// </summary>
public class APIMetadata
{
    /// <summary>
    /// API 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// API 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 버전
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 베이스 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 문서 타입
    /// </summary>
    public APIDocumentationType DocumentationType { get; set; }

    /// <summary>
    /// 서버 정보
    /// </summary>
    public List<ServerInfo> Servers { get; set; } = new();

    /// <summary>
    /// 엔드포인트 목록
    /// </summary>
    public List<EndpointInfo> Endpoints { get; set; } = new();

    /// <summary>
    /// 스키마 정의
    /// </summary>
    public List<SchemaDefinition> Schemas { get; set; } = new();

    /// <summary>
    /// 인증 방식
    /// </summary>
    public List<AuthenticationMethod> AuthMethods { get; set; } = new();

    /// <summary>
    /// 태그/카테고리
    /// </summary>
    public List<APITag> Tags { get; set; } = new();

    /// <summary>
    /// 외부 문서 링크
    /// </summary>
    public List<ExternalDocumentation> ExternalDocs { get; set; } = new();

    /// <summary>
    /// 원본 내용
    /// </summary>
    public string RawContent { get; set; } = string.Empty;
}

/// <summary>
/// 서버 정보
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// 서버 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 환경 (production, staging, development)
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// 엔드포인트 정보
/// </summary>
public class EndpointInfo
{
    /// <summary>
    /// HTTP 메서드
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 경로
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 요약
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 태그
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 매개변수
    /// </summary>
    public List<ParameterInfo> Parameters { get; set; } = new();

    /// <summary>
    /// 요청 본문
    /// </summary>
    public RequestBodyInfo? RequestBody { get; set; }

    /// <summary>
    /// 응답 정보
    /// </summary>
    public Dictionary<string, ResponseInfo> Responses { get; set; } = new();

    /// <summary>
    /// 보안 요구사항
    /// </summary>
    public List<SecurityRequirement> Security { get; set; } = new();

    /// <summary>
    /// 사용 중단 여부
    /// </summary>
    public bool Deprecated { get; set; }
}

/// <summary>
/// 매개변수 정보
/// </summary>
public class ParameterInfo
{
    /// <summary>
    /// 매개변수명
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 위치 (query, header, path, cookie)
    /// </summary>
    public string In { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 필수 여부
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 스키마
    /// </summary>
    public SchemaDefinition? Schema { get; set; }

    /// <summary>
    /// 예제 값
    /// </summary>
    public object? Example { get; set; }
}

/// <summary>
/// 요청 본문 정보
/// </summary>
public class RequestBodyInfo
{
    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 필수 여부
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 컨텐츠 타입별 스키마
    /// </summary>
    public Dictionary<string, MediaTypeInfo> Content { get; set; } = new();
}

/// <summary>
/// 응답 정보
/// </summary>
public class ResponseInfo
{
    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 헤더
    /// </summary>
    public Dictionary<string, HeaderInfo> Headers { get; set; } = new();

    /// <summary>
    /// 컨텐츠
    /// </summary>
    public Dictionary<string, MediaTypeInfo> Content { get; set; } = new();
}

/// <summary>
/// 미디어 타입 정보
/// </summary>
public class MediaTypeInfo
{
    /// <summary>
    /// 스키마
    /// </summary>
    public SchemaDefinition? Schema { get; set; }

    /// <summary>
    /// 예제
    /// </summary>
    public object? Example { get; set; }

    /// <summary>
    /// 인코딩
    /// </summary>
    public Dictionary<string, EncodingInfo> Encoding { get; set; } = new();
}

/// <summary>
/// 헤더 정보
/// </summary>
public class HeaderInfo
{
    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 스키마
    /// </summary>
    public SchemaDefinition? Schema { get; set; }
}

/// <summary>
/// 인코딩 정보
/// </summary>
public class EncodingInfo
{
    /// <summary>
    /// 컨텐츠 타입
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 헤더
    /// </summary>
    public Dictionary<string, HeaderInfo> Headers { get; set; } = new();

    /// <summary>
    /// 스타일
    /// </summary>
    public string Style { get; set; } = string.Empty;

    /// <summary>
    /// 폭발 여부
    /// </summary>
    public bool Explode { get; set; }
}

/// <summary>
/// 스키마 정의
/// </summary>
public class SchemaDefinition
{
    /// <summary>
    /// 타입
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 형식
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// 제목
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 속성들
    /// </summary>
    public Dictionary<string, SchemaDefinition> Properties { get; set; } = new();

    /// <summary>
    /// 필수 속성들
    /// </summary>
    public List<string> Required { get; set; } = new();

    /// <summary>
    /// 배열 항목 스키마
    /// </summary>
    public SchemaDefinition? Items { get; set; }

    /// <summary>
    /// 열거형 값들
    /// </summary>
    public List<object> Enum { get; set; } = new();

    /// <summary>
    /// 참조
    /// </summary>
    public string? Ref { get; set; }

    /// <summary>
    /// 예제
    /// </summary>
    public object? Example { get; set; }

    /// <summary>
    /// 기본값
    /// </summary>
    public object? Default { get; set; }

    /// <summary>
    /// 최솟값
    /// </summary>
    public double? Minimum { get; set; }

    /// <summary>
    /// 최댓값
    /// </summary>
    public double? Maximum { get; set; }

    /// <summary>
    /// 최소 길이
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// 최대 길이
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 패턴
    /// </summary>
    public string? Pattern { get; set; }
}

/// <summary>
/// 인증 방법
/// </summary>
public class AuthenticationMethod
{
    /// <summary>
    /// 인증 타입
    /// </summary>
    public AuthenticationType Type { get; set; }

    /// <summary>
    /// 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 스킴
    /// </summary>
    public string Scheme { get; set; } = string.Empty;

    /// <summary>
    /// Bearer 형식
    /// </summary>
    public string BearerFormat { get; set; } = string.Empty;

    /// <summary>
    /// 위치 (header, query, cookie)
    /// </summary>
    public string In { get; set; } = string.Empty;

    /// <summary>
    /// OAuth2 플로우
    /// </summary>
    public Dictionary<string, OAuthFlow> Flows { get; set; } = new();
}

/// <summary>
/// OAuth 플로우
/// </summary>
public class OAuthFlow
{
    /// <summary>
    /// 인증 URL
    /// </summary>
    public string AuthorizationUrl { get; set; } = string.Empty;

    /// <summary>
    /// 토큰 URL
    /// </summary>
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>
    /// 갱신 URL
    /// </summary>
    public string RefreshUrl { get; set; } = string.Empty;

    /// <summary>
    /// 스코프
    /// </summary>
    public Dictionary<string, string> Scopes { get; set; } = new();
}

/// <summary>
/// API 태그
/// </summary>
public class APITag
{
    /// <summary>
    /// 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 외부 문서
    /// </summary>
    public ExternalDocumentation? ExternalDocs { get; set; }
}

/// <summary>
/// 외부 문서
/// </summary>
public class ExternalDocumentation
{
    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 보안 요구사항
/// </summary>
public class SecurityRequirement
{
    /// <summary>
    /// 보안 스킴명
    /// </summary>
    public string SchemeName { get; set; } = string.Empty;

    /// <summary>
    /// 스코프
    /// </summary>
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// 엔드포인트 분석 결과
/// </summary>
public class EndpointAnalysisResult
{
    /// <summary>
    /// 총 엔드포인트 수
    /// </summary>
    public int TotalEndpoints { get; set; }

    /// <summary>
    /// HTTP 메서드별 분포
    /// </summary>
    public Dictionary<string, int> MethodDistribution { get; set; } = new();

    /// <summary>
    /// 태그별 분포
    /// </summary>
    public Dictionary<string, int> TagDistribution { get; set; } = new();

    /// <summary>
    /// 인증이 필요한 엔드포인트 수
    /// </summary>
    public int SecuredEndpoints { get; set; }

    /// <summary>
    /// 사용 중단된 엔드포인트 수
    /// </summary>
    public int DeprecatedEndpoints { get; set; }

    /// <summary>
    /// 복잡도 점수 (0.0-1.0)
    /// </summary>
    public double ComplexityScore { get; set; }

    /// <summary>
    /// 커버리지 점수 (0.0-1.0)
    /// </summary>
    public double CoverageScore { get; set; }
}

/// <summary>
/// 스키마 분석 결과
/// </summary>
public class SchemaAnalysisResult
{
    /// <summary>
    /// 총 스키마 수
    /// </summary>
    public int TotalSchemas { get; set; }

    /// <summary>
    /// 타입별 분포
    /// </summary>
    public Dictionary<string, int> TypeDistribution { get; set; } = new();

    /// <summary>
    /// 재사용 가능한 스키마 수
    /// </summary>
    public int ReusableSchemas { get; set; }

    /// <summary>
    /// 검증 규칙이 있는 스키마 수
    /// </summary>
    public int ValidatedSchemas { get; set; }

    /// <summary>
    /// 순환 참조 수
    /// </summary>
    public int CircularReferences { get; set; }

    /// <summary>
    /// 스키마 품질 점수 (0.0-1.0)
    /// </summary>
    public double QualityScore { get; set; }
}

/// <summary>
/// API 품질 평가 결과
/// </summary>
public class APIQualityResult
{
    /// <summary>
    /// 전체 품질 점수 (0.0-1.0)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// 문서화 품질 점수
    /// </summary>
    public double DocumentationScore { get; set; }

    /// <summary>
    /// 일관성 점수
    /// </summary>
    public double ConsistencyScore { get; set; }

    /// <summary>
    /// 완성도 점수
    /// </summary>
    public double CompletenessScore { get; set; }

    /// <summary>
    /// 보안 점수
    /// </summary>
    public double SecurityScore { get; set; }

    /// <summary>
    /// 품질 문제
    /// </summary>
    public List<QualityIssue> Issues { get; set; } = new();

    /// <summary>
    /// 개선 권장사항
    /// </summary>
    public List<ImprovementRecommendation> Recommendations { get; set; } = new();
}



/// <summary>
/// API 사용 예제 결과
/// </summary>
public class APIUsageExamplesResult
{
    /// <summary>
    /// cURL 예제
    /// </summary>
    public List<CodeExample> CurlExamples { get; set; } = new();

    /// <summary>
    /// JavaScript 예제
    /// </summary>
    public List<CodeExample> JavaScriptExamples { get; set; } = new();

    /// <summary>
    /// Python 예제
    /// </summary>
    public List<CodeExample> PythonExamples { get; set; } = new();

    /// <summary>
    /// C# 예제
    /// </summary>
    public List<CodeExample> CSharpExamples { get; set; } = new();

    /// <summary>
    /// 포스트맨 컬렉션
    /// </summary>
    public string? PostmanCollection { get; set; }

    /// <summary>
    /// SDK 생성 가능 여부
    /// </summary>
    public bool SDKGenerationPossible { get; set; }
}

/// <summary>
/// 코드 예제
/// </summary>
public class CodeExample
{
    /// <summary>
    /// 엔드포인트
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 코드
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 언어
    /// </summary>
    public string Language { get; set; } = string.Empty;
}

/// <summary>
/// API 문서 타입
/// </summary>
public enum APIDocumentationType
{
    Unknown,
    OpenAPI30,        // OpenAPI 3.0
    OpenAPI31,        // OpenAPI 3.1
    Swagger20,        // Swagger 2.0
    RAML,             // RAML
    APIBlueprint,     // API Blueprint
    Postman,          // Postman Collection
    Insomnia,         // Insomnia Collection
    GraphQL,          // GraphQL Schema
    AsyncAPI,         // AsyncAPI
    WSDL              // SOAP WSDL
}

/// <summary>
/// 인증 타입
/// </summary>
public enum AuthenticationType
{
    Unknown,
    None,
    ApiKey,
    Http,
    OAuth2,
    OpenIdConnect,
    Basic,
    Bearer,
    Digest
}

/// <summary>
/// 문제 심각도
/// </summary>
public enum IssueSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// 권장사항 우선순위
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}