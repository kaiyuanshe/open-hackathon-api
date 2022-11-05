using Microsoft.Extensions.Configuration;

namespace Kaiyuanshe.OpenHackathon.Server.Storage
{
    public interface IStorageCredentialProvider
    {
        string HackathonServerStorageConnectionString { get; }
    }


    public class StorageCredentialProvider : IStorageCredentialProvider
    {
        string connectionString;

        public StorageCredentialProvider(IConfiguration configuration)
        {
            connectionString = configuration[ConfigurationKeys.StorageConnectionString];
        }

        public string HackathonServerStorageConnectionString => connectionString;
    }
}
