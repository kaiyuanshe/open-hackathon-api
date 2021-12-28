using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers
{
    public interface IAzureBlobContainerV2
    {
        //string CreateContainerSasToken(SharedAccessBlobPolicy policy);
    }

    public abstract class AzureBlobContainerV2 : StorageClientBase, IAzureBlobContainerV2
    {
        BlobContainerClient blobContainerClient = null;
        ILogger logger;
        protected abstract string ContainerName { get; }

        public override string StorageName => blobContainerClient?.AccountName;

        protected AzureBlobContainerV2(ILogger logger)
        {
            this.logger = logger;
        }

        private async Task<BlobContainerClient> GetBlobContainerClientInternal(BlobClientOptions options, CancellationToken cancellationToken)
        {
            if (blobContainerClient == null)
            {
                var conn = StorageCredentialProvider.HackathonServerStorageConnectionString;
                if (options == null)
                {
                    options = new SpecializedBlobClientOptions();
                    var traceIdPolicy = TraceIdHttpPipelinePolicyFactory.GetPipelinePolicy();
                    options.AddPolicy(traceIdPolicy, HttpPipelinePosition.PerRetry);

                    blobContainerClient = new BlobContainerClient(conn, ContainerName, options);
                    logger.TraceInformation($"Building BlobContainerClient for {StorageName}.{ContainerName}.");
                    using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
                    {
                        try
                        {
                            await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                        }
                        catch (RequestFailedException ex)
                        {
                            throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                        }
                    }
                }
            }

            return blobContainerClient;
        }
    }
}
