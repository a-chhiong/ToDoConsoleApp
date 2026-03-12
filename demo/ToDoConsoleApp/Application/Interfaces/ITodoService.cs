using ToDoConsoleApp.Application.DTOs;
using ToDoConsoleApp.Domain.Entities;
using ToDoConsoleApp.Utils;

namespace ToDoConsoleApp.Application.Interfaces;

/// <summary>
/// High-level service for todo operations.
/// Coordinates between presentation and repository layers.
/// </summary>
public interface ITodoService
{
    Task<ResultWrapper<IEnumerable<TodoItem>>> GetAllTodosAsync(CancellationToken cancellationToken = default);
    Task<ResultWrapper<IEnumerable<TodoItem>>> SearchTodosAsync(string? title = null, bool? isCompleted = null, CancellationToken cancellationToken = default);
    Task<ResultWrapper<TodoItem>> GetTodoByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ResultWrapper<int>> CreateTodoAsync(CreateTodoDto dto, CancellationToken cancellationToken = default);
    Task<ResultWrapper<bool>> UpdateTodoAsync(int id, CreateTodoDto dto, CancellationToken cancellationToken = default);
    Task<ResultWrapper<bool>> DeleteTodoAsync(int id, CancellationToken cancellationToken = default);
    Task<ResultWrapper<TodoStatisticsDto>> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class TodoStatisticsDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public double CompletionRate { get; set; }
}