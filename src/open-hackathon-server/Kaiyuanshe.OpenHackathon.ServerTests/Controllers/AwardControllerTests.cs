using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.ResponseBuilder;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Moq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    public class AwardControllerTests
    {
        #region CreateAward
        [Test]
        public async Task CreateAward_TooMany()
        {
            // input
            string hackName = "Hack";
            HackathonEntity hackathon = new HackathonEntity { };
            Award parameter = new Award { };
            var authResult = AuthorizationResult.Success();

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.CanCreateNewAward("hack", default)).ReturnsAsync(false);

            // run
            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAward(hackName, parameter, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Award_TooMany);
        }

        [Test]
        public async Task CreateAward_Succeeded()
        {
            // input
            string hackName = "Hack";
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "foo" };
            Award parameter = new Award { };
            AwardEntity awardEntity = new AwardEntity
            {
                Name = "n",
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                Quantity = 5,
                Target = AwardTarget.individual,
                Pictures = new PictureInfo[]
                {
                    new PictureInfo{ name="p1", description="d1", uri="u1" },
                }
            };
            var authResult = AuthorizationResult.Success();

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.CanCreateNewAward("hack", default)).ReturnsAsync(true);
            moqs.AwardManagement.Setup(t => t.CreateAwardAsync("hack", parameter, default)).ReturnsAsync(awardEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.createAward, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.createAward, It.IsAny<object>(), null, default));

            // run
            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAward(hackName, parameter, default);

            // verify
            moqs.VerifyAll();

            Award resp = AssertHelper.AssertOKResult<Award>(result);
            Assert.AreEqual("desc", resp.description);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("rk", resp.id);
            Assert.AreEqual("n", resp.name);
            Assert.AreEqual(5, resp.quantity);
            Assert.AreEqual(AwardTarget.individual, resp.target);
            Assert.AreEqual(1, resp.pictures.Length);
            Assert.AreEqual("d1", resp.pictures[0].description);
            Assert.AreEqual("u1", resp.pictures[0].uri);
            Assert.AreEqual("p1", resp.pictures[0].name);
        }
        #endregion

        #region GetAward
        [Test]
        public async Task GetAward_AwardNotFound()
        {
            // input
            string hackName = "hack";
            HackathonEntity hackathon = new HackathonEntity { };
            string awardId = "aid";
            AwardEntity awardEntity = null;
            CancellationToken cancellationToken = CancellationToken.None;

            // moq
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", cancellationToken))
                .ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", awardId, cancellationToken))
                .ReturnsAsync(awardEntity);

            // run
            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.GetAward(hackName, awardId, cancellationToken);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, Resources.Award_NotFound);
        }

        [Test]
        public async Task GetAward_Succeeded()
        {
            // input
            string hackName = "hack";
            HackathonEntity hackathon = new HackathonEntity { };
            string awardId = "aid";
            AwardEntity awardEntity = new AwardEntity
            {
                Name = "n",
                PartitionKey = "pk",
                RowKey = "rk",
                Description = "desc",
                Quantity = 5,
                Target = AwardTarget.individual,
                Pictures = new PictureInfo[]
                {
                    new PictureInfo{ name="p1", description="d1", uri="u1" },
                }
            };
            CancellationToken cancellationToken = CancellationToken.None;

            // moq
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", cancellationToken))
                .ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", awardId, cancellationToken))
                .ReturnsAsync(awardEntity);

            // run
            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.GetAward(hackName, awardId, cancellationToken);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();

            Award resp = AssertHelper.AssertOKResult<Award>(result);
            Assert.AreEqual("desc", resp.description);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("rk", resp.id);
            Assert.AreEqual("n", resp.name);
            Assert.AreEqual(5, resp.quantity);
            Assert.AreEqual(AwardTarget.individual, resp.target);
            Assert.AreEqual(1, resp.pictures.Length);
            Assert.AreEqual("d1", resp.pictures[0].description);
            Assert.AreEqual("u1", resp.pictures[0].uri);
            Assert.AreEqual("p1", resp.pictures[0].name);
        }
        #endregion

        #region UpdateAward
        [TestCase(AwardTarget.individual, AwardTarget.individual, 0, true)]
        [TestCase(AwardTarget.individual, AwardTarget.individual, 1, true)]
        [TestCase(AwardTarget.individual, AwardTarget.team, 0, true)]
        [TestCase(AwardTarget.individual, AwardTarget.team, 1, false)]
        public async Task UpdateAward(AwardTarget exiting, AwardTarget update, int assignmentCount, bool expectedSuccess)
        {
            // input
            string hackName = "hack";
            string awardId = "aid";
            HackathonEntity hackathon = new HackathonEntity { };
            Award parameter = new Award
            {
                target = update,
            };
            AwardEntity awardEntity = new AwardEntity
            {
                Name = "n",
                PartitionKey = "hack",
                RowKey = "rk",
                Description = "desc",
                Quantity = 5,
                Target = exiting,
                Pictures = new PictureInfo[]
                {
                    new PictureInfo{ name="p1", description="d1", uri="u1" },
                }
            };
            var authResult = AuthorizationResult.Success();

            // moq
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default))
                .ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", awardId, default))
                .ReturnsAsync(awardEntity);
            var activityLogManagement = new Mock<IActivityLogManagement>();
            if (expectedSuccess)
            {
                awardManagement.Setup(t => t.UpdateAwardAsync(awardEntity, parameter, default))
                    .ReturnsAsync(awardEntity);
                activityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                    && a.ActivityLogType == ActivityLogType.updateAward.ToString()
                    && a.Message == "n"), default));
            }
            if (exiting != update)
            {
                awardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "rk", default))
                    .ReturnsAsync(assignmentCount);
            }
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            // run
            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
                ResponseBuilder = new DefaultResponseBuilder(),
                AuthorizationService = authorizationService.Object,
                ActivityLogManagement = activityLogManagement.Object,
            };
            var result = await controller.UpdateAward(hackName, awardId, parameter, default);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement, authorizationService, activityLogManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            activityLogManagement.VerifyNoOtherCalls();

            if (!expectedSuccess)
            {
                AssertHelper.AssertObjectResult(result, 412, Resources.Award_CannotUpdateTarget);
            }
            else
            {
                Award resp = AssertHelper.AssertOKResult<Award>(result);
                Assert.AreEqual("desc", resp.description);
                Assert.AreEqual("hack", resp.hackathonName);
                Assert.AreEqual("rk", resp.id);
                Assert.AreEqual("n", resp.name);
                Assert.AreEqual(5, resp.quantity);
                Assert.AreEqual(AwardTarget.individual, resp.target);
                Assert.AreEqual(1, resp.pictures.Length);
                Assert.AreEqual("d1", resp.pictures[0].description);
                Assert.AreEqual("u1", resp.pictures[0].uri);
                Assert.AreEqual("p1", resp.pictures[0].name);
            }
        }
        #endregion

        #region ListAwards
        private static IEnumerable ListAwardsTestData()
        {
            // arg0: pagination
            // arg1: next TableCotinuationToken
            // arg2: expected nextlink

            // no pagination, no filter, no top
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    null,
                    null
                );

            // next link
            yield return new TestCaseData(
                    new Pagination { },
                    new Pagination { np = "np", nr = "nr" },
                    "&np=np&nr=nr"
                );

            // next link with top
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    new Pagination { np = "np2", nr = "nr2" },
                    "&top=10&np=np2&nr=nr2"
                );
        }

        [Test, TestCaseSource(nameof(ListAwardsTestData))]
        public async Task ListAwards(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            var hackName = "Hack";
            var cancellationToken = CancellationToken.None;
            HackathonEntity hackathonEntity = new HackathonEntity();
            var awards = new List<AwardEntity>
            {
                new AwardEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                }
            };

            // mock and capture
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", It.IsAny<CancellationToken>()))
                .ReturnsAsync(hackathonEntity);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(p => p.ListPaginatedAwardsAsync("hack", It.IsAny<AwardQueryOptions>(), cancellationToken))
                .Callback<string, AwardQueryOptions, CancellationToken>((n, o, t) =>
                {
                    o.NextPage = next;
                })
                .ReturnsAsync(awards);

            // run
            var controller = new AwardController
            {
                ResponseBuilder = new DefaultResponseBuilder(),
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
            };
            var result = await controller.ListAwards(hackName, pagination, cancellationToken);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();

            var list = AssertHelper.AssertOKResult<AwardList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("rk", list.value[0].id);
        }
        #endregion

        #region DeleteAward
        [Test]
        public async Task DeleteAward_Assigned()
        {
            // input
            HackathonEntity hackathon = new HackathonEntity { };
            AwardEntity awardEntity = new AwardEntity { RowKey = "rk" };
            CancellationToken cancellationToken = CancellationToken.None;
            var authResult = AuthorizationResult.Success();

            // moq
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", cancellationToken))
                .ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", "aid", cancellationToken))
                .ReturnsAsync(awardEntity);
            awardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "rk", default)).ReturnsAsync(1);

            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            // run
            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                AuthorizationService = authorizationService.Object,
                ProblemDetailsFactory = new CustomProblemDetailsFactory(),
            };
            var result = await controller.DeleteAward("hack", "aid", cancellationToken);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement, authorizationService);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 412, Resources.Award_CannotDeleteAssigned);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteAward(bool firstTimeDelete)
        {
            // input
            HackathonEntity hackathon = new HackathonEntity { };
            AwardEntity awardEntity = firstTimeDelete ? new AwardEntity { RowKey = "rk" } : null;
            CancellationToken cancellationToken = CancellationToken.None;
            var authResult = AuthorizationResult.Success();

            // moq
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", cancellationToken))
                .ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", "aid", cancellationToken))
                .ReturnsAsync(awardEntity);
            var activityLogManagement = new Mock<IActivityLogManagement>();
            if (firstTimeDelete)
            {
                awardManagement.Setup(t => t.DeleteAwardAsync(awardEntity, cancellationToken));
                awardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "rk", default)).ReturnsAsync(0);
                activityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                    && a.ActivityLogType == ActivityLogType.deleteAward.ToString()), default));
            }
            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);


            // run
            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                AuthorizationService = authorizationService.Object,
                ActivityLogManagement = activityLogManagement.Object,
            };
            var result = await controller.DeleteAward("hack", "aid", cancellationToken);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement, authorizationService, activityLogManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            activityLogManagement.VerifyNoOtherCalls();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion

        #region CreateAwardAssignment
        [Test]
        public async Task CreateAwardAssignment_ExceedQuantity()
        {
            var hackathon = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity { Quantity = 5, PartitionKey = "hack", RowKey = "award" };
            var parameter = new AwardAssignment { };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            moqs.AwardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "award", default)).ReturnsAsync(5);

            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAwardAssignment("Hack", "award", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, string.Format(Resources.Award_TooManyAssignments, 5));
        }

        [Test]
        public async Task CreateAwardAssignment_AssigneeTeamNotFound()
        {
            var hackathon = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity
            {
                Quantity = 10,
                PartitionKey = "hack",
                RowKey = "award",
                Target = AwardTarget.team,
            };
            var parameter = new AwardAssignment { assigneeId = "teamId" };
            TeamEntity team = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            moqs.AwardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "award", default)).ReturnsAsync(5);
            moqs.TeamManagement.Setup(t => t.GetTeamByIdAsync("hack", "teamId", default)).ReturnsAsync(team);

            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAwardAssignment("Hack", "award", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Team_NotFound);
        }

        [TestCase(null)]
        [TestCase(EnrollmentStatus.pendingApproval)]
        public async Task CreateAwardAssignment_AssigneeUserNotFound(EnrollmentStatus? status)
        {
            var hackathon = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity
            {
                Quantity = 10,
                PartitionKey = "hack",
                RowKey = "award",
                Target = AwardTarget.individual,
            };
            var parameter = new AwardAssignment { assigneeId = "userId" };
            EnrollmentEntity enrollmentEntity = status.HasValue ? new EnrollmentEntity { Status = status.Value } : null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            moqs.AwardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "award", default)).ReturnsAsync(5);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "userId", default)).ReturnsAsync(enrollmentEntity);

            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAwardAssignment("Hack", "award", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Enrollment_NotFound, "userId", "hack"));
        }

        [Test]
        public async Task CreateAwardAssignment_TeamAssigned()
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity
            {
                Quantity = 10,
                PartitionKey = "hack",
                RowKey = "award",
                Target = AwardTarget.team,
            };
            var parameter = new AwardAssignment { assigneeId = "teamId" };
            TeamEntity team = new TeamEntity { CreatorId = "creator", DisplayName = "dn" };
            var assignment = new AwardAssignmentEntity { PartitionKey = "hack", RowKey = "rk", AssigneeId = "teamId", };
            var creator = new UserInfo { };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            moqs.AwardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "award", default)).ReturnsAsync(5);
            moqs.AwardManagement.Setup(t => t.CreateOrUpdateAssignmentAsync(
                It.Is<AwardAssignment>(aa => aa.hackathonName == "hack" && aa.awardId == "award" && aa.assigneeId == "teamId"), default))
                .ReturnsAsync(assignment);
            moqs.TeamManagement.Setup(t => t.GetTeamByIdAsync("hack", "teamId", default)).ReturnsAsync(team);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("creator", default)).ReturnsAsync(creator);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.createAwardAssignmentTeam, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.createAwardAssignmentTeam, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogTeamActivity("foo", "teamId", It.IsAny<string>(), ActivityLogType.createAwardAssignmentTeam, It.IsAny<object>(), null, default));

            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAwardAssignment("Hack", "award", parameter, default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<AwardAssignment>(result);
            Assert.AreEqual("hack", resp.hackathonName);
            Assert.AreEqual("rk", resp.assignmentId);
            Assert.AreEqual("teamId", resp.assigneeId);
            Assert.IsNull(resp.user);
            Assert.AreEqual("dn", resp.team.displayName);
        }

        [Test]
        public async Task CreateAwardAssignment_UserAssigned()
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity
            {
                Quantity = 10,
                PartitionKey = "hack",
                RowKey = "award",
                Target = AwardTarget.individual,
            };
            var parameter = new AwardAssignment { assigneeId = "userId" };
            EnrollmentEntity enrollmentEntity = new EnrollmentEntity { RowKey = "euid", Status = EnrollmentStatus.approved };
            var assignment = new AwardAssignmentEntity { PartitionKey = "hack", RowKey = "rk", AssigneeId = "userId", };
            var user = new UserInfo { GivenName = "gn" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            moqs.AwardManagement.Setup(t => t.GetAssignmentCountAsync("hack", "award", default)).ReturnsAsync(5);
            moqs.AwardManagement.Setup(t => t.CreateOrUpdateAssignmentAsync(
                It.Is<AwardAssignment>(aa => aa.hackathonName == "hack" && aa.awardId == "award" && aa.assigneeId == "userId"), default))
                .ReturnsAsync(assignment);
            moqs.EnrollmentManagement.Setup(e => e.GetEnrollmentAsync("hack", "userId", default)).ReturnsAsync(enrollmentEntity);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("userId", default)).ReturnsAsync(user);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("euid", default)).ReturnsAsync(user);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.createAwardAssignmentIndividual, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", It.IsAny<string>(), ActivityLogType.createAwardAssignmentIndividual, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("userId", "foo", It.IsAny<string>(), ActivityLogType.createAwardAssignmentIndividual, It.IsAny<object>(), nameof(Resources.ActivityLog_User2_createAwardAssignmentIndividual), default));

            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.CreateAwardAssignment("Hack", "award", parameter, default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<AwardAssignment>(result);
            Assert.AreEqual("hack", resp.hackathonName);
            Assert.AreEqual("rk", resp.assignmentId);
            Assert.AreEqual("userId", resp.assigneeId);
            Assert.IsNull(resp.team);
            Assert.AreEqual("gn", resp.user.GivenName);
        }
        #endregion

        #region UpdateAwardAssignment
        [Test]
        public async Task UpdateAwardAssignment()
        {
            var hackathon = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity { PartitionKey = "hack" };
            var parameter = new AwardAssignment { description = "desc" };
            TeamEntity team = new TeamEntity { CreatorId = "creator" };
            var assignment = new AwardAssignmentEntity { RowKey = "rk", AssigneeId = "teamId" };
            var creator = new UserInfo { LastIp = "ip" };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            awardManagement.Setup(t => t.GetAssignmentAsync("hack", "assignid", default)).ReturnsAsync(assignment);
            awardManagement.Setup(t => t.UpdateAssignmentAsync(assignment, parameter, default)).ReturnsAsync(assignment);

            var teamManagement = new Mock<ITeamManagement>();
            teamManagement.Setup(t => t.GetTeamByIdAsync("hack", "teamId", default)).ReturnsAsync(team);

            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("creator", default)).ReturnsAsync(creator);

            var activityLogManagement = new Mock<IActivityLogManagement>();
            activityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                && a.ActivityLogType == ActivityLogType.updateAwardAssignment.ToString()
                && a.TeamId == "teamId"), default));

            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                AuthorizationService = authorizationService.Object,
                TeamManagement = teamManagement.Object,
                UserManagement = userManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ActivityLogManagement = activityLogManagement.Object,
            };
            var result = await controller.UpdateAwardAssignment("Hack", "award", "assignid", parameter, default);

            Mock.VerifyAll(hackathonManagement, authorizationService, awardManagement, teamManagement, userManagement, activityLogManagement);
            hackathonManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            teamManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();
            activityLogManagement.VerifyNoOtherCalls();

            var resp = AssertHelper.AssertOKResult<AwardAssignment>(result);
            Assert.AreEqual("rk", resp.assignmentId);
            Assert.AreEqual("teamId", resp.assigneeId);
            Assert.IsNull(resp.user);
            Assert.AreEqual("creator", resp.team.creatorId);
            Assert.AreEqual("ip", resp.team.creator.LastIp);
        }
        #endregion

        #region GetAwardAssignment
        [Test]
        public async Task GetAwardAssignment()
        {
            var hackathon = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity { PartitionKey = "hack" };
            TeamEntity team = new TeamEntity { CreatorId = "creator" };
            var assignment = new AwardAssignmentEntity { RowKey = "rk", AssigneeId = "teamId" };
            var creator = new UserInfo { LastIp = "ip" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AwardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            moqs.AwardManagement.Setup(t => t.GetAssignmentAsync("hack", "assignid", default)).ReturnsAsync(assignment);
            moqs.TeamManagement.Setup(t => t.GetTeamByIdAsync("hack", "teamId", default)).ReturnsAsync(team);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("creator", default)).ReturnsAsync(creator);

            var controller = new AwardController();
            moqs.SetupController(controller);
            var result = await controller.GetAwardAssignment("Hack", "award", "assignid", default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<AwardAssignment>(result);
            Assert.AreEqual("rk", resp.assignmentId);
            Assert.AreEqual("teamId", resp.assigneeId);
            Assert.IsNull(resp.user);
            Assert.AreEqual("creator", resp.team.creatorId);
            Assert.AreEqual("ip", resp.team.creator.LastIp);
        }
        #endregion

        #region DeleteAwardAssignment
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteAwardAssignment(bool firstTime)
        {
            var hackathon = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var awardEntity = new AwardEntity { PartitionKey = "hack" };
            AwardAssignmentEntity assignment = firstTime ? new AwardAssignmentEntity() : null;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            var authorizationService = new Mock<IAuthorizationService>();
            authorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var activityLogManagement = new Mock<IActivityLogManagement>();

            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(t => t.GetAwardByIdAsync("hack", "award", default)).ReturnsAsync(awardEntity);
            awardManagement.Setup(t => t.GetAssignmentAsync("hack", "assignid", default)).ReturnsAsync(assignment);
            if (firstTime)
            {
                awardManagement.Setup(t => t.DeleteAssignmentAsync("hack", "assignid", default));
                activityLogManagement.Setup(a => a.LogActivity(It.Is<ActivityLogEntity>(a => a.HackathonName == "hack"
                      && a.ActivityLogType == ActivityLogType.deleteAwardAssignment.ToString()), default));
            }

            var controller = new AwardController
            {
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                AuthorizationService = authorizationService.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
                ActivityLogManagement = activityLogManagement.Object,
            };
            var result = await controller.DeleteAwardAssignment("Hack", "award", "assignid", default);

            Mock.VerifyAll(hackathonManagement, awardManagement, authorizationService, activityLogManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            authorizationService.VerifyNoOtherCalls();
            activityLogManagement.VerifyNoOtherCalls();

            AssertHelper.AssertNoContentResult(result);
        }
        #endregion

        #region ListAssignmentsByAward
        private static IEnumerable ListAssignmentsByAwardTestData()
        {
            // arg0: pagination
            // arg1: next pagination
            // arg2: expected nextlink

            // no pagination, no filter, no top
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    null,
                    null
                );

            // next link
            yield return new TestCaseData(
                    new Pagination { },
                    new Pagination { np = "np", nr = "nr" },
                    "&np=np&nr=nr"
                );

            // next link with top
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    new Pagination { np = "np2", nr = "nr2" },
                    "&top=10&np=np2&nr=nr2"
                );
        }

        [Test, TestCaseSource(nameof(ListAssignmentsByAwardTestData))]
        public async Task ListAssignmentsByAward(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            HackathonEntity hackathon = new HackathonEntity();
            AwardEntity award = new AwardEntity { };
            var assignments = new List<AwardAssignmentEntity>
            {
                new AwardAssignmentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                }
            };
            TeamEntity team = new TeamEntity { CreatorId = "creator" };
            var creator = new UserInfo { Locale = "locale" };

            // mock and capture
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(a => a.GetAwardByIdAsync("hack", "awardId", default)).ReturnsAsync(award);
            awardManagement.Setup(p => p.ListPaginatedAssignmentsAsync("hack", It.Is<AwardAssignmentQueryOptions>(o => o.QueryType == AwardAssignmentQueryType.Award), default))
                .Callback<string, AwardAssignmentQueryOptions, CancellationToken>((n, o, t) =>
                {
                    o.NextPage = next;
                })
                .ReturnsAsync(assignments);

            var teamManagement = new Mock<ITeamManagement>();
            teamManagement.Setup(t => t.GetTeamByIdAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(team);

            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("creator", default)).ReturnsAsync(creator);

            // run
            var controller = new AwardController
            {
                ResponseBuilder = new DefaultResponseBuilder(),
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                TeamManagement = teamManagement.Object,
                UserManagement = userManagement.Object,
            };
            var result = await controller.ListAssignmentsByAward("Hack", "awardId", pagination, default);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement, teamManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            teamManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();

            var list = AssertHelper.AssertOKResult<AwardAssignmentList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("rk", list.value[0].assignmentId);
            Assert.AreEqual("locale", list.value[0].team.creator.Locale);
        }
        #endregion

        #region ListAssignmentsByHackathon
        private static IEnumerable ListAssignmentsByHackathonTestData()
        {
            // arg0: pagination
            // arg1: next pagination
            // arg2: expected nextlink

            // no pagination, no filter, no top
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    null,
                    null
                );

            // next link
            yield return new TestCaseData(
                    new Pagination { },
                    new Pagination { np = "np", nr = "nr" },
                    "&np=np&nr=nr"
                );

            // next link with top
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    new Pagination { np = "np2", nr = "nr2" },
                    "&top=10&np=np2&nr=nr2"
                );
        }

        [Test, TestCaseSource(nameof(ListAssignmentsByHackathonTestData))]
        public async Task ListAssignmentsByHackathon(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            HackathonEntity hackathon = new HackathonEntity();
            List<AwardEntity> awards = new List<AwardEntity> { new AwardEntity { PartitionKey = "hack", RowKey = "awardId" } };
            var assignments = new List<AwardAssignmentEntity>
            {
                new AwardAssignmentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    AwardId = "awardId",
                },
                new AwardAssignmentEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                    AwardId = "awardId2",
                }
            };
            TeamEntity team = new TeamEntity { CreatorId = "creator" };
            var creator = new UserInfo { Locality = "locality" };

            // mock and capture
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var awardManagement = new Mock<IAwardManagement>();
            awardManagement.Setup(a => a.ListAwardsAsync("hack", default)).ReturnsAsync(awards);
            awardManagement.Setup(p => p.ListPaginatedAssignmentsAsync("hack", It.Is<AwardAssignmentQueryOptions>(o => o.QueryType == AwardAssignmentQueryType.Hackathon), default))
                .Callback<string, AwardAssignmentQueryOptions, CancellationToken>((n, o, t) =>
                {
                    o.NextPage = next;
                })
                .ReturnsAsync(assignments);

            var teamManagement = new Mock<ITeamManagement>();
            teamManagement.Setup(t => t.GetTeamByIdAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(team);

            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("creator", default)).ReturnsAsync(creator);

            // run
            var controller = new AwardController
            {
                ResponseBuilder = new DefaultResponseBuilder(),
                HackathonManagement = hackathonManagement.Object,
                AwardManagement = awardManagement.Object,
                TeamManagement = teamManagement.Object,
                UserManagement = userManagement.Object,
            };
            var result = await controller.ListAssignmentsByHackathon("Hack", pagination, default);

            // verify
            Mock.VerifyAll(hackathonManagement, awardManagement, teamManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            awardManagement.VerifyNoOtherCalls();
            teamManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();

            var list = AssertHelper.AssertOKResult<AwardAssignmentList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("rk", list.value[0].assignmentId);
            Assert.AreEqual("locality", list.value[0].team.creator.Locality);
        }
        #endregion
    }
}
