using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Microsoft.Extensions.Logging;
using System;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IManagementClient
    {
        IStorageContext StorageContext { get; set; }
        ICacheProvider Cache { get; set; }
    }

    [Obsolete]
    public abstract class ManagementClientBaseV0
    {
        public IStorageContext StorageContext { get; set; }
        public ICacheProvider Cache { get; set; }
    }

    public abstract class ManagementClientBase<TManagement> : IManagementClient
    {
        public IStorageContext StorageContext { get; set; }
        public ICacheProvider Cache { get; set; }
        public ILogger<TManagement> Logger { get; set; }
    }
}
