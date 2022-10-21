using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Kaiyuanshe.OpenHackathon.Server.Storage.Blobs;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Mutex
{
    internal class BlobLeaseManager
    {
        private readonly MutexBlobContainer mutexBlobContainer;
        private readonly string blobName;
        private AzurePageBlob _leaseBlob;

        public BlobLeaseManager(MutexBlobContainer mutexBlobContainer, string blobName)
        {
            this.mutexBlobContainer = mutexBlobContainer;
            this.blobName = blobName;
        }

        public async Task ReleaseLease(string leaseId, CancellationToken cancellationToken)
        {
            try
            {
                if (_leaseBlob != null)
                {
                    await _leaseBlob.ReleaseLease(leaseId, cancellationToken);

                }
            }
            catch (AzureStorageException)
            {
                // lease will eventually be released after timeout.
            }
        }

        public async Task<string?> AcquireLeaseAsync(TimeSpan holdingPeriod, CancellationToken cancellationToken)
        {
            bool blobNotFound = false;
            try
            {
                var blob = GetBlobReference();
                return await blob.AcquireLeaseAsync(holdingPeriod, cancellationToken);
            }
            catch (AzureStorageException ex)
            {
                if (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    blobNotFound = true;
                }
                else
                {
                    return null;
                }
            }

            if (blobNotFound)
            {
                await CreateBlobAsync(cancellationToken);
                return await AcquireLeaseAsync(holdingPeriod, cancellationToken);
            }

            return null;
        }

        public async Task<bool> RenewLeaseAsync(string leaseId, CancellationToken cancellationToken)
        {
            try
            {
                var blob = GetBlobReference();
                await blob.RenewLeaseAsync(leaseId, cancellationToken);
                return true;
            }
            catch (AzureStorageException)
            {
                return false;
            }
        }

        private async Task CreateBlobAsync(CancellationToken cancellationToken)
        {
            var blob = GetBlobReference();
            if (!await blob.ExistsAsync(cancellationToken))
            {
                try
                {
                    await blob.CreateAsync(0, cancellationToken);
                }
                catch (AzureStorageException ex)
                {
                    if (ex.Status != (int)HttpStatusCode.PreconditionFailed)
                    {
                        throw;
                    }
                }
            }
        }

        private AzurePageBlob GetBlobReference()
        {
            if (_leaseBlob != null)
                return _leaseBlob;

            _leaseBlob = mutexBlobContainer.GetPageBlob(blobName);
            return _leaseBlob;
        }
    }

}
