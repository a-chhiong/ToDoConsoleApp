using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Data.SqlClient;
using ToDoConsoleApp.Utils;

namespace ToDoConsoleApp.Infrastructure.Database.Encryption;

public class AlwaysEncryptedSetup
{
    private const string AesKey = "33507e1576e289c10bd5207bfacde493cfefcf4db9153d3a524833813c2b0ee2";

    public async Task RunAsync(SqlConnection connection, string? pfxPath, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(pfxPath) || string.IsNullOrEmpty(password)) return;
        
        // 1. Generate a CEK (AES key)
        var cek = SHA256.HashData(Encoding.UTF8.GetBytes(AesKey));

        // 2. Encrypt CEK with CMK (RSA public key from cert)
        var cert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable);
        using var rsa = cert.GetRSAPublicKey();
        var encryptedCek = rsa?.Encrypt(cek, RSAEncryptionPadding.OaepSHA256) ?? [];

        // 3. Convert encrypted CEK to hex for SQL
        var hexEncryptedCek = "0x" + BitConverter.ToString(encryptedCek).Replace("-", "");

        // 4. Build SQL statements
//         var resetSql = $"""
//                      IF EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = 'CEK_ToDoApp')
//                      BEGIN
//                          DROP COLUMN ENCRYPTION KEY CEK_ToDoApp;
//                      END
//                      IF EXISTS (SELECT * FROM sys.column_master_keys WHERE name = 'CMK_ToDoApp')
//                      BEGIN
//                          DROP COLUMN MASTER KEY CMK_ToDoApp;
//                      END
//                      """;
        
        var cmkSql = $"""
                      IF NOT EXISTS (SELECT * FROM sys.column_master_keys WHERE name = 'CMK_ToDoApp')
                      BEGIN
                      CREATE COLUMN MASTER KEY CMK_ToDoApp
                      WITH (
                          KEY_STORE_PROVIDER_NAME = 'CUSTOM_FILE_STORE',
                          KEY_PATH = '{pfxPath}'
                      );
                      END
                      """;

        var cekSql = $"""
                      IF NOT EXISTS (SELECT * FROM sys.column_encryption_keys WHERE name = 'CEK_ToDoApp')
                      BEGIN
                      CREATE COLUMN ENCRYPTION KEY CEK_ToDoApp
                      WITH VALUES (
                          COLUMN_MASTER_KEY = CMK_ToDoApp,
                          ALGORITHM = 'RSA_OAEP',
                          ENCRYPTED_VALUE = {hexEncryptedCek}
                      );
                      END
                      """;

        // 5. Execute against SQL Server
        // await using (var cmd = new SqlCommand(resetSql, connection))
        //     await cmd.ExecuteNonQueryAsync(cancellationToken);
        await using (var cmd = new SqlCommand(cmkSql, connection)) 
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        await using (var cmd = new SqlCommand(cekSql, connection))
            await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}