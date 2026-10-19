using TaskTracker.Api.Models;

namespace TaskTracker.Api.Services;

public class InMemoryTaskStore : ITaskStore
{
    private readonly List<TaskItem> _tasks = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public IReadOnlyList<TaskItem> GetAll()
    {
        lock (_lock)
        {
            return _tasks.ToList();
        }
    }

    public TaskItem? GetById(int id)
    {
        lock (_lock)
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    public TaskItem Add(TaskItem task)
    {
        lock (_lock)
        {
            task.Id = _nextId++;
            task.CreatedAt = DateTime.UtcNow;
            _tasks.Add(task);
            return task;
        }
    }

    public TaskItem? Update(int id, string title, string? description, TaskItemStatus status)
    {
        lock (_lock)
        {
            var index = _tasks.FindIndex(t => t.Id == id);
            if (index < 0)
            {
                return null;
            }

            // Replace the object instead of changing it in place, so objects already
            // handed out to other requests are never modified while they are being read.
            var updated = new TaskItem
            {
                Id = id,
                Title = title,
                Description = description,
                Status = status,
                CreatedAt = _tasks[index].CreatedAt
            };
            _tasks[index] = updated;
            return updated;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            return _tasks.RemoveAll(t => t.Id == id) > 0;
        }
    }
}
