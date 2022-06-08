using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Biz
{
    public interface IActivityLogManagement
    {
        /// <summary>
        /// Log activity related to a hackathon.
        /// </summary>
        /// <param name="hackathonName">Name of hackathon, required.</param>
        /// <param name="operatorId">id of user. optional</param>
        /// <param name="args">the args to format message of the log. Can be dynamic and created by `new { p = "value" }`. MUST match the format defined in resource files.</param>
        /// <param name="resourceKey">key in Resources files. Will follow the naming convention if null.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LogHackathonActivity(string hackathonName, string operatorId, ActivityLogType logType, object args, string resourceKey = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Log activity related to a team.
        /// </summary>
        /// <param name="hackathonName">Name of hackathon. optional.</param>
        /// <param name="teamId">id of the team. required.</param>
        /// <param name="operatorId">id of user. Optional.</param>
        /// <param name="args">the args to format message of the log. Can be dynamic and created by `new { p = "value" }`. MUST match the format defined in resource files.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LogTeamActivity(string hackathonName, string teamId, string operatorId, ActivityLogType logType, object args, string resourceKey = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Log activity of a user. Call twice if two different uesrs involved. For example, add a new member(first call) to a team by team admin(second call).
        /// </summary>
        /// <param name="userId">id of the user. required.</param>
        /// <param name="hackathonName">Name of hackathon. optional.</param>
        /// <param name="operatorId">id of the operator. optional.</param>
        /// <param name="args">the args to format message of the log. Can be dynamic and created by `new { p = "value" }`. MUST match the format defined in resource files.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LogUserActivity(string userId, string hackathonName, string operatorId, ActivityLogType logType, object args, string resourceKey = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<ActivityLogEntity>> ListActivityLogs(ActivityLogQueryOptions options, CancellationToken cancellationToken = default);
    }

    public class ActivityLogManagement : ManagementClientBase<ActivityLogManagement>, IActivityLogManagement
    {
        #region LogActivity
        public async Task LogHackathonActivity(string hackathonName, string operatorId, ActivityLogType logType, object args, string resourceKey = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(hackathonName))
                return;

            await CustomizeAndSave(hackathonName, operatorId, logType, args, resourceKey, (e) =>
            {
                e.PartitionKey = hackathonName;
                e.Category = ActivityLogCategory.Hackathon;
            }, cancellationToken);
        }

        public async Task LogTeamActivity(string hackathonName, string teamId, string operatorId, ActivityLogType logType, object args, string resourceKey = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(teamId))
                return;

            await CustomizeAndSave(hackathonName, operatorId, logType, args, resourceKey, (e) =>
            {
                e.PartitionKey = teamId;
                e.Category = ActivityLogCategory.Team;
            }, cancellationToken);
        }

        public async Task LogUserActivity(string userId, string hackathonName, string operatorId, ActivityLogType logType, object args, string resourceKey = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            await CustomizeAndSave(hackathonName, operatorId, logType, args, resourceKey, (e) =>
             {
                 e.PartitionKey = userId;
                 e.Category = ActivityLogCategory.User;
             }, cancellationToken);
        }

        private string GenerateRowKey()
        {
            return $"{StorageUtils.InversedTimeKey(DateTime.UtcNow)}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        private async Task CustomizeAndSave(string hackathonName, string operatorId, ActivityLogType logType, object args, string resourceKey, Action<ActivityLogEntity> customize, CancellationToken cancellationToken)
        {
            var entity = new ActivityLogEntity
            {
                RowKey = GenerateRowKey(),
                HackathonName = hackathonName,
                OperatorId = operatorId,
                ActivityLogType = logType.ToString(),
                CreatedAt = DateTime.UtcNow,
            };
            customize(entity);
            try
            {
                entity.GenerateMessage(args, resourceKey);
                await StorageContext.ActivityLogTable.InsertAsync(entity, cancellationToken);
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
            var filter = GenerateFilter(options);
            if (string.IsNullOrEmpty(filter))
                return Array.Empty<ActivityLogEntity>();

            var continuationToken = options.ContinuationToken();
            var top = options.Top();
            var page = await StorageContext.ActivityLogTable.ExecuteQuerySegmentedAsync(filter, continuationToken, top, null, cancellationToken);
            options.NextPage = Pagination.FromContinuationToken(page.ContinuationToken, top);
            return page.Values;
        }

        private string GenerateFilter(ActivityLogQueryOptions options)
        {
            var catetory = TableQueryHelper.FilterForInt(nameof(ActivityLogEntity.Category), ComparisonOperator.Equal, (int)options.Category);

            switch (options.Category)
            {
                case ActivityLogCategory.Hackathon:
                    if (string.IsNullOrWhiteSpace(options.HackathonName))
                    {
                        return null;
                    }
                    var hackPk = TableQueryHelper.PartitionKeyFilter(options.HackathonName);
                    return TableQueryHelper.And(hackPk, catetory);
                case ActivityLogCategory.Team:
                    if (string.IsNullOrEmpty(options.TeamId))
                    {
                        return null;
                    }
                    var teamPk = TableQueryHelper.PartitionKeyFilter(options.TeamId);
                    return TableQueryHelper.And(teamPk, catetory);
                case ActivityLogCategory.User:
                    if (string.IsNullOrEmpty(options.UserId))
                    {
                        return null;
                    }
                    var userPk = TableQueryHelper.PartitionKeyFilter(options.UserId);
                    return TableQueryHelper.And(userPk, catetory);
                default:
                    return null;
            }
        }
        #endregion
    }

    public static class IActivityLogManagementExtensions
    {
        public static async Task OnHackathonEvent(this IActivityLogManagement activityLogManagement,
            string hackathonName, string operatorId, ActivityLogType logType,
            object args, CancellationToken cancellationToken)
        {
            await activityLogManagement.LogHackathonActivity(hackathonName, operatorId, logType, args, null, cancellationToken);
            await activityLogManagement.LogUserActivity(operatorId, hackathonName, operatorId, logType, args, null, cancellationToken);
        }

        public static async Task OnTeamEvent(this IActivityLogManagement activityLogManagement,
            string hackathonName, string teamId, string operatorId,
            ActivityLogType logType, object args, CancellationToken cancellationToken)
        {
            await activityLogManagement.LogHackathonActivity(hackathonName, operatorId, logType, args, null, cancellationToken);
            await activityLogManagement.LogTeamActivity(hackathonName, teamId, operatorId, logType, args, null, cancellationToken);
            await activityLogManagement.LogUserActivity(operatorId, hackathonName, operatorId, logType, args, null, cancellationToken);
        }

        public static async Task OnTeamMemberEvent(this IActivityLogManagement activityLogManagement,
            string hackathonName, string teamId, string memberId,
            string operatorId, ActivityLogType logType, object args,
            string resourceKeyOfMember = null, string resourceKeyOfOperator = null, CancellationToken cancellationToken = default)
        {
            await activityLogManagement.LogHackathonActivity(hackathonName, operatorId, logType, args, null, cancellationToken);
            await activityLogManagement.LogTeamActivity(hackathonName, teamId, operatorId, logType, args, null, cancellationToken);
            await activityLogManagement.LogUserActivity(operatorId, hackathonName, operatorId, logType, args, resourceKeyOfOperator, cancellationToken);
            await activityLogManagement.LogUserActivity(memberId, hackathonName, operatorId, logType, args, resourceKeyOfMember, cancellationToken);
        }

        public static async Task OnUserEvent(this IActivityLogManagement activityLogManagement,
           string hackathonName, string userId, string operatorId, ActivityLogType logType, object args,
           string resourceKeyOfUser = null, string resourceKeyOfOperator = null, CancellationToken cancellationToken = default)
        {
            await activityLogManagement.LogHackathonActivity(hackathonName, operatorId, logType, args, null, cancellationToken);
            await activityLogManagement.LogUserActivity(operatorId, hackathonName, operatorId, logType, args, resourceKeyOfOperator, cancellationToken);
            await activityLogManagement.LogUserActivity(userId, hackathonName, operatorId, logType, args, resourceKeyOfUser, cancellationToken);
        }
    }
}
