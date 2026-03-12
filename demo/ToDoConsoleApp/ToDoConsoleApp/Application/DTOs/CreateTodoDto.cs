namespace ToDoConsoleApp.Application.DTOs;

/// <summary>
/// Data Transfer Object for creating or updating Todo items.
/// </summary>
public class CreateTodoDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Title is required.");

        if (Title?.Length > 255)
            errors.Add("Title cannot exceed 255 characters.");

        return errors.Count == 0;
    }
}