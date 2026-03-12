using System.Data;
using System.Data.Common;
using ToDoConsoleApp.Application.Interfaces;
using ToDoConsoleApp.Infrastructure.Repositories;

namespace ToDoConsoleApp.Infrastructure.Database;

/// <summary>
/// Unit of Work pattern implementation for managing transactions and repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    
    private IDbTransaction? _transaction;
    private ITodoRepository? _todoRepository;

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public ITodoRepository TodoRepository
    {
        get
        {
            if (_todoRepository == null && _connection is DbConnection connection)
                _todoRepository = new TodoRepository(connection, _transaction);
            return _todoRepository;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // In this simple implementation, SaveChanges does nothing
        // In a more complex scenario, it would flush changes from a change tracker
        return await Task.FromResult(1);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State == ConnectionState.Closed && _connection is DbConnection connection)
            await connection.OpenAsync(cancellationToken);

        _transaction = _connection.BeginTransaction();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _transaction?.Commit();
        }
        catch
        {
            _transaction?.Rollback();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _transaction?.Rollback();
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _transaction?.Dispose();
        if (_connection is DbConnection connection)
            await connection.DisposeAsync();
    }
}