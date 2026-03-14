using System.Data;
using Microsoft.Data.SqlClient;
using ToDoConsoleApp.Infrastructure.Database.Encryption;
using ToDoConsoleApp.Infrastructure.Database.Migration;
using ToDoConsoleApp.Utils;

namespace ToDoConsoleApp.Infrastructure.Database;

/// <summary>
/// Initializes the database schema on application startup.
/// Creates tables if they don't exist.
/// </summary>
public class DatabaseInitializer
{
    private readonly IEncryptionConfiguration? _configuration;
    private readonly SqlConnection _connection;
    private readonly SqlScriptLoader _scriptLoader;
    
    private const string AesKey = "33507e1576e289c10bd5207bfacde493cfefcf4db9153d3a524833813c2b0ee2";
    
    public DatabaseInitializer(
        IEncryptionConfiguration? configuration,
        SqlConnection connection,
        SqlScriptLoader scriptLoader)
    {
        _configuration = configuration;
        _connection = connection;
        _scriptLoader = scriptLoader ?? throw new ArgumentNullException(nameof(scriptLoader));
    }

    /// <summary>
    /// Initializes the database by executing schema creation scripts.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection.State == ConnectionState.Closed)
                await _connection.OpenAsync(cancellationToken);

            await new AlwaysEncryptedSetup().RunAsync(_connection, _configuration?.PfxPath, _configuration?.Password, cancellationToken);
            
            await ExecuteScriptAsync("Schema/CreateMigrationTable.sql", cancellationToken);
            
            await ExecuteScriptAsync("Schema/CreateTodoTable.sql", cancellationToken);
            
            await new TodoTableMigration().RunAsync(_connection);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            if (_connection.State == ConnectionState.Open)
                await _connection.CloseAsync();
        }
    }
    
    private async Task ExecuteScriptAsync(string scriptPath, CancellationToken cancellationToken)
    {
        var batches = _scriptLoader.LoadScriptBatches(scriptPath).ToArray();

        if (batches.Length == 0)
        {
            Console.WriteLine($"Script {scriptPath} is empty or not found");
            return;
        }

        foreach (var batch in batches)
        {
            Console.WriteLine($"Executing...\n{batch}\n");
            await using var command = _connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120; // longer timeout for encryption setup
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        
        Console.WriteLine($"Executed script {scriptPath}");
    } 
}