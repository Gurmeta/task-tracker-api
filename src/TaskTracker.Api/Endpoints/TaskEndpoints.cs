using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Models;
using TaskTracker.Api.Services;
using TaskTracker.Api.Validation;

namespace TaskTracker.Api.Endpoints;

public static class TaskEndpoints
{
    // A list never returns an unbounded number of rows, however large the table grows.
    private const int DefaultLimit = 100;
    private const int MaxLimit = 500;
    private const int MaxSearchLength = 100;

    // Task requests are tiny (a title and a description). Anything bigger is rejected with 413.
    private const long MaxRequestBodyBytes = 64 * 1024;

    // Extension method: lets Program.cs call app.MapTaskEndpoints().
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/tasks")
            .WithTags("Tasks")
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBodyBytes));

        // GET /tasks?status=Done&search=docs&overdue=true&sort=dueDate&order=desc&limit=20&offset=40
        group.MapGet("/", async (
            TaskItemStatus? status, string? search, bool? overdue, DateTime? dueBefore,
            string? sort, string? order, int? limit, int? offset,
            HttpContext http, ITaskStore store, CancellationToken ct) =>
        {
            var pageLimit = limit ?? DefaultLimit;
            var pageOffset = offset ?? 0;

            var errors = new Dictionary<string, string[]>();
            if (pageLimit is < 1 or > MaxLimit)
            {
                errors["limit"] = [$"limit must be between 1 and {MaxLimit}."];
            }
            if (pageOffset < 0)
            {
                errors["offset"] = ["offset must be 0 or greater."];
            }

            var sortField = TaskSortField.Id;
            if (sort is not null && !Enum.TryParse(sort, ignoreCase: true, out sortField))
            {
                errors["sort"] = ["sort must be one of: id, createdAt, dueDate, title."];
            }

            var descending = false;
            if (order is not null)
            {
                if (order.Equals("desc", StringComparison.OrdinalIgnoreCase)) descending = true;
                else if (!order.Equals("asc", StringComparison.OrdinalIgnoreCase)) errors["order"] = ["order must be asc or desc."];
            }

            if (search is { Length: > MaxSearchLength })
            {
                errors["search"] = [$"search must be at most {MaxSearchLength} characters."];
            }
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var query = new TaskQuery(
                status, search, overdue ?? false, dueBefore, sortField, descending, pageLimit, pageOffset);

            // The total lets a client know whether there are more pages to ask for.
            http.Response.Headers["X-Total-Count"] = (await store.CountAsync(query, ct)).ToString();
            return Results.Ok(await store.GetAllAsync(query, ct));
        })
            .WithSummary($"List tasks: filter, search, sort and page (up to {MaxLimit} per page)")
            .WithDescription(
                $"Returns at most `limit` tasks (default {DefaultLimit}, maximum {MaxLimit}) starting after `offset` tasks. " +
                "Filters: `status`, `search` (title or description, case-insensitive), `overdue=true` (past due date and not Done), " +
                "`dueBefore`. Sorting: `sort` = id (default), createdAt, dueDate, title and `order` = asc (default) or desc. " +
                "Tasks without a due date always come last when sorting by dueDate. " +
                "The `X-Total-Count` response header has the total number of matching tasks.")
            .Produces<IReadOnlyList<TaskItem>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/stats", async (ITaskStore store, CancellationToken ct) =>
            Results.Ok(await store.GetStatsAsync(ct)))
            .WithSummary("Task counts by status and the number of overdue tasks")
            .Produces<TaskStats>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", async (int id, ITaskStore store, CancellationToken ct) =>
        {
            var task = await store.GetByIdAsync(id, ct);
            return task is null ? TaskNotFound(id) : Results.Ok(task);
        })
            .WithSummary("Get one task")
            .Produces<TaskItem>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
            return updated is null ? TaskNotFound(id) : Results.Ok(updated);
        })
            .WithSummary("Replace a task")
            .Produces<TaskItem>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}", async (int id, PatchTaskRequest request, ITaskStore store, CancellationToken ct) =>
        {
            if (!RequestValidator.TryValidate(request, out var errors))
            {
                return Results.ValidationProblem(errors);
            }

            var title = request.Title?.Trim();
            if (title is { Length: 0 })
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = ["The title cannot be empty."]
                });
            }

            if (title is null && request.Description is null && request.Status is null && request.DueDate is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [""] = ["Send at least one of: title, description, status, dueDate."]
                });
            }

            var patched = await store.PatchAsync(
                id, title, request.Description?.Trim(), request.Status, request.DueDate, ct);
            return patched is null ? TaskNotFound(id) : Results.Ok(patched);
        })
            .WithSummary("Change only the properties that are sent")
            .Produces<TaskItem>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", async (int id, ITaskStore store, CancellationToken ct) =>
            await store.DeleteAsync(id, ct) ? Results.NoContent() : TaskNotFound(id))
            .WithSummary("Delete a task")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }

    // Same error format (RFC 9457 problem details) as the validation errors, so every
    // error response has a JSON body that says what went wrong.
    private static IResult TaskNotFound(int id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Task not found",
            detail: $"Task {id} was not found.");
}
