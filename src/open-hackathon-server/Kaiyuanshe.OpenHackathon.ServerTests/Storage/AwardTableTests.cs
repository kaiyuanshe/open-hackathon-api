using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class AwardTableTests
    {
        [Test]
        public async Task ListAllAwardsAsync()
        {
            string hackathonName = "hack";
            var list = new List<AwardEntity> { new AwardEntity() };

            var awardTable = new Mock<AwardTable>();
            awardTable.Setup(a => a.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(list);

            var resp = await awardTable.Object.ListAllAwardsAsync(hackathonName, default);

            awardTable.Verify(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default), Times.Once);
            awardTable.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
    }
}
