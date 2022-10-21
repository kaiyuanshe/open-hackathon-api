using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Blobs
{
    public interface IAzurePageBlob
    {
        Task CreateAsync(long size, CancellationToken cancellationToken);
    }

    public class AzurePageBlob : AzureBlob, IAzurePageBlob
    {
        public AzurePageBlob(PageBlobClient blobClient, ILogger logger)
            : base(blobClient, logger)
        {
        }

        public async Task CreateAsync(long size, CancellationToken cancellationToken)
        {
            PageBlobClient pageBlobClient = (PageBlobClient)blobClient;
            using (HttpPipeline.CreateHttpMessagePropertiesScope(GetMessageProperties()))
            {
                try
                {
                    var resp = await pageBlobClient.CreateAsync(size, null, cancellationToken);
                    logger.LogInformation($"CreateAsync for {blobClient.Uri}: success. result: {resp.Value}");
                }
                catch (RequestFailedException ex)
                {
                    throw new AzureStorageException(ex.Status, ex.Message, ex.ErrorCode, ex);
                }
            }
        }
    }

}
