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
    internal class TemplateRepoControllerTests
    {
        #region CreateTemplateRepo
        [Test]
        public async Task CreateTemplateRepo()
        {
            // input
            var hackNameOriginal = "Hack";
            var hackName = hackNameOriginal.ToLower();
            var hackathonEntity = new HackathonEntity() { PartitionKey = hackName };
            var request = new TemplateRepo()
            {
                url = "https://example.com",
            };
            var entity = new TemplateRepoEntity()
            {
                PartitionKey = hackName,
                Url = request.url,
            };

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(hackName, default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.TemplateRepoManagement.Setup(o => o.CreateTemplateRepoAsync(request, default)).ReturnsAsync(entity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity(hackName, "", ActivityLogType.createTemplateRepo, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", hackName, "", ActivityLogType.createTemplateRepo, It.IsAny<object>(), null, default));

            // run
            var controller = new TemplateRepoController();
            moqs.SetupController(controller);
            var result = await controller.CreateTemplateRepo(hackNameOriginal, request, default);

            // verify
            moqs.VerifyAll();
            TemplateRepo resp = AssertHelper.AssertOKResult<TemplateRepo>(result);
            Assert.AreEqual(request.url, resp.url);
        }
        #endregion

        #region UpdateTemplateRepo
        [Test]
        public async Task UpdateTemplateRepo_NotFound()
        {
            // input
            var hackNameOriginal = "Hack";
            var hackName = hackNameOriginal.ToLower();
            var templateRepoId = "notexist";
            var hackathonEntity = new HackathonEntity() { PartitionKey = hackName };
            var request = new TemplateRepo();

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(hackName, default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.TemplateRepoManagement.Setup(o => o.GetTemplateRepoAsync(hackName, templateRepoId, default)).ReturnsAsync((TemplateRepoEntity?)null);

            // run
            var controller = new TemplateRepoController();
            moqs.SetupController(controller);
            var result = await controller.UpdateTemplateRepo(hackNameOriginal, templateRepoId, request, default);

            // verify
            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, Resources.TemplateRepo_NotFound);
        }

        [Test]
        public async Task UpdateTemplateRepo_Ok()
        {
            // input
            var hackNameOriginal = "Hack";
            var hackName = hackNameOriginal.ToLower();
            var templateRepoId = "uuid";
            var hackathonEntity = new HackathonEntity() { PartitionKey = hackName };
            var request = new TemplateRepo();
            var entity = new TemplateRepoEntity()
            {
                PartitionKey = hackName,
                Url = request.url,
            };

            // moq
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(hackName, default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.TemplateRepoManagement.Setup(o => o.GetTemplateRepoAsync(hackName, templateRepoId, default)).ReturnsAsync(entity);
            moqs.TemplateRepoManagement.Setup(o => o.UpdateTemplateRepoAsync(entity, request, default)).ReturnsAsync(entity);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity(hackName, "", ActivityLogType.updateTemplateRepo, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", hackName, "", ActivityLogType.updateTemplateRepo, It.IsAny<object>(), null, default));

            // run
            var controller = new TemplateRepoController();
            moqs.SetupController(controller);
            var result = await controller.UpdateTemplateRepo(hackNameOriginal, templateRepoId, request, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<TemplateRepo>(result);
            Assert.AreEqual(request.url, resp.url);
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
            string hackNameOriginal = "Hack";
            string hackName = hackNameOriginal.ToLower();
            HackathonEntity hackathonEntity = new HackathonEntity();
            List<TemplateRepoEntity> entities = new List<TemplateRepoEntity> {
                new TemplateRepoEntity { PartitionKey = hackName, RowKey = "oid" }
            };

            // mock and capture
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(hackName, default)).ReturnsAsync(hackathonEntity);
            moqs.TemplateRepoManagement.Setup(j => j.ListPaginatedTemplateReposAsync(hackName, It.IsAny<TemplateRepoQueryOptions>(), default))
                .Callback<string, TemplateRepoQueryOptions, CancellationToken>((h, opt, c) => { opt.NextPage = next; })
                .ReturnsAsync(entities);

            // run
            var controller = new TemplateRepoController();
            moqs.SetupController(controller);
            var result = await controller.ListByHackathon(hackNameOriginal, pagination, default);

            // verify
            moqs.VerifyAll();
            var list = AssertHelper.AssertOKResult<TemplateRepoList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual(entities[0].HackathonName, list.value[0].hackathonName);
            Assert.AreEqual(entities[0].Id, list.value[0].id);
        }
        #endregion

        #region DeleteTemplateRepo
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteTemplateRepo(bool existed)
        {
            var hackNameOriginal = "Hack";
            var hackName = hackNameOriginal.ToLower();
            var templateRepoId = "uuid";
            var hackathonEntity = new HackathonEntity() { PartitionKey = hackName };
            TemplateRepoEntity? entity = existed ? new TemplateRepoEntity
            {
                PartitionKey = hackName,
                RowKey = templateRepoId,
            } : null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync(hackName, default)).ReturnsAsync(hackathonEntity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), hackathonEntity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(AuthorizationResult.Success());
            moqs.TemplateRepoManagement.Setup(t => t.GetTemplateRepoAsync(hackName, templateRepoId, default)).ReturnsAsync(entity);
            if (existed)
            {
                moqs.TemplateRepoManagement.Setup(t => t.DeleteTemplateRepoAsync(hackName, templateRepoId, default));
                moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity(hackName, It.IsAny<string>(), ActivityLogType.deleteTemplateRepo, It.IsAny<object>(), null, default));
                moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", hackName, It.IsAny<string>(), ActivityLogType.deleteTemplateRepo, It.IsAny<object>(), null, default));
            }

            var controller = new TemplateRepoController();
            moqs.SetupController(controller);
            var result = await controller.DeleteTemplateRepo(hackNameOriginal, templateRepoId, default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion
    }
}
