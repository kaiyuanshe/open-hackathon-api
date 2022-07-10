using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
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
    public class JudgeControllerTests
    {
        #region CreateJudge
        [Test]
        public async Task CreateJudge_UserNotFound()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Judge();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default));

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.CreateJudge("Hack", "uid", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task CreateJudge_TooMany()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Judge { description = "desc" };
            UserInfo user = new UserInfo { };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.CanCreateJudgeAsync("hack", default)).ReturnsAsync(false);

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.CreateJudge("Hack", "uid", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Judge_TooMany);
        }

        [Test]
        public async Task CreateJudge_Created()
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Judge { description = "desc" };
            UserInfo user = new UserInfo { FamilyName = "fn" };
            var entity = new JudgeEntity { PartitionKey = "pk" };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.CreateJudgeAsync(It.Is<Judge>(j =>
                j.description == "desc" &&
                j.hackathonName == "hack" &&
                j.userId == "uid"), default)).ReturnsAsync(entity);
            moqs.JudgeManagement.Setup(j => j.CanCreateJudgeAsync("hack", default)).ReturnsAsync(true);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.createJudge, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.createJudge, It.IsAny<object>(), null, default));

            // test
            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.CreateJudge("Hack", "uid", parameter, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Judge>(result);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("fn", resp.user.FamilyName);
        }

        #endregion

        #region UpdateJudge
        [Test]
        public async Task UpdateJudge_JudgeNotFound()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            var parameter = new Judge();
            UserInfo user = new UserInfo();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.GetJudgeAsync("hack", "uid", default));

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.UpdateJudge("Hack", "uid", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Judge_NotFound);
        }

        [Test]
        public async Task UpdateJudge_Updated()
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Judge { description = "desc" };
            UserInfo user = new UserInfo { Province = "province" };
            JudgeEntity entity = new JudgeEntity { PartitionKey = "pk", Description = "d1" };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.GetJudgeAsync("hack", "uid", default)).ReturnsAsync(entity);
            moqs.JudgeManagement.Setup(j => j.UpdateJudgeAsync(entity, parameter, default)).ReturnsAsync(entity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.updateJudge, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.updateJudge, It.IsAny<object>(), null, default));

            // test
            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.UpdateJudge("Hack", "uid", parameter, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Judge>(result);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("province", resp.user.Province);
        }

        #endregion

        #region GetJudge
        [Test]
        public async Task GetJudge_HackNotOnline()
        {
            var hackathon = new HackathonEntity { };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.GetJudge("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_NotOnline);
        }

        [Test]
        public async Task GetJudge_NotFound()
        {
            var hackathon = new HackathonEntity { Status = HackathonStatus.online };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default));

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.GetJudge("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task GetJudge_Succeeded()
        {
            var hackathon = new HackathonEntity { Status = HackathonStatus.online };
            UserInfo user = new UserInfo { Profile = "profile" };
            var judgeEntity = new JudgeEntity { PartitionKey = "pk" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.GetJudgeAsync("hack", "uid", default)).ReturnsAsync(judgeEntity);

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.GetJudge("Hack", "uid", default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Judge>(result);
            Assert.AreEqual("pk", resp.hackathonName);
            Assert.AreEqual("profile", resp.user.Profile);
        }

        #endregion

        #region ListJudgesByHackathon
        private static IEnumerable ListJudgesByHackathonTestData()
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

        [Test, TestCaseSource(nameof(ListJudgesByHackathonTestData))]
        public async Task ListJudgesByHackathon(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            HackathonEntity hackathon = new HackathonEntity();
            List<JudgeEntity> judges = new List<JudgeEntity> {
                new JudgeEntity { PartitionKey = "pk", RowKey = "uid" }
            };
            var user = new UserInfo { Website = "https://website" };

            // mock and capture
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.JudgeManagement.Setup(j => j.ListPaginatedJudgesAsync("hack", It.IsAny<JudgeQueryOptions>(), default))
                .Callback<string, JudgeQueryOptions, CancellationToken>((h, opt, c) => { opt.NextPage = next; })
                .ReturnsAsync(judges);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            // run
            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.ListJudgesByHackathon("Hack", pagination, default);

            // verify
            moqs.VerifyAll();
            var list = AssertHelper.AssertOKResult<JudgeList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("uid", list.value[0].userId);
            Assert.AreEqual("https://website", list.value[0].user.Website);
        }
        #endregion

        #region DeleteJudge
        [TestCase(false)]
        [TestCase(true)]
        public async Task DeleteJudge(bool firstTime)
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            UserInfo user = new UserInfo { };
            JudgeEntity? entity = firstTime ? new JudgeEntity() : null;

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.GetJudgeAsync("hack", "uid", default)).ReturnsAsync(entity);
            if (firstTime)
            {
                moqs.JudgeManagement.Setup(j => j.DeleteJudgeAsync("hack", "uid", default));
                moqs.RatingManagement.Setup(j => j.IsRatingCountGreaterThanZero(
                        "hack",
                        It.Is<RatingQueryOptions>(o => o.RatingKindId == null && o.JudgeId == "uid" && o.TeamId == null),
                        default)).ReturnsAsync(false);
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.deleteJudge, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.deleteJudge, It.IsAny<object>(), null, default));
            }

            // test
            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.DeleteJudge("Hack", "uid", default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }

        [Test]
        public async Task DeleteJudge_HasRating()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            UserInfo user = new UserInfo { };
            JudgeEntity entity = new JudgeEntity();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.JudgeManagement.Setup(j => j.GetJudgeAsync("hack", "uid", default)).ReturnsAsync(entity);
            moqs.RatingManagement.Setup(j => j.IsRatingCountGreaterThanZero(
                "hack",
                It.Is<RatingQueryOptions>(o => o.RatingKindId == null && o.JudgeId == "uid" && o.TeamId == null),
                default)).ReturnsAsync(true);

            var controller = new JudgeController();
            moqs.SetupController(controller);
            var result = await controller.DeleteJudge("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, string.Format(Resources.Rating_HasRating, nameof(Judge)));
        }
        #endregion
    }
}
