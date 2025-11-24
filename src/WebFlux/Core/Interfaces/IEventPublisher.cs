using WebFlux.Core.Models;

namespace WebFlux.Core.Interfaces;

/// <summary>
/// 이벤트 발행자 인터페이스
/// 시스템 내에서 발생하는 이벤트를 발행하고 구독자에게 전달
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// 이벤트를 발행합니다.
    /// </summary>
    /// <param name="processingEvent">발행할 이벤트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>발행 작업</returns>
    Task PublishAsync(ProcessingEvent processingEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 이벤트를 동기적으로 발행합니다.
    /// </summary>
    /// <param name="processingEvent">발행할 이벤트</param>
    void Publish(ProcessingEvent processingEvent);

    /// <summary>
    /// 이벤트 구독을 등록합니다.
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제용 IDisposable</returns>
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : ProcessingEvent;

    /// <summary>
    /// 이벤트 구독을 등록합니다. (동기)
    /// </summary>
    /// <typeparam name="T">이벤트 타입</typeparam>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제용 IDisposable</returns>
    IDisposable Subscribe<T>(Action<T> handler) where T : ProcessingEvent;

    /// <summary>
    /// 모든 이벤트를 구독합니다.
    /// </summary>
    /// <param name="handler">이벤트 핸들러</param>
    /// <returns>구독 해제용 IDisposable</returns>
    IDisposable SubscribeAll(Func<ProcessingEvent, Task> handler);

    /// <summary>
    /// 이벤트 발행 통계를 반환합니다.
    /// </summary>
    /// <returns>발행 통계</returns>
    EventPublishingStatistics GetStatistics();
}

/// <summary>
/// 이벤트 발행 통계
/// </summary>
public class EventPublishingStatistics
{
    /// <summary>총 발행된 이벤트 수</summary>
    public long TotalEventsPublished { get; init; }

    /// <summary>이벤트 타입별 발행 수</summary>
    public IReadOnlyDictionary<string, long> EventsByType { get; init; } =
        new Dictionary<string, long>();

    /// <summary>구독자 수</summary>
    public int SubscriberCount { get; init; }

    /// <summary>평균 발행 시간 (밀리초)</summary>
    public double AveragePublishTimeMs { get; init; }

    /// <summary>발행 오류 수</summary>
    public long PublishErrors { get; init; }

    /// <summary>마지막 업데이트 시간</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}