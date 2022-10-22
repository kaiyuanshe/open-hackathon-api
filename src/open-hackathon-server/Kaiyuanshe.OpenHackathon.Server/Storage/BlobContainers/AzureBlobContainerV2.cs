using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Kaiyuanshe.OpenHackathon.Server.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers
{
    public interface IAzureBlobContainerV2
    {
        string BlobContainerUri { get; }
        string CreateBlobSasToken(string blobName, BlobSasPermissions permissions, DateTimeOffset expiresOn);
        Task<string> DownloadBlockBlobAsync(string blobName, CancellationToken cancellationToken);
        Task<byte[]> DownloadBlockBlobAsBytesAsync(string blobName, CancellationToken cancellationToken);
        Task UploadBlockBlobAsync(string blobName, string content, CancellationToken cancellationToken);
        Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken);
        AzurePageBlob GetPageBlob(string blobName);
    }

    public abstract class AzureBlobContainerV2 : StorageClientBase, IAzureBlobContainerV2
    {
        BlobContainerClient blobContainerClient;
        ILogger logger;
        protected abstract string ContainerName { get; }

        public override string StorageName => blobContainerClient?.AccountName ?? string.Empty;

        protected AzureBlobContainerV2(ILogger logger)
        {
            this.logger = logger;
        }

        public string BlobContainerUri
        {
            get
            {
                var client = GetBlobContainerClient();
                return client.GetParentBlobServiceClient().Uri.ToString().TrimEnd('/');
            }
        }

        public string CreateBlobSasToken(string blobName, BlobSasPermissions permissions, DateTimeOffset expiresOn)
        {
            var blobContainerClient = GetBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            var uri = blobClient.GenerateSasUri(permissions, expiresOn);
            return uri.Query;
        }

        public async Task<string> DownloadBlockBlobAsync(string blobName, CancellationToken cancellationToken)
        {
            return await ExecuteBlockBlobInternal(blobName, async (blobClient) =>
            {
                var response = await blobClient.DownloadContentAsync(cancellationToken);
                return response.Value.Content.ToString();
            }, cancellationToken);
        }

        public async Task<byte[]> DownloadBlockBlobAsBytesAsync(string blobName, CancellationToken cancellationToken)
        {
            return await ExecuteBlockBlobInternal(blobName, async (blobClient) =>
            {
                var response = await blobClient.DownloadContentAsync(cancellationToken);
                return response.Value.Content.ToArray();
            }, cancellationToken);
        }

        public async Task UploadBlockBlobAsync(string blobName, string content, CancellationToken cancellationToken)
        {
            await ExecuteBlockBlobInternal(blobName, async (blobClient) =>
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                var options = new BlobUploadOptions { };
                await blobClient.UploadAsync(stream, options, cancellationToken);
            }, cancellationToken);
        }

        public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken)
        {
            return await ExecuteBlockBlobInternal(blobName, async (blobClient) =>
            {
                return await blobClient.ExistsAsync(cancellationToken);
            }, cancellationToken);
        }

        public AzurePageBlob GetPageBlob(string blobName)
        {
            var containerClient = GetBlobContainerClient();
            var pageBlobClient = containerClient.GetPageBlobClient(blobName);
            return new AzurePageBlob(pageBlobClient, logger);
        }

        private async Task ExecuteBlockBlobInternal(string blobName, Func<BlockBlobClient, Task> func, CancellationToken cancellationToken)
        {
            await ExecuteBlockBlobInternal(blobName, async (blobClient) =>
            {
                await func(blobClient);
                return 0;
            }, cancellationToken);
        }

        private async Task<T> ExecuteBlockBlobInternal<T>(string blobName, Func<BlockBlobClient, Task<T>> func, CancellationToken cancellationToken)
        {
            var blobContainerClient = GetBlobContainerClient();
            var blobClient = blobContainerClient.GetBlockBlobClient(blobName);
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    return await func(blobClient);
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }

        private BlobContainerClient GetBlobContainerClient()
        {
            return GetBlobContainerClientInternal(null);
        }

        private BlobContainerClient GetBlobContainerClientInternal(BlobClientOptions? options)
        {
            if (blobContainerClient == null)
            {
                var conn = StorageCredentialProvider.HackathonServerStorageConnectionString;
                if (options == null)
                {
                    options = new SpecializedBlobClientOptions();
                }

                var traceIdPolicy = TraceIdHttpPipelinePolicyFactory.GetPipelinePolicy();
                options.AddPolicy(traceIdPolicy, HttpPipelinePosition.PerRetry);

                blobContainerClient = new BlobContainerClient(conn, ContainerName, options);
                logger.TraceInformation($"Building BlobContainerClient for {StorageName}.{ContainerName}.");
                using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
                {
                    try
                    {
                        blobContainerClient.CreateIfNotExists();
                    }
                    catch (RequestFailedException ex)
                    {
                        throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                    }
                }
            }

            return blobContainerClient;
        }
    }
}
