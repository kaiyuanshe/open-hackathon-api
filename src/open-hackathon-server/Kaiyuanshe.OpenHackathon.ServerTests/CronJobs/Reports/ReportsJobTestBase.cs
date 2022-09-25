using Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kaiyuanshe.OpenHackathon.ServerTests.CronJobs.Reports
{
    internal class ReportsJobTestBase
    {
        protected void SetupReportJob<T>(Moqs moqs, ReportsBaseJob<T> job, List<HackathonEntity> hackathons)
        {
            moqs.SetupCronJob(job);

            moqs.HackathonTable.Setup(h => h.ListAllHackathonsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathons.ToDictionary(h => h.Name, h => h));
            foreach (var h in hackathons)
            {
                var blobName = $"{h.Name}/{job.ReportType}.csv";
                moqs.ReportsContainer.Setup(c => c.ExistsAsync(blobName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                moqs.ReportsContainer.Setup(c => c.UploadBlockBlobAsync(blobName, It.IsAny<string>(), It.IsAny<CancellationToken>()));
            }
        }
    }
}
