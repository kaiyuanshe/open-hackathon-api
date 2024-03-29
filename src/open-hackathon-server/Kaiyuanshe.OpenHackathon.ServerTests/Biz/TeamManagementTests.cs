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
    [TestFixture]
    public class TeamManagementTests
    {
        #region CreateTeamAsync
        [Test]
        public async Task CreateTeamAsync()
        {
            var request = new Team
            {
                hackathonName = "hack",
                creatorId = "uid",
                description = "desc",
                autoApprove = false,
                displayName = "dp",
            };
            TeamMemberEntity? teamMember = null;

            var moqs = new Moqs();
            moqs.TeamTable.Setup(p => p.InsertAsync(It.IsAny<TeamEntity>(), default));
            moqs.TeamMemberTable.Setup(p => p.InsertAsync(It.IsAny<TeamMemberEntity>(), default))
                .Callback<TeamMemberEntity, CancellationToken>((t, c) => { teamMember = t; });
            moqs.CacheProvider.Setup(c => c.Remove(It.IsAny<string>()));
            moqs.CacheProvider.Setup(c => c.Remove("Team-hack"));

            var teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            var result = await teamManagement.CreateTeamAsync(request, default);

            moqs.VerifyAll();
            moqs.CacheProvider.Verify(c => c.Remove("Team-hack"));
            Debug.Assert(teamMember != null);
            Assert.IsNotNull(teamMember);
            moqs.CacheProvider.Verify(c => c.Remove($"Team-{teamMember.TeamId}"));

            Assert.IsNotNull(result);
            Assert.AreEqual(false, result.AutoApprove);
            Assert.AreEqual("uid", result.CreatorId);
            Assert.AreEqual("desc", result.Description);
            Assert.AreEqual("dp", result.DisplayName);
            Assert.AreEqual("hack", result.HackathonName);
            Assert.IsNotNull(result.Id);
            Assert.AreEqual(1, result.MembersCount);

            Assert.IsNotNull(teamMember);
            Debug.Assert(teamMember != null);
            Assert.AreEqual("hack", teamMember.HackathonName);
            Assert.AreEqual("hack", teamMember.PartitionKey);
            Assert.AreEqual(TeamMemberRole.Admin, teamMember.Role);
            Assert.AreEqual(TeamMemberStatus.approved, teamMember.Status);
            Assert.AreEqual(result.Id, teamMember.TeamId);
            Assert.AreEqual("uid", teamMember.UserId);
            Assert.AreEqual(result.CreatedAt, teamMember.CreatedAt);
        }
        #endregion

        #region UpdateTeamAsync
        [Test]
        public async Task UpdateTeamAsync_Updated()
        {
            var request = new Team { description = "newdesc", autoApprove = true };
            var entity = new TeamEntity
            {
                PartitionKey = "pk",
                RowKey = "tid",
                Description = "desc",
                DisplayName = "dp",
                AutoApprove = false
            };

            var moqs = new Moqs();
            moqs.TeamTable.Setup(t => t.MergeAsync(entity, default));
            moqs.CacheProvider.Setup(c => c.Remove("Team-pk"));
            moqs.CacheProvider.Setup(c => c.Remove("Team-tid"));

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            var result = await teamManagement.UpdateTeamAsync(request, entity, default);

            moqs.VerifyAll();
            Assert.AreEqual("newdesc", result.Description);
            Assert.AreEqual("dp", result.DisplayName);
            Assert.AreEqual(true, result.AutoApprove);
        }
        #endregion

        #region UpdateTeamMembersCountAsync
        [Test]
        public async Task UpdateTeamMembersCountAsync()
        {
            string hackathonName = "hack";
            string teamId = "tid";
            var team = new TeamEntity { MembersCount = 5 };
            int count = 10;

            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.RetrieveAsync(hackathonName, teamId, default)).ReturnsAsync(team);
            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(m => m.GetMemberCountAsync(hackathonName, teamId, default)).ReturnsAsync(count);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Team-tid"));

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            await teamManagement.UpdateTeamMembersCountAsync(hackathonName, teamId, default);

            Mock.VerifyAll(storageContext, teamTable, teamMemberTable);
            teamTable.Verify(t => t.MergeAsync(It.Is<TeamEntity>(t => t.MembersCount == 10), default), Times.Once);
            teamTable.VerifyNoOtherCalls();
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
        }
        #endregion

        #region GetTeamByIdAsync
        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        [TestCase("", null)]
        [TestCase(" ", null)]
        public async Task GetTeamByIdAsync_Null(string hackName, string teamId)
        {
            TeamManagement teamManagement = new TeamManagement();
            var result = await teamManagement.GetTeamByIdAsync(hackName, teamId, default);

            Assert.IsNull(result);
        }

        [Test]
        public async Task GetTeamByIdAsync_Succeeded()
        {
            string hackName = "Hack";
            string teamId = "tid";
            TeamEntity teamEntity = new TeamEntity { Description = "desc" };

            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(t => t.RetrieveAsync("hack", "tid", default))
                .ReturnsAsync(teamEntity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object),
            };
            var result = await teamManagement.GetTeamByIdAsync(hackName, teamId, default);

            Mock.VerifyAll(storageContext, teamTable);
            Debug.Assert(result != null);
            storageContext.VerifyNoOtherCalls();
            teamTable.VerifyNoOtherCalls();
            Assert.AreEqual("desc", result.Description);
        }
        #endregion

        #region GetTeamByNameAsync
        [Test]
        public async Task GetTeamByNameAsync()
        {
            var entities = new List<TeamEntity>
            {
                new TeamEntity{  PartitionKey="pk" }
            };

            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.QueryEntitiesAsync("(PartitionKey eq 'hack') and (DisplayName eq 'tn')", null, default)).ReturnsAsync(entities);

            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object
            };
            var result = await teamManagement.GetTeamByNameAsync("hack", "tn", default);

            Mock.VerifyAll(teamTable, storageContext);
            teamTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("pk", result.First().HackathonName);
        }
        #endregion

        #region ListPaginatedTeamsAsync
        private static IEnumerable ListPaginatedTestData()
        {
            var a1 = new TeamEntity
            {
                RowKey = "a1",
                DisplayName = "a1",
                CreatedAt = DateTime.UtcNow.AddDays(1),
            };
            var a2 = new TeamEntity
            {
                RowKey = "a2",
                DisplayName = "a2",
                CreatedAt = DateTime.UtcNow.AddDays(3),
            };
            var a3 = new TeamEntity
            {
                RowKey = "b1",
                DisplayName = "b1",
                CreatedAt = DateTime.UtcNow.AddDays(2),
            };
            var a4 = new TeamEntity
            {
                RowKey = "b2",
                DisplayName = "b2",
                CreatedAt = DateTime.UtcNow.AddDays(4),
            };

            // arg0: options
            // arg1: all entities
            // arg2: expected result
            // arg3: expected Next

            // default options
            yield return new TestCaseData(
                new TeamQueryOptions { HackathonName = "hack" },
                new List<TeamEntity> { a1, a2, a3, a4 },
                new List<TeamEntity> { a4, a2, a3, a1 },
                null
                );

            // search
            yield return new TestCaseData(
                new TeamQueryOptions { HackathonName = "hack", NameSearch = "a" },
                new List<TeamEntity> { a1, a2, a3, a4 },
                new List<TeamEntity> { a2, a1 },
                null
                );

            // top
            yield return new TestCaseData(
                new TeamQueryOptions { HackathonName = "hack", Pagination = new Pagination { top = 2, } },
                new List<TeamEntity> { a1, a2, a3, a4 },
                new List<TeamEntity> { a4, a2, },
                new Pagination { np = "2", nr = "2" }
                );

            // paging
            yield return new TestCaseData(
                new TeamQueryOptions
                {
                    HackathonName = "hack",
                    Pagination = new Pagination
                    {
                        np = "1",
                        nr = "1",
                        top = 2,
                    },
                },
                new List<TeamEntity> { a1, a2, a3, a4 },
                new List<TeamEntity> { a2, a3, },
                new Pagination { np = "3", nr = "3" }
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedTestData))]
        public async Task ListPaginatedTeamsAsync(
            TeamQueryOptions options,
            IEnumerable<TeamEntity> all,
            IEnumerable<TeamEntity> expectedResult,
            Pagination expectedNext)
        {
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.GetOrAddAsync(It.Is<CacheEntry<IEnumerable<TeamEntity>>>(c => c.CacheKey == "Team-hack"), default)).ReturnsAsync(all);

            var mgmt = new TeamManagement()
            {
                Cache = cache.Object,
            };
            var result = await mgmt.ListPaginatedTeamsAsync(options, default);

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

        #region DeleteTeamAsync
        [Test]
        public async Task DeleteTeamAsync()
        {
            TeamEntity team = new TeamEntity { PartitionKey = "pk", RowKey = "rk" };

            var moqs = new Moqs();
            moqs.TeamTable.Setup(t => t.DeleteAsync("pk", "rk", default));
            moqs.CacheProvider.Setup(c => c.Remove("Team-pk"));
            moqs.CacheProvider.Setup(c => c.Remove("Team-rk"));

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            await teamManagement.DeleteTeamAsync(team, default);

            moqs.VerifyAll();
        }
        #endregion

        #region CreateTeamMemberAsync
        [Test]
        public async Task CreateTeamMemberAsync()
        {
            TeamMember request = new TeamMember { hackathonName = "hack", teamId = "tid" };
            TeamEntity team = new TeamEntity { };
            int count = 5;


            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.RetrieveAsync("hack", "tid", default)).ReturnsAsync(team);
            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(t => t.InsertOrMergeAsync(It.IsAny<TeamMemberEntity>(), default));
            teamMemberTable.Setup(m => m.GetMemberCountAsync("hack", "tid", default)).ReturnsAsync(count);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Team-tid"));

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await teamManagement.CreateTeamMemberAsync(request, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamTable.Verify(t => t.MergeAsync(It.Is<TeamEntity>(t => t.MembersCount == 5), default), Times.Once);
            teamTable.VerifyNoOtherCalls();
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.AreEqual("hack", result.HackathonName);
            Assert.AreEqual(TeamMemberRole.Member, result.Role);
        }
        #endregion

        #region UpdateTeamMemberAsync
        [Test]
        public async Task UpdateTeamMemberAsync()
        {
            TeamMember request = new TeamMember { description = "b" };
            TeamMemberEntity member = new TeamMemberEntity { Description = "a", Role = TeamMemberRole.Member, Status = TeamMemberStatus.pendingApproval };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(t => t.MergeAsync(It.Is<TeamMemberEntity>(m => m.Description == "b"), default));

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            var result = await teamManagement.UpdateTeamMemberAsync(member, request, default);

            moqs.VerifyAll();
            Assert.AreEqual("b", result.Description);
            Assert.AreEqual(TeamMemberRole.Member, result.Role);
            Assert.AreEqual(TeamMemberStatus.pendingApproval, result.Status);
        }
        #endregion

        #region GetTeamMemberAsync
        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase("", " ")]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        public async Task GetTeamMember_Null(string hackName, string userId)
        {

            TeamManagement teamManagement = new TeamManagement()
            {
            };
            var result = await teamManagement.GetTeamMemberAsync(hackName, userId, default);
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetTeamMember_Suecceded()
        {
            string userId = "uid";
            TeamMemberEntity teamMember = new TeamMemberEntity { Description = "desc" };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(t => t.RetrieveAsync("hack", "uid", default)).ReturnsAsync(teamMember);

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            var result = await teamManagement.GetTeamMemberAsync("hack", userId, default);

            moqs.VerifyAll();
            Assert.IsNotNull(result);
            Debug.Assert(result != null);
            Assert.AreEqual("desc", result.Description);
        }
        #endregion

        #region UpdateTeamMemberStatusAsync
        [TestCase(TeamMemberStatus.approved)]
        [TestCase(TeamMemberStatus.pendingApproval)]
        public async Task UpdateTeamMemberStatusAsync_Skip(TeamMemberStatus status)
        {
            TeamMemberEntity teamMember = new TeamMemberEntity { Status = status };

            var teamMemberTable = new Mock<ITeamMemberTable>();
            var storageContext = new Mock<IStorageContext>();

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.UpdateTeamMemberStatusAsync(teamMember, status, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
        }

        [TestCase(TeamMemberStatus.pendingApproval, TeamMemberStatus.approved)]
        [TestCase(TeamMemberStatus.approved, TeamMemberStatus.pendingApproval)]
        public async Task UpdateTeamMemberStatusAsync_Succeeded(TeamMemberStatus oldStatus, TeamMemberStatus newStatus)
        {
            TeamMemberEntity teamMember = new TeamMemberEntity { Status = oldStatus };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(t => t.MergeAsync(It.Is<TeamMemberEntity>(
                m => m.Description == null && m.Status == newStatus), default));

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            var result = await teamManagement.UpdateTeamMemberStatusAsync(teamMember, newStatus, default);

            moqs.VerifyAll();
            Assert.IsNull(result.Description);
            Assert.AreEqual(newStatus, result.Status);
        }
        #endregion

        #region UpdateTeamMemberRoleAsync
        [TestCase(TeamMemberRole.Member)]
        [TestCase(TeamMemberRole.Admin)]
        public async Task UpdateTeamMemberRoleAsync_Skip(TeamMemberRole role)
        {
            TeamMemberEntity teamMember = new TeamMemberEntity { Role = role };

            var teamMemberTable = new Mock<ITeamMemberTable>();
            var storageContext = new Mock<IStorageContext>();

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.UpdateTeamMemberRoleAsync(teamMember, role, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
        }

        [TestCase(TeamMemberRole.Member, TeamMemberRole.Admin)]
        [TestCase(TeamMemberRole.Admin, TeamMemberRole.Member)]
        public async Task UpdateTeamMemberRoleAsync_Succeeded(TeamMemberRole oldRole, TeamMemberRole newRole)
        {
            TeamMemberEntity teamMember = new TeamMemberEntity { Role = oldRole };

            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(t => t.MergeAsync(It.Is<TeamMemberEntity>(
                m => m.Description == null && m.Role == newRole), default));

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            var result = await teamManagement.UpdateTeamMemberRoleAsync(teamMember, newRole, default);

            moqs.VerifyAll();
            Assert.IsNull(result.Description);
            Assert.AreEqual(newRole, result.Role);
        }
        #endregion

        #region DeleteTeamMemberAsync
        [Test]
        public async Task DeleteTeamMemberAsync()
        {
            TeamMemberEntity teamMember = new TeamMemberEntity { PartitionKey = "hack", RowKey = "uid", TeamId = "tid" };
            TeamEntity team = new TeamEntity { };
            int count = 5;

            var moqs = new Moqs();
            moqs.TeamTable.Setup(p => p.RetrieveAsync("hack", "tid", default)).ReturnsAsync(team);
            moqs.TeamTable.Setup(p => p.MergeAsync(team, default));
            moqs.TeamMemberTable.Setup(t => t.DeleteAsync("hack", "uid", default));
            moqs.TeamMemberTable.Setup(m => m.GetMemberCountAsync("hack", "tid", default)).ReturnsAsync(count);
            moqs.CacheProvider.Setup(c => c.Remove("Team-hack"));
            moqs.CacheProvider.Setup(c => c.Remove("Team-tid"));

            TeamManagement teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            await teamManagement.DeleteTeamMemberAsync(teamMember, default);

            moqs.VerifyAll();
        }
        #endregion

        #region ListTeamMembersAsync
        [Test]
        public async Task ListTeamMembersAsync()
        {
            List<TeamMemberEntity> teamMembers = new List<TeamMemberEntity>
            {
                new TeamMemberEntity
                {
                    RowKey = "rk1",
                    Role = TeamMemberRole.Admin,
                    Status = TeamMemberStatus.approved
                },
                new TeamMemberEntity
                {
                    RowKey = "rk2",
                    Role = TeamMemberRole.Member,
                    Status = TeamMemberStatus.pendingApproval
                },
            };


            var moqs = new Moqs();
            moqs.TeamMemberTable.Setup(p => p.QueryEntitiesAsync("(PartitionKey eq 'hack') and (TeamId eq 'tid')", null, default))
                .ReturnsAsync(teamMembers);

            var teamManagement = new TeamManagement();
            moqs.SetupManagement(teamManagement);
            teamManagement.Cache = new DefaultCacheProvider(new Mock<ILogger<DefaultCacheProvider>>().Object);
            var results = await teamManagement.ListTeamMembersAsync("hack", "tid", default);

            moqs.VerifyAll();
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("rk1", results.First().UserId);
            Assert.AreEqual(TeamMemberRole.Admin, results.First().Role);
            Assert.AreEqual(TeamMemberStatus.approved, results.First().Status);
            Assert.AreEqual("rk2", results.Last().UserId);
            Assert.AreEqual(TeamMemberRole.Member, results.Last().Role);
            Assert.AreEqual(TeamMemberStatus.pendingApproval, results.Last().Status);
        }
        #endregion

        #region ListPaginatedTeamMembersAsync

        private static IEnumerable ListPaginatedTeamMembersAsyncTestData()
        {
            // arg0: options
            // arg1: expected top
            // arg2: expected query

            // no options
            yield return new TestCaseData(
                new TeamMemberQueryOptions(),
                100,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid')"
                );

            // top
            yield return new TestCaseData(
                new TeamMemberQueryOptions { Pagination = new Pagination { top = 5 } },
                5,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid')"
                );
            yield return new TestCaseData(
                new TeamMemberQueryOptions { Pagination = new Pagination { top = -1 } },
                100,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid')"
                );

            // status
            yield return new TestCaseData(
                new TeamMemberQueryOptions { Status = TeamMemberStatus.approved },
                100,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid') and (Status eq 1)"
                );
            yield return new TestCaseData(
                new TeamMemberQueryOptions { Status = TeamMemberStatus.pendingApproval },
                100,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid') and (Status eq 0)"
                );

            // Role
            yield return new TestCaseData(
                new TeamMemberQueryOptions { Role = TeamMemberRole.Admin },
                100,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid') and (Role eq 0)"
                );
            yield return new TestCaseData(
                new TeamMemberQueryOptions { Role = TeamMemberRole.Member },
                100,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid') and (Role eq 1)"
                );

            // all options
            yield return new TestCaseData(
                new TeamMemberQueryOptions
                {
                    Pagination = new Pagination { top = 20 },
                    Role = TeamMemberRole.Member,
                    Status = TeamMemberStatus.approved,
                },
                20,
                "(PartitionKey eq 'hack') and (TeamId eq 'tid') and (Status eq 1) and (Role eq 1)"
                );
        }

        [Test, TestCaseSource(nameof(ListPaginatedTeamMembersAsyncTestData))]
        public async Task ListPaginatedTeamMembersAsync_Options(TeamMemberQueryOptions options, int expectedTop, string expectedFilter)
        {
            string hackName = "hack";
            var entities = MockHelper.CreatePage<TeamMemberEntity>(
                 new List<TeamMemberEntity>
                 {
                     new TeamMemberEntity{  TeamId="tid" }
                 },
                 "np nr"
                );


            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(p => p.ExecuteQuerySegmentedAsync(expectedFilter, null, expectedTop, null, default))
                .ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object
            };
            var page = await teamManagement.ListPaginatedTeamMembersAsync(hackName, "tid", options, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(1, page.Values.Count());
            Assert.AreEqual("tid", page.Values.First().TeamId);
            var paging = Pagination.FromContinuationToken(page.ContinuationToken);
            Assert.AreEqual("np", paging.np);
            Assert.AreEqual("nr", paging.nr);
        }

        #endregion
    }
}