using TaskTracker.Api.Models;

namespace TaskTracker.Api.Services;

public interface ITaskStore
{
    IReadOnlyList<TaskItem> GetAll();
    TaskItem? GetById(int id);
    TaskItem Add(TaskItem task);
}
