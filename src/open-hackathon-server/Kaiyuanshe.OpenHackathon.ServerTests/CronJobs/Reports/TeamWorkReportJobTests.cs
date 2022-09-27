using Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.CronJobs.Reports
{
    internal class TeamWorkReportJobTests : ReportsJobTestBase
    {
        #region GenerateReport
        [Test]
        public async Task GenerateReport()
        {
            var hackathons = new List<HackathonEntity>
            {
                new HackathonEntity{ PartitionKey = "h1", },
                new HackathonEntity{ PartitionKey = "h2", },
            };
            var teams = new List<TeamEntity>
            {
                new TeamEntity { RowKey = "t1", CreatorId = "c1" }, // teamId not match
                new TeamEntity { RowKey = "t2", CreatorId = "c2" }, // creator not found
                new TeamEntity { RowKey = "t3", CreatorId = "c3" }, // normal 
            };
            var works = new List<TeamWorkEntity>
            {
                new TeamWorkEntity { RowKey = "w1" }, // team id null 
                new TeamWorkEntity { RowKey = "w2", TeamId = "t222" }, // team id not match 
                new TeamWorkEntity { RowKey = "w3", TeamId = "t2" }, // team id not match 
                new TeamWorkEntity { RowKey = "w4", TeamId = "t3" }, // normal
            };
            var user = new UserInfo();

            var moqs = new Moqs();

            var job = new TeamWorkReportJob();
            SetupReportJob(moqs, job, hackathons);
            moqs.TeamTable.Setup(t => t.QueryEntitiesAsync("PartitionKey eq 'h1'", null, It.IsAny<CancellationToken>()));
            moqs.TeamTable.Setup(t => t.QueryEntitiesAsync("PartitionKey eq 'h2'", null, It.IsAny<CancellationToken>())).ReturnsAsync(teams);

            moqs.TeamWorkTable.Setup(e => e.ExecuteQueryAsync("PartitionKey eq 'h1'", It.IsAny<Func<TeamWorkEntity, Task>>(), null, null, It.IsAny<CancellationToken>()));
            moqs.TeamWorkTable.Setup(e => e.ExecuteQueryAsync("PartitionKey eq 'h2'", It.IsAny<Func<TeamWorkEntity, Task>>(), null, null, It.IsAny<CancellationToken>()))
                .Callback<string, Func<TeamWorkEntity, Task>, int?, IEnumerable<string>?, CancellationToken>(async (q, action, l, s, c) =>
                {
                    foreach (var e in works)
                    {
                        await action(e);
                    }
                });

            moqs.UserManagement.Setup(t => t.GetUserByIdAsync("c2", It.IsAny<CancellationToken>())).ReturnsAsync(default(UserInfo));
            moqs.UserManagement.Setup(t => t.GetUserByIdAsync("c3", It.IsAny<CancellationToken>())).ReturnsAsync(user);

            await job.ExecuteNow(null);
            moqs.VerifyAll();
        }
        #endregion
    }
}
