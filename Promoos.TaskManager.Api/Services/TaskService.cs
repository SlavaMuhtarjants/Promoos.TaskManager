using Microsoft.EntityFrameworkCore;

using Promoos.TaskManager.Api.Database;
using Promoos.TaskManager.Api.Dto;
using Promoos.TaskManager.Api.Entities;
using Promoos.TaskManager.Api.Enums;
using Promoos.TaskManager.Api.Results;

namespace Promoos.TaskManager.Api.Services;

public sealed class TaskService(AppDbContext dbContext)
{
    public async Task<Result<Guid>> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskItem
        {
            Id = Guid.Empty,
            Title = request.Name,
            Priority = request.Priority,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await dbContext.Set<TaskItem>().AddAsync(task, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(task.Id);
    }

    public async Task<PagedResponse<TaskResponse>> GetAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<TaskItem>();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TaskResponse(x.Id, x.Title, x.Priority.ToString(), x.IsCompleted, x.CreatedAt, x.CompletedAt))
            .ToArrayAsync(cancellationToken);

        return new PagedResponse<TaskResponse>(items, page, pageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deletedRowsCount = await dbContext.Tasks
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRowsCount == 0
            ? Result.Failure(new Error(ErrorType.NotFound, "The task has not been found"))
            : Result.Success();
    }

    public async Task<Result<TaskResponse>> CompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await dbContext.Set<TaskItem>()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (task == null)
        {
            return Result<TaskResponse>.Failure(new Error(ErrorType.NotFound, "The task has not been found"));
        }

        if (task.IsCompleted)
        {
            return Result<TaskResponse>.Failure(new Error(ErrorType.Validation, "The task has already been completed"));
        }

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<TaskResponse>.Failure(new Error
            (
                ErrorType.Conflict,
                "This task has been modified by another request. Please refresh and try again."
            ));
        }

        return Result<TaskResponse>.Success(new TaskResponse(
            task.Id,
            task.Title,
            task.Priority.ToString(),
            task.IsCompleted,
            task.CreatedAt,
            task.CompletedAt));
    }
}