using TaskTracker.Api.Models;
using TaskTracker.Api.Services;
using TaskTracker.Api.Validation;

namespace TaskTracker.Api.Endpoints;

public static class TaskEndpoints
{
    // Extension method: lets Program.cs call app.MapTaskEndpoints().
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/tasks").WithTags("Tasks");

        // GET /tasks or GET /tasks?status=Done
        group.MapGet("/", async (TaskItemStatus? status, ITaskStore store, CancellationToken ct) =>
            Results.Ok(await store.GetAllAsync(status, ct)))
            .WithSummary("List tasks, optionally filtered by status")
            .Produces<IReadOnlyList<TaskItem>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:int}", async (int id, ITaskStore store, CancellationToken ct) =>
        {
            var task = await store.GetByIdAsync(id, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        })
            .WithSummary("Get one task")
            .Produces<TaskItem>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateTaskRequest request, ITaskStore store, CancellationToken ct) =>
        {
            if (!RequestValidator.TryValidate(request, out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var created = await store.AddAsync(new TaskItem
            {
                Title = request.Title!.Trim(),
                Description = request.Description?.Trim(),
                DueDate = request.DueDate
            }, ct);
            return Results.Created($"/tasks/{created.Id}", created);
        })
            .WithSummary("Create a task")
            .Produces<TaskItem>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", async (int id, UpdateTaskRequest request, ITaskStore store, CancellationToken ct) =>
        {
            if (!RequestValidator.TryValidate(request, out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var updated = await store.UpdateAsync(
                id, request.Title!.Trim(), request.Description?.Trim(), request.Status!.Value, request.DueDate, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
            .WithSummary("Replace a task")
            .Produces<TaskItem>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", async (int id, ITaskStore store, CancellationToken ct) =>
            await store.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithSummary("Delete a task")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return routes;
    }
}
