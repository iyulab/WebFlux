using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// Web App Manifest 파일 파싱 서비스 인터페이스
/// PWA 및 웹앱 메타데이터 처리 (W3C Web App Manifest 표준)
/// </summary>
public interface IManifestParser
{
    /// <summary>
    /// 웹사이트에서 manifest.json 파일을 감지하고 파싱합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>manifest.json 파싱 결과</returns>
    Task<ManifestParseResult> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// HTML 페이지에서 매니페스트 링크를 추출하고 파싱합니다.
    /// </summary>
    /// <param name="htmlContent">HTML 콘텐츠</param>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>manifest.json 파싱 결과</returns>
    Task<ManifestParseResult> ParseFromHtmlAsync(string htmlContent, string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// manifest.json 파일 내용을 직접 파싱합니다.
    /// </summary>
    /// <param name="content">manifest.json 파일 내용</param>
    /// <param name="baseUrl">기본 URL (상대 경로 해석용)</param>
    /// <returns>파싱된 매니페스트 메타데이터</returns>
    Task<ManifestMetadata> ParseContentAsync(string content, string baseUrl);

    /// <summary>
    /// 매니페스트를 기반으로 PWA 호환성을 평가합니다.
    /// </summary>
    /// <param name="metadata">매니페스트 메타데이터</param>
    /// <returns>PWA 호환성 평가 결과</returns>
    Task<PwaCompatibilityResult> EvaluatePwaCompatibilityAsync(ManifestMetadata metadata);

    /// <summary>
    /// 매니페스트에서 앱 아이콘을 분석합니다.
    /// </summary>
    /// <param name="metadata">매니페스트 메타데이터</param>
    /// <returns>아이콘 분석 결과</returns>
    Task<IconAnalysisResult> AnalyzeIconsAsync(ManifestMetadata metadata);

    /// <summary>
    /// 매니페스트를 기반으로 앱 카테고리를 예측합니다.
    /// </summary>
    /// <param name="metadata">매니페스트 메타데이터</param>
    /// <returns>예측된 앱 카테고리</returns>
    Task<AppCategoryPrediction> PredictAppCategoryAsync(ManifestMetadata metadata);

    /// <summary>
    /// 매니페스트의 유효성을 검사합니다.
    /// </summary>
    /// <param name="metadata">매니페스트 메타데이터</param>
    /// <returns>유효성 검사 결과</returns>
    Task<ManifestValidationResult> ValidateManifestAsync(ManifestMetadata metadata);

    /// <summary>
    /// 지원하는 매니페스트 스펙 버전을 반환합니다.
    /// </summary>
    IReadOnlyList<string> GetSupportedSpecVersions();

    /// <summary>
    /// 매니페스트 파싱 통계를 반환합니다.
    /// </summary>
    ManifestStatistics GetStatistics();
}