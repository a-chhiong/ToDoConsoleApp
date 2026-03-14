using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Database.Migration;

public class TodoTableMigration : BaseTableMigration
{
    protected override string MigrationId => "AlterTodoTable~202603141300";
    protected override string MigrationHash => ComputeHash("dbo.Todos:DescriptionX");
    
    protected override async Task ApplyAsync(SqlConnection connection)
    {
        // 1. Add encrypted column if not exists
        await using var existsCmd = new SqlCommand(@"
            SELECT 1 FROM sys.columns 
            WHERE object_id = OBJECT_ID(N'[dbo].[Todos]')
            AND name = 'DescriptionX'", connection);

        var exists = await existsCmd.ExecuteScalarAsync() != null;
        if (!exists)
        {
            var alterCmd = new SqlCommand(@"
                ALTER TABLE dbo.Todos
                ADD DescriptionX NVARCHAR(MAX)
                COLLATE Latin1_General_BIN2 ENCRYPTED
                WITH (
                    ENCRYPTION_TYPE = RANDOMIZED,
                    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256',
                    COLUMN_ENCRYPTION_KEY = CEK_ToDoApp
                ) NULL;", connection);
            await alterCmd.ExecuteNonQueryAsync();
        }
        
        // 2. Copy data with client-side encryption
        await using var selectCmd = new SqlCommand("SELECT Id, Description FROM dbo.Todos", connection);
        await using var reader = await selectCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var description = reader.IsDBNull(1) ? null : reader.GetString(1);

            await using var updateCmd = new SqlCommand(
                "UPDATE dbo.Todos SET DescriptionX = @desc WHERE Id = @id", connection);
            updateCmd.Parameters.AddWithValue("@desc", (object?)description ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@id", id);
            await updateCmd.ExecuteNonQueryAsync();
        }

        // 3. Drop old column and rename
        await new SqlCommand("ALTER TABLE dbo.Todos DROP COLUMN Description;", connection)
            .ExecuteNonQueryAsync();
        await new SqlCommand(
            "EXEC sp_rename 'dbo.Todos.DescriptionX', 'Description', 'COLUMN';", connection)
            .ExecuteNonQueryAsync();
    }
}
