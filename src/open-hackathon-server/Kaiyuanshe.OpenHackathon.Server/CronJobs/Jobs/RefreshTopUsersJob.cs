﻿using Kaiyuanshe.OpenHackathon.Server.Biz;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs
{
    public class RefreshTopUsersJob : NonConcurrentCronJob
    {
        protected override TimeSpan Interval => TimeSpan.FromHours(12);

        public IActivityLogManagement ActivityLogManagement { get; set; }

        protected override async Task ExecuteExclusivelyAsync(CancellationToken token)
        {
            Logger.LogInformation($"RefreshTopUsersJob triggered at {DateTime.UtcNow}");

            var scores = await ActivityLogManagement.CountActivityByUser(365, token);
            await StorageContext.TopUserTable.BatchUpdateTopUsers(scores, token);
        }
    }
}
