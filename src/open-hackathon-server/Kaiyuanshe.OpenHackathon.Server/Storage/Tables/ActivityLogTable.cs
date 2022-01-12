using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IActivityLogTable : IAzureTableV2<ActivityLogEntity>
    {
        
    }

    public class ActivityLogTable : AzureTableV2<ActivityLogEntity>, IActivityLogTable
    {
        protected override string TableName => TableNames.UserActivityLog;

        public ActivityLogTable(ILogger<ActivityLogTable> logger) : base(logger)
        {
        }
    }
}
