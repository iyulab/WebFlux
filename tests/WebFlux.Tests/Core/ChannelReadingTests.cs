using System.Threading.Channels;
using WebFlux.Core.Utilities;
using Xunit;

namespace WebFlux.Tests.Core;

/// <summary>
/// A streaming method's caller that cancels gets no item afterwards. <c>ChannelReader.ReadAllAsync</c> drains what a fast
/// producer already buffered, ignoring the token, and throws only when it next waits.
/// </summary>
public sealed class ChannelReadingTests
{
    [Fact]
    public async Task ReadUntilCancelled_StopsAtTheCancellation_EvenWithItemsBuffered()
    {
        var channel = Channel.CreateUnbounded<int>();
        for (var i = 0; i < 100; i++)
            await channel.Writer.WriteAsync(i, TestContext.Current.CancellationToken);
        channel.Writer.Complete();
        using var cts = new CancellationTokenSource();
        var read = new List<int>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in channel.Reader.ReadUntilCancelledAsync(cts.Token))
            {
                read.Add(item);
                if (read.Count == 5)
                    await cts.CancelAsync();
            }
        });

        Assert.Equal([0, 1, 2, 3, 4], read);
    }

    [Fact]
    public async Task ReadAllAsync_DrainsTheBuffer_AfterTheCancellation()
    {
        // The defect this replaces, pinned: if this starts throwing, the BCL changed and the helper may be redundant.
        var channel = Channel.CreateUnbounded<int>();
        for (var i = 0; i < 100; i++)
            await channel.Writer.WriteAsync(i, TestContext.Current.CancellationToken);
        channel.Writer.Complete();
        using var cts = new CancellationTokenSource();
        var read = 0;

        // It drains every buffered item, and only then, waiting for more, throws.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cts.Token))
            {
                if (++read == 5)
                    await cts.CancelAsync();
            }
        });

        Assert.Equal(100, read);
    }

    [Fact]
    public async Task ReadUntilCancelled_ReadsEverything_WhenNotCancelled()
    {
        var channel = Channel.CreateUnbounded<int>();
        for (var i = 0; i < 10; i++)
            await channel.Writer.WriteAsync(i, TestContext.Current.CancellationToken);
        channel.Writer.Complete();
        var read = new List<int>();

        await foreach (var item in channel.Reader.ReadUntilCancelledAsync(TestContext.Current.CancellationToken))
            read.Add(item);

        Assert.Equal(Enumerable.Range(0, 10), read);
    }
}
