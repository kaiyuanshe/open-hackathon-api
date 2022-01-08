using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Storage
{
    internal class UserActivityLogTableTests
    {
        [Test]
        public async Task LogActivity_Skip()
        {
            var logger = new Mock<ILogger<UserActivityLogTable>>();
            var table = new Mock<UserActivityLogTable>(logger.Object) { CallBase = true };
            table.Setup(t => t.InsertAsync(It.IsAny<UserActivityLogEntity>(), default));

            await table.Object.LogActivity(null);
            await table.Object.LogActivity(new UserActivityLogEntity { });
            await table.Object.LogActivity(new UserActivityLogEntity { PartitionKey = "uid" });
            await table.Object.LogActivity(new UserActivityLogEntity { ActivityLogType = "t" });

            table.Verify(t => t.InsertAsync(It.IsAny<UserActivityLogEntity>(), default), Times.Never);
        }

        [Test]
        public async Task LogActivity()
        {
            var logger = new Mock<ILogger<UserActivityLogTable>>();
            var table = new Mock<UserActivityLogTable>(logger.Object) { CallBase = true };
            table.Setup(t => t.InsertAsync(It.Is<UserActivityLogEntity>(l => l.ActivityId != null), default))
                .Returns(Task.CompletedTask);

            await table.Object.LogActivity(new UserActivityLogEntity
            {
                PartitionKey = "uid",
                ActivityLogType = "t"
            });

            table.Verify(t => t.InsertAsync(It.Is<UserActivityLogEntity>(
                l => l.ActivityId.Length == 36), default), Times.Once);
        }
    }
}
