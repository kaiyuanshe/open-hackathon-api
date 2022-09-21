using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers
{
    public interface IReportsContainer : IAzureBlobContainerV2 { }

    public class ReportsContainer : AzureBlobContainerV2, IReportsContainer
    {
        protected override string ContainerName => BlobContainerNames.Reports;

        public ReportsContainer(ILogger<UserBlobContainer> logger) : base(logger)
        {
        }
    }
}
