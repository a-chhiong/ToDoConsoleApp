namespace ToDoConsoleApp.Domain.Entities;

/// <summary>
/// Core domain entity representing a Todo item.
/// Business logic independent of infrastructure concerns.
/// </summary>
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public override string ToString() => 
        $"[{Id}] {Title} - {(IsCompleted ? "✓ Completed" : "○ Pending")} ({CreatedAt:g})";

    /// <summary>
    /// Validates the todo item before persistence.
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title cannot be empty.");

        if (Title.Length > 255)
            errors.Add("Title cannot exceed 255 characters.");

        return errors.Count == 0;
    }
}