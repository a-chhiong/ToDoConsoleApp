using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;

namespace ToDoConsoleApp.Infrastructure.Encryption;

public class StoreEncryptionConfiguration: IEncryptionConfiguration
{
    private readonly string _certificateThumbprint;
    private readonly StoreName _storeName;
    private readonly StoreLocation _storeLocation;

    public StoreEncryptionConfiguration(string certificateThumbprint, StoreLocation storeLocation = StoreLocation.CurrentUser)
    {
        _certificateThumbprint = certificateThumbprint;
        _storeName = StoreName.My;
        _storeLocation = storeLocation;
    }

    public SqlConnection ConfigureConnection(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled
        };

        // Register custom column encryption key store provider
        var provider = new StoreCertProvider(_certificateThumbprint, _storeName, _storeLocation);
        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
            customProviders: new Dictionary<string, SqlColumnEncryptionKeyStoreProvider> { { "MSSQL_CERTIFICATE_STORE", provider } }
        );

        return new SqlConnection(builder.ConnectionString);
    }
}

public class StoreCertProvider : SqlColumnEncryptionKeyStoreProvider
{
    private readonly string _certificateThumbprint;
    private readonly StoreName _storeName;
    private readonly StoreLocation _storeLocation;

    public StoreCertProvider(string thumbprint, StoreName storeName, StoreLocation storeLocation)
    {
        _certificateThumbprint = thumbprint;
        _storeName = storeName;
        _storeLocation = storeLocation;
    }

    public override byte[] DecryptColumnEncryptionKey(string masterKeyPath, string encryptionAlgorithm, byte[] encryptedColumnEncryptionKey)
    {
        var store = new X509Store(_storeName, _storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var cert = store.Certificates.Find(X509FindType.FindByThumbprint, _certificateThumbprint, false);
        if (cert.Count == 0)
            return [];
            
        using var rsa = cert[0].GetRSAPrivateKey();
        return rsa?.Decrypt(encryptedColumnEncryptionKey, RSAEncryptionPadding.OaepSHA256) ?? [];
    }

    public override byte[] EncryptColumnEncryptionKey(string masterKeyPath, string encryptionAlgorithm, byte[] columnEncryptionKey)
    {
        throw new NotImplementedException("SQL Server handles CEK encryption");
    }
}