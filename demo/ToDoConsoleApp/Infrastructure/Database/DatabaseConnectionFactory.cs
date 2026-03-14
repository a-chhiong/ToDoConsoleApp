using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Infrastructure.Database.Encryption;

namespace ToDoConsoleApp.Infrastructure.Database;

/// <summary>
/// Factory pattern for creating SQL Server database connections.
/// Centralizes connection creation and management.
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly ILogger<DatabaseConnectionFactory>? _logger;

    public DatabaseConnectionFactory(
        string connectionString, 
        IEncryptionConfiguration? encryptionConfig = null, 
        ILogger<DatabaseConnectionFactory>? logger = null)
    {
        ConnectionString = connectionString;
        EncryptionConfig = encryptionConfig;
        _logger = logger;
    }

    /// <summary>
    /// Creates and returns a new database connection.
    /// Caller is responsible for proper disposal.
    /// </summary>
    public SqlConnection CreateConnection()
    {
        _logger?.LogDebug("Creating database connection with Always Encrypt: {AlwaysEncryptEnabled}", EncryptionConfig != null);
        
        return EncryptionConfig != null ? 
            EncryptionConfig.CreateConnection(ConnectionString) 
            : new SqlConnection(ConnectionString);
    }
    
    public string ConnectionString { get; }
    public IEncryptionConfiguration? EncryptionConfig { get; }
}