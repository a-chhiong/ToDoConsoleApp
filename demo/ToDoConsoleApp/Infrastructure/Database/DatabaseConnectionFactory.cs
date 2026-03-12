using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Database;

/// <summary>
/// Factory pattern for creating SQL Server database connections.
/// Centralizes connection creation and management.
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly string _connectionString;

    public DatabaseConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates and returns a new database connection.
    /// Caller is responsible for proper disposal.
    /// </summary>
    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <summary>
    /// Opens a connection asynchronously.
    /// </summary>
    public async Task<DbConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}