using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class TeamMemberTableTests
    {
        [Test]
        public async Task GetMemberCountAsync()
        {
            string hackathonName = "hack";
            string teamId = "tid";

            var logger = new Mock<ILogger<TeamMemberTable>>();

            var awardTable = new Mock<TeamMemberTable>(logger.Object) { };
            await awardTable.Object.GetMemberCountAsync(hackathonName, teamId, default);

            awardTable.Verify(t => t.QueryEntitiesAsync("(PartitionKey eq 'hack') and (TeamId eq 'tid')",
                It.IsAny<IEnumerable<string>>(),
                default), Times.Once);
            awardTable.VerifyNoOtherCalls();
        }
    }
}
