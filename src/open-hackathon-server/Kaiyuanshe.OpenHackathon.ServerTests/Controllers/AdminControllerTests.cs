﻿using Kaiyuanshe.OpenHackathon.Server;
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
    public class AdminControllerTests
    {
        #region CreateAdmin
        [Test]
        public async Task CreateAdmin_UserNotFound()
        {
            var hackathon = new HackathonEntity();
            var authResult = AuthorizationResult.Success();
            UserInfo user = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            var controller = new AdminController();
            moqs.SetupController(controller);
            var result = await controller.CreateAdmin("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task CreateAdmin_Succeeded()
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            UserInfo user = new UserInfo { Name = "name" };
            var adminEntity = new HackathonAdminEntity { PartitionKey = "pk", RowKey = "rk" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.HackathonAdminManagement.Setup(a => a.CreateAdminAsync(It.Is<HackathonAdmin>(ha =>
                ha.hackathonName == "hack" && ha.userId == "uid"), default))
                .ReturnsAsync(adminEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.createHackathonAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", It.IsAny<string>(), ActivityLogType.createHackathonAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("rk", "foo", It.IsAny<string>(), ActivityLogType.createHackathonAdmin, It.IsAny<object>(), nameof(Resources.ActivityLog_User2_createHackathonAdmin), default));

            var controller = new AdminController();
            moqs.SetupController(controller);
            var result = await controller.CreateAdmin("Hack", "uid", default);

            moqs.VerifyAll();
            var admin = AssertHelper.AssertOKResult<HackathonAdmin>(result);
            Assert.AreEqual("pk", admin.hackathonName);
            Assert.AreEqual("rk", admin.userId);
            Assert.AreEqual("name", admin.user.Name);
        }
        #endregion

        #region ListAdmins
        private static IEnumerable ListAdminsTestData()
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

        [Test, TestCaseSource(nameof(ListAdminsTestData))]
        public async Task ListAdmins(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            HackathonEntity hackathon = new HackathonEntity();
            List<HackathonAdminEntity> admins = new List<HackathonAdminEntity> {
                new HackathonAdminEntity { PartitionKey = "pk", RowKey = "uid" }
            };
            var user = new UserInfo { IsDeleted = true };

            // mock and capture
            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var adminManagement = new Mock<IHackathonAdminManagement>();
            adminManagement.Setup(j => j.ListPaginatedHackathonAdminAsync("hack", It.IsAny<AdminQueryOptions>(), default))
                .Callback<string, AdminQueryOptions, CancellationToken>((h, opt, c) => { opt.NextPage = next; })
                .ReturnsAsync(admins);
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            // run
            var controller = new AdminController
            {
                ResponseBuilder = new DefaultResponseBuilder(),
                HackathonManagement = hackathonManagement.Object,
                UserManagement = userManagement.Object,
                HackathonAdminManagement = adminManagement.Object,
            };
            var result = await controller.ListAdmins("Hack", pagination, default);

            // verify
            Mock.VerifyAll(hackathonManagement, adminManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            adminManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();

            var list = AssertHelper.AssertOKResult<HackathonAdminList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("uid", list.value[0].userId);
            Assert.AreEqual(true, list.value[0].user.IsDeleted);
        }
        #endregion

        #region GetAdmin
        [Test]
        public async Task GetAdmin_UserNotFound()
        {
            var hackathon = new HackathonEntity();
            UserInfo user = null;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            var controller = new AdminController
            {
                HackathonManagement = hackathonManagement.Object,
                UserManagement = userManagement.Object,
            };
            var result = await controller.GetAdmin("Hack", "uid", default);

            Mock.VerifyAll(hackathonManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task GetAdmin_NotAdmin()
        {
            var hackathon = new HackathonEntity();
            UserInfo user = new UserInfo();
            HackathonAdminEntity adminEntity = null;

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            var adminManagement = new Mock<IHackathonAdminManagement>();
            adminManagement.Setup(a => a.GetAdminAsync("hack", "uid", default)).ReturnsAsync(adminEntity);

            var controller = new AdminController
            {
                HackathonManagement = hackathonManagement.Object,
                HackathonAdminManagement = adminManagement.Object,
                UserManagement = userManagement.Object,
            };
            var result = await controller.GetAdmin("Hack", "uid", default);

            Mock.VerifyAll(hackathonManagement, adminManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();
            adminManagement.VerifyNoOtherCalls();

            AssertHelper.AssertObjectResult(result, 404, Resources.Admin_NotFound);
        }

        [Test]
        public async Task GetAdmin_Succeeded()
        {
            var hackathon = new HackathonEntity();
            UserInfo user = new UserInfo { StreetAddress = "streetAddress" };
            HackathonAdminEntity adminEntity = new HackathonAdminEntity { PartitionKey = "pk", RowKey = "rk" };

            var hackathonManagement = new Mock<IHackathonManagement>();
            hackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            var userManagement = new Mock<IUserManagement>();
            userManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            var adminManagement = new Mock<IHackathonAdminManagement>();
            adminManagement.Setup(a => a.GetAdminAsync("hack", "uid", default)).ReturnsAsync(adminEntity);

            var controller = new AdminController
            {
                HackathonManagement = hackathonManagement.Object,
                HackathonAdminManagement = adminManagement.Object,
                UserManagement = userManagement.Object,
                ResponseBuilder = new DefaultResponseBuilder(),
            };
            var result = await controller.GetAdmin("Hack", "uid", default);

            Mock.VerifyAll(hackathonManagement, adminManagement, userManagement);
            hackathonManagement.VerifyNoOtherCalls();
            userManagement.VerifyNoOtherCalls();
            adminManagement.VerifyNoOtherCalls();

            var admin = AssertHelper.AssertOKResult<HackathonAdmin>(result);
            Assert.AreEqual("pk", admin.hackathonName);
            Assert.AreEqual("rk", admin.userId);
            Assert.AreEqual("streetAddress", admin.user.StreetAddress);
        }
        #endregion

        #region DeleteAdmin
        [Test]
        public async Task DeleteAdmin_ReDelete()
        {
            var hackathon = new HackathonEntity();
            HackathonAdminEntity adminEntity = null;
            var authResult = AuthorizationResult.Success();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.HackathonAdminManagement.Setup(a => a.GetAdminAsync("hack", "uid", default)).ReturnsAsync(adminEntity);

            var controller = new AdminController();
            moqs.SetupController(controller);
            var result = await controller.DeleteAdmin("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }

        [Test]
        public async Task DeleteAdmin_DeleteCreator()
        {
            var hackathon = new HackathonEntity { CreatorId = "uid" };
            HackathonAdminEntity adminEntity = new HackathonAdminEntity();
            var authResult = AuthorizationResult.Success();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.HackathonAdminManagement.Setup(a => a.GetAdminAsync("hack", "uid", default)).ReturnsAsync(adminEntity);

            var controller = new AdminController();
            moqs.SetupController(controller);
            var result = await controller.DeleteAdmin("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Admin_CannotDeleteCreator);
        }

        [Test]
        public async Task DeleteAdmin_Deleted()
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            HackathonAdminEntity adminEntity = new HackathonAdminEntity { RowKey = "uid" };
            var authResult = AuthorizationResult.Success();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.HackathonAdminManagement.Setup(a => a.GetAdminAsync("hack", "uid", default)).ReturnsAsync(adminEntity);
            moqs.HackathonAdminManagement.Setup(a => a.DeleteAdminAsync("hack", "uid", default));
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(new UserInfo());
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.deleteHackathonAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", It.IsAny<string>(), ActivityLogType.deleteHackathonAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("uid", "foo", It.IsAny<string>(), ActivityLogType.deleteHackathonAdmin, It.IsAny<object>(), nameof(Resources.ActivityLog_User2_deleteHackathonAdmin), default));

            var controller = new AdminController();
            moqs.SetupController(controller);
            var result = await controller.DeleteAdmin("Hack", "uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion
    }
}
