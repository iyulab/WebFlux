namespace WebFlux.Core.Interfaces;

/// <summary>
/// 메인 웹 콘텐츠 처리 파이프라인 인터페이스 (ISP 파사드)
/// IContentExtractService + IContentChunkService를 상속하며,
/// 작업 관리/진단 메서드를 직접 보유
/// </summary>
public interface IWebContentProcessor : IContentExtractService, IContentChunkService
{
    /// <summary>
    /// 사용 가능한 청킹 전략 목록을 반환합니다
    /// </summary>
    /// <returns>사용 가능한 청킹 전략 목록</returns>
    IReadOnlyList<string> GetAvailableChunkingStrategies();
}

