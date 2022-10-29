using Kaiyuanshe.OpenHackathon.Server;
using Kaiyuanshe.OpenHackathon.Server.Auth;
using Kaiyuanshe.OpenHackathon.Server.Biz.Options;
using Kaiyuanshe.OpenHackathon.Server.Controllers;
using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Storage.Entities;
using Microsoft.AspNetCore.Authorization;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Controllers
{
    [TestFixture]
    public class HackathonControllerTests
    {
        #region CheckNameAvailability
        [TestCase("")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase("@")]
        [TestCase("#")]
        [TestCase("%")]
        [TestCase("-")]
        [TestCase("_")]
        [TestCase("=")]
        [TestCase(" ")]
        public async Task CheckNameAvailability_Invalid(string name)
        {
            var parameter = new NameAvailability { name = name };

            var controller = new HackathonController();
            var result = await controller.CheckNameAvailability(parameter, default);
            NameAvailability resp = (NameAvailability)(result);
            Assert.AreEqual(name, resp.name);
            Assert.IsFalse(resp.nameAvailable);
            Assert.AreEqual("Invalid", resp.reason);
            Assert.AreEqual(Resources.Hackathon_Name_Invalid, resp.message);
        }

        [Test]
        public async Task CheckNameAvailability_AlreadyExists()
        {
            var parameter = new NameAvailability { name = "Foo" };
            HackathonEntity entity = new HackathonEntity();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("foo", default))
                .ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CheckNameAvailability(parameter, default);

            moqs.VerifyAll();
            NameAvailability resp = (NameAvailability)(result);
            Assert.AreEqual("Foo", resp.name);
            Assert.IsFalse(resp.nameAvailable);
            Assert.AreEqual("AlreadyExists", resp.reason);
            Assert.AreEqual(Resources.Hackathon_Name_Taken, resp.message);
        }

        [Test]
        public async Task CheckNameAvailability_OK()
        {
            var parameter = new NameAvailability { name = "Foo" };
            HackathonEntity? entity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(h => h.GetHackathonEntityByNameAsync("foo", default))
                .ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CheckNameAvailability(parameter, default);

            moqs.VerifyAll();
            NameAvailability resp = (NameAvailability)(result);
            Assert.AreEqual("Foo", resp.name);
            Assert.IsTrue(resp.nameAvailable);
        }

        #endregion

        #region CreateOrUpdate
        [Test]
        public async Task CreateOrUpdateTest_CreateTooMany()
        {
            var hack = new Hackathon();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(default(HackathonEntity));
            moqs.HackathonManagement.Setup(p => p.CanCreateHackathonAsync(It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(false);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", hack, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_CreateTooMany);
        }

        [Test]
        public async Task CreateOrUpdateTest_Create()
        {
            var hack = new Hackathon { displayName = "dp" };
            var inserted = new HackathonEntity
            {
                PartitionKey = "test2",
                AutoApprove = true
            };
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(default(HackathonEntity));
            moqs.HackathonManagement.Setup(p => p.CreateHackathonAsync(hack, default)).ReturnsAsync(inserted);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(inserted, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.HackathonManagement.Setup(p => p.CanCreateHackathonAsync(It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(true);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("hack", "", ActivityLogType.createHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "hack", "", ActivityLogType.createHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", hack, default);

            // verify
            moqs.VerifyAll();
            Hackathon resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("test2", resp.name);
            Assert.IsTrue(resp.autoApprove);
            Debug.Assert(resp.roles != null);
            Assert.IsTrue(resp.roles.isAdmin);
        }

        [Test]
        public async Task CreateOrUpdateTest_UpdateForbidden()
        {
            var hack = new Hackathon();
            var entity = new HackathonEntity();
            var name = "test1";
            var authResult = AuthorizationResult.Failed();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync(It.IsAny<string>(), default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
                .ReturnsAsync(authResult);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate(name, hack, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task CreateOrUpdateTest_UpdateSucceeded()
        {
            var hack = new Hackathon();
            var entity = new HackathonEntity
            {
                PartitionKey = "test2",
                AutoApprove = true
            };
            var authResult = AuthorizationResult.Success();
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(p => p.UpdateHackathonAsync(hack, default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
                           .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", hack, default);

            // verify
            moqs.VerifyAll();

            Hackathon resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("test2", resp.name);
            Assert.AreEqual("test2", resp.name);
            Assert.IsTrue(resp.autoApprove);
            Debug.Assert(resp.roles != null);
            Assert.IsTrue(resp.roles.isAdmin);
        }

        #endregion

        #region Update
        [Test]
        public async Task UpdateTest_NotFound()
        {
            string name = "Foo";
            HackathonEntity? entity = null;
            Hackathon parameter = new Hackathon();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Update(name, parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name.ToLower()));
        }

        [Test]
        public async Task UpdateTest_ReadOnly()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { ReadOnly = true };
            Hackathon parameter = new Hackathon();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Update(name, parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_ReadOnly);
        }

        [Test]
        public async Task UpdateTest_AccessDenied()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            Hackathon parameter = new Hackathon();
            var authResult = AuthorizationResult.Failed();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
              .ReturnsAsync(authResult);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Update(name, parameter, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task UpdateTest_Updated()
        {
            HackathonEntity entity = new HackathonEntity { PartitionKey = "test2", DisplayName = "displayname" };
            Hackathon parameter = new Hackathon();
            var authResult = AuthorizationResult.Success();
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(p => p.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(p => p.UpdateHackathonAsync(parameter, default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
                           .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "test2", "", ActivityLogType.updateHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.CreateOrUpdate("Hack", parameter, default);

            // verify
            moqs.VerifyAll();

            Hackathon resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("hack", parameter.name);
            Assert.AreEqual("displayname", resp.displayName);
            Debug.Assert(resp.roles != null);
            Assert.IsTrue(resp.roles.isAdmin);
        }

        #endregion

        #region Get
        [Test]
        public async Task GetTest_NotFound()
        {
            string name = "Foo";
            HackathonEntity? entity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Get(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name));
        }

        [Test]
        public async Task GetTest_NotOnlineAnonymous()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Detail = "detail" };
            HackathonRoles? role = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default))
              .ReturnsAsync(role);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Get(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name));
        }

        [Test]
        public async Task GetTest_NotOnlineNotAdmin()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Detail = "detail" };
            HackathonRoles role = new HackathonRoles { isAdmin = false };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default))
              .ReturnsAsync(role);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Get(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 404, string.Format(Resources.Hackathon_NotFound, name));
        }

        [Test]
        public async Task GetTest_OK()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Detail = "detail", ReadOnly = true };
            var role = new HackathonRoles { isAdmin = true };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", CancellationToken.None))
                .ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default))
              .ReturnsAsync(role);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Get(name, default);

            moqs.VerifyAll();
            Hackathon hackathon = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual("detail", hackathon.detail);
            Debug.Assert(hackathon.roles != null);
            Assert.IsTrue(hackathon.roles.isAdmin);
        }

        #endregion

        #region ListHackathon
        private static IEnumerable ListHackathonTestData()
        {
            // arg0: pagination
            // arg1: search
            // arg2: userId
            // arg3: order by
            // arg4: listType
            // arg5: next pagination
            // arg6: expected nextlink

            // no pagination, no filter, no top
            yield return new TestCaseData(
                    new Pagination { },
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10, np = "np", nr = "nr" },
                    "search",
                    null,
                    HackathonOrderBy.updatedAt,
                    HackathonListType.online,
                    null,
                    null
                );

            // with pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10 },
                    "search",
                    null,
                    HackathonOrderBy.updatedAt,
                    HackathonListType.admin,
                    new Pagination { np = "np", nr = "nr" },
                    "&top=10&search=search&orderby=updatedAt&listType=admin&np=np&nr=nr"
                );
            yield return new TestCaseData(
                    new Pagination { top = 10 },
                    "search",
                    null,
                    HackathonOrderBy.updatedAt,
                    HackathonListType.online,
                    new Pagination { np = "np", nr = "nr" },
                    "&top=10&search=search&orderby=updatedAt&listType=online&np=np&nr=nr"
                );

            // with userId, pagination and filters
            yield return new TestCaseData(
                    new Pagination { top = 10 },
                    "search",
                    "uid2",
                    HackathonOrderBy.updatedAt,
                    HackathonListType.admin,
                    new Pagination { np = "np", nr = "nr" },
                    "&top=10&search=search&orderby=updatedAt&listType=admin&np=np&nr=nr"
                );
        }

        [Test, TestCaseSource(nameof(ListHackathonTestData))]
        public async Task ListHackathon(
            Pagination pagination,
            string search,
            string userId,
            HackathonOrderBy? orderBy,
            HackathonListType? listType,
            Pagination next,
            string expectedLink)
        {
            // input
            var hackathons = new List<HackathonEntity>
            {
                new HackathonEntity
                {
                    PartitionKey = "pk",
                    RowKey = "rk",
                }
            };
            var hackWithRoles = new List<Tuple<HackathonEntity, HackathonRoles?>>()
            {
                Tuple.Create(hackathons.First(), (HackathonRoles?)new HackathonRoles{})
            };

            // mock and capture
            var moqs = new Moqs();
            HackathonQueryOptions? optionsCaptured = null;
            moqs.HackathonManagement.Setup(p => p.ListPaginatedHackathonsAsync(It.IsAny<HackathonQueryOptions>(), default))
                .Callback<HackathonQueryOptions, CancellationToken>((o, t) =>
                 {
                     optionsCaptured = o;
                     optionsCaptured.NextPage = next;
                 })
                .ReturnsAsync(hackathons);
            moqs.HackathonManagement.Setup(h => h.ListHackathonRolesAsync(hackathons, It.IsAny<ClaimsPrincipal>(), default))
                .ReturnsAsync(hackWithRoles);
            moqs.HackathonAdminManagement.Setup(a => a.IsPlatformAdmin(It.IsAny<string>(), default)).ReturnsAsync(true);

            // run
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.ListHackathon(pagination, search, userId, orderBy, listType, default);

            // verify
            moqs.VerifyAll();

            var list = AssertHelper.AssertOKResult<HackathonList>(result);
            Assert.AreEqual(expectedLink, list.nextLink);
            Assert.AreEqual(1, list.value.Length);
            Assert.AreEqual("pk", list.value[0].name);
            Debug.Assert(optionsCaptured != null);
            Assert.AreEqual(pagination.top, optionsCaptured.Pagination?.top);
            Assert.AreEqual(orderBy, optionsCaptured.OrderBy);
            Assert.AreEqual(userId ?? string.Empty, optionsCaptured.UserId);
            Assert.AreEqual(true, optionsCaptured.IsPlatformAdmin);
            Assert.AreEqual(listType, optionsCaptured.ListType);
            Assert.AreEqual(pagination.np, optionsCaptured.Pagination?.np);
            Assert.AreEqual(pagination.nr, optionsCaptured.Pagination?.nr);
        }
        #endregion

        #region Delete
        [Test]
        public async Task DeleteTest_NotExist()
        {
            string name = "Foo";
            HackathonEntity? entity = null;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Delete(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }

        [Test]
        public async Task DeleteTest_ReadOnly()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { ReadOnly = true };

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Delete(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_ReadOnly);
        }

        [Test]
        public async Task DeleteTest_NotAdmin()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            var authResult = AuthorizationResult.Failed();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Delete(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 403, Resources.Hackathon_NotAdmin);
        }

        [Test]
        public async Task DeleteTest_HasAwardAssignment()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var assignmentCount = 1;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(a => a.GetAssignmentCountAsync("foo", null, default)).ReturnsAsync(assignmentCount);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Delete(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, Resources.Hackathon_HasAwardAssignment);
        }

        [Test]
        public async Task DeleteTest_Success()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { };
            var authResult = AuthorizationResult.Success();
            var assignmentCount = 0;

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);
            moqs.AwardManagement.Setup(a => a.GetAssignmentCountAsync("foo", null, default)).ReturnsAsync(assignmentCount);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonStatusAsync(entity, HackathonStatus.offline, default));
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.deleteHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.deleteHackathon, It.IsAny<object>(), null, default));

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Delete(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertNoContentResult(result);
        }
        #endregion

        #region RequestPublish
        [Test]
        public async Task RequestPublish_AlreadyOnline()
        {
            string name = "Foo";
            HackathonEntity entity = new HackathonEntity { Status = HackathonStatus.online };
            var authResult = AuthorizationResult.Success();

            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("foo", default)).ReturnsAsync(entity);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator)).ReturnsAsync(authResult);

            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.RequestPublish(name, default);

            moqs.VerifyAll();
            AssertHelper.AssertObjectResult(result, 412, string.Format(Resources.Hackathon_AlreadyOnline, name));
        }

        [TestCase(HackathonStatus.planning)]
        [TestCase(HackathonStatus.pendingApproval)]
        public async Task RequestPublish_Succeeded(HackathonStatus status)
        {
            string name = "Hack";
            HackathonEntity entity = new HackathonEntity
            {
                PartitionKey = "pk",
                Status = status,
                DisplayName = "dpn"
            };
            var authResult = AuthorizationResult.Success();
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonStatusAsync(entity, HackathonStatus.pendingApproval, default))
                .Callback<HackathonEntity, HackathonStatus, CancellationToken>((e, s, c) =>
                {
                    e.Status = s;
                })
                .ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.AuthorizationService.Setup(m => m.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), entity, AuthConstant.Policy.HackathonAdministrator))
               .ReturnsAsync(authResult);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("pk", It.IsAny<string>(), ActivityLogType.publishHackathon, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "pk", It.IsAny<string>(), ActivityLogType.publishHackathon, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.RequestPublish(name, default);

            // verify
            moqs.VerifyAll();

            var resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual(HackathonStatus.pendingApproval, resp.status);
        }
        #endregion

        #region Publish
        [TestCase(HackathonStatus.planning)]
        [TestCase(HackathonStatus.pendingApproval)]
        [TestCase(HackathonStatus.offline)]
        public async Task Publish_Succeeded(HackathonStatus status)
        {
            string name = "Hack";
            HackathonEntity entity = new HackathonEntity { PartitionKey = "foo", Status = status, DisplayName = "dpn", };
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonStatusAsync(entity, HackathonStatus.online, default))
                .Callback<HackathonEntity, HackathonStatus, CancellationToken>((e, s, c) =>
                {
                    e.Status = s;
                }).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("foo", It.IsAny<string>(), ActivityLogType.approveHackahton, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity(It.IsAny<string>(), "foo", It.IsAny<string>(), ActivityLogType.approveHackahton, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.Publish(name, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual(HackathonStatus.online, resp.status);
        }
        #endregion

        #region UpdateReadonly
        [TestCase(true)]
        [TestCase(false)]
        public async Task UpdateReadonly(bool readOnly)
        {
            HackathonEntity entity = new HackathonEntity { DisplayName = "dpn", PartitionKey = "pk" };
            var role = new HackathonRoles { isAdmin = true };

            // mock
            var moqs = new Moqs();
            moqs.HackathonManagement.Setup(m => m.GetHackathonEntityByNameAsync("hack", default)).ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(m => m.UpdateHackathonReadOnlyAsync(entity, readOnly, default))
                .Callback<HackathonEntity, bool, CancellationToken>((h, r, c) =>
                {
                    h.ReadOnly = r;
                })
                .ReturnsAsync(entity);
            moqs.HackathonManagement.Setup(h => h.GetHackathonRolesAsync(entity, It.IsAny<ClaimsPrincipal>(), default)).ReturnsAsync(role);
            var logType = readOnly ? ActivityLogType.archiveHackathon : ActivityLogType.unarchiveHackathon;
            moqs.ActivityLogManagement.Setup(a => a.LogHackathonActivity("pk", "", logType, It.IsAny<object>(), null, default));
            moqs.ActivityLogManagement.Setup(a => a.LogUserActivity("", "pk", "", logType, It.IsAny<object>(), null, default));

            // test
            var controller = new HackathonController();
            moqs.SetupController(controller);
            var result = await controller.UpdateReadonly("Hack", readOnly, default);

            // verify
            moqs.VerifyAll();
            var resp = AssertHelper.AssertOKResult<Hackathon>(result);
            Assert.AreEqual(readOnly, resp.readOnly);
        }
        #endregion
    }
}
