using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Mutex
{
    public interface IMutex
    {
        Task<IMutexContext?> TryLockAsync(CancellationToken cancellationToken);
        Task TryReleaseAsync(CancellationToken cancellationToken = default);
    }

    internal class Mutex : IMutex
    {
        // maximum holding period is 1 minute
        private static readonly TimeSpan HoldingPeriod = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan RenewInterval = TimeSpan.FromSeconds(18);

        private readonly BlobLeaseManager _leaseManager;
        private readonly ILogger _logger;

        private string? _leaseId;
        private Task? _renewLeaseTask;
        private CancellationTokenSource? _cancelRenewTokenSource;
        private Stopwatch? _leaseHoldingPeriod;
        private CancellationTokenSource? _lockReleasedSource;
        private object _lockObj = new object();

        internal MutexContext? Context
        {
            get
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_leaseId) && _lockReleasedSource != null)
                    {
                        return new MutexContext
                        {
                            LeaseId = _leaseId,
                            LockReleased = _lockReleasedSource.Token,
                        };
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        internal Mutex(BlobLeaseManager leaseManager, ILogger logger)
        {
            _leaseManager = leaseManager;
            _logger = logger;
        }

        public async Task<IMutexContext?> TryLockAsync(CancellationToken cancellationToken)
        {
            lock (_lockObj)
            {
                if (!string.IsNullOrEmpty(_leaseId))
                {
                    return Context;
                }
            }

            var leaseId = await _leaseManager.AcquireLeaseAsync(HoldingPeriod, cancellationToken);
            if (!string.IsNullOrEmpty(leaseId))
            {
                // we got the global lease
                lock (_lockObj)
                {
                    // clear outdated lock
                    _cancelRenewTokenSource?.Cancel();
                    _lockReleasedSource?.Cancel();

                    _cancelRenewTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _lockReleasedSource = new CancellationTokenSource();
                    _leaseHoldingPeriod = Stopwatch.StartNew();
                    _renewLeaseTask = KeepRenewingLease(leaseId, RenewInterval, _lockReleasedSource, _leaseHoldingPeriod, _cancelRenewTokenSource.Token);
                    _leaseId = leaseId;

                    return Context;
                }
            }
            else
            {
                // some other thread may got the lock (although not recommended)
                return Context;
            }
        }

        public async Task TryReleaseAsync(CancellationToken cancellationToken)
        {
            Task? renewLeaseTask = null;
            string? idToRelease = null;

            lock (_lockObj)
            {
                if (string.IsNullOrEmpty(_leaseId))
                {
                    return;
                }

                _lockReleasedSource?.Cancel();
                _cancelRenewTokenSource?.Cancel();
                idToRelease = _leaseId;
                _leaseHoldingPeriod?.Stop();

                renewLeaseTask = _renewLeaseTask;

                _leaseId = null;
                _renewLeaseTask = null;
                _cancelRenewTokenSource = null;
                _lockReleasedSource = null;
                _leaseHoldingPeriod = null;
            }

            if (idToRelease != null)
            {
                await _leaseManager.ReleaseLease(idToRelease, cancellationToken);
            }
            if (renewLeaseTask != null)
            {
                await renewLeaseTask;
            }
        }

        private async Task KeepRenewingLease(string leaseId, TimeSpan interval, CancellationTokenSource lockReleased, Stopwatch holdingPeriod, CancellationToken token)
        {
            var renewOffset = new Stopwatch();
            var failCount = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    renewOffset.Restart();
                    var renewed = await _leaseManager.RenewLeaseAsync(leaseId, token);
                    renewOffset.Stop();

                    if (!renewed)
                    {
                        failCount++;
                        if (failCount > 3)
                        {
                            lockReleased.Cancel();
                            return;
                        }
                    }
                    else
                    {
                        failCount = 0;
                    }

                    var renewIntervalAdjusted = interval - renewOffset.Elapsed;
                    if (renewIntervalAdjusted > TimeSpan.Zero)
                    {
                        await Task.Delay(renewIntervalAdjusted, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // The process can be cancelled by the token passed in.
                    // When this happens, it means we are cancelling this renew process actively,
                    // and the lock will be released properly from the call site.
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"fail to renew release: {_leaseId}");
                    // Release lock to enable outside exit correctly.
                    lockReleased.Cancel();
                    throw;
                }
            }
        }
    }

    public static class IDistributedMutexExtension
    {
        public static async Task<IMutexContext?> TryLockUntilTimeoutAsync(
            this IMutex mutex,
            TimeSpan timeout,
            int retryIntervalMS = 200,

            CancellationToken cancellationToken = default(CancellationToken))
        {
            IMutexContext? mutexCtx = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            do
            {
                mutexCtx = await mutex.TryLockAsync(cancellationToken);
                if (mutexCtx == null)
                {
                    await Task.Delay(retryIntervalMS);
                }
            } while (mutexCtx == null && stopwatch.Elapsed < timeout);
            stopwatch.Stop();

            return mutexCtx;
        }

        public static async Task<IMutexContext> LockUntilTimeoutAsync(
            this IMutex mutex,
            TimeSpan timeout,
            int retryIntervalMS = 200,

            CancellationToken cancellationToken = default(CancellationToken))
        {
            var ctx = await mutex.TryLockUntilTimeoutAsync(timeout, retryIntervalMS, cancellationToken);
            if (ctx == null)
            {
                throw new TimeoutException("Failed to obtain lock");
            }
            return ctx;
        }
    }
}
