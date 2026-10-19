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
        group.MapGet("/", (TaskItemStatus? status, ITaskStore store) =>
        {
            var tasks = store.GetAll();
            return Results.Ok(status is null ? tasks : tasks.Where(t => t.Status == status));
        });

        group.MapGet("/{id:int}", (int id, ITaskStore store) =>
        {
            var task = store.GetById(id);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        group.MapPost("/", (CreateTaskRequest request, ITaskStore store) =>
        {
            if (!RequestValidator.TryValidate(request, out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var created = store.Add(new TaskItem
            {
                Title = request.Title!.Trim(),
                Description = request.Description?.Trim()
            });
            return Results.Created($"/tasks/{created.Id}", created);
        });

        group.MapPut("/{id:int}", (int id, UpdateTaskRequest request, ITaskStore store) =>
        {
            if (!RequestValidator.TryValidate(request, out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var updated = store.Update(id, request.Title!.Trim(), request.Description?.Trim(), request.Status!.Value);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:int}", (int id, ITaskStore store) =>
            store.Delete(id) ? Results.NoContent() : Results.NotFound());

        return routes;
    }
}
