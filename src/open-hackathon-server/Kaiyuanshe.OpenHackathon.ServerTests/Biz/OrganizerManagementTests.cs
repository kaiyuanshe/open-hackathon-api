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
using System.Linq;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class OrganizerManagementTests
    {
        #region CreateOrganizer
        [Test]
        public async Task CreateOrganizer()
        {
            var organizer = new Organizer
            {
                type = OrganizerType.sponsor,
                description = "desc",
                logo = new PictureInfo { description = "d2", name = "n2", uri = "u2" },
                name = "name"
            };

            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.InsertAsync(It.Is<OrganizerEntity>(e =>
                e.PartitionKey == "hack"
                && e.RowKey.Length == 36
                && e.Type == OrganizerType.sponsor
                && e.Name == "name"
                && e.Description == "desc"
                && e.Logo.description == "d2"
                && e.Logo.name == "n2"
                && e.Logo.uri == "u2"), default));
            moqs.CacheProvider.Setup(c => c.Remove("Organizer-hack"));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.CreateOrganizer("hack", organizer, default);

            moqs.VerifyAll();
        }
        #endregion

        #region UpdateOrganizer
        [Test]
        public async Task UpdateOrganizer_NoUpdate()
        {
            var entity = new OrganizerEntity
            {
                PartitionKey = "pk",
                Name = "name1",
                Description = "desc1",
                Type = OrganizerType.coorganizer,
                Logo = new PictureInfo { description = "ld1", name = "ln1", uri = "lu1" },
            };
            var organizer = new Organizer();

            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.MergeAsync(It.Is<OrganizerEntity>(e =>
                e.Type == OrganizerType.coorganizer
                && e.Name == "name1"
                && e.Description == "desc1"
                && e.Logo.description == "ld1"
                && e.Logo.name == "ln1"
                && e.Logo.uri == "lu1"), default));
            moqs.CacheProvider.Setup(c => c.Remove("Organizer-pk"));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.UpdateOrganizer(entity, organizer, default);

            moqs.VerifyAll();
        }

        [Test]
        public async Task UpdateOrganizer_UpdateAll()
        {
            var entity = new OrganizerEntity
            {
                PartitionKey = "pk",
                Name = "name1",
                Description = "desc1",
                Type = OrganizerType.coorganizer,
                Logo = new PictureInfo { description = "ld1", name = "ln1", uri = "lu1" },
            };
            var organizer = new Organizer
            {
                name = "name2",
                description = "desc2",
                type = OrganizerType.sponsor,
                logo = new PictureInfo { description = "ld2", name = "ln2", uri = "lu2" },
            };

            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.MergeAsync(It.Is<OrganizerEntity>(e =>
                e.Type == OrganizerType.sponsor
                && e.Name == "name2"
                && e.Description == "desc2"
                && e.Logo.description == "ld2"
                && e.Logo.name == "ln2"
                && e.Logo.uri == "lu2"), default));
            moqs.CacheProvider.Setup(c => c.Remove("Organizer-pk"));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.UpdateOrganizer(entity, organizer, default);

            moqs.VerifyAll();
        }
        #endregion

        #region GetOrganizerById
        [Test]
        public async Task GetOrganizerById()
        {
            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.RetrieveAsync("hack", "oid", default));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.GetOrganizerById("hack", "oid", default);

            moqs.VerifyAll();
        }
        #endregion

        #region ListPaginatedOrganizersAsync
        private static IEnumerable ListPaginatedOrganizersAsyncTestData()
        {
            var a1 = new OrganizerEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new OrganizerEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new OrganizerEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new OrganizerEntity
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
                new OrganizerQueryOptions { },
                new List<OrganizerEntity> { a1, a2, a3, a4 },
                new List<OrganizerEntity> { a4, a2, a3, a1 },
                null
                );

            // by Team
            yield return new TestCaseData(
                new OrganizerQueryOptions { },
                new List<OrganizerEntity> { a1, a2, a3, a4 },
                new List<OrganizerEntity> { a4, a2, a3, a1 },
                null
                );

            // by Hackathon
            yield return new TestCaseData(
                new OrganizerQueryOptions { },
                new List<OrganizerEntity> { a1, a2, a3, a4 },
                new List<OrganizerEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new OrganizerQueryOptions { Pagination = new Pagination { top = 2, } },
                new List<OrganizerEntity> { a1, a2, a3, a4 },
                new List<OrganizerEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new OrganizerQueryOptions
                {
                    Pagination = new Pagination
                    {
                        np = "1",
                        nr = "1",
                        top = 2,
                    },
                },
                new List<OrganizerEntity> { a1, a2, a3, a4 },
                new List<OrganizerEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedOrganizersAsyncTestData))]
        public async Task ListPaginatedAssignmentsAsync(
            OrganizerQueryOptions options,
            IEnumerable<OrganizerEntity> all,
            IEnumerable<OrganizerEntity> expectedResult,
            Pagination expectedNext)
        {
            string hackName = "hack";

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<OrganizerEntity>>>(c => c.CacheKey == "Organizer-hack"), default)).ReturnsAsync(all);

            var organizerManagement = new OrganizerManagement()
            {
                Cache = cache.Object,
            };
            var result = await organizerManagement.ListPaginatedOrganizersAsync(hackName, options, default);

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

        #region DeleteOrganzer
        [Test]
        public async Task DeleteOrganzer()
        {
            var moqs = new Moqs();
            moqs.OrganizerTable.Setup(o => o.DeleteAsync("hack", "oid", default));
            moqs.CacheProvider.Setup(c => c.Remove("Organizer-hack"));

            var management = new OrganizerManagement();
            moqs.SetupManagement(management);
            await management.DeleteOrganizer("hack", "oid", default);

            moqs.VerifyAll();
        }
        #endregion
    }
}
