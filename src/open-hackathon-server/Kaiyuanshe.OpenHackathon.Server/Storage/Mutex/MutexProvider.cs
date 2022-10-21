using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Mutex
{
    public interface IMutexProvider
    {
        IMutex GetInstance(string name);
    }

    public class MutexProvider : IMutexProvider
    {
        private readonly MutexBlobContainer _blobContainer;
        private readonly ILogger _logger;

        public MutexProvider(MutexBlobContainer mutexBlobContainer, ILogger<MutexProvider> logger)
        {
            _blobContainer = mutexBlobContainer;
            _logger = logger;
        }

        public IMutex GetInstance(string name)
        {
            var leaseManager = new BlobLeaseManager(_blobContainer, name);
            return new Mutex(leaseManager, _logger);
        }
    }
}
