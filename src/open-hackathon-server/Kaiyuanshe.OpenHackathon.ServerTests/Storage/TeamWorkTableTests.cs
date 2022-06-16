using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class TeamWorkTableTests
    {
        [Test]
        public async Task ListByTeamAsync()
        {
            var entities = new List<TeamWorkEntity>
            {
                new TeamWorkEntity{ },
            };

            var teamWorkTable = new Mock<TeamWorkTable>() { };
            teamWorkTable.Setup(t => t.QueryEntitiesAsync("(PartitionKey eq 'hack') and (TeamId eq 'tid')", null, default)).ReturnsAsync(entities);

            await teamWorkTable.Object.ListByTeamAsync("hack", "tid", default);

            Mock.VerifyAll(teamWorkTable);
            teamWorkTable.VerifyNoOtherCalls();
        }
    }
}
