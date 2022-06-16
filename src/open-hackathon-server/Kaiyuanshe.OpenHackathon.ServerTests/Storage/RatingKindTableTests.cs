using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class RatingKindTableTests
    {
        #region ListRatingKindsAsync
        [Test]
        public async Task ListRatingKindsAsync()
        {
            string hackathonName = "hack";
            var list = new List<RatingKindEntity> { new RatingKindEntity() };

            var ratingKindTable = new Mock<RatingKindTable>() { };
            ratingKindTable.Setup(a => a.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default)).ReturnsAsync(list);

            var resp = await ratingKindTable.Object.ListRatingKindsAsync(hackathonName, default);

            ratingKindTable.Verify(t => t.QueryEntitiesAsync("PartitionKey eq 'hack'", null, default), Times.Once);
            ratingKindTable.VerifyNoOtherCalls();
            Assert.AreEqual(1, resp.Count());
        }
        #endregion
    }
}
