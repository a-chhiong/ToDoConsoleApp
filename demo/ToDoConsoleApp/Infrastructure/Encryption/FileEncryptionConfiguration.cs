using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Encryption;

public class FileEncryptionConfiguration: IEncryptionConfiguration
{
    private readonly string _pfxPath;
    private readonly string _password;

    public FileEncryptionConfiguration(string pfxPath, string password)
    {
        _pfxPath = pfxPath;
        _password = password;
    }

    public SqlConnection ConfigureConnection(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled
        };

        // Register custom column encryption key store provider
        var provider = new FileCertProvider(_pfxPath, _password);
        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
            new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
            {
                { "CUSTOM_FILE_STORE", provider }
            });

        return new SqlConnection(builder.ConnectionString);
    }
}

public class FileCertProvider : SqlColumnEncryptionKeyStoreProvider
{
    private readonly string _pfxPath;
    private readonly string _password;

    public FileCertProvider(string pfxPath, string password)
    {
        _pfxPath = pfxPath;
        _password = password;
    }

    public override byte[] DecryptColumnEncryptionKey(string masterKeyPath, string encryptionAlgorithm, byte[] encryptedColumnEncryptionKey)
    {
        // masterKeyPath will be '/path/to/todoapp_cmk.pfx' from your SQL metadata
        var cert = new X509Certificate2(_pfxPath, _password, X509KeyStorageFlags.Exportable);
        using var rsa = cert.GetRSAPrivateKey();
        return rsa?.Decrypt(encryptedColumnEncryptionKey, RSAEncryptionPadding.OaepSHA256) ?? [];
    }

    public override byte[] EncryptColumnEncryptionKey(string masterKeyPath, string encryptionAlgorithm, byte[] columnEncryptionKey)
    {
        throw new NotImplementedException("SQL Server handles CEK encryption");
    }
}
