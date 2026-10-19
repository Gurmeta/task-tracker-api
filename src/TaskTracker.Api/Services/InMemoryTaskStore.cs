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
}
