using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.Extensions.Logging;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITeamTable : IAzureTableV2<TeamEntity>
    {
    }

    public class TeamTable : AzureTableV2<TeamEntity>, ITeamTable
    {
        protected override string TableName => TableNames.Team;

        public TeamTable(ILogger<TeamTable> logger) : base(logger)
        {
        }
    }
}
