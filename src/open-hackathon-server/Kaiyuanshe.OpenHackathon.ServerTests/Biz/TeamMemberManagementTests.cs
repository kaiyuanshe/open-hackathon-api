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
    internal class TeamMemberManagementTests
    {
        #region Create
        [Test]
        public async Task Create()
        {
            var parameter = new TeamMember
            {
                hackathonName = "hack",
                userId = "uid",
                description = "desc",
            };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(a => a.InsertAsync(It.Is<TeamMemberEntity>(e =>
                e.PartitionKey == "hack"
                && e.RowKey == "uid"
                && e.Description == "desc"
                && e.CreatedAt != default), default));
            moqs.CacheProvider.Setup(c => c.Remove("TeamMember-hack"));

            var managementClient = new TeamMemberManagement();
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
            var entity = new TeamMemberEntity { Description = "desc" };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(t => t.RetrieveAsync("pk", "rk", default)).ReturnsAsync(entity);

            var managementClient = new TeamMemberManagement();
            moqs.SetupManagement(managementClient);
            var result = await managementClient.GetById("pk", "rk", default);

            moqs.VerifyAll();
            Assert.IsNotNull(result);
            Debug.Assert(result != null);
            Assert.AreEqual("desc", result.Description);
        }
        #endregion

        #region Update
        [Test]
        public async Task Update()
        {
            var parameter = new TeamMember
            {
                description = "desc2",
            };
            var entity = new TeamMemberEntity
            {
                PartitionKey = "pk",
                Description = "desc",
            };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(a => a.MergeAsync(It.Is<TeamMemberEntity>(e =>
                e.Description == "desc2"), default));
            moqs.CacheProvider.Setup(c => c.Remove("TeamMember-pk"));

            var managementClient = new TeamMemberManagement();
            moqs.SetupManagement(managementClient);
            var resp = await managementClient.Update(entity, parameter, default);

            moqs.VerifyAll();
            Assert.IsNotNull(resp);
            Assert.AreEqual("desc2", resp.Description);
        }
        #endregion

        #region ListPaginated
        private static IEnumerable ListPaginatedTestData()
        {
            var a1 = new TeamMemberEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new TeamMemberEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new TeamMemberEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new TeamMemberEntity
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
                new TeamMemberQueryOptions { HackathonName = "hack", TeamId = "tid" },
                new List<TeamMemberEntity> { a1, a2, a3, a4 },
                new List<TeamMemberEntity> { a4, a2, a3, a1 },
                null
                );

            // by Team
            yield return new TestCaseData(
                new TeamMemberQueryOptions { HackathonName = "hack", TeamId = "tid" },
                new List<TeamMemberEntity> { a1, a2, a3, a4 },
                new List<TeamMemberEntity> { a4, a2, a3, a1 },
                null
                );

            // by Hackathon
            yield return new TestCaseData(
                new TeamMemberQueryOptions { HackathonName = "hack", TeamId = "tid" },
                new List<TeamMemberEntity> { a1, a2, a3, a4 },
                new List<TeamMemberEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new TeamMemberQueryOptions { HackathonName = "hack", TeamId = "tid", Pagination = new Pagination { top = 2, } },
                new List<TeamMemberEntity> { a1, a2, a3, a4 },
                new List<TeamMemberEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new TeamMemberQueryOptions
                {
                    HackathonName = "hack",
                    TeamId = "tid",
                    Pagination = new Pagination
                    {
                        np = "1",
                        nr = "1",
                        top = 2,
                    },
                },
                new List<TeamMemberEntity> { a1, a2, a3, a4 },
                new List<TeamMemberEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedTestData))]
        public async Task ListPaginated(
            TeamMemberQueryOptions options,
            IEnumerable<TeamMemberEntity> all,
            IEnumerable<TeamMemberEntity> expectedResult,
            Pagination expectedNext)
        {
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<TeamMemberEntity>>>(c => c.CacheKey == "TeamMember-hack"), default)).ReturnsAsync(all);

            var mgmt = new TeamMemberManagement()
            {
                Cache = cache.Object,
            };
            var result = await mgmt.ListPaginated(options, default);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < expectedResult.Count(); i++)
            {
                Assert.AreEqual(expectedResult.ElementAt(i).UserId, result.ElementAt(i).UserId);
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

        #region Delete
        [Test]
        public async Task Delete()
        {
            var entity = new TeamMemberEntity { PartitionKey = "pk", RowKey = "rk" };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(t => t.DeleteAsync("pk", "rk", default));
            moqs.CacheProvider.Setup(c => c.Remove("TeamMember-pk"));

            var managementClient = new TeamMemberManagement();
            moqs.SetupManagement(managementClient);
            await managementClient.Delete(entity, default);

            moqs.VerifyAll();
        }
        #endregion
    }
}
