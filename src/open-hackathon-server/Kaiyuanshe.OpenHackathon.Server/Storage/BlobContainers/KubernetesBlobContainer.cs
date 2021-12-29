using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers
{
    public interface IKubernetesBlobContainer : IAzureBlobContainerV2
    {
    }

    public class KubernetesBlobContainer : AzureBlobContainerV2, IKubernetesBlobContainer
    {
        protected override string ContainerName => BlobContainerNames.Kubernetes;

        public KubernetesBlobContainer(ILogger<KubernetesBlobContainer> logger) : base(logger)
        {
        }
    }
}
