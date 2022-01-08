using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface IUserActivityLogTable : IAzureTableV2<UserActivityLogEntity>
    {
        Task LogActivity(UserActivityLogEntity entity, CancellationToken cancellationToken = default);
    }

    public class UserActivityLogTable : AzureTableV2<UserActivityLogEntity>, IUserActivityLogTable
    {
        protected override string TableName => TableNames.UserActivityLog;

        public UserActivityLogTable(ILogger logger) : base(logger)
        {
        }

        public async Task LogActivity(UserActivityLogEntity entity, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(entity?.UserId))
                return;

            if (string.IsNullOrEmpty(entity.ActivityLogType))
                return;

            entity.RowKey ??= Guid.NewGuid().ToString();
            try
            {
                await InsertAsync(entity, cancellationToken);
            }
            catch
            {
                // ignore any exception
            }
        }
    }
}
