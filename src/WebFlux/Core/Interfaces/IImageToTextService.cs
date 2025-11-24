using WebFlux.Core.Options;
using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 이미지-텍스트 변환 서비스 인터페이스
/// 소비 애플리케이션에서 GPT-4V, Claude, Gemini 등의 구현체 제공
/// </summary>
public interface IImageToTextService
{
    /// <summary>
    /// 이미지 URL을 텍스트 설명으로 변환합니다.
    /// </summary>
    /// <param name="imageUrl">변환할 이미지 URL</param>
    /// <param name="options">변환 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>이미지에 대한 텍스트 설명</returns>
    Task<string> ConvertImageToTextAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 이미지 바이트 배열을 텍스트 설명으로 변환합니다.
    /// </summary>
    /// <param name="imageBytes">변환할 이미지 바이트 배열</param>
    /// <param name="mimeType">이미지 MIME 타입 (예: "image/jpeg")</param>
    /// <param name="options">변환 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>이미지에 대한 텍스트 설명</returns>
    Task<string> ConvertImageToTextAsync(
        byte[] imageBytes,
        string mimeType,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 여러 이미지를 배치로 처리합니다.
    /// </summary>
    /// <param name="imageUrls">처리할 이미지 URL 목록</param>
    /// <param name="options">변환 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>각 이미지에 대한 텍스트 설명 목록</returns>
    Task<IReadOnlyList<string>> ConvertImagesBatchAsync(
        IEnumerable<string> imageUrls,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 이미지에서 특정 텍스트를 추출합니다 (OCR).
    /// </summary>
    /// <param name="imageUrl">텍스트를 추출할 이미지 URL</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>추출된 텍스트</returns>
    Task<string> ExtractTextFromImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 지원하는 이미지 형식을 반환합니다.
    /// </summary>
    /// <returns>지원하는 MIME 타입 목록</returns>
    IReadOnlyList<string> GetSupportedImageFormats();

    /// <summary>
    /// 웹 이미지에서 텍스트를 추출하고 상세한 결과를 반환합니다. (Phase 5A.2 멀티모달 파이프라인)
    /// </summary>
    /// <param name="imageUrl">텍스트를 추출할 웹 이미지 URL</param>
    /// <param name="options">변환 옵션</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>상세한 이미지-텍스트 변환 결과</returns>
    Task<ImageToTextResult> ExtractTextFromWebImageAsync(
        string imageUrl,
        ImageToTextOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 서비스의 사용 가능 여부를 확인합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>사용 가능 여부</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}