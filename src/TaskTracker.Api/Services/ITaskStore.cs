using TaskTracker.Api.Models;

namespace TaskTracker.Api.Services;

public interface ITaskStore
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(TaskQuery query, CancellationToken ct);
    Task<int> CountAsync(TaskQuery query, CancellationToken ct);
    Task<TaskStats> GetStatsAsync(CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct);
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct);
    Task<TaskItem?> UpdateAsync(
        int id, string title, string? description, TaskItemStatus status, DateTime? dueDate, CancellationToken ct);
    Task<TaskItem?> PatchAsync(
        int id, string? title, string? description, TaskItemStatus? status, DateTime? dueDate, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
