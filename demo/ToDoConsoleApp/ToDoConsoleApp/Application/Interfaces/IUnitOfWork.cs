namespace ToDoConsoleApp.Application.Interfaces;

/// <summary>
/// Unit of Work pattern interface for transaction management.
/// Coordinates multiple repositories and transactions.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    ITodoRepository TodoRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}