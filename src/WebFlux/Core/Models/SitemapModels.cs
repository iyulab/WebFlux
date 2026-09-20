namespace WebFlux.Core.Models;

/// <summary>
/// 변경 빈도
/// </summary>
public enum ChangeFrequency
{
    /// <summary>
    /// 항상
    /// </summary>
    Always,

    /// <summary>
    /// 시간마다
    /// </summary>
    Hourly,

    /// <summary>
    /// 일일
    /// </summary>
    Daily,

    /// <summary>
    /// 주간
    /// </summary>
    Weekly,

    /// <summary>
    /// 월간
    /// </summary>
    Monthly,

    /// <summary>
    /// 연간
    /// </summary>
    Yearly,

    /// <summary>
    /// 절대 변경되지 않음
    /// </summary>
    Never
}
