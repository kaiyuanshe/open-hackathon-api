﻿using Kaiyuanshe.OpenHackathon.Server.Biz;
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
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    public class AwardManagementTests
    {
        #region GetAwardByIdAsync
        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        [TestCase("", null)]
        [TestCase(" ", null)]
        public async Task GeAwardByIdAsync_Null(string hackName, string awardId)
        {
            AwardManagement awardManagement = new AwardManagement();
            var result = await awardManagement.GetAwardByIdAsync(hackName, awardId, default);

            Assert.IsNull(result);
        }

        [Test]
        public async Task GeAwardByIdAsync_Succeeded()
        {
            string hackName = "Hack";
            string awardId = "aid";
            AwardEntity teamEntity = new AwardEntity { Description = "desc" };

            var moqs = new Moqs();
            moqs.AwardTable.Setup(t => t.RetrieveAsync("hack", "aid", default))
                .ReturnsAsync(teamEntity);

            AwardManagement awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            var result = await awardManagement.GetAwardByIdAsync(hackName, awardId, default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("desc", result.Description);
        }
        #endregion

        #region CanCreateNewAward
        private static IEnumerable CanCreateNewAwardTestData()
        {
            // arg0: awards
            // arg1: expectedResult

            List<AwardEntity> notexceed = new List<AwardEntity>();
            for (int i = 0; i < 99; i++)
            {
                notexceed.Add(new AwardEntity());
            }

            // not exceed
            yield return new TestCaseData(notexceed, true);

            // exceed
            List<AwardEntity> exceed = new List<AwardEntity>();
            for (int i = 0; i < 100; i++)
            {
                exceed.Add(new AwardEntity());
            }
            yield return new TestCaseData(exceed, false);
        }

        [Test, TestCaseSource(nameof(CanCreateNewAwardTestData))]
        public async Task CanCreateNewAward(List<AwardEntity> awards, bool expectedResult)
        {
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<AwardEntity>>>(c => c.CacheKey == "Award-hack"), default))
                .ReturnsAsync(awards);

            var awardManagement = new AwardManagement()
            {
                Cache = cache.Object,
            };
            var result = await awardManagement.CanCreateNewAward("hack", default);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult, result);
        }
        #endregion

        #region CreateAwardAsync

        private static IEnumerable CreateAwardAsyncTestData()
        {
            // arg0: request
            // arg1: expected response

            // default quantity/target
            yield return new TestCaseData(
                    new Award { },
                    new AwardEntity { Quantity = 1, Target = AwardTarget.team }
                );

            // all
            yield return new TestCaseData(
                    new Award
                    {
                        quantity = 10,
                        target = AwardTarget.individual,
                        description = "desc",
                        name = "n",
                        pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p1", description="d1", uri="u1" },
                            new PictureInfo{ name="p2", description="d2", uri="u2" },
                            new PictureInfo{ name="p3", description="d3", uri="u3" },
                        }
                    },
                    new AwardEntity
                    {
                        Quantity = 10,
                        Target = AwardTarget.individual,
                        Description = "desc",
                        Name = "n",
                        Pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p1", description="d1", uri="u1" },
                            new PictureInfo{ name="p2", description="d2", uri="u2" },
                            new PictureInfo{ name="p3", description="d3", uri="u3" },
                        }
                    }
                );
        }

        [Test, TestCaseSource(nameof(CreateAwardAsyncTestData))]
        public async Task CreateAwardAsync(Award request, AwardEntity expectedEntity)
        {
            string hackName = "Hack";
            var cancellationToken = CancellationToken.None;

            var awardTable = new Mock<IAwardTable>();
            awardTable.Setup(t => t.InsertAsync(It.IsAny<AwardEntity>(), cancellationToken));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.AwardTable).Returns(awardTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Award-Hack"));

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await awardManagement.CreateAwardAsync(hackName, request, cancellationToken);

            Mock.VerifyAll(storageContext, awardTable, cache);
            storageContext.VerifyNoOtherCalls();
            awardTable.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedEntity.Description, result.Description);
            Assert.AreEqual("hack", result.HackathonName);
            Assert.IsNotNull(result.Id);
            Assert.AreEqual(Guid.Empty.ToString().Length, result.Id.Length);
            Assert.AreEqual(expectedEntity.Name, result.Name);
            Assert.AreEqual("hack", result.PartitionKey);
            Assert.AreEqual(expectedEntity.Quantity, result.Quantity);
            Assert.AreEqual(expectedEntity.Target, result.Target);
            Assert.AreEqual(expectedEntity.Pictures?.Length, result.Pictures?.Length);
            for (int i = 0; i < result.Pictures?.Length; i++)
            {
                Debug.Assert(expectedEntity.Pictures != null);
                Assert.AreEqual(expectedEntity.Pictures[i].uri, result.Pictures[i].uri);
                Assert.AreEqual(expectedEntity.Pictures[i].name, result.Pictures[i].name);
                Assert.AreEqual(expectedEntity.Pictures[i].description, result.Pictures[i].description);
            }
        }
        #endregion

        #region UpdateAwardAsync
        private static IEnumerable UpdateAwardAsyncTestData()
        {
            // arg0: request
            // arg1: existing entity
            // arg2: expected updated entity

            // empty request
            yield return new TestCaseData(
                    new Award { },
                    new AwardEntity
                    {
                        Quantity = 10,
                        Target = AwardTarget.individual,
                        Description = "desc",
                        Name = "n",
                        Pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p1", description="d1", uri="u1" },
                            new PictureInfo{ name="p2", description="d2", uri="u2" },
                            new PictureInfo{ name="p3", description="d3", uri="u3" },
                        }
                    },
                    new AwardEntity
                    {
                        Quantity = 10,
                        Target = AwardTarget.individual,
                        Description = "desc",
                        Name = "n",
                        Pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p1", description="d1", uri="u1" },
                            new PictureInfo{ name="p2", description="d2", uri="u2" },
                            new PictureInfo{ name="p3", description="d3", uri="u3" },
                        }
                    }
                );

            // all updated
            yield return new TestCaseData(
                    new Award
                    {
                        name = "n2",
                        description = "d2",
                        quantity = 5,
                        target = AwardTarget.team,
                        pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p4", description="d4", uri="u4" },
                        }
                    },
                    new AwardEntity
                    {
                        Quantity = 10,
                        Target = AwardTarget.individual,
                        Description = "desc",
                        Name = "n",
                        Pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p1", description="d1", uri="u1" },
                            new PictureInfo{ name="p2", description="d2", uri="u2" },
                            new PictureInfo{ name="p3", description="d3", uri="u3" },
                        }
                    },
                    new AwardEntity
                    {
                        Quantity = 5,
                        Target = AwardTarget.team,
                        Description = "d2",
                        Name = "n2",
                        Pictures = new PictureInfo[]
                        {
                            new PictureInfo{ name="p4", description="d4", uri="u4" },
                        }
                    }
                );
        }

        [Test, TestCaseSource(nameof(UpdateAwardAsyncTestData))]
        public async Task UpdateAwardAsync(Award request, AwardEntity existing, AwardEntity expectedEntity)
        {
            var moqs = new Moqs();
            moqs.AwardTable.Setup(t => t.MergeAsync(It.IsAny<AwardEntity>(), default));
            moqs.CacheProvider.Setup(c => c.Remove("Award-"));

            AwardManagement awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            var result = await awardManagement.UpdateAwardAsync(existing, request, default);

            moqs.VerifyAll();
            Assert.AreEqual(expectedEntity.Description, result.Description);
            Assert.AreEqual(expectedEntity.Name, result.Name);
            Assert.AreEqual(expectedEntity.Quantity, result.Quantity);
            Assert.AreEqual(expectedEntity.Target, result.Target);
            Assert.AreEqual(expectedEntity.Pictures?.Length, result.Pictures?.Length);
            if (expectedEntity.Pictures != null && result.Pictures != null)
            {
                for (int i = 0; i < result.Pictures.Length; i++)
                {
                    Assert.AreEqual(expectedEntity.Pictures[0].uri, result.Pictures[0].uri);
                    Assert.AreEqual(expectedEntity.Pictures[0].name, result.Pictures[0].name);
                    Assert.AreEqual(expectedEntity.Pictures[0].description, result.Pictures[0].description);
                }
            }
        }

        #endregion

        #region ListAwardsAsync
        [Test]
        public async Task ListAwardsAsync()
        {
            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<AwardEntity>>>(e => e.AutoRefresh == false && e.CacheKey == "Award-hack"), default));

            var awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            await awardManagement.ListAwardsAsync("hack", default);

            moqs.VerifyAll();
        }

        [Test]
        public async Task ListAwardsAsync2()
        {
            IEnumerable<AwardEntity> list = new List<AwardEntity>
            {
                new AwardEntity { RowKey = "rk" }
            };

            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.IsAny<CacheEntry<IEnumerable<AwardEntity>>>(), default)).ReturnsAsync(list);

            var awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            var result = await awardManagement.ListAwardsAsync("hack", default);

            moqs.VerifyAll();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("rk", result.Single().Id);
        }
        #endregion

        #region ListPaginatedAwardsAsync

        private static IEnumerable ListPaginatedAwardsAsyncTestData()
        {
            var a1 = new AwardEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new AwardEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new AwardEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new AwardEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(4),
            };

            // arg0: options
            // arg1: awards
            // arg2: expected result
            // arg3: expected Next

            // minimal
            yield return new TestCaseData(
                new AwardQueryOptions { },
                new List<AwardEntity> { a1, a2, a3, a4 },
                new List<AwardEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new AwardQueryOptions { Pagination = new Pagination { top = 2 } },
                new List<AwardEntity> { a1, a2, a3, a4 },
                new List<AwardEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new AwardQueryOptions
                {
                    Pagination = new Pagination
                    {
                        top = 2,
                        np = "1",
                        nr = "1",
                    },
                },
                new List<AwardEntity> { a1, a2, a3, a4 },
                new List<AwardEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedAwardsAsyncTestData))]
        public async Task ListPaginatedAwardsAsync_Options(
            AwardQueryOptions options,
            IEnumerable<AwardEntity> allAwards,
            IEnumerable<AwardEntity> expectedResult,
            Pagination expectedNext)
        {
            string hackName = "foo";

            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<AwardEntity>>>(c => c.CacheKey == "Award-foo"), default))
                .ReturnsAsync(allAwards);

            var awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            var result = await awardManagement.ListPaginatedAwardsAsync(hackName, options, default);

            moqs.VerifyAll();
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
                Debug.Assert(options.NextPage != null);
                Assert.AreEqual(expectedNext.np, options.NextPage.np);
                Assert.AreEqual(expectedNext.nr, options.NextPage.nr);
            }
        }
        #endregion

        #region DeleteAwardAsync
        [Test]
        public async Task DeleteAwardAsync()
        {
            var cancellationToken = CancellationToken.None;
            AwardEntity award = new AwardEntity
            {
                PartitionKey = "pk",
                RowKey = "rk",
            };

            var awardTable = new Mock<IAwardTable>();
            awardTable.Setup(t => t.DeleteAsync("pk", "rk", cancellationToken));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.AwardTable).Returns(awardTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Award-pk"));

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            await awardManagement.DeleteAwardAsync(award, cancellationToken);

            Mock.VerifyAll(storageContext, awardTable, cache);
            storageContext.VerifyNoOtherCalls();
            awardTable.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();
        }
        #endregion

        #region CreateOrUpdateAssignmentAsync
        [Test]
        public async Task CreateOrUpdateAssignmentAsync_Create()
        {
            var request = new AwardAssignment
            {
                assigneeId = "tid",
                awardId = "award",
                description = "desc",
                hackathonName = "hack",
            };

            var awardAssignmentTable = new Mock<IAwardAssignmentTable>();
            awardAssignmentTable.Setup(t => t.InsertOrReplaceAsync(It.Is<AwardAssignmentEntity>(a =>
                a.HackathonName == "hack" &&
                a.AssignmentId == "807201a4-f684-f7dc-dc90-7f555aa67d4b" &&
                a.Description == "desc" &&
                a.AssigneeId == "tid" &&
                a.AwardId == "award"), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.AwardAssignmentTable).Returns(awardAssignmentTable.Object);

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await awardManagement.CreateOrUpdateAssignmentAsync(request, default);

            Mock.VerifyAll(storageContext, awardAssignmentTable);
            storageContext.VerifyNoOtherCalls();
            awardAssignmentTable.VerifyNoOtherCalls();

            Assert.AreEqual("tid", result.AssigneeId);
            Assert.AreEqual("award", result.AwardId);
            Assert.AreEqual("desc", result.Description);
            Assert.AreEqual("hack", result.HackathonName);
            Assert.AreEqual("807201a4-f684-f7dc-dc90-7f555aa67d4b", result.AssignmentId);
        }
        #endregion

        #region UpdateAssignmentAsync
        [TestCase(null, "desc")]
        [TestCase("desc2", "desc2")]
        public async Task UpdateAssignmentAsync(string requestDesc, string expectedDesc)
        {
            AwardAssignmentEntity existing = new AwardAssignmentEntity { Description = "desc" };
            AwardAssignment request = new AwardAssignment { description = requestDesc, assigneeId = "ass", assignmentId = "rk", awardId = "award" };

            var awardAssignmentTable = new Mock<IAwardAssignmentTable>();
            awardAssignmentTable.Setup(t => t.MergeAsync(It.Is<AwardAssignmentEntity>(a => a.Description == expectedDesc), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.AwardAssignmentTable).Returns(awardAssignmentTable.Object);

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await awardManagement.UpdateAssignmentAsync(existing, request, default);

            Mock.VerifyAll(awardAssignmentTable, storageContext);
            awardAssignmentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(expectedDesc, result.Description);
            Assert.IsNull(result.AssigneeId);
            Assert.IsNull(result.AssignmentId);
            Assert.IsNull(result.AwardId);
            Assert.IsNull(result.HackathonName);
        }
        #endregion

        #region GetAssignmentCountAsync
        [Test]
        public async Task GetAssignmentCountAsync_ByHackathon()
        {
            var assignments = new List<AwardAssignmentEntity>
            {
                new AwardAssignmentEntity
                {
                    AssigneeId = "tid",
                    Description = "desc"
                }
            };

            var moqs = new Moqs();
            moqs.AwardAssignmentTable.Setup(t => t.ListByHackathonAsync("hack", default)).ReturnsAsync(assignments);

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object),
            };
            var count = await awardManagement.GetAssignmentCountAsync("hack", "", default);

            moqs.VerifyAll();
            Assert.AreEqual(1, count);
        }

        [Test]
        public async Task GetAssignmentCountAsync_ByAward()
        {
            var assignments = new List<AwardAssignmentEntity>
            {
                new AwardAssignmentEntity
                {
                    AssigneeId = "tid",
                    Description = "desc"
                }
            };

            var moqs = new Moqs();
            moqs.AwardAssignmentTable.Setup(t => t.ListByAwardAsync("hack", "award", default)).ReturnsAsync(assignments);

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = moqs.StorageContext.Object,
                Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object),
            };
            var count = await awardManagement.GetAssignmentCountAsync("hack", "award", default);

            moqs.VerifyAll();
            Assert.AreEqual(1, count);
        }
        #endregion

        #region GetAssignmentAsync
        [Test]
        public async Task GetAssignmentAsync()
        {
            AwardAssignmentEntity entity = new AwardAssignmentEntity { AwardId = "award" };

            var moqs = new Moqs();
            moqs.AwardAssignmentTable.Setup(t => t.RetrieveAsync("hack", "assign", default)).ReturnsAsync(entity);

            AwardManagement awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            var result = await awardManagement.GetAssignmentAsync("hack", "assign", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("award", result.AwardId);
        }
        #endregion

        #region ListPaginatedAssignmentsAsync

        private static IEnumerable ListPaginatedAssignmentsAsyncTestData()
        {
            var a1 = new AwardAssignmentEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new AwardAssignmentEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new AwardAssignmentEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new AwardAssignmentEntity
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
                new AwardAssignmentQueryOptions { AwardId = "awardId", QueryType = AwardAssignmentQueryType.Award, },
                new List<AwardAssignmentEntity> { a1, a2, a3, a4 },
                new List<AwardAssignmentEntity> { a4, a2, a3, a1 },
                null
                );

            // by Team
            yield return new TestCaseData(
                new AwardAssignmentQueryOptions { TeamId = "teamId", QueryType = AwardAssignmentQueryType.Team, },
                new List<AwardAssignmentEntity> { a1, a2, a3, a4 },
                new List<AwardAssignmentEntity> { a4, a2, a3, a1 },
                null
                );

            // by Hackathon
            yield return new TestCaseData(
                new AwardAssignmentQueryOptions { QueryType = AwardAssignmentQueryType.Hackathon, },
                new List<AwardAssignmentEntity> { a1, a2, a3, a4 },
                new List<AwardAssignmentEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new AwardAssignmentQueryOptions { Pagination = new Pagination { top = 2 }, AwardId = "awardId", QueryType = AwardAssignmentQueryType.Award, },
                new List<AwardAssignmentEntity> { a1, a2, a3, a4 },
                new List<AwardAssignmentEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new AwardAssignmentQueryOptions
                {
                    Pagination = new Pagination
                    {
                        top = 2,
                        np = "1",
                        nr = "1"
                    },
                    AwardId = "awardId",
                    QueryType = AwardAssignmentQueryType.Award,
                },
                new List<AwardAssignmentEntity> { a1, a2, a3, a4 },
                new List<AwardAssignmentEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedAssignmentsAsyncTestData))]
        public async Task ListPaginatedAssignmentsAsync(
            AwardAssignmentQueryOptions options,
            IEnumerable<AwardAssignmentEntity> allAwards,
            IEnumerable<AwardAssignmentEntity> expectedResult,
            Pagination expectedNext)
        {
            string hackName = "hack";

            var moqs = new Moqs();
            if (options.QueryType == AwardAssignmentQueryType.Award)
            {
                moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<AwardAssignmentEntity>>>(c => c.CacheKey == "AwardAssignment-hack-awardId"), default))
                  .ReturnsAsync(allAwards);
            }
            if (options.QueryType == AwardAssignmentQueryType.Hackathon)
            {
                moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<AwardAssignmentEntity>>>(c => c.CacheKey == "AwardAssignment-hack"), default))
                  .ReturnsAsync(allAwards);
            }
            if (options.QueryType == AwardAssignmentQueryType.Team)
            {
                moqs.AwardAssignmentTable.Setup(t => t.ListByAssigneeAsync("hack", options.TeamId, default)).ReturnsAsync(allAwards);
            }

            var awardManagement = new AwardManagement();
            moqs.SetupManagement(awardManagement);
            var result = await awardManagement.ListPaginatedAssignmentsAsync(hackName, options, default);

            moqs.VerifyAll();
            Assert.AreEqual(expectedResult.Count(), result.Count());
            for (int i = 0; i < expectedResult.Count(); i++)
            {
                Assert.AreEqual(expectedResult.ElementAt(i).AssignmentId, result.ElementAt(i).AssignmentId);
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
                Assert.AreEqual(expectedNext.nr, options.NextPage.nr);
            }
        }
        #endregion

        #region ListAssignmentsByTeamAsync
        [Test]
        public async Task ListAssignmentsByTeamAsync()
        {
            var assignments = new List<AwardAssignmentEntity>
            {
                new AwardAssignmentEntity
                {
                    Description = "desc"
                }
            };

            var awardAssignmentTable = new Mock<IAwardAssignmentTable>();
            awardAssignmentTable.Setup(t => t.ListByAssigneeAsync("hack", "tid", default)).ReturnsAsync(assignments);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.AwardAssignmentTable).Returns(awardAssignmentTable.Object);

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await awardManagement.ListAssignmentsByTeamAsync("hack", "tid", default);

            Mock.VerifyAll(awardAssignmentTable, storageContext);
            awardAssignmentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("desc", result.First().Description);
        }
        #endregion

        #region DeleteAssignmentAsync
        [Test]
        public async Task DeleteAssignmentAsync()
        {
            var awardAssignmentTable = new Mock<IAwardAssignmentTable>();
            awardAssignmentTable.Setup(t => t.DeleteAsync("hack", "assign", default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.AwardAssignmentTable).Returns(awardAssignmentTable.Object);

            AwardManagement awardManagement = new AwardManagement()
            {
                StorageContext = storageContext.Object,
            };
            await awardManagement.DeleteAssignmentAsync("hack", "assign", default);

            Mock.VerifyAll(awardAssignmentTable, storageContext);
            awardAssignmentTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
        }
        #endregion
    }
}
