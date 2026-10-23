using Promoos.TaskManager.Api.Contracts;

namespace Promoos.TaskManager.Api.Publishers;

public interface ITaskEventPublisher
{
    Task PublishCompletedAsync(
        TaskCompletedEvent message,
        CancellationToken cancellationToken = default);
}