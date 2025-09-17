using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// ai.txt 파일 파싱 서비스 인터페이스
/// AI 에이전트를 위한 웹사이트 가이드라인 및 메타데이터 처리
/// </summary>
public interface IAiTxtParser
{
    /// <summary>
    /// 웹사이트에서 ai.txt 파일을 감지하고 파싱합니다.
    /// </summary>
    /// <param name="baseUrl">웹사이트 기본 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>ai.txt 파싱 결과</returns>
    Task<AiTxtParseResult> ParseFromWebsiteAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// ai.txt 파일 내용을 직접 파싱합니다.
    /// </summary>
    /// <param name="content">ai.txt 파일 내용</param>
    /// <param name="baseUrl">기본 URL (상대 경로 해석용)</param>
    /// <returns>파싱된 ai.txt 메타데이터</returns>
    Task<AiTxtMetadata> ParseContentAsync(string content, string baseUrl);

    /// <summary>
    /// 특정 AI 에이전트에 대한 접근 권한을 확인합니다.
    /// </summary>
    /// <param name="metadata">ai.txt 메타데이터</param>
    /// <param name="agentName">AI 에이전트 이름</param>
    /// <param name="action">수행하려는 작업</param>
    /// <returns>접근 허용 여부</returns>
    bool IsActionAllowed(AiTxtMetadata metadata, string agentName, AiAction action);

    /// <summary>
    /// AI 에이전트에 대한 사용 제한을 확인합니다.
    /// </summary>
    /// <param name="metadata">ai.txt 메타데이터</param>
    /// <param name="agentName">AI 에이전트 이름</param>
    /// <returns>사용 제한 정보</returns>
    AiUsageLimits? GetUsageLimits(AiTxtMetadata metadata, string agentName);

    /// <summary>
    /// 콘텐츠 라이센스 정보를 가져옵니다.
    /// </summary>
    /// <param name="metadata">ai.txt 메타데이터</param>
    /// <param name="contentPath">콘텐츠 경로</param>
    /// <returns>라이센스 정보</returns>
    ContentLicense? GetContentLicense(AiTxtMetadata metadata, string contentPath);

    /// <summary>
    /// 데이터 사용 정책을 확인합니다.
    /// </summary>
    /// <param name="metadata">ai.txt 메타데이터</param>
    /// <param name="usageType">사용 유형</param>
    /// <returns>사용 정책 정보</returns>
    DataUsagePolicy? GetDataUsagePolicy(AiTxtMetadata metadata, DataUsageType usageType);

    /// <summary>
    /// 연락처 정보를 가져옵니다.
    /// </summary>
    /// <param name="metadata">ai.txt 메타데이터</param>
    /// <returns>연락처 정보</returns>
    ContactInfo? GetContactInfo(AiTxtMetadata metadata);

    /// <summary>
    /// 지원하는 ai.txt 버전을 반환합니다.
    /// </summary>
    IReadOnlyList<string> GetSupportedVersions();

    /// <summary>
    /// ai.txt 파싱 통계를 반환합니다.
    /// </summary>
    AiTxtStatistics GetStatistics();
}