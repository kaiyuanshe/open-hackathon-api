using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Mutex;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs
{
    public class CronJobContext
    {
        /// <summary>
        /// if true, won't execute until interval timespan goes by. If false, run it now no matter when job was run last time.
        /// </summary>
        public bool EnforceInterval { get; set; }

        /// <summary>
        /// Time when the current run is triggered
        /// </summary>
        public DateTime FireTime { get; set; }
    }

    public interface ICronJob : IJob
    {
        /// <summary>
        /// CronJob trigger. https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/more-about-triggers.html#calendars
        /// </summary>
        ITrigger Trigger { get; }

        /// <summary>
        /// Get or set the Quartz.IJobDetail.JobDataMap that is associated with the Quartz.IJob.
        /// docs: https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/more-about-jobs.html#jobdatamap
        /// </summary>
        JobDataMap JobDataMap { get; }

        /// <summary>
        /// Execute a CronJob immediately.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task ExecuteNow(CronJobContext context);
    }

    public abstract class CronJobBase : ICronJob
    {
        public ILoggerFactory LoggerFactory { get; set; }
        public ICacheProvider CacheProvider { get; set; }
        public IStorageContext StorageContext { get; set; }

        protected ILogger Logger
        {
            get
            {
                return LoggerFactory.CreateLogger("CronJob");
            }
        }

        public virtual JobDataMap JobDataMap
        {
            get
            {
                return new JobDataMap();
            }
        }

        public Task Execute(IJobExecutionContext context)
        {
            CronJobContext cronJobContext = new CronJobContext
            {
                EnforceInterval = true,
                FireTime = context.FireTimeUtc.DateTime,
            };
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            return ExecuteAsync(cronJobContext, cancellationTokenSource.Token);
        }

        public Task ExecuteNow(CronJobContext? context)
        {
            context = context ?? new CronJobContext();
            context.EnforceInterval = true;
            context.FireTime = DateTime.UtcNow;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            return ExecuteAsync(context, cancellationTokenSource.Token);
        }

        protected abstract Task ExecuteAsync(CronJobContext context, CancellationToken token);

        /// <summary>
        /// Interval between job runs
        /// </summary>
        protected abstract TimeSpan Interval { get; }
        public virtual ITrigger Trigger
        {
            get
            {
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(GetType().Name)
                    .StartNow() // trigger it now
                    .WithSimpleSchedule(x => x.WithInterval(Interval).RepeatForever()) // execute forever with interval
                    .Build();
                return trigger;
            }
        }
    }

    public abstract class NonConcurrentCronJob : CronJobBase
    {
        protected virtual int AcquireLeaseFailureAlertThreshold => 5;
        private int AcquireLeaseFailureCount = 0;

        public IMutexProvider MutexProvider { get; set; }

        /// <summary>
        /// The name of the job. Default to the class name.
        /// </summary>
        public virtual string JobName
        {
            get
            {
                Type t = GetType();
                return t.Name;
            }
        }

        protected abstract Task ExecuteExclusivelyAsync(CancellationToken token);

        /// <summary>
        /// Check if the job schedule interval time has met, or just return true if we can bypass the check.
        /// </summary>
        private async Task<bool> IsTimeToRunJobAsync(string jobName, TimeSpan interval, bool enforceInterval, DateTime fireTime, CancellationToken cancellationToken)
        {
            if (!enforceInterval)
            {
                return true;
            }
            var job = await StorageContext.CronJobTable.RetrieveAsync(jobName, jobName, cancellationToken);
            if (job != null)
            {
                return (fireTime - job.LastExecuteTime + TimeSpan.FromSeconds(1)) >= interval && !job.Paused;
            }
            return true;
        }

        protected sealed override async Task ExecuteAsync(CronJobContext context, CancellationToken token)
        {
            var jobName = JobName;
            var interval = Interval;

            var ready = await IsTimeToRunJobAsync(jobName, interval, context.EnforceInterval, context.FireTime, token);
            if (!ready)
            {
                // Check before acquiring the lock to return early without holding the lock.
                Logger.TraceInformation($"CronJob `{jobName}` skipped. either too frequent or paused.");
                return;
            }

            var mutexName = $"CronJob/{jobName}.lock";
            var jobMutexProvider = MutexProvider.GetInstance(mutexName);
            try
            {
                var mutexCtx = await jobMutexProvider.TryLockAsync(token);
                if (mutexCtx == null)
                {
                    Logger.TraceInformation($"Skip Cron Job `{jobName}` as it is currently ran by other runner");
                    var count = Interlocked.Increment(ref AcquireLeaseFailureCount);
                    if (count >= AcquireLeaseFailureAlertThreshold)
                    {
                        Logger.TraceInformation($"Failed to acquire lease: {jobName}");
                    }
                    return;
                }

                Interlocked.Exchange(ref AcquireLeaseFailureCount, 0);
                using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, mutexCtx.LockReleased))
                {
                    var linkedToken = linkedTokenSource.Token;

                    ready = await IsTimeToRunJobAsync(jobName, interval, context.EnforceInterval, context.FireTime, linkedToken);
                    if (!ready)
                    {
                        // Some other runner has jumped in after we check ready and before we acquired the lock.
                        // Should be rare.
                        return;
                    }

                    var job = new CronJobEntity
                    {
                        PartitionKey = jobName,
                        RowKey = jobName,
                        LastExecuteTime = context.FireTime,
                    };
                    await StorageContext.CronJobTable.InsertOrReplaceAsync(job, linkedToken);

                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        Logger.TraceInformation($"Begin to execute/validate Cronjob `{jobName}` at {DateTime.UtcNow}");
                        await ExecuteExclusivelyAsync(linkedToken);
                    }
                    catch (OperationCanceledException oce)
                    {
                        if (token.IsCancellationRequested)
                        {
                            // Cancelled by the upstream caller, just propagate the cancellation
                            Logger.TraceInformation("Job cancelled by the upstream caller");
                            throw;
                        }
                        else
                        {
                            // Cancelled when the blob lease lock is lost
                            Logger.TraceError($"crob job lease lost: {jobName}", oce);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.TraceError($"unhandled exeception in cron job {jobName}: {ex.Message}", ex);
                        throw;
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }

                    if (stopwatch.Elapsed > interval)
                    {
                        // The job ran longer than the interval. This means the job cannot be scheduled on time, and we need to check
                        // * if the interval setting is reasonable
                        // * the job is capable of handling the current volumes.
                        Logger.TraceInformation($"The job ran longer than the interval: {jobName}");
                    }
                    else
                    {
                        Logger.TraceInformation($"Cron job {jobName} started at {job.LastExecuteTime} and completed in {stopwatch.Elapsed}");
                    }
                }
            }
            finally
            {
                await jobMutexProvider.TryReleaseAsync(token);
            }
        }
    }
}
