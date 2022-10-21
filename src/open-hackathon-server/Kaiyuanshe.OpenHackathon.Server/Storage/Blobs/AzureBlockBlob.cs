using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Blobs
{
    public class AzureBlockBlob : AzureBlob
    {
        public AzureBlockBlob(BlockBlobClient blobClient, ILogger logger)
            : base(blobClient, logger)
        {
        }
    }
}
