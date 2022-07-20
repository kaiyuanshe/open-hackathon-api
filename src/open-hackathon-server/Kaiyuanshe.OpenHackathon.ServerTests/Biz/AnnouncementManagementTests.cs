using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class AnnouncementManagementTests
    {
        #region Create
        [Test]
        public async Task Create()
        {
            var parameter = new Announcement
            {
                hackathonName = "hack",
                title = "title",
                content = "content",
            };

            var moqs = new Moqs();
            moqs.AnnouncementTable.Setup(a => a.InsertAsync(It.Is<AnnouncementEntity>(e =>
                e.PartitionKey == "hack"
                && e.RowKey.Length == 36
                && e.Content == "content"
                && e.Title == "title"
                && e.CreatedAt != default), default));
            moqs.CacheProvider.Setup(c => c.Remove("Announcement-hack"));

            var managementClient = new AnnouncementManagement();
            moqs.SetupManagement(managementClient);
            var resp = await managementClient.Create(parameter, default);

            moqs.VerifyAll();
            Assert.IsNotNull(resp);
            Assert.AreEqual("hack", resp.HackathonName);
        }
        #endregion

        #region GetById
        [Test]
        public async Task GetById()
        {
            var entity = new AnnouncementEntity { Content = "ct" };

            var moqs = new Moqs();
            moqs.AnnouncementTable.Setup(t => t.RetrieveAsync("pk", "rk", default)).ReturnsAsync(entity);

            var managementClient = new AnnouncementManagement();
            moqs.SetupManagement(managementClient);
            var result = await managementClient.GetById("pk", "rk", default);

            moqs.VerifyAll();
            Assert.IsNotNull(result);
            Debug.Assert(result != null);
            Assert.AreEqual("ct", result.Content);
        }
        #endregion

        #region Update
        [Test]
        public async Task Update()
        {
            var parameter = new Announcement
            {
                title = "title2",
                content = "content2",
            };
            var entity = new AnnouncementEntity { PartitionKey = "pk", Title = "title", Content = "content" };

            var moqs = new Moqs();
            moqs.AnnouncementTable.Setup(a => a.MergeAsync(It.Is<AnnouncementEntity>(e =>
                e.Content == "content2"
                && e.Title == "title2"), default));
            moqs.CacheProvider.Setup(c => c.Remove("Announcement-pk"));

            var managementClient = new AnnouncementManagement();
            moqs.SetupManagement(managementClient);
            var resp = await managementClient.Update(entity, parameter, default);

            moqs.VerifyAll();
            Assert.IsNotNull(resp);
            Assert.AreEqual("title2", resp.Title);
        }
        #endregion

        #region ListPaginated
        private static IEnumerable ListPaginatedTestData()
        {
            var a1 = new AnnouncementEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new AnnouncementEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new AnnouncementEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new AnnouncementEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(4),
            };

            // arg0: options
            // arg1: awards
            // arg2: expected result
            // arg3: expected Next

            // by Award
            yield return new TestCaseData(
                new AnnouncementQueryOptions { HackathonName = "hack" },
                new List<AnnouncementEntity> { a1, a2, a3, a4 },
                new List<AnnouncementEntity> { a4, a2, a3, a1 },
                null
                );

            // by Team
            yield return new TestCaseData(
                new AnnouncementQueryOptions { HackathonName = "hack" },
                new List<AnnouncementEntity> { a1, a2, a3, a4 },
                new List<AnnouncementEntity> { a4, a2, a3, a1 },
                null
                );

            // by Hackathon
            yield return new TestCaseData(
                new AnnouncementQueryOptions { HackathonName = "hack" },
                new List<AnnouncementEntity> { a1, a2, a3, a4 },
                new List<AnnouncementEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new AnnouncementQueryOptions { HackathonName = "hack", Pagination = new Pagination { top = 2, } },
                new List<AnnouncementEntity> { a1, a2, a3, a4 },
                new List<AnnouncementEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new AnnouncementQueryOptions
                {
                    HackathonName = "hack",
                    Pagination = new Pagination
                    {
                        np = "1",
                        nr = "1",
                        top = 2,
                    },
                },
                new List<AnnouncementEntity> { a1, a2, a3, a4 },
                new List<AnnouncementEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedTestData))]
        public async Task ListPaginated(
            AnnouncementQueryOptions options,
            IEnumerable<AnnouncementEntity> all,
            IEnumerable<AnnouncementEntity> expectedResult,
            Pagination expectedNext)
        {
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<AnnouncementEntity>>>(c => c.CacheKey == "Announcement-hack"), default)).ReturnsAsync(all);

            var mgmt = new AnnouncementManagement()
            {
                Cache = cache.Object,
            };
            var result = await mgmt.ListPaginated(options, default);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < expectedResult.Count(); i++)
            {
                Assert.AreEqual(expectedResult.ElementAt(i).Id, result.ElementAt(i).Id);
            }
            if (expectedNext == null)
            {
                Assert.IsNull(options.NextPage);
            }
            else
            {
                Assert.IsNotNull(options.NextPage);
                Assert.AreEqual(expectedNext.np, options.NextPage?.np);
                Assert.AreEqual(expectedNext.nr, options.NextPage?.nr);
            }
        }
        #endregion
    }
}
