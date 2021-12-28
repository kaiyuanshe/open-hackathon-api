using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers
{
    /// <summary>
    /// BlobContainer for user-uploaded files. Make sure to enable Static Website on the storage account.
    /// Reference: https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website
    /// </summary>
    public interface IUserBlobContainer : IAzureBlobContainerV2
    {
    }

    public class UserBlobContainer : AzureBlobContainerV2, IUserBlobContainer
    {
        protected override string ContainerName => BlobContainerNames.StaticWebsite;

        public UserBlobContainer(ILogger<UserBlobContainer> logger) : base(logger)
        {
        }
    }
}
