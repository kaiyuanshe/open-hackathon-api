using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Blobs
{
    public interface IAzureBlob
    {
        Task<string> AcquireLeaseAsync(TimeSpan duration, CancellationToken cancellationToken);
        Task ReleaseLease(string leaseId, CancellationToken cancellationToken);
        Task ReleaseLeaseAsync(string leaseId, CancellationToken cancellationToken);
        Task RenewLeaseAsync(string leaseId, CancellationToken cancellationToken);
        Task<bool> ExistsAsync(CancellationToken cancellationToken);
    }

    public abstract class AzureBlob : IAzureBlob
    {
        protected BlobBaseClient blobClient;
        protected ILogger logger;

        public AzureBlob(BlobBaseClient blobClient, ILogger logger)
        {
            this.blobClient = blobClient;
            this.logger = logger;
        }

        public async Task<string> AcquireLeaseAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            var leaseClient = blobClient.GetBlobLeaseClient();
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await leaseClient.AcquireAsync(duration, null, cancellationToken);
                    logger.LogInformation($"AcquireLease for {blobClient.Uri}: success. leaseId: {resp.Value.LeaseId}");
                    return resp.Value.LeaseId;
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }
        public async Task ReleaseLease(string leaseId, CancellationToken cancellationToken)
        {
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    await leaseClient.ReleaseAsync(null, cancellationToken);
                    logger.LogInformation($"ReleaseLease for {blobClient.Uri}: success. leaseId: {leaseId} ");
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public async Task ReleaseLeaseAsync(string leaseId, CancellationToken cancellationToken)
        {
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    await leaseClient.ReleaseAsync(null, cancellationToken);
                    logger.LogInformation($"ReleaseLeaseAsync for {blobClient.Uri}: success. leaseId: {leaseId} ");
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public async Task RenewLeaseAsync(string leaseId, CancellationToken cancellationToken)
        {
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await leaseClient.RenewAsync(null, cancellationToken);
                    logger.LogInformation($"RenewLease for {blobClient.Uri}: success. leaseId: {leaseId}");
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await blobClient.ExistsAsync(cancellationToken);
                    logger.LogInformation($"ExistsAsync for {blobClient.Uri}: success. result: {resp.Value}");
                    return resp.Value;
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        protected IDictionary<string, object?> GetMessageProperties()
        {
            Dictionary<string, object?> properties = new();
            string traceId = Activity.Current?.Id ?? string.Empty;
            properties.Add(HttpHeaderNames.TraceId, traceId);
            return properties;
        }
    }
}
