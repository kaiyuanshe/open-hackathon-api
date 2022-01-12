using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IActivityLogManagement
    {
        Task LogActivity(ActivityLogEntity entity, CancellationToken cancellationToken = default);
    }

    public class ActivityLogManagement : ManagementClientBase, IActivityLogManagement
    {
        private readonly ILogger Logger;

        public ActivityLogManagement(ILogger<ActivityLogManagement> logger)
        {
            Logger = logger;
        }

        public async Task LogActivity(ActivityLogEntity entity, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(entity?.ActivityLogType))
                return;

            entity.RowKey = Guid.NewGuid().ToString(); // override any input
            entity.CreatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(entity.UserId))
            {
                await CloneAndSave(entity, (e) =>
                {
                    e.PartitionKey = entity.UserId;
                    e.Category = ActivityLogCategory.User;
                }, cancellationToken);
            }

            if (!string.IsNullOrEmpty(entity.HackathonName))
            {
                await CloneAndSave(entity, (e) =>
                {
                    e.PartitionKey = entity.HackathonName;
                    e.Category = ActivityLogCategory.Hackathon;
                }, cancellationToken);
            }
        }

        private async Task CloneAndSave(ActivityLogEntity entity, Action<ActivityLogEntity> customize, CancellationToken cancellationToken)
        {
            var clone = entity.Clone();
            customize(clone);
            try
            {
                await StorageContext.ActivityLogTable.InsertAsync(clone, cancellationToken);
            }
            catch
            {
                // ignore any exception
            }
        }
    }
}
