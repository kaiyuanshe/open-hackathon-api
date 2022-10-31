using Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.CronJobs.Reports
{
    internal class EnrollmentReportsJobTests : ReportsJobTestBase
    {
        #region IsEligibleForReport
        private static IEnumerable IsEligibleForReportTestData()
        {
            // arg0: hackathon
            // arg1: expected result

            // readOnly
            yield return new TestCaseData(
                new HackathonEntity { ReadOnly = true },
                false);

            // has invalid EventEndedAt
            yield return new TestCaseData(
                new HackathonEntity { EventEndedAt = DateTime.UtcNow.AddDays(-30).AddMinutes(-1) },
                false);

            // has valid EventEndedAt
            yield return new TestCaseData(
                new HackathonEntity { EventEndedAt = DateTime.UtcNow.AddDays(-30).AddMinutes(1) },
                true);

            // no EventEndedAt, not online
            yield return new TestCaseData(
                new HackathonEntity { Status = HackathonStatus.pendingApproval },
                false);
            yield return new TestCaseData(
                new HackathonEntity { Status = HackathonStatus.offline },
                false);
            yield return new TestCaseData(
                new HackathonEntity { Status = HackathonStatus.planning },
                false);

            // no EventEndedAt, online, created >1 year
            yield return new TestCaseData(
                new HackathonEntity
                {
                    Status = HackathonStatus.online,
                    CreatedAt = DateTime.UtcNow.AddYears(-1).AddMinutes(-1)
                },
                false);

            // no EventEndedAt, online, created <1 year
            yield return new TestCaseData(
                new HackathonEntity
                {
                    Status = HackathonStatus.online,
                    CreatedAt = DateTime.UtcNow.AddYears(-1).AddMinutes(1)
                },
                true);
        }

        [Test, TestCaseSource(nameof(IsEligibleForReportTestData))]
        public void IsEligibleForReport(HackathonEntity hackathon, bool expectedResult)
        {
            var job = new EnrollmentReportsJob();
            Assert.AreEqual(expectedResult, job.IsEligibleForReport(hackathon));
        }
        #endregion

        #region GenerateReport
        [Test]
        public async Task GenerateReport()
        {
            var hackathons = new List<HackathonEntity>
            {
                new HackathonEntity{ PartitionKey = "h1", },
                new HackathonEntity{ PartitionKey = "h2", },
            };
            var enrollments1 = new List<EnrollmentEntity>
            {
                new EnrollmentEntity { RowKey = "u11" },
                new EnrollmentEntity { RowKey = "u12" },
            };
            var enrollments2 = new List<EnrollmentEntity>
            {
                new EnrollmentEntity { RowKey = "u21" },
                new EnrollmentEntity { RowKey = "u22" },
            };
            var userIds = new string[] { "u11", "u12", "u21", "u22" };

            var moqs = new Moqs();

            var job = new EnrollmentReportsJob();
            SetupReportJob(moqs, job, hackathons);
            moqs.EnrollmentTable.Setup(e => e.ExecuteQueryAsync("PartitionKey eq 'h1'", It.IsAny<Func<EnrollmentEntity, Task>>(), null, null, It.IsAny<CancellationToken>()))
                .Callback<string, Func<EnrollmentEntity, Task>, int?, IEnumerable<string>?, CancellationToken>(async (q, action, l, s, c) =>
                {
                    foreach (var e in enrollments1)
                    {
                        await action(e);
                    }
                });
            moqs.EnrollmentTable.Setup(e => e.ExecuteQueryAsync("PartitionKey eq 'h2'", It.IsAny<Func<EnrollmentEntity, Task>>(), null, null, It.IsAny<CancellationToken>()))
                .Callback<string, Func<EnrollmentEntity, Task>, int?, IEnumerable<string>?, CancellationToken>(async (q, action, l, s, c) =>
                {
                    foreach (var e in enrollments2)
                    {
                        await action(e);
                    }
                });
            foreach (var u in userIds)
            {
                moqs.UserManagement.Setup(t => t.GetUserByIdAsync(u, It.IsAny<CancellationToken>()));
            }

            await job.ExecuteNow(null);
            moqs.VerifyAll();
        }
        #endregion
    }
}
