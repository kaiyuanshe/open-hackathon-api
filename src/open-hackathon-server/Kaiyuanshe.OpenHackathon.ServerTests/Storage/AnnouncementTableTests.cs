using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    internal class AnnouncementTableTests
    {
        #region ListByHackathonAsync
        [Test]
        public async Task ListByHackathonAsync()
        {
            string hackathonName = "hack";
            var list = new List<AnnouncementEntity> { new AnnouncementEntity() };

            var table = new Mock<AnnouncementTable>() { };
            table.Setup(a => a.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(list);

            var resp = await table.Object.ListByHackathonAsync(hackathonName, default);

            table.Verify(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default), Times.Once);
            table.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
        #endregion
    }
}
