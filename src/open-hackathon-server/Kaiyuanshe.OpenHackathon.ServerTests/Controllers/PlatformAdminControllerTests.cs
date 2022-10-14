using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
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
    }
}
