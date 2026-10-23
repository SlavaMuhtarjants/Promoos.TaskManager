namespace Promoos.TaskManager.Api.Contracts;

public sealed record TaskCompletedEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset CompletedAt,
    string Priority);