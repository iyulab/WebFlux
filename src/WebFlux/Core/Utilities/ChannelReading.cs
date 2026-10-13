using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace WebFlux.Core.Utilities;

/// <summary>
/// Reads a channel the way a streaming method's caller expects when it cancels: no item after the cancellation.
/// <see cref="ChannelReader{T}.ReadAllAsync"/> looks at its token only while it waits, so every item a fast producer
/// already buffered still comes after the caller cancelled, and the cancellation surfaces only after them.
/// </summary>
internal static class ChannelReading
{
    internal static async IAsyncEnumerable<T> ReadUntilCancelledAsync<T>(
        this ChannelReader<T> reader, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (reader.TryRead(out var item))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }
}
