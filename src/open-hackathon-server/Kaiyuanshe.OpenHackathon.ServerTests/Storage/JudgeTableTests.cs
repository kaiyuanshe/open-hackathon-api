using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class JudgeTableTests
    {
        #region ListByHackathonAsync
        [Test]
        public async Task ListByHackathonAsync()
        {
            string hackathonName = "hack";
            var list = new List<JudgeEntity> { new JudgeEntity() };

            var logger = new Mock<ILogger<JudgeTable>>();
            var judgeTable = new Mock<JudgeTable> (logger.Object){ };
            judgeTable.Setup(a => a.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(list);

            var resp = await judgeTable.Object.ListByHackathonAsync(hackathonName, default);

            judgeTable.Verify(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default), Times.Once);
            judgeTable.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
        #endregion
    }
}
