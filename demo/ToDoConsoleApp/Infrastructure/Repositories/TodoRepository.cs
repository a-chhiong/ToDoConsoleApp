using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Application.Interfaces;
using ToDoConsoleApp.Domain.Entities;
using ToDoConsoleApp.Utils;

namespace ToDoConsoleApp.Infrastructure.Repositories;

/// <summary>
/// Dapper-based implementation of ITodoRepository.
/// Uses Dapper.SqlBuilder for dynamic query construction.
/// All database operations use parameterized queries (SQL injection safe).
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly SqlConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly SqlScriptLoader _scriptLoader;
    private readonly ILogger<TodoRepository>? _logger;
    
    public TodoRepository(
        SqlConnection connection,
        IDbTransaction? transaction = null,
        SqlScriptLoader? scriptLoader = null,
        ILogger<TodoRepository>? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction;
        _scriptLoader = scriptLoader ?? new SqlScriptLoader();
        _logger = logger;
    }

    public async Task<IEnumerable<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Fetching all todo items");
            var sql = _scriptLoader.LoadScript("Todos/GetAll.sql");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var items = await _connection.QueryAsync<TodoItem>(
                sql, 
                transaction: _transaction
            );

            _logger?.LogInformation("Retrieved {Count} todo items", items.Count());
            return items;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching all todo items");
            throw;
        }
    }

    /// <summary>
    /// Gets filtered todos using Dapper.SqlBuilder for dynamic query construction.
    /// Demonstrates best practice for optional WHERE clauses.
    /// </summary>
    public async Task<IEnumerable<TodoItem>> GetFilteredAsync(
        string? title = null, 
        bool? isCompleted = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Fetching filtered todos - Title: {Title}, Completed: {Completed}", title, isCompleted);

            // Create base query template
            var builder = new SqlBuilder();
            var template = builder.AddTemplate(@"
                SELECT [Id], [Title], [Description], [IsCompleted], [CreatedAt], [CompletedAt]
                FROM [dbo].[Todos]
                /**where**/
                /**orderby**/
            ");

            // Dynamically add WHERE conditions based on parameters
            if (!string.IsNullOrEmpty(title))
                builder.Where("[Title] LIKE @Title", new { Title = $"%{title}%" });

            if (isCompleted.HasValue)
                builder.Where("[IsCompleted] = @IsCompleted", new { IsCompleted = isCompleted.Value });

            // Add ORDER BY
            builder.OrderBy("[CreatedAt] DESC");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var items = await _connection.QueryAsync<TodoItem>(
                template.RawSql,
                template.Parameters,
                transaction: _transaction
            );

            _logger?.LogDebug("Retrieved {Count} filtered todos", items.Count());
            return items;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching filtered todos");
            throw;
        }
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Fetching todo item with ID: {TodoId}", id);
            var sql = _scriptLoader.LoadScript("Todos/GetById.sql");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var item = await _connection.QueryFirstOrDefaultAsync<TodoItem>(
                sql,
                new { Id = id },
                transaction: _transaction
            );

            if (item != null)
                _logger?.LogInformation("Todo item found: {TodoId}", id);
            else
                _logger?.LogWarning("Todo item not found: {TodoId}", id);

            return item;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching todo item with ID: {TodoId}", id);
            throw;
        }
    }

    public async Task<int> CreateAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
    {
        try
        {
            if (todoItem == null)
                throw new ArgumentNullException(nameof(todoItem));

            _logger?.LogInformation("Creating new todo item: {TodoTitle}", todoItem.Title);
            var sql = _scriptLoader.LoadScript("Todos/Create.sql");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var parameters = new
            {
                Title = todoItem.Title,
                Description = todoItem.Description,
                IsCompleted = todoItem.IsCompleted,
                CreatedAt = todoItem.CreatedAt
            };

            var id = await _connection.ExecuteScalarAsync<int>(
                sql,
                parameters,
                transaction: _transaction
            );

            _logger?.LogInformation("Todo item created with ID: {TodoId}", id);
            return id;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating todo item: {TodoTitle}", todoItem.Title);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
    {
        try
        {
            if (todoItem == null)
                throw new ArgumentNullException(nameof(todoItem));

            _logger?.LogInformation("Updating todo item: {TodoId}", todoItem.Id);
            var sql = _scriptLoader.LoadScript("Todos/Update.sql");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var parameters = new
            {
                Id = todoItem.Id,
                Title = todoItem.Title,
                Description = todoItem.Description,
                IsCompleted = todoItem.IsCompleted,
                CompletedAt = todoItem.IsCompleted ? DateTime.UtcNow : (DateTime?)null
            };

            var rowsAffected = await _connection.ExecuteAsync(
                sql,
                parameters,
                transaction: _transaction
            );

            if (rowsAffected > 0)
                _logger?.LogInformation("Todo item updated: {TodoId}", todoItem.Id);
            else
                _logger?.LogWarning("No todo item found to update: {TodoId}", todoItem.Id);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating todo item: {TodoId}", todoItem.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Deleting todo item: {TodoId}", id);
            var sql = _scriptLoader.LoadScript("Todos/Delete.sql");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var rowsAffected = await _connection.ExecuteAsync(
                sql,
                new { Id = id },
                transaction: _transaction
            );

            if (rowsAffected > 0)
                _logger?.LogInformation("Todo item deleted: {TodoId}", id);
            else
                _logger?.LogWarning("No todo item found to delete: {TodoId}", id);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting todo item: {TodoId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets count of todos using Dapper.SqlBuilder for optional WHERE clause.
    /// Demonstrates advanced SqlBuilder usage with COUNT aggregation.
    /// </summary>
    public async Task<int> GetCountAsync(bool? isCompleted = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Fetching todo count");

            // Use SqlBuilder for dynamic count query
            var builder = new SqlBuilder();
            var template = builder.AddTemplate(@"
                SELECT COUNT(*) FROM [dbo].[Todos]
                /**where**/
            ");

            if (isCompleted.HasValue)
                builder.Where("[IsCompleted] = @IsCompleted", new { IsCompleted = isCompleted.Value });

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var count = await _connection.ExecuteScalarAsync<int>(
                template.RawSql,
                template.Parameters,
                transaction: _transaction
            );

            _logger?.LogDebug("Todo count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching todo count");
            throw;
        }
    }
}