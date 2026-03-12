namespace ToDoConsoleApp.Application.DTOs;

/// <summary>
/// Query DTO for filtering Todo items.
/// </summary>
public class TodoQueryDto
{
    public string? Title { get; set; }
    public bool? IsCompleted { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}