using Kaiyuanshe.OpenHackathon.Server.CronJobs.Jobs.Reports;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using NUnit.Framework;
using System;
using System.Collections;

namespace Kaiyuanshe.OpenHackathon.ServerTests.CronJobs.Reports
{
    internal class EnrollmentReportsJobTests
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
                new HackathonEntity { EventEndedAt = DateTime.UtcNow.AddMonths(-1).AddMinutes(-1) },
                false);

            // has valid EventEndedAt
            yield return new TestCaseData(
                new HackathonEntity { EventEndedAt = DateTime.UtcNow.AddMonths(-1).AddMinutes(1) },
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

    }
}
