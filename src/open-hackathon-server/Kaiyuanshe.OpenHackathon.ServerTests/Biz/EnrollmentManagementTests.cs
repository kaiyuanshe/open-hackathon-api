using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Moq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    public class EnrollmentManagementTests
    {
        #region CreateEnrollmentAsync
        [TestCase(false, EnrollmentStatus.pendingApproval)]
        [TestCase(true, EnrollmentStatus.approved)]
        public async Task CreateEnrollmentAsync(bool autoApprove, EnrollmentStatus targetStatus)
        {
            HackathonEntity hackathon = new HackathonEntity
            {
                PartitionKey = "hack",
                AutoApprove = autoApprove,
                Enrollment = 5,
            };
            Enrollment request = new Enrollment
            {
                userId = "uid",
                extensions = new Extension[]
                {
                    new Extension { name = "n1", value = "v1" }
                }
            };

            var moqs = new Moqs();
            moqs.EnrollmentTable.Setup(p => p.InsertAsync(It.Is<EnrollmentEntity>(e => e.Status == targetStatus), default));
            if (autoApprove)
            {
                moqs.HackathonTable.Setup(h => h.MergeAsync(hackathon, default));
            }
            moqs.CacheProvider.Setup(c => c.Remove("Enrollment-hack"));


            var management = new EnrollmentManagement();
            moqs.SetupManagement(management);
            await management.CreateEnrollmentAsync(hackathon, request, default);

            moqs.VerifyAll();
            if (autoApprove)
            {
                moqs.HackathonTable.Verify(h => h.MergeAsync(It.Is<HackathonEntity>(h => h.Enrollment == 6), default), Times.Once);
            }
        }
        #endregion

        #region UpdateEnrollmentAsync
        [Test]
        public async Task UpdateEnrollmentAsync()
        {
            EnrollmentEntity existing = new EnrollmentEntity { PartitionKey = "hack" };
            Enrollment request = new Enrollment
            {
                extensions = new Extension[]
                {
                    new Extension { name = "n", value = "v" }
                }
            };

            var enrollmentTable = new Mock<IEnrollmentTable>();
            var storageContext = new Mock<IStorageContext>();
            var hackathonTable = new Mock<IHackathonTable>();
            storageContext.SetupGet(p => p.EnrollmentTable).Returns(enrollmentTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Enrollment-hack"));

            var enrollmentManagement = new EnrollmentManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await enrollmentManagement.UpdateEnrollmentAsync(existing, request, default);

            // verify
            Mock.VerifyAll(enrollmentTable, storageContext, cache);
            enrollmentTable.Verify(e => e.MergeAsync(It.Is<EnrollmentEntity>(en => en.Extensions.Count() == 1 && en.Extensions.First().name == "n" && en.Extensions.First().value == "v"), default), Times.Once);
            enrollmentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(1, result.Extensions.Count());
            Assert.AreEqual("n", result.Extensions.First().name);
            Assert.AreEqual("v", result.Extensions.First().value);
        }
        #endregion

        #region UpdateEnrollmentStatusAsync
        [TestCase(EnrollmentStatus.none, EnrollmentStatus.approved, 1)]
        [TestCase(EnrollmentStatus.pendingApproval, EnrollmentStatus.approved, 1)]
        [TestCase(EnrollmentStatus.approved, EnrollmentStatus.approved, 0)]
        [TestCase(EnrollmentStatus.rejected, EnrollmentStatus.approved, 1)]
        [TestCase(EnrollmentStatus.none, EnrollmentStatus.rejected, 0)]
        [TestCase(EnrollmentStatus.pendingApproval, EnrollmentStatus.rejected, 0)]
        [TestCase(EnrollmentStatus.approved, EnrollmentStatus.rejected, -1)]
        [TestCase(EnrollmentStatus.rejected, EnrollmentStatus.rejected, 0)]
        public async Task EnrollUpdateStatusAsyncTest_Updated(EnrollmentStatus currentStatus, EnrollmentStatus targetStatus, int expectedIncreasement)
        {
            // data
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack", Enrollment = 5 };
            EnrollmentEntity enrollment = new EnrollmentEntity
            {
                Status = currentStatus
            };
            CancellationToken cancellation = CancellationToken.None;

            // setup
            var storageContext = new Mock<IStorageContext>();
            var enrollmentTable = new Mock<IEnrollmentTable>();
            var cache = new Mock<ICacheProvider>();
            if (currentStatus != targetStatus)
            {
                storageContext.SetupGet(p => p.EnrollmentTable).Returns(enrollmentTable.Object);
                enrollmentTable.Setup(p => p.MergeAsync(It.IsAny<EnrollmentEntity>(), cancellation));
                cache.Setup(c => c.Remove("Enrollment-hack"));
            }
            var hackathonTable = new Mock<IHackathonTable>();
            if (expectedIncreasement != 0)
            {
                storageContext.SetupGet(p => p.HackathonTable).Returns(hackathonTable.Object);
                hackathonTable.Setup(h => h.MergeAsync(hackathon, cancellation));
            }

            // test
            var enrollmentManagement = new EnrollmentManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,

            };
            await enrollmentManagement.UpdateEnrollmentStatusAsync(hackathon, enrollment, targetStatus, cancellation);

            // verify
            Mock.VerifyAll(storageContext, enrollmentTable, hackathonTable, cache);
            if (currentStatus != targetStatus)
            {
                enrollmentTable.Verify(p => p.MergeAsync(It.Is<EnrollmentEntity>(p => p.Status == targetStatus), cancellation), Times.Once);
            }
            enrollmentTable.VerifyNoOtherCalls();
            if (expectedIncreasement != 0)
            {
                int expected = 5 + expectedIncreasement;
                hackathonTable.Verify(h => h.MergeAsync(It.Is<HackathonEntity>(h => h.Enrollment == expected), cancellation));
            }
            hackathonTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();
        }
        #endregion

        #region ListPaginatedEnrollmentsAsync
        [TestCase(5, 5)]
        [TestCase(-1, 100)]
        public async Task ListPaginatedEnrollmentsAsync_Options(int topInPara, int expectedTop)
        {
            string hackName = "foo";
            EnrollmentQueryOptions options = new EnrollmentQueryOptions
            {
                Pagination = new Pagination { np = "np", nr = "nr", top = topInPara },
                Status = EnrollmentStatus.approved
            };

            var entities = MockHelper.CreatePage(new List<EnrollmentEntity>
                {
                    new EnrollmentEntity{  PartitionKey="pk" }
                }, "np2 nr2");

            var moqs = new Moqs();
            moqs.EnrollmentTable.Setup(p => p.ExecuteQuerySegmentedAsync("(PartitionKey eq 'foo') and (Status eq 2)", "np nr", expectedTop, null, default))
                .ReturnsAsync(entities);

            var management = new EnrollmentManagement();
            moqs.SetupManagement(management);
            var page = await management.ListPaginatedEnrollmentsAsync(hackName, options, default);

            moqs.VerifyAll();
            Assert.AreEqual(1, page.Values.Count());
            Assert.AreEqual("pk", page.Values.First().HackathonName);
            var pagination = Pagination.FromContinuationToken(page.ContinuationToken);
            Assert.AreEqual("np2", pagination.np);
            Assert.AreEqual("nr2", pagination.nr);
        }
        #endregion

        #region GetEnrollmentAsync
        [TestCase(null, null)]
        [TestCase(null, "uid")]
        [TestCase("hack", null)]
        public async Task GetEnrollmentAsyncTest_NotFound(string hackathon, string userId)
        {
            var enrollmentManagement = new EnrollmentManagement();
            var enrollment = await enrollmentManagement.GetEnrollmentAsync(hackathon, userId);
            Assert.IsNull(enrollment);
        }

        [Test]
        public async Task GetEnrollmentAsyncTest_Succeeded()
        {
            EnrollmentEntity participant = new EnrollmentEntity { Status = EnrollmentStatus.rejected };

            var moqs = new Moqs();
            moqs.EnrollmentTable.Setup(p => p.RetrieveAsync("hack", "uid", default)).ReturnsAsync(participant);

            var management = new EnrollmentManagement();
            moqs.SetupManagement(management);
            var enrollment = await management.GetEnrollmentAsync("Hack", "uid", default);

            moqs.VerifyAll();
            Assert.IsNotNull(enrollment);
            Debug.Assert(enrollment != null);
            Assert.AreEqual(EnrollmentStatus.rejected, enrollment.Status);
        }
        #endregion

        #region IsUserEnrolledAsync
        [TestCase("anotheruser", EnrollmentStatus.approved, false)]
        [TestCase("uid", EnrollmentStatus.none, false)]
        [TestCase("uid", EnrollmentStatus.pendingApproval, false)]
        [TestCase("uid", EnrollmentStatus.rejected, false)]
        [TestCase("uid", EnrollmentStatus.approved, true)]
        public async Task IsUserEnrolledAsync_Small(string userId, EnrollmentStatus status, bool expectedResult)
        {
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack", MaxEnrollment = 1000 };
            IEnumerable<EnrollmentEntity> enrollments = new List<EnrollmentEntity>
            {
                new EnrollmentEntity { RowKey = "uid", Status = status }
            };

            // mock
            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<IEnumerable<EnrollmentEntity>>>(), default))
                .ReturnsAsync(enrollments);

            // test
            var management = new EnrollmentManagement();
            moqs.SetupManagement(management);
            var result = await management.IsUserEnrolledAsync(hackathon, userId, default);
            Assert.IsFalse(await management.IsUserEnrolledAsync(hackathon, "", default));

            // verify
            moqs.VerifyAll();
            Assert.AreEqual(expectedResult, result);
        }

        private static IEnumerable IsUserEnrolledAsync_Big_TestData()
        {
            // arg0: max enrollment
            // arg1: EnrollmentEntity
            // arg2: expected result

            // empty
            yield return new TestCaseData(
                1001,
                null,
                false);

            // unapproved
            yield return new TestCaseData(
                -1,
                new EnrollmentEntity { Status = EnrollmentStatus.none },
                false);
            yield return new TestCaseData(
                -1,
                new EnrollmentEntity { Status = EnrollmentStatus.pendingApproval },
                false);
            yield return new TestCaseData(
                -1,
                new EnrollmentEntity { Status = EnrollmentStatus.rejected },
                false);

            // approved
            yield return new TestCaseData(
                10000,
                new EnrollmentEntity { Status = EnrollmentStatus.approved },
                true);
        }

        [Test, TestCaseSource(nameof(IsUserEnrolledAsync_Big_TestData))]
        public async Task IsUserEnrolledAsync_Big(int maxEnrollment, EnrollmentEntity enrollment, bool expectedResult)
        {
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack", MaxEnrollment = maxEnrollment };
            string userId = "uid";
            CancellationToken cancellationToken = default;

            // mock
            var cache = new Mock<ICacheProvider>();
            var storageContext = new Mock<IStorageContext>();
            var enrollmentTable = new Mock<IEnrollmentTable>();
            enrollmentTable.Setup(p => p.RetrieveAsync("hack", userId, cancellationToken)).ReturnsAsync(enrollment);
            storageContext.SetupGet(p => p.EnrollmentTable).Returns(enrollmentTable.Object);

            // test
            var enrollmentManagement = new EnrollmentManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await enrollmentManagement.IsUserEnrolledAsync(hackathon, userId, cancellationToken);

            // verify
            Mock.VerifyAll(cache, storageContext, enrollmentTable);
            cache.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            enrollmentTable.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult, result);
        }
        #endregion
    }
}
