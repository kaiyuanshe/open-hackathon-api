using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    internal class HackathonTableTests
    {
        [Test]
        public async Task ListAllHackathonsAsync()
        {
            var list = new List<HackathonEntity> { new HackathonEntity { PartitionKey = "pk" } };

            var table = new Mock<HackathonTable>();
            table.Setup(a => a.QueryEntitiesAsync("Status ne 3", null, default)).ReturnsAsync(list);

            var resp = await table.Object.ListAllHackathonsAsync(default);

            table.Verify(t => t.QueryEntitiesAsync("Status ne 3", null, default), Times.Once);
            table.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
    }
}
