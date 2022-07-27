using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Task<IEnumerable<TEntity>> ListPaginated(TOptions options, CancellationToken cancellationToken);
        Task Delete(TEntity entity, CancellationToken cancellationToken);
    }

    public abstract class DefaultManagementClient<TManagement, TParameter, TEntity, TOptions>
        : ManagementClient<TManagement>, IDefaultManagementClient<TParameter, TEntity, TOptions>
        where TEntity : BaseTableEntity, new()
        where TParameter : new()
        where TOptions : TableQueryOptions
    {
        protected abstract TEntity ConvertParamterToEntity(TParameter parameter);
        protected abstract void TryUpdate(TEntity existing, TParameter parameter);
        protected abstract IAzureTableV2<TEntity> Table { get; }
        protected virtual bool EnableCache { get; } = true;
        protected abstract CacheEntryType CacheType { get; }
        protected virtual string GetCacheKey(TEntity entity) => entity.PartitionKey;
        protected virtual TimeSpan Expiry { get; } = TimeSpan.FromHours(6);
        protected abstract Task<IEnumerable<TEntity>> ListWithoutCache(TOptions options, CancellationToken cancellationToken);

        public async Task<TEntity> Create(TParameter parameter, CancellationToken cancellationToken)
        {
            var entity = ConvertParamterToEntity(parameter);
            await Table.InsertAsync(entity, cancellationToken);
            if (EnableCache)
            {
                InvalidateCache(GetCacheKey(entity));
            }
            entity.Timestamp = DateTimeOffset.UtcNow;
            return entity;
        }

        public virtual async Task<IEnumerable<TEntity>> ListPaginated(TOptions options, CancellationToken cancellationToken)
        {
            var allEntities = EnableCache ?
                await ListCachedEntities(options, cancellationToken) :
                await ListWithoutCache(options, cancellationToken);

            // paging
            int.TryParse(options.Pagination?.np, out int np);
            int top = options.Top();
            var filtered = allEntities.OrderByDescending(a => a.CreatedAt).Skip(np).Take(top);

            // next paging
            options.NextPage = null;
            if (np + top < allEntities.Count())
            {
                options.NextPage = new Pagination
                {
                    np = (np + top).ToString(),
                    nr = (np + top).ToString(),
                };
            }

            return filtered;
        }

        public async Task<TEntity> Update(TEntity existing, TParameter parameter, CancellationToken cancellationToken)
        {
            TryUpdate(existing, parameter);
            await Table.MergeAsync(existing, cancellationToken);
            if (EnableCache)
            {
                InvalidateCache(GetCacheKey(existing));
            }
            return existing;
        }

        public async Task Delete(TEntity entity, CancellationToken cancellationToken)
        {
            await Table.DeleteAsync(entity.PartitionKey, entity.RowKey, cancellationToken);
            if (EnableCache)
            {
                InvalidateCache(GetCacheKey(entity));
            }
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

        private async Task<IEnumerable<TEntity>> ListCachedEntities(TOptions options, CancellationToken cancellationToken)
        {
            string cacheKey = GetCacheKey(options.HackathonName);
            return await Cache.GetOrAddAsync(cacheKey, Expiry, async (ct) =>
            {
                return await ListWithoutCache(options, ct);
            }, true, cancellationToken);
        }
        #endregion
    }
}
