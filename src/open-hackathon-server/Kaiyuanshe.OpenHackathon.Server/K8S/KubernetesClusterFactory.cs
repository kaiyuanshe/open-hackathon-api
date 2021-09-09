﻿using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.K8S
{
    public interface IKubernetesClusterFactory
    {
        Task<IKubernetesCluster> GetDefaultKubernetes(CancellationToken cancellationToken);
    }

    public class KubernetesClusterFactory : IKubernetesClusterFactory
    {
        IKubernetesCluster _default;
        static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public IKubernetesConfigProvider KubernetesConfigProvider { get; set; }

        public async Task<IKubernetesCluster> GetDefaultKubernetes(CancellationToken cancellationToken)
        {
            if (_default != null)
            {
                return _default;
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_default != null)
                {
                    return _default;
                }

                var config = await KubernetesConfigProvider.GetDefaultConfigAsync(cancellationToken);
                _default = new KubernetesCluster(config);
                return _default;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
