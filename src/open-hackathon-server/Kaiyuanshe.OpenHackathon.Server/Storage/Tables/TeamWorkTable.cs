using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITeamWorkTable : IAzureTableV2<TeamWorkEntity>
    {
        Task<IEnumerable<TeamWorkEntity>> ListByTeamAsync(string hackathonName, string teamId, CancellationToken cancellationToken = default);
    }

    public class TeamWorkTable : AzureTableV2<TeamWorkEntity>, ITeamWorkTable
    {
        protected override string TableName => TableNames.TeamWork;

        public async Task<IEnumerable<TeamWorkEntity>> ListByTeamAsync(string hackathonName, string teamId, CancellationToken cancellationToken = default)
        {
            var pkFilter = TableQueryHelper.PartitionKeyFilter(hackathonName);
            var teamIdFilter = TableQueryHelper.FilterForString(nameof(TeamWorkEntity.TeamId), ComparisonOperator.Equal, teamId);
            var filter = TableQueryHelper.And(pkFilter, teamIdFilter);
            return await QueryEntitiesAsync(filter, null, cancellationToken);
        }
    }
}
