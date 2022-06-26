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
    internal class OrganizerControllerTests
    {
        #region CreateOrganizer
        [Test]
        public async Task CreateOrganizer()
        {
            // input
            string hackName = "Hack";
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "foo" };
            Organizer parameter = new Organizer { };
            var authResult = AuthorizationResult.Success();
            OrganizerEntity organizerEntity = new OrganizerEntity { Name = "name" };

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.OrganizerManagement.Setup(o => o.CreateOrganizer("hack", parameter, default)).ReturnsAsync(organizerEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", "", ActivityLogType.createOrganizer, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", "", ActivityLogType.createOrganizer, It.IsAny<object>(), null, default));

            // run
            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrganizer(hackName, parameter, default);

            // verify
            moqs.VerifyAll();
            Organizer resp = AssertHelper.AssertOKResult<Organizer>(result);
            Assert.AreEqual("name", resp.name);
        }
        #endregion

    }
}
