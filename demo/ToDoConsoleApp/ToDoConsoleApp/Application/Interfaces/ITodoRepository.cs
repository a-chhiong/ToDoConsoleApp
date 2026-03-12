using ToDoConsoleApp.Domain.Entities;

namespace ToDoConsoleApp.Application.Interfaces;

/// <summary>
/// Repository interface for Todo CRUD operations.
/// Defines contracts independent of infrastructure implementation.
/// </summary>
public interface ITodoRepository
{
    Task<IEnumerable<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TodoItem>> GetFilteredAsync(string? title = null, bool? isCompleted = null, CancellationToken cancellationToken = default);
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(TodoItem todoItem, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(bool? isCompleted = null, CancellationToken cancellationToken = default);
}