using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 패키지 생태계 메타데이터 분석 서비스
/// package.json, composer.json, requirements.txt 등을 통해 프로젝트 의존성과 기술 스택을 파악
/// </summary>
public interface IPackageEcosystemAnalyzer
{
    /// <summary>
    /// 웹사이트에서 패키지 생태계 파일들을 발견하고 분석
    /// </summary>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>패키지 생태계 분석 결과</returns>
    Task<PackageEcosystemAnalysisResult> AnalyzeFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 패키지 파일 URL에서 직접 분석
    /// </summary>
    /// <param name="packageFileUrl">패키지 파일 URL</param>
    /// <param name="packageType">패키지 타입</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>패키지 메타데이터</returns>
    Task<PackageMetadata> AnalyzePackageFileAsync(string packageFileUrl, PackageEcosystemType packageType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 기술 스택 분석 및 분류
    /// </summary>
    /// <param name="packageMetadata">패키지 메타데이터</param>
    /// <returns>기술 스택 분석 결과</returns>
    Task<TechStackAnalysisResult> AnalyzeTechStackAsync(PackageMetadata packageMetadata);

    /// <summary>
    /// 프로젝트 복잡도 및 성숙도 평가
    /// </summary>
    /// <param name="packageMetadata">패키지 메타데이터</param>
    /// <returns>프로젝트 복잡도 평가</returns>
    Task<ProjectComplexityResult> EvaluateProjectComplexityAsync(PackageMetadata packageMetadata);

    /// <summary>
    /// 보안 취약성 의존성 감지
    /// </summary>
    /// <param name="packageMetadata">패키지 메타데이터</param>
    /// <returns>보안 분석 결과</returns>
    Task<SecurityAnalysisResult> AnalyzeSecurityRisksAsync(PackageMetadata packageMetadata);
}