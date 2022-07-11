using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IManagementClient
    {
        IStorageContext StorageContext { get; set; }
        ICacheProvider Cache { get; set; }
    }

    public interface IDefaultManagementClient<TParameter, TEntity, TOptions>
        where TEntity : BaseTableEntity
        where TParameter : new()
        where TOptions : TableQueryOptions
    {
        Task<TEntity> Create(TParameter parameter, CancellationToken cancellationToken);
        Task<TEntity> Update(TEntity existing, TParameter parameter, CancellationToken cancellationToken);
        Task<TEntity?> Get(string partitionKey, string rowkey, CancellationToken cancellationToken);
        Task<IEnumerable<TEntity>> List(TOptions options, CancellationToken cancellationToken);
        Task Delete(string partitionKey, string rowkey, CancellationToken cancellationToken);
    }

    public abstract class ManagementClientBase<TManagement> : IManagementClient
    {
        public IStorageContext StorageContext { get; set; }
        public ICacheProvider Cache { get; set; }
        public ILogger<TManagement> Logger { get; set; }
    }

    //public abstract class DefaultManagementClient<TManagement, TParameter, TEntity> 
    //    : ManagementClientBase<TManagement>, IManagementClient<TParameter, TEntity>
    //    where TEntity : BaseTableEntity
    //    where TParameter : new()
    //{
    //}
}
