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

        #region UpdateOrganizer
        [Test]
        public async Task UpdateOrganizer_NotFound()
        {
            var hackathon = new HackathonEntity { PartitionKey = "pk" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Organizer();
            OrganizerEntity? organizerEntity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.OrganizerManagement.Setup(o => o.GetOrganizerById("pk", "oid", default)).ReturnsAsync(organizerEntity);

            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.UpdateOrganizer("Hack", "oid", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Organizer_NotFound);
        }

        [Test]
        public async Task UpdateOrganizer_Updated()
        {
            var hackathon = new HackathonEntity { PartitionKey = "pk" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Organizer();
            OrganizerEntity? organizerEntity = new OrganizerEntity { Name = "name" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.OrganizerManagement.Setup(o => o.GetOrganizerById("pk", "oid", default)).ReturnsAsync(organizerEntity);
            moqs.OrganizerManagement.Setup(o => o.UpdateOrganizer(organizerEntity, parameter, default)).ReturnsAsync(organizerEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("pk", "", ActivityLogType.updateOrganizer, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "pk", "", ActivityLogType.updateOrganizer, It.IsAny<object>(), null, default));

            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.UpdateOrganizer("Hack", "oid", parameter, default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Organizer>(result);
            Assert.AreEqual("name", resp.name);
        }
        #endregion

        #region GetOrganizer
        [Test]
        public async Task GetOrganizer_NotFound()
        {
            var hackathon = new HackathonEntity { PartitionKey = "pk" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);

            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.GetOrganizer("Hack", "oid", default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Organizer_NotFound);
        }

        [Test]
        public async Task GetOrganizer_Succeeded()
        {
            var hackathon = new HackathonEntity { PartitionKey = "pk" };
            OrganizerEntity organizerEntity = new OrganizerEntity { Name = "orgname" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.OrganizerManagement.Setup(o => o.GetOrganizerById("pk", "oid", default)).ReturnsAsync(organizerEntity);

            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.GetOrganizer("Hack", "oid", default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Organizer>(result);
            Assert.AreEqual("orgname", resp.name);
        }
        #endregion

        #region ListByHackathon
        private static IEnumerable ListByHackathonTestData()
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

        [Test, TestCaseSource(nameof(ListByHackathonTestData))]
        public async Task ListByHackathon(
            Pagination pagination,
            Pagination next,
            string expectedLink)
        {
            // input
            HackathonEntity hackathon = new HackathonEntity();
            List<OrganizerEntity> organizers = new List<OrganizerEntity> {
                new OrganizerEntity { PartitionKey = "pk", RowKey = "oid" }
            };

            // mock and capture
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.OrganizerManagement.Setup(j => j.ListPaginatedOrganizersAsync("hack", It.IsAny<OrganizerQueryOptions>(), default))
                .Callback<string, OrganizerQueryOptions, CancellationToken>((h, opt, c) => { opt.NextPage = next; })
                .ReturnsAsync(organizers);

            // run
            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.ListByHackathon("Hack", pagination, default);

            // verify
            moqs.VerifyAll();
            var list = AssertHelper.AssertOKResult<OrganizerList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("oid", list.value[0].id);
        }
        #endregion

        #region DeleteOrganizer
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteOrganizer(bool firstTime)
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            OrganizerEntity? entity = firstTime ? new OrganizerEntity
            {
                PartitionKey = "hack",
                RowKey = "oid"
            } : null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.OrganizerManagement.Setup(t => t.GetOrganizerById("foo", "oid", default)).ReturnsAsync(entity);
            if (firstTime)
            {
                moqs.OrganizerManagement.Setup(t => t.DeleteOrganzer("hack", "oid", default));
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.deleteOrganizer, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", It.IsAny<string>(), ActivityLogType.deleteOrganizer, It.IsAny<object>(), null, default));
            }

            var controller = new OrganizerController();
            moqs.SetupController(controller);
            var result = await controller.DeleteOrganizer("Hack", "oid", default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion
    }
}
