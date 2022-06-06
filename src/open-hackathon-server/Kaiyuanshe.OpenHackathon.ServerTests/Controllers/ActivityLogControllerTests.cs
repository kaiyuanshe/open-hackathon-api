using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    internal class ActivityLogControllerTests
    {
        #region ListAcitivitiesByHackathon
        [Test]
        public async Task ListAcitivitiesByHackathon_HackNotFound()
        {
            HackathonEntity hackathon = null;

            var mockContext = new Moqs();
            mockContext.HackathonManagement.Setup(u => u.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            var controller = new ActivityLogController();
            mockContext.SetupController(controller);
            var result = await controller.ListAcitivitiesByHackathon("Hack", null, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, "Hack"));
        }

        [Test]
        public async Task ListAcitivitiesByHackathon_Success()
        {
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack" };
            var pagination = new Pagination();
            var entities = new List<ActivityLogEntity>
            {
                new ActivityLogEntity{ },
                new ActivityLogEntity{ }
            };

            var mockContext = new Moqs();
            mockContext.HackathonManagement.Setup(u => u.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            mockContext.ActivityLogManagement.Setup(l => l.ListActivityLogs(
                It.Is<ActivityLogQueryOptions>(o =>
                    o.Pagination == pagination
                    && o.Category == ActivityLogCategory.Hackathon
                    && o.HackathonName == "hack"), default))
                .ReturnsAsync(entities)
                .Callback<ActivityLogQueryOptions, CancellationToken>((o, c) =>
                {
                    o.NextPage = new Pagination { np = "np", nr = "nr", top = 1 };
                });

            var controller = new ActivityLogController();
            mockContext.SetupController(controller);
            var result = await controller.ListAcitivitiesByHackathon("Hack", pagination, default);

            mockContext.VerifyAll();
            var logs = AssertHelper.AssertOKResult<ActivityLogList>(result);
            Assert.AreEqual(2, logs.value.Length);
            Assert.AreEqual("&np=np&nr=nr&top=1", logs.nextLink);
        }
        #endregion

        #region ListAcitivitiesByUser
        [Test]
        public async Task ListAcitivitiesByUser_UserNotFound()
        {
            UserInfo user = null;

            var mockContext = new Moqs();
            mockContext.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);

            var controller = new ActivityLogController();
            mockContext.SetupController(controller);
            var result = await controller.ListAcitivitiesByUser("uid", null, default);

            mockContext.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.User_NotFound);
        }

        [Test]
        public async Task ListAcitivitiesByUser_Success()
        {
            UserInfo user = new UserInfo { };
            var pagination = new Pagination();
            var entities = new List<ActivityLogEntity>
            {
                new ActivityLogEntity{ },
                new ActivityLogEntity{ }
            };

            var mockContext = new Moqs();
            mockContext.UserManagement.Setup(u => u.GetUserByIdAsync("uid", default)).ReturnsAsync(user);
            mockContext.ActivityLogManagement.Setup(l => l.ListActivityLogs(
                It.Is<ActivityLogQueryOptions>(o =>
                    o.Pagination == pagination
                    && o.Category == ActivityLogCategory.User
                    && o.UserId == "uid"), default))
                .ReturnsAsync(entities)
                .Callback<ActivityLogQueryOptions, CancellationToken>((o, c) =>
                {
                    o.NextPage = new Pagination { np = "np", nr = "nr", top = 1 };
                });

            var controller = new ActivityLogController();
            mockContext.SetupController(controller);
            var result = await controller.ListAcitivitiesByUser("uid", pagination, default);

            mockContext.VerifyAll();
            var logs = AssertHelper.AssertOKResult<ActivityLogList>(result);
            Assert.AreEqual(2, logs.value.Length);
            Assert.AreEqual("&np=np&nr=nr&top=1", logs.nextLink);
        }
        #endregion
    }
}
