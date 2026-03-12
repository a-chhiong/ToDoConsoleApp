using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Application.DTOs;
using ToDoConsoleApp.Application.Interfaces;
using ToDoConsoleApp.Domain.Entities;
using ToDoConsoleApp.Utils;

namespace ToDoConsoleApp.Application.Services;

/// <summary>
/// Business logic service for Todo operations.
/// Implements ITodoService with repository coordination.
/// </summary>
public class TodoService : ITodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TodoService> _logger;

    public TodoService(IUnitOfWork unitOfWork, ILogger<TodoService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResultWrapper<IEnumerable<TodoItem>>> GetAllTodosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all todos");
            var todos = await _unitOfWork.TodoRepository.GetAllAsync(cancellationToken);
            return ResultWrapper<IEnumerable<TodoItem>>.Success(todos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all todos");
            return ResultWrapper<IEnumerable<TodoItem>>.Failure("Failed to fetch todos", ex.Message);
        }
    }

    public async Task<ResultWrapper<IEnumerable<TodoItem>>> SearchTodosAsync(string? title = null, bool? isCompleted = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching todos with filters - Title: {Title}, Completed: {Completed}", title, isCompleted);
            var todos = await _unitOfWork.TodoRepository.GetFilteredAsync(title, isCompleted, cancellationToken);
            return ResultWrapper<IEnumerable<TodoItem>>.Success(todos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching todos");
            return ResultWrapper<IEnumerable<TodoItem>>.Failure("Failed to search todos", ex.Message);
        }
    }

    public async Task<ResultWrapper<TodoItem>> GetTodoByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id <= 0)
                return ResultWrapper<TodoItem>.Failure("Invalid ID", "Todo ID must be greater than zero");

            _logger.LogInformation("Fetching todo with ID: {TodoId}", id);
            var todo = await _unitOfWork.TodoRepository.GetByIdAsync(id, cancellationToken);

            if (todo == null)
                return ResultWrapper<TodoItem>.Failure("Not found", $"Todo with ID {id} not found");

            return ResultWrapper<TodoItem>.Success(todo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching todo with ID: {TodoId}", id);
            return ResultWrapper<TodoItem>.Failure("Failed to fetch todo", ex.Message);
        }
    }

    public async Task<ResultWrapper<int>> CreateTodoAsync(CreateTodoDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (dto == null)
                return ResultWrapper<int>.Failure("Invalid input", "Todo data is required");

            if (!dto.IsValid(out var errors))
                return ResultWrapper<int>.Failure("Validation failed", string.Join(", ", errors));

            var todo = new TodoItem
            {
                Title = dto.Title,
                Description = dto.Description,
                IsCompleted = dto.IsCompleted,
                CreatedAt = DateTime.UtcNow
            };

            if (!todo.IsValid(out var entityErrors))
                return ResultWrapper<int>.Failure("Validation failed", string.Join(", ", entityErrors));

            _logger.LogInformation("Creating new todo: {TodoTitle}", todo.Title);
            var id = await _unitOfWork.TodoRepository.CreateAsync(todo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Todo created with ID: {TodoId}", id);
            return ResultWrapper<int>.Success(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating todo");
            return ResultWrapper<int>.Failure("Failed to create todo", ex.Message);
        }
    }

    public async Task<ResultWrapper<bool>> UpdateTodoAsync(int id, CreateTodoDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id <= 0)
                return ResultWrapper<bool>.Failure("Invalid ID", "Todo ID must be greater than zero");

            if (dto == null)
                return ResultWrapper<bool>.Failure("Invalid input", "Todo data is required");

            if (!dto.IsValid(out var errors))
                return ResultWrapper<bool>.Failure("Validation failed", string.Join(", ", errors));

            var existingTodo = await _unitOfWork.TodoRepository.GetByIdAsync(id, cancellationToken);
            if (existingTodo == null)
                return ResultWrapper<bool>.Failure("Not found", $"Todo with ID {id} not found");

            existingTodo.Title = dto.Title;
            existingTodo.Description = dto.Description;
            
            if (dto.IsCompleted && !existingTodo.IsCompleted)
                existingTodo.CompletedAt = DateTime.UtcNow;
            
            existingTodo.IsCompleted = dto.IsCompleted;

            _logger.LogInformation("Updating todo with ID: {TodoId}", id);
            var updated = await _unitOfWork.TodoRepository.UpdateAsync(existingTodo, cancellationToken);
            
            if (updated)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResultWrapper<bool>.Success(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating todo with ID: {TodoId}", id);
            return ResultWrapper<bool>.Failure("Failed to update todo", ex.Message);
        }
    }

    public async Task<ResultWrapper<bool>> DeleteTodoAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id <= 0)
                return ResultWrapper<bool>.Failure("Invalid ID", "Todo ID must be greater than zero");

            _logger.LogInformation("Deleting todo with ID: {TodoId}", id);
            var deleted = await _unitOfWork.TodoRepository.DeleteAsync(id, cancellationToken);
            
            if (deleted)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!deleted)
                return ResultWrapper<bool>.Failure("Not found", $"Todo with ID {id} not found");

            _logger.LogInformation("Todo deleted: {TodoId}", id);
            return ResultWrapper<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting todo with ID: {TodoId}", id);
            return ResultWrapper<bool>.Failure("Failed to delete todo", ex.Message);
        }
    }

    public async Task<ResultWrapper<TodoStatisticsDto>> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching todo statistics");
            var todos = await _unitOfWork.TodoRepository.GetAllAsync(cancellationToken);
            var todoList = todos.ToList();

            var total = todoList.Count;
            var completed = todoList.Count(t => t.IsCompleted);
            var pending = total - completed;

            var stats = new TodoStatisticsDto
            {
                Total = total,
                Completed = completed,
                Pending = pending,
                CompletionRate = total > 0 ? (completed * 100.0 / total) : 0
            };

            return ResultWrapper<TodoStatisticsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching statistics");
            return ResultWrapper<TodoStatisticsDto>.Failure("Failed to fetch statistics", ex.Message);
        }
    }
}