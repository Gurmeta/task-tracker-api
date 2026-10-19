using TaskTracker.Api.Models;

namespace TaskTracker.Api.Services;

public interface ITaskStore
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(TaskItemStatus? status, int limit, int offset, CancellationToken ct);
    Task<int> CountAsync(TaskItemStatus? status, CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken ct);
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct);
    Task<TaskItem?> UpdateAsync(
        int id, string title, string? description, TaskItemStatus status, DateTime? dueDate, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}
