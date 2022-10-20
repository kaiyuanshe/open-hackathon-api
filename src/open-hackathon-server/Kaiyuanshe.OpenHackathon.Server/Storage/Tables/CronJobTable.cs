using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ICronJobTable : IAzureTableV2<CronJobEntity>
    {
    }

    public class CronJobTable : AzureTableV2<CronJobEntity>, ICronJobTable
    {
        protected override string TableName => TableNames.CronJob;
    }
}
