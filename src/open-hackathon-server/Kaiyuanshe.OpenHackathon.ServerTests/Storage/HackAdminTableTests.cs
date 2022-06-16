using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    [TestFixture]
    public class HackAdminTableTests
    {
        [Test]
        public async Task ListParticipantsByHackathonAsyncTest()
        {
            var table = new Mock<HackathonAdminTable>() { CallBase = true };

            List<HackathonAdminEntity> participants = new List<HackathonAdminEntity>
            {
                new HackathonAdminEntity{ PartitionKey="pk1" },
                new HackathonAdminEntity{}
            };
            table.Setup(t => t.QueryEntitiesAsync("PartitionKey eq 'foo'", null, default)).ReturnsAsync(participants);

            var results = await table.Object.ListByHackathonAsync("foo", default);
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("pk1", results.First().HackathonName);
        }
    }
}
