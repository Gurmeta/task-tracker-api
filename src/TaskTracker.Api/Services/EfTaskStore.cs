using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Data;
using TaskTracker.Api.Models;

namespace TaskTracker.Api.Services;

public class EfTaskStore(AppDbContext db) : ITaskStore
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(TaskQuery query, CancellationToken ct) =>
        await Sort(Filter(query), query).Skip(query.Offset).Take(query.Limit).ToListAsync(ct);

    public async Task<int> CountAsync(TaskQuery query, CancellationToken ct) =>
        await Filter(query).CountAsync(ct);

    public async Task<TaskStats> GetStatsAsync(CancellationToken ct)
    {
        var byStatus = await db.Tasks.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Count(TaskItemStatus status) => byStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

        var now = DateTime.UtcNow;
        var overdue = await db.Tasks.AsNoTracking()
            .CountAsync(t => t.DueDate < now && t.Status != TaskItemStatus.Done, ct);

        return new TaskStats(
            byStatus.Sum(x => x.Count),
            Count(TaskItemStatus.Todo),
            Count(TaskItemStatus.InProgress),
            Count(TaskItemStatus.Done),
            overdue);
    }

    private IQueryable<TaskItem> Filter(TaskQuery q)
    {
        var query = db.Tasks.AsNoTracking();

        if (q.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            // LIKE is case-insensitive for ASCII in SQLite. % and _ typed by the client are escaped
            // so they are searched for literally instead of acting as wildcards.
            var pattern = $"%{EscapeLike(q.Search.Trim())}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Title, pattern, "\\") ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern, "\\")));
        }

        if (q.Overdue)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t => t.DueDate < now && t.Status != TaskItemStatus.Done);
        }

        if (q.DueBefore is { } dueBefore)
        {
            query = query.Where(t => t.DueDate < dueBefore);
        }

        return query;
    }

    // Id is always the last sort key, so pages are stable when other values are equal.
    private static IQueryable<TaskItem> Sort(IQueryable<TaskItem> query, TaskQuery q) => (q.Sort, q.Descending) switch
    {
        (TaskSortField.CreatedAt, false) => query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
        (TaskSortField.CreatedAt, true) => query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id),
        (TaskSortField.DueDate, false) => query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ThenBy(t => t.Id),
        (TaskSortField.DueDate, true) => query.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate).ThenBy(t => t.Id),
        (TaskSortField.Title, false) => query.OrderBy(t => t.Title).ThenBy(t => t.Id),
        (TaskSortField.Title, true) => query.OrderByDescending(t => t.Title).ThenBy(t => t.Id),
        (_, true) => query.OrderByDescending(t => t.Id),
        _ => query.OrderBy(t => t.Id)
    };

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct)
    {
        task.CreatedAt = DateTime.UtcNow;
        task.CompletedAt = task.Status == TaskItemStatus.Done ? task.CreatedAt : null;
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem?> UpdateAsync(
        int id, string title, string? description, TaskItemStatus status, DateTime? dueDate, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return null;
        }

        task.Title = title;
        task.Description = description;
        task.DueDate = dueDate;
        Apply(task, status);
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem?> PatchAsync(
        int id, string? title, string? description, TaskItemStatus? status, DateTime? dueDate, CancellationToken ct)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return null;
        }

        task.Title = title ?? task.Title;
        task.Description = description ?? task.Description;
        task.DueDate = dueDate ?? task.DueDate;
        Apply(task, status ?? task.Status);
        await db.SaveChangesAsync(ct);
        return task;
    }

    // Sets the status together with the timestamps that depend on it.
    private static void Apply(TaskItem task, TaskItemStatus status)
    {
        var now = DateTime.UtcNow;
        if (status == TaskItemStatus.Done && task.Status != TaskItemStatus.Done)
        {
            task.CompletedAt = now;
        }
        else if (status != TaskItemStatus.Done)
        {
            task.CompletedAt = null;
        }

        task.Status = status;
        task.UpdatedAt = now;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct) =>
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct) > 0;
}
