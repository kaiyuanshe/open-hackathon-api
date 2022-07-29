using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Kaiyuanshe.OpenHackathon.Server.Extensions;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IAzureTableV2<TEntity> where TEntity : BaseTableEntity, new()
    {
        Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task MergeAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task InsertOrMergeAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task RetrieveAndMergeAsync(string partitionKey, string rowKey, Action<TEntity> func, CancellationToken cancellationToken = default);
        Task InsertOrReplaceAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task ReplaceAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);
        Task<TEntity?> RetrieveAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> QueryEntitiesAsync(string filter, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
        Task ExecuteQueryAsync(string filter, Action<TEntity> action, int? limit = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
        Task ExecuteQueryAsync(string filter, Func<TEntity, Task> asyncAction, int? limit = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
        Task ExecuteQueryInParallelAsync(string filter, Func<TEntity, Task> asyncAction, int maxParallelism = 5, int? limit = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
        Task<Page<TEntity>> ExecuteQuerySegmentedAsync(string? filter, string? continuationToken, int? maxPerPage = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
    }

    public abstract class AzureTableV2<TEntity> : StorageClientBase, IAzureTableV2<TEntity> where TEntity : BaseTableEntity, new()
    {
        TableClient? tableClient;
        protected abstract string TableName { get; }

        public override string? StorageName => tableClient?.AccountName;
        public ILogger<TEntity> Logger { get; set; }

        public virtual async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await client.AddEntityAsync(entity.ToTableEntity(), cancellationToken);
                    entity.ETag = resp.Headers.ETag.GetValueOrDefault().ToString();
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task MergeAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await client.UpdateEntityAsync(entity.ToTableEntity(), new ETag(entity.ETag), TableUpdateMode.Merge, cancellationToken);
                    entity.ETag = resp.Headers.ETag.GetValueOrDefault().ToString();
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task InsertOrMergeAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await client.UpsertEntityAsync(entity.ToTableEntity(), TableUpdateMode.Merge, cancellationToken);
                    entity.ETag = resp.Headers.ETag.GetValueOrDefault().ToString();
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task RetrieveAndMergeAsync(string partitionKey, string rowKey, Action<TEntity> func, CancellationToken cancellationToken = default)
        {
            var entity = await RetrieveAsync(partitionKey, rowKey, cancellationToken);
            if (entity != null)
            {
                func(entity);
                await MergeAsync(entity, cancellationToken);
            }
        }

        public virtual async Task InsertOrReplaceAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await client.UpsertEntityAsync(entity.ToTableEntity(), TableUpdateMode.Replace, cancellationToken);
                    entity.ETag = resp.Headers.ETag.GetValueOrDefault().ToString();
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task ReplaceAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await client.UpdateEntityAsync(entity.ToTableEntity(), new ETag(entity.ETag), TableUpdateMode.Replace, cancellationToken);
                    entity.ETag = resp.Headers.ETag.GetValueOrDefault().ToString();
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task DeleteAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    await client.DeleteEntityAsync(partitionKey, rowKey, default, cancellationToken);
                }
                catch (RequestFailedException ex)
                {
                    if (ex.Status != 404)
                        throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task<TEntity?> RetrieveAsync(string partitionKey, string rowKey, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await client.GetEntityAsync<TableEntity>(partitionKey, rowKey, null, cancellationToken);
                    return resp.Value.ToBaseTableEntity<TEntity>();
                }
                catch (RequestFailedException ex)
                {
                    if (ex.Status == 404)
                    {
                        return default(TEntity);
                    }

                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task<IEnumerable<TEntity>> QueryEntitiesAsync(string filter, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        {
            List<TEntity> entities = new List<TEntity>();
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var query = client.QueryAsync<TableEntity>(filter, null, select, cancellationToken);
                    await query.ForEach(entity => entities.Add(entity.ToBaseTableEntity<TEntity>()), null, cancellationToken);
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
            return entities;
        }

        public virtual async Task ExecuteQueryAsync(string filter, Action<TEntity> action, int? limit = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var query = client.QueryAsync<TableEntity>(filter, limit, select, cancellationToken);
                    await query.ForEach(entity => action(entity.ToBaseTableEntity<TEntity>()), limit, cancellationToken);
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task ExecuteQueryAsync(string filter, Func<TEntity, Task> asyncAction, int? limit = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var query = client.QueryAsync<TableEntity>(filter, limit, select, cancellationToken);
                    await query.ForEach(entity => asyncAction(entity.ToBaseTableEntity<TEntity>()), limit, cancellationToken);
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task ExecuteQueryInParallelAsync(string filter, Func<TEntity, Task> asyncAction, int maxParallelism = 5, int? limit = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var query = client.QueryAsync<TableEntity>(filter, limit, select, cancellationToken);
                    await query.ParallelForEachAsync(entity => asyncAction(entity.ToBaseTableEntity<TEntity>()), maxParallelism, limit, cancellationToken);
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public virtual async Task<Page<TEntity>> ExecuteQuerySegmentedAsync(string? filter, string? continuationToken, int? maxPerPage = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        {
            var client = await GetTableClientAsync(cancellationToken);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var query = client.QueryAsync<TableEntity>(filter, maxPerPage, select, cancellationToken);
                    var page = query.AsPages(continuationToken, maxPerPage);
                    var enumerator = page.GetAsyncEnumerator(cancellationToken);
                    if (await enumerator.MoveNextAsync())
                    {
                        var current = enumerator.Current;
                        return Page<TEntity>.FromValues(
                            current.Values.ToBaseTableEntities<TEntity>().ToList().AsReadOnly(),
                            current.ContinuationToken,
                            current.GetRawResponse());
                    }
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    return Page<TEntity>.FromValues(new List<TEntity>(), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        private async Task<TableClient> GetTableClientAsync(CancellationToken cancellationToken)
        {
            if (tableClient == null)
            {
                var conn = StorageCredentialProvider.HackathonServerStorageConnectionString;
                var clientOptions = new TableClientOptions();
                var traceIdPolicy = TraceIdHttpPipelinePolicyFactory.GetPipelinePolicy();
                clientOptions.AddPolicy(traceIdPolicy, HttpPipelinePosition.PerRetry);

                tableClient = new TableClient(conn, TableName, clientOptions);
                Logger.TraceInformation($"Building TableClient for {StorageName}.{TableName}.");
                using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
                {
                    try
                    {
                        await tableClient.CreateIfNotExistsAsync(cancellationToken);
                    }
                    catch (RequestFailedException ex)
                    {
                        throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                    }
                }
            }

            return tableClient;
        }
    }
}
