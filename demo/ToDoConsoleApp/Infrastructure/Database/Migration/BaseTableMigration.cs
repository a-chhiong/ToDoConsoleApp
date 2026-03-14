using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Database.Migration;

public abstract class BaseTableMigration
{
    protected abstract string MigrationId { get; }
    protected abstract string MigrationHash { get; }

    // Each migration defines its steps here
    protected abstract Task ApplyAsync(SqlConnection connection);

    public async Task RunAsync(SqlConnection connection)
    {
        // Guard: skip if already applied
        await using var checkCmd = new SqlCommand(
            "SELECT 1 FROM dbo.Migrations WHERE Id = @id", connection);
        checkCmd.Parameters.AddWithValue("@id", MigrationId);

        var alreadyApplied = await checkCmd.ExecuteScalarAsync() != null;
        if (alreadyApplied)
        {
            Console.WriteLine($"Migration {MigrationId} already applied. Skipping.");
            return;
        }

        // Apply migration
        await ApplyAsync(connection);

        // Record migration
        await using var recordCmd = new SqlCommand(
            "INSERT INTO dbo.Migrations (Id, Hash) VALUES (@id, @hash);", connection);
        recordCmd.Parameters.AddWithValue("@id", MigrationId);
        recordCmd.Parameters.AddWithValue("@hash", MigrationHash);
        await recordCmd.ExecuteNonQueryAsync();

        Console.WriteLine($"Migration {MigrationId} applied successfully.");
    }
    
    /// <summary>
    /// Utility to compute a SHA256 hash from a string.
    /// </summary>
    protected static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}