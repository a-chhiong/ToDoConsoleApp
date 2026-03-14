using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Database.Encryption;

public class FileEncryptionConfiguration: IEncryptionConfiguration
{
    public FileEncryptionConfiguration(string pfxPath, string password)
    {
        PfxPath = pfxPath;
        Password = password;
        
        // Register custom column encryption key store provider
        var provider = new FileCertProvider(PfxPath, Password);
        try
        {
            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
                customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>
                {
                    { "CUSTOM_FILE_STORE", provider }
                }
            );
        }
        catch (SqlException e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public SqlConnection CreateConnection(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled
        };

        return new SqlConnection(builder.ConnectionString);
    }

    public string PfxPath { get; }
    public string Password { get; }
}

public class FileCertProvider : SqlColumnEncryptionKeyStoreProvider
{
    public FileCertProvider(string pfxPath, string password)
    {
        PfxPath = pfxPath;
        Password = password;
    }

    public override byte[] DecryptColumnEncryptionKey(
        string masterKeyPath, 
        string encryptionAlgorithm, 
        byte[] encryptedColumnEncryptionKey)
    {
        var cert = new X509Certificate2(PfxPath, Password, X509KeyStorageFlags.Exportable);
        using var rsa = cert.GetRSAPrivateKey();
        return rsa?.Decrypt(encryptedColumnEncryptionKey, RSAEncryptionPadding.OaepSHA256) ?? [];
    }
    
    public override byte[] EncryptColumnEncryptionKey(
        string masterKeyPath,
        string encryptionAlgorithm,
        byte[] columnEncryptionKey)
    {
        var cert = new X509Certificate2(PfxPath, Password, X509KeyStorageFlags.Exportable);
        using var rsa = cert.GetRSAPublicKey();
        return rsa?.Encrypt(columnEncryptionKey, RSAEncryptionPadding.OaepSHA256) ?? [];
    }

    private string PfxPath { get; }
    private string Password { get; }
}
