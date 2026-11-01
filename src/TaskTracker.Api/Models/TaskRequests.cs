using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Api.Models;

// Request DTOs: what the client is allowed to send. Id and CreatedAt are not part of them.
public record CreateTaskRequest
{
    [Required, StringLength(100)]
    public string? Title { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public DateTime? DueDate { get; init; }
}

public record UpdateTaskRequest
{
    [Required, StringLength(100)]
    public string? Title { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    // Nullable + Required: a missing status is an error instead of silently becoming Todo.
    [Required]
    public TaskItemStatus? Status { get; init; }

    public DateTime? DueDate { get; init; }
}

// Partial update: only the properties present in the JSON body are changed.
// A property that is absent (or null) is left alone. To clear a due date, use PUT.
public record PatchTaskRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? Title { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public TaskItemStatus? Status { get; init; }

    public DateTime? DueDate { get; init; }
}
