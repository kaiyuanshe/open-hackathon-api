using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class AwardAssignmentTableTests
    {
        #region ListByHackathonAsync
        [Test]
        public async Task ListByHackathonAsync()
        {
            string hackathonName = "hack";
            var list = new List<AwardAssignmentEntity> { new AwardAssignmentEntity() };

            var awardTable = new Mock<AwardAssignmentTable>() { };
            awardTable.Setup(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(list);

            var resp = await awardTable.Object.ListByHackathonAsync(hackathonName, default);

            awardTable.Verify(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default), Times.Once);
            awardTable.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
        #endregion

        #region ListByAwardAsync
        [Test]
        public async Task ListByAwardAsync()
        {
            string hackathonName = "hack";
            string awardId = "aid";
            var list = new List<AwardAssignmentEntity> { new AwardAssignmentEntity() };

            var awardTable = new Mock<AwardAssignmentTable>() { };
            awardTable.Setup(t => t.QueryEntitiesAsync("(PartitionKey eq 'hack') and (AwardId eq 'aid')", null, default)).ReturnsAsync(list);
           
            var resp = await awardTable.Object.ListByAwardAsync(hackathonName, awardId, default);

            awardTable.Verify(t => t.QueryEntitiesAsync("(PartitionKey eq 'hack') and (AwardId eq 'aid')", null, default), Times.Once);
            awardTable.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
        #endregion

        #region ListByAssigneeAsync
        [Test]
        public async Task ListByAssigneeAsync()
        {
            string hackathonName = "hack";
            string assigneeId = "assid";
            var list = new List<AwardAssignmentEntity> { new AwardAssignmentEntity() };

            var awardTable = new Mock<AwardAssignmentTable>() { };
            awardTable.Setup(t => t.QueryEntitiesAsync("(PartitionKey eq 'hack') and (AssigneeId eq 'assid')", null, default)).ReturnsAsync(list);
          
            var resp = await awardTable.Object.ListByAssigneeAsync(hackathonName, assigneeId, default);

            awardTable.Verify(t => t.QueryEntitiesAsync("(PartitionKey eq 'hack') and (AssigneeId eq 'assid')", null, default), Times.Once);
            awardTable.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
        #endregion
    }
}
