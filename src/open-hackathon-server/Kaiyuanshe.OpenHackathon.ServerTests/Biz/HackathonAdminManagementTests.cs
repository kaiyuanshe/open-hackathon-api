﻿using Kaiyuanshe.OpenHackathon.Server.Auth;
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
    public class HackathonAdminManagementTests
    {
        #region CreateAdminAsync
        [Test]
        public async Task CreateAdminAsync()
        {
            var admin = new HackathonAdmin
            {
                hackathonName = "hack",
                userId = "uid"
            };

            var hackathonAdminTable = new Mock<IHackathonAdminTable>();
            hackathonAdminTable.Setup(a => a.InsertOrMergeAsync(It.Is<HackathonAdminEntity>(a =>
                    a.HackathonName == "hack" && a.UserId == "uid"),
                    default));

            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.HackathonAdminTable).Returns(hackathonAdminTable.Object);

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("HackathonAdmin-hack"));

            var management = new HackathonAdminManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await management.CreateAdminAsync(admin, default);

            Mock.VerifyAll(hackathonAdminTable, storageContext, cache);
            hackathonAdminTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            Assert.AreEqual("hack", result.HackathonName);
            Assert.AreEqual("uid", result.UserId);
        }
        #endregion

        #region ListHackathonAdminAsyncTest
        [Test]
        public async Task ListHackathonAdminAsyncTest()
        {
            string name = "hack";
            CancellationToken cancellationToken = CancellationToken.None;
            var data = new List<HackathonAdminEntity>()
            {
                new HackathonAdminEntity{ PartitionKey="pk1", },
                new HackathonAdminEntity{ PartitionKey="pk2", },
            };

            var storageContext = new Mock<IStorageContext>();
            var hackAdminTable = new Mock<IHackathonAdminTable>();
            storageContext.SetupGet(p => p.HackathonAdminTable).Returns(hackAdminTable.Object);
            hackAdminTable.Setup(p => p.ListByHackathonAsync(name, cancellationToken)).ReturnsAsync(data);
            var cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);

            var hackathonAdminManagement = new HackathonAdminManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache,
            };
            var results = await hackathonAdminManagement.ListHackathonAdminAsync(name, cancellationToken);

            Mock.VerifyAll(storageContext, hackAdminTable);
            hackAdminTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("pk1", results.First().HackathonName);
            Assert.AreEqual("pk2", results.Last().HackathonName);
        }
        #endregion

        #region ListPaginatedHackathonAdminAsync
        private static IEnumerable ListPaginatedHackathonAdminAsyncTestData()
        {
            var a1 = new HackathonAdminEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new HackathonAdminEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new HackathonAdminEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new HackathonAdminEntity
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
                new AdminQueryOptions { },
                new List<HackathonAdminEntity> { a1, a2, a3, a4 },
                new List<HackathonAdminEntity> { a4, a2, a3, a1 },
                null
                );

            // by Team
            yield return new TestCaseData(
                new AdminQueryOptions { },
                new List<HackathonAdminEntity> { a1, a2, a3, a4 },
                new List<HackathonAdminEntity> { a4, a2, a3, a1 },
                null
                );

            // by Hackathon
            yield return new TestCaseData(
                new AdminQueryOptions { },
                new List<HackathonAdminEntity> { a1, a2, a3, a4 },
                new List<HackathonAdminEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new AdminQueryOptions { Pagination = new Pagination { top = 2, } },
                new List<HackathonAdminEntity> { a1, a2, a3, a4 },
                new List<HackathonAdminEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new AdminQueryOptions
                {
                    Pagination = new Pagination
                    {
                        top = 2,
                        np = "1",
                        nr = "1",
                    },
                },
                new List<HackathonAdminEntity> { a1, a2, a3, a4 },
                new List<HackathonAdminEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedHackathonAdminAsyncTestData))]
        public async Task ListPaginatedHackathonAdminAsync(
            AdminQueryOptions options,
            IEnumerable<HackathonAdminEntity> all,
            IEnumerable<HackathonAdminEntity> expectedResult,
            Pagination expectedNext)
        {
            string hackName = "hack";

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<HackathonAdminEntity>>>(c => c.CacheKey == "HackathonAdmin-hack"), default)).ReturnsAsync(all);

            var adminManagement = new HackathonAdminManagement()
            {
                Cache = cache.Object,
            };
            var result = await adminManagement.ListPaginatedHackathonAdminAsync(hackName, options, default);

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
                Debug.Assert(options.NextPage != null);
                Assert.AreEqual(expectedNext.np, options.NextPage.np);
                Assert.AreEqual(expectedNext.np, options.NextPage.nr);
            }
        }
        #endregion

        #region GetAdminAsync
        [Test]
        public async Task GetAdminAsync()
        {
            var adminEntity = new HackathonAdminEntity { PartitionKey = "pk" };

            var moqs = new Moqs();
            moqs.HackathonAdminTable.Setup(a => a.RetrieveAsync("hack", "uid", default)).ReturnsAsync(adminEntity);

            var management = new HackathonAdminManagement();
            moqs.SetupManagement(management);
            var result = await management.GetAdminAsync("hack", "uid", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("pk", result.HackathonName);
        }
        #endregion

        #region DeleteAdminAsync
        [Test]
        public async Task DeleteAdminAsync()
        {
            var moqs = new Moqs();
            moqs.HackathonAdminTable.Setup(a => a.DeleteAsync("hack", "uid", default));
            moqs.CacheProvider.Setup(c => c.Remove("HackathonAdmin-hack"));

            var management = new HackathonAdminManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                Cache = moqs.CacheProvider.Object,
            };
            await management.DeleteAdminAsync("hack", "uid", default);

            moqs.VerifyAll();
        }
        #endregion

        #region IsHackathonAdmin
        private static IEnumerable IsHackathonAdminTestData()
        {
            // arg0: user
            // arg1: admins
            // arg2: expected result

            // no userId
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                })),
                null,
                false);

            // platform admin
            yield return new TestCaseData(
                new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(AuthConstant.ClaimType.UserId, "uid"),
                    new Claim(AuthConstant.ClaimType.PlatformAdministrator, "uid"),
                })),
                null,
                true);

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
                true);

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
                false);
        }

        [Test, TestCaseSource(nameof(IsHackathonAdminTestData))]
        public async Task IsHackathonAdmin(ClaimsPrincipal user, IEnumerable<HackathonAdminEntity> admins, bool expectedResult)
        {
            var cache = new Mock<ICacheProvider>();
            if (admins != null)
            {
                cache.Setup(h => h.GetOrAddAsync(It.IsAny<CacheEntry<IEnumerable<HackathonAdminEntity>>>(), default)).ReturnsAsync(admins);
            }

            var hackathonAdminManagement = new Mock<HackathonAdminManagement>() { CallBase = true };
            hackathonAdminManagement.Object.Cache = cache.Object;

            var result = await hackathonAdminManagement.Object.IsHackathonAdmin("hack", user, default);

            Mock.VerifyAll(hackathonAdminManagement, cache);
            hackathonAdminManagement.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult, result);
        }

        #endregion

        #region IsPlatformAdmin
        private static IEnumerable IsPlatformAdminTestData()
        {
            yield return new TestCaseData(null).Returns(false);

            yield return new TestCaseData(new HackathonAdminEntity
            {
                PartitionKey = "hack"
            }).Returns(false);

            yield return new TestCaseData(new HackathonAdminEntity()).Returns(true);
        }

        [Test, TestCaseSource(nameof(IsPlatformAdminTestData))]
        public async Task<bool> IsPlatformAdmin(HackathonAdminEntity admin)
        {
            var moqs = new Moqs();
            moqs.HackathonAdminTable.Setup(p => p.GetPlatformRole("uid", default)).ReturnsAsync(admin);

            var adminManagement = new HackathonAdminManagement()
            {
                StorageContext = moqs.StorageContext.Object,
            };
            var result = await adminManagement.IsPlatformAdmin("uid", default);

            moqs.VerifyAll();
            return result;
        }
        #endregion
    }
}
