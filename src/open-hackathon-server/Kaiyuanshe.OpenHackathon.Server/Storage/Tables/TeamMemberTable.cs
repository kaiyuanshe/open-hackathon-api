using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITeamMemberTable : IAzureTableV2<TeamMemberEntity>
    {
        /// <summary>
        /// Get count of all team members including pendingApproval ones.
        /// </summary>
        Task<int> GetMemberCountAsync(string hackathonName, string teamId, CancellationToken cancellationToken = default);
    }

    public class TeamMemberTable : AzureTableV2<TeamMemberEntity>, ITeamMemberTable
    {
        protected override string TableName => TableNames.TeamMember;

        public TeamMemberTable(ILogger<TeamMemberTable> logger) : base(logger)
        {
        }

        #region GetMemberCountAsync
        public async Task<int> GetMemberCountAsync(string hackathonName, string teamId, CancellationToken cancellationToken = default)
        {
            var pkFilter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            var teamIdFilter = TableQueryHelper.FilterForString(nameof(TeamMemberEntity.TeamId), ComparisonOperator.Equal, teamId);
            var filter = TableQueryHelper.And(pkFilter, teamIdFilter);

            var select = new List<string> { nameof(TeamMemberEntity.RowKey) };
            var entities = await QueryEntitiesAsync(filter, select, cancellationToken);
            return entities.Count();
        }
        #endregion
    }
}
