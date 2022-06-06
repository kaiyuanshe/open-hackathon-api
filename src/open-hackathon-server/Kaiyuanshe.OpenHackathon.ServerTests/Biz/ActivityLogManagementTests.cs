using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
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
        #region LogHackathonActivity
        [Test]
        public async Task LogHackathonActivity()
        {
            var args = new { userName = "un" };

            var moqs = new Moqs();
            moqs.ActivityLogTable.Setup(t => t.InsertAsync(It.Is<ActivityLogEntity>(l =>
                l.ActivityId.Length == 28
                && l.PartitionKey == "hack"
                && l.Category == ActivityLogCategory.Hackathon
                && l.OperatorId == "op"
                && l.HackathonName == "hack"
                && l.CreatedAt > DateTime.UtcNow.AddMinutes(-1)
                && l.ActivityLogType == ActivityLogType.createHackathon.ToString()
                && l.Messages.Count() == 2
                && l.Messages["zh-CN"] != null // CultureInfo unable to set/get on Github actions 
                && l.Messages["en-US"] == "Created by user: un."), default));

            var management = new ActivityLogManagement()
            {
                StorageContext = moqs.StorageContext.Object,
            };
            await management.LogHackathonActivity("hack", "op", ActivityLogType.createHackathon, args, default);

            moqs.VerifyAll();
        }
        #endregion

        #region LogTeamActivity
        [Test]
        public async Task LogTeamActivity()
        {
            var moqs = new Moqs();
            moqs.ActivityLogTable.Setup(t => t.InsertAsync(It.Is<ActivityLogEntity>(l =>
                l.ActivityId.Length == 28
                && l.PartitionKey == "tid"
                && l.Category == ActivityLogCategory.Team
                && l.OperatorId == "op"
                && l.HackathonName == "hack"
                && l.CreatedAt > DateTime.UtcNow.AddMinutes(-1)
                && l.ActivityLogType == ActivityLogType.createTeam.ToString()
                && l.Messages.Count() == 2
                && l.Messages["zh-CN"] == null
                && l.Messages["en-US"] == null), default));

            var management = new ActivityLogManagement()
            {
                StorageContext = moqs.StorageContext.Object,
            };
            await management.LogTeamActivity("hack", "tid", "op", ActivityLogType.createTeam, null, default);

            moqs.VerifyAll();
        }
        #endregion

        #region LogUserActivity
        [Test]
        public async Task LogUserActivity()
        {
            var args = new { hackathonName = "hack" };

            var moqs = new Moqs();
            moqs.ActivityLogTable.Setup(t => t.InsertAsync(It.Is<ActivityLogEntity>(l =>
                l.ActivityId.Length == 28
                && l.PartitionKey == "uid"
                && l.Category == ActivityLogCategory.User
                && l.OperatorId == "op"
                && l.HackathonName == "foo"
                && l.CreatedAt > DateTime.UtcNow.AddMinutes(-1)
                && l.ActivityLogType == ActivityLogType.createHackathon.ToString()
                && l.Messages.Count() == 2
                && l.Messages["zh-CN"] != null // CultureInfo unable to set/get on Github actions 
                && l.Messages["en-US"] == "[hack]Created a new hackathon: hack"
                ), default));

            var management = new ActivityLogManagement()
            {
                StorageContext = moqs.StorageContext.Object,
            };
            await management.LogUserActivity("uid", "foo", "op", ActivityLogType.createHackathon, args, default);

            moqs.VerifyAll();
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

            // catetory hackathon, not ready
            yield return new TestCaseData(
                new ActivityLogQueryOptions { Category = ActivityLogCategory.Hackathon },
                null,
                null, // filter
                null,
                null,
                null
                );

            // catetory Team, not ready
            yield return new TestCaseData(
                new ActivityLogQueryOptions { Category = ActivityLogCategory.Team },
                null,
                null, // filter
                null,
                null,
                null
                );

            // catetory User, without UserId
            yield return new TestCaseData(
                new ActivityLogQueryOptions { Category = ActivityLogCategory.User },
                null,
                null, // filter
                null,
                null,
                null
                );

            // Category=user, with top
            yield return new TestCaseData(
                new ActivityLogQueryOptions
                {
                    Category = ActivityLogCategory.User,
                    HackathonName = "hack",
                    TeamId = "tid",
                    UserId = "uid",
                    Pagination = new Pagination { top = 5 }
                },
                null,
                "(PartitionKey eq 'uid') and (Category eq 1)",
                null,
                5,
                new Pagination { top = 5 }
                );

            // Category=user, with top and token
            yield return new TestCaseData(
                new ActivityLogQueryOptions
                {
                    Category = ActivityLogCategory.User,
                    HackathonName = "hack",
                    TeamId = "tid",
                    UserId = "uid",
                    Pagination = new Pagination { top = 5, np = "np", nr = "nr" }
                },
                null,
                "(PartitionKey eq 'uid') and (Category eq 1)",
                "np nr",
                5,
                new Pagination { top = 5 }
                );

            // Category=user, with returned pagination
            yield return new TestCaseData(
                new ActivityLogQueryOptions
                {
                    Category = ActivityLogCategory.User,
                    HackathonName = "hack",
                    TeamId = "tid",
                    UserId = "uid",
                },
                "np2 nr2",
                "(PartitionKey eq 'uid') and (Category eq 1)",
                null,
                100,
                new Pagination { top = 100, np = "np2", nr = "nr2" }
                );

            // Category=user, with all other options
            yield return new TestCaseData(
                new ActivityLogQueryOptions
                {
                    Category = ActivityLogCategory.User,
                    HackathonName = "hack",
                    TeamId = "tid",
                    UserId = "uid",
                    Pagination = new Pagination { top = 10, np = "np", nr = "nr" }
                },
                "np2 nr2",
                "(PartitionKey eq 'uid') and (Category eq 1)",
                "np nr",
                10,
                new Pagination { top = 10, np = "np2", nr = "nr2" }
                );

            // Category=hackathon, with all other options
            yield return new TestCaseData(
                new ActivityLogQueryOptions
                {
                    Category = ActivityLogCategory.Hackathon,
                    HackathonName = "hack",
                    TeamId = "tid",
                    UserId = "uid",
                    Pagination = new Pagination { top = 10, np = "np", nr = "nr" }
                },
                "np2 nr2",
                "(PartitionKey eq 'hack') and (Category eq 0)",
                "np nr",
                10,
                new Pagination { top = 10, np = "np2", nr = "nr2" }
                );
        }

        [Test, TestCaseSource(nameof(ListActivityLogsTestData))]
        public async Task ListActivityLogs(ActivityLogQueryOptions options, string returnedToken,
            string expectedFilter, string expectedToken, int? expectedTop, Pagination expectedNext)
        {
            var logs = new List<ActivityLogEntity>()
            {
                new ActivityLogEntity{ },
                new ActivityLogEntity{ },
            };

            var page = MockHelper.CreatePage(logs, returnedToken);
            var moqs = new Moqs();
            if (!string.IsNullOrEmpty(expectedFilter))
            {
                moqs.ActivityLogTable.Setup(a => a.ExecuteQuerySegmentedAsync(expectedFilter, expectedToken, expectedTop, null, default))
                    .ReturnsAsync(page);
            }

            var activityLogManagement = new ActivityLogManagement()
            {
                StorageContext = moqs.StorageContext.Object,
            };
            var result = await activityLogManagement.ListActivityLogs(options, default);

            moqs.VerifyAll();
            if (expectedTop.HasValue)
            {
                Assert.AreEqual(2, result.Count());
            }
            AssertHelper.AssertEqual(expectedNext, options.NextPage);
        }
        #endregion
    }
}
