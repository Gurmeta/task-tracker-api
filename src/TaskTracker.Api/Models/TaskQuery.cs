namespace TaskTracker.Api.Models;

public enum TaskSortField
{
    Id,
    CreatedAt,
    DueDate,
    Title
}

// Everything GET /tasks can filter, sort and page by.
public record TaskQuery(
    TaskItemStatus? Status = null,
    string? Search = null,
    bool Overdue = false,
    DateTime? DueBefore = null,
    TaskSortField Sort = TaskSortField.Id,
    bool Descending = false,
    int Limit = 100,
    int Offset = 0);

public record TaskStats(int Total, int Todo, int InProgress, int Done, int Overdue);
