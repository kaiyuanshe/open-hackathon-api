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

            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.InsertAsync(It.IsAny<TeamEntity>(), default));
            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(p => p.InsertAsync(It.IsAny<TeamMemberEntity>(), default))
                .Callback<TeamMemberEntity, CancellationToken>((t, c) => { teamMember = t; });
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.CreateTeamAsync(request, default);

            Mock.VerifyAll(teamTable, teamMemberTable, storageContext);
            Assert.IsNotNull(result);
            Assert.AreEqual(false, result.AutoApprove);
            Assert.AreEqual("uid", result.CreatorId);
            Assert.AreEqual("desc", result.Description);
            Assert.AreEqual("dp", result.DisplayName);
            Assert.AreEqual("hack", result.HackathonName);
            Assert.IsNotNull(result.Id);
            Assert.AreEqual(1, result.MembersCount);

            Assert.IsNotNull(teamMember);
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
                RowKey = "tid",
                Description = "desc",
                DisplayName = "dp",
                AutoApprove = false
            };


            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(t => t.MergeAsync(entity, default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Team-tid"));

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            var result = await teamManagement.UpdateTeamAsync(request, entity, default);

            Mock.VerifyAll(storageContext, teamTable, cache);

            storageContext.VerifyNoOtherCalls();
            teamTable.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();

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
                Cache = new DefaultCacheProvider(null),
            };
            var result = await teamManagement.GetTeamByIdAsync(hackName, teamId, default);

            Mock.VerifyAll(storageContext, teamTable);

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
        [Test]
        public async Task ListPaginatedTeamsAsync_NoOptions()
        {
            string hackName = "foo";
            TeamQueryOptions options = null;
            var entities = MockHelper.CreatePage<TeamEntity>(
                 new List<TeamEntity>
                 {
                     new TeamEntity{  PartitionKey="pk" }
                 }, "np nr"
                );


            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.ExecuteQuerySegmentedAsync("PartitionKey eq 'foo'", null, 100, null, default)).ReturnsAsync(entities);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object
            };
            var page = await teamManagement.ListPaginatedTeamsAsync(hackName, options, default);

            Mock.VerifyAll(teamTable, storageContext);
            teamTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(1, page.Values.Count());
            Assert.AreEqual("pk", page.Values.First().HackathonName);
            var pagination = Pagination.FromContinuationToken(page.ContinuationToken);
            Assert.AreEqual("np", pagination.np);
            Assert.AreEqual("nr", pagination.nr);
        }

        [TestCase(5, 5)]
        [TestCase(-1, 100)]
        public async Task ListPaginatedTeamsAsync_Options(int topInPara, int expectedTop)
        {
            string hackName = "foo";
            TeamQueryOptions options = new TeamQueryOptions
            {
                Pagination = new Pagination { nr = "nr", np = "np", top = topInPara },
            };
            var entities = MockHelper.CreatePage<TeamEntity>(
                 new List<TeamEntity>
                 {
                     new TeamEntity{  PartitionKey="pk" }
                 }, "np2 nr2"
                );


            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.ExecuteQuerySegmentedAsync("PartitionKey eq 'foo'", "np nr", expectedTop, null, default)).ReturnsAsync(entities);

            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object
            };
            var page = await teamManagement.ListPaginatedTeamsAsync(hackName, options, default);

            Mock.VerifyAll(teamTable, storageContext);
            teamTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();

            Assert.AreEqual(1, page.Values.Count());
            Assert.AreEqual("pk", page.Values.First().HackathonName);
            var pagination = Pagination.FromContinuationToken(page.ContinuationToken);
            Assert.AreEqual("np2", pagination.np);
            Assert.AreEqual("nr2", pagination.nr);
        }
        #endregion

        #region DeleteTeamAsync
        [Test]
        public async Task DeleteTeamAsync()
        {
            TeamEntity team = new TeamEntity { PartitionKey = "pk", RowKey = "rk" };

            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(t => t.DeleteAsync("pk", "rk", default));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Team-rk"));

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            await teamManagement.DeleteTeamAsync(team, default);

            Mock.VerifyAll(teamTable, storageContext, cache);
            teamTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();
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
            TeamMemberEntity captured = null;

            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(t => t.MergeAsync(It.IsAny<TeamMemberEntity>(), default))
                .Callback<TeamMemberEntity, CancellationToken>((tm, c) => { captured = tm; });
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.UpdateTeamMemberAsync(member, request, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.AreEqual("b", result.Description);
            Assert.AreEqual("b", captured.Description);
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

            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(t => t.RetrieveAsync("hack", "uid", default))
                .ReturnsAsync(teamMember);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.GetTeamMemberAsync("hack", userId, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.IsNotNull(result);
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
            TeamMemberEntity captured = null;

            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(t => t.MergeAsync(It.IsAny<TeamMemberEntity>(), default))
                .Callback<TeamMemberEntity, CancellationToken>((tm, c) => { captured = tm; });
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.UpdateTeamMemberStatusAsync(teamMember, newStatus, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.IsNull(result.Description);
            Assert.IsNull(captured.Description);
            Assert.AreEqual(newStatus, result.Status);
            Assert.AreEqual(newStatus, captured.Status);
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
            TeamMemberEntity captured = null;

            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(t => t.MergeAsync(It.IsAny<TeamMemberEntity>(), default))
                .Callback<TeamMemberEntity, CancellationToken>((tm, c) => { captured = tm; });
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);


            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.UpdateTeamMemberRoleAsync(teamMember, newRole, default);

            Mock.VerifyAll(teamMemberTable, storageContext);
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            Assert.IsNull(result.Description);
            Assert.IsNull(captured.Description);
            Assert.AreEqual(newRole, result.Role);
            Assert.AreEqual(newRole, captured.Role);
        }
        #endregion

        #region DeleteTeamMemberAsync
        [Test]
        public async Task DeleteTeamMemberAsync()
        {
            TeamMemberEntity teamMember = new TeamMemberEntity { PartitionKey = "hack", RowKey = "uid", TeamId = "tid" };
            TeamEntity team = new TeamEntity { };
            int count = 5;


            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.RetrieveAsync("hack", "tid", default)).ReturnsAsync(team);
            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(t => t.DeleteAsync("hack", "uid", default));
            teamMemberTable.Setup(m => m.GetMemberCountAsync("hack", "tid", default)).ReturnsAsync(count);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            var cache = new Mock<ICacheProvider>();
            cache.Setup(c => c.Remove("Team-tid"));

            TeamManagement teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache.Object,
            };
            await teamManagement.DeleteTeamMemberAsync(teamMember, default);

            Mock.VerifyAll(storageContext, teamTable, teamMemberTable, cache);
            teamTable.Verify(t => t.MergeAsync(It.Is<TeamEntity>(t => t.MembersCount == 5), default), Times.Once);
            teamTable.VerifyNoOtherCalls();
            teamMemberTable.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            cache.VerifyNoOtherCalls();
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


            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(p => p.QueryEntitiesAsync("(PartitionKey eq 'hack') and (TeamId eq 'tid')", null, default))
                .ReturnsAsync(teamMembers);

            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);
            var cache = new DefaultCacheProvider(null);

            var teamManagement = new TeamManagement()
            {
                StorageContext = storageContext.Object,
                Cache = cache,
            };
            var results = await teamManagement.ListTeamMembersAsync("hack", "tid", default);

            Mock.VerifyAll(teamMemberTable, storageContext);
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