﻿using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
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

    public abstract class ManagementClient<TManagement> : IManagementClient
    {
        public IStorageContext StorageContext { get; set; }
        public ICacheProvider Cache { get; set; }
        public ILogger<TManagement> Logger { get; set; }
    }

    public interface IDefaultManagementClient<TParameter, TEntity, TOptions>
        where TEntity : BaseTableEntity, new()
        where TParameter : new()
        where TOptions : TableQueryOptions
    {
        Task<TEntity> Create(TParameter parameter, CancellationToken cancellationToken);
        Task<TEntity> Update(TEntity existing, TParameter parameter, CancellationToken cancellationToken);
        Task<TEntity?> Get(string partitionKey, string rowkey, CancellationToken cancellationToken);
        Task<IEnumerable<TEntity>> List(TOptions options, CancellationToken cancellationToken);
        Task Delete(string partitionKey, string rowkey, CancellationToken cancellationToken);
    }

    public abstract class DefaultManagementClient<TManagement, TParameter, TEntity, TOptions>
        : ManagementClient<TManagement>, IDefaultManagementClient<TParameter, TEntity, TOptions>
        where TEntity : BaseTableEntity, new()
        where TParameter : new()
        where TOptions : TableQueryOptions
    {
        protected abstract TEntity ConvertParamterToEntity(TParameter parameter);
        protected abstract IAzureTableV2<TEntity> Table { get; }
        protected virtual bool EnableCache { get; } = true;
        protected abstract CacheEntryType CacheType { get; }

        public async Task<TEntity> Create(TParameter parameter, CancellationToken cancellationToken)
        {
            var entity = ConvertParamterToEntity(parameter);
            await Table.InsertAsync(entity, cancellationToken);
            if (EnableCache)
            {
                InvalidateCache(entity.PartitionKey);
            }
            return entity;
        }

        public Task Delete(string partitionKey, string rowkey, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<TEntity?> Get(string partitionKey, string rowkey, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<TEntity>> List(TOptions options, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<TEntity> Update(TEntity existing, TParameter parameter, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        #region Cache
        private string GetCacheKey(string subKey)
        {
            return CacheKeys.GetCacheKey(CacheType, subKey);
        }

        private void InvalidateCache(string subKey)
        {
            Cache.Remove(GetCacheKey(subKey));
        }
        #endregion
    }
}
