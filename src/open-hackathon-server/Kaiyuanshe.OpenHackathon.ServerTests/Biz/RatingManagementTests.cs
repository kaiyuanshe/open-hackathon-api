﻿using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
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
    public class RatingManagementTests
    {
        #region CanCreateRatingKindAsync
        private static IEnumerable CanCreateRatingKindAsyncTestData()
        {
            // arg0: judges
            // arg1: expectedResult

            List<RatingKindEntity> notexceed = new List<RatingKindEntity>();
            for (int i = 0; i < 99; i++)
            {
                notexceed.Add(new RatingKindEntity());
            }

            // not exceed
            yield return new TestCaseData(notexceed, true);

            // exceed
            List<RatingKindEntity> exceed = new List<RatingKindEntity>();
            for (int i = 0; i < 100; i++)
            {
                exceed.Add(new RatingKindEntity());
            }
            yield return new TestCaseData(exceed, false);
        }

        [Test, TestCaseSource(nameof(CanCreateRatingKindAsyncTestData))]
        public async Task CanCreateRatingKindAsync(List<RatingKindEntity> judges, bool expectedResult)
        {

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<RatingKindEntity>>>(c => c.CacheKey == "RatingKind-hack"), default))
                .ReturnsAsync(judges);

            var ratingManagement = new RatingManagement()
            {
                Cache = cache.Object,
            };
            var result = await ratingManagement.CanCreateRatingKindAsync("hack", default);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

            Assert.AreEqual(expectedResult, result);
        }
        #endregion

        #region CreateRatingKindAsync
        [TestCase(null, 10)]
        [TestCase(5, 5)]
        public async Task CreateRatingKindAsync(int? maxRating, int expectedMaxRating)
        {
            RatingKind parameter = new RatingKind
            {
                name = "name",
                description = "desc",
                hackathonName = "hack",
                maximumScore = maxRating,
            };


            var ratingKindTable = new Mock<IRatingKindTable>();
            ratingKindTable.Setup(t => t.InsertAsync(It.Is<RatingKindEntity>(en =>
                en.Name == "name" &&
                en.Description == "desc" &&
                en.PartitionKey == "hack" &&
                en.MaximumScore == expectedMaxRating &&
                !string.IsNullOrWhiteSpace(en.Id) &&
                en.CreatedAt > DateTime.UtcNow.AddMinutes(-1)), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.RatingKindTable).Returns(ratingKindTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("RatingKind-hack"));

            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await ratingManagement.CreateRatingKindAsync(parameter, default);

            Mock.VerifyAll(storageContext, ratingKindTable, cache);
            ratingKindTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            Assert.AreEqual("name", result.Name);
            Assert.AreEqual("desc", result.Description);
            Assert.AreEqual("hack", result.HackathonName);
            Assert.AreEqual(expectedMaxRating, result.MaximumScore);
        }
        #endregion

        #region UpdateRatingKindAsync
        [TestCase(null, 4)]
        [TestCase(8, 8)]
        public async Task UpdateRatingKindAsync(int? newMaxRating, int expectedNewValue)
        {
            RatingKindEntity existing = new RatingKindEntity
            {
                PartitionKey = "pk",
                Name = "n1",
                Description = "d1",
                MaximumScore = 4
            };
            RatingKind parameter = new RatingKind { name = "n2", description = "d2", maximumScore = newMaxRating };


            var ratingKindTable = new Mock<IRatingKindTable>();
            ratingKindTable.Setup(t => t.MergeAsync(It.Is<RatingKindEntity>(en =>
                en.Name == "n2" &&
                en.Description == "d2" &&
                en.MaximumScore == expectedNewValue), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(s => s.RatingKindTable).Returns(ratingKindTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("RatingKind-pk"));

            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await ratingManagement.UpdateRatingKindAsync(existing, parameter, default);

            Mock.VerifyAll(storageContext, ratingKindTable, cache);
            ratingKindTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

            Assert.AreEqual("n2", result.Name);
            Assert.AreEqual("d2", result.Description);
            Assert.AreEqual("pk", result.HackathonName);
            Assert.AreEqual(expectedNewValue, result.MaximumScore);
        }
        #endregion

        #region GetCachedRatingKindAsync
        private static IEnumerable GetCachedRatingKindAsyncTestData()
        {
            var k1 = new RatingKindEntity { RowKey = "kid" };
            var k2 = new RatingKindEntity { RowKey = "r2" };

            yield return new TestCaseData(new List<RatingKindEntity> { k1, k2 })
            {
                ExpectedResult = k1
            };

            yield return new TestCaseData(new List<RatingKindEntity> { k2 })
            {
                ExpectedResult = default(RatingKindEntity)
            };
        }
        [Test, TestCaseSource(nameof(GetCachedRatingKindAsyncTestData))]
        public async Task<RatingKindEntity?> GetCachedRatingKindAsync(List<RatingKindEntity> ratingKinds)
        {
            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<RatingKindEntity>>>(c => c.CacheKey == "RatingKind-hack"), default))
                .ReturnsAsync(ratingKinds);

            var ratingManagement = new RatingManagement();
            moqs.SetupManagement(ratingManagement);
            var result = await ratingManagement.GetCachedRatingKindAsync("hack", "kid", default);

            moqs.VerifyAll();
            return result;
        }
        #endregion

        #region GetRatingKindAsync
        [Test]
        public async Task GetRatingKindAsync()
        {
            RatingKindEntity kind = new RatingKindEntity { PartitionKey = "pk" };

            var moqs = new Moqs();
            moqs.RatingKindTable.Setup(t => t.RetrieveAsync("hack", "kid", default)).ReturnsAsync(kind);

            var ratingManagement = new RatingManagement();
            moqs.SetupManagement(ratingManagement);
            var result = await ratingManagement.GetRatingKindAsync("hack", "kid", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("pk", result.HackathonName);
        }
        #endregion

        #region ListPaginatedRatingKindsAsync

        private static IEnumerable ListPaginatedRatingKindsAsyncTestData()
        {
            var a1 = new RatingKindEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new RatingKindEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new RatingKindEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new RatingKindEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(4),
            };

            // arg0: options
            // arg1: all kinds
            // arg2: expected result
            // arg3: expected Next

            // minimal
            yield return new TestCaseData(
                new RatingKindQueryOptions { },
                new List<RatingKindEntity> { a1, a2, a3, a4 },
                new List<RatingKindEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new RatingKindQueryOptions { Pagination = new Pagination { top = 2 } },
                new List<RatingKindEntity> { a1, a2, a3, a4 },
                new List<RatingKindEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new RatingKindQueryOptions
                {
                    Pagination = new Pagination
                    {
                        top = 2,
                        np = "1",
                        nr = "1"
                    },
                },
                new List<RatingKindEntity> { a1, a2, a3, a4 },
                new List<RatingKindEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedRatingKindsAsyncTestData))]
        public async Task ListPaginatedAwardsAsync_Options(
            RatingKindQueryOptions options,
            IEnumerable<RatingKindEntity> allAwards,
            IEnumerable<RatingKindEntity> expectedResult,
            Pagination expectedNext)
        {
            string hackName = "foo";

            var moqs = new Moqs();
            moqs.CacheProvider.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<RatingKindEntity>>>(c => c.CacheKey == "RatingKind-foo"), default))
                .ReturnsAsync(allAwards);

            var ratingManagement = new RatingManagement();
            moqs.SetupManagement(ratingManagement);
            var result = await ratingManagement.ListPaginatedRatingKindsAsync(hackName, options, default);

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

        #region DeleteJudgeAsync
        [Test]
        public async Task DeleteJudgeAsync()
        {

            var ratingKindTable = new Mock<IRatingKindTable>();
            ratingKindTable.Setup(j => j.DeleteAsync("hack", "kid", default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.Setup(s => s.RatingKindTable).Returns(ratingKindTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("RatingKind-hack"));

            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            await ratingManagement.DeleteRatingKindAsync("hack", "kid", default);

            Mock.VerifyAll(ratingKindTable, storageContext, cache);
            ratingKindTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();
        }
        #endregion

        #region CreateRatingAsync
        [Test]
        public async Task CreateRatingAsync()
        {
            var parameter = new Rating
            {
                hackathonName = "hack",
                description = "desc",
                judgeId = "jid",
                ratingKindId = "kid",
                score = 5,
                teamId = "tid",
            };

            var ratingTable = new Mock<IRatingTable> { };
            ratingTable.Setup(t => t.InsertOrMergeAsync(It.Is<RatingEntity>(r =>
                  r.CreatedAt > DateTime.UtcNow.AddMinutes(-1) &&
                  r.Description == "desc" &&
                  r.HackathonName == "hack" &&
                  r.Id == "2f4291c6-d890-8fa1-316b-8c2b08256b25" &&
                  r.JudgeId == "jid" &&
                  r.RatingKindId == "kid" &&
                  r.Score == 5 &&
                  r.TeamId == "tid"
                ), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.Setup(s => s.RatingTable).Returns(ratingTable.Object);


            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await ratingManagement.CreateRatingAsync(parameter, default);

            Mock.VerifyAll(ratingTable, storageContext);
            ratingTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual("desc", result.Description);
        }
        #endregion

        #region UpdateRatingAsync
        [Test]
        public async Task UpdateRatingAsync()
        {
            var existing = new RatingEntity { PartitionKey = "h1", Score = 1, Description = "d1", TeamId = "t1" };
            var parameter = new Rating
            {
                hackathonName = "h2",
                description = "d2",
                score = 2,
                teamId = "t2",
            };

            var ratingTable = new Mock<IRatingTable> { };
            ratingTable.Setup(t => t.MergeAsync(It.Is<RatingEntity>(r =>
                  r.Description == "d2" &&
                  r.HackathonName == "h1" &&
                  r.Score == 2 &&
                  r.TeamId == "t1"
                ), default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.Setup(s => s.RatingTable).Returns(ratingTable.Object);


            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await ratingManagement.UpdateRatingAsync(existing, parameter, default);

            Mock.VerifyAll(ratingTable, storageContext);
            ratingTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual("d2", result.Description);
        }
        #endregion

        #region GetRatingAsync
        [Test]
        public async Task GetRatingAsync_ForCreate()
        {
            var entity = new RatingEntity { Description = "desc" };

            var moqs = new Moqs();
            moqs.RatingTable.Setup(t => t.RetrieveAsync("hack", "2f4291c6-d890-8fa1-316b-8c2b08256b25", default)).ReturnsAsync(entity);

            var ratingManagement = new RatingManagement();
            moqs.SetupManagement(ratingManagement);
            var result = await ratingManagement.GetRatingAsync("hack", "jid", "tid", "kid", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("desc", result.Description);
        }

        [Test]
        public async Task GetRatingAsync()
        {
            var entity = new RatingEntity { Description = "desc" };

            var moqs = new Moqs();
            moqs.RatingTable.Setup(t => t.RetrieveAsync("hack", "rid", default)).ReturnsAsync(entity);

            var ratingManagement = new RatingManagement();
            moqs.SetupManagement(ratingManagement);
            var result = await ratingManagement.GetRatingAsync("hack", "rid", default);

            moqs.VerifyAll();
            Debug.Assert(result != null);
            Assert.AreEqual("desc", result.Description);
        }
        #endregion

        #region IsRatingCountGreaterThanZero
        private static IEnumerable IsRatingCountGreaterThanZeroTestData()
        {
            // arg0: options
            // arg1: expected query

            // filter by kind
            yield return new TestCaseData(
               new RatingQueryOptions { RatingKindId = "kid" },
               "(PartitionKey eq 'hack') and (RatingKindId eq 'kid')"
               );

            // filter by team
            yield return new TestCaseData(
               new RatingQueryOptions { TeamId = "tid" },
               "(PartitionKey eq 'hack') and (TeamId eq 'tid')"
               );

            // filter by judge
            yield return new TestCaseData(
               new RatingQueryOptions { JudgeId = "uid" },
               "(PartitionKey eq 'hack') and (JudgeId eq 'uid')"
               );
        }

        [Test, TestCaseSource(nameof(IsRatingCountGreaterThanZeroTestData))]
        public async Task IsRatingCountGreaterThanZero(RatingQueryOptions options, string expectedFilter)
        {
            string hackName = "hack";
            var entities = new List<RatingEntity>
            {
                new RatingEntity{  RowKey="rk" }
            };


            var ratingTable = new Mock<IRatingTable>();
            ratingTable.Setup(p => p.QueryEntitiesAsync(expectedFilter, It.IsAny<IEnumerable<string>>(), default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.RatingTable).Returns(ratingTable.Object);

            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object
            };
            var result = await ratingManagement.IsRatingCountGreaterThanZero(hackName, options, default);

            Mock.VerifyAll(ratingTable, storageContext);
            ratingTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.IsTrue(result);
        }
        #endregion

        #region ListPaginatedRatingsAsync
        private static IEnumerable ListPaginatedRatingsAsyncTestData()
        {
            // arg0: options
            // arg1: expected top
            // arg2: expected query

            // no options
            yield return new TestCaseData(
                new RatingQueryOptions { },
                100,
                "PartitionKey eq 'hack'"
                );

            // top
            yield return new TestCaseData(
                new RatingQueryOptions { Pagination = new Pagination { top = 5 } },
                5,
                "PartitionKey eq 'hack'"
                );
            yield return new TestCaseData(
                new RatingQueryOptions { Pagination = new Pagination { top = -1 } },
                100,
                "PartitionKey eq 'hack'"
                );

            // filter by kind
            yield return new TestCaseData(
               new RatingQueryOptions { RatingKindId = "kid" },
               100,
               "(PartitionKey eq 'hack') and (RatingKindId eq 'kid')"
               );

            // filter by team
            yield return new TestCaseData(
               new RatingQueryOptions { TeamId = "tid" },
               100,
               "(PartitionKey eq 'hack') and (TeamId eq 'tid')"
               );

            // filter by judge
            yield return new TestCaseData(
               new RatingQueryOptions { JudgeId = "uid" },
               100,
               "(PartitionKey eq 'hack') and (JudgeId eq 'uid')"
               );

            // all filters
            yield return new TestCaseData(
               new RatingQueryOptions { JudgeId = "uid", TeamId = "tid", RatingKindId = "kid" },
               100,
               "(PartitionKey eq 'hack') and (JudgeId eq 'uid') and (RatingKindId eq 'kid') and (TeamId eq 'tid')"
               );
        }

        [Test, TestCaseSource(nameof(ListPaginatedRatingsAsyncTestData))]
        public async Task ListPaginatedRatingsAsync(RatingQueryOptions options, int expectedTop, string expectedFilter)
        {
            string hackName = "hack";
            var entities = MockHelper.CreatePage(
                 new List<RatingEntity>
                 {
                     new RatingEntity{  RowKey="rk" }
                 },
                 "np nr"
            );


            var ratingTable = new Mock<IRatingTable>();
            ratingTable.Setup(p => p.ExecuteQuerySegmentedAsync(
                expectedFilter, null, expectedTop, null, default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.RatingTable).Returns(ratingTable.Object);

            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object
            };
            var page = await ratingManagement.ListPaginatedRatingsAsync(hackName, options, default);

            Mock.VerifyAll(ratingTable, storageContext);
            ratingTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(1, page.Values.Count());
            Assert.AreEqual("rk", page.Values.First().Id);
            var pagination = Pagination.FromContinuationToken(page.ContinuationToken);
            Assert.AreEqual("np", pagination.np);
            Assert.AreEqual("nr", pagination.nr);
        }
        #endregion

        #region DeleteRatingAsync
        [Test]
        public async Task DeleteRatingAsync()
        {
            var ratingTable = new Mock<IRatingTable> { };
            ratingTable.Setup(t => t.DeleteAsync("hack", "rid", default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.Setup(s => s.RatingTable).Returns(ratingTable.Object);


            var ratingManagement = new RatingManagement()
            {
                StorageContext = storageContext.Object,
            };
            await ratingManagement.DeleteRatingAsync("hack", "rid", default);

            Mock.VerifyAll(ratingTable, storageContext);
            ratingTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
        }
        #endregion
    }
}
