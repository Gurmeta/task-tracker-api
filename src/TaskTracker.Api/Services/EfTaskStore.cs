using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Data;
using TaskTracker.Api.Models;

namespace TaskTracker.Api.Services;

public class EfTaskStore(AppDbContext db) : ITaskStore
{
    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(
        TaskItemStatus? status, int limit, int offset, CancellationToken ct) =>
        await Filter(status).OrderBy(t => t.Id).Skip(offset).Take(limit).ToListAsync(ct);

    public async Task<int> CountAsync(TaskItemStatus? status, CancellationToken ct) =>
        await Filter(status).CountAsync(ct);

    private IQueryable<TaskItem> Filter(TaskItemStatus? status)
    {
        var query = db.Tasks.AsNoTracking();
        return status is null ? query : query.Where(t => t.Status == status);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct) =>
        await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct)
    {
        task.CreatedAt = DateTime.UtcNow;
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
        task.Status = status;
        task.DueDate = dueDate;
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct) =>
        await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct) > 0;
}
