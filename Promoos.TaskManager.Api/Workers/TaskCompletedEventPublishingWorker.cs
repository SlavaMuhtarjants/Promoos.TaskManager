using Promoos.TaskManager.Api.Channels;
using Promoos.TaskManager.Api.Publishers;

namespace Promoos.TaskManager.Api.Workers;

public sealed class TaskCompletedEventPublishingWorker(
    TaskCompletedEventChannel eventChannel,
    ITaskEventPublisher publisher,
    ILogger<TaskCompletedEventPublishingWorker> logger) : BackgroundService, IAsyncDisposable
{
    // Max time to drain queued events on shutdown before the remaining ones are abandoned.
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    // Cancelled only when draining exceeds ShutdownTimeout; stops the read loop and any in-flight publication.
    private readonly CancellationTokenSource _abortCts = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // stoppingToken is deliberately ignored: queued events must be drained on shutdown.
        // The loop ends when StopAsync/DisposeAsync completes the channel and it becomes empty.
        try
        {
            await foreach (var message in eventChannel.ReadEventsAsync(_abortCts.Token))
            {
                try
                {
                    await publisher.PublishCompletedAsync(message, _abortCts.Token);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unexpected failure publishing event for {TaskId}", message.TaskId);
                }
            }
        }
        catch (OperationCanceledException) when (_abortCts.IsCancellationRequested)
        {
            logger.LogWarning(
                "Shutdown timed out; {Count} queued task-completed events were not published",
                eventChannel.Count);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        eventChannel.Complete();

        if (ExecuteTask is { } executing && await Task.WhenAny(executing, Task.Delay(ShutdownTimeout)) != executing)
        {
            _abortCts.Cancel();
        }

        await base.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        eventChannel.Complete();

        if (ExecuteTask is { IsCompleted: false } executing)
        {
            if (await Task.WhenAny(executing, Task.Delay(ShutdownTimeout)) != executing)
            {
                _abortCts.Cancel();
            }

            try
            {
                await executing;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Publishing worker failed while shutting down");
            }
        }

        _abortCts.Dispose();
    }
}
