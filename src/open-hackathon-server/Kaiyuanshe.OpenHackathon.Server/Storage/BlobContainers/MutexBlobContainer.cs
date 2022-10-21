using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers
{
    public interface IMutexBlobContainer : IAzureBlobContainerV2 { }

    public class MutexBlobContainer : AzureBlobContainerV2, IMutexBlobContainer
    {
        protected override string ContainerName => BlobContainerNames.Mutex;

        public MutexBlobContainer(ILogger<MutexBlobContainer> logger) : base(logger)
        {
        }
    }
}
