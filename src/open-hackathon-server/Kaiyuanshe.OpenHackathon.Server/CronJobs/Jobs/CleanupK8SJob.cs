using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs
{
    public class CleanupK8SJob : CronJobBase
    {
        protected override TimeSpan Interval => TimeSpan.FromDays(1);

        public IExperimentManagement ExperimentManagement { get; set; }

        protected override async Task ExecuteAsync(CronJobContext context, CancellationToken token)
        {
            string? filter = TableQueryHelper.And(
                TableQueryHelper.FilterForBool(nameof(HackathonEntity.ExperimentCleaned), ComparisonOperator.Equal, false),
                TableQueryHelper.FilterForBool(nameof(HackathonEntity.ReadOnly), ComparisonOperator.Equal, true)
            );
            await StorageContext.HackathonTable.ExecuteQueryAsync(filter, async (hackaton) =>
            {
                await ExperimentManagement.CleanupKubernetesExperimentsAsync(hackaton.Name, token);
                await ExperimentManagement.CleanupKubernetesTemplatesAsync(hackaton.Name, token);
                // mark as cleaned
                hackaton.ExperimentCleaned = true;
                await StorageContext.HackathonTable.MergeAsync(hackaton, token);
            }, null, null, token);
        }
    }
}
