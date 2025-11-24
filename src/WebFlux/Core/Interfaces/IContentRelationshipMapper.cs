using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 콘텐츠 관계 분석 및 매핑 서비스
/// 웹사이트의 페이지 간 관계, 네비게이션 구조, 콘텐츠 계층을 분석
/// </summary>
public interface IContentRelationshipMapper
{
    /// <summary>
    /// 웹사이트의 전체 콘텐츠 관계 분석
    /// </summary>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="maxDepth">분석할 최대 깊이</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>콘텐츠 관계 분석 결과</returns>
    Task<ContentRelationshipAnalysisResult> AnalyzeContentRelationshipsAsync(string baseUrl, int maxDepth = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 페이지의 관계 분석
    /// </summary>
    /// <param name="pageUrl">페이지 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>페이지 관계 정보</returns>
    Task<PageRelationshipInfo> AnalyzePageRelationshipsAsync(string pageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 사이트 네비게이션 구조 분석
    /// </summary>
    /// <param name="analysisResult">콘텐츠 관계 분석 결과</param>
    /// <returns>네비게이션 구조 분석 결과</returns>
    Task<NavigationStructureResult> AnalyzeNavigationStructureAsync(ContentRelationshipAnalysisResult analysisResult);

    /// <summary>
    /// 콘텐츠 계층 구조 생성
    /// </summary>
    /// <param name="analysisResult">콘텐츠 관계 분석 결과</param>
    /// <returns>콘텐츠 계층 구조</returns>
    Task<ContentHierarchyResult> BuildContentHierarchyAsync(ContentRelationshipAnalysisResult analysisResult);

    /// <summary>
    /// 관련 콘텐츠 추천 생성
    /// </summary>
    /// <param name="pageUrl">기준 페이지 URL</param>
    /// <param name="analysisResult">콘텐츠 관계 분석 결과</param>
    /// <returns>관련 콘텐츠 추천 결과</returns>
    Task<RelatedContentResult> GenerateRelatedContentAsync(string pageUrl, ContentRelationshipAnalysisResult analysisResult);

    /// <summary>
    /// 콘텐츠 클러스터링 수행
    /// </summary>
    /// <param name="analysisResult">콘텐츠 관계 분석 결과</param>
    /// <returns>콘텐츠 클러스터 결과</returns>
    Task<ContentClusterResult> PerformContentClusteringAsync(ContentRelationshipAnalysisResult analysisResult);
}