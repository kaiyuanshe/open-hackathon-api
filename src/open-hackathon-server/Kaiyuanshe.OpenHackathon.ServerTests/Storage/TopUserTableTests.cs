using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    public class TopUserTableTests
    {
        #region BatchUpdateTopUsers
        private static IEnumerable BatchUpdateTopUsersTestData()
        {
            // arg0: scores
            // arg1: expected entitis

            // by order
            yield return new TestCaseData(
                new Dictionary<string, int>
                {
                    ["a"] = 5,
                    ["b"] = 10,
                    ["c"] = 7,
                },
                new List<TopUserEntity>
                {
                    new TopUserEntity { PartitionKey = "0", RowKey = "0", Score = 10, UserId = "b" },
                    new TopUserEntity { PartitionKey = "1", RowKey = "1", Score = 7, UserId = "c" },
                    new TopUserEntity { PartitionKey = "2", RowKey = "2", Score = 5, UserId = "a" },
                });

            // top
            Dictionary<string, int> result = new Dictionary<string, int>();
            List<TopUserEntity> userEntities = new List<TopUserEntity>();
            int all = 110;
            for (int i = 0; i < all; i++)
            {
                result.Add(i.ToString(), i);
                if (i >= all - 100)
                {
                    userEntities.Add(new TopUserEntity
                    {
                        PartitionKey = (all - 1 - i).ToString(),
                        RowKey = (all - 1 - i).ToString(),
                        UserId = i.ToString(),
                        Score = i
                    });
                }
            }
            yield return new TestCaseData(result, userEntities);
        }

        [Test, TestCaseSource(nameof(BatchUpdateTopUsersTestData))]
        public async Task BatchUpdateTopUsers(Dictionary<string, int> scores, List<TopUserEntity> expectedEntities)
        {

            var topUserTable = new Mock<TopUserTable>();
            foreach (var expected in expectedEntities)
            {
                topUserTable.Setup(t => t.InsertOrReplaceAsync(It.Is<TopUserEntity>(e =>
                    e.PartitionKey == expected.PartitionKey
                    && e.RowKey == expected.RowKey
                    && e.Score == expected.Score
                    && e.UserId == expected.UserId), default));
            }

            await topUserTable.Object.BatchUpdateTopUsers(scores, default);

            topUserTable.VerifyAll();
            topUserTable.VerifyNoOtherCalls();
        }
        #endregion
    }
}
