using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Infrastructure.Encryption;

namespace ToDoConsoleApp.Infrastructure.Database;

/// <summary>
/// Factory pattern for creating SQL Server database connections.
/// Centralizes connection creation and management.
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly string _connectionString;
    private readonly IEncryptionConfiguration? _encryptionConfig;
    private readonly ILogger<DatabaseConnectionFactory>? _logger;

    public DatabaseConnectionFactory(
        string connectionString, 
        IEncryptionConfiguration? encryptionConfig = null, 
        ILogger<DatabaseConnectionFactory>? logger = null)
    {
        _connectionString = connectionString;
        _encryptionConfig = encryptionConfig;
        _logger = logger;
    }

    /// <summary>
    /// Creates and returns a new database connection.
    /// Caller is responsible for proper disposal.
    /// </summary>
    public DbConnection CreateConnection()
    {
        _logger?.LogDebug("Creating database connection with Always Encrypt: {AlwaysEncryptEnabled}", _encryptionConfig != null);
        
        return _encryptionConfig != null ? 
            _encryptionConfig.ConfigureConnection(_connectionString) 
            : new SqlConnection(_connectionString);
    }
}