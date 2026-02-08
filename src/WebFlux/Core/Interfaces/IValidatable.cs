using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 옵션 검증을 위한 인터페이스
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// 옵션 값을 검증합니다
    /// </summary>
    /// <returns>검증 결과</returns>
    ValidationResult Validate();
}
