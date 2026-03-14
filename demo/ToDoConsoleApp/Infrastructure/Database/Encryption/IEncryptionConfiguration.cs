using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Database.Encryption;

public interface IEncryptionConfiguration
{
    public SqlConnection CreateConnection(string connectionString);
    public string PfxPath { get; }
    public string Password { get; }
}