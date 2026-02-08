namespace WebFlux.Core.Models;

/// <summary>
/// 검증 결과
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 검증 성공 여부
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 오류 목록
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 경고 목록
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
