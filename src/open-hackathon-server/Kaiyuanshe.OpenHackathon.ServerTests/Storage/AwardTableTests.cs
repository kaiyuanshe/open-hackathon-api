using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class AwardTableTests
    {
        [Test]
        public async Task ListAllAwardsAsync()
        {
            string hackathonName = "hack";

            var logger = new Mock<ILogger<AwardTable>>();
            var awardTable = new Mock<AwardTable>(logger.Object);
            awardTable.Setup(a => a.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(new List<AwardEntity>());

            await awardTable.Object.ListAllAwardsAsync(hackathonName, default);

            awardTable.Verify(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default), Times.Once);
            awardTable.VerifyNoOtherCalls();
        }
    }
}
