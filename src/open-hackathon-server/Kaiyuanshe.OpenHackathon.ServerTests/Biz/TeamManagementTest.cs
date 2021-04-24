﻿using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Kaiyuanshe.OpenHackathon.Server.Storage.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Biz
{
    [TestFixture]
    public class TeamManagementTest
    {
        [TestCase(null, "uid")]
        [TestCase("", "uid")]
        [TestCase(" ", "uid")]
        [TestCase("foo", null)]
        [TestCase("foo", "")]
        [TestCase("foo", " ")]
        public async Task CreateTeamAsync_Null(string hackathonName, string userId)
        {
            var request = new Team { hackathonName = hackathonName, creatorId = userId };
            var cancellationToken = CancellationToken.None;
            var logger = new Mock<ILogger<TeamManagement>>();
            var teamManagement = new TeamManagement(logger.Object);
            Assert.IsNull(await teamManagement.CreateTeamAsync(request, cancellationToken));
            Mock.VerifyAll(logger);
        }

        [Test]
        public async Task CreateTeamAsync_Succeed()
        {
            var request = new Team
            {
                hackathonName = "hack",
                creatorId = "uid",
                description = "desc",
                autoApprove = false,
                displayName = "dp",
            };
            var cancellationToken = CancellationToken.None;
            TeamMemberEntity teamMember = null;

            var logger = new Mock<ILogger<TeamManagement>>();
            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(p => p.InsertAsync(It.IsAny<TeamEntity>(), cancellationToken));
            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(p => p.InsertAsync(It.IsAny<TeamMemberEntity>(), cancellationToken))
                .Callback<TeamMemberEntity, CancellationToken>((t, c) => { teamMember = t; });
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            var teamManagement = new TeamManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.CreateTeamAsync(request, cancellationToken);

            Mock.VerifyAll(logger, teamTable, teamMemberTable, storageContext);
            Assert.IsNotNull(result);
            Assert.AreEqual(false, result.AutoApprove);
            Assert.AreEqual("uid", result.CreatorId);
            Assert.AreEqual("desc", result.Description);
            Assert.AreEqual("dp", result.DisplayName);
            Assert.AreEqual("hack", result.HackathonName);
            Assert.IsNotNull(result.Id);

            Assert.IsNotNull(teamMember);
            Assert.AreEqual("hack", teamMember.HackathonName);
            Assert.AreEqual(result.Id, teamMember.PartitionKey);
            Assert.AreEqual(TeamMemberRole.Admin, teamMember.Role);
            Assert.AreEqual(TeamMemberStatus.approved, teamMember.Status);
            Assert.AreEqual(result.Id, teamMember.TeamId);
            Assert.AreEqual("uid", teamMember.UserId);
            Assert.AreEqual(result.CreatedAt, teamMember.CreatedAt);
        }

        [Test]
        public async Task ListTeamMembersAsync()
        {
            var cancellationToken = CancellationToken.None;
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

            string query = null;
            TableContinuationToken continuationToken = null;
            var logger = new Mock<ILogger<TeamManagement>>();
            var teamMemberTable = new Mock<ITeamMemberTable>();
            teamMemberTable.Setup(p => p.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<TeamMemberEntity>>(), It.IsAny<TableContinuationToken>(), cancellationToken))
                .Callback<TableQuery<TeamMemberEntity>, TableContinuationToken, CancellationToken>((q, t, c) =>
                {
                    query = q.FilterString;
                    continuationToken = t;
                })
                .ReturnsAsync(MockHelper.CreateTableQuerySegment(teamMembers, null));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamMemberTable).Returns(teamMemberTable.Object);

            var teamManagement = new TeamManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };
            var results = await teamManagement.ListTeamMembersAsync("tid", cancellationToken);

            Mock.VerifyAll(logger, teamMemberTable, storageContext);
            Assert.AreEqual("PartitionKey eq 'tid'", query);
            Assert.IsNull(continuationToken);
            Assert.AreEqual(2, results.Count());
            Assert.AreEqual("rk1", results.First().UserId);
            Assert.AreEqual(TeamMemberRole.Admin, results.First().Role);
            Assert.AreEqual(TeamMemberStatus.approved, results.First().Status);
            Assert.AreEqual("rk2", results.Last().UserId);
            Assert.AreEqual(TeamMemberRole.Member, results.Last().Role);
            Assert.AreEqual(TeamMemberStatus.pendingApproval, results.Last().Status);
        }

        [Test]
        public async Task UpdateTeamAsync_Null()
        {
            var cancellationToken = CancellationToken.None;
            var logger = new Mock<ILogger<TeamManagement>>();
            var storageContext = new Mock<IStorageContext>();

            TeamManagement teamManagement = new TeamManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };
            Assert.IsNull(await teamManagement.UpdateTeamAsync(null, null, cancellationToken));
            Assert.IsNull(await teamManagement.UpdateTeamAsync(new Team(), null, cancellationToken));

            TeamEntity entity = new TeamEntity();
            Assert.AreEqual(entity, await teamManagement.UpdateTeamAsync(null, entity, cancellationToken));
            Mock.VerifyAll(logger, storageContext);
            logger.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
        }

        [Test]
        public async Task UpdateTeamAsync_Updated()
        {
            var request = new Team { description = "newdesc", autoApprove = true };
            var entity = new TeamEntity { Description = "desc", DisplayName = "dp", AutoApprove = false };

            var cancellationToken = CancellationToken.None;
            var logger = new Mock<ILogger<TeamManagement>>();
            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(t => t.MergeAsync(entity, cancellationToken));
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);

            TeamManagement teamManagement = new TeamManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.UpdateTeamAsync(request, entity, cancellationToken);

            Mock.VerifyAll(logger, storageContext, teamTable);
            logger.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            teamTable.VerifyNoOtherCalls();
            Assert.AreEqual("newdesc", result.Description);
            Assert.AreEqual("dp", result.DisplayName);
            Assert.AreEqual(true, result.AutoApprove);
        }

        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        [TestCase("", null)]
        [TestCase(" ", null)]
        public async Task GetTeamByIdAsync_Null(string hackName, string teamId)
        {
            var cancellationToken = CancellationToken.None;
            var logger = new Mock<ILogger<TeamManagement>>();
            TeamManagement teamManagement = new TeamManagement(logger.Object);
            var result = await teamManagement.GetTeamByIdAsync(hackName, teamId, cancellationToken);

            Mock.VerifyAll(logger);
            logger.VerifyNoOtherCalls();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetTeamByIdAsync_Succeeded()
        {
            string hackName = "Hack";
            string teamId = "tid";
            var cancellationToken = CancellationToken.None;
            TeamEntity teamEntity = new TeamEntity { Description = "desc" };

            var logger = new Mock<ILogger<TeamManagement>>();
            var teamTable = new Mock<ITeamTable>();
            teamTable.Setup(t => t.RetrieveAsync("hack", "tid", cancellationToken))
                .ReturnsAsync(teamEntity);
            var storageContext = new Mock<IStorageContext>();
            storageContext.SetupGet(p => p.TeamTable).Returns(teamTable.Object);

            TeamManagement teamManagement = new TeamManagement(logger.Object)
            {
                StorageContext = storageContext.Object,
            };
            var result = await teamManagement.GetTeamByIdAsync(hackName, teamId, cancellationToken);

            Mock.VerifyAll(logger, storageContext, teamTable);
            logger.VerifyNoOtherCalls();
            storageContext.VerifyNoOtherCalls();
            teamTable.VerifyNoOtherCalls();
            Assert.AreEqual("desc", result.Description);
        }
    }
}
