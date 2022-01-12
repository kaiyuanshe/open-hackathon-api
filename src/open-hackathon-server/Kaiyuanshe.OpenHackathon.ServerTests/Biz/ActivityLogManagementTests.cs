using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class ActivityLogManagementTests
    {
        [Test]
        public async Task LogActivity_Skip()
        {
            var logger = new Mock<ILogger<ActivityLogTable>>();

            var table = new Mock<ActivityLogTable>(logger.Object);
            table.Setup(t => t.InsertAsync(It.IsAny<ActivityLogEntity>(), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.ActivityLogTable).Returns(table.Object);

            var management = new ActivityLogManagement(null)
            {
                StorageContext = storageContext.Object
            };
            // null entity
            await management.LogActivity(null);
            // no type
            await management.LogActivity(new ActivityLogEntity { });
            // neither userId nor hackathonName
            await management.LogActivity(new ActivityLogEntity { ActivityLogType = "t" });

            table.Verify(t => t.InsertAsync(It.IsAny<ActivityLogEntity>(), default), Times.Never);
        }

        private static IEnumerable LogActivityTestData()
        {
            // entity
            // expect log user activity
            // expect log hackathon activity
            yield return new TestCaseData(
                new ActivityLogEntity { ActivityLogType = "t", },
                false,
                false
                );

            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "t",
                    UserId = "uid"
                },
                true,
                false
                );

            yield return new TestCaseData(
                new ActivityLogEntity
                {
                    ActivityLogType = "t",
                    HackathonName = "hack"
                },
                false,
                true
                );

            yield return new TestCaseData(
               new ActivityLogEntity
               {
                   ActivityLogType = "t",
                   UserId = "uid",
                   HackathonName = "hack"
               },
               true,
               true
               );
        }

        [Test, TestCaseSource(nameof(LogActivityTestData))]
        public async Task LogActivity(ActivityLogEntity entity, bool expectUserLog, bool expectHackLog)
        {
            var logger = new Mock<ILogger<ActivityLogTable>>();
            var table = new Mock<ActivityLogTable>(logger.Object);
            table.Setup(t => t.InsertAsync(It.Is<ActivityLogEntity>(l => l.ActivityId.Length == 36
                && l.PartitionKey != null
                && l.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                && l.ActivityLogType == entity.ActivityLogType), default)).Returns(Task.CompletedTask);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.ActivityLogTable).Returns(table.Object);

            var management = new ActivityLogManagement(null)
            {
                StorageContext = storageContext.Object,
            };
            await management.LogActivity(entity, default);

            if (expectUserLog)
            {
                table.Verify(t => t.InsertAsync(It.Is<ActivityLogEntity>(l =>
                    l.ActivityId.Length == 36
                    && l.PartitionKey != null
                    && l.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                    && l.Category == ActivityLogCategory.User
                    && l.UserId == entity.UserId
                    && l.HackathonName == entity.HackathonName
                    && l.ActivityLogType == entity.ActivityLogType), default), Times.Once);
            }

            if (expectHackLog)
            {
                table.Verify(t => t.InsertAsync(It.Is<ActivityLogEntity>(l =>
                    l.ActivityId.Length == 36
                    && l.PartitionKey != null
                    && l.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                    && l.Category == ActivityLogCategory.Hackathon
                    && l.UserId == entity.UserId
                    && l.HackathonName == entity.HackathonName
                    && l.ActivityLogType == entity.ActivityLogType), default), Times.Once);
            }
            table.VerifyNoOtherCalls();
        }
    }
}
