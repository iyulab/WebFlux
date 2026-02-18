using FluentAssertions;
using WebFlux.Services;

namespace WebFlux.Tests.Services;

public class EventPublisherTests
{
    // --- Subscribe + PublishAsync (async handler) ---

    [Fact]
    public async Task PublishAsync_AsyncHandler_ReceivesEvent()
    {
        var publisher = new EventPublisher();
        UrlProcessedEvent? received = null;

        publisher.Subscribe<UrlProcessedEvent>(evt =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var published = new UrlProcessedEvent { Url = "https://example.com", ContentLength = 1024 };
        await publisher.PublishAsync(published);

        received.Should().NotBeNull();
        received!.Url.Should().Be("https://example.com");
        received.ContentLength.Should().Be(1024);
    }

    [Fact]
    public async Task PublishAsync_MultipleAsyncHandlers_AllReceive()
    {
        var publisher = new EventPublisher();
        var received = new List<string>();

        publisher.Subscribe<UrlProcessedEvent>(_ =>
        {
            received.Add("handler1");
            return Task.CompletedTask;
        });
        publisher.Subscribe<UrlProcessedEvent>(_ =>
        {
            received.Add("handler2");
            return Task.CompletedTask;
        });

        await publisher.PublishAsync(new UrlProcessedEvent { Url = "test" });

        received.Should().HaveCount(2);
        received.Should().Contain(["handler1", "handler2"]);
    }

    [Fact]
    public async Task PublishAsync_DifferentEventType_HandlerNotCalled()
    {
        var publisher = new EventPublisher();
        var called = false;

        publisher.Subscribe<UrlProcessedEvent>(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await publisher.PublishAsync(new UrlProcessingStartedEvent { Url = "test" });

        called.Should().BeFalse();
    }

    // --- Subscribe (sync handler) ---

    [Fact]
    public async Task PublishAsync_SyncHandler_ReceivesEvent()
    {
        var publisher = new EventPublisher();
        UrlProcessingStartedEvent? received = null;
        var tcs = new TaskCompletionSource();

        publisher.Subscribe<UrlProcessingStartedEvent>(evt =>
        {
            received = evt;
            tcs.SetResult();
        });

        await publisher.PublishAsync(new UrlProcessingStartedEvent { Url = "https://example.com" });

        // Sync handlers run on Task.Run background thread, wait briefly
        await Task.WhenAny(tcs.Task, Task.Delay(2000));

        received.Should().NotBeNull();
        received!.Url.Should().Be("https://example.com");
    }

    // --- Unsubscribe via IDisposable ---

    [Fact]
    public async Task Subscribe_Dispose_UnsubscribesHandler()
    {
        var publisher = new EventPublisher();
        var callCount = 0;

        var subscription = publisher.Subscribe<UrlProcessedEvent>(_ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        await publisher.PublishAsync(new UrlProcessedEvent { Url = "first" });
        callCount.Should().Be(1);

        subscription.Dispose();

        await publisher.PublishAsync(new UrlProcessedEvent { Url = "second" });
        callCount.Should().Be(1); // Should not increase
    }

    [Fact]
    public async Task Subscribe_SyncDispose_UnsubscribesHandler()
    {
        var publisher = new EventPublisher();
        var callCount = 0;
        var tcs = new TaskCompletionSource();

        var subscription = publisher.Subscribe<UrlProcessedEvent>(evt =>
        {
            callCount++;
            tcs.TrySetResult();
        });

        await publisher.PublishAsync(new UrlProcessedEvent { Url = "first" });
        await Task.WhenAny(tcs.Task, Task.Delay(2000));
        callCount.Should().Be(1);

        subscription.Dispose();

        // After dispose, sync handler removed from list
        var stats = publisher.GetStatistics();
        stats.SubscriberCount.Should().Be(0);
    }

    // --- Null argument checks ---

    [Fact]
    public async Task PublishAsync_NullEvent_ThrowsArgumentNullException()
    {
        var publisher = new EventPublisher();

        var act = async () => await publisher.PublishAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Subscribe_NullAsyncHandler_ThrowsArgumentNullException()
    {
        var publisher = new EventPublisher();

        var act = () => publisher.Subscribe<UrlProcessedEvent>((Func<UrlProcessedEvent, Task>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Subscribe_NullSyncHandler_ThrowsArgumentNullException()
    {
        var publisher = new EventPublisher();

        var act = () => publisher.Subscribe<UrlProcessedEvent>((Action<UrlProcessedEvent>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // --- Statistics ---

    [Fact]
    public void GetStatistics_NoEvents_ReturnsZeroCounts()
    {
        var publisher = new EventPublisher();

        var stats = publisher.GetStatistics();

        stats.TotalEventsPublished.Should().Be(0);
        stats.SubscriberCount.Should().Be(0);
        stats.PublishErrors.Should().Be(0);
        stats.EventsByType.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatistics_AfterPublish_TracksCounts()
    {
        var publisher = new EventPublisher();
        publisher.Subscribe<UrlProcessedEvent>(_ => Task.CompletedTask);

        await publisher.PublishAsync(new UrlProcessedEvent { Url = "a" });
        await publisher.PublishAsync(new UrlProcessedEvent { Url = "b" });
        await publisher.PublishAsync(new UrlProcessingStartedEvent { Url = "c" });

        var stats = publisher.GetStatistics();

        stats.TotalEventsPublished.Should().Be(3);
        stats.EventsByType.Should().ContainKey("UrlProcessedEvent");
        stats.EventsByType["UrlProcessedEvent"].Should().Be(2);
        stats.EventsByType["UrlProcessingStartedEvent"].Should().Be(1);
    }

    [Fact]
    public async Task GetStatistics_SubscriberCount_ReflectsCurrentState()
    {
        var publisher = new EventPublisher();

        var sub1 = publisher.Subscribe<UrlProcessedEvent>(_ => Task.CompletedTask);
        var sub2 = publisher.Subscribe<UrlProcessedEvent>(_ => Task.CompletedTask);

        publisher.GetStatistics().SubscriberCount.Should().Be(2);

        sub1.Dispose();
        publisher.GetStatistics().SubscriberCount.Should().Be(1);

        sub2.Dispose();
        publisher.GetStatistics().SubscriberCount.Should().Be(0);

        await Task.CompletedTask; // suppress async warning
    }

    // --- SubscribeAll ---

    [Fact]
    public void SubscribeAll_NullHandler_ThrowsArgumentNullException()
    {
        var publisher = new EventPublisher();

        var act = () => publisher.SubscribeAll(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // --- EventSubscription ---

    [Fact]
    public void EventSubscription_DoubleDispose_DoesNotThrow()
    {
        var callCount = 0;
        var subscription = new EventSubscription(() => callCount++);

        subscription.Dispose();
        subscription.Dispose();

        callCount.Should().Be(1); // Unsubscribe action called only once
    }

    // --- CompositeEventSubscription ---

    [Fact]
    public void CompositeEventSubscription_DisposesAll()
    {
        var disposed = new List<string>();
        var sub1 = new EventSubscription(() => disposed.Add("s1"));
        var sub2 = new EventSubscription(() => disposed.Add("s2"));

        var composite = new CompositeEventSubscription([sub1, sub2]);
        composite.Dispose();

        disposed.Should().HaveCount(2);
        disposed.Should().Contain(["s1", "s2"]);
    }

    [Fact]
    public void CompositeEventSubscription_DoubleDispose_OnlyOnce()
    {
        var callCount = 0;
        var sub = new EventSubscription(() => callCount++);

        var composite = new CompositeEventSubscription([sub]);
        composite.Dispose();
        composite.Dispose();

        callCount.Should().Be(1);
    }
}
