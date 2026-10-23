namespace Promoos.TaskManager.Api.Dto;

public sealed record TaskResponse(
    Guid Id,
    string Name,
    string Priority,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);