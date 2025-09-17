using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// API 문서 추출 및 분석 서비스
/// OpenAPI, Swagger, Postman Collection 등을 분석하여 API 구조를 파악
/// </summary>
public interface IAPIDocumentationExtractor
{
    /// <summary>
    /// 웹사이트에서 API 문서를 자동 발견하고 분석
    /// </summary>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>API 문서 분석 결과</returns>
    Task<APIDocumentationAnalysisResult> AnalyzeFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 API 문서 URL에서 직접 분석
    /// </summary>
    /// <param name="apiDocUrl">API 문서 URL</param>
    /// <param name="documentationType">문서 타입</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>API 메타데이터</returns>
    Task<APIMetadata> ExtractAPIMetadataAsync(string apiDocUrl, APIDocumentationType documentationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// API 엔드포인트 구조 분석
    /// </summary>
    /// <param name="apiMetadata">API 메타데이터</param>
    /// <returns>엔드포인트 분석 결과</returns>
    Task<EndpointAnalysisResult> AnalyzeEndpointsAsync(APIMetadata apiMetadata);

    /// <summary>
    /// 데이터 모델 및 스키마 분석
    /// </summary>
    /// <param name="apiMetadata">API 메타데이터</param>
    /// <returns>스키마 분석 결과</returns>
    Task<SchemaAnalysisResult> AnalyzeSchemasAsync(APIMetadata apiMetadata);

    /// <summary>
    /// API 품질 및 완성도 평가
    /// </summary>
    /// <param name="apiMetadata">API 메타데이터</param>
    /// <returns>API 품질 평가 결과</returns>
    Task<APIQualityResult> EvaluateAPIQualityAsync(APIMetadata apiMetadata);

    /// <summary>
    /// API 사용 예제 및 샘플 생성
    /// </summary>
    /// <param name="apiMetadata">API 메타데이터</param>
    /// <returns>사용 예제 결과</returns>
    Task<APIUsageExamplesResult> GenerateUsageExamplesAsync(APIMetadata apiMetadata);
}