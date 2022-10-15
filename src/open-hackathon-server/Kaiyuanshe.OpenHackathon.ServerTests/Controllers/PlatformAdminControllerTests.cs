using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    internal class PlatformAdminControllerTests
    {
        #region CreatePlatformAdmin
        [Test]
        public async Task CreatePlatformAdmin_UserNotFound()
        {
            UserInfo? user = null;

            var moqs = new Moqs();
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            var controller = new PlatformAdminController();
            moqs.SetupController(controller);
            var result = await controller.CreatePlatformAdmin("uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task CreatePlatformAdmin_Succeeded()
        {
            UserInfo user = new UserInfo { Name = "name" };
            var adminEntity = new HackathonAdminEntity { RowKey = "rk" };

            var moqs = new Moqs();
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.HackathonAdminManagement.Setup(a => a.CreateAdminAsync(It.Is<HackathonAdmin>(ha =>
                ha.hackathonName == string.Empty && ha.userId == "uid"), default))
                .ReturnsAsync(adminEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("", "", ActivityLogType.createPlatformAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("rk", "", "", ActivityLogType.createPlatformAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "", "", ActivityLogType.createPlatformAdmin, It.IsAny<object>(), nameof(Resources.ActivityLog_User_createPlatformAdmin2), default));

            var controller = new PlatformAdminController();
            moqs.SetupController(controller);
            var result = await controller.CreatePlatformAdmin("uid", default);

            moqs.VerifyAll();
            var admin = AssertHelper.AssertOKResult<HackathonAdmin>(result);
            Assert.AreEqual("rk", admin.userId);
            Assert.AreEqual("name", admin.user.Name);
        }
        #endregion

        #region DeletePlatformAdmin
        [Test]
        public async Task DeletePlatformAdmin_UserNotFound()
        {
            UserInfo? user = null;

            var moqs = new Moqs();
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            var controller = new PlatformAdminController();
            moqs.SetupController(controller);
            var result = await controller.DeletePlatformAdmin("uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task DeletePlatformAdmin_Succeeded()
        {
            UserInfo user = new UserInfo { Name = "name" };

            var moqs = new Moqs();
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            moqs.HackathonAdminManagement.Setup(a => a.DeleteAdminAsync("", "uid", default));
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("", "", ActivityLogType.deletePlatformAdmin, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "", "", ActivityLogType.deletePlatformAdmin, It.IsAny<object>(), nameof(Resources.ActivityLog_User_deletePlatformAdmin), default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("uid", "", "", ActivityLogType.deletePlatformAdmin, It.IsAny<object>(), nameof(Resources.ActivityLog_User_deletePlatformAdmin2), default));

            var controller = new PlatformAdminController();
            moqs.SetupController(controller);
            var result = await controller.DeletePlatformAdmin("uid", default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion

        #region ListPlatformAdmins
        private static IEnumerable ListPlatformAdminsTestData()
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

        [Test, TestCaseSource(nameof(ListPlatformAdminsTestData))]
        public async Task ListPlatformAdmins(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            List<HackathonAdminEntity> admins = new List<HackathonAdminEntity> {
                new HackathonAdminEntity { PartitionKey = "pk", RowKey = "uid" }
            };
            var user = new UserInfo { Username = "un" };

            // mock and capture
            var moqs = new Moqs();
            moqs.HackathonAdminManagement.Setup(j => j.ListPaginatedHackathonAdminAsync(string.Empty, It.IsAny<AdminQueryOptions>(), default))
                .Callback<string, AdminQueryOptions, CancellationToken>((h, opt, c) => { opt.NextPage = next; })
                .ReturnsAsync(admins);
            moqs.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            // run
            var controller = new PlatformAdminController();
            moqs.SetupController(controller);
            var result = await controller.ListPlatformAdmins(pagination, default);

            // verify
            moqs.VerifyAll();

            var list = AssertHelper.AssertOKResult<HackathonAdminList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("uid", list.value[0].userId);
            Assert.AreEqual("un", list.value[0].user.Username);
        }
        #endregion
    }
}
