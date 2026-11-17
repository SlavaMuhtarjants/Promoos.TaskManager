using Promoos.TaskManager.Api.Contracts;

using System.Threading.Channels;

namespace Promoos.TaskManager.Api.Channels;

// In-memory, bounded hand-off between the HTTP endpoints (producers) and TaskCompletedEventPublishingWorker (single consumer).
public sealed class TaskCompletedEventChannel
{
    private readonly Channel<TaskCompletedEvent> _channel = Channel.CreateBounded<TaskCompletedEvent>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public int Count => _channel.Reader.Count;

    public ValueTask AddEventAsync(TaskCompletedEvent message, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<TaskCompletedEvent> ReadEventsAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    // Stops accepting new events; already queued events remain readable so they can be drained on shutdown.
    public void Complete() => _channel.Writer.TryComplete();
}
