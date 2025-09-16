using WebFlux.Core.Models;
using WebFlux.Core.Options;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// llms.txt 파일 파싱 서비스 인터페이스
/// AI 친화적 웹 표준을 통한 사이트 구조 및 메타데이터 추출
/// </summary>
public interface ILlmsParser
{
    /// <summary>
    /// 웹사이트에서 llms.txt 파일을 감지하고 파싱합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>llms.txt 파싱 결과</returns>
    Task<LlmsParseResult?> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// llms.txt 파일 내용을 직접 파싱합니다.
    /// </summary>
    /// <param name="content">llms.txt 파일 내용</param>
    /// <param name="baseUrl">기본 URL (상대 경로 해석용)</param>
    /// <returns>파싱된 llms.txt 메타데이터</returns>
    Task<LlmsMetadata> ParseContentAsync(string content, string baseUrl);

    /// <summary>
    /// llms.txt 메타데이터를 기반으로 크롤링 전략을 최적화합니다.
    /// </summary>
    /// <param name="metadata">llms.txt 메타데이터</param>
    /// <param name="crawlOptions">기존 크롤링 옵션</param>
    /// <returns>최적화된 크롤링 옵션</returns>
    Task<CrawlOptions> OptimizeCrawlOptionsAsync(LlmsMetadata metadata, CrawlOptions crawlOptions);

    /// <summary>
    /// llms.txt 메타데이터를 기반으로 청킹 전략을 개선합니다.
    /// </summary>
    /// <param name="metadata">llms.txt 메타데이터</param>
    /// <param name="content">추출된 콘텐츠</param>
    /// <param name="chunkingOptions">기존 청킹 옵션</param>
    /// <returns>개선된 청킹 옵션</returns>
    Task<ChunkingOptions> EnhanceChunkingOptionsAsync(
        LlmsMetadata metadata,
        ExtractedContent content,
        ChunkingOptions chunkingOptions);

    /// <summary>
    /// 지원하는 llms.txt 버전을 반환합니다.
    /// </summary>
    IReadOnlyList<string> GetSupportedVersions();

    /// <summary>
    /// llms.txt 파싱 통계를 반환합니다.
    /// </summary>
    LlmsParsingStatistics GetStatistics();
}