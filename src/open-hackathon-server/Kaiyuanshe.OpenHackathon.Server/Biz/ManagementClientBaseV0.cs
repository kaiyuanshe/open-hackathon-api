using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using System;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    [Obsolete]
    public abstract class ManagementClientBaseV0
    {
        public IStorageContext StorageContext { get; set; }
        public ICacheProvider Cache { get; set; }
    }
}
