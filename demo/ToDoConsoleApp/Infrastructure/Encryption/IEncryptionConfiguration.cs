using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Encryption;

public interface IEncryptionConfiguration
{
    public SqlConnection ConfigureConnection(string connectionString);
}