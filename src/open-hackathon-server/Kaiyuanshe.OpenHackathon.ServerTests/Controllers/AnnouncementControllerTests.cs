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
using System.Diagnostics;
using System.Security.Claims;
using System.Threading;
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

        #region UpdateAnnouncement
        [Test]
        public async Task UpdateAnnouncement_NotFound()
        {
            var hackathon = new HackathonEntity { PartitionKey = "pk" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Announcement();
            AnnouncementEntity? organizerEntity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AnnouncementManagement.Setup(o => o.GetById("pk", "aid", default)).ReturnsAsync(organizerEntity);

            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.UpdateAnnouncement("Hack", "aid", parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.Announcement_NotFound);
        }

        [Test]
        public async Task UpdateAnnouncement_Updated()
        {
            var hackathon = new HackathonEntity { PartitionKey = "pk" };
            var authResult = AuthorizationResult.Success();
            var parameter = new Announcement();
            AnnouncementEntity? organizerEntity = new AnnouncementEntity { Title = "title" };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AnnouncementManagement.Setup(o => o.GetById("pk", "aid", default)).ReturnsAsync(organizerEntity);
            moqs.AnnouncementManagement.Setup(o => o.Update(organizerEntity, parameter, default)).ReturnsAsync(organizerEntity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("pk", "", ActivityLogType.updateAnnouncement, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "pk", "", ActivityLogType.updateAnnouncement, It.IsAny<object>(), null, default));

            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.UpdateAnnouncement("Hack", "aid", parameter, default);

            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Announcement>(result);
            Assert.AreEqual("title", resp.title);
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
            HackathonEntity hackathon = new HackathonEntity { PartitionKey = "hack" };
            List<AnnouncementEntity> entities = new List<AnnouncementEntity> {
                new AnnouncementEntity { PartitionKey = "pk", RowKey = "oid" }
            };

            // mock and capture
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AnnouncementManagement.Setup(j => j.ListPaginated(
                It.Is<AnnouncementQueryOptions>(a => a.HackathonName == "hack"), default))
                .Callback<AnnouncementQueryOptions, CancellationToken>((opt, c) => { opt.NextPage = next; })
                .ReturnsAsync(entities);

            // run
            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.ListByHackathon("Hack", pagination, default);

            // verify
            moqs.VerifyAll();
            var list = AssertHelper.AssertOKResult<AnnouncementList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].hackathonName);
            Assert.AreEqual("oid", list.value[0].id);
        }
        #endregion

        #region DeleteAnnouncement
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteAnnouncement(bool firstTime)
        {
            var hackathon = new HackathonEntity { PartitionKey = "foo" };
            var authResult = AuthorizationResult.Success();
            AnnouncementEntity? entity = firstTime ? new AnnouncementEntity
            {
                PartitionKey = "foo",
                RowKey = "aid"
            } : null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(hackathon);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathon, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);
            moqs.AnnouncementManagement.Setup(t => t.GetById("foo", "aid", default)).ReturnsAsync(entity);
            if (firstTime)
            {
                Debug.Assert(entity != null);
                moqs.AnnouncementManagement.Setup(t => t.Delete(entity, default));
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.deleteAnnouncement, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "foo", It.IsAny<string>(), ActivityLogType.deleteAnnouncement, It.IsAny<object>(), null, default));
            }

            var controller = new AnnouncementController();
            moqs.SetupController(controller);
            var result = await controller.DeleteAnnouncement("Hack", "aid", default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion
    }
}
