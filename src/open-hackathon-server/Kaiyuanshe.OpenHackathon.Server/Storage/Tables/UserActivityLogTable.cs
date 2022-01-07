using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IUserActivityLogTable : IAzureTableV2<UserActivityLogEntity>
    {
    }

    public class UserActivityLogTable : AzureTableV2<UserActivityLogEntity>, IUserActivityLogTable
    {
        protected override string TableName => TableNames.UserActivityLog;
      
        public UserActivityLogTable(ILogger logger) : base(logger)
        {
        }
    }
}
