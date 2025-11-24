using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 사이트맵 분석 서비스 인터페이스
/// sitemap.xml, sitemap.txt, sitemap index 파일 처리
/// </summary>
public interface ISitemapAnalyzer
{
    /// <summary>
    /// 웹사이트에서 사이트맵을 자동 감지하고 분석합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사이트맵 분석 결과</returns>
    Task<SitemapAnalysisResult> AnalyzeFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 특정 사이트맵 URL을 직접 분석합니다.
    /// </summary>
    /// <param name="sitemapUrl">사이트맵 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사이트맵 분석 결과</returns>
    Task<SitemapAnalysisResult> AnalyzeSitemapAsync(string sitemapUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 사이트맵 내용을 직접 파싱합니다.
    /// </summary>
    /// <param name="content">사이트맵 내용</param>
    /// <param name="baseUrl">기본 URL</param>
    /// <param name="format">사이트맵 형식</param>
    /// <returns>파싱된 사이트맵 메타데이터</returns>
    Task<SitemapMetadata> ParseContentAsync(string content, string baseUrl, SitemapFormat format);

    /// <summary>
    /// 여러 사이트맵을 병합하여 통합 분석 결과를 생성합니다.
    /// </summary>
    /// <param name="sitemaps">사이트맵 메타데이터 목록</param>
    /// <returns>통합 사이트맵 메타데이터</returns>
    Task<SitemapMetadata> MergeSitemapsAsync(IEnumerable<SitemapMetadata> sitemaps);

    /// <summary>
    /// 사이트맵 기반으로 크롤링 우선순위를 계산합니다.
    /// </summary>
    /// <param name="metadata">사이트맵 메타데이터</param>
    /// <param name="targetUrl">대상 URL</param>
    /// <returns>우선순위 점수 (1-10)</returns>
    int CalculateCrawlPriority(SitemapMetadata metadata, string targetUrl);

    /// <summary>
    /// 사이트맵에서 URL 패턴을 분석합니다.
    /// </summary>
    /// <param name="metadata">사이트맵 메타데이터</param>
    /// <returns>URL 패턴 분석 결과</returns>
    Task<UrlPatternAnalysis> AnalyzeUrlPatternsAsync(SitemapMetadata metadata);

    /// <summary>
    /// 지원하는 사이트맵 형식을 반환합니다.
    /// </summary>
    IReadOnlyList<SitemapFormat> GetSupportedFormats();

    /// <summary>
    /// 사이트맵 분석 통계를 반환합니다.
    /// </summary>
    SitemapAnalysisStatistics GetStatistics();
}