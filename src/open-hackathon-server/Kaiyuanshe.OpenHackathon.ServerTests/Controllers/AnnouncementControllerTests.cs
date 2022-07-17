using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    internal class AnnouncementControllerTests
    {
        #region CreateAnnouncement
        [Test]
        public async Task CreateAnnouncement()
        {
            // input
            string hackName = "Hack";
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "foo" };
            Announcement parameter = new Announcement { };
            var authResult = AuthorizationResult.Success();
            AnnouncementEntity organizerEntity = new AnnouncementEntity { Content = "ct" };

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AnnouncementManagement.Setup(o => o.Create(parameter, default)).ReturnsAsync(organizerEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", "", ActivityLogType.createAnnouncement, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", "", ActivityLogType.createAnnouncement, It.IsAny<object>(), null, default));

            // run
            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.CreateAnnouncement(hackName, parameter, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Announcement>(result);
            Assert.AreEqual("ct", resp.content);
        }
        #endregion

        #region GetAnnouncement
        [Test]
        public async Task GetAnnouncement_NotFound()
        {
            // input
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "foo" };
            AnnouncementEntity? announcementEntity = null;

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AnnouncementManagement.Setup(o => o.GetById("foo", "aid", default)).ReturnsAsync(announcementEntity);

            // run
            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.GetAnnouncement("Hack", "aid", default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Announcement_NotFound);
        }

        [Test]
        public async Task GetAnnouncement_Succeed()
        {
            // input
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "foo" };
            AnnouncementEntity? announcementEntity = new AnnouncementEntity { Content = "ct" };

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AnnouncementManagement.Setup(o => o.GetById("foo", "aid", default)).ReturnsAsync(announcementEntity);

            // run
            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.GetAnnouncement("Hack", "aid", default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Announcement>(result);
            Assert.AreEqual("ct", resp.content);
        }
        #endregion
    }
}
