using DotLiquid.Util;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class ActivityLogManagementTests
    {
        #region LogActivity
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
            table.Setup(t => t.InsertAsync(It.Is<ActivityLogEntity>(l => l.ActivityId.Length == 28
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
                    l.ActivityId.Length == 28
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
                    l.ActivityId.Length == 28
                    && l.PartitionKey != null
                    && l.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                    && l.Category == ActivityLogCategory.Hackathon
                    && l.UserId == entity.UserId
                    && l.HackathonName == entity.HackathonName
                    && l.ActivityLogType == entity.ActivityLogType), default), Times.Once);
            }
            table.VerifyNoOtherCalls();
        }

        [TestCase(null)]
        [TestCase("msg")]
        public async Task LogActivity_Message(string defaultMsg)
        {
            var entity = new ActivityLogEntity
            {
                UserId = "uid",
                Message = defaultMsg,
                ActivityLogType = ActivityLogType.createHackathon.ToString(),
                Args = new string[] { "u", "h" }
            };

            var logger = new Mock<ILogger<ActivityLogTable>>();
            var storageContext = new MockStorageContext();
            var expectedMsg = defaultMsg ?? "u created a new hackathon: h.";
            storageContext.ActivityLogTable.Setup(t => t.InsertAsync(It.Is<ActivityLogEntity>(l => l.ActivityId.Length == 28
                && l.PartitionKey != null
                && l.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                && l.Category == ActivityLogCategory.User
                && l.UserId == "uid"
                && l.Message == expectedMsg
                && l.ActivityLogType == entity.ActivityLogType), default)).Returns(Task.CompletedTask);

            var management = new ActivityLogManagement(null)
            {
                StorageContext = storageContext.Object,
            };
            await management.LogActivity(entity, default);

            storageContext.VerifyAll();
        }

        #endregion

        #region ListActivityLogs
        private static IEnumerable ListActivityLogsTestData()
        {
            // options
            // returned continuationToken
            // expected filter
            // expected continuationToken
            // expected top
            // expected next Pagination

            // no filter, default top
            yield return new TestCaseData(
                new ActivityLogQueryOptions { },
                null,
                "Category eq 1",
                null,
                100,
                new Pagination { top = 100 }
                );

            // no filter, top
            yield return new TestCaseData(
                new ActivityLogQueryOptions { Pagination = new Pagination { top = 5 } },
                null,
                "Category eq 1",
                null,
                5,
                new Pagination { top = 5 }
                );

            // no filter, top & token
            yield return new TestCaseData(
                new ActivityLogQueryOptions { Pagination = new Pagination { top = 5, np = "np", nr = "nr" } },
                null,
                "Category eq 1",
                "np nr",
                5,
                new Pagination { top = 5 }
                );

            // no filter, with returned pagination
            yield return new TestCaseData(
               new ActivityLogQueryOptions { },
               "np2 nr2",
               "Category eq 1",
               null,
               100,
               new Pagination { top = 100, np = "np2", nr = "nr2" }
               );

            // with HackathonName
            yield return new TestCaseData(
                new ActivityLogQueryOptions { HackathonName = "hack" },
                null,
                "(PartitionKey eq 'hack') and (Category eq 0)",
                null,
                100,
                new Pagination { top = 100 }
                );

            // with UserId
            yield return new TestCaseData(
                new ActivityLogQueryOptions { UserId = "uid" },
                null,
                "(PartitionKey eq 'uid') and (Category eq 1)",
                null,
                100,
                new Pagination { top = 100 }
                );

            // with HackathonName and UserId
            yield return new TestCaseData(
                new ActivityLogQueryOptions { HackathonName = "hack", UserId = "uid" },
                null,
                "(PartitionKey eq 'hack') and (Category eq 0) and (UserId eq 'uid')",
                null,
                100,
                new Pagination { top = 100 }
                );

            // all
            yield return new TestCaseData(
                new ActivityLogQueryOptions
                {
                    HackathonName = "hack",
                    UserId = "uid",
                    Pagination = new Pagination { top = 10, np = "np", nr = "nr" }
                },
                "np2 nr2",
                "(PartitionKey eq 'hack') and (Category eq 0) and (UserId eq 'uid')",
                "np nr",
                10,
                new Pagination { top = 10, np = "np2", nr = "nr2" }
                );
        }

        [Test, TestCaseSource(nameof(ListActivityLogsTestData))]
        public async Task ListActivityLogs(ActivityLogQueryOptions options, string returnedToken,
            string expectedFilter, string expectedToken, int expectedTop, Pagination expectedNext)
        {
            var logs = new List<ActivityLogEntity>()
            {
                new ActivityLogEntity{ },
                new ActivityLogEntity{ },
            };

            var page = MockHelper.CreatePage(logs, returnedToken);
            var storageContext = new MockStorageContext();
            storageContext.ActivityLogTable.Setup(a => a.ExecuteQuerySegmentedAsync(expectedFilter, expectedToken, expectedTop, null, default))
                .ReturnsAsync(page);

            var activityLogManagement = new ActivityLogManagement(null)
            {
                StorageContext = storageContext.Object,
            };
            var result = await activityLogManagement.ListActivityLogs(options, default);

            storageContext.VerifyAll();
            Assert.AreEqual(2, result.Count());
            AssertHelper.AssertEqual(expectedNext, options.NextPage);
        }
        #endregion
    }
}
