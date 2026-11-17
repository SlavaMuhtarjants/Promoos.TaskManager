using Microsoft.AspNetCore.Mvc;

using Promoos.TaskManager.Api.Contracts;
using Promoos.TaskManager.Api.Dto;
using Promoos.TaskManager.Api.Mappers;
using Promoos.TaskManager.Api.Channels;
using Promoos.TaskManager.Api.Services;

namespace Promoos.TaskManager.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks")
            .WithTags("Tasks");

        group.MapPost("/", CreateTaskAsync);

        group.MapGet("/", GetTasksAsync);

        group.MapDelete("/{id:guid}", DeleteTaskAsync);

        group.MapPut("/{id:guid}/complete", CompleteTaskAsync);

        return app;
    }

    private static async Task<IResult> CreateTaskAsync(
        CreateTaskRequest request,
        [FromServices] TaskService taskService,
        CancellationToken cancellationToken)
    {
        var result = await taskService.CreateAsync(request, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTasksAsync(
        [FromServices] TaskService taskService,
        CancellationToken cancellationToken)
    {
        var items = await taskService.GetAsync(cancellationToken);

        return TypedResults.Ok(items);
    }

    private static async Task<IResult> DeleteTaskAsync(
        Guid id,
        [FromServices] TaskService taskService,
        CancellationToken cancellationToken)
    {
        var result = await taskService.DeleteAsync(id, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CompleteTaskAsync(
        Guid id,
        [FromServices] TaskService taskService,
        [FromServices] TaskCompletedEventChannel eventChannel,
        CancellationToken cancellationToken)
    {
        var result = await taskService.CompleteAsync(id, cancellationToken);

        if (result.IsSuccess)
        {
            var message = new TaskCompletedEvent(
                result.Value!.Id,
                result.Value!.Name,
                result.Value.CompletedAt!.Value,
                result.Value.Priority);

            await eventChannel.AddEventAsync(message, cancellationToken);

            return Microsoft.AspNetCore.Http.Results.Ok();
        }

        return result.ToHttpResult();
    }
}