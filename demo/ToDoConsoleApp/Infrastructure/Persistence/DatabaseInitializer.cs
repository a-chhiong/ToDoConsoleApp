using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using ToDoConsoleApp.Utils;

namespace ToDoConsoleApp.Infrastructure.Persistence;

/// <summary>
/// Initializes the database schema on application startup.
/// Creates tables if they don't exist.
/// </summary>
public class DatabaseInitializer
{
    private readonly DbConnection _connection;
    private readonly SqlScriptLoader _scriptLoader;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        DbConnection connection,
        SqlScriptLoader scriptLoader,
        ILogger<DatabaseInitializer> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _scriptLoader = scriptLoader ?? throw new ArgumentNullException(nameof(scriptLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the database by executing schema creation scripts.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database initialization");

            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            var schemaScript = _scriptLoader.LoadScript("Schema/CreateTodoTable.sql");

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = schemaScript;
                command.CommandTimeout = 60;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
        finally
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }
    }
}