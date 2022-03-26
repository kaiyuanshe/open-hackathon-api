using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs
{
    public class CleanupExperimentJob : CronJobBase
    {
        protected override TimeSpan Interval => TimeSpan.FromDays(1);

        public IExperimentManagement ExperimentManagement { get; set; }

        protected override async Task ExecuteAsync(CronJobContext context, CancellationToken token)
        {
            string filter = TableQueryHelper.FilterForBool(nameof(HackathonEntity.ExperimentCleaned), ComparisonOperator.Equal, false);
            await StorageContext.HackathonTable.ExecuteQueryAsync(filter, async (hackaton) =>
            {
                if (hackaton.ReadOnly)
                {
                    await ExperimentManagement.DeleteExperimentsAsync(hackaton.Name, token);
                }
            }, null, null, token);
        }
    }
}
