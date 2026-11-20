namespace Promoos.TaskManager.Api.Dto;

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);