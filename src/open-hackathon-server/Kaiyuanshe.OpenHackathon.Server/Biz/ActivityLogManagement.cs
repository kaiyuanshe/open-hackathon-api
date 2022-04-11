using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IActivityLogManagement
    {
        Task LogActivity(ActivityLogEntity entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<ActivityLogEntity>> ListActivityLogs(ActivityLogQueryOptions options, CancellationToken cancellationToken = default);
    }

    public class ActivityLogManagement : ManagementClientBase, IActivityLogManagement
    {
        private readonly ILogger Logger;

        public ActivityLogManagement(ILogger<ActivityLogManagement> logger)
        {
            Logger = logger;
        }

        #region LogActivity
        public async Task LogActivity(ActivityLogEntity entity, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(entity?.ActivityLogType))
                return;

            // override any input
            entity.RowKey = $"{StorageUtils.InversedTimeKey(DateTime.UtcNow) }-{Guid.NewGuid().ToString().Substring(0, 8)}";
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
        #endregion

        #region ListActivityLogs
        public async Task<IEnumerable<ActivityLogEntity>> ListActivityLogs(ActivityLogQueryOptions options, CancellationToken cancellationToken = default)
        {
            string filter;
            if (!string.IsNullOrWhiteSpace(options.HackathonName))
            {
                var pk = TableQueryHelper.PartitionKeyFilter(options.HackathonName);
                var catetory = TableQueryHelper.FilterForInt(nameof(ActivityLogEntity.Category), ComparisonOperator.Equal, (int)ActivityLogCategory.Hackathon);
                if (!string.IsNullOrWhiteSpace(options.UserId))
                {
                    var userIdFilter = TableQueryHelper.FilterForString(nameof(ActivityLogEntity.UserId), ComparisonOperator.Equal, options.UserId);
                    filter = TableQueryHelper.And(pk, catetory, userIdFilter);
                }
                else
                {
                    filter = TableQueryHelper.And(pk, catetory);
                }
            }
            else if (!string.IsNullOrWhiteSpace(options.UserId))
            {
                var pk = TableQueryHelper.PartitionKeyFilter(options.UserId);
                var catetory = TableQueryHelper.FilterForInt(nameof(ActivityLogEntity.Category), ComparisonOperator.Equal, (int)ActivityLogCategory.User);
                filter = TableQueryHelper.And(pk, catetory);
            }
            else
            {
                filter = TableQueryHelper.FilterForInt(nameof(ActivityLogEntity.Category), ComparisonOperator.Equal, (int)ActivityLogCategory.User);
            }

            var continuationToken = options.ContinuationToken();
            var top = options.Top();

            var page = await StorageContext.ActivityLogTable.ExecuteQuerySegmentedAsync(filter, continuationToken, top, null, cancellationToken);
            options.NextPage = Pagination.FromContinuationToken(page.ContinuationToken, top);
            return page.Values;
        }
        #endregion
    }
}
