using Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.CronJobs
{
    internal class RefreshTopUsersJobTests
    {
        [Test]
        public async Task ExecuteAsync()
        {
            var dic = new Dictionary<string, int>();

            var moqs = new Moqs();
            moqs.ActivityLogManagement.Setup(a => a.CountActivityByUser(365, It.IsAny<CancellationToken>())).ReturnsAsync(dic);
            moqs.TopUserTable.Setup(t => t.BatchUpdateTopUsers(dic, It.IsAny<CancellationToken>()));

            var job = new RefreshTopUsersJob();
            moqs.SetupNonCurrentCronJob(job);
            job.ActivityLogManagement = moqs.ActivityLogManagement.Object;

            await job.ExecuteNow(null);
            moqs.VerifyAll();
        }
    }
}
