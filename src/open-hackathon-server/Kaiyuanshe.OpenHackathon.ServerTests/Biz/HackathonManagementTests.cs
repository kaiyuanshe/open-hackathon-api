using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
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
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    [TestFixture]
    public class HackathonManagementTests
    {
        #region CanCreateHackathonAsync
        private static IEnumerable CanCreateHackathonAsyncTestData()
        {
            // arg0: entities
            // arg1: expectedResult

            // >3 in 1 day
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "a", new HackathonEntity{ CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddHours(-1) } },
                    { "b", new HackathonEntity{ CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddHours(-12) } },
                    { "c", new HackathonEntity{ CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddHours(-23) } },
                },
                false);

            // >10 in 1 month
            Dictionary<string, HackathonEntity> month = new Dictionary<string, HackathonEntity>();
            for (int i = 0; i < 10; i++)
            {
                month.Add(i.ToString(), new HackathonEntity { CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddDays(-5) });
            }
            yield return new TestCaseData(
                month,
                false);

            // pass
            Dictionary<string, HackathonEntity> pass = new Dictionary<string, HackathonEntity>();
            pass.Add("a", new HackathonEntity { CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddHours(-23) });
            pass.Add("b", new HackathonEntity { CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddHours(-1) });
            pass.Add("c", new HackathonEntity { CreatorId = "uid2", CreatedAt = DateTime.UtcNow.AddHours(-1) });
            for (int i = 0; i < 7; i++)
            {
                pass.Add(i.ToString(), new HackathonEntity { CreatorId = "uid", CreatedAt = DateTime.UtcNow.AddDays(-5) });
            }
            yield return new TestCaseData(
                pass,
                true);
        }

        [Test, TestCaseSource(nameof(CanCreateHackathonAsyncTestData))]
        public async Task CanCreateHackathonAsync(Dictionary<string, HackathonEntity> entities, bool expectedResult)
        {
            var user = MockHelper.ClaimsPrincipalWithUserId("uid");

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<Dictionary<string, HackathonEntity>>>(), default)).ReturnsAsync(entities);

            var hackathonManagement = new HackathonManagement()
            {
                Cache = cache.Object,
            };
            var result = await hackathonManagement.CanCreateHackathonAsync(user, default);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();
            Assert.AreEqual(expectedResult, result);
        }
        #endregion

        #region CreateHackathonAsyncTest
        [Test]
        public async Task CreateHackathonAsyncTest()
        {
            // input
            var request = new Hackathon
            {
                name = "test",
                location = "loc",
                eventStartedAt = DateTime.UtcNow,
                tags = new string[] { "a", "b", "c" },
                banners = new PictureInfo[] { new PictureInfo { name = "pn", description = "pd", uri = "pu" } },
                creatorId = "uid"
            };

            // mock
            var moqs = new Moqs();
            moqs.HackathonTable.Setup(p => p.InsertAsync(It.Is<HackathonEntity>(h =>
                h.Location == "loc"
                && h.CreatorId == "uid"
                && h.Tags.Length == 3
                && h.Banners.Length == 1
                && h.Banners[0].name == "pn"
                && h.Banners[0].description == "pd"
                && h.Banners[0].uri == "pu"
                && h.ReadOnly == false), default));
            moqs.CacheProvider.Setup(c => c.Remove(HackathonManagement.cacheKeyForAllHackathon));
            moqs.HackathonAdminManagement.Setup(a => a.CreateAdminAsync(It.Is<HackathonAdmin>(a =>
                a.hackathonName == "test"
                && a.userId == "uid"), default));

            // test
            var hackathonManagement = new HackathonManagement
            {
                HackathonAdminManagement = moqs.HackathonAdminManagement.Object
            };
            moqs.SetupManagement(hackathonManagement);
            var result = await hackathonManagement.CreateHackathonAsync(request, CancellationToken.None);

            // verify
            moqs.VerifyAll();
            Assert.AreEqual("loc", result.Location);
            Assert.AreEqual("test", result.DisplayName); // fallback to name
            Assert.AreEqual("test", result.PartitionKey);
            Assert.AreEqual("uid", result.CreatorId);
            Assert.AreEqual(string.Empty, result.RowKey);
            Assert.IsTrue(result.EventStartedAt.HasValue);
            Assert.IsFalse(result.EventEndedAt.HasValue);
            Assert.IsFalse(result.ReadOnly);
        }
        #endregion

        #region UpdateHackathonStatusAsync
        [TestCase(HackathonStatus.planning, HackathonStatus.planning)]
        [TestCase(HackathonStatus.pendingApproval, HackathonStatus.pendingApproval)]
        [TestCase(HackathonStatus.online, HackathonStatus.online)]
        [TestCase(HackathonStatus.offline, HackathonStatus.offline)]
        public async Task UpdateHackathonStatusAsync_Skip(HackathonStatus origin, HackathonStatus target)
        {
            HackathonEntity hackathonEntity = new HackathonEntity { Status = origin };

            var moqs = new Moqs();

            var hackathonManagement = new HackathonManagement();
            moqs.SetupManagement(hackathonManagement);
            moqs.HackathonTable.Setup(h => h.MergeAsync(hackathonEntity, default));

            var updated = await hackathonManagement.UpdateHackathonStatusAsync(hackathonEntity, target, default);
            Assert.AreEqual(target, updated.Status);
        }

        [TestCase(HackathonStatus.planning, HackathonStatus.pendingApproval)]
        [TestCase(HackathonStatus.planning, HackathonStatus.online)]
        [TestCase(HackathonStatus.planning, HackathonStatus.offline)]
        [TestCase(HackathonStatus.pendingApproval, HackathonStatus.online)]
        [TestCase(HackathonStatus.pendingApproval, HackathonStatus.offline)]
        [TestCase(HackathonStatus.online, HackathonStatus.offline)]
        public async Task UpdateHackathonStatusAsync_Succeeded(HackathonStatus origin, HackathonStatus target)
        {
            HackathonEntity hackathonEntity = new HackathonEntity { Status = origin };

            var moqs = new Moqs();
            moqs.HackathonTable.Setup(p => p.MergeAsync(It.Is<HackathonEntity>(h => h.Status == target), default));
            moqs.CacheProvider.Setup(c => c.Remove(HackathonManagement.cacheKeyForAllHackathon));

            // test
            var hackathonManagement = new HackathonManagement();
            moqs.SetupManagement(hackathonManagement);
            var updated = await hackathonManagement.UpdateHackathonStatusAsync(hackathonEntity, target, default);

            moqs.VerifyAll();
            Assert.AreEqual(target, updated.Status);
        }
        #endregion

        #region UpdateHackathonReadOnlyAsync
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateHackathonReadOnlyAsync(bool readOnly)
        {
            HackathonEntity entity = new HackathonEntity();

            var hackathonTable = new Mock<IHackathonTable>();
            hackathonTable.Setup(p => p.MergeAsync(It.Is<HackathonEntity>(h => h.ReadOnly == readOnly), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.HackathonTable).Returns(hackathonTable.Object);

            var hackathonManager = new HackathonManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await hackathonManager.UpdateHackathonReadOnlyAsync(entity, readOnly, default);

            Mock.VerifyAll(storageContext, hackathonTable);
            hackathonTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(readOnly, result.ReadOnly);
        }
        #endregion

        #region GetHackathonEntityByNameAsync
        [Test]
        public async Task GetHackathonEntityByNameAsync_Null()
        {
            string name = "test";
            HackathonEntity? entity = null;

            var moqs = new Moqs();
            moqs.HackathonTable.Setup(p => p.RetrieveAsync(name, string.Empty, default)).ReturnsAsync(entity);

            var hackathonManager = new HackathonManagement
            {
                StorageContext = moqs.StorageContext.Object,
            };
            var result = await hackathonManager.GetHackathonEntityByNameAsync(name, CancellationToken.None);

            moqs.VerifyAll();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetHackathonEntityByNameAsync_Offline()
        {
            string name = "test";
            var entity = new HackathonEntity
            {
                Status = HackathonStatus.offline
            };

            var moqs = new Moqs();
            moqs.HackathonTable.Setup(p => p.RetrieveAsync(name, string.Empty, default)).ReturnsAsync(entity);

            var hackathonManager = new HackathonManagement
            {
                StorageContext = moqs.StorageContext.Object,
            };
            var result = await hackathonManager.GetHackathonEntityByNameAsync(name, CancellationToken.None);

            moqs.VerifyAll();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetHackathonEntityByNameAsync_Success()
        {
            string name = "test";
            var entity = new HackathonEntity
            {
                Location = "loc"
            };

            var moqs = new Moqs();
            moqs.HackathonTable.Setup(p => p.RetrieveAsync(name, string.Empty, default)).ReturnsAsync(entity);

            var hackathonManager = new HackathonManagement
            {
                StorageContext = moqs.StorageContext.Object,
            };
            var result = await hackathonManager.GetHackathonEntityByNameAsync(name, CancellationToken.None);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("loc", result.Location);
        }
        #endregion

        #region ListPaginatedHackathonsAsync

        #region online, paging, ordering
        private static IEnumerable ListPaginatedHackathonsAsyncTestData_online()
        {
            var offline = new HackathonEntity { Status = HackathonStatus.offline };
            var planning = new HackathonEntity { Status = HackathonStatus.planning };
            var pendingApproval = new HackathonEntity { Status = HackathonStatus.pendingApproval };

            var h1 = new HackathonEntity
            {
                Status = HackathonStatus.online,
                PartitionKey = "h1 search",
                CreatedAt = DateTime.Now.AddDays(1),
                Timestamp = DateTimeOffset.Now.AddDays(1),
                Enrollment = 2,
            };
            var h2 = new HackathonEntity
            {
                Status = HackathonStatus.online,
                PartitionKey = "h2",
                DisplayName = "asearch DisplayName",
                CreatedAt = DateTime.Now.AddDays(2),
                Timestamp = DateTimeOffset.Now.AddDays(-1),
                Enrollment = 5,
            };
            var h3 = new HackathonEntity
            {
                Status = HackathonStatus.online,
                PartitionKey = "h3",
                Detail = "search Detail",
                CreatedAt = DateTime.Now.AddDays(3),
                Timestamp = DateTimeOffset.Now.AddDays(2),
                Enrollment = 1,
            };
            var h4 = new HackathonEntity
            {
                Status = HackathonStatus.online,
                PartitionKey = "searc h4",
                CreatedAt = DateTime.Now.AddDays(4),
                Timestamp = DateTimeOffset.Now.AddDays(-2),
                Enrollment = 4,
            };

            // arg0: all hackathons
            // arg1: options
            // arg2: expected result
            // arg3: expected next

            // empty
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "planning", planning},
                    { "pendingApproval", pendingApproval }
                },
                new HackathonQueryOptions { },
                new List<HackathonEntity>
                {
                },
                null
                );

            // search and default ordering
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions { Search = "search" },
                new List<HackathonEntity>
                {
                    h3, h2, h1
                },
                null
                );

            // ordering by updateBy
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions { OrderBy = HackathonOrderBy.updatedAt },
                new List<HackathonEntity>
                {
                    h3, h1, h2, h4
                },
                null
                );

            // ordering by hot
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions { OrderBy = HackathonOrderBy.hot },
                new List<HackathonEntity>
                {
                    h2, h4, h1, h3
                },
                null
                );

            // paging
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions
                {
                    Pagination = new Pagination { np = "3", nr = "3" },
                },
                new List<HackathonEntity>
                {
                    h1
                },
                null
                );

            // unknown paging para
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions
                {
                    Pagination = new Pagination { np = "not an int", nr = "not an int" },
                },
                new List<HackathonEntity>
                {
                    h4, h3, h2, h1
                },
                null
                );

            // top
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions
                {
                    Pagination = new Pagination { top = 3 },
                },
                new List<HackathonEntity>
                {
                    h4, h3, h2
                },
                new Pagination
                {
                    np = "3",
                    nr = "3"
                }
                );

            // top + paging
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "offline", offline },
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new HackathonQueryOptions
                {
                    Pagination = new Pagination
                    {
                        np = "1",
                        nr = "1",
                        top = 2,
                    },
                },
                new List<HackathonEntity>
                {
                    h3, h2
                },
                new Pagination
                {
                    np = "3",
                    nr = "3"
                }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedHackathonsAsyncTestData_online))]
        public async Task ListPaginatedHackathonsAsync_Online(
            Dictionary<string, HackathonEntity> allHackathons,
            HackathonQueryOptions options,
            List<HackathonEntity> expectedResult,
            Pagination expectedNext)
        {
            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<Dictionary<string, HackathonEntity>>>(), default))
                .ReturnsAsync(allHackathons);

            var hackathonManagement = new HackathonManagement();
            moqs.SetupManagement(hackathonManagement);
            var result = await hackathonManagement.ListPaginatedHackathonsAsync(options, default);

            moqs.VerifyAll();
            if (expectedNext == null)
            {
                Assert.IsNull(options.NextPage);
            }
            else
            {
                Debug.Assert(options.NextPage != null);
                Assert.AreEqual(options.NextPage.np, expectedNext.np);
                Assert.AreEqual(options.NextPage.nr, expectedNext.nr);
            }
            Assert.AreEqual(result.Count(), expectedResult.Count());
            for (int i = 0; i < result.Count(); i++)
            {
                Assert.AreEqual(result.ElementAt(i).Name, expectedResult[i].Name);
                Assert.AreEqual(result.ElementAt(i).DisplayName, expectedResult[i].DisplayName);
                Assert.AreEqual(result.ElementAt(i).Detail, expectedResult[i].Detail);
                Assert.AreEqual(result.ElementAt(i).CreatedAt, expectedResult[i].CreatedAt);
                Assert.AreEqual(result.ElementAt(i).Timestamp, expectedResult[i].Timestamp);
                Assert.AreEqual(result.ElementAt(i).Status, expectedResult[i].Status);
            }
        }

        #endregion

        #region admin
        private static IEnumerable ListPaginatedHackathonsAsyncTestData_admin()
        {
            var h1 = new HackathonEntity
            {
                PartitionKey = "h1",
                CreatedAt = DateTime.Now.AddDays(1),
            };
            var h2 = new HackathonEntity
            {
                PartitionKey = "h2",
                CreatedAt = DateTime.Now.AddDays(2),
            };
            var h3 = new HackathonEntity
            {
                PartitionKey = "h3",
                CreatedAt = DateTime.Now.AddDays(3),
            };
            var h4 = new HackathonEntity
            {
                PartitionKey = "h4",
                CreatedAt = DateTime.Now.AddDays(4),
            };

            var a0 = new HackathonAdminEntity { RowKey = "uid" };
            var a1 = new HackathonAdminEntity { RowKey = "a1" };
            var a2 = new HackathonAdminEntity { RowKey = "a2" };

            // arg0: userId
            // arg1: isPlatformAdmin
            // arg2: all hackathons
            // arg3: admins
            // arg4: expected result

            // empty user
            yield return new TestCaseData(
                null,
                false,
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                null,
                new List<HackathonEntity>()
                );

            // Platform Admin
            yield return new TestCaseData(
                "uid",
                true,
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                null,
                new List<HackathonEntity>
                {
                    h4, h3, h2, h1
                }
                );

            // normal user
            yield return new TestCaseData(
                "uid",
                false,
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 },
                },
                new Dictionary<string, List<HackathonAdminEntity>>
                {
                    { "h1", new List<HackathonAdminEntity> { } },
                    { "h2", new List<HackathonAdminEntity> { a0 } },
                    { "h3", new List<HackathonAdminEntity> { a1, a2 } },
                    { "h4", new List<HackathonAdminEntity> { a0, a1 } },
                },
                new List<HackathonEntity>
                {
                    h4, h2
                }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedHackathonsAsyncTestData_admin))]
        public async Task ListPaginatedHackathonsAsync_admin(
            string userId,
            bool isPlatformAdmin,
            Dictionary<string, HackathonEntity> allHackathons,
            Dictionary<string, List<HackathonAdminEntity>> admins,
            List<HackathonEntity> expectedResult)
        {
            var options = new HackathonQueryOptions
            {
                OrderBy = HackathonOrderBy.createdAt,
                ListType = HackathonListType.admin,
                UserId = userId,
                IsPlatformAdmin = isPlatformAdmin,
            };

            var cache = new Mock<ICacheProvider>();
            if (!string.IsNullOrEmpty(userId))
            {
                cache.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<Dictionary<string, HackathonEntity>>>(), default))
                    .ReturnsAsync(allHackathons);
            }
            var hackathonAdminManagement = new Mock<IHackathonAdminManagement>();
            if (admins != null)
            {
                foreach (var item in admins)
                {
                    hackathonAdminManagement.Setup(p => p.ListHackathonAdminAsync(item.Key, default))
                        .ReturnsAsync(item.Value);
                }
            }

            var hackathonManagement = new HackathonManagement()
            {
                Cache = cache.Object,
                HackathonAdminManagement = hackathonAdminManagement.Object,
            };
            var result = await hackathonManagement.ListPaginatedHackathonsAsync(options, default);

            Mock.VerifyAll(cache, hackathonAdminManagement);
            cache.VerifyNoOtherCalls();
            hackathonAdminManagement.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < result.Count(); i++)
            {
                Assert.AreEqual(expectedResult[i].Name, result.ElementAt(i).Name);
                Assert.AreEqual(expectedResult[i].CreatedAt, result.ElementAt(i).CreatedAt);
            }
        }
        #endregion

        #region enrolled
        private static IEnumerable ListPaginatedHackathonsAsyncTestData_enrolled()
        {
            var h1 = new HackathonEntity
            {
                PartitionKey = "h1",
                CreatedAt = DateTime.Now.AddDays(1),
            };
            var h2 = new HackathonEntity
            {
                PartitionKey = "h2",
                CreatedAt = DateTime.Now.AddDays(2),
            };
            var h3 = new HackathonEntity
            {
                PartitionKey = "h3",
                CreatedAt = DateTime.Now.AddDays(3),
            };
            var h4 = new HackathonEntity
            {
                PartitionKey = "h4",
                CreatedAt = DateTime.Now.AddDays(4),
            };
            var h5 = new HackathonEntity
            {
                PartitionKey = "h5",
                CreatedAt = DateTime.Now.AddDays(4),
                Status = HackathonStatus.offline,
            };

            // arg0: userId
            // arg1: all hackathons
            // arg2: enrolled
            // arg3: expected result

            // empty userId
            yield return new TestCaseData(
                null,
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                null,
                new List<HackathonEntity>()
                );

            // empty userId
            yield return new TestCaseData(
                "",
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                null,
                new List<HackathonEntity>()
                );

            // normal user
            yield return new TestCaseData(
                "uid",
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 },
                    { "h5", h5 }
                },
                new Dictionary<string, bool>
                {
                    { "h1", true },
                    { "h2", false },
                    { "h3", true },
                    { "h4", false },
                },
                new List<HackathonEntity>
                {
                    h3, h1
                });
        }

        [Test, TestCaseSource(nameof(ListPaginatedHackathonsAsyncTestData_enrolled))]
        public async Task ListPaginatedHackathonsAsync_enrolled(
            string userId,
            Dictionary<string, HackathonEntity> allHackathons,
            Dictionary<string, bool> enrolled,
            List<HackathonEntity> expectedResult)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            var options = new HackathonQueryOptions
            {
                OrderBy = HackathonOrderBy.createdAt,
                ListType = HackathonListType.enrolled,
                UserId = userId,
            };

            var cache = new Mock<ICacheProvider>();
            var enrollmentManagement = new Mock<IEnrollmentManagement>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                cache.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<Dictionary<string, HackathonEntity>>>(), cancellationToken))
                    .ReturnsAsync(allHackathons);

                foreach (var h in allHackathons.Values)
                {
                    if (enrolled.ContainsKey(h.Name))
                    {
                        enrollmentManagement.Setup(e => e.IsUserEnrolledAsync(h, userId, cancellationToken)).ReturnsAsync(enrolled[h.Name]);
                    }
                }
            }

            var hackathonManagement = new HackathonManagement()
            {
                Cache = cache.Object,
                EnrollmentManagement = enrollmentManagement.Object,
            };
            var result = await hackathonManagement.ListPaginatedHackathonsAsync(options, cancellationToken);

            Mock.VerifyAll(cache, enrollmentManagement);
            cache.VerifyNoOtherCalls();
            enrollmentManagement.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < result.Count(); i++)
            {
                Assert.AreEqual(expectedResult[i].Name, result.ElementAt(i).Name);
                Assert.AreEqual(expectedResult[i].CreatedAt, result.ElementAt(i).CreatedAt);
            }
        }
        #endregion

        #region fresh
        private static IEnumerable ListPaginatedHackathonsAsyncTestData_fresh()
        {
            //  EventStartedAt = null
            var h1 = new HackathonEntity
            {
                PartitionKey = "h1",
                CreatedAt = DateTime.Now.AddDays(1),
            };
            // status != online
            var h2 = new HackathonEntity
            {
                PartitionKey = "h2",
                CreatedAt = DateTime.Now.AddDays(2),
                EventStartedAt = DateTime.Now.AddDays(2),
            };
            // already started
            var h3 = new HackathonEntity
            {
                PartitionKey = "h3",
                CreatedAt = DateTime.Now.AddDays(3),
                Status = HackathonStatus.online,
                EventStartedAt = DateTime.Now.AddDays(-3),
            };
            // qualified
            var h4 = new HackathonEntity
            {
                PartitionKey = "h4",
                CreatedAt = DateTime.Now.AddDays(4),
                Status = HackathonStatus.online,
                EventStartedAt = DateTime.Now.AddDays(1),
            };

            // arg0: user
            // arg1: all hackathons
            // arg2: enrolled
            // arg3: expected result

            // normal user
            yield return new TestCaseData(
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                    { "h3", h3 },
                    { "h4", h4 }
                },
                new List<HackathonEntity>
                {
                    h4
                });
        }

        [Test, TestCaseSource(nameof(ListPaginatedHackathonsAsyncTestData_fresh))]
        public async Task ListPaginatedHackathonsAsync_fresh(
            Dictionary<string, HackathonEntity> allHackathons,
            List<HackathonEntity> expectedResult)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            var options = new HackathonQueryOptions
            {
                OrderBy = HackathonOrderBy.createdAt,
                ListType = HackathonListType.fresh,
            };

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<Dictionary<string, HackathonEntity>>>(), cancellationToken))
                .ReturnsAsync(allHackathons);

            var hackathonManagement = new HackathonManagement()
            {
                Cache = cache.Object,
            };
            var result = await hackathonManagement.ListPaginatedHackathonsAsync(options, cancellationToken);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < result.Count(); i++)
            {
                Assert.AreEqual(expectedResult[i].Name, result.ElementAt(i).Name);
                Assert.AreEqual(expectedResult[i].CreatedAt, result.ElementAt(i).CreatedAt);
            }
        }
        #endregion

        #region created
        private static IEnumerable ListPaginatedHackathonsAsyncTestData_created()
        {
            var h1 = new HackathonEntity
            {
                PartitionKey = "h1",
                CreatorId = "u1"
            };
            var h2 = new HackathonEntity
            {
                PartitionKey = "h2",
                CreatorId = "u2"
            };

            // arg0: userId
            // arg1: all hackathons
            // arg2: expected result

            // empty user
            yield return new TestCaseData(
                null,
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                },
                new List<HackathonEntity>
                {
                });

            // normal user
            yield return new TestCaseData(
                "u1",
                new Dictionary<string, HackathonEntity>
                {
                    { "h1", h1 },
                    { "h2", h2 },
                },
                new List<HackathonEntity>
                {
                    h1
                });
        }

        [Test, TestCaseSource(nameof(ListPaginatedHackathonsAsyncTestData_created))]
        public async Task ListPaginatedHackathonsAsync_created(
            string userId,
            Dictionary<string, HackathonEntity> allHackathons,
            List<HackathonEntity> expectedResult)
        {
            var options = new HackathonQueryOptions
            {
                UserId = userId,
                ListType = HackathonListType.created,
            };

            var cache = new Mock<ICacheProvider>();
            if (!string.IsNullOrEmpty(userId))
            {
                cache.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<Dictionary<string, HackathonEntity>>>(), default))
                    .ReturnsAsync(allHackathons);
            }

            var hackathonManagement = new HackathonManagement()
            {
                Cache = cache.Object,
            };
            var result = await hackathonManagement.ListPaginatedHackathonsAsync(options, default);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < result.Count(); i++)
            {
                Assert.AreEqual(expectedResult[i].Name, result.ElementAt(i).Name);
            }
        }
        #endregion

        #endregion

        #region ListAllHackathonsAsync
        [Test]
        public async Task ListAllHackathonsAsync()
        {
            Dictionary<string, HackathonEntity> data = new Dictionary<string, HackathonEntity>
            {
                { "test", new HackathonEntity{ } }
            };

            var moqs = new Moqs();
            moqs.HackathonTable.Setup(p => p.ListAllHackathonsAsync(default)).ReturnsAsync(data);

            var hackathonManagement = new HackathonManagement();
            moqs.SetupManagement(hackathonManagement);
            hackathonManagement.Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            var result = await hackathonManagement.ListAllHackathonsAsync(default);

            moqs.VerifyAll();
            Assert.AreEqual(1, result.Count);
        }
        #endregion

        #region UpdateHackathonAsyncTest
        [Test]
        public async Task UpdateHackathonAsyncTest()
        {
            string name = "test";
            var entity = new HackathonEntity
            {
                Location = "loc"
            };
            var request = new Hackathon
            {
                name = name,
            };

            var hackathonTable = new Mock<IHackathonTable>();
            hackathonTable.Setup(p => p.RetrieveAndMergeAsync(name, string.Empty, It.IsAny<Action<HackathonEntity>>(), default));
            hackathonTable.Setup(p => p.RetrieveAsync(name, string.Empty, CancellationToken.None))
                .ReturnsAsync(entity);

            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.HackathonTable).Returns(hackathonTable.Object);

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove(HackathonManagement.cacheKeyForAllHackathon));

            // test
            var hackathonManager = new HackathonManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await hackathonManager.UpdateHackathonAsync(request, CancellationToken.None);

            Mock.VerifyAll(hackathonTable, storageContext, cache);
            hackathonTable.Verify(p => p.RetrieveAndMergeAsync(name, string.Empty, It.IsAny<Action<HackathonEntity>>(), CancellationToken.None), Times.Once);
            hackathonTable.Verify(p => p.RetrieveAsync(name, string.Empty, CancellationToken.None), Times.Once);
            hackathonTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            storageContext.VerifyGet(p => p.HackathonTable, Times.Exactly(2));
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual("loc", result.Location);
        }
        #endregion

        #region ListHackathonRolesAsync
        [Test]
        public async Task ListHackathonRolesAsync()
        {
            var hackathons = new List<HackathonEntity>
            {
                new HackathonEntity{ PartitionKey = "hack1", },
                new HackathonEntity{ PartitionKey = "hack2", },
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(AuthConstant.ClaimType.UserId, "uid")
            }));
            var admins1 = new List<HackathonAdminEntity>
            {
                new HackathonAdminEntity{ RowKey = "other" }
            };
            var admins2 = new List<HackathonAdminEntity>
            {
                new HackathonAdminEntity{ RowKey = "uid" }
            };

            var moqs = new Moqs();
            moqs.HackathonAdminManagement.Setup(a => a.ListHackathonAdminAsync("hack1", default)).ReturnsAsync(admins1);
            moqs.HackathonAdminManagement.Setup(a => a.ListHackathonAdminAsync("hack2", default)).ReturnsAsync(admins2);

            moqs.EnrollmentManagement.Setup(e => e.IsUserEnrolledAsync(hackathons.ElementAt(0), "uid", default)).ReturnsAsync(false);
            moqs.EnrollmentManagement.Setup(e => e.IsUserEnrolledAsync(hackathons.ElementAt(1), "uid", default)).ReturnsAsync(true);

            moqs.JudgeManagement.Setup(j => j.IsJudgeAsync("hack1", "uid", default)).ReturnsAsync(false);
            moqs.JudgeManagement.Setup(j => j.IsJudgeAsync("hack2", "uid", default)).ReturnsAsync(true);

            var hackathonManagement = new HackathonManagement()
            {
                HackathonAdminManagement = moqs.HackathonAdminManagement.Object,
                EnrollmentManagement = moqs.EnrollmentManagement.Object,
                JudgeManagement = moqs.JudgeManagement.Object,
            };

            var result = await hackathonManagement.ListHackathonRolesAsync(hackathons, user, default);

            moqs.VerifyAll();
            var role1 = result.ElementAt(0).Item2;
            Debug.Assert(role1 != null);
            Assert.IsFalse(role1.isAdmin);
            Assert.IsFalse(role1.isEnrolled);
            Assert.IsFalse(role1.isJudge);

            var role2 = result.ElementAt(1).Item2;
            Debug.Assert(role2 != null);
            Assert.IsTrue(role2.isAdmin);
            Assert.IsTrue(role2.isEnrolled);
            Assert.IsTrue(role2.isJudge);
        }
        #endregion

        #region GetHackathonRolesAsync
        [Test]
        public async Task GetHackathonRolesAsyncTest_NoUserIdClaim()
        {
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack" };
            ClaimsPrincipal user = new ClaimsPrincipal();
            ClaimsPrincipal user2 = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(AuthConstant.ClaimType.PlatformAdministrator, "uid"),
            }));

            var hackathonManagement = new HackathonManagement();
            Assert.IsNull(await hackathonManagement.GetHackathonRolesAsync(hackathon, null, default));
            Assert.IsNull(await hackathonManagement.GetHackathonRolesAsync(hackathon, user, default));
            Assert.IsNull(await hackathonManagement.GetHackathonRolesAsync(hackathon, user2, default));
        }

        private static IEnumerable GetHackathonRolesAsyncTestData()
        {
            // arg0: user
            // arg1: admins
            // arg2: enrolled
            // arg3: isJudge
            // expected roles

            // platform admin
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(AuthConstant.ClaimType.UserId, "uid"),
                    new Claim(AuthConstant.ClaimType.PlatformAdministrator, "uid"),
                })),
                null,
                false,
                false,
                new HackathonRoles
                {
                    isAdmin = true,
                    isEnrolled = false,
                    isJudge = false
                });

            // hack admin
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(AuthConstant.ClaimType.UserId, "uid"),
                })),
                new List<HackathonAdminEntity>
                {
                    new HackathonAdminEntity { PartitionKey="hack", RowKey="uid" },
                },
                false,
                false,
                new HackathonRoles
                {
                    isAdmin = true,
                    isEnrolled = false,
                    isJudge = false
                });

            // neither platform nor hack admin
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(AuthConstant.ClaimType.UserId, "uid"),
                })),
                new List<HackathonAdminEntity>
                {
                    new HackathonAdminEntity { PartitionKey="hack", RowKey="other" },
                    new HackathonAdminEntity { PartitionKey="hack", RowKey="other2" }
                },
                false,
                false,
                new HackathonRoles
                {
                    isAdmin = false,
                    isEnrolled = false,
                    isJudge = false
                });

            // enrolled
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(AuthConstant.ClaimType.UserId, "uid"),
                    new Claim(AuthConstant.ClaimType.PlatformAdministrator, "uid"),
                })),
                null,
                true,
                false,
                new HackathonRoles
                {
                    isAdmin = true,
                    isEnrolled = true,
                    isJudge = false
                });

            // judge
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(AuthConstant.ClaimType.UserId, "uid"),
                    new Claim(AuthConstant.ClaimType.PlatformAdministrator, "uid"),
                })),
                null,
                false,
                true,
                new HackathonRoles
                {
                    isAdmin = true,
                    isEnrolled = false,
                    isJudge = true
                });
        }

        [Test, TestCaseSource(nameof(GetHackathonRolesAsyncTestData))]
        public async Task GetHackathonRolesAsync(ClaimsPrincipal user, IEnumerable<HackathonAdminEntity> admins, bool enrolled, bool isJudge, HackathonRoles expectedRoles)
        {
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack" };

            var moqs = new Moqs();
            if (admins != null)
            {
                moqs.HackathonAdminManagement.Setup(h => h.ListHackathonAdminAsync(hackathon.Name, default)).ReturnsAsync(admins);
            }
            moqs.EnrollmentManagement.Setup(h => h.IsUserEnrolledAsync(hackathon, "uid", default)).ReturnsAsync(enrolled);
            moqs.JudgeManagement.Setup(j => j.IsJudgeAsync("hack", "uid", default)).ReturnsAsync(isJudge);

            var hackathonManagement = new HackathonManagement()
            {
                EnrollmentManagement = moqs.EnrollmentManagement.Object,
                HackathonAdminManagement = moqs.HackathonAdminManagement.Object,
                JudgeManagement = moqs.JudgeManagement.Object,
            };
            var role = await hackathonManagement.GetHackathonRolesAsync(hackathon, user, default);

            moqs.VerifyAll();
            Debug.Assert(role != null);
            Assert.AreEqual(expectedRoles.isAdmin, role.isAdmin);
            Assert.AreEqual(expectedRoles.isEnrolled, role.isEnrolled);
            Assert.AreEqual(expectedRoles.isJudge, role.isJudge);
        }
        #endregion
    }
}
