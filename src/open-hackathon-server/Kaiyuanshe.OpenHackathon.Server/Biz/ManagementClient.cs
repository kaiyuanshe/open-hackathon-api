using System.Net.Http;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IManagementClient
    {
        IStorageContext StorageContext { get; set; }
        ICacheProvider Cache { get; set; }
        IHttpClientFactory HttpClientFactory { get; set; }
    }

    public abstract class ManagementClient<TManagement> : IManagementClient
    {
        public IStorageContext StorageContext { get; set; }
        public ICacheProvider Cache { get; set; }
        public IHttpClientFactory HttpClientFactory { get; set; }
        public ILogger<TManagement> Logger { get; set; }
    }
}
