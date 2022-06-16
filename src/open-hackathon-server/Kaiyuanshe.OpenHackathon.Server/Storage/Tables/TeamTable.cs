using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Tables
{
    public interface ITeamTable : IAzureTableV2<TeamEntity>
    {
    }

    public class TeamTable : AzureTableV2<TeamEntity>, ITeamTable
    {
        protected override string TableName => TableNames.Team;
    }
}
