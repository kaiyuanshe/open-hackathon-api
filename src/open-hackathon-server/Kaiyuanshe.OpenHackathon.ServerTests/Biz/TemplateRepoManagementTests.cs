using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Cache;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    internal class TemplateRepoManagementTests
    {
        private static bool AssertListEquals<T>(IEnumerable<T>? lhs, IEnumerable<T>? rhs)
        {
            Assert.AreEqual(lhs == null, rhs == null);
            if (lhs == null && rhs == null) return true;

            Assert.AreEqual(lhs.Count(), rhs.Count());

            var iterL = lhs.GetEnumerator();
            var iterR = rhs.GetEnumerator();
            while (iterL.MoveNext() && iterR.MoveNext()) {
                Assert.AreEqual(iterL.Current, iterR.Current);
            }
            return true;
        }

        private static bool ListEquals<T>(IEnumerable<T>? lhs, IEnumerable<T>? rhs)
        {
            if ((lhs == null) != (rhs == null)) return false;
            if (lhs == null && rhs == null) return true;
            if (lhs.Count() != rhs.Count()) return false;
            var iterL = lhs.GetEnumerator();
            var iterR = rhs.GetEnumerator();
            while (iterL.MoveNext() && iterR.MoveNext()) {
                if (!object.Equals(iterL.Current, iterR.Current)) return false;
            }
            return true;
        }

        private static bool DictionaryEquals<TA, TB>(IDictionary<TA, TB>? lhs, IDictionary<TA, TB>? rhs)
        {
            if ((lhs == null) != (rhs == null)) return false;
            if (lhs == null && rhs == null) return true;
            if (lhs.Count != rhs.Count) return false;
            foreach (var kvp in lhs) {
                if (rhs.TryGetValue(kvp.Key, out var vb)) {
                    if (object.Equals(kvp.Value, vb)) {
                        continue;
                    }
                }
                return false;
            }
            return true;
        }

        #region CreateTemplateRepoAsync
        [Test]
        public async Task CreateTemplateRepoAsync()
        {
            var hackathonName = "hack";
            var request = new TemplateRepo
            {
                hackathonName = hackathonName,
                url = "https://github.com/idea2app/Next-Bootstrap-ts",
            };

            var repoLanguages = new Dictionary<string, string>()
            {
                ["TypeScript"] = "13409",
                ["Less"] = "970",
                ["JavaScript"] = "558",
                ["Shell"] = "248",
            };
            var repoTopics = new List<string>()
            {
                "nextjs", "bootstrap", "typescript", "template", "scaffold"
            };

            var moqs = new Moqs();
            moqs.HttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(e =>
                    e.Method == HttpMethod.Get
                    && e.RequestUri == new Uri(string.Format("https://api.github.com/repos/idea2app/Next-Bootstrap-ts/languages"))
                    && e.Headers.GetValues("Accept").Where(s => string.Equals(s, "application/vnd.github+json")).Any()
                ),
                ItExpr.IsAny<CancellationToken>()
            ).ReturnsAsync(() => new HttpResponseMessage() {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"TypeScript\": 13409, \"Less\": 970, \"JavaScript\": 558, \"Shell\": 248}"),
            }).Verifiable();
            // moqs.HttpMessageHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(e => e == true));
            moqs.HttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(e =>
                    e.Method == HttpMethod.Get
                    && e.RequestUri == new Uri(string.Format("https://api.github.com/repos/idea2app/Next-Bootstrap-ts/topics"))
                    && e.Headers.GetValues("Accept").Where(s => string.Equals(s, "application/vnd.github+json")).Any()
                ),
                ItExpr.IsAny<CancellationToken>()
            ).ReturnsAsync(() => new HttpResponseMessage() {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"names\": [\"nextjs\", \"bootstrap\", \"typescript\", \"template\", \"scaffold\"]}"),
            }).Verifiable();
            // moqs.HttpMessageHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(e => e == true));
            moqs.TemplateRepoTable.Setup(o => o.InsertAsync(It.Is<TemplateRepoEntity>(e =>
                e.PartitionKey == hackathonName
                && e.RowKey.Length == 36
                && e.Url == request.url
                && e.IsFetched == true
                && DictionaryEquals(e.RepoLanguages, repoLanguages)
                && ListEquals(e.RepoTopics, repoTopics)
            ), default));
            moqs.CacheProvider.Setup(c => c.Remove(CacheKeys.GetCacheKey(CacheEntryType.TemplateRepo, hackathonName)));

            var management = new TemplateRepoManagement();
            moqs.SetupManagement(management);
            await management.CreateTemplateRepoAsync(request.ShallowCopy(), default);

            moqs.VerifyAll();
        }
        #endregion

        #region UpdateTemplateRepoAsync
        [Test]
        public async Task UpdateTemplateRepoAsync()
        {
            var hackathonName = "hack";
            var templateRepoId = "uuid";
            var entity = new TemplateRepoEntity()
            {
                PartitionKey = hackathonName,
                RowKey = templateRepoId,
                Url = "http://www.example.com/",
                IsFetched = false,
            };
            var request = new TemplateRepo()
            {
                hackathonName = hackathonName,
                id = templateRepoId,
                url = "https://github.com/idea2app/Next-Bootstrap-ts",
            };

            var repoLanguages = new Dictionary<string, string>()
            {
                ["TypeScript"] = "13409",
                ["Less"] = "970",
                ["JavaScript"] = "558",
                ["Shell"] = "248",
            };
            var repoTopics = new List<string>()
            {
                "nextjs", "bootstrap", "typescript", "template", "scaffold"
            };

            var moqs = new Moqs();
            moqs.HttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(e =>
                    e.Method == HttpMethod.Get
                    && e.RequestUri == new Uri(string.Format("https://api.github.com/repos/idea2app/Next-Bootstrap-ts/languages"))
                    && e.Headers.GetValues("Accept").Where(s => string.Equals(s, "application/vnd.github+json")).Any()
                ),
                ItExpr.IsAny<CancellationToken>()
            ).ReturnsAsync(() => new HttpResponseMessage() {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"TypeScript\": 13409, \"Less\": 970, \"JavaScript\": 558, \"Shell\": 248}"),
            }).Verifiable();
            // moqs.HttpMessageHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(e => e == true));
            moqs.HttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(e =>
                    e.Method == HttpMethod.Get
                    && e.RequestUri == new Uri(string.Format("https://api.github.com/repos/idea2app/Next-Bootstrap-ts/topics"))
                    && e.Headers.GetValues("Accept").Where(s => string.Equals(s, "application/vnd.github+json")).Any()
                ),
                ItExpr.IsAny<CancellationToken>()
            ).ReturnsAsync(() => new HttpResponseMessage() {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("{\"names\": [\"nextjs\", \"bootstrap\", \"typescript\", \"template\", \"scaffold\"]}"),
            }).Verifiable();
            // moqs.HttpMessageHandler.Protected().Setup("Dispose", ItExpr.Is<bool>(e => e == true));
            moqs.TemplateRepoTable.Setup(o => o.MergeAsync(It.Is<TemplateRepoEntity>(e =>
                e.PartitionKey == hackathonName
                && e.RowKey == templateRepoId
                && e.Url == request.url
                && e.IsFetched == true
                && DictionaryEquals(e.RepoLanguages, repoLanguages)
                && ListEquals(e.RepoTopics, repoTopics)
            ), default));
            moqs.CacheProvider.Setup(c => c.Remove(CacheKeys.GetCacheKey(CacheEntryType.TemplateRepo, hackathonName)));

            var management = new TemplateRepoManagement();
            moqs.SetupManagement(management);
            await management.UpdateTemplateRepoAsync(entity, request.ShallowCopy(), default);

            moqs.VerifyAll();
        }
        #endregion

        #region GetTemplateRepoAsync
        [Test]
        public async Task GetTemplateRepoAsync()
        {
            var hackathonName = "hack";
            var templateRepoId = "uuid";

            var moqs = new Moqs();
            moqs.TemplateRepoTable.Setup(o => o.RetrieveAsync(hackathonName, templateRepoId, default));

            var management = new TemplateRepoManagement();
            moqs.SetupManagement(management);
            await management.GetTemplateRepoAsync(hackathonName, templateRepoId, default);

            moqs.VerifyAll();
        }
        #endregion

        #region ListPaginatedTemplateReposAsync
        private static IEnumerable ListPaginatedTemplateReposAsyncTestData()
        {
            var a1 = new TemplateRepoEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new TemplateRepoEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new TemplateRepoEntity
            {
                RowKey = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new TemplateRepoEntity
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
                new TemplateRepoQueryOptions { },
                new List<TemplateRepoEntity> { a1, a2, a3, a4 },
                new List<TemplateRepoEntity> { a4, a2, a3, a1 },
                null
                );

            // by Team
            yield return new TestCaseData(
                new TemplateRepoQueryOptions { },
                new List<TemplateRepoEntity> { a1, a2, a3, a4 },
                new List<TemplateRepoEntity> { a4, a2, a3, a1 },
                null
                );

            // by Hackathon
            yield return new TestCaseData(
                new TemplateRepoQueryOptions { },
                new List<TemplateRepoEntity> { a1, a2, a3, a4 },
                new List<TemplateRepoEntity> { a4, a2, a3, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new TemplateRepoQueryOptions { Pagination = new Pagination { top = 2, } },
                new List<TemplateRepoEntity> { a1, a2, a3, a4 },
                new List<TemplateRepoEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new TemplateRepoQueryOptions
                {
                    Pagination = new Pagination
                    {
                        np = "1",
                        nr = "1",
                        top = 2,
                    },
                },
                new List<TemplateRepoEntity> { a1, a2, a3, a4 },
                new List<TemplateRepoEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedTemplateReposAsyncTestData))]
        public async Task ListPaginatedAssignmentsAsync(
            TemplateRepoQueryOptions options,
            IEnumerable<TemplateRepoEntity> all,
            IEnumerable<TemplateRepoEntity> expectedResult,
            Pagination expectedNext)
        {
            string hackathonName = "hack";

            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<TemplateRepoEntity>>>(c =>
                c.CacheKey == CacheKeys.GetCacheKey(CacheEntryType.TemplateRepo, hackathonName)), default)
            ).ReturnsAsync(all);

            var management = new TemplateRepoManagement()
            {
                Cache = cache.Object,
            };
            var result = await management.ListPaginatedTemplateReposAsync(hackathonName, options, default);
            AssertListEquals(expectedResult, result);

            Mock.VerifyAll(cache);
            cache.VerifyNoOtherCalls();

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

        #region DeleteTemplateRepoAsync
        [Test]
        public async Task DeleteTemplateRepoAsync()
        {
            var hackathonName = "hack";
            var templateRepoId = "uuid";

            var moqs = new Moqs();
            moqs.TemplateRepoTable.Setup(o => o.DeleteAsync(hackathonName, templateRepoId, default));
            moqs.CacheProvider.Setup(c => c.Remove(CacheKeys.GetCacheKey(CacheEntryType.TemplateRepo, hackathonName)));

            var management = new TemplateRepoManagement();
            moqs.SetupManagement(management);
            await management.DeleteTemplateRepoAsync(hackathonName, templateRepoId, default);

            moqs.VerifyAll();
        }
        #endregion
    }
}
