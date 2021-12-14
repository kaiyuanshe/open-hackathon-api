using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Kaiyuanshe.OpenHackathon.Server.Extensions;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IAzureTableV2<TEntity> where TEntity : BaseTableEntity, new()
    {
        Task<IEnumerable<TEntity>> QueryEntitiesAsync(string filter, IEnumerable<string> select = null, CancellationToken cancellationToken = default);
    }

    public abstract class AzureTableV2<TEntity> : StorageClientBase, IAzureTableV2<TEntity> where TEntity : BaseTableEntity, new()
    {
        TableClient tableClient = null;
        ILogger logger;
        protected abstract string TableName { get; }

        protected AzureTableV2(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<IEnumerable<TEntity>> QueryEntitiesAsync(string filter, IEnumerable<string> select = null, CancellationToken cancellationToken = default)
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

        private async Task<TableClient> GetTableClientAsync(CancellationToken cancellationToken)
        {
            if (tableClient == null)
            {
                var conn = StorageCredentialProvider.HackathonServerStorageConnectionString;
                var clientOptions = new TableClientOptions();
                var operationIdPolicy = TraceIdHttpPipelinePolicyFactory.GetPipelinePolicy();
                clientOptions.AddPolicy(operationIdPolicy, HttpPipelinePosition.PerRetry);

                logger.TraceInformation($"Building TableClient for {StorageName}.{TableName}.");
                tableClient = new TableClient(conn, TableName, clientOptions);
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
